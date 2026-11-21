Berikut implementasi lengkap **Phase 4 (Elite & Boss Multi-Stage, Attack Chains, Stagger Escalation, dan Enemy Modifiers)** yang menyatu langsung dengan sistem Phase 1–3 tanpa mengubah *wiring* scene yang sudah ada.

---

### 1. `EnemyModifierSO.cs` (File Baru)

Sistem modifier roguelike yang mengubah **perilaku taktis dan ritme serangan** musuh (`Swift`, `Armored`, `Aggressive`, `Empowered`, `Tactical`), lengkap dengan `CreateRuntime(...)` agar bisa langsung dipakai tanpa harus membuat asset satu per satu terlebih dahulu.

```csharp
using UnityEngine;

public enum EnemyModifierType
{
    Swift,
    Armored,
    Aggressive,
    Empowered,
    Tactical,
    Custom
}

[CreateAssetMenu(fileName = "NewEnemyModifier", menuName = "TLS/Enemy/Enemy Modifier")]
public class EnemyModifierSO : ScriptableObject
{
    [Header("Identitas Modifier")]
    public string ModifierName = "Swift";
    public EnemyModifierType ModifierType = EnemyModifierType.Swift;

    [Header("Multiplier Ritme & Statistik")]
    [Min(0.5f)] public float MoveSpeedMultiplier = 1.0f;
    [Min(0.5f)] public float RepositionSpeedMultiplier = 1.0f;
    [Min(0.3f)] public float AttackCooldownMultiplier = 1.0f;
    [Min(0.3f)] public float DecisionDelayMultiplier = 1.0f;
    [Min(0.5f)] public float DamageMultiplier = 1.0f;

    [Header("Perubahan Perilaku Taktis")]
    [Range(0f, 0.6f)] public float BonusStaggerResistance = 0f;
    public bool GrantFrontalGuard = false;
    public bool GrantSideDash = false;
    [Range(0f, 1f)] public float BonusFlankRepositionChance = 0f;
    public bool ForceRepositionAfterAttack = false;
    [Range(0f, 0.5f)] public float BonusChainChance = 0f;

    [Header("Bonus Attack Pattern (Opsional)")]
    public EnemyAttackPatternSO ExtraAttackPattern;

    public static EnemyModifierSO CreateRuntime(EnemyModifierType type)
    {
        EnemyModifierSO mod = CreateInstance<EnemyModifierSO>();
        mod.ModifierType = type;
        mod.ModifierName = type.ToString();

        switch (type)
        {
            case EnemyModifierType.Swift:
                mod.MoveSpeedMultiplier = 1.30f;
                mod.RepositionSpeedMultiplier = 1.40f;
                mod.DecisionDelayMultiplier = 0.75f;
                mod.GrantSideDash = true;
                break;

            case EnemyModifierType.Armored:
                mod.MoveSpeedMultiplier = 0.90f;
                mod.BonusStaggerResistance = 0.30f;
                mod.GrantFrontalGuard = true;
                break;

            case EnemyModifierType.Aggressive:
                mod.MoveSpeedMultiplier = 1.15f;
                mod.AttackCooldownMultiplier = 0.65f;
                mod.DecisionDelayMultiplier = 0.55f;
                mod.DamageMultiplier = 1.15f;
                mod.BonusChainChance = 0.30f;
                break;

            case EnemyModifierType.Empowered:
                mod.MoveSpeedMultiplier = 1.15f;
                mod.DamageMultiplier = 1.25f;
                mod.AttackCooldownMultiplier = 0.85f;
                break;

            case EnemyModifierType.Tactical:
                mod.RepositionSpeedMultiplier = 1.25f;
                mod.GrantSideDash = true;
                mod.BonusFlankRepositionChance = 0.35f;
                mod.ForceRepositionAfterAttack = true;
                break;
        }

        return mod;
    }
}

```

---

### 2. `EnemyAttackPatternSO.cs`

Menambahkan **Phase Gate** (`minBossPhase` / `maxBossPhase`) agar Elite & Boss membuka pola serangan baru di fase berikutnya, serta **Attack Chain** (`chainFollowUpPattern`) sehingga satu pola serangan dapat langsung menyambung ke pola serangan lain tanpa jeda kembali ke *Approach*.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public enum TelegraphShape
{
    Cone,
    Circle,
    Line,
    TargetedZone
}

public enum AttackDeliveryType
{
    Melee,
    Projectile,
    TargetedZone,
    SupportEmpower
}

