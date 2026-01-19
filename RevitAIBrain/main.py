import customtkinter as ctk
import threading
import time
import brain
import bridge

import automator

ctk.set_appearance_mode("Dark")
ctk.set_default_color_theme("blue")

class BIMismApp(ctk.CTk):
    def __init__(self):
        super().__init__()
        
        self.runner = automator.AutoRunner(self.log)

        self.title("BIMism AI Agent (Python Engine)")
        self.geometry("500x700")
        self.attributes("-topmost", True) # Keep on top

        # Grid layout
        self.grid_rowconfigure(0, weight=1)
        self.grid_columnconfigure(0, weight=1)

        # Chat History
        self.chat_display = ctk.CTkTextbox(self, width=480, height=500)
        self.chat_display.grid(row=0, column=0, padx=10, pady=10, sticky="nsew")
        self.chat_display.insert("0.0", "System: Initialized. Connected to Revit Bridge.\n")
        self.chat_display.configure(state="disabled")

        # Automation Controls
        self.auto_frame = ctk.CTkFrame(self)
        self.auto_frame.grid(row=1, column=0, padx=10, pady=5, sticky="ew")
        
        self.btn_load = ctk.CTkButton(self.auto_frame, text="Load Tasks", command=self.load_tasks)
        self.btn_load.pack(side="left", padx=5, pady=5)
        
        self.btn_run = ctk.CTkButton(self.auto_frame, text="▶ Run Auto-Test", fg_color="green", command=self.start_auto)
        self.btn_run.pack(side="left", padx=5, pady=5)
        
        self.btn_stop = ctk.CTkButton(self.auto_frame, text="⏹ Stop", fg_color="red", command=self.stop_auto)
        self.btn_stop.pack(side="left", padx=5, pady=5)

        # Status Bar
        self.status_frame = ctk.CTkFrame(self, height=30)
        self.status_frame.grid(row=3, column=0, padx=10, pady=5, sticky="ew")
        
        self.lbl_status = ctk.CTkLabel(self.status_frame, text="🔴 Revit Link: Disconnected", text_color="red")
        self.lbl_status.pack(side="left", padx=10)

        self.input_frame = ctk.CTkFrame(self)
        self.input_frame.grid(row=2, column=0, padx=10, pady=10, sticky="ew")
        self.input_frame.grid_columnconfigure(0, weight=1)

        # Input Area (Fixed: Created ONCE)
        self.entry = ctk.CTkEntry(self.input_frame, placeholder_text="Type command here...")
        self.entry.grid(row=0, column=0, padx=5, pady=5, sticky="ew")
        self.entry.bind("<Return>", self.send_message)

        self.send_btn = ctk.CTkButton(self.input_frame, text="Send", width=60, command=self.send_message)
        self.send_btn.grid(row=0, column=1, padx=5, pady=5)

        # Start Status Loop
        self.after(2000, self.check_connection)

    def check_connection(self):
        try:
            import os
            import time
            import json
            path = r"C:\Temp\BIMism_Context.json"
            if os.path.exists(path):
                mtime = os.path.getmtime(path)
                age = time.time() - mtime
                
                # Try to read context
                view_name = "Unknown"
                try:
                    with open(path, 'r') as f:
                        data = json.load(f)
                        view_name = data.get("ActiveView", "Unknown")
                except: pass

                if age < 60: # Active in last minute
                    self.lbl_status.configure(text=f"🟢 Connected to: {view_name}", text_color="green")
                else:
                    self.lbl_status.configure(text=f"🟡 Idle (Last seen: {view_name})", text_color="orange")
            else:
                self.lbl_status.configure(text="🔴 Disconnected (No Context Found)", text_color="red")
        except:
             self.lbl_status.configure(text="🔴 connection Error", text_color="red")
        
        self.after(2000, self.check_connection)


        
        self.after(2000, self.check_connection)

    def log(self, sender, message):
        self.chat_display.configure(state="normal")
        self.chat_display.insert("end", f"{sender}: {message}\n\n")
        self.chat_display.see("end")
        self.chat_display.configure(state="disabled")

    def send_message(self, event=None):
        text = self.entry.get()
        if not text: return
        
        self.entry.delete(0, "end")
        self.log("You", text)
        
        # Run in thread to not freeze UI
        threading.Thread(target=self.process_command, args=(text,)).start()

    def process_command(self, text):
        self._run_attempt(text, retry_count=0, previous_code=None)

    def _run_attempt(self, text, retry_count, previous_code, error_msg=None):
        MAX_RETRIES = 3
        
        if retry_count == 0:
            self.log("AI", "Thinking...")
            code, source = brain.ask_ai(text)
        else:
            self.log("System", f"⚠️ Error detected. Attempting Self-Correction ({retry_count}/{MAX_RETRIES})...")
            code, source = brain.ask_ai_fix(text, previous_code, error_msg)
            
        # Clean up markdown
        code = code.replace("```csharp", "").replace("```", "").strip()
        
        self.log("System", f"Source: {source}")
        self.log("AI", f"Running Code:\n{code}")
        
        # Send to Bridge
        cmd_id = bridge.send_command(code)
        if not cmd_id:
            self.log("System", "Failed to write to Bridge file.")
            return

        # Poll for result
        for _ in range(40): # Wait up to 20 seconds
            time.sleep(0.5)
            status, result = bridge.get_status(cmd_id)
            if status == "SUCCESS":
                self.log("Revit", "✅ Execution Success")
                # Auto-Evolution Step
                if "SELF-CORRECTION" in str(locals().get('source', '')) or "Gen-AI" in str(locals().get('source', '')):
                     brain.save_learning(text, code)
                     self.log("System", "🧠 Knowledge Saved! Next run will be INSTANT.")
                return
            elif status == "ERROR":
                self.log("Revit", f"❌ Error: {result}")
                
                if retry_count < MAX_RETRIES:
                    # Recursive Retry
                    self._run_attempt(text, retry_count + 1, code, result)
                else:
                    self.log("System", "❌ Maximum retries reached. Fix failed.")
                return
        
        self.log("System", "⏱️ Timeout waiting for Revit.")

    def load_tasks(self):
        self.runner.load_tasks()

    def start_auto(self):
        self.runner.start()

    def stop_auto(self):
        self.runner.stop()
        self.log("System", "Automation stopped by user.")

if __name__ == "__main__":
    app = BIMismApp()
    app.mainloop()
