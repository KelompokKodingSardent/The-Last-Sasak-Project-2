Implementasi **Phase 6 — Loot & Reward Integration** menghubungkan perolehan item, *Souls*, dan *EXP* dari dunia permainan ke sistem masing-masing tanpa mencampurkan tanggung jawab `Inventory`, menjaga agar item tidak hilang saat *Inventory* penuh, dan mendukung siklus *object pooling* yang bersih.

### Struktur Target Phase 6

```text
Assets/_Project/Scripts/Items/Loot/
├── LootRewardContracts.cs
├── LootDefinitionSO.cs
├── LootPickup.cs
├── LootCollector.cs
├── LootDropController.cs
└── RewardCollector.cs

```

---

### 1. Kontrak Reward & Interaksi (`LootRewardContracts.cs`)

Menghubungkan sistem loot ke `CurrencyWallet`, `ExperienceSystem`, dan `PlayerInteract` milik proyek tanpa membuat ketergantungan sirkular.

```csharp
using UnityEngine;

namespace TheLastSasak.Items.Loot
{
    public enum LootRewardType
    {
        Item = 0,
        Currency = 1,
        Experience = 2
    }

    public enum LootPickupCondition
    {
        AutoOnTrigger = 0,
        ManualInteract = 1
    }

    /// <summary>
    /// Implementasikan pada CurrencyWallet milik proyek.
    /// </summary>
    public interface ICurrencyReceiver
    {
        void AddCurrency(int amount);
    }

    /// <summary>
    /// Implementasikan pada ExperienceSystem milik proyek.
    /// </summary>
    public interface IExperienceReceiver
    {
        void AddExperience(int amount);
    }

    /// <summary>
    /// Implementasikan pada komponen interaksi dunia agar PlayerInteract dapat memanggilnya.
    /// </summary>
    public interface ILootInteractable
    {
        string GetInteractPrompt();
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}

```

---

### 2. Konfigurasi Data Loot (`LootDefinitionSO.cs`)

Mendefinisikan identitas loot, jenis reward (`Item`, `Currency`, atau `Experience`), jumlah, visual *pickup*, dan kondisi pengambilan (**Point 9.1** & **Point 6.2**).

```csharp
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Loot
{
    [CreateAssetMenu(fileName = "Loot_", menuName = "The Last Sasak/Loot/Loot Definition")]
    public class LootDefinitionSO : ScriptableObject
    {
        [Header("Persistent Identity")]
        [Tooltip("ID unik loot (contoh: loot.klewang, loot.souls_small).")]
        [SerializeField] private string lootId;

        [Header("Reward Configuration")]
        [SerializeField] private LootRewardType rewardType = LootRewardType.Item;
        [Tooltip("Wajib diisi jika RewardType bernilai Item.")]
        [SerializeField] private ItemDefinitionSO itemDefinition;
        [Min(1)]
        [SerializeField] private int amount = 1;

        [Header("World Presentation & Pickup Rule")]
        [SerializeField] private LootPickup pickupPrefab;
        [SerializeField] private LootPickupCondition pickupCondition = LootPickupCondition.AutoOnTrigger;

        public string LootId => lootId;
        public LootRewardType RewardType => rewardType;
        public ItemDefinitionSO ItemDefinition => itemDefinition;
        public int Amount => amount;
        public LootPickup PickupPrefab => pickupPrefab;
        public LootPickupCondition PickupCondition => pickupCondition;

        public string GetDisplayLabel()
        {
            return rewardType switch
            {
                LootRewardType.Item => itemDefinition != null ? itemDefinition.DisplayName : "Unknown Item",
                LootRewardType.Currency => $"{amount} Souls",
                LootRewardType.Experience => $"{amount} EXP",
                _ => name
            };
        }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(lootId))
            {
                lootId = lootId.Trim().ToLowerInvariant();
            }

            if (amount < 1)
            {
                amount = 1;
            }

            if (string.IsNullOrWhiteSpace(lootId))
            {
                Debug.LogWarning($"[LootDefinitionSO] '{name}' belum memiliki LootId.", this);
            }

            if (rewardType == LootRewardType.Item && itemDefinition == null)
            {
                Debug.LogError($"[LootDefinitionSO] '{name}' bertipe Item tetapi ItemDefinition masih kosong.", this);
            }
            else if (rewardType != LootRewardType.Item && itemDefinition != null)
            {
                Debug.LogWarning($"[LootDefinitionSO] '{name}' bertipe {rewardType}; ItemDefinition tidak akan digunakan.", this);
            }
        }
    }
}

```

---

