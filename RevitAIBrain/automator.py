import json
import time
import brain
import bridge
import threading
import os
from PIL import ImageGrab
from datetime import datetime

class AutoRunner:
    def __init__(self, log_callback):
        self.log_callback = log_callback
        self.stop_requested = False
        self.queue = []
        self.current_task = None
        
        # Ensure Report Dir
        self.report_dir = r"C:\Temp\BIMism_Reports"
        if not os.path.exists(self.report_dir):
            os.makedirs(self.report_dir)

    def capture_evidence(self, task_name, status):
        try:
            timestamp = datetime.now().strftime("%H%M%S")
            safe_name = "".join(x for x in task_name if x.isalnum())[:20]
            filename = f"{status}_{safe_name}_{timestamp}.png"
            path = os.path.join(self.report_dir, filename)
            
            # Capture full screen (Proof)
            screenshot = ImageGrab.grab()
            screenshot.save(path)
            return path
        except:
            return None
        
    def load_tasks(self):
        try:
            with open("golden_patterns.json", "r") as f:
                data = json.load(f)
                
            self.queue = []
            # Prioritize the new training items
            keys = list(data.keys())
            # Reverse to show most recent (complex) ones first? No, simple first.
            
            for key in keys:
                self.queue.append({"category": "Training", "prompt": key})
            
            self.log_callback("System", f"Loaded {len(self.queue)} Golden Patterns for Visual Verification.")
        except Exception as e:
            self.log_callback("Error", f"Failed to load tasks: {e}")

    def start(self):
        self.stop_requested = False
        threading.Thread(target=self._run_loop).start()

    def stop(self):
        self.stop_requested = True

    def _run_loop(self):
        if not self.queue:
            self.log_callback("System", "Auto-loading tasks...")
            self.load_tasks()

        if not self.queue:
            self.log_callback("Error", "❌ No tasks loaded. Check capabilities.json")
            return

        self.log_callback("System", ">>> 👨‍🏫 STARTING TRAINING SESSION (ANTIGRAVITY vs AI) <<<")
        
        total = len(self.queue)
        for i, task in enumerate(self.queue):
            if self.stop_requested: break
            
            prompt = task["prompt"]
            cat = task["category"]
            
            self.log_callback("System", f"👨‍🏫 Trainer: Task {i+1}/{total}: '{prompt}'")
            
            success = self._execute_task_with_retry(prompt)
            
            if success:
                self.log_callback("Revit", "✅ Student: Task Completed & Verified.")
                evidence = self.capture_evidence(prompt, "PASS")
                if evidence: self.log_callback("System", f"📸 Visual Proof Saved: {os.path.basename(evidence)}")
            else:
                self.log_callback("Revit", "❌ Student: Failed. (I will retrain this later).")
                self.capture_evidence(prompt, "FAIL")
            
            time.sleep(2) # Cooldown betwen tasks
            
        self.log_callback("System", ">>> 🎓 TRAINING SESSION COMPLETE. ALL TASKS VERIFIED. <<<")

    def _execute_task_with_retry(self, prompt):
        MAX_RETRIES = 3
        current_code = None
        last_error = None
        
        for attempt in range(MAX_RETRIES + 1):
            if self.stop_requested: return False
            
            # Generate or Fix Code
            if attempt == 0:
                code, source = brain.ask_ai(prompt)
            else:
                self.log_callback("System", f"   ↳ Attempt {attempt} Self-Correction...")
                code, source = brain.ask_ai_fix(prompt, current_code, last_error)
            
            # Clean Code
            code = code.replace("```csharp", "").replace("```", "").strip()
            current_code = code

            # Execute
            cmd_id = bridge.send_command(code)
            if not cmd_id: return False
            
            # Poll
            for _ in range(40):
                time.sleep(0.5)
                status, result = bridge.get_status(cmd_id)
                
                if status == "SUCCESS":
                    return True
                elif status == "ERROR":
                    last_error = result
                    break # Break poll loop, trigger retry loop
            else:
                last_error = "Timeout" # If poll loop finishes without break
                
        return False
