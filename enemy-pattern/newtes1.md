Berikut implementasi lengkap **Phase 3 (Enemy Variety: Ranger, Bulwark, dan Controller)** yang terintegrasi penuh dengan sistem *CombatDirector*, *GroundTelegraphView*, *ProjectilePool*, dan *StaggerComponent* milikmu.

---

### 1. `EnemyAttackPatternSO.cs`

Memperluas *attack pattern* agar satu struktur data yang sama dapat mengeksekusi serangan **Melee**, **Projectile** (*Focused*, *Burst*, *Retreat Shot*), **TargetedZone** (*Ground Mark*, *Summon Hazard*, *Projectile Rain*), maupun **SupportEmpower** (*Controller Buff*).

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
}

```

---

### 2. `GroundTelegraphView.cs`

Menambahkan dukungan proyeksi **TargetedZone** di titik target arena (untuk *Ground Mark* milik Ranger dan *Summon Hazard / Projectile Rain* milik Controller) serta warna khusus untuk *Support Empower*.

```csharp
using UnityEngine;

public class GroundTelegraphView : MonoBehaviour
{
    [Header("Referensi Child di Scene/Prefab (Wajib Di-assign)")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Transform outerBoundTransform;
    [SerializeField] private Transform fillProgressTransform;
    [SerializeField] private Renderer fillRenderer;

    [Header("Warna Keterbacaan Isometrik")]
    [SerializeField] private Color parryableColor = new Color(1f, 0.25f, 0.15f, 0.35f);
    [SerializeField] private Color heavyColor = new Color(1f, 0.65f, 0.05f, 0.45f);
    [SerializeField] private Color supportColor = new Color(0.25f, 0.85f, 1f, 0.35f);
    [SerializeField] private Color flashReadyColor = new Color(1f, 1f, 1f, 0.75f);

    private MaterialPropertyBlock _propBlock;
    private static readonly int ColorPropId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorPropId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        Hide();
    }

    public void Show(AttackSwingStep step, bool canBeParried)
    {
        Show(step, canBeParried, null);
    }

    public void Show(AttackSwingStep step, bool canBeParried, Vector3? worldOverridePos)
    {
        if (visualRoot == null) return;

        visualRoot.SetActive(true);

        if (step.telegraphShape == TelegraphShape.TargetedZone && worldOverridePos.HasValue)
        {
            Vector3 pos = worldOverridePos.Value;
            visualRoot.transform.position = new Vector3(pos.x, pos.y + 0.03f, pos.z);
            visualRoot.transform.rotation = Quaternion.identity;
        }
        else
        {
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;
        }

        ApplyShapeScale(step);

        if (outerBoundTransform != null && fillProgressTransform != null)
            fillProgressTransform.localPosition = outerBoundTransform.localPosition + new Vector3(0f, 0.005f, 0f);

        SetProgress(0f);

        Color baseCol = step.deliveryType == AttackDeliveryType.SupportEmpower
            ? supportColor
            : (canBeParried ? parryableColor : heavyColor);

        ApplyColor(baseCol);
    }

    public void UpdateWorldPosition(Vector3 worldPos)
    {
        if (visualRoot == null || !visualRoot.activeSelf) return;
        visualRoot.transform.position = new Vector3(worldPos.x, worldPos.y + 0.03f, worldPos.z);
    }

    public void SetProgress(float normalizedProgress)
    {
        if (fillProgressTransform == null) return;

        float t = Mathf.Clamp01(normalizedProgress);
        Vector3 targetScale = outerBoundTransform != null ? outerBoundTransform.localScale : Vector3.one;

        fillProgressTransform.localScale = new Vector3(
            targetScale.x * Mathf.Max(0.05f, t),
            targetScale.y,
            targetScale.z * Mathf.Max(0.05f, t)
        );
    }

    public void TriggerStartupFlash()
    {
        ApplyColor(flashReadyColor);
    }

    public void Hide()
    {
        if (visualRoot != null)
        {
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;
            visualRoot.SetActive(false);
        }
    }

    private void ApplyShapeScale(AttackSwingStep step)
    {
        if (outerBoundTransform == null) return;

        switch (step.telegraphShape)
        {
            case TelegraphShape.TargetedZone:
            {
                float radius = step.deliveryType == AttackDeliveryType.SupportEmpower
                    ? step.empowerRadius
                    : step.zoneRadius;
                float diameter = radius * 2f;
                outerBoundTransform.localScale = new Vector3(diameter, 1f, diameter);
                outerBoundTransform.localPosition = Vector3.zero;
                break;
            }

            case TelegraphShape.Circle:
            {
                float diameter = step.range * 2f;
                outerBoundTransform.localScale = new Vector3(diameter, 1f, diameter);
                outerBoundTransform.localPosition = new Vector3(0f, 0.03f, 0f);
                break;
            }

            case TelegraphShape.Line:
            {
                outerBoundTransform.localScale = new Vector3(step.width, 1f, step.range);
                outerBoundTransform.localPosition = new Vector3(0f, 0.03f, step.range * 0.5f);
                break;
            }

            case TelegraphShape.Cone:
            default:
            {
                float arcWidth = 2f * step.range * Mathf.Sin(step.angle * 0.5f * Mathf.Deg2Rad);
                outerBoundTransform.localScale = new Vector3(Mathf.Max(0.8f, arcWidth), 1f, step.range);
                outerBoundTransform.localPosition = new Vector3(0f, 0.03f, step.range * 0.5f);
                break;
            }
        }
    }

