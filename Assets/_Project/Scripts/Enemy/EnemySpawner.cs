using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;    // Prefab DummyEnemy
    public int spawnCount = 2;        // Jumlah musuh untuk testing (1 atau 2)
    public float spawnRadius = 10f;   // Jarak acak spawn dari lokasi spawner

    void Start()
    {
        SpawnEnemies();
    }

    public void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy Prefab belum dimasukkan ke EnemySpawner!");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                0,
                Random.Range(-spawnRadius, spawnRadius)
            );

            Vector3 spawnPosition = transform.position + randomOffset;

            // Spawn musuh baru
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            Debug.Log($"[SPAWNER] Musuh #{i + 1} berhasil dispawn di posisi: {spawnPosition}");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}