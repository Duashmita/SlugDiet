using UnityEngine;
using System.IO;

namespace UndercoverBarber.API
{
    /// <summary>
    /// Utility class to load chatbot configuration from JSON file or environment variables.
    /// This allows keeping API keys out of version control.
    /// </summary>
    public static class ChatbotConfigLoader
    {
        private const string CONFIG_FILE_NAME = "ChatbotConfig";
        private const string ENV_API_KEY = "UNDERCOVER_BARBER_API_KEY";
        private const string ENV_PROVIDER = "UNDERCOVER_BARBER_PROVIDER";

        [System.Serializable]
        private class JsonConfig
        {
            public string provider;
            public string apiKey;
            public string apiEndpoint;
            public string modelName;
            public float temperature;
            public int maxTokens;
            public string systemPrompt;
            public float minRequestInterval;
            public bool useFallbackOnError;
        }

        /// <summary>
        /// Load configuration with priority: Environment Variables > JSON File > ScriptableObject
        /// </summary>
        public static void ApplyConfiguration(ChatbotConfig config)
        {
            if (config == null) return;

            // Try to load from JSON first
            LoadFromJson(config);

            // Override with environment variables if present (highest priority)
            LoadFromEnvironment(config);
        }

        private static void LoadFromJson(ChatbotConfig config)
        {
            TextAsset jsonFile = Resources.Load<TextAsset>(CONFIG_FILE_NAME);

            if (jsonFile != null)
            {
                try
                {
                    JsonConfig jsonConfig = JsonUtility.FromJson<JsonConfig>(jsonFile.text);

                    if (!string.IsNullOrEmpty(jsonConfig.apiKey) && jsonConfig.apiKey != "YOUR_API_KEY_HERE")
                        config.apiKey = jsonConfig.apiKey;

                    if (!string.IsNullOrEmpty(jsonConfig.apiEndpoint))
                        config.apiEndpoint = jsonConfig.apiEndpoint;

                    if (!string.IsNullOrEmpty(jsonConfig.modelName))
                        config.modelName = jsonConfig.modelName;

                    if (!string.IsNullOrEmpty(jsonConfig.provider))
                    {
                        if (System.Enum.TryParse<ChatbotProvider>(jsonConfig.provider, true, out var provider))
                            config.provider = provider;
                    }

                    if (jsonConfig.temperature > 0)
                        config.temperature = jsonConfig.temperature;

                    if (jsonConfig.maxTokens > 0)
                        config.maxTokens = jsonConfig.maxTokens;

                    if (!string.IsNullOrEmpty(jsonConfig.systemPrompt))
                        config.systemPrompt = jsonConfig.systemPrompt;

                    config.minRequestInterval = jsonConfig.minRequestInterval;
                    config.useFallbackOnError = jsonConfig.useFallbackOnError;

                    Debug.Log("Chatbot config loaded from JSON file.");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to parse chatbot config JSON: {e.Message}");
                }
            }
        }

        private static void LoadFromEnvironment(ChatbotConfig config)
        {
            // API Key from environment
            string envApiKey = System.Environment.GetEnvironmentVariable(ENV_API_KEY);
            if (!string.IsNullOrEmpty(envApiKey))
            {
                config.apiKey = envApiKey;
                Debug.Log("Chatbot API key loaded from environment variable.");
            }

            // Provider from environment
            string envProvider = System.Environment.GetEnvironmentVariable(ENV_PROVIDER);
            if (!string.IsNullOrEmpty(envProvider))
            {
                if (System.Enum.TryParse<ChatbotProvider>(envProvider, true, out var provider))
                {
                    config.provider = provider;

                    // Update endpoint based on provider
                    config.apiEndpoint = provider switch
                    {
                        ChatbotProvider.OpenAI => "https://api.openai.com/v1/chat/completions",
                        ChatbotProvider.Anthropic => "https://api.anthropic.com/v1/messages",
                        _ => config.apiEndpoint
                    };
                }
            }
        }

        /// <summary>
        /// Load API key from a local file (not in Resources, for security)
        /// Place a file called 'api_key.txt' in the persistent data path
        /// </summary>
        public static string LoadApiKeyFromFile()
        {
            string keyFilePath = Path.Combine(Application.persistentDataPath, "api_key.txt");

            if (File.Exists(keyFilePath))
            {
                try
                {
                    string key = File.ReadAllText(keyFilePath).Trim();
                    Debug.Log("API key loaded from file.");
                    return key;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to read API key file: {e.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Save API key to local file (for runtime configuration)
        /// </summary>
        public static void SaveApiKeyToFile(string apiKey)
        {
            string keyFilePath = Path.Combine(Application.persistentDataPath, "api_key.txt");

            try
            {
                File.WriteAllText(keyFilePath, apiKey);
                Debug.Log($"API key saved to: {keyFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save API key: {e.Message}");
            }
        }
    }
}
