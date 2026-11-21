using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public interface IStatModifier
{
    float Modify(float baseValue);
}

public class ArtifactController : MonoBehaviour, IStatModifier
{
    [Header("Legacy Artifact (Kompatibilitas Scene Lama)")]
    public ArtifactDefinitionSO Artifact;
    public float DamageMultiplier = 1f;
    public float HealthBonus;

    [Header("Phase 5 — Gameplay-Changing Artifacts")]
    public List<GameplayArtifactSO> EquippedGameplayArtifacts = new List<GameplayArtifactSO>();

    [Header("Debug Live Combat Buffs (Read-Only)")]
    [SerializeField] private int currentComboStacks;
    [SerializeField] private float activeCounterTimer;
    [SerializeField] private float totalContextualMultiplier = 1f;

    private float _comboResetTimer;
    private float _activeCounterMultiplier = 1f;
    private Health _playerHealth;

    private FieldInfo _healthCurField;
    private PropertyInfo _healthCurProp;
    private MethodInfo _healMethod;

    private void Awake()
    {
        _playerHealth = GetComponent<Health>();
        CacheHealthReflection();
    }

    private void Start()
    {
        if (Artifact != null)
            Equip(Artifact);

        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            ApplyArtifactStatBonuses(EquippedGameplayArtifacts[i]);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnEntityDied += HandleEntityDied;
        GameEvents.OnRoomCleared += HandleRoomCleared;
    }

    private void OnDisable()
    {
        GameEvents.OnEntityDied -= HandleEntityDied;
        GameEvents.OnRoomCleared -= HandleRoomCleared;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (_comboResetTimer > 0f)
        {
            _comboResetTimer -= dt;
            if (_comboResetTimer <= 0f)
                currentComboStacks = 0;
        }

        if (activeCounterTimer > 0f)
        {
            activeCounterTimer -= dt;
            if (activeCounterTimer <= 0f)
                _activeCounterMultiplier = 1f;
        }

        totalContextualMultiplier = GetCurrentBaseAndBuffMultiplier();
    }

    // Kompatibilitas penuh dengan sistem lama
    public void Equip(ArtifactDefinitionSO definition)
    {
        Artifact = definition;
        if (definition == null)
            return;

        DamageMultiplier = definition.DamageMultiplier;
        HealthBonus = definition.HealthBonus;
        if (_playerHealth != null && HealthBonus > 0f)
            _playerHealth.Configure(_playerHealth.MaxHealth + HealthBonus);
    }

    public void EquipGameplayArtifact(GameplayArtifactSO artifact)
    {
        if (artifact == null || EquippedGameplayArtifacts.Contains(artifact))
            return;

        EquippedGameplayArtifacts.Add(artifact);
        ApplyArtifactStatBonuses(artifact);
    }

    private void ApplyArtifactStatBonuses(GameplayArtifactSO artifact)
    {
        if (artifact == null) return;

        if (artifact.BaseDamageMultiplier > 0f && !Mathf.Approximately(artifact.BaseDamageMultiplier, 1f))
            DamageMultiplier *= artifact.BaseDamageMultiplier;

        if (artifact.MaxHealthBonus > 0f && _playerHealth != null)
        {
            HealthBonus += artifact.MaxHealthBonus;
            _playerHealth.Configure(_playerHealth.MaxHealth + artifact.MaxHealthBonus);
        }
    }

    // Dipanggil oleh AttackController / CombatSystem melalui IStatModifier
    public float Modify(float baseValue)
    {
        RegisterComboHit();
        return baseValue * GetCurrentBaseAndBuffMultiplier();
    }

    public float ApplyDamage(float amount)
    {
        return Modify(amount);
    }

    /// <summary>
    /// Evaluasi damage kontekstual terhadap target tertentu (Staggered, Backstab, Recovery, Combo, Counter).
    /// </summary>
    public float EvaluateContextualDamage(float baseValue, GameObject target)
    {
        RegisterComboHit();
        float mult = GetCurrentBaseAndBuffMultiplier();

        if (target != null && EquippedGameplayArtifacts.Count > 0)
        {
            EnemyBrain brain = target.GetComponentInParent<EnemyBrain>();
            bool isStaggered = brain != null && brain.CurrentState == EnemyBrainState.Staggered;
            bool isVulnerableWindow = brain != null &&
                (brain.CurrentState == EnemyBrainState.Recovery || brain.CurrentState == EnemyBrainState.Telegraph);

            Vector3 toPlayer = transform.position - target.transform.position;
            toPlayer.y = 0f;
            bool isBackstab = toPlayer.sqrMagnitude > 0.01f &&
                              Vector3.Angle(target.transform.forward, toPlayer.normalized) > 115f;

            for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
            {
                GameplayArtifactSO art = EquippedGameplayArtifacts[i];
                if (art == null) continue;

                if (isStaggered && art.StaggeredTargetDamageMultiplier > 1f)
                    mult *= art.StaggeredTargetDamageMultiplier;

                if ((isBackstab || isVulnerableWindow) && art.BackstabOrRecoveryCritMultiplier > 1f)
                    mult *= art.BackstabOrRecoveryCritMultiplier;
            }
        }

        return baseValue * mult;
    }

