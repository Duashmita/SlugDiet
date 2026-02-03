using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UndercoverBarber.Core;

namespace UndercoverBarber.Chase
{
    public class CarChaseController : MonoBehaviour
    {
        [Header("Player Car")]
        [SerializeField] private Transform playerCar;
        [SerializeField] private float baseSpeed = 20f;
        [SerializeField] private float nitroBoost = 30f;

        [Header("Suspect Car")]
        [SerializeField] private Transform suspectCar;
        [SerializeField] private float suspectSpeed = 18f;

        [Header("Chase Settings")]
        [SerializeField] private float startingDistance = 200f;
        [SerializeField] private float catchDistance = 10f;
        [SerializeField] private float distanceDecayRate = 0.15f;

        [Header("Car Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float collisionDamage = 25f;
        [SerializeField] private float nitroDamage = 5f;

        [Header("Traffic")]
        [SerializeField] private GameObject[] trafficPrefabs;
        [SerializeField] private Transform trafficSpawnPoint;
        [SerializeField] private float trafficSpawnInterval = 0.8f;
        [SerializeField] private float trafficSpeed = 15f;

        [Header("Lanes")]
        [SerializeField] private float laneWidth = 3f;
        [SerializeField] private int numberOfLanes = 3;
        [SerializeField] private float laneSwitchSpeed = 15f;

        [Header("Road Animation")]
        [SerializeField] private Material roadMaterial;
        [SerializeField] private float roadScrollSpeed = 2f;

        // Events
        public UnityEvent<float> OnDistanceChanged;
        public UnityEvent<float> OnHealthChanged;
        public UnityEvent OnTrafficHit;
        public UnityEvent OnNitroUsed;
        public UnityEvent OnChaseWon;
        public UnityEvent OnChaseLost;

        private float currentDistance;
        private float currentHealth;
        private int currentLane = 1;
        private bool isChasing;
        private bool nitroAvailable = true;
        private float nitroCooldlown = 3f;
        private List<GameObject> activeTraffic = new List<GameObject>();
        private Coroutine trafficSpawner;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged.AddListener(OnGameStateChanged);
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.CarChase)
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
            currentHealth = maxHealth;
            currentLane = 1;
            isChasing = true;
            nitroAvailable = true;

            ClearTraffic();
            trafficSpawner = StartCoroutine(SpawnTraffic());

            UpdateUI();
        }

        public void StopChase()
        {
            isChasing = false;

            if (trafficSpawner != null)
                StopCoroutine(trafficSpawner);

            ClearTraffic();
        }

        private void Update()
        {
            if (!isChasing) return;

            // Closing in on suspect
            currentDistance -= distanceDecayRate * Time.deltaTime;

            // Handle input
            HandleInput();

            // Update car position
            UpdateCarPosition();

            // Animate road
            AnimateRoad();

            // Check win/lose
            CheckChaseStatus();

            UpdateUI();
        }

