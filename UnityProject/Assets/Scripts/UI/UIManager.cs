using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UndercoverBarber.Core;
using UndercoverBarber.Data;
using UndercoverBarber.Gameplay;
using UndercoverBarber.Chase;

namespace UndercoverBarber.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screen Panels")]
        [SerializeField] private GameObject titleScreen;
        [SerializeField] private GameObject briefingScreen;
        [SerializeField] private GameObject barbershopScreen;
        [SerializeField] private GameObject streetChaseScreen;
        [SerializeField] private GameObject carChaseScreen;
        [SerializeField] private GameObject resultScreen;

        [Header("Title Screen")]
        [SerializeField] private Button startButton;

        [Header("Briefing Screen")]
        [SerializeField] private TextMeshProUGUI suspectCodenameText;
        [SerializeField] private TextMeshProUGUI suspectTraitsText;
        [SerializeField] private TextMeshProUGUI suspectActivityText;
        [SerializeField] private Image suspectSilhouette;
        [SerializeField] private Button beginMissionButton;

        [Header("Barbershop Screen")]
        [SerializeField] private TextMeshProUGUI customerNameText;
        [SerializeField] private Image customerAvatarImage;
        [SerializeField] private TextMeshProUGUI customerCountText;
        [SerializeField] private TextMeshProUGUI reputationText;
        [SerializeField] private Slider haircutProgressSlider;
        [SerializeField] private Button[] dialogueButtons;
        [SerializeField] private Button nextCustomerButton;
        [SerializeField] private Button suspectButton;
        [SerializeField] private Transform dialogueContainer;
        [SerializeField] private GameObject dialogueEntryPrefab;
        [SerializeField] private GameObject thoughtBubble;
        [SerializeField] private TextMeshProUGUI thoughtText;

        [Header("Street Chase Screen")]
        [SerializeField] private TextMeshProUGUI streetDistanceText;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Button sprintButton;
        [SerializeField] private Button[] streetLaneButtons;

        [Header("Car Chase Screen")]
        [SerializeField] private TextMeshProUGUI carDistanceText;
        [SerializeField] private Slider carHealthSlider;
        [SerializeField] private Button nitroButton;
        [SerializeField] private Button[] carLaneButtons;

        [Header("Result Screen")]
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultMessageText;
        [SerializeField] private TextMeshProUGUI resultStatsText;
        [SerializeField] private Image resultIcon;
        [SerializeField] private Sprite victoryIcon;
        [SerializeField] private Sprite defeatIcon;
        [SerializeField] private Button playAgainButton;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            SetupButtons();
            SubscribeToEvents();
        }

        private void SetupButtons()
        {
            // Title
            startButton?.onClick.AddListener(() => GameManager.Instance?.ChangeState(GameState.Briefing));

            // Briefing
            beginMissionButton?.onClick.AddListener(() => GameManager.Instance?.ChangeState(GameState.Barbershop));

            // Barbershop
            if (dialogueButtons != null)
            {
                for (int i = 0; i < dialogueButtons.Length; i++)
                {
                    int type = i;
                    dialogueButtons[i]?.onClick.AddListener(() =>
                        DialogueManager.Instance?.InitiateDialogue((DialogueType)type));
                }
            }

            nextCustomerButton?.onClick.AddListener(() => GameManager.Instance?.NextCustomer());
            suspectButton?.onClick.AddListener(() => GameManager.Instance?.IdentifySuspect());

            // Chase
            sprintButton?.onClick.AddListener(() => FindObjectOfType<StreetChaseController>()?.Sprint());
            nitroButton?.onClick.AddListener(() => FindObjectOfType<CarChaseController>()?.UseNitro());

            // Lane buttons
            SetupLaneButtons();

            // Result
            playAgainButton?.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        }

        private void SetupLaneButtons()
        {
            if (streetLaneButtons != null)
            {
                for (int i = 0; i < streetLaneButtons.Length; i++)
                {
                    int lane = i;
                    if (i == 0)
                        streetLaneButtons[i]?.onClick.AddListener(() =>
                            FindObjectOfType<StreetChaseController>()?.MoveLeft());
                    else if (i == 1)
                        streetLaneButtons[i]?.onClick.AddListener(() =>
                            FindObjectOfType<StreetChaseController>()?.Jump());
                    else
                        streetLaneButtons[i]?.onClick.AddListener(() =>
                            FindObjectOfType<StreetChaseController>()?.MoveRight());
                }
            }

            if (carLaneButtons != null)
            {
                for (int i = 0; i < carLaneButtons.Length; i++)
                {
                    int lane = i;
                    carLaneButtons[i]?.onClick.AddListener(() =>
                        FindObjectOfType<CarChaseController>()?.ChangeLane(lane));
                }
            }
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged.AddListener(OnStateChanged);
                GameManager.Instance.OnReputationChanged.AddListener(UpdateReputation);
                GameManager.Instance.OnCustomerChanged.AddListener(OnCustomerChanged);
                GameManager.Instance.OnGameEnded.AddListener(ShowResult);
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueAdded.AddListener(AddDialogueEntry);
                DialogueManager.Instance.OnThoughtBubble.AddListener(ShowThought);
                DialogueManager.Instance.OnDialogueLimitReached.AddListener(OnDialogueLimitReached);
            }

            if (HaircutController.Instance != null)
            {
                HaircutController.Instance.OnProgressChanged.AddListener(UpdateHaircutProgress);
            }

            // Subscribe to chase events
            var streetChase = FindObjectOfType<StreetChaseController>();
            if (streetChase != null)
            {
                streetChase.OnDistanceChanged.AddListener(UpdateStreetDistance);
                streetChase.OnStaminaChanged.AddListener(UpdateStamina);
            }

            var carChase = FindObjectOfType<CarChaseController>();
            if (carChase != null)
            {
                carChase.OnDistanceChanged.AddListener(UpdateCarDistance);
                carChase.OnHealthChanged.AddListener(UpdateCarHealth);
            }
        }

        private void OnStateChanged(GameState state)
        {
            // Hide all screens
            titleScreen?.SetActive(false);
            briefingScreen?.SetActive(false);
            barbershopScreen?.SetActive(false);
            streetChaseScreen?.SetActive(false);
            carChaseScreen?.SetActive(false);
            resultScreen?.SetActive(false);

            // Show appropriate screen
            switch (state)
            {
                case GameState.Title:
                    titleScreen?.SetActive(true);
                    break;
                case GameState.Briefing:
                    briefingScreen?.SetActive(true);
                    UpdateBriefingScreen();
                    break;
                case GameState.Barbershop:
                    barbershopScreen?.SetActive(true);
                    break;
                case GameState.StreetChase:
                    streetChaseScreen?.SetActive(true);
                    break;
                case GameState.CarChase:
                    carChaseScreen?.SetActive(true);
                    break;
                case GameState.Result:
                    resultScreen?.SetActive(true);
                    break;
            }
        }

        private void UpdateBriefingScreen()
        {
            var suspect = GameManager.Instance?.CurrentSuspect;
            if (suspect == null) return;

            if (suspectCodenameText != null)
                suspectCodenameText.text = suspect.codename;

            if (suspectTraitsText != null)
                suspectTraitsText.text = string.Join("\n• ", suspect.traits);

            if (suspectActivityText != null)
                suspectActivityText.text = suspect.lastKnownActivity;

            if (suspectSilhouette != null && suspect.silhouette != null)
                suspectSilhouette.sprite = suspect.silhouette;
        }

        private void OnCustomerChanged(Customer customer)
        {
            if (customerNameText != null)
                customerNameText.text = customer.GetName();

            if (customerAvatarImage != null && customer.GetAvatar() != null)
                customerAvatarImage.sprite = customer.GetAvatar();

            if (customerCountText != null)
                customerCountText.text = $"{GameManager.Instance.CurrentCustomerIndex + 1}/5";

            // Reset progress
            if (haircutProgressSlider != null)
                haircutProgressSlider.value = 0;

            // Clear dialogue
            ClearDialogue();

            // Enable dialogue buttons
            SetDialogueButtonsEnabled(true);

            // Show suspect button
            if (suspectButton != null)
                suspectButton.gameObject.SetActive(true);
        }

        private void UpdateReputation(int reputation)
        {
            if (reputationText != null)
            {
                string stars = new string('★', reputation) + new string('☆', 5 - reputation);
                reputationText.text = stars;
            }
        }

        private void UpdateHaircutProgress(float progress)
        {
            if (haircutProgressSlider != null)
                haircutProgressSlider.value = progress / 100f;
        }

        private void AddDialogueEntry(string message, bool isPlayer)
        {
            if (dialogueContainer == null || dialogueEntryPrefab == null) return;

            GameObject entry = Instantiate(dialogueEntryPrefab, dialogueContainer);
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = message;

            // Style based on speaker
            var bg = entry.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = isPlayer
                    ? new Color(0.06f, 0.2f, 0.38f)
                    : new Color(0.09f, 0.13f, 0.24f);
            }

            // Scroll to bottom
            StartCoroutine(ScrollToBottom());
        }

        private IEnumerator ScrollToBottom()
        {
            yield return null;
            var scrollRect = dialogueContainer.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        private void ClearDialogue()
        {
            if (dialogueContainer == null) return;

            foreach (Transform child in dialogueContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void ShowThought(string thought)
        {
            if (thoughtBubble == null) return;

            if (thoughtText != null)
                thoughtText.text = thought;

            thoughtBubble.SetActive(true);
            StartCoroutine(HideThoughtAfterDelay(3f));
        }

        private IEnumerator HideThoughtAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            thoughtBubble?.SetActive(false);
        }

        private void OnDialogueLimitReached()
        {
            SetDialogueButtonsEnabled(false);
        }

        private void SetDialogueButtonsEnabled(bool enabled)
        {
            if (dialogueButtons != null)
            {
                foreach (var btn in dialogueButtons)
                {
                    if (btn != null)
                        btn.interactable = enabled;
                }
            }
        }

        // Chase UI updates
        private void UpdateStreetDistance(float distance)
        {
            if (streetDistanceText != null)
                streetDistanceText.text = $"{Mathf.Max(0, Mathf.RoundToInt(distance))}m";
        }

        private void UpdateStamina(float staminaPercent)
        {
            if (staminaSlider != null)
                staminaSlider.value = staminaPercent;
        }

        private void UpdateCarDistance(float distance)
        {
            if (carDistanceText != null)
                carDistanceText.text = $"{Mathf.Max(0, Mathf.RoundToInt(distance))}m";
        }

        private void UpdateCarHealth(float healthPercent)
        {
            if (carHealthSlider != null)
                carHealthSlider.value = healthPercent;
        }

        private void ShowResult(bool victory, string message)
        {
            if (resultTitleText != null)
                resultTitleText.text = victory ? "MISSION COMPLETE" : "MISSION FAILED";

            if (resultMessageText != null)
                resultMessageText.text = message;

            if (resultIcon != null)
                resultIcon.sprite = victory ? victoryIcon : defeatIcon;

            if (resultStatsText != null)
            {
                var gm = GameManager.Instance;
                resultStatsText.text = $"Suspect: {gm?.CurrentSuspect?.codename ?? "Unknown"}\n" +
                                      $"Customers Served: {gm?.CurrentCustomerIndex + 1 ?? 0}\n" +
                                      $"Final Reputation: {new string('★', gm?.Reputation ?? 0)}\n" +
                                      $"Correct ID: {(gm?.CaughtCorrectSuspect ?? false ? "Yes" : "No")}";
            }
        }
    }
}