[Serializable]
public class AttackSwingStep
{
    [Header("Tipe Eksekusi (Phase 3)")]
    public AttackDeliveryType deliveryType = AttackDeliveryType.Melee;

    [Header("Timing (Detik)")]
    [Tooltip("Durasi peringatan visual di lantai sebelum serangan dimulai")]
    public float telegraphDuration = 0.45f;
    [Tooltip("Durasi ancang-ancang animasi setelah telegraph selesai")]
    public float startupDuration = 0.12f;
    [Tooltip("Durasi hitbox aktif / pelepasan serangan")]
    public float executionDuration = 0.10f;
    [Tooltip("Jeda setelah tahap ini selesai")]
    public float recoveryDuration = 0.35f;

    [Header("Area & Telegraph")]
    public TelegraphShape telegraphShape = TelegraphShape.Cone;
    public float range = 2.2f;
    [Range(15f, 360f)]
    public float angle = 95f;
    public float width = 1.2f;

    [Header("Combat & Movement")]
    public float damageMultiplier = 1.0f;
    public float staggerDamage = 15f;
    [Tooltip("Dorongan maju (+) atau mundur/retreat (-) saat startup")]
    public float lungeDistance = 0.6f;
    public float lungeDuration = 0.12f;
    public bool lockRotationOnStartup = true;

    [Header("Phase 3 — Projectile Settings (Ranger)")]
    [Min(1)] public int projectileCount = 1;
    [Range(0f, 120f)] public float projectileSpreadAngle = 18f;

    [Header("Phase 3 — Targeted Zone / Hazard (Ranger & Controller)")]
    public float zoneRadius = 2.0f;
    [Tooltip("0 = ledakan instan saat execution (Ground Mark), >0 = hazard bertahan di arena")]
    public float zonePersistDuration = 0f;
    public float zoneTickInterval = 0.65f;
    public bool trackPlayerDuringTelegraph = true;
    [Tooltip("Opsional: prefab VFX hazard. Jika kosong, memakai visual ground zone otomatis")]
    public GameObject customHazardPrefab;

    [Header("Phase 3 — Support Empower (Controller)")]
    public float empowerRadius = 7.5f;
    public float empowerDuration = 5.0f;
    public float empowerSpeedMultiplier = 1.25f;
    public float empowerDamageMultiplier = 1.20f;
}

[CreateAssetMenu(fileName = "NewAttackPattern", menuName = "TheLastSasak/Combat/Enemy Attack Pattern")]
public class EnemyAttackPatternSO : ScriptableObject
{
    [Header("Identitas Pattern")]
    public string patternName = "Quick Slash";
    [Range(1, 100)]
    public int selectionWeight = 50;
    public float cooldown = 1.5f;

    [Header("Syarat Jarak (Approach)")]
    public float minTriggerRange = 0.5f;
    public float maxTriggerRange = 2.4f;

    [Header("Aturan Interaksi Player")]
    public bool canBeParried = true;
    public float parryStaggerBonus = 35f;
    public float baseDamage = 12f;

    [Header("Rangkaian Tahap Serangan (1 = Single, 2+ = Combo/Burst)")]
    public List<AttackSwingStep> swings = new List<AttackSwingStep>();

    [Header("Post-Attack Reposition")]
    public bool repositionAfterAttack = false;
    public float repositionDistance = 1.8f;

    [Header("Phase 4 — Multi-Stage Gate & Attack Chain")]
    [Min(1)] public int minBossPhase = 1;
    [Min(1)] public int maxBossPhase = 99;
    [Tooltip("Opsional: pattern lanjutan yang langsung dieksekusi setelah recovery tanpa kembali ke Approach")]
    public EnemyAttackPatternSO chainFollowUpPattern;
    [Range(0f, 1f)] public float chainFollowUpChance = 0f;
    [Min(1)] public int chainRequiredPhase = 1;
}

