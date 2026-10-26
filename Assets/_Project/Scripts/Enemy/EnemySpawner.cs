using UnityEngine;
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
        // Generate posisi random dalam bentuk lingkaran (bisa Kiri, Kanan, Belakang, Depan)
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(4f, spawnRadius);
        
        Vector3 spawnPosition = new Vector3(
            playerTransform.position.x + (randomDirection.x * randomDistance),
            playerTransform.position.y,
            playerTransform.position.z + (randomDirection.y * randomDistance)
        );

        // Spawn musuh di posisi random tersebut
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"[SPAWNER] Musuh muncul di posisi: {spawnPosition}");
    }
}