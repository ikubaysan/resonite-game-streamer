#!/usr/bin/env python3
"""
WebSocket -> vJoy bridge with optional JSON "profile" rules.
Directional inputs now drive vJoy X/Y axes (not buttons, not POV).

- Messages are two-char pairs like "a1", "u0"; batches like "a1u1x0" are fine.
- Also accepts axis messages like "[0.543; -0.295]" (x; y) or "[0.543, -0.295]".
- U/D/L/R map to axes via pyvjoy.VJoyDevice.set_axis(HID_USAGE_X/Y, value)
"""

from __future__ import annotations

import argparse
import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Tuple, Set

import pyvjoy
from websocket_server import WebsocketServer

# ------------------------------ Constants ------------------------------

# Regular buttons only (directions are handled as axes)
BUTTON_MAP: Dict[str, int] = {
    "a": 1,
    "b": 2,
    "x": 3,
    "y": 4,
    # Extra virtuals for combos (example: Doom)
    "j": 5,
    "k": 6,
}

DIRECTIONS = ("u", "d", "l", "r")

# vJoy axis constants & values
AXIS_X = pyvjoy.HID_USAGE_X
AXIS_Y = pyvjoy.HID_USAGE_Y
AXIS_MIN = 0x0000
AXIS_MID = 0x4000
AXIS_MAX = 0x8000

# Float comparisons: treat very small values as zero when parsing [x; y]
EPS_ZERO = 1e-6

# ------------------------------ Profiles & Rules ------------------------------

@dataclass
class MutualExclusion:
    block: str
    while_: str  # 'while' is a keyword in Python

    @staticmethod
    def from_dict(d: Dict[str, str]) -> "MutualExclusion":
        return MutualExclusion(block=d["block"], while_=d["while"])


@dataclass
class ComboPress:
    emit: str
    when: List[str]

    @staticmethod
    def from_dict(d: Dict[str, object]) -> "ComboPress":
        emit = str(d["emit"])
        when_list = [str(x) for x in d.get("when", [])]
        return ComboPress(emit=emit, when=when_list)


@dataclass
class InputProfile:
    disabled_buttons: List[str] = field(default_factory=list)
    mutual_exclusions: List[MutualExclusion] = field(default_factory=list)
    combo_presses: List[ComboPress] = field(default_factory=list)

    @staticmethod
    def load(path: Path) -> "InputProfile":
        data = json.loads(path.read_text(encoding="utf-8"))
        disabled = data.get("disabled_buttons", []) or []
        mex = [MutualExclusion.from_dict(m) for m in (data.get("mutual_exclusions", []) or [])]
        combos = [ComboPress.from_dict(c) for c in (data.get("combo_presses", []) or [])]
        return InputProfile(disabled_buttons=disabled, mutual_exclusions=mex, combo_presses=combos)


class InputRuleEngine:
    def __init__(self, profile: Optional[InputProfile]) -> None:
        self.profile = profile

    @staticmethod
    def _is_press(action: int) -> bool:
        return action != 0

    def should_block(self, btn: str, action: int, state: Dict[str, int]) -> Optional[str]:
        if self.profile is None:
            return None

        # Disabled buttons: affect only presses; do not block releases
        if self._is_press(action) and btn in self.profile.disabled_buttons:
            return f"'{btn}' is disabled by profile"

        # Mutual exclusions: only for presses
        if self._is_press(action):
            for rule in self.profile.mutual_exclusions:
                if btn == rule.block and state.get(rule.while_, 0) == 1:
                    return f"'{btn}' blocked while '{rule.while_}' is held (profile)"
        return None

    def desired_combo_emit_state(self, state: Dict[str, int]) -> Dict[str, int]:
        result: Dict[str, int] = {}
        if not self.profile or not self.profile.combo_presses:
            return result

        disabled: Set[str] = set(self.profile.disabled_buttons)

        for combo in self.profile.combo_presses:
            if not combo.when:
                continue
            all_pressed = all(state.get(btn, 0) == 1 for btn in combo.when)
            desired = 1 if all_pressed else 0
            if combo.emit in disabled:
                desired = 0
            if combo.emit in result:
                result[combo.emit] = 1 if (result[combo.emit] == 1 or desired == 1) else 0
            else:
                result[combo.emit] = desired
        return result

