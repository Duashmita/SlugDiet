using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace UndercoverBarber.Core
{
    public enum GameState
    {
        Title,
        Briefing,
        Barbershop,
        StreetChase,
        CarChase,
        Result
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.Title;

        [Header("Game Data")]
        [SerializeField] private SuspectDatabase suspectDatabase;
        [SerializeField] private CustomerDatabase customerDatabase;

        [Header("Settings")]
        [SerializeField] private int maxCustomers = 5;
        [SerializeField] private int startingReputation = 3;

        // Current game session data
        public SuspectProfile CurrentSuspect { get; private set; }
        public Customer[] SessionCustomers { get; private set; }
        public int TrueSuspectIndex { get; private set; }
        public int CurrentCustomerIndex { get; private set; } = -1;
        public int Reputation { get; private set; }
        public bool CaughtCorrectSuspect { get; private set; }

        // Events
        public UnityEvent<GameState> OnStateChanged;
        public UnityEvent<int> OnReputationChanged;
        public UnityEvent<Customer> OnCustomerChanged;
        public UnityEvent<bool, string> OnGameEnded;

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

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            Reputation = startingReputation;
            CurrentCustomerIndex = -1;
            CaughtCorrectSuspect = false;
            ChangeState(GameState.Title);
        }

        public void ChangeState(GameState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.Briefing:
                    SetupMission();
                    break;
                case GameState.Barbershop:
                    StartCoroutine(StartBarbershopSequence());
                    break;
            }
        }

        private void SetupMission()
        {
            // Select random suspect profile
            CurrentSuspect = suspectDatabase.GetRandomSuspect();

            // Generate customer lineup
            SessionCustomers = customerDatabase.GenerateCustomerLineup(maxCustomers);

            // Randomly assign true suspect
            TrueSuspectIndex = Random.Range(0, SessionCustomers.Length);
            SessionCustomers[TrueSuspectIndex].SetAsSuspect(CurrentSuspect);
        }

        private IEnumerator StartBarbershopSequence()
        {
            yield return new WaitForSeconds(1.5f);
            NextCustomer();
        }

        public void NextCustomer()
        {
            CurrentCustomerIndex++;

            if (CurrentCustomerIndex >= SessionCustomers.Length)
            {
                // All customers served without identifying suspect
                EndGame(false, "The suspect slipped away. Mission failed.");
                return;
            }

            OnCustomerChanged?.Invoke(SessionCustomers[CurrentCustomerIndex]);
        }

        public void IdentifySuspect()
        {
            if (CurrentCustomerIndex < 0 || CurrentCustomerIndex >= SessionCustomers.Length)
                return;

            Customer current = SessionCustomers[CurrentCustomerIndex];
            CaughtCorrectSuspect = current.IsSuspect;

            if (CaughtCorrectSuspect)
            {
                // Correct! Start chase
                ChangeState(GameState.StreetChase);
            }
            else
            {
                // Wrong suspect
                ModifyReputation(-1);

                if (Reputation <= 0)
                {
                    EndGame(false, "Your cover is blown! Too many false accusations.");
                }
                else
                {
                    // Continue with next customer
                    NextCustomer();
                }
            }
        }

        public void ModifyReputation(int amount)
        {
            Reputation = Mathf.Clamp(Reputation + amount, 0, 5);
            OnReputationChanged?.Invoke(Reputation);
        }

        public void CompleteStreetChase(bool success)
        {
            if (success)
            {
                ChangeState(GameState.CarChase);
            }
            else
            {
                EndGame(false, "The suspect escaped on foot!");
            }
        }

        public void CompleteCarChase(bool success)
        {
            if (success)
            {
                EndGame(true, "Suspect apprehended! Excellent detective work!");
            }
            else
            {
                EndGame(false, "Your vehicle was too damaged. The suspect escaped!");
            }
        }

        public void EndGame(bool victory, string message)
        {
            ChangeState(GameState.Result);
            OnGameEnded?.Invoke(victory, message);
        }

        public void RestartGame()
        {
            InitializeGame();
        }

        public GameState GetCurrentState()
        {
            return currentState;
        }
    }
}