### 3. World Pickup & Pooling Lifecycle (`LootPickup.cs`)

Menangani pengambilan objek loot di dunia permainan dengan empat jaminan utama (**Point 9.3** & **Test L01–L06**):

1. **Anti Double-Reward (`L05`):** Flag `_isCollected` mencegah transaksi ganda pada frame yang sama.
2. **Inventory Penuh Aman (`L02`):** Jika `Inventory` penuh, pickup tetap berada di dunia dan tidak hilang.
3. **Partial Stack Preservation:** Jika tumpukan hanya muat sebagian (misalnya 2 dari 5), `_remainingAmount` berkurang menjadi 3 dan objek tetap menunggu di dunia.
4. **Reset State Pooling (`L06`):** `ResetPickupState()` membersihkan sisa status lama setiap kali pickup dipakai ulang dari pool.

```csharp
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

```

---

### 4. Player Loot Collector (`LootCollector.cs`)

Dipasang pada root `Player`. Memeriksa `RewardType` dan menyalurkan setiap hadiah ke sistem pemiliknya tanpa pernah memasukkan *Souls* atau *EXP* ke `Inventory`.

```csharp
using System;
using UnityEngine;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Inventory;

namespace TheLastSasak.Items.Loot
{
    public class LootCollector : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Inventory.Inventory inventory;
        [Tooltip("Komponen CurrencyWallet yang mengimplementasikan ICurrencyReceiver.")]
        [SerializeField] private MonoBehaviour currencyWalletComponent;
        [Tooltip("Komponen ExperienceSystem yang mengimplementasikan IExperienceReceiver.")]
        [SerializeField] private MonoBehaviour experienceSystemComponent;

        private ICurrencyReceiver _currencyReceiver;
        private IExperienceReceiver _experienceReceiver;

        public event Action<LootDefinitionSO, int> OnLootCollected;
        public event Action<LootDefinitionSO> OnLootCollectionFailed;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<Inventory.Inventory>();
            }

            _currencyReceiver = currencyWalletComponent != null
                ? currencyWalletComponent as ICurrencyReceiver
                : GetComponent<ICurrencyReceiver>();

            _experienceReceiver = experienceSystemComponent != null
                ? experienceSystemComponent as IExperienceReceiver
                : GetComponent<IExperienceReceiver>();
        }

        public void SetCurrencyReceiver(ICurrencyReceiver receiver)
        {
            _currencyReceiver = receiver;
        }

        public void SetExperienceReceiver(IExperienceReceiver receiver)
        {
            _experienceReceiver = receiver;
        }

        public bool TryCollectLoot(LootDefinitionSO definition, int requestedAmount, out int consumedAmount)
        {
            consumedAmount = 0;

            if (definition == null || requestedAmount <= 0)
            {
                return false;
            }

            switch (definition.RewardType)
            {
                case LootRewardType.Item:
                    return CollectItemReward(definition, requestedAmount, out consumedAmount);

                case LootRewardType.Currency:
                    return CollectCurrencyReward(definition, requestedAmount, out consumedAmount);

                case LootRewardType.Experience:
                    return CollectExperienceReward(definition, requestedAmount, out consumedAmount);

                default:
                    return false;
            }
        }

        private bool CollectItemReward(LootDefinitionSO definition, int requestedAmount, out int consumedAmount)
        {
            consumedAmount = 0;
            ItemDefinitionSO itemDef = definition.ItemDefinition;

            if (inventory == null || itemDef == null)
            {
                OnLootCollectionFailed?.Invoke(definition);
                return false;
            }

            bool addedAny = inventory.TryAddItem(itemDef, requestedAmount, out consumedAmount);
            if (!addedAny || consumedAmount <= 0)
            {
                OnLootCollectionFailed?.Invoke(definition);
                return false;
            }

            OnLootCollected?.Invoke(definition, consumedAmount);
            return true;
        }

        private bool CollectCurrencyReward(LootDefinitionSO definition, int requestedAmount, out int consumedAmount)
        {
            consumedAmount = 0;

            if (_currencyReceiver == null)
            {
                Debug.LogWarning("[LootCollector] ICurrencyReceiver (CurrencyWallet) belum terhubung.", this);
                OnLootCollectionFailed?.Invoke(definition);
                return false;
            }

            _currencyReceiver.AddCurrency(requestedAmount);
            consumedAmount = requestedAmount;
            OnLootCollected?.Invoke(definition, consumedAmount);
            return true;
        }

        private bool CollectExperienceReward(LootDefinitionSO definition, int requestedAmount, out int consumedAmount)
        {
            consumedAmount = 0;

            if (_experienceReceiver == null)
            {
                Debug.LogWarning("[LootCollector] IExperienceReceiver (ExperienceSystem) belum terhubung.", this);
                OnLootCollectionFailed?.Invoke(definition);
                return false;
            }

            _experienceReceiver.AddExperience(requestedAmount);
            consumedAmount = requestedAmount;
            OnLootCollected?.Invoke(definition, consumedAmount);
            return true;
        }
    }
}

```