```

---

### 3. `EnemyDefinitionSO.cs`

Menambahkan archetype `Boss`, konfigurasi `BossStageConfig` (transisi tahap berdasarkan ambang batas HP atau jumlah *Stagger Break*), serta eskalasi ketahanan stagger setelah musuh bangkit dari *Staggered*.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyArchetype
{
    // Legacy archetypes (dipakai EnemySpawnPoint & RoomEncounterController)
    Melee,
    Ranged,
    Elite,

    // Phase 1-4 archetypes
    Vanguard,
    Skirmisher,
    Ranger,
    Bulwark,
    Controller,
    Boss
}

public enum PreferredCombatZone
{
    Front,
    Flank,
    Rear,
    Ranged
}

[Serializable]
public class BossStageConfig
{
    public string stageName = "Stage 2 — Enraged";
    [Tooltip("Masuk ke stage ini jika rasio HP <= nilai ini (0.6 = 60% HP)")]
    [Range(0.05f, 0.95f)] public float hpThresholdNormalized = 0.6f;
    [Tooltip("Atau masuk ke stage ini setelah terkena Stagger Break sebanyak N kali (0 = abaikan)")]
    public int staggerBreaksRequired = 0;

    [Header("Perubahan Tempo Stage")]
    [Min(0.6f)] public float moveSpeedMultiplier = 1.2f;
    [Min(0.4f)] public float cooldownMultiplier = 0.75f;
    [Min(0.4f)] public float decisionDelayMultiplier = 0.7f;
    [Min(0.8f)] public float damageMultiplier = 1.15f;

    [Header("Pola Serangan Baru di Stage Ini")]
    public EnemyAttackPatternSO stageTransitionPattern;
    public List<EnemyAttackPatternSO> unlockedPatterns = new List<EnemyAttackPatternSO>();
}

[CreateAssetMenu(fileName = "NewEnemyDefinition", menuName = "TLS/Enemy/Enemy Definition")]
public class EnemyDefinitionSO : ScriptableObject
{
    [Header("Identitas & Statistik Dasar (Legacy + Core)")]
    public string DisplayName = "Enemy";
    public EnemyArchetype Archetype = EnemyArchetype.Vanguard;
    public float MaxHealth = 40f;
    public float MoveSpeed = 3.5f;
    public float DetectRange = 12f;
    public float AttackRange = 1.8f;
    public float AttackDamage = 12f;
    public float AttackCooldown = 1.2f;
    public int ExpReward = 10;
    public int SoulsReward = 2;
    public AttackDefinitionSO Attack;

    [Header("Phase 1 — Positioning & Movement")]
    public float IdealCombatDistance = 2.0f;
    public float SpacingTolerance = 0.45f;
    public float StrafeSpeed = 2.2f;
    public float RepositionSpeed = 3.6f;
    public float RotationSpeed = 12f;

    [Header("Phase 1 — Stagger & Decision Rhythm")]
    public float StaggerPunishDuration = 2.0f;
    public Vector2 AttackDecisionDelay = new Vector2(0.25f, 0.55f);

    [Header("Phase 2 — Tactical Flanking & Spacing")]
    public PreferredCombatZone PreferredZone = PreferredCombatZone.Front;
    [Range(30f, 160f)] public float FlankAngle = 95f;
    public bool AllowRearFlank = false;
    [Range(15f, 90f)] public float PlayerFacingAvoidAngle = 45f;
    [Range(0f, 1f)] public float FlankRepositionChance = 0.65f;

    [Header("Phase 2 — Skirmisher Side Dash & Separation")]
    public bool CanSideDash = false;
    public float SideDashDistance = 2.2f;
    public float SideDashDuration = 0.18f;
    public float SideDashCooldown = 2.6f;
    public float SeparationRadius = 1.35f;
    public float SeparationStrength = 2.4f;

    [Header("Phase 3 — Ranger Spacing & Line of Sight")]
    [Tooltip("Jika player mendekat lebih dari jarak ini, Ranger akan mundur / Retreat Shot")]
    public float MinSafeDistance = 0f;
    public bool RequireLineOfSight = false;
    public LayerMask ObstacleLayerMask;

    [Header("Phase 3 — Bulwark Heavy Defense")]
    [Range(0f, 0.85f)] public float BaseStaggerResistance = 0f;
    public bool HasFrontalGuard = false;
    [Range(45f, 160f)] public float FrontalGuardAngle = 110f;

    [Header("Phase 4 — Elite & Boss Multi-Stage")]
    public bool IsEliteOrBoss = false;
    [Tooltip("Setiap kali pulih dari Staggered, MaxStagger bertambah sebesar persentase ini agar boss tidak mudah di-stunlock terus-menerus")]
    [Range(0f, 0.5f)] public float StaggerGrowthPerBreak = 0.20f;
    public List<BossStageConfig> BossStages = new List<BossStageConfig>();

    [Header("Phase 1-4 — Attack Patterns")]
    public List<EnemyAttackPatternSO> AttackPatterns = new List<EnemyAttackPatternSO>();

    public static EnemyDefinitionSO CreateRuntime(string name, EnemyArchetype type)
    {
        EnemyDefinitionSO so = CreateInstance<EnemyDefinitionSO>();
        so.name = name;
        so.DisplayName = name;
        so.Archetype = type;

        if (type == EnemyArchetype.Skirmisher)
        {
            so.MaxHealth = 32f;
            so.MoveSpeed = 4.3f;
            so.StrafeSpeed = 3.1f;
            so.RepositionSpeed = 4.6f;
            so.AttackRange = 1.7f;
            so.IdealCombatDistance = 2.1f;
            so.AttackDamage = 10f;
            so.PreferredZone = PreferredCombatZone.Flank;
            so.FlankAngle = 100f;
            so.AllowRearFlank = true;
            so.CanSideDash = true;
            so.AttackDecisionDelay = new Vector2(0.20f, 0.42f);
        }
        else if (type == EnemyArchetype.Ranged || type == EnemyArchetype.Ranger)
        {
            so.AttackRange = 8.5f;
            so.IdealCombatDistance = 7.5f;
            so.MinSafeDistance = 3.8f;
            so.RequireLineOfSight = true;
            so.DetectRange = 14f;
            so.AttackDamage = 9f;
            so.MaxHealth = 30f;
            so.MoveSpeed = 3.4f;
            so.StrafeSpeed = 2.8f;
            so.RepositionSpeed = 4.0f;
            so.PreferredZone = PreferredCombatZone.Ranged;
        }
        else if (type == EnemyArchetype.Bulwark)
        {
            so.MaxHealth = 140f;
            so.AttackDamage = 20f;
            so.MoveSpeed = 2.4f;
            so.StrafeSpeed = 1.1f;
            so.RepositionSpeed = 2.2f;
            so.RotationSpeed = 6.5f;
            so.AttackRange = 2.4f;
            so.IdealCombatDistance = 2.2f;
            so.BaseStaggerResistance = 0.35f;
            so.HasFrontalGuard = true;
            so.PreferredZone = PreferredCombatZone.Front;
        }
        else if (type == EnemyArchetype.Elite || type == EnemyArchetype.Boss)
        {
            bool isBoss = type == EnemyArchetype.Boss;
            so.IsEliteOrBoss = true;
            so.MaxHealth = isBoss ? 320f : 180f;
            so.AttackDamage = isBoss ? 22f : 18f;
            so.MoveSpeed = 3.4f;
            so.StrafeSpeed = 2.4f;
            so.RepositionSpeed = 3.8f;
            so.AttackRange = 2.5f;
            so.IdealCombatDistance = 2.3f;
            so.BaseStaggerResistance = 0.25f;
            so.StaggerPunishDuration = isBoss ? 2.4f : 2.1f;
            so.StaggerGrowthPerBreak = 0.20f;
            so.PreferredZone = PreferredCombatZone.Front;
        }
        else if (type == EnemyArchetype.Controller)
        {
            so.MaxHealth = 36f;
            so.AttackDamage = 10f;
            so.MoveSpeed = 3.1f;
            so.AttackRange = 9.5f;
            so.IdealCombatDistance = 8.5f;
            so.MinSafeDistance = 4.2f;
            so.DetectRange = 15f;
            so.PreferredZone = PreferredCombatZone.Ranged;
        }

        return so;
    }
}

```

