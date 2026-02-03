using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UndercoverBarber.Core;

namespace UndercoverBarber.Chase
{
    public class StreetChaseController : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float laneWidth = 2f;

        [Header("Suspect Settings")]
        [SerializeField] private Transform suspectTransform;
        [SerializeField] private float suspectSpeed = 4f;

        [Header("Chase Settings")]
        [SerializeField] private float startingDistance = 100f;
        [SerializeField] private float catchDistance = 5f;
        [SerializeField] private float distanceDecayRate = 0.1f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenRate = 5f;
        [SerializeField] private float sprintStaminaCost = 20f;
        [SerializeField] private float sprintBoost = 15f;

        [Header("Obstacles")]
        [SerializeField] private GameObject[] obstaclePrefabs;
        [SerializeField] private Transform obstacleSpawnPoint;
        [SerializeField] private float obstacleSpawnInterval = 1.5f;
        [SerializeField] private float obstacleSpeed = 10f;
        [SerializeField] private float obstaclePenalty = 10f;
        [SerializeField] private float staminaPenalty = 15f;

        [Header("Lanes")]
        [SerializeField] private int currentLane = 1; // 0, 1, 2 (left, center, right)
        [SerializeField] private float laneSwitchSpeed = 10f;

        // Events
        public UnityEvent<float> OnDistanceChanged;
        public UnityEvent<float> OnStaminaChanged;
        public UnityEvent OnObstacleHit;
        public UnityEvent OnChaseWon;
        public UnityEvent OnChaseLost;

        private float currentDistance;
        private float currentStamina;
        private bool isChasing;
        private bool isJumping;
        private List<GameObject> activeObstacles = new List<GameObject>();
        private Coroutine obstacleSpawner;

