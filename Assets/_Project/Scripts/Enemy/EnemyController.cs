
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class EnemyController : MonoBehaviour
{
    private enum BrainState
    {
        Idle,
        Flanking,
        Approaching,
        Telegraphing,
        Recovering,
        Stunned
    }

    private NavMeshAgent agent;
    private Transform player;
    private CombatDirector director;

    private BrainState state = BrainState.Idle;

    [Header("Detection")]
    public float chaseRange = 10f;
    public float attackRange = 2f;

    [Header("Flanking Movement")]
    public float flankRadiusMin = 3.5f;
    public float flankRadiusMax = 5f;
    public float orbitDegreesPerSecond = 35f;
    public float destinationRefreshInterval = 0.5f;

    [Header("Combat Timing")]
    public float telegraphDuration = 0.7f;
    public float recoveryDuration = 0.8f;
    public float restAfterAttack = 1.5f;

    [Header("Rotation")]
    public float turnSpeed = 8f;

    [Header("Combat Events")]
    public UnityEvent onTelegraphStarted;
    public UnityEvent onAttack;

    private float stateTimer;
    private float restTimer;
    private float nextDestinationTime;

    private float orbitAngle;
    private float orbitDirection;
    private float flankRadius;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (GetComponent<EnemyBrain>() != null)
            enabled = false;
    }

    private void Start()
    {
        if (agent == null)
        {
            Debug.LogError("Enemy membutuhkan NavMeshAgent!", this);
            enabled = false;
            return;
        }

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        director = CombatDirector.Instance;

        if (director == null)
        {
            Debug.LogError(
                "CombatDirector belum tersedia di scene!"
            );
        }

        orbitAngle = Random.Range(0f, 360f);
        orbitDirection = Random.value > 0.5f ? 1f : -1f;

        flankRadius = Random.Range(
            flankRadiusMin,
            flankRadiusMax
        );

        // Variasi prioritas local avoidance.
        agent.avoidancePriority = Random.Range(20, 80);
    }

    private void Update()
    {
        if (player == null)
        {
            EnterIdle();
            return;
        }

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (state == BrainState.Stunned)
        {
            HandleStunned();
            return;
        }

        float distance = GetDistanceToPlayer();

        // Player berada di luar jangkauan deteksi.
        if (distance > chaseRange)
        {
            EnterIdle();
            return;
        }

        // Waktu istirahat setelah menyerang.
        if (restTimer > 0f)
        {
            restTimer -= Time.deltaTime;
            HandleFlanking();
            return;
        }

        // Mencoba mendapatkan slot agresor.
        bool hasSlot =
            director == null || director.RequestAggro(this);

        if (hasSlot)
        {
            HandleAggressor(distance);
        }
        else
        {
            HandleFlanking();
        }
    }

    // =========================================
    // AGGRESSOR BEHAVIOR
    // =========================================

    private void HandleAggressor(float distance)
    {
        // Sedang memberikan telegraph.
        if (state == BrainState.Telegraphing)
        {
            agent.isStopped = true;
            FacePlayer();

            if (distance > attackRange + 0.8f)
            {
                state = BrainState.Approaching;
                return;
            }

            stateTimer -= Time.deltaTime;

            if (stateTimer <= 0f)
            {
                onAttack?.Invoke();

                state = BrainState.Recovering;
                stateTimer = recoveryDuration;
            }

            return;
        }

        // Sedang melakukan recovery.
        if (state == BrainState.Recovering)
        {
            agent.isStopped = true;
            FacePlayer();

            stateTimer -= Time.deltaTime;

            if (stateTimer <= 0f)
            {
                // Selesaikan giliran menyerang.
                if (director != null)
                    director.ReleaseAggro(this);

                state = BrainState.Flanking;
                restTimer = restAfterAttack;
            }

            return;
        }

        // Mendekati player.
        state = BrainState.Approaching;

        agent.isStopped = false;
        agent.stoppingDistance = Mathf.Max(
            0.1f,
            attackRange * 0.75f
        );

        if (distance > attackRange)
        {
            Vector3 target = player.position;

            // Cari titik tujuan yang berada pada NavMesh.
            if (NavMesh.SamplePosition(
                player.position,
                out NavMeshHit hit,
                1.5f,
                agent.areaMask))
            {
                target = hit.position;
            }

            RefreshDestination(target);
            return;
        }

        // Player sudah berada dalam jangkauan.
        agent.isStopped = true;

        FacePlayer();

        state = BrainState.Telegraphing;
        stateTimer = telegraphDuration;

        onTelegraphStarted?.Invoke();
    }

    // =========================================
    // FLANKER / ORBIT BEHAVIOR
    // =========================================

    private void HandleFlanking()
    {
        state = BrainState.Flanking;

        agent.isStopped = false;
        agent.stoppingDistance = 0.25f;

        // Putar sudut orbit.
        orbitAngle +=
            orbitDirection *
            orbitDegreesPerSecond *
            Time.deltaTime;

        float radians = orbitAngle * Mathf.Deg2Rad;

        // Tentukan posisi mengitari player.
        Vector3 offset = new Vector3(
            Mathf.Cos(radians),
            0f,
            Mathf.Sin(radians)
        ) * flankRadius;

        Vector3 desiredPosition = player.position + offset;

        // Perbarui tujuan secara berkala.
        if (Time.time < nextDestinationTime)
            return;

        nextDestinationTime =
            Time.time + Mathf.Max(0.1f, destinationRefreshInterval);

        // Pastikan tujuan berada pada NavMesh.
        if (NavMesh.SamplePosition(
            desiredPosition,
            out NavMeshHit hit,
            2f,
            agent.areaMask))
        {
            agent.SetDestination(hit.position);
        }
    }

    // =========================================
    // STUN BEHAVIOR
    // =========================================

    public void ApplyStun(float duration)
    {
        if (agent == null)
            return;

        if (director != null)
            director.ReleaseAggro(this);

        state = BrainState.Stunned;
        stateTimer = Mathf.Max(0.1f, duration);

        agent.isStopped = true;

        if (agent.isOnNavMesh)
            agent.ResetPath();
    }

    private void HandleStunned()
    {
        agent.isStopped = true;

        FacePlayer();

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            state = BrainState.Flanking;
            restTimer = 0.8f;
        }
    }

    // =========================================
    // GENERAL FUNCTIONS
    // =========================================

    private float GetDistanceToPlayer()
    {
        Vector3 difference = player.position - transform.position;

        difference.y = 0f;

        return difference.magnitude;
    }

    private void RefreshDestination(Vector3 destination)
    {
        if (Time.time < nextDestinationTime)
            return;

        nextDestinationTime =
            Time.time + Mathf.Max(0.1f, destinationRefreshInterval);

        agent.SetDestination(destination);
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }
    }

    private void EnterIdle()
    {
        if (state == BrainState.Idle)
            return;

        state = BrainState.Idle;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (director != null)
            director.ReleaseAggro(this);
    }

    private void OnDisable()
    {
        if (director != null)
            director.Unregister(this);
    }
}
