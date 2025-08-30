#!/usr/bin/env python3
"""
Print analog stick movements from a specific gamepad on Windows.

Defaults to selecting the 2nd device whose name is "Twin USB Joystick".
Prints left stick (axes 0/1) and, if available, right stick (axes 2/3).
Only prints when movement changes beyond a deadzone / epsilon to avoid spam.

Usage examples:
  python print_sticks.py
  python print_sticks.py --name "Twin USB Joystick" --nth 2
  python print_sticks.py --deadzone 0.10 --interval 0.02

Requires: pygame  (pip install pygame)
"""

import argparse
import sys
import time
from typing import List, Optional, Tuple

import pygame


def find_joystick_index(target_name: str, nth: int) -> Optional[int]:
    """
    Among all connected joysticks, find the 1-based nth device whose name matches target_name.
    Returns the pygame index or None if not found.
    """
    matches: List[int] = []
    for i in range(pygame.joystick.get_count()):
        name = pygame.joystick.Joystick(i).get_name()
        if name == target_name:
            matches.append(i)
    if len(matches) >= nth:
        return matches[nth - 1]
    return None


def select_joystick(target_name: str, nth: int) -> pygame.joystick.Joystick:
    """
    Initialize and return the requested joystick. Raises SystemExit with a helpful message if not found.
    """
    pygame.joystick.quit()
    pygame.joystick.init()

    count = pygame.joystick.get_count()
    if count == 0:
        sys.exit("No joysticks/gamepads detected. Plug one in and try again.")

    idx = find_joystick_index(target_name, nth)
    if idx is None:
        # Build a helpful listing
        names = [pygame.joystick.Joystick(i).get_name() for i in range(count)]
        lines = ["Available devices:"]
        for i, n in enumerate(names):
            lines.append(f"  [{i}] {n}")
        listing = "\n".join(lines)
        sys.exit(
            f'Could not find the {nth} device named "{target_name}".\n'
            f"{listing}\n"
            f'If devices share the same name, try a different --nth (1,2,3...).'
        )

    js = pygame.joystick.Joystick(idx)
    js.init()
    print(
        f'Using joystick index {idx}: "{js.get_name()}" '
        f"(axes={js.get_numaxes()}, buttons={js.get_numbuttons()}, hats={js.get_numhats()})"
    )
    return js


def clamp_deadzone(val: float, deadzone: float) -> float:
    """
    Apply deadzone around zero and round tiny noise away.
    """
    if abs(val) < deadzone:
        return 0.0
    return val


def read_axes(js: pygame.joystick.Joystick, deadzone: float) -> Tuple[float, float, Optional[float], Optional[float]]:
    """
    Returns (lx, ly, rx, ry).
    Right stick may be None if fewer than 4 axes.
    """
    n = js.get_numaxes()
    # SDL/pygame usually map left stick to axes 0/1; right stick to 2/3 (if present),
    # but this can vary by device/driver.
    lx = clamp_deadzone(js.get_axis(0) if n > 0 else 0.0, deadzone)
    ly = clamp_deadzone(js.get_axis(1) if n > 1 else 0.0, deadzone)
    rx = clamp_deadzone(js.get_axis(2) if n > 2 else 0.0, deadzone) if n > 2 else None
    ry = clamp_deadzone(js.get_axis(3) if n > 3 else 0.0, deadzone) if n > 3 else None
    return lx, ly, rx, ry


def changed(a: Optional[float], b: Optional[float], eps: float) -> bool:
    """
    True if either None-state changed or value diff exceeds eps.
    """
    if a is None or b is None:
        return a != b
    return abs(a - b) >= eps


def main() -> None:
    parser = argparse.ArgumentParser(description="Print analog stick movements for a specific controller.")
    parser.add_argument("--name", default="Twin USB Joystick", help='Device name to match (default: "Twin USB Joystick")')
    parser.add_argument("--nth", type=int, default=1, help="Select the nth occurrence (1-based). Default: 1")
    parser.add_argument("--deadzone", type=float, default=0.05, help="Deadzone threshold around 0. Default: 0.05")
    parser.add_argument(
        "--epsilon",
        type=float,
        default=0.01,
        help="Minimum change required to print a new line (spam guard). Default: 0.01",
    )
    parser.add_argument(
        "--interval",
        type=float,
        default=0.01,
        help="Polling interval in seconds (sleep between reads). Default: 0.01 (~100 Hz)",
    )
    args = parser.parse_args()

    pygame.init()
    pygame.display.init()  # not strictly required on Windows, but safe
    pygame.event.set_allowed(None)  # reduce event queue noise
    pygame.event.set_allowed([pygame.JOYAXISMOTION, pygame.JOYDEVICEADDED, pygame.JOYDEVICEREMOVED])

    js = select_joystick(args.name, args.nth)

    prev = (None, None, None, None)  # type: Tuple[Optional[float], Optional[float], Optional[float], Optional[float]]

    print("Press Ctrl+C to quit.\n")
    try:
        while True:
            # Pump pygame events so axis state gets refreshed
            for event in pygame.event.get():
                if event.type == pygame.JOYDEVICEREMOVED:
                    if event.instance_id == js.get_instance_id():
                        print("Selected joystick was removed. Exiting.")
                        return
                elif event.type == pygame.JOYDEVICEADDED:
                    # You could re-scan here if you want hotplug logic.
                    pass

            lx, ly, rx, ry = read_axes(js, args.deadzone)

            if (
                changed(prev[0], lx, args.epsilon)
                or changed(prev[1], ly, args.epsilon)
                or changed(prev[2], rx, args.epsilon)
                or changed(prev[3], ry, args.epsilon)
            ):
                # Build a compact print. Round for readability.
                def fmt(v: Optional[float]) -> str:
                    return "None" if v is None else f"{v:+.3f}"

                if rx is None or ry is None:
                    print(f"L({fmt(lx)}, {fmt(ly)})")
                else:
                    print(f"L({fmt(lx)}, {fmt(ly)})  R({fmt(rx)}, {fmt(ry)})")

                prev = (lx, ly, rx, ry)

            time.sleep(args.interval)
    except KeyboardInterrupt:
        pass
    finally:
        try:
            js.quit()
        except Exception:
            pass
        pygame.quit()


if __name__ == "__main__":
    main()
