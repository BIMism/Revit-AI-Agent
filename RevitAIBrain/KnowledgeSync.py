import json
import os

def sync_knowledge():
    # 1. Read Python Brain (Golden Patterns)
    try:
        with open("golden_patterns.json", "r") as f:
            golden_data = json.load(f)
    except:
        print("No golden patterns found.")
        return

    # 2. Format for C# Brain (RevitKnowledge.json)
    knowledge_list = []
    
    for prompt, code in golden_data.items():
        # Create keywords from prompt
        # e.g. "Create a simple roof" -> ["create", "simple", "roof"]
        keywords = prompt.lower().split()
        keywords = [k for k in keywords if len(k) > 2 and k not in ["the", "and", "for", "with"]]
        
        entry = {
            "keywords": keywords,
            "description": prompt,
            "code": code
        }
        knowledge_list.append(entry)

    # 3. Write to Revit Pluging Assets
    output_path = r"..\Assets\RevitKnowledge.json"
    
    # Backup existing
    if os.path.exists(output_path):
        os.rename(output_path, output_path + ".bak")
        
    with open(output_path, "w") as f:
        json.dump(knowledge_list, f, indent=4)
        
    print(f"Successfully synced {len(knowledge_list)} patterns to Revit Sidebar.")

if __name__ == "__main__":
    sync_knowledge()
