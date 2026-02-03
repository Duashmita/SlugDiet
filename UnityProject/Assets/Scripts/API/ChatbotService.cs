using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UndercoverBarber.Data;

namespace UndercoverBarber.API
{
    public class ChatbotService : MonoBehaviour
    {
        public static ChatbotService Instance { get; private set; }

        [SerializeField] private ChatbotConfig config;

        private float lastRequestTime;
        private List<ChatMessage> conversationHistory = new List<ChatMessage>();

        public event Action<string> OnResponseReceived;
        public event Action<string> OnError;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void StartNewConversation(Customer customer)
        {
            conversationHistory.Clear();

            // Add system prompt with character context
            string systemMessage = config.systemPrompt + "\n\n" + customer.GetPersonalityPrompt();
            conversationHistory.Add(new ChatMessage("system", systemMessage));
        }

        public void SendMessage(string playerMessage, DialogueType dialogueType, Action<string> callback)
        {
            if (!config.IsConfigured)
            {
                Debug.LogWarning("Chatbot API not configured. Using fallback responses.");
                callback?.Invoke(GetFallbackResponse(dialogueType));
                return;
            }

            // Rate limiting
            if (Time.time - lastRequestTime < config.minRequestInterval)
            {
                Debug.Log("Rate limited. Using fallback.");
                callback?.Invoke(GetFallbackResponse(dialogueType));
                return;
            }

            conversationHistory.Add(new ChatMessage("user", playerMessage));
            StartCoroutine(SendAPIRequest(callback, dialogueType));
        }

        private IEnumerator SendAPIRequest(Action<string> callback, DialogueType fallbackType)
        {
            lastRequestTime = Time.time;

            string jsonBody = BuildRequestBody();

            using (UnityWebRequest request = new UnityWebRequest(config.apiEndpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                // Set headers based on provider
                request.SetRequestHeader("Content-Type", "application/json");

                switch (config.provider)
                {
                    case ChatbotProvider.OpenAI:
                        request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                        break;
                    case ChatbotProvider.Anthropic:
                        request.SetRequestHeader("x-api-key", config.apiKey);
                        request.SetRequestHeader("anthropic-version", "2023-06-01");
                        break;
                    case ChatbotProvider.Custom:
                        request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                        break;
                }

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = ParseResponse(request.downloadHandler.text);
                    conversationHistory.Add(new ChatMessage("assistant", response));
                    OnResponseReceived?.Invoke(response);
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"API Error: {request.error}");
                    OnError?.Invoke(request.error);

                    if (config.useFallbackOnError)
                    {
                        callback?.Invoke(GetFallbackResponse(fallbackType));
                    }
                }
            }
        }

        private string BuildRequestBody()
        {
            switch (config.provider)
            {
                case ChatbotProvider.OpenAI:
                    return BuildOpenAIRequest();
                case ChatbotProvider.Anthropic:
                    return BuildAnthropicRequest();
                default:
                    return BuildOpenAIRequest();
            }
        }

        private string BuildOpenAIRequest()
        {
            var messages = new List<object>();
            foreach (var msg in conversationHistory)
            {
                messages.Add(new { role = msg.role, content = msg.content });
            }

            var requestObj = new
            {
                model = config.modelName,
                messages = messages,
                temperature = config.temperature,
                max_tokens = config.maxTokens
            };

            return JsonUtility.ToJson(new OpenAIRequest
            {
                model = config.modelName,
                temperature = config.temperature,
                max_tokens = config.maxTokens,
                messages = conversationHistory.ToArray()
            });
        }

        private string BuildAnthropicRequest()
        {
            // Anthropic has different format - system is separate
            string systemPrompt = "";
            var messages = new List<ChatMessage>();

            foreach (var msg in conversationHistory)
            {
                if (msg.role == "system")
                    systemPrompt = msg.content;
                else
                    messages.Add(msg);
            }

            return JsonUtility.ToJson(new AnthropicRequest
            {
                model = config.modelName,
                max_tokens = config.maxTokens,
                system = systemPrompt,
                messages = messages.ToArray()
            });
        }

        private string ParseResponse(string json)
        {
            try
            {
                switch (config.provider)
                {
                    case ChatbotProvider.OpenAI:
                        var openAIResponse = JsonUtility.FromJson<OpenAIResponse>(json);
                        return openAIResponse.choices[0].message.content;

                    case ChatbotProvider.Anthropic:
                        var anthropicResponse = JsonUtility.FromJson<AnthropicResponse>(json);
                        return anthropicResponse.content[0].text;

                    default:
                        return ParseGenericResponse(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse response: {e.Message}");
                return "...";
            }
        }

        private string ParseGenericResponse(string json)
        {
            // Try to find common response patterns
            if (json.Contains("\"content\":"))
            {
                int start = json.IndexOf("\"content\":\"") + 11;
                int end = json.IndexOf("\"", start);
                if (start > 10 && end > start)
                    return json.Substring(start, end - start);
            }
            return "...";
        }

        private string GetFallbackResponse(DialogueType type)
        {
            // Fallback responses when API is unavailable
            string[] fallbacks = type switch
            {
                DialogueType.SmallTalk => new[] {
                    "Yeah, nice day isn't it?",
                    "Can't complain, I suppose.",
                    "Mmhmm, sure is."
                },
                DialogueType.Probe => new[] {
                    "Oh, you know, this and that.",
                    "Nothing too exciting.",
                    "I keep busy."
                },
                DialogueType.Direct => new[] {
                    "That's a strange question...",
                    "Why do you ask?",
                    "I'd rather not say."
                },
                _ => new[] { "..." }
            };

            return fallbacks[UnityEngine.Random.Range(0, fallbacks.Length)];
        }

        public void ClearHistory()
        {
            conversationHistory.Clear();
        }
    }

    // Data structures for API communication
    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [Serializable]
    public class OpenAIRequest
    {
        public string model;
        public float temperature;
        public int max_tokens;
        public ChatMessage[] messages;
    }

    [Serializable]
    public class OpenAIResponse
    {
        public OpenAIChoice[] choices;
    }

    [Serializable]
    public class OpenAIChoice
    {
        public ChatMessage message;
    }

    [Serializable]
    public class AnthropicRequest
    {
        public string model;
        public int max_tokens;
        public string system;
        public ChatMessage[] messages;
    }

    [Serializable]
    public class AnthropicResponse
    {
        public AnthropicContent[] content;
    }

    [Serializable]
    public class AnthropicContent
    {
        public string type;
        public string text;
    }
}
