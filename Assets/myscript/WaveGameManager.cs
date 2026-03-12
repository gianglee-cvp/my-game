using UnityEngine;
using System.Collections.Generic;

public class WaveGameManager : MonoBehaviour
{
    [Header("Wave Control")]
    public EnemySpawner[] enemySpawners;
    [Tooltip("Legacy single spawner field. Used only when enemySpawners is empty.")]
    public EnemySpawner enemySpawner;
    [Min(1)] public int wavesToTriggerBoss = 3;
    public bool syncSpawnerMaxWavesWithBossWave = true;
    public bool stopAutoWavesWhenBossSpawns = true;

    [Header("Boss Setup")]
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public bool parentBossUnderManager = false;

    [Header("Boss Wiring")]
    public bool assignPlayerToBoss = true;
    public Transform player;
    public bool assignSpawnerToBoss = false;

    [Header("UI Reference")]
    public WaveBlinkUI waveUI;
    private int lastAnnouncedWave = 0;

    [Header("Runtime State")]
    [SerializeField] private int currentWave = 0;

    private bool bossSpawned = false;
    private GameObject spawnedBoss;
    private readonly List<EnemySpawner> runtimeSpawners = new List<EnemySpawner>();

    public int CurrentWave => currentWave;

    private void Awake()
    {
        BuildSpawnerList();
        ApplyWaveLimitToSpawners();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void OnEnable()
    {
        RegisterSpawnerCallbacks();
    }

    private void Start()
    {
        UpdateCurrentWaveState();
        TrySpawnBossFromWaveProgress();
    }

    private void Update()
    {
        UpdateCurrentWaveState();
        TrySpawnBossFromWaveProgress();
    }

    private void OnDisable()
    {
        UnregisterSpawnerCallbacks();
    }

    private void RegisterSpawnerCallbacks()
    {
        for (int i = 0; i < runtimeSpawners.Count; i++)
        {
            EnemySpawner spawner = runtimeSpawners[i];
            if (spawner == null) continue;
            spawner.OnWaveCompleted += HandleWaveCompleted;
            spawner.OnWaveStarted += HandleWaveStarted;
        }
    }

    private void UnregisterSpawnerCallbacks()
    {
        for (int i = 0; i < runtimeSpawners.Count; i++)
        {
            EnemySpawner spawner = runtimeSpawners[i];
            if (spawner == null) continue;
            spawner.OnWaveCompleted -= HandleWaveCompleted;
            spawner.OnWaveStarted -= HandleWaveStarted;
        }
    }

    private void HandleWaveCompleted(int completedWaveCount)
    {
        UpdateCurrentWaveState();
        TrySpawnBossFromWaveProgress();
    }

    private void HandleWaveStarted(int waveNumber)
    {
        if (waveUI != null && waveNumber > lastAnnouncedWave)
        {
            lastAnnouncedWave = waveNumber;
            waveUI.StartBlink(waveNumber);
        }
    }

    private void TrySpawnBossFromWaveProgress()
    {
        if (bossSpawned || runtimeSpawners.Count == 0)
        {
            return;
        }

        for (int i = 0; i < runtimeSpawners.Count; i++)
        {
            EnemySpawner spawner = runtimeSpawners[i];
            if (spawner == null)
            {
                return;
            }

            if (spawner.ActiveWaveNumber < wavesToTriggerBoss)
            {
                return;
            }
        }

        SpawnBoss();
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("WaveGameManager: bossPrefab is missing.");
            return;
        }

        Vector3 spawnPosition = bossSpawnPoint != null ? bossSpawnPoint.position : transform.position;
        Quaternion spawnRotation = bossSpawnPoint != null ? bossSpawnPoint.rotation : transform.rotation;
        Transform parent = parentBossUnderManager ? transform : null;

        spawnedBoss = Instantiate(bossPrefab, spawnPosition, spawnRotation, parent);
        bossSpawned = spawnedBoss != null;

        if (!bossSpawned)
        {
            return;
        }

        if (stopAutoWavesWhenBossSpawns)
        {
            for (int i = 0; i < runtimeSpawners.Count; i++)
            {
                EnemySpawner spawner = runtimeSpawners[i];
                if (spawner == null) continue;
                spawner.autoSpawnWaves = false;
            }
        }

        enemyboss1 bossAI = spawnedBoss.GetComponent<enemyboss1>();
        if (bossAI == null)
        {
            bossAI = spawnedBoss.GetComponentInChildren<enemyboss1>();
        }

        if (bossAI != null)
        {
            if (assignPlayerToBoss && player != null)
            {
                bossAI.player = player;
            }

            bossAI.mySpawner = assignSpawnerToBoss ? GetPrimarySpawner() : null;
        }
    }

    private void BuildSpawnerList()
    {
        runtimeSpawners.Clear();

        if (enemySpawners != null && enemySpawners.Length > 0)
        {
            for (int i = 0; i < enemySpawners.Length; i++)
            {
                AddSpawnerIfValid(enemySpawners[i]);
            }
        }

        if (runtimeSpawners.Count == 0 && enemySpawner != null)
        {
            AddSpawnerIfValid(enemySpawner);
        }

        if (runtimeSpawners.Count == 0)
        {
            EnemySpawner[] foundSpawners = Object.FindObjectsByType<EnemySpawner>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
            for (int i = 0; i < foundSpawners.Length; i++)
            {
                AddSpawnerIfValid(foundSpawners[i]);
            }
        }

        enemySpawner = GetPrimarySpawner();
    }

    private void AddSpawnerIfValid(EnemySpawner spawner)
    {
        if (spawner == null)
        {
            return;
        }

        if (!runtimeSpawners.Contains(spawner))
        {
            runtimeSpawners.Add(spawner);
        }
    }

    private EnemySpawner GetPrimarySpawner()
    {
        return runtimeSpawners.Count > 0 ? runtimeSpawners[0] : null;
    }

    private void ApplyWaveLimitToSpawners()
    {
        if (!syncSpawnerMaxWavesWithBossWave)
        {
            return;
        }

        for (int i = 0; i < runtimeSpawners.Count; i++)
        {
            EnemySpawner spawner = runtimeSpawners[i];
            if (spawner == null) continue;
            spawner.maxWaves = wavesToTriggerBoss;
        }
    }

    private void UpdateCurrentWaveState()
    {
        if (runtimeSpawners.Count == 0)
        {
            currentWave = 0;
            return;
        }

        int globalWave = int.MaxValue;

        for (int i = 0; i < runtimeSpawners.Count; i++)
        {
            EnemySpawner spawner = runtimeSpawners[i];
            if (spawner == null)
            {
                continue;
            }

            int spawnerWave = spawner.ActiveWaveNumber > 0
                ? spawner.ActiveWaveNumber
                : Mathf.Max(1, spawner.CompletedWaves + 1);

            globalWave = Mathf.Min(globalWave, spawnerWave);
        }

        if (globalWave == int.MaxValue)
        {
            currentWave = 0;
            return;
        }

        if (bossSpawned)
        {
            currentWave = Mathf.Min(globalWave, wavesToTriggerBoss);
            return;
        }

        currentWave = globalWave;
    }
}
