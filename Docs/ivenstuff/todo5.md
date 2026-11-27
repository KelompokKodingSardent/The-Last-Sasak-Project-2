Implementasi **Phase 5 — Stat Modifier, Armor & Artifact** memusatkan seluruh kalkulasi atribut pemain di `PlayerStatController`, menjaga seluruh `ScriptableObject` tetap *immutable* saat runtime, dan menerapkan kebijakan perubahan `MaxHealth` tanpa *auto-heal*.

### Struktur Target Phase 5

```text
Assets/_Project/Scripts/
├── Items/
│   ├── Modifiers/
│   │   └── ModifierCalculator.cs
│   ├── Artifact/
│   │   └── ArtifactController.cs
│   └── Equipment/
│       └── EquipmentTransactionHandler.cs  (Update routing Artifact)
└── Player/
    └── Stats/
        ├── PlayerBaseStatsSO.cs
        ├── MaxHealthPolicy.cs
        └── PlayerStatController.cs

```

---

### 1. Deterministic Modifier Calculator (`Items/Modifiers/ModifierCalculator.cs`)

Menghitung nilai akhir setiap statistik dengan urutan deterministik sesuai rumus:

$$\text{FinalStat} = (\text{Base} + \sum \text{Additive}) \times \prod (1 + \text{Multiplicative})$$