        private void Start()
        {
            // Subscribe to game state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged.AddListener(OnGameStateChanged);
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.StreetChase)
            {
                StartChase();
            }
            else if (isChasing)
            {
                StopChase();
            }
        }

        public void StartChase()
        {
            currentDistance = startingDistance;
            currentStamina = maxStamina;
            currentLane = 1;
            isChasing = true;
            isJumping = false;

            // Clear any existing obstacles
            ClearObstacles();

            // Start spawning obstacles
            obstacleSpawner = StartCoroutine(SpawnObstacles());

            UpdateUI();
        }

        public void StopChase()
        {
            isChasing = false;

            if (obstacleSpawner != null)
                StopCoroutine(obstacleSpawner);

            ClearObstacles();
        }

        private void Update()
        {
            if (!isChasing) return;

            // Update distance (suspect tires over time)
            currentDistance -= distanceDecayRate * Time.deltaTime;

            // Regenerate stamina
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);

            // Handle input
            HandleInput();

            // Update player position (lane)
            UpdatePlayerPosition();

            // Check win/lose conditions
            CheckChaseStatus();

            UpdateUI();
        }

        private void HandleInput()
        {
            // Keyboard input
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                MoveLeft();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                MoveRight();
            }
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                Sprint();
            }

            // Touch/swipe input for mobile
            HandleTouchInput();
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    // Store touch start position
                }
                else if (touch.phase == TouchPhase.Ended)
                {
                    Vector2 swipe = touch.deltaPosition;

                    if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
                    {
                        // Horizontal swipe
                        if (swipe.x > 50) MoveRight();
                        else if (swipe.x < -50) MoveLeft();
                    }
                    else if (swipe.y > 50)
                    {
                        // Swipe up = jump
                        Jump();
                    }
                }
            }
        }

        public void MoveLeft()
        {
            currentLane = Mathf.Max(0, currentLane - 1);
        }

        public void MoveRight()
        {
            currentLane = Mathf.Min(2, currentLane + 1);
        }

        public void Jump()
        {
            if (!isJumping)
            {
                StartCoroutine(DoJump());
            }
        }

        private IEnumerator DoJump()
        {
            isJumping = true;

            Vector3 startPos = playerTransform.position;
            Vector3 jumpPeak = startPos + Vector3.up * 2f;

            // Jump up
            float jumpTime = 0.3f;
            float elapsed = 0f;

            while (elapsed < jumpTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / jumpTime;
                playerTransform.position = Vector3.Lerp(startPos, jumpPeak, t);
                yield return null;
            }

            // Fall down
            elapsed = 0f;
            while (elapsed < jumpTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / jumpTime;
                playerTransform.position = Vector3.Lerp(jumpPeak, startPos, t);
                yield return null;
            }

            playerTransform.position = startPos;
            isJumping = false;
        }

        public void Sprint()
        {
            if (currentStamina >= sprintStaminaCost)
            {
                currentStamina -= sprintStaminaCost;
                currentDistance -= sprintBoost;
                OnStaminaChanged?.Invoke(currentStamina / maxStamina);
            }
        }

        private void UpdatePlayerPosition()
        {
            if (playerTransform == null) return;

            float targetX = (currentLane - 1) * laneWidth;
            Vector3 targetPos = new Vector3(
                targetX,
                playerTransform.position.y,
                playerTransform.position.z
            );

            playerTransform.position = Vector3.Lerp(
                playerTransform.position,
                targetPos,
                laneSwitchSpeed * Time.deltaTime
            );
        }

        private IEnumerator SpawnObstacles()
        {
            while (isChasing)
            {
                SpawnObstacle();
                yield return new WaitForSeconds(obstacleSpawnInterval + Random.Range(-0.3f, 0.5f));
            }
        }

        private void SpawnObstacle()
        {
            if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            int lane = Random.Range(0, 3);

            Vector3 spawnPos = obstacleSpawnPoint != null
                ? obstacleSpawnPoint.position
                : new Vector3(0, 0, 20f);

            spawnPos.x = (lane - 1) * laneWidth;

            GameObject obstacle = Instantiate(prefab, spawnPos, Quaternion.identity);
            var mover = obstacle.AddComponent<ObstacleMove>();
            mover.Initialize(obstacleSpeed, lane, this);

            activeObstacles.Add(obstacle);
        }

        public void OnObstacleCollision(int obstacleLane, bool isJumpable)
        {
            // Check if player is in same lane and not jumping over jumpable obstacle
            if (obstacleLane == currentLane)
            {
                if (isJumpable && isJumping)
                {
                    // Successfully jumped over
                    return;
                }

                // Hit obstacle
                currentDistance += obstaclePenalty;
                currentStamina -= staminaPenalty;
                OnObstacleHit?.Invoke();
            }
        }

        public void RemoveObstacle(GameObject obstacle)
        {
            activeObstacles.Remove(obstacle);
            Destroy(obstacle);
        }

        private void ClearObstacles()
        {
            foreach (var obstacle in activeObstacles)
            {
                if (obstacle != null)
                    Destroy(obstacle);
            }
            activeObstacles.Clear();
        }

        private void CheckChaseStatus()
        {
            if (currentDistance <= catchDistance)
            {
                // Caught the suspect!
                StopChase();
                OnChaseWon?.Invoke();
                GameManager.Instance?.CompleteStreetChase(true);
            }
            else if (currentDistance >= startingDistance * 1.5f || currentStamina <= 0)
            {
                // Suspect escaped
                StopChase();
                OnChaseLost?.Invoke();
                GameManager.Instance?.CompleteStreetChase(false);
            }
        }

        private void UpdateUI()
        {
            OnDistanceChanged?.Invoke(currentDistance);
            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
        }

        // Public getters
        public float GetDistance() => currentDistance;
        public float GetStaminaPercent() => currentStamina / maxStamina;
        public bool IsJumping() => isJumping;
    }

    // Helper component for obstacle movement
    public class ObstacleMove : MonoBehaviour
    {
        private float speed;
        private int lane;
        private StreetChaseController controller;
        private bool hasTriggered;

        public void Initialize(float speed, int lane, StreetChaseController controller)
        {
            this.speed = speed;
            this.lane = lane;
            this.controller = controller;
        }

        private void Update()
        {
            transform.Translate(Vector3.back * speed * Time.deltaTime);

            // Check if passed player
            if (!hasTriggered && transform.position.z < 0)
            {
                hasTriggered = true;
                controller.OnObstacleCollision(lane, true);
            }

            // Remove when off screen
            if (transform.position.z < -10f)
            {
                controller.RemoveObstacle(gameObject);
            }
        }
    }
}
