using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Chase, Telegraph, Attack, Cooldown }
    public EnemyState currentState;

    private NavMeshAgent agent;
    private Transform player;

    [Header("Combat Settings")]
    public float attackRange = 2.5f;
    public float telegraphDuration = 0.8f; // Waktu diam / ancang-ancang sebelum nyerang
    private float telegraphTimer;
    
    [Header("Combo Settings")]
    [Range(0f, 1f)] public float comboChance = 0.5f; // Peluang lanjut kombo
    private int comboStep = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentState = EnemyState.Chase;
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Chase:
                agent.isStopped = false;
                agent.SetDestination(player.position);

                // Kalau sudah masuk jarak serang, masuk ke tahap Telegraph (Diam dulu sebentar)
                if (distanceToPlayer <= attackRange)
                {
                    currentState = EnemyState.Telegraph;
                    telegraphTimer = telegraphDuration;
                    agent.isStopped = true; // Berhenti ngejar biar player bisa lihat ancang-ancang
                }
                break;

            case EnemyState.Telegraph:
                // Menghadap ke player saat ancang-ancang
                Vector3 direction = (player.position - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);

                telegraphTimer -= Time.deltaTime;
                if (telegraphTimer <= 0f)
                {
                    // Waktu telegraph habis, eksekusi serangan!
                    currentState = EnemyState.Attack;
                    ExecuteAttack();
                }
                break;

            case EnemyState.Attack:
                // Logika animasi serangan / hitbox aktif di sini
                // Setelah animasi selesai, tentukan apakah lanjut kombo atau cooldown
                break;

            case EnemyState.Cooldown:
                // Jeda sebentar setelah menyerang sebelum boleh mengejar/menyerang lagi
                break;
        }
    }

    void ExecuteAttack()
    {
        Debug.Log($"[AI] Musuh menyerang! (Combo Step: {comboStep + 1})");
        // Trigger Animasi Serangan (misal: anim.SetTrigger("Attack" + comboStep);)

        // Simulasi selesai menyerang (bisa diganti Animation Event)
        // Di sini kita cek sistem kombo ala ZZZ:
        if (comboStep == 0 && Random.value < comboChance)
        {
            // Lanjut ke serangan combo berikutnya!
            comboStep++;
            currentState = EnemyState.Telegraph; // Masuk telegraph lagi untuk serangan kedua
            telegraphTimer = telegraphDuration * 0.5f; // Kombo biasanya lebih cepat jedanya
        }
        else
        {
            // Reset kombo dan masuk cooldown
            comboStep = 0;
            Invoke("ResetToChase", 1.5f); // Jeda cooldown sebelum ngejar lagi
        }
    }

    void ResetToChase()
    {
        currentState = EnemyState.Chase;
    }
}