    private void ApplyColor(Color color)
    {
        if (fillRenderer == null) return;

        fillRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(ColorPropId, color);
        _propBlock.SetColor(LegacyColorPropId, color);
        fillRenderer.SetPropertyBlock(_propBlock);
    }
}

```

---

### 3. `StaggerComponent.cs`

Menambahkan **Stagger Resistance** dan **Guard Stance** untuk Bulwark (serangan normal ke arah tameng depan mengalami reduksi stagger drastis, sedangkan sukses *Parry* atau serangan dari sisi samping/belakang tetap menembus pertahanan).

```csharp
using UnityEngine;

public class StaggerComponent : MonoBehaviour
{
    public float MaxStagger = 100f;
    public float Current;

    [Header("Phase 3 — Stagger Resistance & Bulwark Guard")]
    [Range(0f, 0.85f)] public float StaggerResistance = 0f;
    [Range(0.05f, 1f)] public float GuardStaggerMultiplier = 0.25f;
    public bool IsGuardActive { get; set; }

    public System.Action OnStaggered;

    public void Add(float amount)
    {
        Add(amount, false);
    }

    public void Add(float amount, bool bypassResistance)
    {
        if (Current >= MaxStagger || amount <= 0f)
            return;

        float finalAmount = amount;
        if (!bypassResistance)
        {
            finalAmount *= (1f - Mathf.Clamp01(StaggerResistance));
            if (IsGuardActive)
                finalAmount *= Mathf.Clamp(GuardStaggerMultiplier, 0.05f, 1f);
        }

        Current += finalAmount;
        if (Current >= MaxStagger)
        {
            Current = MaxStagger;
            IsGuardActive = false;
            OnStaggered?.Invoke();
            EnemyBrain brain = GetComponent<EnemyBrain>();
            brain?.ResetBrain();
        }
    }

    public void ResetStagger()
    {
        Current = 0f;
    }
}

```

---

### 4. `EnemyDefinitionSO.cs`

Menambahkan parameter jarak aman & *Line of Sight* untuk Ranger, serta *Stagger Resistance* & *Frontal Guard* untuk Bulwark, lengkap dengan fallback runtime untuk `Ranger`, `Bulwark`, dan `Controller`.

```csharp
using System.Collections.Generic;
using UnityEngine;

public enum EnemyArchetype
{
    // Legacy archetypes (dipakai EnemySpawnPoint & RoomEncounterController)
    Melee,
    Ranged,
    Elite,

    // Phase 1+ archetypes
    Vanguard,
    Skirmisher,
    Ranger,
    Bulwark,
    Controller
}

public enum PreferredCombatZone
{
    Front,
    Flank,
    Rear,
    Ranged
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

    [Header("Phase 1-3 — Attack Patterns")]
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
        else if (type == EnemyArchetype.Elite || type == EnemyArchetype.Bulwark)
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

### 5. `EnemyMovement.cs`

Mendukung **negative `lungeDistance**` (sehingga *Retreat Shot* milik Ranger otomatis mendorong mundur di atas NavMesh sambil tetap membidik player) serta `GetLateralSightlinePoint(...)` untuk mencari garis pandang di sekitar pilar.

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Coroutine _lungeRoutine;

