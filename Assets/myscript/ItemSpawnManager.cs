using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ItemSpawnManager : MonoBehaviour
{
    [Header("Item Prefabs")]
    public GameObject coinPrefab;
    public GameObject crossPrefab;
    public GameObject shieldPrefab;

    [Header("Startup Counts")]
    [Min(0)] public int coinCount = 20;
    [Min(0)] public int crossCount = 6;
    [Min(0)] public int shieldCount = 10;
    
    [Header("Spawn Area")]
    public Vector3 spawnAreaCenter = Vector3.zero;
    public Vector3 spawnAreaSize = new Vector3(60f, 0f, 60f);

    [Header("Validation")]
    [Tooltip("Layers that should block item spawn (enemy, obstacle, wall, player, existing item). Exclude ground/floor.")]
    public LayerMask blockedLayers;
    [Min(0.1f)] public float clearRadius = 1.2f;
    [Min(0.1f)] public float minItemSpacing = 1.6f;
    [Min(0.1f)] public float navMeshSampleDistance = 3f;
    [Min(1)] public int maxAttemptsPerItem = 80;
    public float yOffset = 0.05f;
    public bool shuffleSpawnOrder = true;

    [Header("Drop Table")]
    public float noDropWeight = 50f;
    public List<DropItem> dropTable = new List<DropItem>();

    [System.Serializable]
    public class DropItem
    {
        public string name;
        public GameObject prefab;
        public float weight = 10f;
    }

    private readonly List<Vector3> acceptedPositions = new List<Vector3>();

    public void SpawnItemAtTransform(Transform target)
    {
        if (target == null) return;

        GameObject itemPrefab = GetRandomItemPrefab();
        if (itemPrefab == null) return;

        // Spawn tai vi tri enemy voi yOffset (cung y voi item spawn luc dau game)
        Vector3 spawnPos = target.position;
        spawnPos.y = yOffset;

        Instantiate(itemPrefab, spawnPos, Quaternion.identity, transform);
    }

    private GameObject GetRandomItemPrefab()
    {
        if (dropTable == null || dropTable.Count == 0) return null;

        float totalWeight = noDropWeight;
        foreach (var item in dropTable)
        {
            if (item.prefab != null)
                totalWeight += item.weight;
        }

        if (totalWeight <= 0f) return null;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = noDropWeight;

        // Neu random rơi vao noDropWeight thi return null
        if (randomValue < currentWeight)
            return null;

        foreach (var item in dropTable)
        {
            if (item.prefab == null) continue;
            currentWeight += item.weight;
            if (randomValue < currentWeight)
                return item.prefab;
        }

        return null;
    }

    private void Start()
    {
        SpawnAtGameStart();
    }

    [ContextMenu("Spawn At Game Start")]
    public void SpawnAtGameStart()
    {
        acceptedPositions.Clear();
        List<GameObject> queue = BuildSpawnQueue();
        if (shuffleSpawnOrder)
        {
            Shuffle(queue);
        }

        int spawned = 0;
        for (int i = 0; i < queue.Count; i++)
        {
            GameObject prefab = queue[i];
            if (prefab == null)
            {
                Debug.LogWarning("[ItemSpawnManager] Missing item prefab. Skipping one spawn.");
                continue;
            }

            if (!TryFindValidSpawnPosition(out Vector3 position))
            {
                Debug.LogWarning("[ItemSpawnManager] Failed to find valid position for an item.");
                continue;
            }

            Instantiate(prefab, position, Quaternion.identity, transform);
            acceptedPositions.Add(position);
            spawned++;
        }

        Debug.Log($"[ItemSpawnManager] Spawned {spawned}/{queue.Count} items.");
    }

    private List<GameObject> BuildSpawnQueue()
    {
        List<GameObject> queue = new List<GameObject>(coinCount + crossCount + shieldCount);
        AddItems(queue, coinPrefab, coinCount);
        AddItems(queue, crossPrefab, crossCount);
        AddItems(queue, shieldPrefab, shieldCount);
        return queue;
    }

    private static void AddItems(List<GameObject> queue, GameObject prefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            queue.Add(prefab);
        }
    }

    private bool TryFindValidSpawnPosition(out Vector3 result)
    {
        for (int attempt = 0; attempt < maxAttemptsPerItem; attempt++)
        {
            Vector3 candidate = GetRandomPointInArea();
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                continue;
            }

            Vector3 spawnPoint = navHit.position + Vector3.up * yOffset;
            if (Physics.CheckSphere(spawnPoint, clearRadius, blockedLayers, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            if (!HasEnoughSpacing(spawnPoint))
            {
                continue;
            }

            result = spawnPoint;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private bool HasEnoughSpacing(Vector3 point)
    {
        float minDistanceSqr = minItemSpacing * minItemSpacing;
        for (int i = 0; i < acceptedPositions.Count; i++)
        {
            Vector3 other = acceptedPositions[i];
            if ((point - other).sqrMagnitude < minDistanceSqr)
            {
                return false;
            }
        }

        return true;
    }

    private Vector3 GetRandomPointInArea()
    {
        Vector3 half = spawnAreaSize * 0.5f;
        return new Vector3(
            Random.Range(spawnAreaCenter.x - half.x, spawnAreaCenter.x + half.x),
            spawnAreaCenter.y,
            Random.Range(spawnAreaCenter.z - half.z, spawnAreaCenter.z + half.z)
        );
    }

    private static void Shuffle(List<GameObject> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
    }
}
