using System;
using UnityEngine;

namespace TheLastSasak.Items.Loot
{
    public class LootPickup : MonoBehaviour, ILootInteractable
    {
        [Header("Default Scene Configuration (untuk pickup yang ditaruh manual di Scene)")]
        [SerializeField] private LootDefinitionSO defaultLootDefinition;
        [Min(0)]
        [SerializeField] private int overrideAmount = 0;

        [Header("Trigger Settings")]
        [SerializeField] private string playerTag = "Player";

        private LootDefinitionSO _activeDefinition;
        private int _remainingAmount;
        private bool _isCollected;
        private LootDropController _poolOwner;
        private LootPickup _sourcePrefab;

        public event Action<LootPickup> OnCollected;
        public event Action<LootPickup, int, int> OnPartiallyCollected;
        public event Action<LootPickup> OnCollectionRejected;

        public LootDefinitionSO Definition => _activeDefinition;
        public int RemainingAmount => _remainingAmount;
        public bool IsCollected => _isCollected;
        internal LootPickup SourcePrefab => _sourcePrefab;

        private void Awake()
        {
            // Inisialisasi otomatis untuk pickup yang dipasang langsung di scene (seperti CombatTest)
            if (_activeDefinition == null && defaultLootDefinition != null)
            {
                int initialAmount = overrideAmount > 0 ? overrideAmount : defaultLootDefinition.Amount;
                Initialize(defaultLootDefinition, initialAmount, null, null);
            }
        }

        public void Initialize(
            LootDefinitionSO definition,
            int amount,
            LootDropController poolOwner,
            LootPickup sourcePrefab)
        {
            ResetPickupState();

            _activeDefinition = definition;
            _remainingAmount = definition != null
                ? Mathf.Max(1, amount > 0 ? amount : definition.Amount)
                : 0;
            _poolOwner = poolOwner;
            _sourcePrefab = sourcePrefab;
        }

        /// <summary>
        /// Membersihkan seluruh state internal sebelum dipakai ulang oleh Object Pool (L06).
        /// </summary>
        public void ResetPickupState()
        {
            _activeDefinition = null;
            _remainingAmount = 0;
            _isCollected = false;
            _poolOwner = null;
            _sourcePrefab = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected || _activeDefinition == null)
            {
                return;
            }

            if (_activeDefinition.PickupCondition != LootPickupCondition.AutoOnTrigger)
            {
                return;
            }

            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
            {
                return;
            }

            LootCollector collector = other.GetComponentInParent<LootCollector>();
            if (collector != null)
            {
                TryCollectBy(collector);
            }
        }

        // Implementasi ILootInteractable untuk PlayerInteract
        public string GetInteractPrompt()
        {
            return _activeDefinition != null
                ? $"Ambil {_activeDefinition.GetDisplayLabel()}"
                : "Ambil Item";
        }

        public bool CanInteract(GameObject interactor)
        {
            if (_isCollected || _activeDefinition == null || _remainingAmount <= 0 || interactor == null)
            {
                return false;
            }

            return interactor.GetComponentInParent<LootCollector>() != null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            LootCollector collector = interactor.GetComponentInParent<LootCollector>();
            if (collector != null)
            {
                TryCollectBy(collector);
            }
        }

        public bool TryCollectBy(LootCollector collector)
        {
            if (_isCollected || _activeDefinition == null || _remainingAmount <= 0 || collector == null)
            {
                return false;
            }

            bool anyConsumed = collector.TryCollectLoot(_activeDefinition, _remainingAmount, out int consumedAmount);

            if (!anyConsumed || consumedAmount <= 0)
            {
                // Inventory penuh atau transaksi ditolak: item tetap di dunia agar tidak hilang (L02)
                OnCollectionRejected?.Invoke(this);
                return false;
            }

            _remainingAmount = Mathf.Max(0, _remainingAmount - consumedAmount);

            if (_remainingAmount == 0)
            {
                _isCollected = true;
                OnCollected?.Invoke(this);
                DespawnOrReturnToPool();
            }
            else
            {
                // Sebagian stack berhasil masuk, sisanya tetap tinggal di pickup ini
                OnPartiallyCollected?.Invoke(this, consumedAmount, _remainingAmount);
            }

            return true;
        }

        private void DespawnOrReturnToPool()
        {
            if (_poolOwner != null)
            {
                _poolOwner.ReturnPickupToPool(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}