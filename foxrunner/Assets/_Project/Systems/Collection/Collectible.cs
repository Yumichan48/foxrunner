using UnityEngine;
using DG.Tweening;

namespace FoxRunner.Collection
{
    /// <summary>
    /// Individual collectible item component
    /// Handles visual effects, rarity, and collection state
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        #region Configuration
        [Header("=== COLLECTIBLE DATA ===")]
        [SerializeField] private CollectibleType type = CollectibleType.Coin;
        [SerializeField] private CollectibleRarity rarity = CollectibleRarity.Common;
        [SerializeField] private bool isSpecial = false;
        [SerializeField] private float despawnTime = 30f;

        [Header("=== VISUAL EFFECTS ===")]
        [SerializeField] private GameObject visualModel;
        [SerializeField] private ParticleSystem idleParticles;
        [SerializeField] private ParticleSystem collectionParticles;
        [SerializeField] private AudioClip collectionSound;

        [Header("=== ANIMATION ===")]
        [SerializeField] private bool enableIdleAnimation = true;
        [SerializeField] private float bobHeight = 0.1f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float rotationSpeed = 90f;

        [Header("=== DEBUG ===")]
        [SerializeField] private bool showDebugInfo = false;
        #endregion

        #region Private Fields
        private bool isCollected = false;
        private Vector3 initialPosition;
        private Collider collectibleCollider;
        private Tween bobTween;
        private Tween rotationTween;
        private float spawnTime;
        #endregion

        #region Properties
        public CollectibleType Type => type;
        public CollectibleRarity Rarity => rarity;
        public bool IsSpecial => isSpecial;
        public bool IsCollected => isCollected;
        public float Age => Time.time - spawnTime;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            collectibleCollider = GetComponent<Collider>();
            initialPosition = transform.position;
            spawnTime = Time.time;

