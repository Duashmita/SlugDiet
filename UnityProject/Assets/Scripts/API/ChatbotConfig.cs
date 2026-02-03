using UnityEngine;

namespace UndercoverBarber.API
{
    [CreateAssetMenu(fileName = "ChatbotConfig", menuName = "Undercover Barber/Chatbot Config")]
    public class ChatbotConfig : ScriptableObject
    {
        [Header("API Provider")]
        public ChatbotProvider provider = ChatbotProvider.OpenAI;

        [Header("API Settings")]
        [Tooltip("Your API key - Keep this secret! Consider using environment variables in production.")]
        public string apiKey = "YOUR_API_KEY_HERE";

        [Tooltip("API endpoint URL")]
        public string apiEndpoint = "https://api.openai.com/v1/chat/completions";

        [Header("Model Settings")]
        public string modelName = "gpt-3.5-turbo";

        [Range(0f, 2f)]
        public float temperature = 0.8f;

        [Range(1, 500)]
        public int maxTokens = 150;

        [Header("Game Context")]
        [TextArea(5, 10)]
        public string systemPrompt = @"You are playing a character in a barbershop game.
You are a customer getting a haircut from an undercover cop (the player).
Stay in character based on the personality provided.
Keep responses short (1-3 sentences) and conversational.
Never break character or mention that you're an AI.";

        [Header("Rate Limiting")]
        [Tooltip("Minimum seconds between API calls")]
        public float minRequestInterval = 1f;

        [Tooltip("Fallback to offline responses if API fails")]
        public bool useFallbackOnError = true;

        public bool IsConfigured => !string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_API_KEY_HERE";
    }

    public enum ChatbotProvider
    {
        OpenAI,
        Anthropic,
        Custom
    }
}
