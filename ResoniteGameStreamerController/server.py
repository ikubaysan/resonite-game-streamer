import json
import pyvjoy
from websocket_server import WebsocketServer


class VJoyController:
    """
    Wraps vJoy operations and button state/logic.
    Mirrors the original functional behavior:
      - Button map and local pressed-state tracking
      - b/y mutual exclusivity: ignore a press if the other is held; allow releases
      - D-pad axis exclusivity: u<->d and l<->r
      - Reset all on connect/disconnect
    """
    BUTTON_MAP = {
        "a": 1,
        "b": 2,
        "x": 3,
        "y": 4,
        "u": 5,
        "d": 6,
        "l": 7,
        "r": 8,
    }

    def __init__(self, device_id: int = 1) -> None:
        self.vj = pyvjoy.VJoyDevice(device_id)
        self.button_state = {k: 0 for k in self.BUTTON_MAP.keys()}

    def reset_buttons(self) -> None:
        """Set all buttons to unpressed state (vJoy and local state)."""
        for name, btn in self.BUTTON_MAP.items():
            self.vj.set_button(btn, 0)
            self.button_state[name] = 0

    def _set_pressed(self, name: str) -> None:
        """Press a single logical button, updating vJoy + local state."""
        self.vj.set_button(self.BUTTON_MAP[name], 1)
        self.button_state[name] = 1
        print(f"Button {name} pressed")

    def _set_released(self, name: str) -> None:
        """Release a single logical button, updating vJoy + local state."""
        self.vj.set_button(self.BUTTON_MAP[name], 0)
        self.button_state[name] = 0
        print(f"Button {name} released")

    def _release_axis_pair(self, a: str, b: str) -> None:
        """Release both buttons in the given axis pair."""
        self._set_released(a)
        self._set_released(b)

    def process_message(self, message: str, client, server) -> None:
        """
        Parses and applies the same logic as the original on_message:
          - Validate 2+ chars
          - btn_name is char 0, action is char 1 -> int
          - Unknown button check
          - b/y mutual exclusion on press
          - D-pad exclusivity both on press and release (axis-wise)
          - Echo back 'Received: <message>'
        """
        print(f"Received message '{message}' from Client({client['id']})")

        if not message or len(message) < 2:
            print(f"Ignoring invalid/short message '{message}'")
            return

        btn_name = message[0]
        try:
            action = int(message[1])
        except ValueError:
            print(f"Invalid message '{message}'")
            return

        # Unknown button guard
        if btn_name not in self.BUTTON_MAP:
            print(f"Unknown button {btn_name}")
            return

        # b/y mutual-exclusion on press: ignore a press if the other is held
        # (releases are always allowed to pass through)
        if btn_name == "b" and action != 0 and self.button_state.get("y", 0) == 1:
            print("Ignoring 'b' press because 'y' is currently pressed.")
            server.send_message(client, f"Ignored: {message} (y held)")
            return
        if btn_name == "y" and action != 0 and self.button_state.get("b", 0) == 1:
            print("Ignoring 'y' press because 'b' is currently pressed.")
            server.send_message(client, f"Ignored: {message} (b held)")
            return

        # Process normal logic (including D-pad exclusivity)
        if action == 0:
            # Release logic
            if btn_name in ["u", "d"]:
                # Release the whole vertical axis (u & d)
                self._release_axis_pair("u", "d")
            elif btn_name in ["l", "r"]:
                # Release the whole horizontal axis (l & r)
                self._release_axis_pair("l", "r")
            else:
                self._set_released(btn_name)
        else:
            # Press logic (release opposite on same axis first)
            if btn_name == "u":
                self._set_released("d")
            elif btn_name == "d":
                self._set_released("u")
            elif btn_name == "l":
                self._set_released("r")
            elif btn_name == "r":
                self._set_released("l")

            self._set_pressed(btn_name)

        # Echo back to the client (same as original)
        server.send_message(client, f"Received: {message}")


class VJoyWebSocketBridge:
    """
    Bridges WebSocketServer callbacks to VJoyController.
    Keeps the same side effects:
      - Reset all buttons on client connect/disconnect
      - Print informative messages
    """
    def __init__(self, host: str = "127.0.0.1", port: int = 1985, device_id: int = 1) -> None:
        self.host = host
        self.port = int(port)
        self.controller = VJoyController(device_id=device_id)
        self.server = WebsocketServer(host=self.host, port=self.port)

        # Bind handlers
        self.server.set_fn_new_client(self._on_client_connected)
        self.server.set_fn_client_left(self._on_client_disconnected)
        self.server.set_fn_message_received(self._on_message)

    # --- WebSocket handlers ---

    def _on_client_connected(self, client, server) -> None:
        ip, _port = client["address"]
        print(f"Client({client['id']}) connected from IP: {ip}")
        self.controller.reset_buttons()
        print("Buttons reset after new client connection")

    def _on_client_disconnected(self, client, server) -> None:
        print(f"Client({client['id']}) disconnected")
        self.controller.reset_buttons()
        print("Buttons reset after client disconnection")

    def _on_message(self, client, server, message: str) -> None:
        self.controller.process_message(message, client, server)

    def run(self) -> None:
        print(f"Server is running at ws://{self.host}:{self.port}")
        self.server.run_forever()


if __name__ == "__main__":
    # Same defaults as the original script
    server_address = "127.0.0.1"
    server_port = 1985

    bridge = VJoyWebSocketBridge(host=server_address, port=server_port, device_id=1)
    bridge.run()