# ------------------------------ Controller ------------------------------

class VJoyController:
    """
    Thin wrapper around pyvjoy with local button state tracking.
    Directions (u/d/l/r) drive X/Y axes:
      - X: left = MIN, right = MAX, neutral = MID
      - Y: up = MIN,  down = MAX, neutral = MID
    Diagonals supported by resolving horizontal and vertical independently
    to the latest-still-held in each axis family.
    """
    def __init__(self, device_id: int = 1) -> None:
        self.vj = pyvjoy.VJoyDevice(device_id)

        # Logical state for profiles/combos (buttons + directions)
        self.state: Dict[str, int] = {k: 0 for k in list(BUTTON_MAP.keys()) + list(DIRECTIONS)}

        # Track which directions are held, and recency per axis
        self._held: Set[str] = set()        # subset of DIRECTIONS
        self._horiz_hist: List[str] = []    # entries 'l' or 'r'
        self._vert_hist: List[str] = []     # entries 'u' or 'd'

        # Initialize axes neutral
        self._apply_axes()

    # ---- Buttons ----
    def set_button(self, name: str, pressed: bool) -> None:
        if name in DIRECTIONS:
            self.set_direction(name, pressed)
            return
        btn_id = BUTTON_MAP[name]
        self.vj.set_button(btn_id, 1 if pressed else 0)
        self.state[name] = 1 if pressed else 0

    def reset_all(self) -> None:
        # Reset buttons
        for name, btn in BUTTON_MAP.items():
            self.vj.set_button(btn, 0)
            self.state[name] = 0

        # Reset directions & axes
        self._held.clear()
        self._horiz_hist.clear()
        self._vert_hist.clear()
        self.state.update({d: 0 for d in DIRECTIONS})
        self._apply_axes()

    # ---- Directions → Axes ----
    def set_direction(self, name: str, pressed: bool) -> None:
        if name not in DIRECTIONS:
            return

        if pressed:
            self._held.add(name)
            if name in ("l", "r"):
                self._horiz_hist.append(name)
            else:
                self._vert_hist.append(name)
            self.state[name] = 1
        else:
            self._held.discard(name)
            if name in ("l", "r"):
                self._horiz_hist = [h for h in self._horiz_hist if h != name]
            else:
                self._vert_hist = [v for v in self._vert_hist if v != name]
            self.state[name] = 0

        self._apply_axes()

    def _resolve_axis(self, hist: List[str], neg_name: str, pos_name: str) -> int:
        """
        From a history list (e.g., ['l','r','l']) choose the latest entry still held.
        Map negative to MIN, positive to MAX, none to MID.
        """
        choice = None
        for name in reversed(hist):
            if name in self._held:
                choice = name
                break

        if choice == neg_name:
            return AXIS_MIN
        if choice == pos_name:
            return AXIS_MAX
        return AXIS_MID

    def _apply_axes(self) -> None:
        x_val = self._resolve_axis(self._horiz_hist, "l", "r")
        y_val = self._resolve_axis(self._vert_hist, "u", "d")

        # Write to vJoy
        self.vj.set_axis(AXIS_X, x_val)
        self.vj.set_axis(AXIS_Y, y_val)

# ------------------------------ Bridge (WebSocket callbacks) ------------------------------

