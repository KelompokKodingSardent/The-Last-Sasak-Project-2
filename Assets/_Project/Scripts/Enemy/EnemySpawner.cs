using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;
    public Transform playerTransform;
    public int maxEnemies = 5;
    public float spawnRadius = 8f;
    public float spawnInterval = 3f;
    public bool AutoSpawn;

    private readonly HashSet<Health> _trackedAlive = new HashSet<Health>();

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (AutoSpawn)
            StartCoroutine(SpawnRoutine());
    }

    private void OnEnable()
    {
        GameEvents.OnEntityDied += HandleEntityDied;
    }

    private void OnDisable()
    {
        GameEvents.OnEntityDied -= HandleEntityDied;
        _trackedAlive.Clear();
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            PruneInvalidEntries();
            if (_trackedAlive.Count < maxEnemies)
                SpawnEnemy();
        }
    }

    public void SpawnEnemy()
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

        Health health = instance.GetComponent<Health>();
        if (health != null)
            _trackedAlive.Add(health);
    }

    private void HandleEntityDied(Health health)
    {
        if (health != null)
            _trackedAlive.Remove(health);
    }

    private void PruneInvalidEntries()
    {
        _trackedAlive.RemoveWhere(h => h == null || h.IsDead || !h.gameObject.activeInHierarchy);
    }
}