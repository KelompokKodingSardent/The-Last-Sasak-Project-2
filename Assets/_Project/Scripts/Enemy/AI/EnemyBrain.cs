using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum EnemyBrainState
{
    Idle,
    Approach,
    Positioning,
    Telegraph,
    Startup,
    Execution,
    Recovery,
    Staggered,
    Reposition,
    Dead
}

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyAttackController))]
[RequireComponent(typeof(Health))]
public class EnemyBrain : MonoBehaviour
{
    [Header("Definisi Archetype (Wajib Di-assign)")]
    [SerializeField] private EnemyDefinitionSO definition;

    [Header("Phase 4 — Enemy Modifiers (Opsional)")]
    [SerializeField] private List<EnemyModifierSO> initialModifiers = new List<EnemyModifierSO>();

    [Header("Debug State (Read-Only)")]
    [SerializeField] private EnemyBrainState currentState = EnemyBrainState.Idle;
    [SerializeField] private int currentStage = 1;
    [SerializeField] private int staggerBreakCount = 0;
    [SerializeField] private EnemyTacticalRole currentRoleDebug = EnemyTacticalRole.Hold;
    [SerializeField] private CombatSector currentSectorDebug = CombatSector.Front;
    [SerializeField] private string currentPatternDebug;
    [SerializeField] private bool isEmpoweredDebug;
    [SerializeField] private bool isGuardActiveDebug;

    private EnemyMovement _movement;
    private EnemyAttackController _attackController;
    private Health _health;
    private StaggerComponent _stagger;
    private Transform _playerTarget;

    private readonly Dictionary<EnemyAttackPatternSO, float> _patternCooldowns = new Dictionary<EnemyAttackPatternSO, float>();
    private readonly List<EnemyAttackPatternSO> _activePatterns = new List<EnemyAttackPatternSO>();
    private readonly List<EnemyAttackPatternSO> _runtimeFallbackPatterns = new List<EnemyAttackPatternSO>();
    private readonly List<EnemyModifierSO> _activeModifiers = new List<EnemyModifierSO>();

    private EnemyAttackPatternSO _selectedPattern;
    private int _currentSwingIndex;
    private float _stateTimer;
    private float _stateDuration;
    private float _decisionTimer;
    private float _sideDashCooldownTimer;
    private float _strafeSign = 1f;
    private float _strafeSwitchTimer;
    private bool _isSideDashReposition;
    private Vector3 _repositionDestination;

    // Phase 3: Controller Empower Buff State
    private float _empowerTimer;
    private float _empowerSpeedMultiplier = 1f;
    private float _empowerDamageMultiplier = 1f;

    // Phase 4: Boss Stage & Modifier Multipliers
    private float _stageMoveSpeedMult = 1f;
    private float _stageCooldownMult = 1f;
    private float _stageDecisionMult = 1f;
    private float _stageDamageMult = 1f;

    private float _modMoveSpeedMult = 1f;
    private float _modRepositionSpeedMult = 1f;
    private float _modCooldownMult = 1f;
    private float _modDecisionMult = 1f;
    private float _modDamageMult = 1f;
    private float _modBonusStaggerRes = 0f;
    private float _modBonusFlankChance = 0f;
    private float _modBonusChainChance = 0f;
    private bool _modGrantGuard;
    private bool _modGrantSideDash;
    private bool _modForceReposition;

    private float _initialMaxStagger = 100f;

    // Cached reflection untuk membaca rasio HP dari Health.cs tanpa ketergantungan nama field
    private FieldInfo _healthCurField;
    private FieldInfo _healthMaxField;
    private PropertyInfo _healthCurProp;
    private PropertyInfo _healthMaxProp;

    public EnemyBrainState CurrentState => currentState;
    public int CurrentStage => currentStage;
    public bool IsEliteOrBoss => definition != null &&
        (definition.IsEliteOrBoss || definition.Archetype == EnemyArchetype.Elite || definition.Archetype == EnemyArchetype.Boss);

    public float SpeedMultiplier => (_empowerTimer > 0f ? _empowerSpeedMultiplier : 1f) * _stageMoveSpeedMult * _modMoveSpeedMult;
    public float RepositionSpeedMultiplier => (_empowerTimer > 0f ? _empowerSpeedMultiplier : 1f) * _stageMoveSpeedMult * _modRepositionSpeedMult;
    public float DamageMultiplier => (_empowerTimer > 0f ? _empowerDamageMultiplier : 1f) * _stageDamageMult * _modDamageMult;

    public EnemyDefinitionSO Definition
    {
        get => definition;
        set => Bind(value);
    }

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _attackController = GetComponent<EnemyAttackController>();
        _health = GetComponent<Health>();
        _stagger = GetComponent<StaggerComponent>();
        _strafeSign = Random.value > 0.5f ? 1f : -1f;

        if (_stagger != null)
            _initialMaxStagger = _stagger.MaxStagger;

