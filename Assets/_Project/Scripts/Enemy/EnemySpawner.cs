using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;
    public Transform playerTransform;
    public int maxEnemies = 5;
    public float spawnRadius = 8f;
    public float spawnInterval = 3f;

    int alive;

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (alive < maxEnemies)
                SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null || playerTransform == null)
            return;

        Vector2 randomDirection = Random.insideUnitCircle;
        if (randomDirection.sqrMagnitude < 0.001f)
            randomDirection = Vector2.right;
        randomDirection.Normalize();

        Vector3 desiredPosition = playerTransform.position +
            new Vector3(randomDirection.x, 0f, randomDirection.y) * Random.Range(4f, spawnRadius);

        if (!NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return;

        GameObject instance = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.Get(enemyPrefab, hit.position, Quaternion.identity)
            : Instantiate(enemyPrefab, hit.position, Quaternion.identity);

        alive++;
        Health health = instance.GetComponent<Health>();
        if (health != null)
            health.OnDied += HandleDeath;
    }

    void HandleDeath()
    {
        alive = Mathf.Max(0, alive - 1);
    }
}
