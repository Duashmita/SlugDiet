using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UndercoverBarber.Core;
using UndercoverBarber.Data;
using UndercoverBarber.API;

namespace UndercoverBarber.Gameplay
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxDialoguesPerCustomer = 5;
        [SerializeField] private bool useAIChatbot = true;

        [Header("Player Lines")]
        [SerializeField] private string[] smallTalkLines = {
            "So, nice weather today...",
            "Catch any good games lately?",
            "Come here often?"
        };

        [SerializeField] private string[] probeLines = {
            "What do you do for work?",
            "You from around here?",
            "Family man?"
        };

        [SerializeField] private string[] directLines = {
            "You seem a bit on edge...",
            "You look familiar. Been in trouble before?",
            "What brings you to this neighborhood?"
        };

        // Events
        public UnityEvent<string, bool> OnDialogueAdded; // message, isPlayer
        public UnityEvent<string> OnThoughtBubble;
        public UnityEvent OnDialogueLimitReached;
        public UnityEvent<float> OnSuspicionChanged;

        private Customer currentCustomer;
        private int dialogueCount;
        private float suspicionLevel;
        private List<DialogueEntry> dialogueHistory = new List<DialogueEntry>();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCustomerChanged.AddListener(OnNewCustomer);
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCustomerChanged.RemoveListener(OnNewCustomer);
            }
        }

        private void OnNewCustomer(Customer customer)
        {
            currentCustomer = customer;
            dialogueCount = 0;
            suspicionLevel = 0;
            dialogueHistory.Clear();

            // Initialize chatbot with customer context
            if (useAIChatbot && ChatbotService.Instance != null)
            {
                ChatbotService.Instance.StartNewConversation(customer);
            }

            // Customer greets with haircut request
            AddDialogue(customer.GetHaircutRequest(), false);
        }

        public void InitiateDialogue(DialogueType type)
        {
            if (currentCustomer == null) return;
            if (dialogueCount >= maxDialoguesPerCustomer)
            {
                OnDialogueLimitReached?.Invoke();
                return;
            }

            dialogueCount++;

            // Get player line
            string playerLine = GetPlayerLine(type);
            AddDialogue(playerLine, true);

            // Update suspicion based on dialogue type
            UpdateSuspicion(type);

            // Get customer response
            if (useAIChatbot && ChatbotService.Instance != null)
            {
                // Use AI chatbot
                ChatbotService.Instance.SendMessage(playerLine, type, (response) =>
                {
                    AddDialogue(response, false);
                    CheckForSuspectClues();
                });
            }
            else
            {
                // Use preset responses
                string response = currentCustomer.GetResponse(type);
                AddDialogue(response, false);
                CheckForSuspectClues();
            }

            // Check if dialogue limit reached
            if (dialogueCount >= maxDialoguesPerCustomer)
            {
                OnDialogueLimitReached?.Invoke();
            }
        }

        private string GetPlayerLine(DialogueType type)
        {
            string[] pool = type switch
            {
                DialogueType.SmallTalk => smallTalkLines,
                DialogueType.Probe => probeLines,
                DialogueType.Direct => directLines,
                _ => smallTalkLines
            };

            return pool[Random.Range(0, pool.Length)];
        }

        private void UpdateSuspicion(DialogueType type)
        {
            float suspicionIncrease = type switch
            {
                DialogueType.SmallTalk => 0f,
                DialogueType.Probe => 10f,
                DialogueType.Direct => 25f,
                _ => 0f
            };

            suspicionLevel = Mathf.Clamp(suspicionLevel + suspicionIncrease, 0f, 100f);
            OnSuspicionChanged?.Invoke(suspicionLevel);

            // If suspect gets too suspicious, they might comment on it
            if (suspicionLevel > 50f && currentCustomer.IsSuspect)
            {
                AddDialogue("*eyes narrow* You ask a lot of questions for a barber...", false);
            }
        }

        private void CheckForSuspectClues()
        {
            if (currentCustomer.IsSuspect && dialogueCount >= 2)
            {
                // Show player thought bubble
                string[] thoughts = {
                    "Hmm, something about this one...",
                    "Wait, that matches the briefing...",
                    "Could this be our suspect?",
                    "That trait seems familiar..."
                };

                OnThoughtBubble?.Invoke(thoughts[Random.Range(0, thoughts.Length)]);
            }
        }

        private void AddDialogue(string message, bool isPlayer)
        {
            var entry = new DialogueEntry
            {
                message = message,
                isPlayer = isPlayer,
                timestamp = Time.time
            };

            dialogueHistory.Add(entry);
            OnDialogueAdded?.Invoke(message, isPlayer);
        }

        public void AddSystemMessage(string message)
        {
            AddDialogue(message, false);
        }

        public float GetSuspicionLevel() => suspicionLevel;
        public int GetDialogueCount() => dialogueCount;
        public int GetMaxDialogues() => maxDialoguesPerCustomer;
        public bool CanContinueDialogue() => dialogueCount < maxDialoguesPerCustomer;
    }

    [System.Serializable]
    public class DialogueEntry
    {
        public string message;
        public bool isPlayer;
        public float timestamp;
    }
}