Jika terdapat modifier bertipe `Override`, nilai `Override` terakhir menggantikan hasil kalkulasi standar sebelum diterapkan batas minimum/maksimum (`StatBounds`).

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace TheLastSasak.Items.Modifiers
{
    public readonly struct StatBounds
    {
        public readonly float Min;
        public readonly float Max;

        public StatBounds(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Clamp(float value)
        {
            return Mathf.Clamp(value, Min, Max);
        }
    }

    public static class ModifierCalculator
    {
        private static readonly Dictionary<StatType, StatBounds> DefaultBounds = new Dictionary<StatType, StatBounds>
        {
            { StatType.MaxHealth, new StatBounds(1f, 99999f) },
            { StatType.AttackDamage, new StatBounds(0f, 99999f) },
            { StatType.AttackSpeed, new StatBounds(0.1f, 10f) },
            { StatType.DamageReduction, new StatBounds(0f, 0.85f) },
            { StatType.PoiseResistance, new StatBounds(0f, 1000f) },
            { StatType.KnockbackResistance, new StatBounds(0f, 1f) },
            { StatType.MoveSpeed, new StatBounds(0f, 50f) },
            { StatType.StaminaRegen, new StatBounds(0f, 1000f) }
        };

        public static StatBounds GetBounds(StatType statType)
        {
            return DefaultBounds.TryGetValue(statType, out StatBounds bounds)
                ? bounds
                : new StatBounds(float.MinValue, float.MaxValue);
        }

        public static float CalculateStat(float baseValue, StatType targetStat, IEnumerable<IStatModifier> modifiers)
        {
            return CalculateStat(baseValue, targetStat, modifiers, GetBounds(targetStat));
        }

        public static float CalculateStat(
            float baseValue,
            StatType targetStat,
            IEnumerable<IStatModifier> modifiers,
            StatBounds bounds)
        {
            if (modifiers == null)
            {
                return bounds.Clamp(baseValue);
            }

            float additiveSum = 0f;
            float multiplicativeFactor = 1f;
            bool hasOverride = false;
            float overrideValue = 0f;

            foreach (IStatModifier mod in modifiers)
            {
                if (mod == null || mod.StatType != targetStat)
                {
                    continue;
                }

                switch (mod.Operation)
                {
                    case ModifierOperation.Additive:
                        additiveSum += mod.Value;
                        break;

                    case ModifierOperation.Multiplicative:
                        // Konvensi: +0.15f berarti +15% (pengali 1.15f)
                        multiplicativeFactor *= Mathf.Max(0f, 1f + mod.Value);
                        break;

                    case ModifierOperation.Override:
                        hasOverride = true;
                        overrideValue = mod.Value;
                        break;
                }
            }

            float rawResult = hasOverride
                ? overrideValue
                : (baseValue + additiveSum) * multiplicativeFactor;

            return bounds.Clamp(rawResult);
        }

        public static void CalculateAllStats(
            IReadOnlyDictionary<StatType, float> baseStats,
            IReadOnlyList<IStatModifier> modifiers,
            Dictionary<StatType, float> outputResults)
        {
            outputResults.Clear();

            foreach (KeyValuePair<StatType, float> pair in baseStats)
            {
                StatType stat = pair.Key;
                float finalValue = CalculateStat(pair.Value, stat, modifiers);
                outputResults[stat] = finalValue;
            }
        }
    }
}

```

---

### 2. Artifact Controller (`Items/Artifact/ArtifactController.cs`)

Mengelola slot artefak secara terpisah dari `EquipmentController`, memvalidasi kepemilikan via `IItemOwnershipQuery`, mencegah duplikasi artefak yang sama pada dua slot sekaligus (test **S08**), dan meneruskan daftar modifier ke `PlayerStatController`.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Equipment;
using TheLastSasak.Items.Modifiers;

namespace TheLastSasak.Items.Artifact
{
    public class ArtifactController : MonoBehaviour
    {
        [Header("Slot Configuration")]
        [Range(1, 4)]
        [SerializeField] private int maxArtifactSlots = 2;

        [Header("Initial Loadout (Optional / Testing)")]
        [SerializeField] private List<ArtifactDefinitionSO> startingArtifacts = new List<ArtifactDefinitionSO>();

        [Header("Validation Options")]
        [SerializeField] private bool allowEquipWithoutInventoryInEditor = true;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private ArtifactDefinitionSO[] equippedArtifacts;

        private IWeaponSwitchGate _actionGate;
        private IItemOwnershipQuery _ownershipQuery;

        public event Action<int, ArtifactDefinitionSO> OnArtifactEquipped;
        public event Action<int, ArtifactDefinitionSO> OnArtifactUnequipped;
        public event Action OnArtifactsChanged;

        public int SlotCount => maxArtifactSlots;
        public IReadOnlyList<ArtifactDefinitionSO> EquippedArtifacts => equippedArtifacts;

        private void Awake()
        {
            EnsureArrayInitialized();
            _actionGate = GetComponent<IWeaponSwitchGate>();
            _ownershipQuery = GetComponent<IItemOwnershipQuery>();
        }

        private void Start()
        {
            ApplyInitialArtifacts();
        }

        private void EnsureArrayInitialized()
        {
            if (equippedArtifacts == null || equippedArtifacts.Length != maxArtifactSlots)
            {
                equippedArtifacts = new ArtifactDefinitionSO[maxArtifactSlots];
            }
        }

        public void SetActionGate(IWeaponSwitchGate actionGate)
        {
            _actionGate = actionGate;
        }

        public void SetOwnershipQuery(IItemOwnershipQuery ownershipQuery)
        {
            _ownershipQuery = ownershipQuery;
        }

        public void ApplyInitialArtifacts()
        {
            EnsureArrayInitialized();
            Array.Clear(equippedArtifacts, 0, equippedArtifacts.Length);

            if (startingArtifacts != null)
            {
                int count = Mathf.Min(startingArtifacts.Count, maxArtifactSlots);
                for (int i = 0; i < count; i++)
                {
                    ArtifactDefinitionSO artifact = startingArtifacts[i];
                    if (artifact != null && !IsArtifactEquipped(artifact))
                    {
                        equippedArtifacts[i] = artifact;
                        OnArtifactEquipped?.Invoke(i, artifact);
                    }
                }
            }

            OnArtifactsChanged?.Invoke();
        }

        public bool TryEquipArtifact(int slotIndex, ArtifactDefinitionSO artifact)
        {
            EnsureArrayInitialized();

            if (!IsValidSlotIndex(slotIndex))
            {
                Debug.LogWarning($"[ArtifactController] Index slot artefak tidak valid: {slotIndex}.", this);
                return false;
            }

            if (artifact == null)
            {
                Debug.LogWarning("[ArtifactController] Gagal equip: ArtifactDefinitionSO null.", this);
                return false;
            }

            if (!CanModifyArtifacts())
            {
                return false;
            }

            if (!VerifyOwnership(artifact))
            {
                Debug.LogWarning($"[ArtifactController] Artefak '{artifact.ItemId}' tidak dimiliki di Inventory.", this);
                return false;
            }

            // S08: Cegah artefak yang sama dipasang di dua slot berbeda
            int existingSlot = FindSlotOfArtifact(artifact);
            if (existingSlot != -1 && existingSlot != slotIndex)
            {
                Debug.LogWarning($"[ArtifactController] Artefak '{artifact.ItemId}' sudah terpasang di slot {existingSlot}.", this);
                return false;
            }

            if (equippedArtifacts[slotIndex] == artifact)
            {
                return true;
            }

            ArtifactDefinitionSO previous = equippedArtifacts[slotIndex];
            if (previous != null)
            {
                equippedArtifacts[slotIndex] = null;
                OnArtifactUnequipped?.Invoke(slotIndex, previous);
            }

            equippedArtifacts[slotIndex] = artifact;
            OnArtifactEquipped?.Invoke(slotIndex, artifact);
            OnArtifactsChanged?.Invoke();
            return true;
        }

        public bool TryEquipArtifactAuto(ArtifactDefinitionSO artifact)
        {
            if (artifact == null) return false;

            int existingSlot = FindSlotOfArtifact(artifact);
            if (existingSlot != -1)
            {
                return true;
            }

            int emptySlot = FindFirstEmptySlot();
            int targetSlot = emptySlot != -1 ? emptySlot : 0;
            return TryEquipArtifact(targetSlot, artifact);
        }

        public bool TryUnequipArtifact(int slotIndex)
        {
            EnsureArrayInitialized();

            if (!IsValidSlotIndex(slotIndex))
            {
                return false;
            }

            ArtifactDefinitionSO current = equippedArtifacts[slotIndex];
            if (current == null)
            {
                return false;
            }

            if (!CanModifyArtifacts())
            {
                return false;
            }

            equippedArtifacts[slotIndex] = null;
            OnArtifactUnequipped?.Invoke(slotIndex, current);
            OnArtifactsChanged?.Invoke();
            return true;
        }

        public void ForceUnequipArtifact(ArtifactDefinitionSO artifact)
        {
            int slotIndex = FindSlotOfArtifact(artifact);
            if (slotIndex == -1) return;

            ArtifactDefinitionSO previous = equippedArtifacts[slotIndex];
            equippedArtifacts[slotIndex] = null;
            OnArtifactUnequipped?.Invoke(slotIndex, previous);
            OnArtifactsChanged?.Invoke();
        }

        public ArtifactDefinitionSO GetArtifactInSlot(int slotIndex)
        {
            EnsureArrayInitialized();
            return IsValidSlotIndex(slotIndex) ? equippedArtifacts[slotIndex] : null;
        }

        public IReadOnlyList<ArtifactDefinitionSO> GetActiveArtifacts()
        {
            EnsureArrayInitialized();
            List<ArtifactDefinitionSO> active = new List<ArtifactDefinitionSO>(maxArtifactSlots);
            for (int i = 0; i < equippedArtifacts.Length; i++)
            {
                if (equippedArtifacts[i] != null)
                {
                    active.Add(equippedArtifacts[i]);
                }
            }
            return active;
        }

        public IEnumerable<StatModifier> GetArtifactModifiers()
        {
            EnsureArrayInitialized();
            for (int i = 0; i < equippedArtifacts.Length; i++)
            {
                ArtifactDefinitionSO artifact = equippedArtifacts[i];
                if (artifact == null) continue;

                foreach (StatModifier mod in artifact.GetScopedModifiers())
                {
                    yield return mod;
                }
            }
        }

        public bool IsArtifactEquipped(ArtifactDefinitionSO artifact)
        {
            return FindSlotOfArtifact(artifact) != -1;
        }

        public int FindSlotOfArtifact(ArtifactDefinitionSO artifact)
        {
            EnsureArrayInitialized();
            if (artifact == null) return -1;

            for (int i = 0; i < equippedArtifacts.Length; i++)
            {
                if (equippedArtifacts[i] == artifact)
                {
                    return i;
                }
            }
            return -1;
        }

        public int FindFirstEmptySlot()
        {
            EnsureArrayInitialized();
            for (int i = 0; i < equippedArtifacts.Length; i++)
            {
                if (equippedArtifacts[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool IsValidSlotIndex(int index)
        {
            return index >= 0 && index < maxArtifactSlots;
        }

        private bool CanModifyArtifacts()
        {
            if (_actionGate == null) return true;
            return _actionGate.CanChangeEquipment() && _actionGate.CanSwitchWeapon();
        }

        private bool VerifyOwnership(ItemDefinitionSO item)
        {
            if (_ownershipQuery != null)
            {
                return _ownershipQuery.HasItem(item);
            }

            return allowEquipWithoutInventoryInEditor;
        }
    }
}

```

---

### 3. Player Base Stats & Max Health Policy (`Player/Stats/`)

#### `PlayerBaseStatsSO.cs`

Asset konfigurasi nilai dasar pemain (`Player Base Stats: 1` pada Phase 1) sehingga nilai dasar tidak di-*hardcode* di dalam script.

```csharp
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Modifiers;

namespace TheLastSasak.Player.Stats
{
    [CreateAssetMenu(fileName = "PlayerBaseStats_Default", menuName = "The Last Sasak/Stats/Player Base Stats")]
    public class PlayerBaseStatsSO : ScriptableObject
    {
        [Header("Core Combat & Survival Stats")]
        [Min(1f)]
        [SerializeField] private float maxHealth = 100f;
        [Min(0f)]
        [SerializeField] private float attackDamage = 25f;
        [Min(0.1f)]
        [SerializeField] private float attackSpeed = 1.0f;

        [Header("Defense & Poise")]
        [Range(0f, 0.85f)]
        [SerializeField] private float damageReduction = 0f;
        [Min(0f)]
        [SerializeField] private float poiseResistance = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float knockbackResistance = 0f;

        [Header("Mobility & Stamina")]
        [Min(0f)]
        [SerializeField] private float moveSpeed = 5.0f;
        [Min(0f)]
        [SerializeField] private float staminaRegen = 15.0f;

        public float MaxHealth => maxHealth;
        public float AttackDamage => attackDamage;
        public float AttackSpeed => attackSpeed;
        public float DamageReduction => damageReduction;
        public float PoiseResistance => poiseResistance;
        public float KnockbackResistance => knockbackResistance;
        public float MoveSpeed => moveSpeed;
        public float StaminaRegen => staminaRegen;

        public void PopulateDictionary(Dictionary<StatType, float> target)
        {
            target[StatType.MaxHealth] = maxHealth;
            target[StatType.AttackDamage] = attackDamage;
            target[StatType.AttackSpeed] = attackSpeed;
            target[StatType.DamageReduction] = damageReduction;
            target[StatType.PoiseResistance] = poiseResistance;
            target[StatType.KnockbackResistance] = knockbackResistance;
            target[StatType.MoveSpeed] = moveSpeed;
            target[StatType.StaminaRegen] = staminaRegen;
        }
    }
}

```

#### `MaxHealthPolicy.cs`

Mengatur perilaku HP saat `MaxHealth` berubah akibat pemasangan/pelepasan armor atau artefak (**Point 5.5**).

```csharp
namespace TheLastSasak.Player.Stats
{
    public enum MaxHealthRecalculationPolicy
    {
        /// <summary>
        /// Default v1.0: Current HP tetap (50/100 menjadi 50/120), hanya di-clamp jika melebihi MaxHealth baru.
        /// </summary>
        KeepCurrentAndClamp = 0,

        /// <summary>
        /// Mempertahankan rasio persentase HP (50/100 menjadi 60/120).
        /// </summary>
        PreservePercentage = 1
    }

    public interface IHealthStatReceiver
    {
        void ApplyMaxHealthChange(float previousMaxHealth, float newMaxHealth, MaxHealthRecalculationPolicy policy);
    }

    public static class HealthPolicyUtility
    {
        public static float ResolveNewCurrentHealth(
            float currentHealth,
            float previousMaxHealth,
            float newMaxHealth,
            MaxHealthRecalculationPolicy policy)
        {
            if (newMaxHealth <= 0f)
            {
                return 0f;
            }

            if (policy == MaxHealthRecalculationPolicy.PreservePercentage && previousMaxHealth > 0f)
            {
                float ratio = currentHealth / previousMaxHealth;
                return UnityEngine.Mathf.Clamp(newMaxHealth * ratio, 0f, newMaxHealth);
            }

            // KeepCurrentAndClamp: tidak pernah auto-heal saat MaxHealth naik
            return UnityEngine.Mathf.Clamp(currentHealth, 0f, newMaxHealth);
        }
    }
}

```

---

### 4. Central Stat Hub (`Player/Stats/PlayerStatController.cs`)

Satu-satunya sumber kebenaran statistik akhir. Menggabungkan *Base Stats*, *Permanent Upgrades*, *In-Run Progression*, *Active Weapon*, *Equipped Armor*, *Equipped Artifacts*, dan *Temporary Effects*, serta menyediakan kalkulasi pratinjau tanpa mutasi untuk `StatComparisonUI` di Phase 8.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Artifact;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Equipment;
using TheLastSasak.Items.Modifiers;

namespace TheLastSasak.Player.Stats
{
    public class PlayerStatController : MonoBehaviour
    {
        [Header("Base Configuration")]
        [SerializeField] private PlayerBaseStatsSO baseStatsAsset;

        [Header("Fallback Base Values (jika BaseStatsSO kosong)")]
        [SerializeField] private float fallbackMaxHealth = 100f;
        [SerializeField] private float fallbackAttackDamage = 25f;
        [SerializeField] private float fallbackAttackSpeed = 1.0f;
        [SerializeField] private float fallbackMoveSpeed = 5.0f;
        [SerializeField] private float fallbackStaminaRegen = 15.0f;

        [Header("Dependencies")]
        [SerializeField] private EquipmentController equipmentController;
        [SerializeField] private ArtifactController artifactController;

        [Header("Health Policy (Point 5.5)")]
        [SerializeField] private MaxHealthRecalculationPolicy maxHealthPolicy = MaxHealthRecalculationPolicy.KeepCurrentAndClamp;

        private readonly Dictionary<StatType, float> _baseStats = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _finalStats = new Dictionary<StatType, float>();

        // Jalur modifier non-equipment
        private readonly List<IStatModifier> _permanentUpgrades = new List<IStatModifier>();
        private readonly List<IStatModifier> _inRunProgression = new List<IStatModifier>();
        private readonly List<IStatModifier> _temporaryEffects = new List<IStatModifier>();
        private readonly List<IStatModifier> _combinedBuffer = new List<IStatModifier>(32);

        private IHealthStatReceiver _healthReceiver;
        private float _lastKnownMaxHealth;

        public event Action OnStatsRecalculated;
        public event Action<float, float, MaxHealthRecalculationPolicy> OnMaxHealthChanged;

        // Shortcut properti statistik akhir
        public float FinalMaxHealth => GetStat(StatType.MaxHealth);
        public float FinalAttackDamage => GetStat(StatType.AttackDamage);
        public float FinalAttackSpeed => GetStat(StatType.AttackSpeed);
        public float FinalDamageReduction => GetStat(StatType.DamageReduction);
        public float FinalPoiseResistance => GetStat(StatType.PoiseResistance);
        public float FinalKnockbackResistance => GetStat(StatType.KnockbackResistance);
        public float FinalMoveSpeed => GetStat(StatType.MoveSpeed);
        public float FinalStaminaRegen => GetStat(StatType.StaminaRegen);

        private void Awake()
        {
            if (equipmentController == null)
            {
                equipmentController = GetComponent<EquipmentController>();
            }

            if (artifactController == null)
            {
                artifactController = GetComponent<ArtifactController>();
            }

            _healthReceiver = GetComponent<IHealthStatReceiver>();

            InitializeBaseStats();
            RecalculateStats(notifyHealthReceiver: false);
            _lastKnownMaxHealth = FinalMaxHealth;
        }

        private void OnEnable()
        {
            if (equipmentController != null)
            {
                equipmentController.OnEquipmentChanged += HandleEquipmentChanged;
            }

            if (artifactController != null)
            {
                artifactController.OnArtifactsChanged += HandleArtifactsChanged;
            }
        }

        private void OnDisable()
        {
            if (equipmentController != null)
            {
                equipmentController.OnEquipmentChanged -= HandleEquipmentChanged;
            }

            if (artifactController != null)
            {
                artifactController.OnArtifactsChanged -= HandleArtifactsChanged;
            }
        }

        private void Start()
        {
            RecalculateStats(notifyHealthReceiver: true);
        }

        private void InitializeBaseStats()
        {
            _baseStats.Clear();

            if (baseStatsAsset != null)
            {
                baseStatsAsset.PopulateDictionary(_baseStats);
            }
            else
            {
                _baseStats[StatType.MaxHealth] = fallbackMaxHealth;
                _baseStats[StatType.AttackDamage] = fallbackAttackDamage;
                _baseStats[StatType.AttackSpeed] = fallbackAttackSpeed;
                _baseStats[StatType.DamageReduction] = 0f;
                _baseStats[StatType.PoiseResistance] = 0f;
                _baseStats[StatType.KnockbackResistance] = 0f;
                _baseStats[StatType.MoveSpeed] = fallbackMoveSpeed;
                _baseStats[StatType.StaminaRegen] = fallbackStaminaRegen;
            }
        }

        private void HandleEquipmentChanged()
        {
            RecalculateStats(notifyHealthReceiver: true);
        }

        private void HandleArtifactsChanged()
        {
            RecalculateStats(notifyHealthReceiver: true);
        }

        public void RecalculateStats()
        {
            RecalculateStats(notifyHealthReceiver: true);
        }

        private void RecalculateStats(bool notifyHealthReceiver)
        {
            float previousMaxHealth = _lastKnownMaxHealth > 0f ? _lastKnownMaxHealth : GetBaseStat(StatType.MaxHealth);

            CollectAllActiveModifiers(_combinedBuffer);
            ModifierCalculator.CalculateAllStats(_baseStats, _combinedBuffer, _finalStats);

            float newMaxHealth = FinalMaxHealth;
            if (!Mathf.Approximately(previousMaxHealth, newMaxHealth))
            {
                _lastKnownMaxHealth = newMaxHealth;
                if (notifyHealthReceiver)
                {
                    _healthReceiver?.ApplyMaxHealthChange(previousMaxHealth, newMaxHealth, maxHealthPolicy);
                    OnMaxHealthChanged?.Invoke(previousMaxHealth, newMaxHealth, maxHealthPolicy);
                }
            }

            OnStatsRecalculated?.Invoke();
        }

        /// <summary>
        /// Mengumpulkan modifier sesuai urutan arsitektur Bagian III Point 8.2.
        /// </summary>
        private void CollectAllActiveModifiers(List<IStatModifier> targetList)
        {
            targetList.Clear();

            // 1. Permanent Upgrades
            targetList.AddRange(_permanentUpgrades);

            // 2. In-Run Progression
            targetList.AddRange(_inRunProgression);

            // 3. Active Weapon Modifiers (hanya senjata yang sedang aktif di tangan)
            if (equipmentController != null && equipmentController.ActiveWeapon != null)
            {
                foreach (StatModifier mod in equipmentController.ActiveWeapon.GetScopedModifiers())
                {
                    targetList.Add(mod);
                }
            }

            // 4. Armor Modifiers
            if (equipmentController != null && equipmentController.EquippedArmor != null)
            {
                foreach (StatModifier mod in equipmentController.EquippedArmor.GetScopedModifiers())
                {
                    targetList.Add(mod);
                }
            }

            // 5. Artifact Modifiers
            if (artifactController != null)
            {
                foreach (StatModifier mod in artifactController.GetArtifactModifiers())
                {
                    targetList.Add(mod);
                }
            }

            // 6. Temporary Effects
            targetList.AddRange(_temporaryEffects);
        }

        public float GetBaseStat(StatType statType)
        {
            return _baseStats.TryGetValue(statType, out float value) ? value : 0f;
        }

        public float GetStat(StatType statType)
        {
            return _finalStats.TryGetValue(statType, out float value) ? value : GetBaseStat(statType);
        }

        // API untuk Permanent Upgrades, In-Run Progression & Temporary Effects
        public void SetPermanentUpgrades(IEnumerable<IStatModifier> modifiers)
        {
            _permanentUpgrades.Clear();
            if (modifiers != null) _permanentUpgrades.AddRange(modifiers);
            RecalculateStats(notifyHealthReceiver: true);
        }

        public void SetInRunProgression(IEnumerable<IStatModifier> modifiers)
        {
            _inRunProgression.Clear();
            if (modifiers != null) _inRunProgression.AddRange(modifiers);
            RecalculateStats(notifyHealthReceiver: true);
        }

        public void AddTemporaryModifier(IStatModifier modifier)
        {
            if (modifier == null) return;
            _temporaryEffects.Add(modifier);
            RecalculateStats(notifyHealthReceiver: true);
        }

        public void RemoveTemporaryModifiersBySource(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) return;

            int removed = _temporaryEffects.RemoveAll(m => m.SourceId == sourceId);
            if (removed > 0)
            {
                RecalculateStats(notifyHealthReceiver: true);
            }
        }

        /// <summary>
        /// Menghitung pratinjau statistik untuk UI perbandingan (Phase 8) tanpa mengubah state aktif.
        /// </summary>
        public Dictionary<StatType, float> PreviewStatsWithCandidate(EquipmentDefinitionSO candidate)
        {
            WeaponDefinitionSO previewWeapon = equipmentController != null ? equipmentController.ActiveWeapon : null;
            ArmorDefinitionSO previewArmor = equipmentController != null ? equipmentController.EquippedArmor : null;

            if (candidate is WeaponDefinitionSO candidateWeapon)
            {
                previewWeapon = candidateWeapon;
            }
            else if (candidate is ArmorDefinitionSO candidateArmor)
            {
                previewArmor = candidateArmor;
            }

            List<IStatModifier> previewBuffer = new List<IStatModifier>(32);
            previewBuffer.AddRange(_permanentUpgrades);
            previewBuffer.AddRange(_inRunProgression);

            if (previewWeapon != null)
            {
                foreach (StatModifier mod in previewWeapon.GetScopedModifiers())
                {
                    previewBuffer.Add(mod);
                }
            }

            if (previewArmor != null)
            {
                foreach (StatModifier mod in previewArmor.GetScopedModifiers())
                {
                    previewBuffer.Add(mod);
                }
            }

            if (artifactController != null)
            {
                foreach (StatModifier mod in artifactController.GetArtifactModifiers())
                {
                    previewBuffer.Add(mod);
                }
            }

            previewBuffer.AddRange(_temporaryEffects);

            Dictionary<StatType, float> result = new Dictionary<StatType, float>(_baseStats.Count);
            ModifierCalculator.CalculateAllStats(_baseStats, previewBuffer, result);
            return result;
        }
    }
}

