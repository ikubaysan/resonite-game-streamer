#!/usr/bin/env python3
"""
vjoy_ws_bridge.py

WebSocket → vJoy bridge with optional JSON "profile" rules.

Usage examples:
  # no profile (no special rules):
  python vjoy_ws_bridge.py --host 127.0.0.1 --port 1985

  # use a profile by name (resolved to ./profiles/<name>.json from current working dir):
  python vjoy_ws_bridge.py --host 127.0.0.1 --port 1985 --profile pokemon

  # explicit file (relative or absolute) also works:
  python vjoy_ws_bridge.py --profile ./profiles/pokemon.json
  python vjoy_ws_bridge.py --profile /abs/path/to/pokemon.json

JSON profile shape (all optional keys):
{
  "disabled_buttons": ["b"],
  "mutual_exclusions": [
    { "block": "b", "while": "y" },
    { "block": "y", "while": "b" }
  ]
}
"""

from __future__ import annotations

import argparse
import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Tuple

import pyvjoy
from websocket_server import WebsocketServer


# ------------------------------ Constants ------------------------------

BUTTON_MAP: Dict[str, int] = {
    "a": 1,
    "b": 2,
    "x": 3,
    "y": 4,
    "u": 5,
    "d": 6,
    "l": 7,
    "r": 8,
    "j": 9,
    "k": 10,
}

D_PAD_GROUPS: List[Tuple[str, str]] = [
    ("u", "d"),
    ("l", "r"),
]


# ------------------------------ Profiles & Rules ------------------------------

@dataclass
class MutualExclusion:
    """
    A single rule: block 'block' while 'while_' is currently pressed.
    Example: {"block": "b", "while": "y"} blocks b-press while y is held.
    """
    block: str
    while_: str  # 'while' is a keyword in Python

    @staticmethod
    def from_dict(d: Dict[str, str]) -> "MutualExclusion":
        return MutualExclusion(block=d["block"], while_=d["while"])


@dataclass
class InputProfile:
    """
    A profile of input rules loaded from JSON. All fields optional in the file.
    """
    disabled_buttons: List[str] = field(default_factory=list)
    mutual_exclusions: List[MutualExclusion] = field(default_factory=list)

    @staticmethod
    def load(path: Path) -> "InputProfile":
        data = json.loads(path.read_text(encoding="utf-8"))
        disabled = data.get("disabled_buttons", []) or []
        mex = [MutualExclusion.from_dict(m) for m in (data.get("mutual_exclusions", []) or [])]
        return InputProfile(disabled_buttons=disabled, mutual_exclusions=mex)


class InputRuleEngine:
    """
    Evaluates whether a given (button, action) should be blocked based on:
      - disabled buttons (presses are blocked; releases allowed for safety)
      - mutual exclusions (press of 'block' is blocked while 'while' is held)
    """
    def __init__(self, profile: Optional[InputProfile]) -> None:
        self.profile = profile

    @staticmethod
    def _is_press(action: int) -> bool:
        return action != 0

    def should_block(self, btn: str, action: int, state: Dict[str, int]) -> Optional[str]:
        """
        Returns a human-readable reason string if the input should be blocked, else None.
        """
        if self.profile is None:
            return None  # No profile -> no special rules

        # Disabled buttons: block presses; allow releases so nothing gets "stuck"
        if self._is_press(action) and btn in self.profile.disabled_buttons:
            return f"'{btn}' is disabled by profile"

        # Mutual exclusions: only apply to presses; releases should pass
        if self._is_press(action):
            for rule in self.profile.mutual_exclusions:
                if btn == rule.block and state.get(rule.while_, 0) == 1:
                    return f"'{btn}' blocked while '{rule.while_}' is held (profile)"

        return None


# ------------------------------ Controller ------------------------------

class VJoyController:
    """
    Thin wrapper around pyvjoy with local button state tracking.
    """
    def __init__(self, device_id: int = 1) -> None:
        self.vj = pyvjoy.VJoyDevice(device_id)
        self.state: Dict[str, int] = {k: 0 for k in BUTTON_MAP.keys()}

    def reset_all(self) -> None:
        for name, btn in BUTTON_MAP.items():
            self.vj.set_button(btn, 0)
            self.state[name] = 0

    def set_button(self, name: str, pressed: bool) -> None:
        btn_id = BUTTON_MAP[name]
        self.vj.set_button(btn_id, 1 if pressed else 0)
        self.state[name] = 1 if pressed else 0

    def enforce_dpad_exclusivity_on_press(self, btn: str) -> None:
        """
        If a directional is pressed, ensure its opposite is released
        to maintain exclusivity (U↔D, L↔R).
        """
        for a, b in D_PAD_GROUPS:
            if btn == a:
                self.set_button(b, False)
            elif btn == b:
                self.set_button(a, False)

    def paired_release_behavior(self, btn: str) -> None:
        """
        On release of one d-pad in a pair, release both in that pair.
        """
        for a, b in D_PAD_GROUPS:
            if btn in (a, b):
                self.set_button(a, False)
                self.set_button(b, False)
                break


