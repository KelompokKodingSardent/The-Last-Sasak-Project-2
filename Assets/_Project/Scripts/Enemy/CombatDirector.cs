using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyTacticalRole
{
    Engage,     // Mendapat izin menyerang / menekan langsung
    Flank,      // Mencari sudut samping / belakang player
    Hold,       // Menjaga jarak / menunggu giliran di slotnya
    Reposition  // Sedang mundur atau bergeser mencari sudut baru
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

    [Header("Attack Budget (Phase 2)")]
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

        bool isRanged = IsRangedRequester(requester);
        HashSet<Component> pool = isRanged ? _activeRangedAttackers : _activeMeleeAttackers;
        int limit = isRanged ? maxConcurrentRangedAttackers : maxConcurrentMeleeAttackers;

        if (pool.Contains(requester))
            return true;

        if (pool.Count >= limit)
            return false;

        pool.Add(requester);

        if (requester is EnemyBrain brain)
            _assignedRoles[brain] = EnemyTacticalRole.Engage;

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

    /// <summary>
    /// Menghitung titik tujuan taktis di NavMesh berdasarkan sektor yang diberikan ke musuh.
    /// Jika Skirmisher harus menyeberang dari depan player ke sisi belakang/berlawanan,
    /// fungsi ini mengembalikan waypoint memutar (wide-arc) agar tidak menabrak hitbox player.
    /// </summary>
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

        // Tambahkan sedikit spread jika lebih dari satu musuh berada di sektor yang sama
        int sameSectorRank = GetSameSectorRank(enemy, sector);
        if (sameSectorRank > 0)
        {
            float spread = (sameSectorRank % 2 == 1 ? 1f : -1f) * 24f * Mathf.Ceil(sameSectorRank * 0.5f);
            targetDir = Quaternion.Euler(0f, spread, 0f) * targetDir;
        }

        Vector3 desiredPoint = playerPos + targetDir * desiredDistance;

        // Jika Skirmisher ingin ke Rear/Flank seberang sementara posisinya berada di depan player,
        // arahkan dulu ke titik lambung luar (wide-arc waypoint) agar pergerakan terlihat cerdas.
        if (def != null && (def.PreferredZone == PreferredCombatZone.Flank || def.PreferredZone == PreferredCombatZone.Rear))
        {
            Vector3 toEnemy = enemy.transform.position - playerPos;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > 0.05f)
            {
                float signedCurrent = Vector3.SignedAngle(playerForward, toEnemy.normalized, Vector3.up);
                float signedTarget = Vector3.SignedAngle(playerForward, targetDir, Vector3.up);

                // Sedang berada di depan player (|signedCurrent| < 45) dan target ada di sisi belakang/seberang
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

        // Fallback: cari sudut alternatif yang valid di NavMesh
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

        // Urutkan evaluasi: musuh yang paling dekat dengan player mendapat prioritas klaim slot lebih dulu
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

            // Jangan ganggu musuh yang sedang mengeksekusi serangan atau staggered
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
                // Jika slot Front sudah terlalu penuh, alihkan Vanguard berlebih ke sisi samping agar tidak menumpuk
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
                    _assignedRoles[enemy] = _activeMeleeAttackers.Contains(enemy)
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

        // 1. Occupancy penalty (hindari sektor yang sudah diisi musuh lain)
        int occupants = CountEnemiesInSector(sector, enemy);
        float score = -occupants * 6.0f;

        // 2. Distance score (lebih suka sektor yang lebih dekat dengan posisi musuh saat ini)
        float travelDist = Vector3.Distance(enemy.transform.position, slotPos);
        score -= travelDist * 0.8f;

        // 3. Player Facing bonus (sektor di luar pandangan depan player lebih menguntungkan untuk flank)
        if (sector == CombatSector.Rear)
            score += 2.2f;
        else if (sector == CombatSector.LeftFlank || sector == CombatSector.RightFlank)
            score += 1.5f;

        // 4. Navigation check (penalti besar jika titik tidak valid di NavMesh)
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