```

---

### 5. Integrasi Artifact ke `EquipmentTransactionHandler.cs` (Phase 4)

Perbarui `EquipmentTransactionHandler.cs` agar `TryEquipFromInventory` dan otomatis *unequip* saat item dibuang juga mendukung `ItemCategory.Artifact`:

```csharp
// 1. Tambahkan field dependency di EquipmentTransactionHandler.cs:
// [SerializeField] private TheLastSasak.Items.Artifact.ArtifactController artifactController;

// 2. Di dalam Awake():
// if (artifactController == null) artifactController = GetComponent<TheLastSasak.Items.Artifact.ArtifactController>();
// if (artifactController != null && inventory != null) artifactController.SetOwnershipQuery(inventory);

// 3. Di dalam switch (item.Category) pada TryEquipFromInventory():
case ItemCategory.Artifact:
    if (item is not ArtifactDefinitionSO artifact || artifactController == null)
    {
        return false;
    }
    return artifactController.TryEquipArtifactAuto(artifact);

// 4. Di dalam HandleInventoryItemRemoved():
else if (removedItem is ArtifactDefinitionSO artifact && artifactController != null)
{
    artifactController.ForceUnequipArtifact(artifact);
}

```

---

### 6. Panduan Wiring & Validasi Phase 5 (S01–S09)

1. **Asset `PlayerBaseStats_Default`:**
* Buat via menu **Create - The Last Sasak - Stats - Player Base Stats** di `Assets/_Project/ScriptableObjects/Stats/`.
* Pasang asset tersebut ke komponen `PlayerStatController` pada root `Player`.


2. **Komponen pada Root `Player`:**
* Tambahkan `ArtifactController` (`Max Artifact Slots = 2`) dan `PlayerStatController`.
* Implementasikan `IHealthStatReceiver` pada komponen `Health` pemain menggunakan `HealthPolicyUtility.ResolveNewCurrentHealth(...)` agar saat `Armor_BesiAmbat` (+20 `MaxHealth`) dipasang pada kondisi HP `50/100`, HP menjadi `50/120` (**S07**).


3. **Verifikasi Contoh Perhitungan Deterministik (S06):**
* Base `MaxHealth = 100` + `Armor_BesiAmbat` (`+20 Additive`) + `Artifact_BatuAmbang` (`+25 Additive`) = **145**.
* Base `AttackDamage = 25` × `Artifact_PecahanArwah` (`+0.15 Multiplicative`) = $25 \times 1.15 =$ **28.75**.