---

### 5. Loot Drop Controller & Object Pool (`LootDropController.cs`)

Mengelola *spawn* pickup dari tabel drop musuh, peti, atau *encounter*, serta mendaur ulang instance `LootPickup` melalui pool internal per prefab (**Point 9.2** & **Point 6.5**).

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheLastSasak.Items.Loot
{
    [Serializable]
    public struct LootDropEntry
    {
        public LootDefinitionSO lootDefinition;
        [Range(0f, 1f)]
        public float dropChance;
        [Min(1)]
        public int minAmount;
        [Min(1)]
        public int maxAmount;
    }

    public class LootDropController : MonoBehaviour
    {
        [Header("Drop Spawn Settings")]
        [SerializeField] private float horizontalScatterRadius = 0.75f;
        [SerializeField] private float verticalSpawnOffset = 0.25f;
        [SerializeField] private Transform pooledContainer;

        private readonly Dictionary<LootPickup, Queue<LootPickup>> _pools = new Dictionary<LootPickup, Queue<LootPickup>>();

        public event Action<LootPickup> OnLootSpawned;

        private void Awake()
        {
            if (pooledContainer == null)
            {
                GameObject containerObj = new GameObject("PooledLootPickups");
                containerObj.transform.SetParent(transform);
                pooledContainer = containerObj.transform;
            }
        }

        public void RollAndSpawnDrops(IReadOnlyList<LootDropEntry> dropTable, Vector3 originPosition)
        {
            if (dropTable == null || dropTable.Count == 0)
            {
                return;
            }

            for (int i = 0; i < dropTable.Count; i++)
            {
                LootDropEntry entry = dropTable[i];
                if (entry.lootDefinition == null || entry.dropChance <= 0f)
                {
                    continue;
                }

                if (UnityEngine.Random.value <= entry.dropChance)
                {
                    int min = Mathf.Max(1, entry.minAmount);
                    int max = Mathf.Max(min, entry.maxAmount);
                    int rolledAmount = UnityEngine.Random.Range(min, max + 1);

                    Vector3 spawnPos = ComputeScatteredPosition(originPosition);
                    SpawnLoot(entry.lootDefinition, spawnPos, rolledAmount);
                }
            }
        }

        public LootPickup SpawnLoot(LootDefinitionSO definition, Vector3 worldPosition, int overrideAmount = -1)
        {
            if (definition == null)
            {
                return null;
            }

            if (definition.PickupPrefab == null)
            {
                Debug.LogError($"[LootDropController] '{definition.LootId}' tidak memiliki PickupPrefab.", definition);
                return null;
            }

            LootPickup instance = GetOrCreateFromPool(definition.PickupPrefab);
            instance.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);

            int finalAmount = overrideAmount > 0 ? overrideAmount : definition.Amount;
            instance.Initialize(definition, finalAmount, this, definition.PickupPrefab);
            instance.gameObject.SetActive(true);

            OnLootSpawned?.Invoke(instance);
            return instance;
        }

        public void ReturnPickupToPool(LootPickup pickup)
        {
            if (pickup == null)
            {
                return;
            }

            LootPickup prefabKey = pickup.SourcePrefab;
            pickup.ResetPickupState();
            pickup.gameObject.SetActive(false);

            if (pooledContainer != null)
            {
                pickup.transform.SetParent(pooledContainer);
            }

            if (prefabKey == null)
            {
                return;
            }

            if (!_pools.TryGetValue(prefabKey, out Queue<LootPickup> queue))
            {
                queue = new Queue<LootPickup>();
                _pools[prefabKey] = queue;
            }

            queue.Enqueue(pickup);
        }

        private LootPickup GetOrCreateFromPool(LootPickup prefab)
        {
            if (_pools.TryGetValue(prefab, out Queue<LootPickup> queue))
            {
                while (queue.Count > 0)
                {
                    LootPickup pooled = queue.Dequeue();
                    if (pooled != null)
                    {
                        return pooled;
                    }
                }
            }

            LootPickup created = Instantiate(prefab, pooledContainer);
            return created;
        }

        private Vector3 ComputeScatteredPosition(Vector3 origin)
        {
            Vector2 circle = UnityEngine.Random.insideUnitCircle * horizontalScatterRadius;
            return origin + new Vector3(circle.x, verticalSpawnOffset, circle.y);
        }
    }
}

