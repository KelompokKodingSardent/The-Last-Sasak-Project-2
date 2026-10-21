using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Hurt
    }

    [Header("State Status")]
    public State currentState;

    [Header("References")]
    public NavMeshAgent agent;
    public Rigidbody rb;
    public Transform player;

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Detection & Movement")]
    public float patrolRadius = 8f;
    public float chaseRange = 6f;
    public float attackRange = 1.8f;
    public float maxIdleTime = 2f;

    [Header("Attack Settings")]
    public float attackCooldown = 1.5f;
    public float attackHitDelay = 0.3f; // Delay hit box aktif setelah animasi/aba-aba
    public float attackDamage = 10f;
    public Transform attackPoint;      // Posisi pusat area serangan
    public float attackHitRadius = 1f;  // Jangkauan hit box
    public LayerMask playerLayer;

    [Header("Knockback Settings")]
    public float knockbackDuration = 0.3f;

    private Vector3 startPosition;
    private float idleTimer;
    private float lastAttackTime;
    private bool isAttacking;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // Pastikan Rigidbody diset agar tidak bentrok dengan NavMeshAgent secara normal
        rb.isKinematic = true;

        currentHealth = maxHealth;
        startPosition = transform.position;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        ChangeState(State.Idle);
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                UpdateIdleState();
                break;
            case State.Patrol:
                UpdatePatrolState();
                break;
            case State.Chase:
                UpdateChaseState();
                break;
            case State.Attack:
                UpdateAttackState();
                break;
            case State.Hurt:
                // Logika ditangani via Coroutine Knockback
                break;
        }
    }

    public void ChangeState(State newState)
{
    if (currentState == State.Hurt && newState != State.Idle && newState != State.Chase) return;

    currentState = newState;

    switch (currentState)
    {
        case State.Idle:
            idleTimer = 0f;
            if (agent.enabled && agent.isOnNavMesh) agent.ResetPath(); // Tambah pengaman
            break;
        case State.Patrol:
            SetNewPatrolDestination();
            break;
        case State.Chase:
            break;
        case State.Attack:
            if (agent.enabled && agent.isOnNavMesh) agent.ResetPath(); // Tambah pengaman
            break;
        case State.Hurt:
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            break;
    }
}

    #region State Updates
    void UpdateIdleState()
    {
        if (CheckPlayerInChaseRange()) return;

        idleTimer += Time.deltaTime;
        if (idleTimer >= maxIdleTime)
        {
            ChangeState(State.Patrol);
        }
    }

    void UpdatePatrolState()
{
    // Pengaman agar tidak error saat agent dimatikan (misal saat knockback)
    if (!agent.enabled || !agent.isOnNavMesh) return;

    if (CheckPlayerInChaseRange()) return;

    if (!agent.pathPending && agent.remainingDistance < 0.5f)
    {
        ChangeState(State.Idle);
    }
}

    void UpdateChaseState()
{
    // Pengaman agar tidak error saat agent dimatikan
    if (!agent.enabled || !agent.isOnNavMesh) return;

    if (player == null) return;

    float distance = Vector3.Distance(transform.position, player.position);

    if (distance <= attackRange)
    {
        ChangeState(State.Attack);
    }
    else if (distance > chaseRange * 1.5f)
    {
        ChangeState(State.Idle);
    }
    else
    {
        agent.SetDestination(player.position);
    }
}

    void UpdateAttackState()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange && !isAttacking)
        {
            ChangeState(State.Chase);
            return;
        }

        // Rotasi menghadap player
        Vector3 lookDir = new Vector3(player.position.x - transform.position.x, 0, player.position.z - transform.position.z);
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
        }

        if (Time.time >= lastAttackTime + attackCooldown && !isAttacking)
        {
            StartCoroutine(ExecuteAttack());
        }
    }
    #endregion

    #region Combat & Knockback
    IEnumerator ExecuteAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        Debug.Log("[DEBUG AI] Musuh mulai mengayunkan serangan!");

        yield return new WaitForSeconds(attackHitDelay);

        // Pengecekan Hit Area (Area Serangan Musuh)
        Vector3 hitPoint = attackPoint != null ? attackPoint.position : transform.position + transform.forward;
        Collider[] hitPlayers = Physics.OverlapSphere(hitPoint, attackHitRadius, playerLayer);

        foreach (Collider p in hitPlayers)
        {
            Debug.Log("[DEBUG AI] HIT! Serangan musuh MENGENAI Player!");
            // Di sini kamu bisa panggil script Player misal: p.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
        }

        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    public void TakeDamage(int damageAmount, Vector3 attackerPosition, float knockbackForce)
    {
        currentHealth -= damageAmount;
        Debug.Log($"[DEBUG AI] Musuh TERKENA HIT! Sisa HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Trigger Knockback
        StopAllCoroutines();
        isAttacking = false;
        StartCoroutine(ApplyKnockback(attackerPosition, knockbackForce));
    }

    IEnumerator ApplyKnockback(Vector3 attackerPosition, float force)
    {
        ChangeState(State.Hurt);

        // Nonaktifkan NavMeshAgent agar Rigidbody bisa terdorong secara fisik
        agent.enabled = false;
        rb.isKinematic = false;

        Vector3 pushDirection = (transform.position - attackerPosition).normalized;
        pushDirection.y = 0.1f; // Tambahkan sedikit dorongan ke atas

        rb.AddForce(pushDirection * force, ForceMode.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        // Kembalikan ke kontrol NavMeshAgent
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        agent.enabled = true;

        ChangeState(State.Chase);
    }

    void Die()
    {
        Debug.Log("[DEBUG AI] Musuh Mati!");
        Destroy(gameObject);
    }
    #endregion

    #region Helper Methods
    bool CheckPlayerInChaseRange()
    {
        if (player != null && Vector3.Distance(transform.position, player.position) <= chaseRange)
        {
            ChangeState(State.Chase);
            return true;
        }
        return false;
    }

    void SetNewPatrolDestination()
    {
        if (!agent.enabled) return;

        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // Menampilkan visual jangkauan serangan dan deteksi di Editor Unity
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 hitPoint = attackPoint != null ? attackPoint.position : transform.position + transform.forward;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(hitPoint, attackHitRadius);
    }
    #endregion
}