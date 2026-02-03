using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UndercoverBarber.Core;
using UndercoverBarber.Data;

namespace UndercoverBarber.Gameplay
{
    public class HaircutController : MonoBehaviour
    {
        public static HaircutController Instance { get; private set; }

        [Header("Tools")]
        [SerializeField] private HaircutTool currentTool = HaircutTool.Scissors;

        [Header("Hair Settings")]
        [SerializeField] private GameObject hairParticlePrefab;
        [SerializeField] private Transform hairContainer;
        [SerializeField] private int hairCount = 200;
        [SerializeField] private float cutRadius = 0.5f;

        [Header("Progress")]
        [SerializeField] private float progressPerCut = 0.5f;

        // Events
        public UnityEvent<float> OnProgressChanged;
        public UnityEvent OnHaircutComplete;
        public UnityEvent<HaircutTool> OnToolChanged;

        private List<HairParticle> hairParticles = new List<HairParticle>();
        private float currentProgress;
        private bool isComplete;
        private bool isActive;
        private Camera mainCamera;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            mainCamera = Camera.main;
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
            ResetHaircut();
            GenerateHair();
            isActive = true;
        }

        private void GenerateHair()
        {
            ClearHair();

            for (int i = 0; i < hairCount; i++)
            {
                // Generate hair in a semi-circle pattern (top of head)
                float angle = Random.Range(0f, Mathf.PI);
                float distance = Random.Range(0.8f, 1.2f);

                Vector3 localPos = new Vector3(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance + 0.3f,
                    Random.Range(-0.1f, 0.1f)
                );

                var particle = new HairParticle
                {
                    position = localPos,
                    isCut = false,
                    length = Random.Range(0.3f, 0.6f)
                };

                hairParticles.Add(particle);

                // Create visual representation if prefab exists
                if (hairParticlePrefab != null && hairContainer != null)
                {
                    var visual = Instantiate(hairParticlePrefab, hairContainer);
                    visual.transform.localPosition = localPos;
                    visual.transform.localScale = Vector3.one * particle.length;
                    particle.visualObject = visual;
                }
            }
        }

        private void ClearHair()
        {
            foreach (var particle in hairParticles)
            {
                if (particle.visualObject != null)
                    Destroy(particle.visualObject);
            }
            hairParticles.Clear();
        }

        private void ResetHaircut()
        {
            currentProgress = 0f;
            isComplete = false;
            OnProgressChanged?.Invoke(0f);
        }

        private void Update()
        {
            if (!isActive || isComplete) return;

            // Handle input
            if (Input.GetMouseButton(0))
            {
                HandleCutInput(Input.mousePosition);
            }

            // Touch input for mobile/AR
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    HandleCutInput(touch.position);
                }
            }
        }

        private void HandleCutInput(Vector3 screenPosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                CutHairAt(hit.point);
            }
            else
            {
                // For 2D or if no collider hit, convert screen to world
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(
                    new Vector3(screenPosition.x, screenPosition.y, 5f)
                );
                CutHairAt(worldPos);
            }
        }

        public void CutHairAt(Vector3 worldPosition)
        {
            if (isComplete) return;

            int cutCount = 0;
            float toolRadius = GetToolRadius();

            foreach (var particle in hairParticles)
            {
                if (particle.isCut) continue;

                Vector3 particleWorld = hairContainer != null
                    ? hairContainer.TransformPoint(particle.position)
                    : particle.position;

                float distance = Vector3.Distance(worldPosition, particleWorld);

                if (distance <= toolRadius)
                {
                    CutParticle(particle);
                    cutCount++;
                }
            }

            if (cutCount > 0)
            {
                UpdateProgress(cutCount);
                PlayCutEffect(worldPosition);
            }
        }

        private void CutParticle(HairParticle particle)
        {
            particle.isCut = true;

            if (particle.visualObject != null)
            {
                // Animate hair falling
                var rb = particle.visualObject.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = particle.visualObject.AddComponent<Rigidbody>();

                rb.useGravity = true;
                rb.AddForce(Vector3.down * 2f + Random.insideUnitSphere, ForceMode.Impulse);

                // Destroy after delay
                Destroy(particle.visualObject, 2f);
            }
        }

        private void UpdateProgress(int cutCount)
        {
            float progressIncrease = cutCount * progressPerCut;
            currentProgress = Mathf.Clamp(currentProgress + progressIncrease, 0f, 100f);

            // Calculate actual progress based on cut hair
            int totalCut = 0;
            foreach (var p in hairParticles)
                if (p.isCut) totalCut++;

            float actualProgress = (float)totalCut / hairParticles.Count * 100f;
            currentProgress = Mathf.Max(currentProgress, actualProgress);

            OnProgressChanged?.Invoke(currentProgress);

            if (currentProgress >= 100f && !isComplete)
            {
                CompleteHaircut();
            }
        }

        private void CompleteHaircut()
        {
            isComplete = true;
            OnHaircutComplete?.Invoke();

            // Notify dialogue manager
            DialogueManager.Instance?.AddSystemMessage("Looking good! Thanks for the cut.");
        }

        private void PlayCutEffect(Vector3 position)
        {
            // Play sound based on tool
            switch (currentTool)
            {
                case HaircutTool.Scissors:
                    // Play snip sound
                    break;
                case HaircutTool.Clippers:
                    // Play buzz sound
                    break;
                case HaircutTool.Razor:
                    // Play razor sound
                    break;
            }

            // Spawn particle effect
            // ParticleManager.Instance?.PlayEffect("HairCut", position);
        }

        private float GetToolRadius()
        {
            return currentTool switch
            {
                HaircutTool.Scissors => cutRadius * 0.8f,
                HaircutTool.Clippers => cutRadius * 1.5f,
                HaircutTool.Razor => cutRadius * 1.0f,
                _ => cutRadius
            };
        }

        public void SetTool(HaircutTool tool)
        {
            currentTool = tool;
            OnToolChanged?.Invoke(tool);
        }

        public void SetTool(int toolIndex)
        {
            SetTool((HaircutTool)toolIndex);
        }

        public float GetProgress() => currentProgress;
        public bool IsComplete() => isComplete;
        public HaircutTool GetCurrentTool() => currentTool;
    }

    public enum HaircutTool
    {
        Scissors,
        Clippers,
        Razor
    }

    [System.Serializable]
    public class HairParticle
    {
        public Vector3 position;
        public bool isCut;
        public float length;
        public GameObject visualObject;
    }
}