```

---

### 6. Enemy Death Reward Coordinator (`RewardCollector.cs`)

Dipasang pada musuh atau peti. Memisahkan distribusi *EXP* dan *Souls* langsung saat musuh mati dari *item drop* fisik di dunia, serta mencegah pemberian hadiah dua kali ketika event kematian terpicu ulang (**Point 6.4** & **Test L05**).

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace TheLastSasak.Items.Loot
{
    public class RewardCollector : MonoBehaviour
    {
        [Header("Direct Kill Rewards (Non-Inventory)")]
        [Min(0)]
        [SerializeField] private int directSoulsReward = 25;
        [Min(0)]
        [SerializeField] private int directExperienceReward = 50;

        [Header("World Loot Drops")]
        [SerializeField] private List<LootDropEntry> lootDrops = new List<LootDropEntry>();
        [SerializeField] private LootDropController lootDropController;

        private bool _rewardsDistributed;

        private void Awake()
        {
            if (lootDropController == null)
            {
                lootDropController = FindFirstObjectByType<LootDropController>();
            }
        }

        /// <summary>
        /// Panggil saat musuh kembali di-spawn dari Enemy Pool agar reward siap dipakai lagi.
        /// </summary>
        public void ResetRewardState()
        {
            _rewardsDistributed = false;
        }

        /// <summary>
        /// Hubungkan ke event OnDeath milik EnemyHealth / EnemyController.
        /// </summary>
        public void DistributeRewards(GameObject killerObject)
        {
            // Jaminan anti double-reward apabila OnDeath terpicu lebih dari sekali
            if (_rewardsDistributed)
            {
                return;
            }

            _rewardsDistributed = true;

            if (killerObject != null)
            {
                if (directSoulsReward > 0)
                {
                    ICurrencyReceiver currencyReceiver = killerObject.GetComponentInParent<ICurrencyReceiver>();
                    currencyReceiver?.AddCurrency(directSoulsReward);
                }

                if (directExperienceReward > 0)
                {
                    IExperienceReceiver expReceiver = killerObject.GetComponentInParent<IExperienceReceiver>();
                    expReceiver?.AddExperience(directExperienceReward);
                }
            }

            if (lootDropController != null && lootDrops != null && lootDrops.Count > 0)
            {
                lootDropController.RollAndSpawnDrops(lootDrops, transform.position);
            }
        }
    }
}

```

---

### 7. Panduan Wiring & Validasi Phase 6 (L01–L06)

1. **Adapter pada Sistem Lama:**
* Tambahkan `: ICurrencyReceiver` pada `CurrencyWallet` dan arahkan `AddCurrency(int amount)` ke metode penambahan *Souls* yang sudah ada.
* Tambahkan `: IExperienceReceiver` pada `ExperienceSystem` dan arahkan `AddExperience(int amount)` ke metode penambahan *EXP* yang sudah ada.


2. **Setup Prefab `Player` & Scene:**
* Tambahkan `LootCollector` pada root `Player` (pastikan tag GameObject `Player` adalah `"Player"` dan memiliki `Collider` untuk trigger).
* Buat GameObject `LootDropController` di scene `CombatTest.unity` dan pasang script `LootDropController`.


3. **Pembuatan Asset `LootDefinitionSO` di `Assets/_Project/ScriptableObjects/Loot/`:**
* `Loot_Klewang`: `RewardType = Item`, `ItemDefinition = Weapon_Klewang`, `Amount = 1`.
* `Loot_Souls_Small`: `RewardType = Currency`, `Amount = 50`.
* `Loot_Exp_Small`: `RewardType = Experience`, `Amount = 100`.


4. **Checklist Pengujian L01–L06:**
* **L01 & L02:** Penuhi 20 slot `Inventory` dengan item lain, lalu tabrak `Loot_Klewang` — pickup tetap di scene dan tidak hilang; kosongkan 1 slot lalu tabrak ulang — item masuk ke `Inventory` dan pickup nonaktif.
* **L03 & L04:** Ambil `Loot_Souls_Small` dan `Loot_Exp_Small` — *Souls* dan *EXP* bertambah, jumlah slot terpakai di `Inventory` tetap `0`.
* **L05 & L06:** Panggil `RewardCollector.DistributeRewards()` dua kali berturut-turut — hadiah hanya masuk sekali; ambil pickup hasil drop dua siklus berturut-turut — instance dari pool kembali bersih tanpa sisa `RemainingAmount` lama.