    public bool IsMoving => _agent != null && _agent.enabled && _agent.velocity.sqrMagnitude > 0.05f;
    public bool IsLunging => _lungeRoutine != null;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false; // Rotasi dikontrol manual agar responsif saat telegraph
    }

    public void MoveTo(Vector3 destination, float speed)
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = false;
        _agent.speed = speed;
        _agent.SetDestination(destination);
    }

    public void Stop()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    public void FacePosition(Vector3 worldTarget, float rotationSpeed)
    {
        Vector3 dir = worldTarget - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    public void FacePositionImmediate(Vector3 worldTarget)
    {
        Vector3 dir = worldTarget - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    /// <summary>
    /// Mendukung jarak positif (lunge maju) maupun negatif (retreat backstep).
    /// </summary>
    public void PerformLunge(Vector3 direction, float distance, float duration)
    {
        if (Mathf.Abs(distance) <= 0.01f || duration <= 0.01f) return;

        if (_lungeRoutine != null)
            StopCoroutine(_lungeRoutine);

        Vector3 moveDir = direction.normalized * Mathf.Sign(distance);
        _lungeRoutine = StartCoroutine(LungeRoutine(moveDir, Mathf.Abs(distance), duration));
    }

    public void CancelLunge()
    {
        if (_lungeRoutine != null)
        {
            StopCoroutine(_lungeRoutine);
            _lungeRoutine = null;
        }
    }

    private IEnumerator LungeRoutine(Vector3 dir, float distance, float duration)
    {
        Stop();
        float elapsed = 0f;
        float speed = distance / duration;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.Move(dir * (speed * dt));
            }
            elapsed += dt;
            yield return null;
        }

        _lungeRoutine = null;
    }

    public void ApplyNeighborSeparation(IReadOnlyList<EnemyBrain> neighbors, float radius, float strength, float dt)
    {
        if (neighbors == null || radius <= 0.01f || strength <= 0.01f) return;
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh || IsLunging) return;

        Vector3 push = Vector3.zero;
        float radiusSqr = radius * radius;
        Vector3 myPos = transform.position;

        for (int i = 0; i < neighbors.Count; i++)
        {
            EnemyBrain other = neighbors[i];
            if (other == null || other.gameObject == gameObject || other.CurrentState == EnemyBrainState.Dead)
                continue;

            Vector3 diff = myPos - other.transform.position;
            diff.y = 0f;
            float sqrDist = diff.sqrMagnitude;

            if (sqrDist < 0.0001f)
            {
                push += transform.right * 0.5f;
                continue;
            }

            if (sqrDist < radiusSqr)
            {
                float dist = Mathf.Sqrt(sqrDist);
                float weight = (radius - dist) / radius;
                push += (diff / dist) * weight;
            }
        }

        if (push.sqrMagnitude > 0.0001f)
        {
            Vector3 clampedPush = Vector3.ClampMagnitude(push, 1.25f) * (strength * dt);
            _agent.Move(clampedPush);
        }
    }

    public Vector3 GetRepositionPoint(Vector3 playerPos, float retreatDistance)
    {
        Vector3 away = (transform.position - playerPos);
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -transform.forward;
        away.Normalize();

        float sideAngle = Random.Range(-42f, 42f);
        Vector3 candidate = transform.position + (Quaternion.Euler(0f, sideAngle, 0f) * away * retreatDistance);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    public Vector3 GetFlankDodgePoint(Vector3 playerPos, float preferredDistance, float sideSign, float dashDistance)
    {
        Vector3 toPlayer = playerPos - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) toPlayer = transform.forward;
        toPlayer.Normalize();

        Vector3 lateral = Vector3.Cross(Vector3.up, toPlayer) * Mathf.Sign(sideSign);
        float currentDist = Vector3.Distance(transform.position, playerPos);
        float radialBias = currentDist > preferredDistance ? 0.35f : -0.25f;

        Vector3 dashDir = (lateral + toPlayer * radialBias).normalized;
        Vector3 candidate = transform.position + dashDir * dashDistance;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.8f, NavMesh.AllAreas))
            return hit.position;

        Vector3 oppositeCandidate = transform.position - lateral * dashDistance;
        if (NavMesh.SamplePosition(oppositeCandidate, out NavMeshHit oppHit, 1.8f, NavMesh.AllAreas))
            return oppHit.position;

        return transform.position;
    }

    /// <summary>
    /// Mencari titik geser menyamping di NavMesh ketika garis tembak Ranger terhalang pilar/obstacle.
    /// </summary>
    public Vector3 GetLateralSightlinePoint(Vector3 playerPos, float sideSign, float stepDistance)
    {
        Vector3 toPlayer = playerPos - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) toPlayer = transform.forward;
        toPlayer.Normalize();

        Vector3 lateral = Vector3.Cross(Vector3.up, toPlayer) * Mathf.Sign(sideSign);
        Vector3 candidate = transform.position + lateral * stepDistance;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.2f, NavMesh.AllAreas))
            return hit.position;

        Vector3 opposite = transform.position - lateral * stepDistance;
        if (NavMesh.SamplePosition(opposite, out NavMeshHit oppHit, 2.2f, NavMesh.AllAreas))
            return oppHit.position;

        return transform.position;
    }
}

```

---

### 6. `ArenaHazardZone.cs` (File Baru)

Komponen ringan untuk hazard lantai sementara (dipakai *Summon Hazard* milik Controller). Dapat menggunakan prefab custom atau otomatis membuat visual lingkaran permukaan arena tanpa memerlukan konfigurasi scene tambahan.

```csharp
using UnityEngine;

