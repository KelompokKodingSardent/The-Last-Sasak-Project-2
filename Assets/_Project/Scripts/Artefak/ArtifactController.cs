// ArtifactController.cs (Tanpa Reflection & terintegrasi ke PlayerStatController)
using System.Collections.Generic;
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
    private PlayerStatController _statController;

    private void Awake()
    {
        _playerHealth = GetComponent<Health>();
        _statController = GetComponent<PlayerStatController>();
    }

    private void Start()
    {
        if (Artifact != null)
            Equip(Artifact);
        else
            RecalculateArtifactStats();
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

    public void Equip(ArtifactDefinitionSO definition)
    {
        Artifact = definition;
        RecalculateArtifactStats();
    }

    public void EquipGameplayArtifact(GameplayArtifactSO artifact)
    {
        if (artifact == null || EquippedGameplayArtifacts.Contains(artifact))
            return;

        EquippedGameplayArtifacts.Add(artifact);
        RecalculateArtifactStats();
    }

    public float GetTotalMaxHealthBonus()
    {
        float total = Artifact != null ? Artifact.HealthBonus : 0f;
        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            if (EquippedGameplayArtifacts[i] != null)
                total += EquippedGameplayArtifacts[i].MaxHealthBonus;
        }
        return total;
    }

    public float GetPassiveDamageMultiplier()
    {
        float mult = Artifact != null && Artifact.DamageMultiplier > 0f ? Artifact.DamageMultiplier : 1f;
        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            GameplayArtifactSO art = EquippedGameplayArtifacts[i];
            if (art != null && art.BaseDamageMultiplier > 0f)
                mult *= art.BaseDamageMultiplier;
        }
        return mult;
    }

    private void RecalculateArtifactStats()
    {
        DamageMultiplier = GetPassiveDamageMultiplier();
        HealthBonus = GetTotalMaxHealthBonus();

        if (_statController != null)
            _statController.RecalculateStats(resetHealthToFull: false);
    }

    public float Modify(float baseValue)
    {
        RegisterComboHit();
        return baseValue * GetCurrentBaseAndBuffMultiplier();
    }

    public float ApplyDamage(float amount) => Modify(amount);

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

        if (totalHeal > 0f && _playerHealth != null)
            _playerHealth.Heal(totalHeal);
    }

    private void HandleRoomCleared()
    {
        float totalHeal = 0f;
        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            if (EquippedGameplayArtifacts[i] != null)
                totalHeal += EquippedGameplayArtifacts[i].HealOnRoomClear;
        }

        if (totalHeal > 0f && _playerHealth != null)
            _playerHealth.Heal(totalHeal);
    }
}