        CacheHealthReflection();
    }

    private void OnEnable()
    {
        _health.OnDied += HandleDied;
        _attackController.OnAttackParried += HandleParried;

        if (_stagger != null)
            _stagger.OnStaggered += HandleStaggerBroken;

        if (CombatDirector.Instance != null)
            CombatDirector.Instance.RegisterEnemy(this);
    }

    private void OnDisable()
    {
        _health.OnDied -= HandleDied;
        _attackController.OnAttackParried -= HandleParried;

        if (_stagger != null)
            _stagger.OnStaggered -= HandleStaggerBroken;

        if (CombatDirector.Instance != null)
            CombatDirector.Instance.UnregisterEnemy(this);
    }

    private void Start()
    {
        FindPlayer();
        RebuildModifierCache();
        ApplyDefinitionStats();
        InitializePatterns();

        if (CombatDirector.Instance != null)
            CombatDirector.Instance.RegisterEnemy(this);

        ChangeState(EnemyBrainState.Idle);
    }

    public void Bind(EnemyDefinitionSO newDefinition)
    {
        definition = newDefinition;
        currentStage = 1;
        staggerBreakCount = 0;
        ResetStageMultipliers();
        RebuildModifierCache();
        ApplyDefinitionStats();
        InitializePatterns();
        ResetBrain();
    }

    // Overload kompatibilitas untuk RoomEncounterController.cs (3 argumen)
    public void Bind(EnemyDefinitionSO newDefinition, object arg2, object arg3)
    {
        if (arg2 is Transform t2) _playerTarget = t2;
        else if (arg2 is GameObject g2) _playerTarget = g2.transform;
        else if (arg2 is ProjectilePool p2) _attackController.SetProjectilePool(p2);

        if (arg3 is Transform t3) _playerTarget = t3;
        else if (arg3 is GameObject g3) _playerTarget = g3.transform;
        else if (arg3 is ProjectilePool p3) _attackController.SetProjectilePool(p3);

        Bind(newDefinition);
    }

    public void AddModifier(EnemyModifierSO modifier)
    {
        if (modifier == null || _activeModifiers.Contains(modifier)) return;
        _activeModifiers.Add(modifier);
        RecalculateModifiers();
        ApplyDefinitionStats();
        if (modifier.ExtraAttackPattern != null && !_patternCooldowns.ContainsKey(modifier.ExtraAttackPattern))
        {
            _activePatterns.Add(modifier.ExtraAttackPattern);
            _patternCooldowns.Add(modifier.ExtraAttackPattern, 0f);
        }
    }

    public void ClearModifiers()
    {
        _activeModifiers.Clear();
        RecalculateModifiers();
        ApplyDefinitionStats();
    }

    public void ApplyEmpower(float duration, float speedMult, float damageMult)
    {
        _empowerTimer = Mathf.Max(_empowerTimer, duration);
        _empowerSpeedMultiplier = Mathf.Max(_empowerSpeedMultiplier, speedMult);
        _empowerDamageMultiplier = Mathf.Max(_empowerDamageMultiplier, damageMult);
    }

    private void RebuildModifierCache()
    {
        _activeModifiers.Clear();
        if (initialModifiers != null)
        {
            for (int i = 0; i < initialModifiers.Count; i++)
            {
                if (initialModifiers[i] != null && !_activeModifiers.Contains(initialModifiers[i]))
                    _activeModifiers.Add(initialModifiers[i]);
            }
        }
        RecalculateModifiers();
    }

    private void RecalculateModifiers()
    {
        _modMoveSpeedMult = 1f;
        _modRepositionSpeedMult = 1f;
        _modCooldownMult = 1f;
        _modDecisionMult = 1f;
        _modDamageMult = 1f;
        _modBonusStaggerRes = 0f;
        _modBonusFlankChance = 0f;
        _modBonusChainChance = 0f;
        _modGrantGuard = false;
        _modGrantSideDash = false;
        _modForceReposition = false;

        for (int i = 0; i < _activeModifiers.Count; i++)
        {
            EnemyModifierSO mod = _activeModifiers[i];
            if (mod == null) continue;

            _modMoveSpeedMult *= mod.MoveSpeedMultiplier;
            _modRepositionSpeedMult *= mod.RepositionSpeedMultiplier;
            _modCooldownMult *= mod.AttackCooldownMultiplier;
            _modDecisionMult *= mod.DecisionDelayMultiplier;
            _modDamageMult *= mod.DamageMultiplier;
            _modBonusStaggerRes += mod.BonusStaggerResistance;
            _modBonusFlankChance += mod.BonusFlankRepositionChance;
            _modBonusChainChance += mod.BonusChainChance;
            if (mod.GrantFrontalGuard) _modGrantGuard = true;
            if (mod.GrantSideDash) _modGrantSideDash = true;
            if (mod.ForceRepositionAfterAttack) _modForceReposition = true;
        }
    }

    private void ResetStageMultipliers()
    {
        _stageMoveSpeedMult = 1f;
        _stageCooldownMult = 1f;
        _stageDecisionMult = 1f;
        _stageDamageMult = 1f;
    }

    private void ApplyDefinitionStats()
    {
        if (definition == null || _stagger == null) return;
        _stagger.StaggerResistance = Mathf.Clamp(definition.BaseStaggerResistance + _modBonusStaggerRes, 0f, 0.85f);
        _stagger.MaxStagger = _initialMaxStagger * (1f + staggerBreakCount * definition.StaggerGrowthPerBreak);
    }

    public void ResetBrain()
    {
        if (currentState == EnemyBrainState.Staggered)
            return;

        _attackController.AbortAttack();
        _movement.CancelLunge();
        _movement.Stop();
        ReleaseDirectorSlot();

        _selectedPattern = null;
        _currentSwingIndex = 0;
        _sideDashCooldownTimer = 0f;
        _isSideDashReposition = false;
        _empowerTimer = 0f;
        _empowerSpeedMultiplier = 1f;
        _empowerDamageMultiplier = 1f;

        if (_stagger != null)
        {
            _stagger.ResetStagger();
            _stagger.IsGuardActive = false;
        }

        var keys = new List<EnemyAttackPatternSO>(_patternCooldowns.Keys);
        foreach (var k in keys)
            _patternCooldowns[k] = 0f;

        ChangeState(EnemyBrainState.Idle);
    }

    private void InitializePatterns()
    {
        _activePatterns.Clear();
        _patternCooldowns.Clear();

        if (definition == null) return;

        if (definition.AttackPatterns != null)
        {
            foreach (var p in definition.AttackPatterns)
            {
                RegisterPatternIfValid(p);
            }
        }

        // Daftarkan juga pola serangan tambahan dari BossStages & Modifiers
        if (definition.BossStages != null)
        {
            foreach (var stage in definition.BossStages)
            {
                if (stage == null || stage.unlockedPatterns == null) continue;
                foreach (var p in stage.unlockedPatterns)
                    RegisterPatternIfValid(p);
            }
        }

        for (int i = 0; i < _activeModifiers.Count; i++)
        {
            if (_activeModifiers[i] != null)
                RegisterPatternIfValid(_activeModifiers[i].ExtraAttackPattern);
        }

        if (_activePatterns.Count == 0)
        {
            BuildRuntimeFallbackPatterns();
            foreach (var p in _runtimeFallbackPatterns)
                RegisterPatternIfValid(p);
        }
    }

    private void RegisterPatternIfValid(EnemyAttackPatternSO p)
    {
        if (p != null && p.swings != null && p.swings.Count > 0 && !_patternCooldowns.ContainsKey(p))
        {
            _activePatterns.Add(p);
            _patternCooldowns.Add(p, 0f);
        }
    }

    private void BuildRuntimeFallbackPatterns()
    {
        _runtimeFallbackPatterns.Clear();

        if (definition.Archetype == EnemyArchetype.Ranged || definition.Archetype == EnemyArchetype.Ranger)
        {
            EnemyAttackPatternSO focused = ScriptableObject.CreateInstance<EnemyAttackPatternSO>();
            focused.patternName = "Focused Shot (Runtime)";
            focused.selectionWeight = 60;
            focused.cooldown = 1.6f;
            focused.minTriggerRange = 3.2f;
            focused.maxTriggerRange = definition.AttackRange + 1.5f;
            focused.canBeParried = true;
            focused.baseDamage = definition.AttackDamage;
            focused.repositionAfterAttack = true;
            focused.repositionDistance = 2.0f;
            focused.swings.Add(new AttackSwingStep
            {
                deliveryType = AttackDeliveryType.Projectile,
                telegraphDuration = 0.55f,
                startupDuration = 0.10f,
                executionDuration = 0.08f,
                recoveryDuration = 0.45f,
                telegraphShape = TelegraphShape.Line,
                range = definition.AttackRange,
                width = 0.5f,
                projectileCount = 1
            });
            _runtimeFallbackPatterns.Add(focused);

            EnemyAttackPatternSO retreat = ScriptableObject.CreateInstance<EnemyAttackPatternSO>();
            retreat.patternName = "Retreat Shot (Runtime)";
            retreat.selectionWeight = 85;
            retreat.cooldown = 2.4f;
            retreat.minTriggerRange = 0.2f;
            retreat.maxTriggerRange = 4.5f;
            retreat.canBeParried = true;
            retreat.baseDamage = definition.AttackDamage * 0.85f;
            retreat.swings.Add(new AttackSwingStep
            {
                deliveryType = AttackDeliveryType.Projectile,
                telegraphDuration = 0.35f,
                startupDuration = 0.08f,
                executionDuration = 0.08f,
                recoveryDuration = 0.40f,
                telegraphShape = TelegraphShape.Line,
                range = 6f,
                width = 0.5f,
                projectileCount = 1,
                lungeDistance = -2.4f,
                lungeDuration = 0.22f
            });
            _runtimeFallbackPatterns.Add(retreat);
        }
        else if (definition.Archetype == EnemyArchetype.Bulwark)
        {
            EnemyAttackPatternSO bash = ScriptableObject.CreateInstance<EnemyAttackPatternSO>();
            bash.patternName = "Shield Bash (Runtime)";
            bash.selectionWeight = 55;
            bash.cooldown = 1.8f;
            bash.minTriggerRange = 0.2f;
            bash.maxTriggerRange = 2.6f;
            bash.canBeParried = true;
            bash.parryStaggerBonus = 45f;
            bash.baseDamage = definition.AttackDamage * 0.85f;
            bash.swings.Add(new AttackSwingStep
            {
                deliveryType = AttackDeliveryType.Melee,
                telegraphDuration = 0.50f,
                startupDuration = 0.12f,
                executionDuration = 0.12f,
                recoveryDuration = 0.65f,
                telegraphShape = TelegraphShape.Cone,
                range = 2.4f,
                angle = 105f,
                staggerDamage = 25f,
                lungeDistance = 0.5f,
                lungeDuration = 0.12f
            });
            _runtimeFallbackPatterns.Add(bash);

            EnemyAttackPatternSO slam = ScriptableObject.CreateInstance<EnemyAttackPatternSO>();
            slam.patternName = "Ground Slam (Runtime)";
            slam.selectionWeight = 45;
            slam.cooldown = 3.2f;
            slam.minTriggerRange = 0.2f;
            slam.maxTriggerRange = 3.2f;
            slam.canBeParried = false;
            slam.baseDamage = definition.AttackDamage * 1.25f;
            slam.swings.Add(new AttackSwingStep
            {
                deliveryType = AttackDeliveryType.Melee,
                telegraphDuration = 0.80f,
                startupDuration = 0.15f,
                executionDuration = 0.15f,
                recoveryDuration = 0.90f,
                telegraphShape = TelegraphShape.Circle,
                range = 3.1f,
                staggerDamage = 35f
            });
            _runtimeFallbackPatterns.Add(slam);
        }
        else if (definition.Archetype == EnemyArchetype.Elite || definition.Archetype == EnemyArchetype.Boss)
        {
            // Pola 1: Double Cross Chain (Parryable)
            EnemyAttackPatternSO chainCombo = ScriptableObject.CreateInstance<EnemyAttackPatternSO>();
            chainCombo.patternName = "Elite Double Slash (Runtime)";
            chainCombo.selectionWeight = 65;
            chainCombo.cooldown = 1.6f;
            chainCombo.minTriggerRange = 0.2f;
            chainCombo.maxTriggerRange = 2.8f;
            chainCombo.canBeParried = true;
            chainCombo.parryStaggerBonus = 40f;
            chainCombo.baseDamage = definition.AttackDamage;
            chainCombo.swings.Add(new AttackSwingStep
            {
                deliveryType = AttackDeliveryType.Melee,
                telegraphDuration = 0.45f,
                startupDuration = 0.10f,
                executionDuration = 0.10f,
                recoveryDuration = 0.22f,
                telegraphShape = TelegraphShape.Cone,
                range = 2.4f,
                angle = 100f,
                staggerDamage = 18f,
                lungeDistance = 0.75f,
                lungeDuration = 0.10f
            });
            chainCombo.swings.Add(new AttackSwingStep
            {
                deliveryType = AttackDeliveryType.Melee,
                telegraphDuration = 0.32f,
                startupDuration = 0.10f,
                executionDuration = 0.12f,
                recoveryDuration = 0.65f,
                telegraphShape = TelegraphShape.Cone,
                range = 2.6f,
                angle = 115f,
                damageMultiplier = 1.2f,
                staggerDamage = 24f,
                lungeDistance = 0.95f,
                lungeDuration = 0.12f
            });

            // Pola 2: Heavy Nova Slam (Stage 2+ atau Chain Follow-up)
            EnemyAttackPatternSO novaSlam = ScriptableObject.CreateInstance<EnemyAttackPatternSO>();
            novaSlam.patternName = "Shatter Nova (Runtime)";
            novaSlam.selectionWeight = 45;
            novaSlam.cooldown = 3.0f;
            novaSlam.minTriggerRange = 0.2f;
            novaSlam.maxTriggerRange = 3.6f;
            novaSlam.canBeParried = false;
            novaSlam.baseDamage = definition.AttackDamage * 1.3f;
            novaSlam.repositionAfterAttack = true;
            novaSlam.repositionDistance = 2.2f;
            novaSlam.swings.Add(new AttackSwingStep
            {
                deliveryType = AttackDeliveryType.Melee,
                telegraphDuration = 0.72f,
                startupDuration = 0.14f,
                executionDuration = 0.15f,
                recoveryDuration = 0.85f,
                telegraphShape = TelegraphShape.Circle,
                range = 3.4f,
                staggerDamage = 35f
            });

            // Sambungkan Double Slash -> Shatter Nova di Stage 2+!
            chainCombo.chainFollowUpPattern = novaSlam;
            chainCombo.chainFollowUpChance = 0.75f;
            chainCombo.chainRequiredPhase = 2;

            _runtimeFallbackPatterns.Add(chainCombo);
            _runtimeFallbackPatterns.Add(novaSlam);
        }
        else if (definition.Archetype == EnemyArchetype.Controller)
        {
            EnemyAttackPatternSO hazard = ScriptableObject.CreateInstance<EnemyAttackPatternSO>();
            hazard.patternName = "Summon Hazard (Runtime)";
            hazard.selectionWeight = 60;
            hazard.cooldown = 3.5f;
            hazard.minTriggerRange = 1.5f;
            hazard.maxTriggerRange = definition.AttackRange + 2.0f;
            hazard.canBeParried = false;
            hazard.baseDamage = definition.AttackDamage;
            hazard.repositionAfterAttack = true;
            hazard.repositionDistance = 2.2f;
            hazard.swings.Add(new AttackSwingStep
            {
                deliveryType = AttackDeliveryType.TargetedZone,
                telegraphDuration = 0.75f,
                startupDuration = 0.12f,
                executionDuration = 0.10f,
                recoveryDuration = 0.55f,
                telegraphShape = TelegraphShape.TargetedZone,
                zoneRadius = 2.1f,
                zonePersistDuration = 3.2f,
                zoneTickInterval = 0.75f,
                trackPlayerDuringTelegraph = true
            });
            _runtimeFallbackPatterns.Add(hazard);

            EnemyAttackPatternSO empower = ScriptableObject.CreateInstance<EnemyAttackPatternSO>();
            empower.patternName = "Empower Allies (Runtime)";
            empower.selectionWeight = 40;
            empower.cooldown = 6.0f;
            empower.minTriggerRange = 1.0f;
            empower.maxTriggerRange = 14.0f;
            empower.canBeParried = false;
            empower.swings.Add(new AttackSwingStep
            {
                deliveryType = AttackDeliveryType.SupportEmpower,
                telegraphDuration = 0.65f,
                startupDuration = 0.10f,
                executionDuration = 0.10f,
                recoveryDuration = 0.50f,
                telegraphShape = TelegraphShape.TargetedZone,
                empowerRadius = 8.0f,
                empowerDuration = 5.0f,
                empowerSpeedMultiplier = 1.25f,
                empowerDamageMultiplier = 1.20f
            });
            _runtimeFallbackPatterns.Add(empower);
        }
        else
        {
            bool isSkirmisher = definition.Archetype == EnemyArchetype.Skirmisher;
            EnemyAttackPatternSO slash = ScriptableObject.CreateInstance<EnemyAttackPatternSO>();
            slash.patternName = isSkirmisher ? "Quick Cross Slash (Runtime)" : "Basic Slash (Runtime)";
            slash.selectionWeight = 100;
            slash.cooldown = definition.AttackCooldown;
            slash.minTriggerRange = 0.2f;
            slash.maxTriggerRange = definition.AttackRange + 0.45f;
            slash.canBeParried = true;
            slash.parryStaggerBonus = 35f;
            slash.baseDamage = definition.AttackDamage;
            slash.repositionAfterAttack = isSkirmisher;
            slash.repositionDistance = isSkirmisher ? 2.1f : 1.4f;
            slash.swings.Add(new AttackSwingStep
            {
                deliveryType = AttackDeliveryType.Melee,
                telegraphDuration = isSkirmisher ? 0.36f : 0.45f,
                startupDuration = 0.09f,
                executionDuration = 0.10f,
                recoveryDuration = isSkirmisher ? 0.45f : 0.55f,
                telegraphShape = TelegraphShape.Cone,
                range = definition.AttackRange,
                angle = isSkirmisher ? 80f : 95f,
                damageMultiplier = 1f,
                staggerDamage = 15f,
                lungeDistance = isSkirmisher ? 0.65f : 0.45f,
                lungeDuration = 0.10f,
                lockRotationOnStartup = true
            });
            _runtimeFallbackPatterns.Add(slash);
        }
    }

    private void FindPlayer()
    {
        if (_playerTarget != null) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _playerTarget = playerObj.transform;
    }

    private void Update()
    {
        if (currentState == EnemyBrainState.Dead || definition == null)
            return;

        if (_playerTarget == null)
        {
            FindPlayer();
            if (_playerTarget == null) return;
        }

        float dt = Time.deltaTime;
        TickCooldowns(dt);
        EvaluateBossStageTransitions();
        UpdateGuardState();
        UpdateDebugInfo();

        switch (currentState)
        {
            case EnemyBrainState.Idle:
                UpdateIdle();
                break;
            case EnemyBrainState.Approach:
                UpdateApproach(dt);
                break;
            case EnemyBrainState.Positioning:
                UpdatePositioning(dt);
                break;
            case EnemyBrainState.Telegraph:
                UpdateTelegraph(dt);
                break;
            case EnemyBrainState.Startup:
                UpdateStartup(dt);
                break;
            case EnemyBrainState.Execution:
                UpdateExecution(dt);
                break;
            case EnemyBrainState.Recovery:
                UpdateRecovery(dt);
                break;
            case EnemyBrainState.Staggered:
                UpdateStaggered(dt);
                break;
            case EnemyBrainState.Reposition:
                UpdateReposition(dt);
                break;
        }
    }

    private void EvaluateBossStageTransitions()
    {
        if (!IsEliteOrBoss || currentState == EnemyBrainState.Dead || currentState == EnemyBrainState.Staggered)
            return;

        float hpRatio = GetNormalizedHealth();

        // 1. Jika ada konfigurasi BossStages di EnemyDefinitionSO
        if (definition.BossStages != null && definition.BossStages.Count > 0)
        {
            int targetStageIndex = currentStage - 2; // Stage 2 berada di index 0
            int nextIndex = currentStage - 1;

            if (nextIndex < definition.BossStages.Count)
            {
                BossStageConfig nextCfg = definition.BossStages[nextIndex];
                if (nextCfg != null)
                {
                    bool hpTriggered = hpRatio <= nextCfg.hpThresholdNormalized;
                    bool staggerTriggered = nextCfg.staggerBreaksRequired > 0 && staggerBreakCount >= nextCfg.staggerBreaksRequired;

                    if (hpTriggered || staggerTriggered)
                    {
                        EnterBossStage(currentStage + 1, nextCfg);
                    }
                }
            }
            return;
        }

        // 2. Fallback otomatis untuk Elite/Boss tanpa daftar BossStages manual (masuk Stage 2 saat HP <= 55% atau setelah 1x Stagger Break)
        if (currentStage == 1 && (hpRatio <= 0.55f || staggerBreakCount >= 1))
        {
            BossStageConfig runtimeEnrage = new BossStageConfig
            {
                stageName = "Enraged Phase",
                moveSpeedMultiplier = 1.22f,
                cooldownMultiplier = 0.72f,
                decisionDelayMultiplier = 0.65f,
                damageMultiplier = 1.18f
            };
            EnterBossStage(2, runtimeEnrage);
        }
    }

    private void EnterBossStage(int newStage, BossStageConfig cfg)
    {
        currentStage = newStage;
        _stageMoveSpeedMult = cfg.moveSpeedMultiplier;
        _stageCooldownMult = cfg.cooldownMultiplier;
        _stageDecisionMult = cfg.decisionDelayMultiplier;
        _stageDamageMult = cfg.damageMultiplier;

        // Reset cooldown pola agar pola serangan baru segera tersedia
        var keys = new List<EnemyAttackPatternSO>(_patternCooldowns.Keys);
        foreach (var k in keys)
            _patternCooldowns[k] = 0f;

        if (cfg.stageTransitionPattern != null && cfg.stageTransitionPattern.swings.Count > 0)
        {
            _attackController.AbortAttack();
            _movement.CancelLunge();
            CommitPattern(cfg.stageTransitionPattern);
        }
    }

    private void TickCooldowns(float dt)
    {
        if (_empowerTimer > 0f)
            _empowerTimer = Mathf.Max(0f, _empowerTimer - dt);

        if (_sideDashCooldownTimer > 0f)
            _sideDashCooldownTimer = Mathf.Max(0f, _sideDashCooldownTimer - dt);

        if (_patternCooldowns.Count == 0) return;

        float cdStep = dt * (_empowerTimer > 0f ? 1.25f : 1f);

        var keys = new List<EnemyAttackPatternSO>(_patternCooldowns.Keys);
        foreach (var key in keys)
        {
            if (_patternCooldowns[key] > 0f)
                _patternCooldowns[key] = Mathf.Max(0f, _patternCooldowns[key] - cdStep);
        }
    }

    private void UpdateGuardState()
    {
        if (_stagger == null) return;

        bool hasGuard = definition.HasFrontalGuard || _modGrantGuard;
        if (!hasGuard)
        {
            _stagger.IsGuardActive = false;
            return;
        }

        bool canGuard = currentState == EnemyBrainState.Idle ||
                        currentState == EnemyBrainState.Approach ||
                        currentState == EnemyBrainState.Positioning;

        _stagger.IsGuardActive = canGuard;
    }

    private void UpdateDebugInfo()
    {
        isEmpoweredDebug = _empowerTimer > 0f;
        isGuardActiveDebug = _stagger != null && _stagger.IsGuardActive;

        if (CombatDirector.Instance != null)
        {
            currentRoleDebug = CombatDirector.Instance.GetAssignedRole(this);
            currentSectorDebug = CombatDirector.Instance.GetAssignedSector(this);
        }
    }

    private float DistanceToPlayer()
    {
        Vector3 diff = _playerTarget.position - transform.position;
        diff.y = 0f;
        return diff.magnitude;
    }

    private bool HasClearLineOfSightToPlayer()
    {
        if (!definition.RequireLineOfSight) return true;

        Vector3 origin = transform.position + Vector3.up * 0.9f;
        Vector3 target = _playerTarget.position + Vector3.up * 0.9f;
        Vector3 dir = target - origin;
        float dist = dir.magnitude;
        if (dist <= 0.2f) return true;

        int mask = definition.ObstacleLayerMask.value != 0
            ? definition.ObstacleLayerMask.value
            : (1 << 9);

        return !Physics.Raycast(origin, dir.normalized, dist, mask, QueryTriggerInteraction.Ignore);
    }

    private bool IsInPlayerFrontArc()
    {
        Vector3 playerForward = _playerTarget.forward;
        playerForward.y = 0f;
        if (playerForward.sqrMagnitude < 0.001f) return false;

        Vector3 toEnemy = transform.position - _playerTarget.position;
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude < 0.001f) return true;

        float angle = Vector3.Angle(playerForward.normalized, toEnemy.normalized);
        return angle <= definition.PlayerFacingAvoidAngle;
    }

    private bool IsFlankerArchetype()
    {
        return definition.Archetype == EnemyArchetype.Skirmisher ||
               definition.PreferredZone == PreferredCombatZone.Flank ||
               definition.PreferredZone == PreferredCombatZone.Rear ||
               _modGrantSideDash;
    }

    private bool IsRangedArchetype()
    {
        return definition.PreferredZone == PreferredCombatZone.Ranged ||
               definition.Archetype == EnemyArchetype.Ranged ||
               definition.Archetype == EnemyArchetype.Ranger ||
               definition.Archetype == EnemyArchetype.Controller;
    }

    private void ApplySpacing(float dt)
    {
        if (CombatDirector.Instance == null) return;
        _movement.ApplyNeighborSeparation(
            CombatDirector.Instance.ActiveEnemies,
            definition.SeparationRadius,
            definition.SeparationStrength,
            dt
        );
    }

    private void ChangeState(EnemyBrainState nextState)
    {
        currentState = nextState;
        _stateTimer = 0f;
        _isSideDashReposition = false;

        switch (nextState)
        {
            case EnemyBrainState.Idle:
                _movement.Stop();
                break;

            case EnemyBrainState.Positioning:
                _movement.Stop();
                float rawDelay = Random.Range(definition.AttackDecisionDelay.x, definition.AttackDecisionDelay.y);
                _decisionTimer = rawDelay * _stageDecisionMult * _modDecisionMult;
                _strafeSwitchTimer = Random.Range(0.7f, 1.3f);
                break;

            case EnemyBrainState.Telegraph:
            {
                AttackSwingStep step = _selectedPattern.swings[_currentSwingIndex];
                _stateDuration = step.telegraphDuration;
                _movement.Stop();
                _attackController.BeginTelegraph(_selectedPattern, step, _playerTarget);
                break;
            }

            case EnemyBrainState.Startup:
            {
                AttackSwingStep step = _selectedPattern.swings[_currentSwingIndex];
                _stateDuration = step.startupDuration;
                if (step.lockRotationOnStartup)
                    _movement.FacePositionImmediate(_playerTarget.position);

                _attackController.BeginStartup();
                _movement.PerformLunge(transform.forward, step.lungeDistance, step.lungeDuration);
                break;
            }

            case EnemyBrainState.Execution:
            {
                AttackSwingStep step = _selectedPattern.swings[_currentSwingIndex];
                _stateDuration = step.executionDuration;
                _attackController.BeginExecution();
                break;
            }

            case EnemyBrainState.Recovery:
            {
                AttackSwingStep step = _selectedPattern.swings[_currentSwingIndex];
                _stateDuration = step.recoveryDuration;
                _attackController.EndExecution();
                break;
            }

            case EnemyBrainState.Staggered:
                _attackController.AbortAttack();
                _movement.CancelLunge();
                _movement.Stop();
                ReleaseDirectorSlot();
                _stateDuration = definition.StaggerPunishDuration;
                break;

            case EnemyBrainState.Reposition:
                ReleaseDirectorSlot();
                float dist = _selectedPattern != null ? _selectedPattern.repositionDistance : 2.0f;
                _repositionDestination = _movement.GetRepositionPoint(_playerTarget.position, dist);
                _stateDuration = 0.85f;
                _movement.MoveTo(_repositionDestination, definition.RepositionSpeed * RepositionSpeedMultiplier);
                break;

            case EnemyBrainState.Dead:
                _attackController.AbortAttack();
                _movement.CancelLunge();
                _movement.Stop();
                ReleaseDirectorSlot();
                break;
        }
    }

    private void TriggerSideDashReposition()
    {
        ReleaseDirectorSlot();
        currentState = EnemyBrainState.Reposition;
        _stateTimer = 0f;
        _isSideDashReposition = true;
        _sideDashCooldownTimer = definition.SideDashCooldown;
        _strafeSign = -_strafeSign;

        float desiredDist = Mathf.Max(definition.IdealCombatDistance, definition.AttackRange);
        _repositionDestination = _movement.GetFlankDodgePoint(
            _playerTarget.position,
            desiredDist,
            _strafeSign,
            definition.SideDashDistance
        );

        Vector3 dashVec = _repositionDestination - transform.position;
        dashVec.y = 0f;

        _stateDuration = definition.SideDashDuration + 0.24f;
        _movement.PerformLunge(dashVec, dashVec.magnitude, definition.SideDashDuration);
    }

    private void UpdateIdle()
    {
        if (DistanceToPlayer() <= definition.DetectRange)
            ChangeState(EnemyBrainState.Approach);
    }

    private void UpdateApproach(float dt)
    {
        float dist = DistanceToPlayer();
        _movement.FacePosition(_playerTarget.position, definition.RotationSpeed);
        ApplySpacing(dt);

        if (IsRangedArchetype() && definition.MinSafeDistance > 0f && dist <= definition.MinSafeDistance)
        {
            ChangeState(EnemyBrainState.Positioning);
            return;
        }

        float desiredDist = Mathf.Max(definition.IdealCombatDistance, definition.AttackRange);
        Vector3 tacticalTarget = CombatDirector.Instance != null
            ? CombatDirector.Instance.GetTacticalMoveTarget(this, _playerTarget, desiredDist)
            : _playerTarget.position;

        float distToSlot = Vector3.Distance(transform.position, tacticalTarget);

        if (dist <= desiredDist + definition.SpacingTolerance && distToSlot <= 0.95f)
        {
            ChangeState(EnemyBrainState.Positioning);
            return;
        }

        _movement.MoveTo(tacticalTarget, definition.MoveSpeed * SpeedMultiplier);
    }

    private void UpdatePositioning(float dt)
    {
        float dist = DistanceToPlayer();
        _movement.FacePosition(_playerTarget.position, definition.RotationSpeed);
        ApplySpacing(dt);

        float maxStayDist = Mathf.Max(definition.IdealCombatDistance, definition.AttackRange) + definition.SpacingTolerance + 0.6f;
        if (dist > maxStayDist)
        {
            ChangeState(EnemyBrainState.Approach);
            return;
        }

        _strafeSwitchTimer -= dt;
        if (_strafeSwitchTimer <= 0f)
        {
            _strafeSign = -_strafeSign;
            _strafeSwitchTimer = Random.Range(0.8f, 1.5f);
        }

        if (definition.RequireLineOfSight && !HasClearLineOfSightToPlayer())
        {
            Vector3 sightPoint = _movement.GetLateralSightlinePoint(_playerTarget.position, _strafeSign, 2.5f);
            _movement.MoveTo(sightPoint, definition.StrafeSpeed * SpeedMultiplier);
            return;
        }

        if (IsRangedArchetype() && definition.MinSafeDistance > 0f && dist < definition.MinSafeDistance)
        {
            EnemyAttackPatternSO closePattern = SelectAvailablePattern(dist);
            if (closePattern != null && (CombatDirector.Instance == null || CombatDirector.Instance.TryRequestAttackSlot(this)))
            {
                CommitPattern(closePattern);
                return;
            }

            ChangeState(EnemyBrainState.Reposition);
            return;
        }

        float desiredDist = Mathf.Max(definition.IdealCombatDistance, definition.AttackRange);
        Vector3 slotTarget = CombatDirector.Instance != null
            ? CombatDirector.Instance.GetTacticalMoveTarget(this, _playerTarget, desiredDist)
            : transform.position;

        Vector3 toPlayer = (_playerTarget.position - transform.position).normalized;
        Vector3 lateral = Vector3.Cross(Vector3.up, toPlayer) * (_strafeSign * 0.45f);
        _movement.MoveTo(slotTarget + lateral, definition.StrafeSpeed * 0.65f * SpeedMultiplier);

        _decisionTimer -= dt;
        if (_decisionTimer > 0f) return;

        float effectiveFlankChance = Mathf.Clamp01(definition.FlankRepositionChance + _modBonusFlankChance);
        if (IsFlankerArchetype() && IsInPlayerFrontArc() && Random.value < effectiveFlankChance)
        {
            if ((definition.CanSideDash || _modGrantSideDash) && _sideDashCooldownTimer <= 0f)
            {
                TriggerSideDashReposition();
                return;
            }

            if (CombatDirector.Instance != null && CombatDirector.Instance.TryPickAlternativeFlankSector(this, _playerTarget))
            {
                ChangeState(EnemyBrainState.Approach);
                return;
            }
        }

        EnemyAttackPatternSO candidate = SelectAvailablePattern(dist);
        if (candidate == null)
        {
            _decisionTimer = 0.15f;
            return;
        }

        if (CombatDirector.Instance != null && !CombatDirector.Instance.TryRequestAttackSlot(this))
        {
            if (IsFlankerArchetype() && CombatDirector.Instance.TryPickAlternativeFlankSector(this, _playerTarget))
            {
                ChangeState(EnemyBrainState.Approach);
                return;
            }

            _decisionTimer = 0.22f;
            return;
        }

        CommitPattern(candidate);
    }

    private void CommitPattern(EnemyAttackPatternSO pattern)
    {
        _selectedPattern = pattern;
        _currentSwingIndex = 0;
        currentPatternDebug = _selectedPattern.patternName;

        if (!_patternCooldowns.ContainsKey(_selectedPattern))
            _patternCooldowns.Add(_selectedPattern, 0f);

        _patternCooldowns[_selectedPattern] = _selectedPattern.cooldown * _stageCooldownMult * _modCooldownMult;
        ChangeState(EnemyBrainState.Telegraph);
    }

    private void UpdateTelegraph(float dt)
    {
        AttackSwingStep step = _selectedPattern.swings[_currentSwingIndex];

        if (!step.lockRotationOnStartup || _stateTimer < _stateDuration * 0.75f)
            _movement.FacePosition(_playerTarget.position, definition.RotationSpeed * 0.85f);

        _stateTimer += dt;
        float progress = _stateDuration > 0f ? _stateTimer / _stateDuration : 1f;
        _attackController.UpdateTelegraphProgress(progress);

        if (_stateTimer >= _stateDuration)
            ChangeState(EnemyBrainState.Startup);
    }

    private void UpdateStartup(float dt)
    {
        _stateTimer += dt;
        if (_stateTimer >= _stateDuration)
            ChangeState(EnemyBrainState.Execution);
    }

    private void UpdateExecution(float dt)
    {
        _stateTimer += dt;
        if (_stateTimer >= _stateDuration)
            ChangeState(EnemyBrainState.Recovery);
    }

    private void UpdateRecovery(float dt)
    {
        _stateTimer += dt;
        if (_stateTimer < _stateDuration) return;

        // 1. Lanjutkan swing berikutnya dalam pattern yang sama
        if (_selectedPattern != null && _currentSwingIndex + 1 < _selectedPattern.swings.Count)
        {
            _currentSwingIndex++;
            ChangeState(EnemyBrainState.Telegraph);
            return;
        }

        // 2. Phase 4 Attack Chain: sambung langsung ke follow-up pattern jika memenuhi syarat fase & jarak
        if (_selectedPattern != null &&
            _selectedPattern.chainFollowUpPattern != null &&
            _selectedPattern.chainFollowUpPattern.swings.Count > 0 &&
            currentStage >= _selectedPattern.chainRequiredPhase)
        {
            float dist = DistanceToPlayer();
            EnemyAttackPatternSO nextChain = _selectedPattern.chainFollowUpPattern;
            float effectiveChainChance = Mathf.Clamp01(_selectedPattern.chainFollowUpChance + _modBonusChainChance);

            if (dist >= nextChain.minTriggerRange &&
                dist <= nextChain.maxTriggerRange &&
                Random.value < effectiveChainChance)
            {
                CommitPattern(nextChain);
                return;
            }
        }

        bool shouldReposition = (_selectedPattern != null && _selectedPattern.repositionAfterAttack) || _modForceReposition;
        ReleaseDirectorSlot();

        if (shouldReposition)
            ChangeState(EnemyBrainState.Reposition);
        else
            ChangeState(EnemyBrainState.Approach);
    }

    private void UpdateStaggered(float dt)
    {
        _stateTimer += dt;
        if (_stateTimer >= _stateDuration)
        {
            if (_stagger != null)
            {
                _stagger.ResetStagger();
                ApplyDefinitionStats(); // Perbarui MaxStagger sesuai StaggerGrowthPerBreak
            }

            ChangeState(EnemyBrainState.Approach);
        }
    }

    private void UpdateReposition(float dt)
    {
        _movement.FacePosition(_playerTarget.position, definition.RotationSpeed);
        if (!_isSideDashReposition)
            ApplySpacing(dt);

        _stateTimer += dt;

        float remaining = Vector3.Distance(transform.position, _repositionDestination);
        if ((remaining <= 0.35f && !_movement.IsLunging) || _stateTimer >= _stateDuration)
        {
            if (_isSideDashReposition)
                ChangeState(EnemyBrainState.Positioning);
            else
                ChangeState(EnemyBrainState.Approach);
        }
    }

    private EnemyAttackPatternSO SelectAvailablePattern(float currentDistance)
    {
        int totalWeight = 0;
        List<EnemyAttackPatternSO> validPatterns = new List<EnemyAttackPatternSO>();

        foreach (var pattern in _activePatterns)
        {
            if (pattern == null || pattern.swings == null || pattern.swings.Count == 0)
                continue;

            if (currentStage < pattern.minBossPhase || currentStage > pattern.maxBossPhase)
                continue;

            if (_patternCooldowns.TryGetValue(pattern, out float cd) && cd > 0f)
                continue;

            if (currentDistance < pattern.minTriggerRange || currentDistance > pattern.maxTriggerRange)
                continue;

            if (pattern.swings[0].deliveryType == AttackDeliveryType.SupportEmpower && !HasNearbyAllies(pattern.swings[0].empowerRadius))
                continue;

            validPatterns.Add(pattern);
            totalWeight += pattern.selectionWeight;
        }

        if (validPatterns.Count == 0) return null;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var pattern in validPatterns)
        {
            cumulative += pattern.selectionWeight;
            if (roll < cumulative)
                return pattern;
        }

        return validPatterns[0];
    }

    private bool HasNearbyAllies(float radius)
    {
        if (CombatDirector.Instance == null) return false;
        IReadOnlyList<EnemyBrain> enemies = CombatDirector.Instance.ActiveEnemies;
        float radiusSqr = radius * radius;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyBrain other = enemies[i];
            if (other != null && other.gameObject != gameObject && other.CurrentState != EnemyBrainState.Dead)
            {
                if ((other.transform.position - transform.position).sqrMagnitude <= radiusSqr)
                    return true;
            }
        }
        return false;
    }

    private void HandleParried(float staggerBonus)
    {
        if (currentState == EnemyBrainState.Dead) return;

        if (_stagger != null)
        {
            _stagger.Add(staggerBonus, true);
            if (currentState == EnemyBrainState.Staggered)
                return;
        }

        _attackController.AbortAttack();
        _movement.CancelLunge();
        ReleaseDirectorSlot();
        _selectedPattern = null;
        _stateDuration = 0.75f;
        currentState = EnemyBrainState.Recovery;
        _stateTimer = 0f;
    }

    private void HandleStaggerBroken()
    {
        if (currentState == EnemyBrainState.Dead) return;
        staggerBreakCount++;
        ChangeState(EnemyBrainState.Staggered);
    }

    private void HandleDied()
    {
        ChangeState(EnemyBrainState.Dead);
    }

    private void ReleaseDirectorSlot()
    {
        if (CombatDirector.Instance != null)
            CombatDirector.Instance.ReleaseAttackSlot(this);
    }

    private void CacheHealthReflection()
    {
        if (_health == null) return;
        var t = _health.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        _healthCurField = t.GetField("Current", flags) ?? t.GetField("CurrentHealth", flags) ?? t.GetField("currentHealth", flags);
        _healthMaxField = t.GetField("Max", flags) ?? t.GetField("MaxHealth", flags) ?? t.GetField("maxHealth", flags);
        _healthCurProp = t.GetProperty("Current", flags) ?? t.GetProperty("CurrentHealth", flags);
        _healthMaxProp = t.GetProperty("Max", flags) ?? t.GetProperty("MaxHealth", flags);
    }

    private float GetNormalizedHealth()
    {
        if (_health == null) return 1f;

        float cur = -1f;
        float max = -1f;

        if (_healthCurField != null) cur = System.Convert.ToSingle(_healthCurField.GetValue(_health));
        else if (_healthCurProp != null) cur = System.Convert.ToSingle(_healthCurProp.GetValue(_health));

        if (_healthMaxField != null) max = System.Convert.ToSingle(_healthMaxField.GetValue(_health));
        else if (_healthMaxProp != null) max = System.Convert.ToSingle(_healthMaxProp.GetValue(_health));

        if (cur >= 0f && max > 0f)
            return Mathf.Clamp01(cur / max);

        return 1f;
    }
}