public class ArenaHazardZone : MonoBehaviour
{
    private GameObject _owner;
    private float _radius;
    private float _damagePerTick;
    private float _duration;
    private float _tickInterval;
    private LayerMask _playerLayerMask;
    private int _attackId;

    private float _elapsed;
    private float _tickTimer;
    private readonly Collider[] _hitBuffer = new Collider[4];

    public static ArenaHazardZone Create(
        Vector3 position,
        float radius,
        float damagePerTick,
        float duration,
        float tickInterval,
        GameObject owner,
        LayerMask playerMask,
        GameObject customPrefab = null)
    {
        GameObject root;
        if (customPrefab != null)
        {
            root = Instantiate(customPrefab, position, Quaternion.identity);
        }
        else
        {
            root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "RuntimeHazardZone";
            Collider col = root.GetComponent<Collider>();
            if (col != null) Destroy(col);

            root.transform.position = new Vector3(position.x, position.y + 0.02f, position.z);
            root.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

            Renderer rend = root.GetComponent<Renderer>();
            if (rend != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                rend.GetPropertyBlock(block);
                Color hazardCol = new Color(0.65f, 0.15f, 0.9f, 0.55f);
                block.SetColor("_BaseColor", hazardCol);
                block.SetColor("_Color", hazardCol);
                rend.SetPropertyBlock(block);
            }
        }

        ArenaHazardZone zone = root.GetComponent<ArenaHazardZone>();
        if (zone == null)
            zone = root.AddComponent<ArenaHazardZone>();

        zone.Initialize(radius, damagePerTick, duration, tickInterval, owner, playerMask);
        return zone;
    }

    private void Initialize(float radius, float damage, float duration, float tickInterval, GameObject owner, LayerMask mask)
    {
        _radius = Mathf.Max(0.4f, radius);
        _damagePerTick = damage;
        _duration = Mathf.Max(0.2f, duration);
        _tickInterval = Mathf.Max(0.2f, tickInterval);
        _owner = owner;
        _playerLayerMask = mask;
        _tickTimer = 0.05f; // Tick awal segera setelah muncul
        _attackId = CombatSystem.Instance != null ? CombatSystem.Instance.NextAttackId() : 0;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        _elapsed += dt;
        if (_elapsed >= _duration)
        {
            Destroy(gameObject);
            return;
        }

        _tickTimer -= dt;
        if (_tickTimer <= 0f)
        {
            _tickTimer = _tickInterval;
            ApplyZoneTick();
        }
    }

    private void ApplyZoneTick()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, _radius, _hitBuffer, _playerLayerMask);
        for (int i = 0; i < count; i++)
        {
            Collider col = _hitBuffer[i];
            if (col == null) continue;

            DamageInfo info = new DamageInfo
            {
                Amount = _damagePerTick,
                Instigator = _owner != null ? _owner : gameObject,
                HitPoint = transform,
                CanBeParried = false,
                Stagger = 8f,
                AttackId = _attackId
            };

            if (CombatSystem.Instance != null)
            {
                CombatSystem.Instance.ApplyDamage(col, info);
            }
            else
            {
                IDamageable dmg = col.GetComponentInParent<IDamageable>();
                dmg?.TakeDamage(info);
            }
        }
    }
}