class VJoyWebSocketBridge:
    """
    WebSocket server → vJoy bridge with optional profile-based rules.
    - Accepts messages as two-character pairs: <button><0|1>
      e.g. "a1" (press A), "y0" (release Y).
    - Also supports concatenated pairs in one message: e.g. "a1y1u0".
    - NEW: Accepts axis messages like "[x; y]" or "[x, y]" where x,y are floats in [-1,1].
           Sign determines active directions (diagonals supported).
    """
    def __init__(self, host: str, port: int, profile: Optional[InputProfile]) -> None:
        self.host = host
        self.port = port
        self.controller = VJoyController(device_id=1)
        self.rules = InputRuleEngine(profile=profile)
        self.server = WebsocketServer(host=self.host, port=self.port)

        # Bind callbacks
        self.server.set_fn_new_client(self._on_client_connected)
        self.server.set_fn_client_left(self._on_client_disconnected)
        self.server.set_fn_message_received(self._on_message)

    # ------------- WebSocketServer callbacks -------------

    def _on_client_connected(self, client: dict, server: WebsocketServer) -> None:
        ip, _ = client["address"]
        print(f"Client({client['id']}) connected from IP: {ip}")
        self.controller.reset_all()
        print("Inputs reset after new client connection")

    def _on_client_disconnected(self, client: dict, server: WebsocketServer) -> None:
        print(f"Client({client['id']}) disconnected")
        self.controller.reset_all()
        print("Inputs reset after client disconnection")

    def _on_message(self, client: dict, server: WebsocketServer, message: str) -> None:
        print(f"Received '{message}' from Client({client['id']})")

        # First: bracketed vector form like "[0.543; -0.295]"
        if self._looks_like_axis_vector(message):
            parsed = self._parse_axis_vector(message)
            if parsed is None:
                print(f"Ignoring invalid vector message '{message}' (failed to parse)")
                return
            x, y = parsed
            self._apply_axis_vector(client, server, x, y)
            server.send_message(client, f"Received vector: x={x:.6f}, y={y:.6f}")
            return

        # Otherwise: fall back to two-char pairs
        pairs = self._parse_pairs(message)
        if not pairs:
            print(f"Ignoring invalid message '{message}'")
            return

        for btn_name, action in pairs:
            self._handle_one(client, server, btn_name, action)

        # Evaluate combos once per message
        self._apply_combos()

        server.send_message(client, f"Received: {message}")

    # ------------- Helpers: bracketed axis parsing -------------

    @staticmethod
    def _looks_like_axis_vector(msg: str) -> bool:
        # Identify by starting with '[' and ending with ']'
        return len(msg) >= 5 and msg[0] == "[" and msg[-1] == "]"

    @staticmethod
    def _parse_axis_vector(msg: str) -> Optional[Tuple[float, float]]:
        """
        Accepts variants like:
          "[0.5; -0.25]"
          "[ 0.5 , -0.25 ]"
          "[0;0]"
        Returns (x, y) or None if invalid.
        """
        try:
            body = msg[1:-1].strip()
            # normalize separators to ';'
            body = body.replace(",", ";")
            parts = [p.strip() for p in body.split(";") if p.strip() != ""]
            if len(parts) != 2:
                return None
            x = float(parts[0])
            y = float(parts[1])
            return (x, y)
        except Exception:
            return None

    def _apply_axis_vector(self, client: dict, server: WebsocketServer, x: float, y: float) -> None:
        """
        Apply axis sign → direction states:
          x > 0 => right=1, left=0
          x < 0 => left=1,  right=0
          x == 0 => left=0, right=0
          (same for y with up/down)
        """
        # Decide desired directional states using sign with tiny epsilon for 'zero'
        def sgn(v: float) -> int:
            if v > EPS_ZERO:
                return 1
            if v < -EPS_ZERO:
                return -1
            return 0

        sx = sgn(x)
        sy = sgn(y)

        desired: Dict[str, int] = {
            "l": 1 if sx < 0 else 0,
            "r": 1 if sx > 0 else 0,
            "u": 1 if sy > 0 else 0,
            "d": 1 if sy < 0 else 0,
        }

        # Pretty print what we're doing
        active = [k for k, v in desired.items() if v == 1]
        if active:
            print(f"[vector] x={x:.6f}, y={y:.6f} → activate {', '.join(active)}; neutralize others")
        else:
            print(f"[vector] x={x:.6f}, y={y:.6f} → all neutral")

        # Route via the same per-button path so profile rules still apply
        # (process all four directions every time to keep state consistent)
        for dir_name in ("l", "r", "u", "d"):
            self._handle_one(client, server, dir_name, desired[dir_name])

        # Evaluate combos after applying vector-driven directions
        self._apply_combos()

    # ------------- Helpers: pair parsing -------------

    def _parse_pairs(self, msg: str) -> List[Tuple[str, int]]:
        pairs: List[Tuple[str, int]] = []
        if len(msg) < 2:
            return pairs

        def _valid_button(ch: str) -> bool:
            # allow both regular buttons and direction letters
            return ch in BUTTON_MAP or ch in DIRECTIONS

        def _valid_action(ch: str) -> bool:
            return ch in ("0", "1")

        for i in range(0, len(msg) - 1, 2):
            b, a = msg[i], msg[i + 1]
            if _valid_button(b) and _valid_action(a):
                pairs.append((b, int(a)))
            else:
                break

        return pairs

    def _handle_one(self, client: dict, server: WebsocketServer, btn_name: str, action: int) -> None:
        # Apply profile rules (if any)
        reason = self.rules.should_block(btn_name, action, self.controller.state)
        if reason is not None:
            print(f"Ignoring {btn_name}{action}: {reason}")
            server.send_message(client, f"Ignored: {btn_name}{action} ({reason})")
            return

        is_dir = btn_name in DIRECTIONS
        pressed = (action != 0)

        # Route to axes for directions; buttons otherwise
        self.controller.set_button(btn_name, pressed)
        print(f"{'Dir' if is_dir else 'Button'} {btn_name} {'pressed' if pressed else 'released'}")

    def _apply_combos(self) -> None:
        desired = self.rules.desired_combo_emit_state(self.controller.state)
        if not desired:
            return

        for emit_btn, want in desired.items():
            current = self.controller.state.get(emit_btn, 0)
            if want == 1 and current == 0:
                self.controller.set_button(emit_btn, True)
                print(f"[combo] '{emit_btn}' pressed (conditions met)")
            elif want == 0 and current == 1:
                self.controller.set_button(emit_btn, False)
                print(f"[combo] '{emit_btn}' released (conditions no longer met)")

    # ------------- Run -------------

    def run_forever(self) -> None:
        print(f"Server is running at ws://{self.host}:{self.port}")
        self.server.run_forever()

