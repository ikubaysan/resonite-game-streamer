import socketserver
import json
import pyvjoy
from websocket_server import WebsocketServer

# Button mappings
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

# Track current pressed state (0/1) for each logical button
button_state = {k: 0 for k in BUTTON_MAP.keys()}

# Initialize the vJoy device
vj = pyvjoy.VJoyDevice(1)

def reset_buttons():
    """Set all buttons to unpressed state (vJoy and local state)."""
    for name, btn in BUTTON_MAP.items():
        vj.set_button(btn, 0)
        button_state[name] = 0

def on_client_connected(client, server):
    ip, port = client['address']
    print(f"Client({client['id']}) connected from IP: {ip}")
    reset_buttons()  # Reset buttons when a new client connects
    print("Buttons reset after new client connection")

def on_client_disconnected(client, server):
    """Called when a client disconnects."""
    print(f"Client({client['id']}) disconnected")
    reset_buttons()  # Reset buttons when a client disconnects
    print("Buttons reset after client disconnection")

def on_message(client, server, message):
    print(f"Received message '{message}' from Client({client['id']})")
    if not message or len(message) < 2:
        print(f"Ignoring invalid/short message '{message}'")
        return

    btn_name = message[0]
    try:
        action = int(message[1])  # Convert string '0'/'1' to int
    except ValueError:
        print(f"Invalid message '{message}'")
        return

    # Unknown button guard
    if btn_name not in BUTTON_MAP:
        print(f"Unknown button {btn_name}")
        return

    # ---- Special case: can't have 'x' and 'y' pressed at the same time.
    # EXACT requirement: if 'x' is currently pressed, ignore any attempt to press 'y'.
    # (We still allow 'y0' releases to pass through.)
    # May want to change this if playing a game where I need to press both at the same time.
    if btn_name == "y" and action != 0 and button_state.get("x", 0) == 1:
        print("Ignoring 'y' press because 'x' is currently pressed.")
        server.send_message(client, f"Ignored: {message} (x held)")
        return

    # Process normal logic (including D-pad exclusivity)
    if action == 0:
        # Release logic
        if btn_name in ["u", "d"]:
            vj.set_button(BUTTON_MAP["u"], 0)
            vj.set_button(BUTTON_MAP["d"], 0)
            button_state["u"] = 0
            button_state["d"] = 0
        elif btn_name in ["l", "r"]:
            vj.set_button(BUTTON_MAP["l"], 0)
            vj.set_button(BUTTON_MAP["r"], 0)
            button_state["l"] = 0
            button_state["r"] = 0
        else:
            vj.set_button(BUTTON_MAP[btn_name], 0)
            button_state[btn_name] = 0
        print(f"Button {btn_name} released")
    else:
        # Press logic
        if btn_name == "u":
            vj.set_button(BUTTON_MAP["d"], 0); button_state["d"] = 0
        elif btn_name == "d":
            vj.set_button(BUTTON_MAP["u"], 0); button_state["u"] = 0
        elif btn_name == "l":
            vj.set_button(BUTTON_MAP["r"], 0); button_state["r"] = 0
        elif btn_name == "r":
            vj.set_button(BUTTON_MAP["l"], 0); button_state["l"] = 0

        # Finally press the requested button
        vj.set_button(BUTTON_MAP[btn_name], 1)
        button_state[btn_name] = 1
        print(f"Button {btn_name} pressed")

    server.send_message(client, f"Received: {message}")

server_address = '127.0.0.1'  # localhost for simplicity, change as needed
server_port = 1985

server = WebsocketServer(host=server_address, port=server_port)
server.set_fn_new_client(on_client_connected)
server.set_fn_client_left(on_client_disconnected)
server.set_fn_message_received(on_message)
print(f"Server is running at ws://{server_address}:{server_port}")
server.run_forever()