```

---

### 7. `EnemyAttackController.cs`

Mengeksekusi seluruh tipe serangan Phase 1–3 (`Melee`, `Projectile`, `TargetedZone`, dan `SupportEmpower`) melalui satu antarmuka bersih yang dipanggil oleh `EnemyBrain`.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackController : MonoBehaviour, IParryable
{
    [Header("Referensi Komponen (Assign di Inspector)")]
    [SerializeField] private GroundTelegraphView telegraphView;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private LayerMask playerLayerMask;

    private readonly Collider[] _hitBuffer = new Collider[8];
    private readonly HashSet<GameObject> _hitThisSwing = new HashSet<GameObject>();

    private EnemyAttackPatternSO _activePattern;
    private AttackSwingStep _activeStep;
    private ProjectilePool _projectilePool;
    private Transform _currentTarget;
    private Vector3 _lockedZonePosition;
    private bool _isExecutingHitbox;
    private bool _parriedThisSwing;
    private int _currentAttackId;

    public bool IsHitboxActive => _isExecutingHitbox;

    public event Action<float> OnAttackParried;

    private void Awake()
    {
        if (attackOrigin == null)
            attackOrigin = transform;
    }

    public void SetProjectilePool(ProjectilePool pool)
    {
        _projectilePool = pool;
    }

    public void BeginTelegraph(EnemyAttackPatternSO pattern, AttackSwingStep step, Transform target = null)
    {
        _activePattern = pattern;
        _activeStep = step;
        _currentTarget = target;
        _isExecutingHitbox = false;
        _parriedThisSwing = false;

        if (step.telegraphShape == TelegraphShape.TargetedZone)
        {
            if (step.deliveryType == AttackDeliveryType.SupportEmpower)
                _lockedZonePosition = transform.position;
            else
                _lockedZonePosition = target != null ? target.position : transform.position + transform.forward * 4f;

            if (telegraphView != null)
                telegraphView.Show(step, pattern.canBeParried, _lockedZonePosition);
        }
        else
        {
            if (telegraphView != null)
                telegraphView.Show(step, pattern.canBeParried);
        }
    }

    public void UpdateTelegraphProgress(float normalizedProgress)
    {
        if (_activeStep != null &&
            _activeStep.telegraphShape == TelegraphShape.TargetedZone &&
            _activeStep.deliveryType != AttackDeliveryType.SupportEmpower &&
            _activeStep.trackPlayerDuringTelegraph &&
            _currentTarget != null &&
            normalizedProgress < 0.8f) // Kunci posisi di 20% akhir agar player punya jendela dodge yang adil
        {
            _lockedZonePosition = _currentTarget.position;
            if (telegraphView != null)
                telegraphView.UpdateWorldPosition(_lockedZonePosition);
        }

        if (telegraphView != null)
            telegraphView.SetProgress(normalizedProgress);
    }

    public void BeginStartup()
    {
        if (telegraphView != null)
            telegraphView.TriggerStartupFlash();
    }

    public void BeginExecution()
    {
        _hitThisSwing.Clear();
        _parriedThisSwing = false;
        _currentAttackId = CombatSystem.Instance != null ? CombatSystem.Instance.NextAttackId() : 0;

        if (telegraphView != null)
            telegraphView.Hide();

        if (_activePattern == null || _activeStep == null) return;

        switch (_activeStep.deliveryType)
        {
            case AttackDeliveryType.Melee:
                _isExecutingHitbox = true;
                break;

            case AttackDeliveryType.Projectile:
                _isExecutingHitbox = false;
                FireProjectiles();
                break;

            case AttackDeliveryType.TargetedZone:
                _isExecutingHitbox = false;
                ExecuteTargetedZone();
                break;

            case AttackDeliveryType.SupportEmpower:
                _isExecutingHitbox = false;
                ExecuteSupportEmpower();
                break;
        }
    }

    public void EndExecution()
    {
        _isExecutingHitbox = false;
        _hitThisSwing.Clear();
    }

    public void AbortAttack()
    {
        _isExecutingHitbox = false;
        _hitThisSwing.Clear();
        _activePattern = null;
        _activeStep = null;

        if (telegraphView != null)
            telegraphView.Hide();
    }

    public void OnParried(Transform parrier)
    {
        NotifyParried();
    }

    public void NotifyParried()
    {
        if (_parriedThisSwing) return;
        _parriedThisSwing = true;

        float staggerBonus = _activePattern != null ? _activePattern.parryStaggerBonus : 35f;
        AbortAttack();
        OnAttackParried?.Invoke(staggerBonus);
    }

    private void FixedUpdate()
    {
        if (!_isExecutingHitbox || _activePattern == null || _activeStep == null)
            return;

        if (_activeStep.deliveryType == AttackDeliveryType.Melee)
            EvaluateMeleeHitbox();
    }

    private void FireProjectiles()
    {
        if (_projectilePool == null)
            _projectilePool = FindFirstObjectByType<ProjectilePool>();

        if (_projectilePool == null) return;

        Vector3 spawnPos = attackOrigin.position + Vector3.up * 0.9f;
        Vector3 baseDir = transform.forward;

        if (_currentTarget != null)
        {
            Vector3 toTarget = _currentTarget.position - attackOrigin.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                baseDir = toTarget.normalized;
        }

        int count = Mathf.Max(1, _activeStep.projectileCount);
        float finalDamage = GetScaledDamage();

        if (count == 1)
        {
            _projectilePool.Spawn(spawnPos, baseDir, gameObject, false, finalDamage);
            return;
        }

        float totalSpread = _activeStep.projectileSpreadAngle;
        float startAngle = -totalSpread * 0.5f;
        float stepAngle = count > 1 ? totalSpread / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + stepAngle * i;
            Vector3 shotDir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            _projectilePool.Spawn(spawnPos, shotDir, gameObject, false, finalDamage);
        }
    }

    private void ExecuteTargetedZone()
    {
        float finalDamage = GetScaledDamage();

        if (_activeStep.zonePersistDuration > 0.05f)
        {
            // Controller — Summon Hazard (area berbahaya bertahan beberapa detik)
            ArenaHazardZone.Create(
                _lockedZonePosition,
                _activeStep.zoneRadius,
                finalDamage,
                _activeStep.zonePersistDuration,
                _activeStep.zoneTickInterval,
                gameObject,
                playerLayerMask,
                _activeStep.customHazardPrefab
            );
            return;
        }

        // Ranger Ground Mark / Controller Projectile Rain (ledakan area instan)
        int count = Physics.OverlapSphereNonAlloc(_lockedZonePosition, _activeStep.zoneRadius, _hitBuffer, playerLayerMask);
        for (int i = 0; i < count; i++)
        {
            Collider col = _hitBuffer[i];
            if (col == null) continue;

            GameObject targetObj = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
            if (_hitThisSwing.Contains(targetObj)) continue;

            _hitThisSwing.Add(targetObj);
            DeliverDamage(col, finalDamage, false);
        }
    }

    private void ExecuteSupportEmpower()
    {
        if (CombatDirector.Instance == null) return;

        IReadOnlyList<EnemyBrain> enemies = CombatDirector.Instance.ActiveEnemies;
        float radiusSqr = _activeStep.empowerRadius * _activeStep.empowerRadius;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyBrain ally = enemies[i];
            if (ally == null || ally.gameObject == gameObject || ally.CurrentState == EnemyBrainState.Dead)
                continue;

            float sqrDist = (ally.transform.position - transform.position).sqrMagnitude;
            if (sqrDist <= radiusSqr)
            {
                ally.ApplyEmpower(
                    _activeStep.empowerDuration,
                    _activeStep.empowerSpeedMultiplier,
                    _activeStep.empowerDamageMultiplier
                );
            }
        }
    }

    private float GetScaledDamage()
    {
        float mult = 1f;
        EnemyBrain brain = GetComponent<EnemyBrain>();
        if (brain != null)
            mult = brain.DamageMultiplier;

        return _activePattern.baseDamage * _activeStep.damageMultiplier * mult;
    }

    private void EvaluateMeleeHitbox()
    {
        Vector3 origin = attackOrigin.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        int count = Physics.OverlapSphereNonAlloc(origin, _activeStep.range, _hitBuffer, playerLayerMask);
        for (int i = 0; i < count; i++)
        {
            Collider col = _hitBuffer[i];
            if (col == null) continue;

            GameObject targetObj = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
            if (_hitThisSwing.Contains(targetObj)) continue;

            if (!IsInsideShape(origin, forward, targetObj.transform.position, _activeStep))
                continue;

            _hitThisSwing.Add(targetObj);
            DeliverDamage(col, GetScaledDamage(), _activePattern.canBeParried);
        }
    }

    private bool IsInsideShape(Vector3 origin, Vector3 forward, Vector3 targetPos, AttackSwingStep step)
    {
        Vector3 toTarget = targetPos - origin;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        if (dist > step.range) return false;

        switch (step.telegraphShape)
        {
            case TelegraphShape.Circle:
            case TelegraphShape.TargetedZone:
                return true;

            case TelegraphShape.Cone:
                if (dist < 0.01f) return true;
                float angle = Vector3.Angle(forward, toTarget.normalized);
                return angle <= step.angle * 0.5f;

            case TelegraphShape.Line:
                float forwardDot = Vector3.Dot(forward, toTarget);
                if (forwardDot < 0f || forwardDot > step.range) return false;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                float lateralOffset = Mathf.Abs(Vector3.Dot(right, toTarget));
                return lateralOffset <= step.width * 0.5f;

            default:
                return true;
        }
    }

    private void DeliverDamage(Collider targetCol, float finalDamage, bool canBeParried)
    {
        DamageInfo info = new DamageInfo
        {
            Amount = finalDamage,
            Instigator = gameObject,
            HitPoint = attackOrigin,
            CanBeParried = canBeParried,
            Stagger = _activeStep.staggerDamage,
            AttackId = _currentAttackId
        };

        DamageResult result;
        if (CombatSystem.Instance != null)
        {
            result = CombatSystem.Instance.ApplyDamage(targetCol, info);
        }
        else
        {
            IDamageable damageable = targetCol.GetComponentInParent<IDamageable>();
            result = damageable != null ? damageable.TakeDamage(info) : DamageResult.Ignored;
        }

        if (result.Outcome == DamageOutcome.Parried)
        {
            NotifyParried();
        }
    }
}

```

