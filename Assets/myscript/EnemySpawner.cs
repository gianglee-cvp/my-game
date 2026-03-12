using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Setup")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;

    [Header("Wave Settings")]
    public int startEnemyCount = 3;
    public float timeBetweenWaves = 5f;
    public float timeBetweenSpawn = 1f;

    [Header("Spawn Check")]
    public float checkRadius = 1.5f;
    public LayerMask checkLayer;

    [Header("Flow")]
    public bool autoSpawnWaves = true;
    [Min(0)] public int maxWaves = 0; // 0 = unlimited

    private int currentWave = 1;
    private int enemiesAlive = 0;
    private bool isSpawning = false;
    private int activeWaveNumber = 0;
    private int completedWaves = 0;
    private bool waveActive = false;

    public event System.Action<int> OnWaveCompleted;
    public event System.Action<int> OnWaveStarted;

    public int CurrentWave => currentWave;
    public int ActiveWaveNumber => activeWaveNumber;
    public int CompletedWaves => completedWaves;
    public int EnemiesAlive => enemiesAlive;
    public bool IsSpawning => isSpawning;

    void Start()
    {
        if (!CanStartWave(currentWave))
        {
            return;
        }

        isSpawning = true;
        StartCoroutine(StartWave());
    }

    IEnumerator StartWave()
    {
        if (!CanStartWave(currentWave))
        {
            isSpawning = false;
            waveActive = false;
            yield break;
        }

        isSpawning = true;
        activeWaveNumber = currentWave;
        waveActive = true;
        OnWaveStarted?.Invoke(activeWaveNumber);
        int amount = startEnemyCount * currentWave;

        for (int i = 0; i < amount; i++)
        {
            if (i > 0)
            {
                // Wait until spawn area is clear first.
                while (Physics.CheckSphere(spawnPoint.position, checkRadius, checkLayer))
                    yield return null;

                // Only start spawn interval after area is clear.
                yield return new WaitForSeconds(timeBetweenSpawn);

                // Re-check after waiting in case something moved into spawn area.
                while (Physics.CheckSphere(spawnPoint.position, checkRadius, checkLayer))
                    yield return null;
            }

            SpawnEnemy();
        }

        isSpawning = false;
        currentWave++;
    }

    void SpawnEnemy()
    {
        GameObject enemy = EnemyPool.Spawn(enemyPrefab, spawnPoint.position, spawnPoint.rotation, this);
        if (enemy == null) return;

        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
            ai.mySpawner = this;

        enemiesAlive++;
    }

    void Update()
    {
        if (enemiesAlive == 0 && !isSpawning)
        {
            if (waveActive)
            {
                completedWaves = Mathf.Max(completedWaves, activeWaveNumber);
                waveActive = false;
                OnWaveCompleted?.Invoke(completedWaves);
            }

            if (autoSpawnWaves && CanStartWave(completedWaves + 1))
            {
                isSpawning = true;
                StartCoroutine(NextWave());
            }
        }
    }

    IEnumerator NextWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);
        StartCoroutine(StartWave());
    }

    public void EnemyDied()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
    }

    private bool CanStartWave(int waveNumber)
    {
        return maxWaves <= 0 || waveNumber <= maxWaves;
    }

    void OnDrawGizmosSelected()
    {
        if (spawnPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnPoint.position, checkRadius);
    }
}

public static class EnemyPool
{
    private static readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.Queue<GameObject>> Pools =
        new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Queue<GameObject>>();

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, EnemySpawner spawner)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();
        if (!Pools.TryGetValue(key, out System.Collections.Generic.Queue<GameObject> queue))
        {
            queue = new System.Collections.Generic.Queue<GameObject>();
            Pools[key] = queue;
        }

        while (queue.Count > 0)
        {
            GameObject pooled = queue.Dequeue();
            if (pooled == null) continue;

            pooled.transform.SetPositionAndRotation(position, rotation);
            PooledEnemy pooledEnemy = pooled.GetComponent<PooledEnemy>();
            if (pooledEnemy != null)
            {
                pooledEnemy.Spawner = spawner;
            }
            pooled.SetActive(true);
            return pooled;
        }

        GameObject instance = Object.Instantiate(prefab, position, rotation);
        PooledEnemy marker = instance.GetComponent<PooledEnemy>();
        if (marker == null)
        {
            marker = instance.AddComponent<PooledEnemy>();
        }
        marker.PoolKey = key;
        marker.Spawner = spawner;
        return instance;
    }

    public static void Despawn(GameObject enemy)
    {
        if (enemy == null) return;

        PooledEnemy marker = enemy.GetComponent<PooledEnemy>();
        if (marker == null)
        {
            Object.Destroy(enemy);
            return;
        }

        if (marker.Spawner != null)
        {
            marker.Spawner.EnemyDied();
        }

        int key = marker.PoolKey;
        if (!Pools.TryGetValue(key, out System.Collections.Generic.Queue<GameObject> queue))
        {
            queue = new System.Collections.Generic.Queue<GameObject>();
            Pools[key] = queue;
        }

        enemy.SetActive(false);
        queue.Enqueue(enemy);
    }
}

public class PooledEnemy : MonoBehaviour
{
    public int PoolKey { get; set; }
    public EnemySpawner Spawner { get; set; }
}
