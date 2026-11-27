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

        // Properti kompatibilitas untuk sistem Combat & Movement lama
        public float FinalAttackMultiplier
        {
            get
            {
                float baseAtk = GetBaseStat(StatType.AttackDamage);
                return baseAtk > 0f ? FinalAttackDamage / baseAtk : 1f;
            }
        }
        public float FinalFlatAttackBonus => Mathf.Max(0f, FinalAttackDamage - GetBaseStat(StatType.AttackDamage));
        public float FinalStaggerMultiplier => 1f;
        public float FinalDodgeRecoveryMultiplier => 1f;
        public float FinalParryWindowBonus => 0f;

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
            if (_healthReceiver is Health healthComponent)
            {
                bool wasAtFullHealth = Mathf.Approximately(healthComponent.CurrentHealth, healthComponent.MaxHealth);
                healthComponent.SyncInitialMaxHealth(FinalMaxHealth, fillCurrentToMax: wasAtFullHealth);
            }

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

        private void CollectAllActiveModifiers(List<IStatModifier> targetList)
        {
            targetList.Clear();

            // 1. Permanent Upgrades
            targetList.AddRange(_permanentUpgrades);

            // 2. In-Run Progression
            targetList.AddRange(_inRunProgression);

            // 3. Active Weapon Modifiers
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

        // Adapter kompatibilitas untuk PlayerProgression & PermanentUpgradeSystem
        public void SetPermanentHealthBonus(float bonus)
        {
            _permanentUpgrades.RemoveAll(m => m.SourceId == "permanent_health_bonus");
            if (bonus > 0f)
            {
                _permanentUpgrades.Add(new StatModifier(StatType.MaxHealth, ModifierOperation.Additive, bonus, "permanent_health_bonus"));
            }
            RecalculateStats(notifyHealthReceiver: true);
        }

        public void SetRunLevelHealthBonus(float bonus)
        {
            _inRunProgression.RemoveAll(m => m.SourceId == "run_level_health_bonus");
            if (bonus > 0f)
            {
                _inRunProgression.Add(new StatModifier(StatType.MaxHealth, ModifierOperation.Additive, bonus, "run_level_health_bonus"));
            }
            RecalculateStats(notifyHealthReceiver: true);
        }

        public float ComputeOutgoingDamage(float rawAttackDamage)
        {
            return Mathf.Max(0f, rawAttackDamage * FinalAttackMultiplier);
        }

        public float ComputeIncomingDamage(float rawIncomingDamage)
        {
            return Mathf.Max(0f, rawIncomingDamage * (1f - Mathf.Clamp01(FinalDamageReduction)));
        }

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

            List<ArtifactDefinitionSO> previewArtifacts = new List<ArtifactDefinitionSO>();
            if (artifactController != null)
            {
                previewArtifacts.AddRange(artifactController.GetActiveArtifacts());
                if (candidate is ArtifactDefinitionSO candidateArtifact && !previewArtifacts.Contains(candidateArtifact))
                {
                    if (previewArtifacts.Count < artifactController.SlotCount)
                        previewArtifacts.Add(candidateArtifact);
                    else if (previewArtifacts.Count > 0)
                        previewArtifacts[0] = candidateArtifact;
                }
            }

            for (int i = 0; i < previewArtifacts.Count; i++)
            {
                foreach (StatModifier mod in previewArtifacts[i].GetScopedModifiers())
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