import requests
import json

OLLAMA_URL = "http://localhost:11434/api/generate"
MODEL = "qwen2.5-coder:latest"

# 1. Load Golden Patterns (The "Fast Track")
GOLDEN_CACHE = {}
LEARNED_CACHE = {}
KNOWLEDGE_BASE = {}

def load_caches():
    global GOLDEN_CACHE, LEARNED_CACHE, KNOWLEDGE_BASE
    try:
        with open("golden_patterns.json", "r") as f:
            GOLDEN_CACHE = json.load(f)
    except: pass
    
    try:
        with open("learned_patterns.json", "r") as f:
            LEARNED_CACHE = json.load(f)
    except: pass
    
    try:
        with open("knowledge_base.json", "r") as f:
            KNOWLEDGE_BASE = json.load(f).get("concepts", {})
    except: pass

load_caches()

SYSTEM_PROMPT = """
You are an expert Revit API Developer for C# .NET.
Your task is to generate C# code to automate compliance tasks.
You are running inside a Revit Addin context. 
'doc', 'uidoc', and 'uiapp' are already available.

RULES:
1. Output ONLY valid C# lines of code. No markdown, no class definitions.
2. Use implicit typing (var) where possible.
3. If asked to select, use uidoc.Selection.SetElementIds.
4. DO NOT create Transactions. A Transaction ("AI Dynamic Actions") is ALREADY open. Starting another one will crash Revit.
5. Just write the creation/modification logic directly.
"""

def get_live_context():
    try:
        with open(r"C:\Temp\BIMism_Context.json", "r") as f:
            return json.load(f)
    except:
        return {}

def ask_ai(user_prompt):
    # 2. Check Cache First (Zero Latency)
    for key, code in GOLDEN_CACHE.items():
        if key.lower() in user_prompt.lower():
            return code, "⚡ INSTANT (Golden Cache)"

    for key, code in LEARNED_CACHE.items():
        if key.lower() in user_prompt.lower():
            return code, "🧠 LEARNED (Auto-Evolution)"

    # 3. Load Live Context & RAG Knowledge
    context = get_live_context()
    context_str = json.dumps(context, indent=2) if context else "No context available."
    
    # RAG: Find Relevant Concepts
    rag_docs = []
    for topic, data in KNOWLEDGE_BASE.items():
        # Simple Keyword Match for now (Can be semantic later)
        # e.g. "Wall" in user_prompt -> Inject Wall Creation
        keywords = topic.lower().split()
        if any(k in user_prompt.lower() for k in keywords) or topic.lower() in user_prompt.lower():
            rag_docs.append(f"Docs [{topic}]: {data['description']}\nExample: {data['code']}")
            
    rag_context = "\n\n".join(rag_docs)
    
    # 4. Fallback to Slow AI (Context Aware)
    full_prompt = f"{SYSTEM_PROMPT}\n\nPROJECT CONTEXT:\n{context_str}\n\nAPI DOCUMENTATION (RAG):\n{rag_context}\n\nUSER: {user_prompt}\n\CODE:"
    
    payload = {
        "model": MODEL,
        "prompt": full_prompt,
        "stream": False,
        "options": {
            "temperature": 0.2
        }
    }
    
    try:
        response = requests.post(OLLAMA_URL, json=payload)
        response.raise_for_status()
        result = response.json()
        return result.get("response", "").strip(), "🧠 THINKING (Gen-AI)"
    except Exception as e:
        return f"// Error: {str(e)}", "❌ ERROR"

def ask_ai_fix(original_prompt, bad_code, error_msg):
    full_prompt = f"""{SYSTEM_PROMPT}

CONTEXT:
User asked: "{original_prompt}"
You generated this code:
{bad_code}

It failed with this error:
{error_msg}

TASK:
Fix the code. Do not apologize. Just output the corrected C# code.
CODE:"""
    
    payload = {
        "model": MODEL,
        "prompt": full_prompt,
        "stream": False,
        "options": {
            "temperature": 0.1 # Lower temp for fixes
        }
    }
    
    try:
        response = requests.post(OLLAMA_URL, json=payload)
        response.raise_for_status()
        result = response.json()
        return result.get("response", "").strip(), "🔧 SELF-CORRECTION"
    except Exception as e:
        return f"Error contacting AI: {e}", "❌ ERROR"
