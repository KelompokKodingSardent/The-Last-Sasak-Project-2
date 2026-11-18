using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;
    public Transform playerTransform;
    public int maxEnemies = 5;
    public float spawnRadius = 8f; // Jarak radius spawn dari player
    public float spawnInterval = 3f;

    private void Start()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Cek jumlah musuh aktif (bisa disesuaikan dengan object pooling-mu)
            if (GameObject.FindGameObjectsWithTag("Enemy").Length < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    
void SpawnEnemy()
{
    Vector2 randomDirection = Random.insideUnitCircle;

    if (randomDirection.sqrMagnitude < 0.001f)
        randomDirection = Vector2.right;

    randomDirection.Normalize();

    float randomDistance = Random.Range(4f, spawnRadius);

    Vector3 desiredPosition = playerTransform.position +
        new Vector3(
            randomDirection.x,
            0f,
            randomDirection.y
        ) * randomDistance;

    // Cari posisi terdekat yang bisa dilalui.
    if (NavMesh.SamplePosition(
        desiredPosition,
        out NavMeshHit hit,
        2f,
        NavMesh.AllAreas))
    {
        Instantiate(
            enemyPrefab,
            hit.position,
            Quaternion.identity
        );

        Debug.Log($"[SPAWNER] Musuh muncul: {hit.position}");
    }
    else
    {
        Debug.LogWarning(
            "[SPAWNER] Tidak menemukan posisi spawn di NavMesh!"
        );
    }
}
}