---

### 4. `CombatDirector.cs`

Memberikan prioritas slot serangan bagi **Elite & Boss** (`IsEliteOrBoss`) agar pertarungan boss tetap agresif dan tidak tertahan oleh musuh pendukung biasa.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyTacticalRole
{
    Engage,
    Flank,
    Hold,
    Reposition
}

public enum CombatSector
{
    Front = 0,
    LeftFlank = 1,
    RightFlank = 2,
    Rear = 3,
    RangedOuter = 4
}

public class CombatDirector : MonoBehaviour
{
    public static CombatDirector Instance { get; private set; }

    [Header("Attack Budget (Phase 2-4)")]
    [SerializeField] private int maxConcurrentMeleeAttackers = 1;
    [SerializeField] private int maxConcurrentFlankers = 2;
    [SerializeField] private int maxConcurrentRangedAttackers = 1;

    [Header("Spatial Slot Tuning")]
    [SerializeField] private float slotReassignInterval = 0.25f;
    [SerializeField] private float wideFlankClearance = 2.4f;

    private readonly List<EnemyBrain> _registeredEnemies = new List<EnemyBrain>();
    private readonly HashSet<Component> _activeMeleeAttackers = new HashSet<Component>();
    private readonly HashSet<Component> _activeRangedAttackers = new HashSet<Component>();
    private readonly Dictionary<EnemyBrain, CombatSector> _assignedSectors = new Dictionary<EnemyBrain, CombatSector>();
    private readonly Dictionary<EnemyBrain, EnemyTacticalRole> _assignedRoles = new Dictionary<EnemyBrain, EnemyTacticalRole>();