# ------------------------------ CLI ------------------------------

def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="WebSocket → vJoy bridge with optional JSON profile rules.")
    p.add_argument("--host", default="127.0.0.1", help="Bind host (default: 127.0.0.1)")
    p.add_argument("--port", type=int, default=1985, help="Bind port (default: 1985)")
    p.add_argument("--profile", type=str, default=None,
                   help="Profile name (resolved to ./profiles/<name>.json) or a direct path to a JSON file")
    return p.parse_args()

def _load_profile_from_path(path: Path) -> InputProfile:
    profile = InputProfile.load(path)
    print(
        f"Loaded profile from {path} "
        f"(disabled={profile.disabled_buttons}, "
        f"mutual_exclusions={[f'{m.block}|while {m.while_}' for m in profile.mutual_exclusions]}, "
        f"combos={[f'{c.emit}|when {c.when}' for c in profile.combo_presses]})"
    )
    return profile

def maybe_load_profile(profile_arg: Optional[str]) -> Optional[InputProfile]:
    if not profile_arg:
        return None

    path = Path(profile_arg)

    if path.exists() and path.is_file():
        return _load_profile_from_path(path)

    name = path.stem if path.suffix.lower() == ".json" else path.name
    candidate = Path.cwd() / "profiles" / f"{name}.json"

    if candidate.exists() and candidate.is_file():
        return _load_profile_from_path(candidate)

    raise FileNotFoundError(f"Profile not found: tried '{profile_arg}' and '{candidate}'")

def main() -> None:
    args = parse_args()
    profile = maybe_load_profile(args.profile)
    bridge = VJoyWebSocketBridge(host=args.host, port=args.port, profile=profile)
    bridge.run_forever()

if __name__ == "__main__":
    main()
