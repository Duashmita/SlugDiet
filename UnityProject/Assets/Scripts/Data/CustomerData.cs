using UnityEngine;

namespace UndercoverBarber.Data
{
    [CreateAssetMenu(fileName = "NewCustomer", menuName = "Undercover Barber/Customer Data")]
    public class CustomerData : ScriptableObject
    {
        [Header("Identity")]
        public string customerName;
        public Sprite avatar;
        public CustomerPersonality personality;

        [Header("Haircut")]
        [TextArea(2, 3)]
        public string haircutRequest;

        [Header("Dialogue Pools")]
        [TextArea(2, 3)]
        public string[] smallTalkResponses;
        [TextArea(2, 3)]
        public string[] probeResponses;
        [TextArea(2, 3)]
        public string[] directResponses;

        [Header("AI Chatbot Context")]
        [TextArea(3, 6)]
        public string personalityPrompt;

        public string GetResponse(DialogueType type)
        {
            string[] pool = type switch
            {
                DialogueType.SmallTalk => smallTalkResponses,
                DialogueType.Probe => probeResponses,
                DialogueType.Direct => directResponses,
                _ => smallTalkResponses
            };

            if (pool == null || pool.Length == 0) return "...";
            return pool[Random.Range(0, pool.Length)];
        }
    }

    public enum CustomerPersonality
    {
        Friendly,
        Gruff,
        Nervous,
        Confident,
        Chatty,
        Mysterious
    }

    // Runtime customer instance
    [System.Serializable]
    public class Customer
    {
        public CustomerData baseData;
        public bool IsSuspect { get; private set; }
        public SuspectProfile suspectProfile;
        public bool haircutComplete;
        public int dialogueCount;
        public float suspicionLevel;

        public Customer(CustomerData data)
        {
            baseData = data;
            IsSuspect = false;
            haircutComplete = false;
            dialogueCount = 0;
            suspicionLevel = 0f;
        }

        public void SetAsSuspect(SuspectProfile profile)
        {
            IsSuspect = true;
            suspectProfile = profile;
        }

        public string GetName() => baseData.customerName;
        public Sprite GetAvatar() => baseData.avatar;
        public string GetHaircutRequest()
        {
            if (IsSuspect && !string.IsNullOrEmpty(suspectProfile.specialHaircutRequest))
                return suspectProfile.specialHaircutRequest;
            return baseData.haircutRequest;
        }

        public string GetResponse(DialogueType type)
        {
            // If suspect, sometimes return clue dialogue
            if (IsSuspect && Random.value > 0.5f)
            {
                string clue = suspectProfile.GetRandomClue(type);
                if (!string.IsNullOrEmpty(clue)) return clue;
            }

            return baseData.GetResponse(type);
        }

        public string GetPersonalityPrompt()
        {
            string prompt = baseData.personalityPrompt;

            if (IsSuspect)
            {
                prompt += $"\n\nSECRET: You are actually '{suspectProfile.codename}'. " +
                          $"You have these traits that might slip out: {string.Join(", ", suspectProfile.traits)}. " +
                          "Be subtle but occasionally hint at these traits.";
            }

            return prompt;
        }
    }
}
