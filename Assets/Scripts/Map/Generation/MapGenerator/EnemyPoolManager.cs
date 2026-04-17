using System.Collections.Generic;
using UnityEngine;

public class EnemyPoolManager : MonoBehaviour
{
    public static EnemyPoolManager Instance { get; private set; }

    [Header("Universal Enemy")]
    [Tooltip("Always present in every floor's pool regardless of segment.")]
    public EnemyEntry universalEnemy;

    
    public EnemyEntry[] enemies;

    [Header("Segment Settings")]
    [Tooltip("How many segments to divide floors into.")]
    public int segmentCount    = 3;
    [Tooltip("How many floors belong to each segment.")]
    public int floorsPerSegment = 3;

    private List<EnemyEntry>[] _segmentPools;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPools();
    }

    public void RebuildPools() => BuildPools();

    void BuildPools()
    {
        _segmentPools = new List<EnemyEntry>[segmentCount];
        for (int i = 0; i < segmentCount; i++)
            _segmentPools[i] = new List<EnemyEntry>();

        if (enemies == null || enemies.Length == 0) return;

        var melee  = new List<EnemyEntry>();
        var ranged = new List<EnemyEntry>();
        foreach (var e in enemies)
        {
            if (e?.normal == null) continue;
            if (e.type == EnemyType.Melee) melee.Add(e);
            else                           ranged.Add(e);
        }
        Shuffle(melee);
        Shuffle(ranged);

        for (int i = 0; i < melee.Count;  i++) _segmentPools[i % segmentCount].Add(melee[i]);
        for (int i = 0; i < ranged.Count; i++) _segmentPools[i % segmentCount].Add(ranged[i]);

        for (int s = 0; s < segmentCount; s++)
        {
            var neighbours = GetNeighbourIndices(s);
            if (neighbours.Count == 0) continue;

            int  neighbourIdx = neighbours[Random.Range(0, neighbours.Count)];
            var  pool         = _segmentPools[neighbourIdx];
            if (pool.Count == 0) continue;

            _segmentPools[s].Add(pool[Random.Range(0, pool.Count)]);
        }
    }

    public List<EnemyEntry> GetPoolForFloor(int floor)
    {
        int segIdx = Mathf.Clamp((floor - 1) / floorsPerSegment, 0, segmentCount - 1);
        var pool   = new List<EnemyEntry>(_segmentPools[segIdx]);

        if (universalEnemy?.normal != null)
            pool.Add(universalEnemy);

        return pool;
    }

    List<int> GetNeighbourIndices(int s)
    {
        var result = new List<int>();
        if (s > 0)               result.Add(s - 1);
        if (s < segmentCount - 1) result.Add(s + 1);
        return result;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j   = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