# ------------------------------ Bridge (WebSocket callbacks) ------------------------------

class VJoyWebSocketBridge:
    """
    WebSocket server → vJoy bridge with optional profile-based rules.
    - Accepts messages as two-character pairs: <button><0|1>
      e.g. "a1" (press A), "y0" (release Y).
    - Also supports concatenated pairs in one message: e.g. "a1y1u0".
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
        print("Buttons reset after new client connection")

    def _on_client_disconnected(self, client: dict, server: WebsocketServer) -> None:
        print(f"Client({client['id']}) disconnected")
        self.controller.reset_all()
        print("Buttons reset after client disconnection")

    def _on_message(self, client: dict, server: WebsocketServer, message: str) -> None:
        print(f"Received '{message}' from Client({client['id']})")

        # Support batches like "a1y0u1" (multiple of 2), fall back to first pair if short
        pairs = self._parse_pairs(message)
        if not pairs:
            print(f"Ignoring invalid message '{message}'")
            return

        for btn_name, action in pairs:
            self._handle_one(client, server, btn_name, action)

        server.send_message(client, f"Received: {message}")

    # ------------- Helpers -------------

    def _parse_pairs(self, msg: str) -> List[Tuple[str, int]]:
        pairs: List[Tuple[str, int]] = []
        if len(msg) < 2:
            return pairs

        def _valid_button(ch: str) -> bool:
            return ch in BUTTON_MAP

        def _valid_action(ch: str) -> bool:
            return ch in ("0", "1")

        # Walk in steps of 2; if trailing junk exists, ignore it
        for i in range(0, len(msg) - 1, 2):
            b, a = msg[i], msg[i + 1]
            if _valid_button(b) and _valid_action(a):
                pairs.append((b, int(a)))
            else:
                # Stop on first invalid pair to avoid partial misreads
                break

        return pairs

    def _handle_one(self, client: dict, server: WebsocketServer, btn_name: str, action: int) -> None:
        # Guard unknown (shouldn't happen thanks to parsing)
        if btn_name not in BUTTON_MAP:
            print(f"Unknown button '{btn_name}'")
            return

        # Apply profile rules (if any)
        reason = self.rules.should_block(btn_name, action, self.controller.state)
        if reason is not None:
            print(f"Ignoring {btn_name}{action}: {reason}")
            server.send_message(client, f"Ignored: {btn_name}{action} ({reason})")
            return

        # Core logic (preserves original behavior)
        if action == 0:
            # Release
            if btn_name in ("u", "d", "l", "r"):
                # Paired release: release both in the pair
                self.controller.paired_release_behavior(btn_name)
            else:
                self.controller.set_button(btn_name, False)
            print(f"Button {btn_name} released")
        else:
            # Press
            if btn_name in ("u", "d", "l", "r"):
                # Enforce opposite off on d-pad press
                self.controller.enforce_dpad_exclusivity_on_press(btn_name)
            self.controller.set_button(btn_name, True)
            print(f"Button {btn_name} pressed")

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
        f"mutual_exclusions={[f'{m.block}|while {m.while_}' for m in profile.mutual_exclusions]})"
    )
    return profile


def maybe_load_profile(profile_arg: Optional[str]) -> Optional[InputProfile]:
    """
    Try to resolve --profile argument to a JSON file:
      1. If profile_arg is None -> return None.
      2. If it exists directly (absolute or relative) -> load it.
      3. Otherwise, treat it as a profile name and look in ./profiles/<name>.json.
         The '.json' extension is optional when passing just the name.
    """
    if not profile_arg:
        return None

    path = Path(profile_arg)

    # Case 1: already a valid file path (absolute or relative)
    if path.exists() and path.is_file():
        return _load_profile_from_path(path)

    # Case 2: treat as a profile name under ./profiles/<name>.json
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