---

### 8. `EnemyBrain.cs`

Menghubungkan seluruh perilaku Phase 1–3:

* **Ranger**: menjaga jarak aman (`MinSafeDistance`), mengecek *Line of Sight* terhadap obstacle sebelum menembak (geser lateral jika terhalang pilar), serta memilih pola *Retreat Shot* saat player terlalu dekat.
* **Bulwark**: mengaktifkan *Guard Stance* saat tidak sedang menyerang/staggered, dan menonaktifkannya saat *Telegraph/Startup/Execution/Staggered* sehingga player punya jendela serangan balik yang jelas.
* **Controller**: menjaga jarak belakang, menyebarkan *Empower* buff ke musuh lain, dan menjatuhkan *Hazard* ke arena.

```csharp
using System.Collections.Generic;
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

    [Header("Debug State (Read-Only)")]
    [SerializeField] private EnemyBrainState currentState = EnemyBrainState.Idle;
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

    public EnemyBrainState CurrentState => currentState;
    public float SpeedMultiplier => _empowerTimer > 0f ? _empowerSpeedMultiplier : 1f;
    public float DamageMultiplier => _empowerTimer > 0f ? _empowerDamageMultiplier : 1f;

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
        ApplyDefinitionStats();
        InitializePatterns();

        if (CombatDirector.Instance != null)
            CombatDirector.Instance.RegisterEnemy(this);

        ChangeState(EnemyBrainState.Idle);
    }

    public void Bind(EnemyDefinitionSO newDefinition)
    {
        definition = newDefinition;
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

    public void ApplyEmpower(float duration, float speedMult, float damageMult)
    {
        _empowerTimer = Mathf.Max(_empowerTimer, duration);
        _empowerSpeedMultiplier = Mathf.Max(_empowerSpeedMultiplier, speedMult);
        _empowerDamageMultiplier = Mathf.Max(_empowerDamageMultiplier, damageMult);
    }

    private void ApplyDefinitionStats()
    {
        if (definition == null || _stagger == null) return;
        _stagger.StaggerResistance = definition.BaseStaggerResistance;
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
                if (p != null && p.swings != null && p.swings.Count > 0 && !_patternCooldowns.ContainsKey(p))
                {
                    _activePatterns.Add(p);
                    _patternCooldowns.Add(p, 0f);
                }
            }
        }

        // Fallback otomatis sesuai archetype jika AttackPatterns belum diisi di Inspector
        if (_activePatterns.Count == 0)
        {
            BuildRuntimeFallbackPatterns();
            foreach (var p in _runtimeFallbackPatterns)
            {
                _activePatterns.Add(p);
                _patternCooldowns.Add(p, 0f);
            }
        }
    }

    private void BuildRuntimeFallbackPatterns()
    {
        _runtimeFallbackPatterns.Clear();

        if (definition.Archetype == EnemyArchetype.Ranged || definition.Archetype == EnemyArchetype.Ranger)
        {
            // 1. Focused Shot
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
                projectileCount = 1,
                lungeDistance = 0f
            });
            _runtimeFallbackPatterns.Add(focused);

            // 2. Retreat Shot (ketika player mendekat)
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
        else if (definition.Archetype == EnemyArchetype.Bulwark || definition.Archetype == EnemyArchetype.Elite)
        {
            // 1. Shield Bash (Cone)
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

            // 2. Ground Slam (Circle - Unparryable)
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
                staggerDamage = 35f,
                lungeDistance = 0f
            });
            _runtimeFallbackPatterns.Add(slam);
        }
        else if (definition.Archetype == EnemyArchetype.Controller)
        {
            // 1. Summon Hazard (Targeted Zone)
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

            // 2. Empower Allies (Support)
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
            // Vanguard / Skirmisher fallback
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

    private void TickCooldowns(float dt)
    {
        if (_empowerTimer > 0f)
            _empowerTimer = Mathf.Max(0f, _empowerTimer - dt);

        if (_sideDashCooldownTimer > 0f)
            _sideDashCooldownTimer = Mathf.Max(0f, _sideDashCooldownTimer - dt);

        if (_patternCooldowns.Count == 0) return;

        // Empower juga mempercepat pemulihan cooldown serangan musuh
        float cdStep = dt * (_empowerTimer > 0f ? 1.25f : 1f);

        var keys = new List<EnemyAttackPatternSO>(_patternCooldowns.Keys);
        foreach (var key in keys)
        {
            if (_patternCooldowns[key] > 0f)
                _patternCooldowns[key] = Mathf.Max(0f, _patternCooldowns[key] - cdStep);
        }
    }

    /// <summary>
    /// Bulwark mengaktifkan Guard Stance saat tidak sedang menyerang atau staggered,
    /// menciptakan ritme jelas: serang saat recovery/flank atau parry serangannya.
    /// </summary>
    private void UpdateGuardState()
    {
        if (_stagger == null) return;

        if (!definition.HasFrontalGuard)
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
            : (1 << 9); // Default Environment layer

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
               definition.PreferredZone == PreferredCombatZone.Rear;
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
                _decisionTimer = Random.Range(definition.AttackDecisionDelay.x, definition.AttackDecisionDelay.y);
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
                _movement.MoveTo(_repositionDestination, definition.RepositionSpeed * SpeedMultiplier);
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

        // Ranger / Controller: jika player terlalu dekat, segera masuk Positioning/Retreat agar bisa Retreat Shot
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

        // Khusus Ranger: pastikan ada garis pandang (Line of Sight) ke player sebelum menembak
        if (definition.RequireLineOfSight && !HasClearLineOfSightToPlayer())
        {
            Vector3 sightPoint = _movement.GetLateralSightlinePoint(_playerTarget.position, _strafeSign, 2.5f);
            _movement.MoveTo(sightPoint, definition.StrafeSpeed * SpeedMultiplier);
            return;
        }

        // Khusus Ranger / Controller: jika player menerobos masuk ke jarak terlalu dekat
        if (IsRangedArchetype() && definition.MinSafeDistance > 0f && dist < definition.MinSafeDistance)
        {
            // Coba pilih serangan jarak dekat / Retreat Shot terlebih dahulu sebelum mundur biasa
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

        // Skirmisher menghindari garis pandang depan player sebelum menyerang
        if (IsFlankerArchetype() && IsInPlayerFrontArc() && Random.value < definition.FlankRepositionChance)
        {
            if (definition.CanSideDash && _sideDashCooldownTimer <= 0f)
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
        _patternCooldowns[_selectedPattern] = _selectedPattern.cooldown;
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

        if (_selectedPattern != null && _currentSwingIndex + 1 < _selectedPattern.swings.Count)
        {
            _currentSwingIndex++;
            ChangeState(EnemyBrainState.Telegraph);
            return;
        }

        bool shouldReposition = _selectedPattern != null && _selectedPattern.repositionAfterAttack;
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
                _stagger.ResetStagger();

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

            if (_patternCooldowns.TryGetValue(pattern, out float cd) && cd > 0f)
                continue;

            if (currentDistance < pattern.minTriggerRange || currentDistance > pattern.maxTriggerRange)
                continue;

            // Jangan gunakan SupportEmpower jika tidak ada musuh lain di sekitar Controller
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
            // Sukses Parry selalu menembus Guard/Stagger Resistance Bulwark
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
}

```

