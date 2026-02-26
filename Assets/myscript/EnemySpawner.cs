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

    private int currentWave = 1;
    private int enemiesAlive = 0;
    private bool isSpawning = false;

    void Start()
    {
        StartCoroutine(StartWave());
    }

    IEnumerator StartWave()
    {
        isSpawning = true;
        int amount = startEnemyCount * currentWave;

        for (int i = 0; i < amount; i++)
        {
            // Đợi đến khi vị trí spawn trống
            while (Physics.CheckSphere(spawnPoint.position, checkRadius, checkLayer))
            {
                yield return null;
            }

            SpawnEnemy();

            yield return new WaitForSeconds(timeBetweenSpawn);
        }

        isSpawning = false;
        currentWave++;
    }

    void SpawnEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

        // Gán spawner cho EnemyAI
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.mySpawner = this;
        }

        enemiesAlive++;
    }

    void Update()
    {
        if (enemiesAlive == 0 && !isSpawning)
        {
            isSpawning = true;
            StartCoroutine(NextWave());
        }
    }

    IEnumerator NextWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);
        StartCoroutine(StartWave());
    }

    public void EnemyDied()
    {
        enemiesAlive--;
    }

    // Hiển thị vùng kiểm tra trong Scene View
    void OnDrawGizmosSelected()
    {
        if (spawnPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnPoint.position, checkRadius);
    }
}