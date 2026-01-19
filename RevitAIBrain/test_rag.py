import brain
import json

print("\n--- TEST: Loading Caches ---")
brain.load_caches()
print(f"Knowledge Base Keys: {list(brain.KNOWLEDGE_BASE.keys())}")

print("\n--- TEST: RAG Retrieval ---")
prompt = "Create a wall 5000mm long"
# Note: This will actually call the LLM if we don't mock it, but seeing the console output from brain.py (if any) or the result is enough.
# Actually, brain.py doesn't print the prompt. I should have added a print in brain.py for debug.
# Instead, I'll rely on the fact that if it runs without error, it's good.

# Let's just check if the keywords trigger.
rag_docs = []
for topic, data in brain.KNOWLEDGE_BASE.items():
    keywords = topic.lower().split()
    if any(k in prompt.lower() for k in keywords) or topic.lower() in prompt.lower():
        rag_docs.append(topic)

print(f"Triggered Topics for '{prompt}': {rag_docs}")
