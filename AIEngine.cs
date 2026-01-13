using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace RevitAIAgent
{
    public enum AIProvider
    {
        Ollama,   // FREE - Local AI (Recommended)
        OpenAI,
        Gemini
    }

    public class AIEngine
    {
        private static readonly HttpClient client = new HttpClient();
        
        // ===== CONFIGURATION =====
        // Choose your provider: Ollama (FREE), OpenAI, or Gemini
        public static AIProvider Provider = AIProvider.Ollama;  // <-- SWITCHED TO OLLAMA
        
        // Ollama - FREE, runs locally at http://localhost:11434
        public static string OllamaModel = "qwen2.5-coder"; // Much better at coding than llama3.2
        
        // OpenAI (ChatGPT) - Requires API key with credits
        // Set environment variable OPENAI_API_KEY or replace this placeholder
        public static string OpenAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "YOUR_OPENAI_API_KEY_HERE";
        
        // Gemini - Free but has rate limits
        // Set environment variable GEMINI_API_KEY or replace this placeholder
        public static string GeminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "YOUR_GEMINI_API_KEY_HERE";
        // =========================

        public AIEngine()
        {
            client.Timeout = TimeSpan.FromSeconds(600); // 10 minutes for first-time model loading
        }

        public async Task<string> GetAIResponse(string prompt)
        {
            switch (Provider)
            {
                case AIProvider.Ollama:
                    return await CallOllama(prompt);
                case AIProvider.OpenAI:
                    return await CallOpenAI(prompt);
                case AIProvider.Gemini:
                    return await CallGemini(prompt);
                default:
                    return "Error: Unknown provider.";
            }
        }

        private async Task<string> CallOllama(string prompt)
        {
            // Ollama runs locally on port 11434
            string url = "http://localhost:11434/api/generate";

            var requestBody = new
            {
                model = OllamaModel,
                prompt = prompt,
                stream = false
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync(url, httpContent);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"Error from Ollama ({response.StatusCode}): {responseBody}. Is Ollama running?";
                }

                dynamic result = JsonConvert.DeserializeObject(responseBody);
                return result.response;
            }
            catch (HttpRequestException ex)
            {
                return "Error: Cannot connect to Ollama.\n\n" +
                       "Please ensure Ollama is running:\n" +
                       "1. Open Windows Terminal or PowerShell\n" +
                       "2. Run: ollama serve\n" +
                       "3. Wait for it to start, then try again\n\n" +
                       $"Technical details: {ex.Message}";
            }
            catch (TaskCanceledException ex)
            {
                return $"Timeout: Ollama took too long (over 10 minutes). " +
                       $"The model might still be downloading or your request is too complex. " +
                       $"Try a simpler command or wait for the model to finish loading.\n\nDetails: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Exception connecting to Ollama: {ex.Message}";
            }
        }

        private async Task<string> CallOpenAI(string prompt)
        {
            if (string.IsNullOrEmpty(OpenAIKey))
            {
                return "Error: OpenAI API Key is missing.";
            }

            string url = "https://api.openai.com/v1/chat/completions";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 2000
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAIKey}");

            try
            {
                HttpResponseMessage response = await client.PostAsync(url, httpContent);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"Error from OpenAI ({response.StatusCode}): {responseBody}";
                }

                dynamic result = JsonConvert.DeserializeObject(responseBody);
                return result.choices[0].message.content;
            }
            catch (Exception ex)
            {
                return $"Exception connecting to OpenAI: {ex.Message}";
            }
        }

        private async Task<string> CallGemini(string prompt)
        {
            if (string.IsNullOrEmpty(GeminiKey))
            {
                return "Error: Gemini API Key is missing.";
            }

            string model = "gemini-2.0-flash-lite-001";
            string url = $"https://generativelanguage.googleapis.com/v1/models/{model}:generateContent?key={GeminiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 429)
                {
                    return "Rate Limit Exceeded. Please wait ~60 seconds.";
                }

                if (!response.IsSuccessStatusCode)
                {
                    return $"Error from Gemini ({response.StatusCode}): {responseBody}";
                }

                dynamic result = JsonConvert.DeserializeObject(responseBody);
                return result.candidates[0].content.parts[0].text;
            }
            catch (Exception ex)
            {
                return $"Exception connecting to Gemini: {ex.Message}";
            }
        }
    }
}