    private Transform _cachedPlayer;
    private float _reassignTimer;

    public IReadOnlyList<EnemyBrain> ActiveEnemies => _registeredEnemies;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        _reassignTimer -= Time.deltaTime;
        if (_reassignTimer <= 0f)
        {
            _reassignTimer = slotReassignInterval;
            CleanupInvalidEntries();
            ReevaluateGroupCoordination();
        }
    }

    public void RegisterEnemy(EnemyBrain enemy)
    {
        if (enemy == null || _registeredEnemies.Contains(enemy)) return;
        _registeredEnemies.Add(enemy);
        ReevaluateGroupCoordination();
    }

    public void UnregisterEnemy(EnemyBrain enemy)
    {
        if (enemy == null) return;
        _registeredEnemies.Remove(enemy);
        _assignedSectors.Remove(enemy);
        _assignedRoles.Remove(enemy);
        ReleaseAttackSlot(enemy);
    }

    public bool TryRequestAttackSlot(Component requester)
    {
        if (requester == null) return false;

        CleanupInvalidEntries();

        // Elite dan Boss selalu mendapat prioritas serangan agar tidak tertahan oleh minion
        if (requester is EnemyBrain brain && brain.IsEliteOrBoss)
        {
            _assignedRoles[brain] = EnemyTacticalRole.Engage;
            return true;
        }

        bool isRanged = IsRangedRequester(requester);
        HashSet<Component> pool = isRanged ? _activeRangedAttackers : _activeMeleeAttackers;
        int limit = isRanged ? maxConcurrentRangedAttackers : maxConcurrentMeleeAttackers;

        if (pool.Contains(requester))
            return true;

        if (pool.Count >= limit)
            return false;

        pool.Add(requester);

        if (requester is EnemyBrain normalBrain)
            _assignedRoles[normalBrain] = EnemyTacticalRole.Engage;

        return true;
    }

    public void ReleaseAttackSlot(Component requester)
    {
        if (requester == null) return;
        _activeMeleeAttackers.Remove(requester);
        _activeRangedAttackers.Remove(requester);
    }

    // Legacy compatibility untuk EnemyController.cs
    public bool RequestAggro(Component requester) => TryRequestAttackSlot(requester);
    public void ReleaseAggro(Component requester) => ReleaseAttackSlot(requester);
    public void Unregister(Component enemy)
    {
        if (enemy is EnemyBrain brain)
            UnregisterEnemy(brain);
        else
            ReleaseAttackSlot(enemy);
    }

    public EnemyTacticalRole GetAssignedRole(EnemyBrain enemy)
    {
        if (enemy != null && _assignedRoles.TryGetValue(enemy, out EnemyTacticalRole role))
            return role;
        return EnemyTacticalRole.Hold;
    }

    public CombatSector GetAssignedSector(EnemyBrain enemy)
    {
        if (enemy != null && _assignedSectors.TryGetValue(enemy, out CombatSector sector))
            return sector;
        return CombatSector.Front;
    }

    public Vector3 GetTacticalMoveTarget(EnemyBrain enemy, Transform player, float desiredDistance)
    {
        if (enemy == null || player == null)
            return enemy != null ? enemy.transform.position : Vector3.zero;

        _cachedPlayer = player;
        CombatSector sector = GetAssignedSector(enemy);
        EnemyDefinitionSO def = enemy.Definition;

        Vector3 playerPos = player.position;
        Vector3 playerForward = player.forward;
        playerForward.y = 0f;
        if (playerForward.sqrMagnitude < 0.001f) playerForward = Vector3.forward;
        playerForward.Normalize();

        float angleOffset = GetSectorAngle(sector, def);
        Vector3 targetDir = Quaternion.Euler(0f, angleOffset, 0f) * playerForward;

        int sameSectorRank = GetSameSectorRank(enemy, sector);
        if (sameSectorRank > 0)
        {
            float spread = (sameSectorRank % 2 == 1 ? 1f : -1f) * 24f * Mathf.Ceil(sameSectorRank * 0.5f);
            targetDir = Quaternion.Euler(0f, spread, 0f) * targetDir;
        }

        Vector3 desiredPoint = playerPos + targetDir * desiredDistance;

        if (def != null && (def.PreferredZone == PreferredCombatZone.Flank || def.PreferredZone == PreferredCombatZone.Rear))
        {
            Vector3 toEnemy = enemy.transform.position - playerPos;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > 0.05f)
            {
                float signedCurrent = Vector3.SignedAngle(playerForward, toEnemy.normalized, Vector3.up);
                float signedTarget = Vector3.SignedAngle(playerForward, targetDir, Vector3.up);

                if (Mathf.Abs(signedCurrent) < def.PlayerFacingAvoidAngle && Mathf.Abs(signedTarget) > 75f)
                {
                    float sideSign = signedTarget >= 0f ? 1f : -1f;
                    Vector3 wideDir = Quaternion.Euler(0f, sideSign * 85f, 0f) * playerForward;
                    Vector3 arcWaypoint = playerPos + wideDir * Mathf.Max(desiredDistance, wideFlankClearance);

                    if (TrySampleWalkablePoint(arcWaypoint, 2.2f, out Vector3 validArc))
                        return validArc;
                }
            }
        }

        if (TrySampleWalkablePoint(desiredPoint, 2.2f, out Vector3 validPoint))
            return validPoint;

        for (int step = 1; step <= 4; step++)
        {
            float altAngle = step * 35f * (step % 2 == 0 ? -1f : 1f);
            Vector3 altDir = Quaternion.Euler(0f, altAngle, 0f) * targetDir;
            if (TrySampleWalkablePoint(playerPos + altDir * desiredDistance, 2.0f, out Vector3 fallbackPoint))
                return fallbackPoint;
        }

        return playerPos;
    }

    public bool TryPickAlternativeFlankSector(EnemyBrain enemy, Transform player)
    {
        if (enemy == null || player == null) return false;

        CombatSector current = GetAssignedSector(enemy);
        EnemyDefinitionSO def = enemy.Definition;

        CombatSector[] candidates = (def != null && def.AllowRearFlank)
            ? new[] { CombatSector.LeftFlank, CombatSector.RightFlank, CombatSector.Rear }
            : new[] { CombatSector.LeftFlank, CombatSector.RightFlank };

        CombatSector bestSector = current;
        float bestScore = float.MinValue;

        foreach (CombatSector candidate in candidates)
        {
            if (candidate == current) continue;

            float score = ScoreSectorForEnemy(enemy, candidate, player);
            if (score > bestScore)
            {
                bestScore = score;
                bestSector = candidate;
            }
        }

        if (bestSector != current)
        {
            _assignedSectors[enemy] = bestSector;
            _assignedRoles[enemy] = EnemyTacticalRole.Flank;
            return true;
        }

        return false;
    }

    private void ReevaluateGroupCoordination()
    {
        if (_registeredEnemies.Count == 0) return;

        if (_cachedPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _cachedPlayer = playerObj.transform;
        }

        if (_cachedPlayer == null) return;

        int activeFlankers = 0;

        _registeredEnemies.Sort((a, b) =>
        {
            if (a == null) return 1;
            if (b == null) return -1;
            float da = (a.transform.position - _cachedPlayer.position).sqrMagnitude;
            float db = (b.transform.position - _cachedPlayer.position).sqrMagnitude;
            return da.CompareTo(db);
        });

        for (int i = 0; i < _registeredEnemies.Count; i++)
        {
            EnemyBrain enemy = _registeredEnemies[i];
            if (enemy == null || enemy.CurrentState == EnemyBrainState.Dead || enemy.Definition == null)
                continue;

            if (enemy.CurrentState == EnemyBrainState.Telegraph ||
                enemy.CurrentState == EnemyBrainState.Startup ||
                enemy.CurrentState == EnemyBrainState.Execution ||
                enemy.CurrentState == EnemyBrainState.Staggered)
            {
                _assignedRoles[enemy] = EnemyTacticalRole.Engage;
                continue;
            }

            if (enemy.CurrentState == EnemyBrainState.Reposition)
            {
                _assignedRoles[enemy] = EnemyTacticalRole.Reposition;
                continue;
            }

            EnemyDefinitionSO def = enemy.Definition;
            bool isRanged = def.PreferredZone == PreferredCombatZone.Ranged ||
                            def.Archetype == EnemyArchetype.Ranged ||
                            def.Archetype == EnemyArchetype.Ranger ||
                            def.Archetype == EnemyArchetype.Controller;

            if (isRanged)
            {
                _assignedSectors[enemy] = CombatSector.RangedOuter;
                _assignedRoles[enemy] = EnemyTacticalRole.Hold;
                continue;
            }

            bool wantsFlank = (def.PreferredZone == PreferredCombatZone.Flank || def.PreferredZone == PreferredCombatZone.Rear) &&
                              activeFlankers < maxConcurrentFlankers;

            if (wantsFlank)
            {
                CombatSector chosen = ChooseBestFlankSector(enemy, _cachedPlayer);
                _assignedSectors[enemy] = chosen;
                _assignedRoles[enemy] = EnemyTacticalRole.Flank;
                activeFlankers++;
            }
            else
            {
                int frontCount = CountEnemiesInSector(CombatSector.Front, enemy);
                if (frontCount >= 2 && activeFlankers < maxConcurrentFlankers)
                {
                    CombatSector side = ChooseBestFlankSector(enemy, _cachedPlayer);
                    _assignedSectors[enemy] = side;
                    _assignedRoles[enemy] = EnemyTacticalRole.Flank;
                    activeFlankers++;
                }
                else
                {
                    _assignedSectors[enemy] = CombatSector.Front;
                    _assignedRoles[enemy] = (_activeMeleeAttackers.Contains(enemy) || enemy.IsEliteOrBoss)
                        ? EnemyTacticalRole.Engage
                        : EnemyTacticalRole.Hold;
                }
            }
        }
    }

    private CombatSector ChooseBestFlankSector(EnemyBrain enemy, Transform player)
    {
        EnemyDefinitionSO def = enemy.Definition;
        CombatSector[] options = (def != null && def.AllowRearFlank)
            ? new[] { CombatSector.LeftFlank, CombatSector.RightFlank, CombatSector.Rear }
            : new[] { CombatSector.LeftFlank, CombatSector.RightFlank };

        CombatSector best = options[0];
        float bestScore = float.MinValue;

        for (int i = 0; i < options.Length; i++)
        {
            float score = ScoreSectorForEnemy(enemy, options[i], player);
            if (score > bestScore)
            {
                bestScore = score;
                best = options[i];
            }
        }

        return best;
    }

    private float ScoreSectorForEnemy(EnemyBrain enemy, CombatSector sector, Transform player)
    {
        EnemyDefinitionSO def = enemy.Definition;
        float dist = def != null ? def.IdealCombatDistance : 2.1f;

        Vector3 playerForward = player.forward;
        playerForward.y = 0f;
        if (playerForward.sqrMagnitude < 0.001f) playerForward = Vector3.forward;
        playerForward.Normalize();

        float angle = GetSectorAngle(sector, def);
        Vector3 slotPos = player.position + (Quaternion.Euler(0f, angle, 0f) * playerForward * dist);

        int occupants = CountEnemiesInSector(sector, enemy);
        float score = -occupants * 6.0f;

        float travelDist = Vector3.Distance(enemy.transform.position, slotPos);
        score -= travelDist * 0.8f;

        if (sector == CombatSector.Rear)
            score += 2.2f;
        else if (sector == CombatSector.LeftFlank || sector == CombatSector.RightFlank)
            score += 1.5f;

        if (!TrySampleWalkablePoint(slotPos, 1.8f, out _))
            score -= 20f;

        return score;
    }

    private float GetSectorAngle(CombatSector sector, EnemyDefinitionSO def)
    {
        float flank = def != null ? def.FlankAngle : 95f;
        switch (sector)
        {
            case CombatSector.LeftFlank:
                return -flank;
            case CombatSector.RightFlank:
                return flank;
            case CombatSector.Rear:
                return 165f;
            case CombatSector.RangedOuter:
            case CombatSector.Front:
            default:
                return 0f;
        }
    }

    private int CountEnemiesInSector(CombatSector sector, EnemyBrain ignore)
    {
        int count = 0;
        foreach (var kvp in _assignedSectors)
        {
            if (kvp.Key != null && kvp.Key != ignore && kvp.Key.CurrentState != EnemyBrainState.Dead && kvp.Value == sector)
                count++;
        }
        return count;
    }

    private int GetSameSectorRank(EnemyBrain enemy, CombatSector sector)
    {
        int rank = 0;
        for (int i = 0; i < _registeredEnemies.Count; i++)
        {
            EnemyBrain other = _registeredEnemies[i];
            if (other == null || other.CurrentState == EnemyBrainState.Dead) continue;
            if (other == enemy) break;

            if (_assignedSectors.TryGetValue(other, out CombatSector otherSector) && otherSector == sector)
                rank++;
        }
        return rank;
    }

    private bool TrySampleWalkablePoint(Vector3 candidate, float maxRadius, out Vector3 result)
    {
        result = candidate;
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, maxRadius, NavMesh.AllAreas))
            return false;

        result = hit.position;
        return true;
    }

    private bool IsRangedRequester(Component requester)
    {
        if (requester is EnemyBrain brain && brain.Definition != null)
        {
            EnemyArchetype arch = brain.Definition.Archetype;
            return brain.Definition.PreferredZone == PreferredCombatZone.Ranged ||
                   arch == EnemyArchetype.Ranged ||
                   arch == EnemyArchetype.Ranger ||
                   arch == EnemyArchetype.Controller;
        }
        return false;
    }

    private void CleanupInvalidEntries()
    {
        _registeredEnemies.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy || e.CurrentState == EnemyBrainState.Dead);
        _activeMeleeAttackers.RemoveWhere(e => e == null || !e.gameObject.activeInHierarchy);
        _activeRangedAttackers.RemoveWhere(e => e == null || !e.gameObject.activeInHierarchy);
    }
}