    /// <summary>
    /// Dipanggil saat player berhasil melakukan Parry terhadap serangan musuh.
    /// </summary>
    public void NotifyParrySuccess(GameObject parriedEnemy)
    {
        float extraStagger = 0f;

        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            GameplayArtifactSO art = EquippedGameplayArtifacts[i];
            if (art == null) continue;

            if (art.Playstyle == ArtifactPlaystyleType.ParryCounter || art.ParryBonusStagger > 0f)
            {
                extraStagger += art.ParryBonusStagger;
                ActivateCounterWindow(art.CounterDamageMultiplier, art.CounterWindowDuration);
            }
        }

        if (extraStagger > 0f && parriedEnemy != null)
        {
            StaggerComponent stagger = parriedEnemy.GetComponentInParent<StaggerComponent>();
            stagger?.Add(extraStagger, true);
        }
    }

    /// <summary>
    /// Dipanggil saat player melakukan Dodge.
    /// </summary>
    public void NotifyDodgeSuccess()
    {
        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            GameplayArtifactSO art = EquippedGameplayArtifacts[i];
            if (art == null) continue;

            if (art.Playstyle == ArtifactPlaystyleType.DodgeMobility && art.CounterDamageMultiplier > 1f)
            {
                ActivateCounterWindow(art.CounterDamageMultiplier, art.CounterWindowDuration);
            }
        }
    }

    private void RegisterComboHit()
    {
        int highestMaxStacks = 0;
        float longestReset = 0f;

        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            GameplayArtifactSO art = EquippedGameplayArtifacts[i];
            if (art != null && art.ComboDamagePerStack > 0f)
            {
                highestMaxStacks = Mathf.Max(highestMaxStacks, art.MaxComboStacks);
                longestReset = Mathf.Max(longestReset, art.ComboResetTime);
            }
        }

        if (highestMaxStacks > 0)
        {
            currentComboStacks = Mathf.Min(highestMaxStacks, currentComboStacks + 1);
            _comboResetTimer = longestReset;
        }
    }

    private void ActivateCounterWindow(float multiplier, float duration)
    {
        _activeCounterMultiplier = Mathf.Max(_activeCounterMultiplier, multiplier);
        activeCounterTimer = Mathf.Max(activeCounterTimer, duration);
    }

    private float GetCurrentBaseAndBuffMultiplier()
    {
        float mult = DamageMultiplier <= 0f ? 1f : DamageMultiplier;

        if (activeCounterTimer > 0f && _activeCounterMultiplier > 1f)
            mult *= _activeCounterMultiplier;

        if (currentComboStacks > 0)
        {
            float perStack = 0f;
            for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
            {
                if (EquippedGameplayArtifacts[i] != null)
                    perStack += EquippedGameplayArtifacts[i].ComboDamagePerStack;
            }
            mult *= (1f + currentComboStacks * perStack);
        }

        return mult;
    }

    private void HandleEntityDied(Health victim)
    {
        if (victim == null || victim == _playerHealth || victim.CompareTag("Player"))
            return;

        float totalHeal = 0f;
        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            if (EquippedGameplayArtifacts[i] != null)
                totalHeal += EquippedGameplayArtifacts[i].HealOnKill;
        }

        if (totalHeal > 0f)
            HealPlayer(totalHeal);
    }

    private void HandleRoomCleared()
    {
        float totalHeal = 0f;
        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            if (EquippedGameplayArtifacts[i] != null)
                totalHeal += EquippedGameplayArtifacts[i].HealOnRoomClear;
        }

        if (totalHeal > 0f)
            HealPlayer(totalHeal);
    }

    private void HealPlayer(float amount)
    {
        if (_playerHealth == null || amount <= 0f) return;

        if (_healMethod != null)
        {
            _healMethod.Invoke(_playerHealth, new object[] { amount });
            return;
        }

        if (_healthCurField != null)
        {
            float cur = System.Convert.ToSingle(_healthCurField.GetValue(_playerHealth));
            float next = Mathf.Min(_playerHealth.MaxHealth, cur + amount);
            _healthCurField.SetValue(_playerHealth, next);
        }
        else if (_healthCurProp != null && _healthCurProp.CanWrite)
        {
            float cur = System.Convert.ToSingle(_healthCurProp.GetValue(_playerHealth));
            float next = Mathf.Min(_playerHealth.MaxHealth, cur + amount);
            _healthCurProp.SetValue(_playerHealth, next);
        }
    }

    private void CacheHealthReflection()
    {
        if (_playerHealth == null) return;
        var t = _playerHealth.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        _healMethod = t.GetMethod("Heal", flags) ?? t.GetMethod("Restore", flags) ?? t.GetMethod("AddHealth", flags);
        _healthCurField = t.GetField("Current", flags) ?? t.GetField("CurrentHealth", flags) ?? t.GetField("currentHealth", flags);
        _healthCurProp = t.GetProperty("Current", flags) ?? t.GetProperty("CurrentHealth", flags);
    }
}