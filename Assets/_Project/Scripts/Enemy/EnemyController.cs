using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    public enum State
    {
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

    [Header("Combat & Movement Settings")]
    public float attackRange = 1.5f;        // Jarak dekat untuk mulai memukul
    public float attackCooldown = 1.2f;     // Jeda antar serangan
    public float attackHitDelay = 0.2f;     // Delay waktu kena pukul
    public float attackDamage = 10f;
    
    public Transform attackPoint;
    public float attackHitRadius = 1f;
    public LayerMask playerLayer;

    [Header("Knockback Settings")]
    public float knockbackDuration = 0.25f;

    private float lastAttackTime;
    private bool isAttacking;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        currentHealth = maxHealth;

        // Cari Player secara otomatis jika belum di-drag di Inspector
        FindPlayerTarget();

        // Musuh style Hades / Minecraft Dungeons: Langsung CHASE saat spawn
        ChangeState(State.Chase);
    }

    void Update()
    {
        // Jika player tidak ditemukan/hancur, cari ulang
        if (player == null)
        {
            FindPlayerTarget();
            return;
        }

        switch (currentState)
        {
            case State.Chase:
                UpdateChaseState();
                break;
            case State.Attack:
                UpdateAttackState();
                break;
            case State.Hurt:
                break;
        }
    }

    void FindPlayerTarget()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    public void ChangeState(State newState)
    {
        if (currentState == State.Hurt && newState != State.Chase) return;

        currentState = newState;

        switch (currentState)
        {
            case State.Chase:
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                }
                break;
            case State.Attack:
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.isStopped = true;
                }
                break;
            case State.Hurt:
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                break;
        }
    }

    void UpdateChaseState()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Jika sudah dekat, masuk ke mode Attack
        if (distance <= attackRange)
        {
            ChangeState(State.Attack);
        }
        else
        {
            // Terus kejar posisi player
            agent.SetDestination(player.position);
        }
    }

    void UpdateAttackState()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // Jika player kabur keluar dari jarak serang, kembali mengejar
        if (distance > attackRange + 0.5f && !isAttacking)
        {
            ChangeState(State.Chase);
            return;
        }

        // Musuh selalu berputar menghadap Player saat bersiap menyerang
        Vector3 lookDir = new Vector3(player.position.x - transform.position.x, 0, player.position.z - transform.position.z);
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
        }

        if (Time.time >= lastAttackTime + attackCooldown && !isAttacking)
        {
            StartCoroutine(ExecuteAttack());
        }
    }

    IEnumerator ExecuteAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        Debug.Log("[DEBUG AI] Musuh mulai mengayunkan serangan!");

        yield return new WaitForSeconds(attackHitDelay);

        Vector3 hitPoint = attackPoint != null ? attackPoint.position : transform.position + transform.forward;
        Collider[] hitPlayers = Physics.OverlapSphere(hitPoint, attackHitRadius, playerLayer);

        foreach (Collider p in hitPlayers)
        {
            Debug.Log("[DEBUG AI] HIT! Serangan musuh MENGENAI Player!");
        }

        yield return new WaitForSeconds(0.3f);
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

        StopAllCoroutines();
        isAttacking = false;
        StartCoroutine(ApplyKnockback(attackerPosition, knockbackForce));
    }

    IEnumerator ApplyKnockback(Vector3 attackerPosition, float force)
    {
        ChangeState(State.Hurt);

        agent.enabled = false;
        rb.isKinematic = false;

        Vector3 pushDirection = (transform.position - attackerPosition).normalized;
        pushDirection.y = 0.1f;

        rb.AddForce(pushDirection * force, ForceMode.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 hitPoint = attackPoint != null ? attackPoint.position : transform.position + transform.forward;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(hitPoint, attackHitRadius);
    }
}