```

---

### 5. `EnemyBrain.cs`

Mengintegrasikan seluruh fitur **Phase 4**:

* **Multi-Stage Boss/Elite Escalation:** Memantau rasio HP dan jumlah *Stagger Break* untuk masuk ke Stage 2 / Stage 3, membuka pola serangan baru, mempercepat tempo, serta mengeksekusi *Transition Pattern* (misalnya *Ground Slam* / *Shockwave*) saat pergantian tahap.
* **Attack Chains:** Di akhir *Recovery*, jika pola serangan memiliki `chainFollowUpPattern` dan tahap boss memenuhi syarat, musuh langsung menyambung serangan berikutnya tanpa melepas slot serangan.
* **Stagger Break Escalation:** Saat pulih dari *Staggered*, `MaxStagger` meningkat bertahap sesuai `StaggerGrowthPerBreak`.
* **Enemy Modifiers:** Mendukung daftar modifier di Inspector maupun penambahan modifier dinamis saat runtime (`AddModifier` / `RollRandomModifiers`).

```csharp
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

```

---

### Setup Cepat di Unity Editor untuk Menguji Phase 4 (`CombatTest`)

1. **Aktifkan `Example_Elite` sebagai Multi-Stage Boss/Elite:**
* Pilih object `Example_Elite` di `CombatTest`, aktifkan (`SetActive(true)`), dan pastikan sudah memiliki komponen `EnemyBrain`, `EnemyMovement`, `EnemyAttackController`, `StaggerComponent` (`Max Stagger = 140`), serta `GroundTelegraphView`.
* Buat asset `EnemyDefinitionSO` baru bernama `Boss_Def`:
* **Archetype:** `Boss` | **Is Elite Or Boss:** Centang (`true`)
* **Max Health:** `300` | **Stagger Growth Per Break:** `0.2`
* Tambahkan 1 elemen di **Boss Stages**:
* `Stage Name`: `"Enraged Stage 2"`
* `Hp Threshold Normalized`: `0.55` (masuk Stage 2 saat HP <= 55%)
* `Stagger Breaks Required`: `1` (atau langsung masuk Stage 2 setelah 1x Stagger Break)
* `Move Speed Multiplier`: `1.25` | `Cooldown Multiplier`: `0.7`






2. **Uji Attack Chain & Enemy Modifiers:**
* Buat 2 `EnemyModifierSO` (**Create → TLS → Enemy → Enemy Modifier**):
* `Mod_Aggressive` (`Aggressive`, `Attack Cooldown Multiplier = 0.65`, `Bonus Chain Chance = 0.35`)
* `Mod_Swift` (`Swift`, `Move Speed Multiplier = 1.3`, `Grant Side Dash = true`)


* Drag `Mod_Aggressive` ke daftar **Initial Modifiers** pada `EnemyBrain` milik `Example_Elite` untuk melihat pola serangannya saling menyambung (*Attack Chain*) di Stage 2.



Setelah Phase 4 teruji, kita siap masuk ke **Phase 5 (Roguelike Encounter: Room Composition, Room Modifier, Artefak Gameplay-Changing, dan Encounter Difficulty Progression)**—sebelum masuk ke Phase 5, kirimkan `ArtifactController.cs`, `RoomController.cs`, dan `RoomManager.cs` agar integrasinya presisi.