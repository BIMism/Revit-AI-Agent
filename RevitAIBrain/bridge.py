import json
import os
import time
import uuid

BRIDGE_FILE = r"C:\Temp\BIMism_Bridge.json"

def ensure_bridge_file():
    directory = os.path.dirname(BRIDGE_FILE)
    if not os.path.exists(directory):
        os.makedirs(directory)
    
    if not os.path.exists(BRIDGE_FILE):
        with open(BRIDGE_FILE, 'w') as f:
            json.dump({"status": "IDLE"}, f)

def send_command(code):
    ensure_bridge_file()
    
    command_id = str(uuid.uuid4())
    command = {
        "CommandId": command_id,
        "Action": "EXECUTE_CODE",
        "Code": code,
        "Status": "PENDING",
        "Result": ""
    }
    
    try:
        with open(BRIDGE_FILE, 'w') as f:
            json.dump(command, f, indent=4)
        return command_id
    except Exception as e:
        print(f"Error writing bridge file: {e}")
        return None

def get_status(command_id):
    try:
        with open(BRIDGE_FILE, 'r') as f:
            data = json.load(f)
            
        if data.get("CommandId") == command_id:
            return data.get("Status"), data.get("Result")
        
        return "UNKNOWN", ""
    except:
        return "ERROR", ""