            ValidateSetup();
        }

        void Start()
        {
            InitializeVisuals();
            StartIdleAnimations();
            StartDespawnTimer();
        }

        void OnDestroy()
        {
            CleanupTweens();
        }
        #endregion

        #region Initialization
        private void ValidateSetup()
        {
            if (!collectibleCollider)
            {
                Debug.LogError($"[Collectible] {name} missing Collider component!");
                return;
            }

            // Ensure collider is trigger
            collectibleCollider.isTrigger = true;

            // Set up layer if not already set
            if (gameObject.layer == 0)
            {
                gameObject.layer = LayerMask.NameToLayer("Collectibles");
                if (gameObject.layer == -1)
                {
                    Debug.LogWarning($"[Collectible] {name} could not find 'Collectibles' layer. Using default layer.");
                }
            }
        }

        private void InitializeVisuals()
        {
            // Apply rarity-based visual modifications
            ApplyRarityVisuals();

            // Start idle particles if available
            if (idleParticles)
            {
                idleParticles.Play();
            }

            // Apply special collectible effects
            if (isSpecial)
            {
                ApplySpecialEffects();
            }
        }

        private void ApplyRarityVisuals()
        {
            if (!visualModel) return;

            // Get or add a renderer component
            Renderer renderer = visualModel.GetComponent<Renderer>();
            if (!renderer) return;

            // Apply rarity-based color tint
            Color rarityColor = GetRarityColor();
            Material material = renderer.material;
            material.color = rarityColor;

            // Apply emission for higher rarities
            if (rarity >= CollectibleRarity.Rare)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", rarityColor * 0.5f);
            }
        }

        private Color GetRarityColor()
        {
            return rarity switch
            {
                CollectibleRarity.Common => Color.white,
                CollectibleRarity.Uncommon => Color.green,
                CollectibleRarity.Rare => Color.blue,
                CollectibleRarity.Epic => Color.magenta,
                CollectibleRarity.Legendary => Color.yellow,
                _ => Color.white
            };
        }

        private void ApplySpecialEffects()
        {
            // Special collectibles get enhanced visual effects
            if (visualModel)
            {
                // Add pulsing scale animation
                visualModel.transform.DOScale(1.2f, 0.5f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }

            // Enhanced particle effects
            if (idleParticles)
            {
                var main = idleParticles.main;
                main.startSize = main.startSize.constant * 1.5f;
                main.startSpeed = main.startSpeed.constant * 1.3f;
            }
        }
        #endregion

        #region Animation System
        private void StartIdleAnimations()
        {
            if (!enableIdleAnimation) return;

            // Gentle bobbing animation
            if (bobHeight > 0)
            {
                bobTween = transform.DOMoveY(initialPosition.y + bobHeight, 1f / bobSpeed)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }

            // Rotation animation
            if (rotationSpeed > 0)
            {
                rotationTween = transform.DORotate(Vector3.up * 360f, 360f / rotationSpeed, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Incremental)
                    .SetEase(Ease.Linear);
            }
        }

        private void CleanupTweens()
        {
            bobTween?.Kill();
            rotationTween?.Kill();
        }
        #endregion

        #region Collection System
        public void SetCollected(bool collected)
        {
            if (isCollected == collected) return;

            isCollected = collected;

            if (isCollected)
            {
                OnCollected();
            }
        }

        private void OnCollected()
        {
            // Disable collider to prevent multiple collections
            if (collectibleCollider)
            {
                collectibleCollider.enabled = false;
            }

            // Stop idle animations
            CleanupTweens();

            // Stop idle particles
            if (idleParticles)
            {
                idleParticles.Stop();
            }

            // Play collection particles
            if (collectionParticles)
            {
                collectionParticles.Play();
            }

            // Play collection sound
            if (collectionSound)
            {
                AudioSource.PlayClipAtPoint(collectionSound, transform.position);
            }

            // Collection animation will be handled by CollectionSystem
        }

        public void ForceCollect()
        {
            if (!isCollected)
            {
                var collectionSystem = FindObjectOfType<CollectionSystem>();
                if (collectionSystem)
                {
                    collectionSystem.CollectCollectible(this);
                }
            }
        }
        #endregion

        #region Despawn System
        private void StartDespawnTimer()
        {
            if (despawnTime > 0)
            {
                Invoke(nameof(DespawnCollectible), despawnTime);
            }
        }

        private void DespawnCollectible()
        {
            if (isCollected) return;

            Debug.Log($"[Collectible] {name} despawning after {despawnTime} seconds");

            // Fade out animation
            if (visualModel)
            {
                Renderer renderer = visualModel.GetComponent<Renderer>();
                if (renderer && renderer.material)
                {
                    Material material = renderer.material;
                    material.DOFade(0f, 1f).OnComplete(() =>
                    {
                        Destroy(gameObject);
                    });
                }
                else
                {
                    Destroy(gameObject, 1f);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region Utility Methods
        public void SetType(CollectibleType newType)
        {
            type = newType;
            // Could update visuals based on type here
        }

        public void SetRarity(CollectibleRarity newRarity)
        {
            rarity = newRarity;
            ApplyRarityVisuals();
        }

        public void SetSpecial(bool special)
        {
            isSpecial = special;
            if (special)
            {
                ApplySpecialEffects();
            }
        }

        public CollectibleData GetCollectibleData()
        {
            return new CollectibleData
            {
                type = type,
                rarity = rarity,
                isSpecial = isSpecial,
                age = Age,
                position = transform.position
            };
        }
        #endregion

        #region Debug & Gizmos
        void OnDrawGizmos()
        {
            if (!showDebugInfo) return;

            // Draw collection radius (if this collectible has magnetic properties)
            Gizmos.color = isCollected ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw rarity indicator
            Gizmos.color = GetRarityColor();
            Gizmos.DrawCube(transform.position + Vector3.up * 0.8f, Vector3.one * 0.1f);
        }

        void OnDrawGizmosSelected()
        {
            // Show despawn timer as a circle that shrinks over time
            if (Application.isPlaying && despawnTime > 0)
            {
                float timeRemaining = despawnTime - Age;
                float radius = (timeRemaining / despawnTime) * 2f;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, radius);
            }
        }
        #endregion
    }

    #region Supporting Data Structures
    [System.Serializable]
    public class CollectibleData
    {
        public CollectibleType type;
        public CollectibleRarity rarity;
        public bool isSpecial;
        public float age;
        public Vector3 position;
    }
    #endregion
}