---

### Setup Cepat di Unity Editor untuk Menguji Phase 3 (`CombatTest`)

1. **Siapkan `Ranger_Def` (pada `Example_Ranged`):**
* Buat `EnemyDefinitionSO` dengan **Archetype:** `Ranger`, **Preferred Zone:** `Ranged`, **Ideal Combat Distance:** `7.5`, **Min Safe Distance:** `3.8`, **Require Line Of Sight:** Centang (`true`), **Obstacle Layer Mask:** `Environment`.
* Buat 3 `EnemyAttackPatternSO` untuk Ranger:
* **Focused Shot:** `Delivery Type = Projectile`, `Projectile Count = 1`, `Telegraph Shape = Line`, `Reposition After Attack = true`.
* **Burst Shot:** `Delivery Type = Projectile`, `Projectile Count = 3`, `Projectile Spread Angle = 24`.
* **Ground Mark:** `Delivery Type = TargetedZone`, `Telegraph Shape = TargetedZone`, `Zone Radius = 1.8`, `Zone Persist Duration = 0` (ledakan instan saat execution).
* **Retreat Shot:** `Delivery Type = Projectile`, `Max Trigger Range = 4.5`, `Lunge Distance = -2.4` (mundur sambil menembak).




2. **Siapkan `Bulwark_Def` (pada `Example_Elite`):**
* Buat `EnemyDefinitionSO` dengan **Archetype:** `Bulwark`, **Base Stagger Resistance:** `0.35`, **Has Frontal Guard:** Centang (`true`), dan pasang komponen `StaggerComponent` (`Max Stagger = 140`).
* Buat 3 `EnemyAttackPatternSO`:
* **Shield Bash:** `Cone`, `Can Be Parry = true`, `Parry Stagger Bonus = 50`.
* **Ground Slam:** `Circle`, `Range = 3.2`, `Can Be Parry = false`.
* **Charging Strike:** `Line`, `Range = 5.5`, `Lunge Distance = 4.2`, `Lock Rotation On Startup = true`.




3. **Siapkan `Controller_Def` (duplikat dari `Example_Ranged`):**
* Buat `EnemyDefinitionSO` dengan **Archetype:** `Controller`, **Preferred Zone:** `Ranged`, **Ideal Combat Distance:** `8.5`.
* Buat 2 `EnemyAttackPatternSO`:
* **Summon Hazard:** `Delivery Type = TargetedZone`, `Telegraph Shape = TargetedZone`, `Zone Radius = 2.2`, `Zone Persist Duration = 3.5`, `Can Be Parry = false`.
* **Empower Allies:** `Delivery Type = SupportEmpower`, `Telegraph Shape = TargetedZone`, `Empower Radius = 8.0`, `Empower Duration = 5.0`.





Jika sudah diuji di `CombatTest`, beri tahu kalau kamu siap lanjut ke **Phase 4 (Elite & Boss Multi-Stage Pattern + Enemy Modifier)** atau ingin menyetel *balancing* angka dari kelima archetype ini terlebih dahulu.