        private void HandleInput()
        {
            // Keyboard
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                ChangeLane(currentLane - 1);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                ChangeLane(currentLane + 1);
            }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift))
            {
                UseNitro();
            }

            // Touch input
            HandleTouchInput();
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Ended)
                {
                    // Determine which third of screen was tapped
                    float screenThird = Screen.width / 3f;

                    if (touch.position.x < screenThird)
                    {
                        ChangeLane(currentLane - 1);
                    }
                    else if (touch.position.x > screenThird * 2)
                    {
                        ChangeLane(currentLane + 1);
                    }
                    else
                    {
                        // Center tap = nitro
                        UseNitro();
                    }
                }
            }
        }

        public void ChangeLane(int newLane)
        {
            currentLane = Mathf.Clamp(newLane, 0, numberOfLanes - 1);
        }

        public void UseNitro()
        {
            if (!nitroAvailable) return;

            nitroAvailable = false;
            currentDistance -= nitroBoost;
            currentHealth -= nitroDamage;

            OnNitroUsed?.Invoke();

            // Visual effect
            StartCoroutine(NitroEffect());

            // Cooldown
            StartCoroutine(NitroCooldown());
        }

        private IEnumerator NitroEffect()
        {
            // Speed boost visual
            float originalSpeed = roadScrollSpeed;
            roadScrollSpeed *= 2f;

            yield return new WaitForSeconds(0.5f);

            roadScrollSpeed = originalSpeed;
        }

        private IEnumerator NitroCooldown()
        {
            yield return new WaitForSeconds(nitroCooldlown);
            nitroAvailable = true;
        }

        private void UpdateCarPosition()
        {
            if (playerCar == null) return;

            float targetX = (currentLane - (numberOfLanes - 1) / 2f) * laneWidth;
            Vector3 targetPos = new Vector3(
                targetX,
                playerCar.position.y,
                playerCar.position.z
            );

            playerCar.position = Vector3.Lerp(
                playerCar.position,
                targetPos,
                laneSwitchSpeed * Time.deltaTime
            );
        }

        private void AnimateRoad()
        {
            if (roadMaterial != null)
            {
                Vector2 offset = roadMaterial.mainTextureOffset;
                offset.y += roadScrollSpeed * Time.deltaTime;
                roadMaterial.mainTextureOffset = offset;
            }
        }

        private IEnumerator SpawnTraffic()
        {
            while (isChasing)
            {
                SpawnTrafficCar();
                yield return new WaitForSeconds(trafficSpawnInterval + Random.Range(-0.2f, 0.3f));
            }
        }

        private void SpawnTrafficCar()
        {
            if (trafficPrefabs == null || trafficPrefabs.Length == 0) return;

            GameObject prefab = trafficPrefabs[Random.Range(0, trafficPrefabs.Length)];
            int lane = Random.Range(0, numberOfLanes);

            Vector3 spawnPos = trafficSpawnPoint != null
                ? trafficSpawnPoint.position
                : new Vector3(0, 0, 50f);

            spawnPos.x = (lane - (numberOfLanes - 1) / 2f) * laneWidth;

            GameObject traffic = Instantiate(prefab, spawnPos, Quaternion.identity);
            var mover = traffic.AddComponent<TrafficMove>();
            mover.Initialize(trafficSpeed, lane, this);

            activeTraffic.Add(traffic);
        }

        public void OnTrafficCollision(int trafficLane)
        {
            if (trafficLane == currentLane)
            {
                // Collision!
                currentHealth -= collisionDamage;
                currentDistance += 10f; // Fall behind

                OnTrafficHit?.Invoke();

                // Screen shake or visual feedback
                StartCoroutine(CollisionEffect());
            }
        }

        private IEnumerator CollisionEffect()
        {
            if (playerCar != null)
            {
                Vector3 originalPos = playerCar.position;

                for (int i = 0; i < 5; i++)
                {
                    playerCar.position = originalPos + Random.insideUnitSphere * 0.3f;
                    yield return new WaitForSeconds(0.05f);
                }

                playerCar.position = originalPos;
            }
        }

        public void RemoveTraffic(GameObject traffic)
        {
            activeTraffic.Remove(traffic);
            Destroy(traffic);
        }

        private void ClearTraffic()
        {
            foreach (var traffic in activeTraffic)
            {
                if (traffic != null)
                    Destroy(traffic);
            }
            activeTraffic.Clear();
        }

        private void CheckChaseStatus()
        {
            if (currentDistance <= catchDistance)
            {
                // Caught!
                StopChase();
                OnChaseWon?.Invoke();
                GameManager.Instance?.CompleteCarChase(true);
            }
            else if (currentHealth <= 0)
            {
                // Car destroyed
                StopChase();
                OnChaseLost?.Invoke();
                GameManager.Instance?.CompleteCarChase(false);
            }
            else if (currentDistance >= startingDistance * 1.5f)
            {
                // Too far behind
                StopChase();
                OnChaseLost?.Invoke();
                GameManager.Instance?.CompleteCarChase(false);
            }
        }

        private void UpdateUI()
        {
            OnDistanceChanged?.Invoke(currentDistance);
            OnHealthChanged?.Invoke(currentHealth / maxHealth);
        }

        // Public getters
        public float GetDistance() => currentDistance;
        public float GetHealthPercent() => currentHealth / maxHealth;
        public bool IsNitroAvailable() => nitroAvailable;
    }

    // Traffic movement component
    public class TrafficMove : MonoBehaviour
    {
        private float speed;
        private int lane;
        private CarChaseController controller;
        private bool hasTriggered;

        public void Initialize(float speed, int lane, CarChaseController controller)
        {
            this.speed = speed;
            this.lane = lane;
            this.controller = controller;
        }

        private void Update()
        {
            transform.Translate(Vector3.back * speed * Time.deltaTime);

            // Check collision zone
            if (!hasTriggered && transform.position.z < 5f && transform.position.z > -5f)
            {
                hasTriggered = true;
                controller.OnTrafficCollision(lane);
            }

            // Remove when off screen
            if (transform.position.z < -20f)
            {
                controller.RemoveTraffic(gameObject);
            }
        }
    }
}
