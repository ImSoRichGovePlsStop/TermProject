using System.Collections.Generic;
using UnityEngine;

public static class Randomizer
{
    private static TestModuleEntry[] _cachedPool;

    private static TestModuleEntry[] GetPool()
    {
        if (_cachedPool != null) return _cachedPool;

        var data = Resources.LoadAll<ModuleData>("Module");
        if (data == null || data.Length == 0)
        {
            Debug.LogWarning("[Randomizer] No ModuleData found in Resources/Module");
            return System.Array.Empty<TestModuleEntry>();
        }

        _cachedPool = new TestModuleEntry[data.Length];
        for (int i = 0; i < data.Length; i++)
            _cachedPool[i] = new TestModuleEntry { data = data[i] };

        Debug.Log($"[Randomizer] Loaded {_cachedPool.Length} modules from Resources/Module");
        return _cachedPool;
    }

    public static List<TestModuleEntry> Roll(
        int minCount,
        int maxCount,
        float meanCost,
        float sd)
    {
        var result = new List<TestModuleEntry>();
        var pool = GetPool();

        if (pool.Length == 0)
            return result;

        int count = Mathf.Min(Random.Range(minCount, maxCount + 1), pool.Length);

        var shuffled = new List<TestModuleEntry>(pool);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (int i = 0; i < count; i++)
        {
            var entry = new TestModuleEntry
            {
                data = shuffled[i].data,
                rarity = CostToRarity(shuffled[i].data, meanCost, sd)
            };
            result.Add(entry);
        }

        return result;
    }

    private static Rarity CostToRarity(ModuleData data, float meanCost, float sd)
    {
        float sampledCost = SampleGaussian(meanCost, sd);

        int bestIndex = 0;
        float bestDelta = float.MaxValue;

        for (int i = 0; i < data.cost.Length; i++)
        {
            if (data.cost[i] <= 0) continue; 

            float delta = Mathf.Abs(data.cost[i] - sampledCost);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestIndex = i;
            }
        }

        return IndexToRarity(bestIndex);
    }

    private static float SampleGaussian(float mean, float sd)
    {
        float u1 = 1f - Random.value;
        float u2 = 1f - Random.value;
        float z = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        return mean + z * sd;
    }

    private static Rarity IndexToRarity(int index) => index switch
    {
        0 => Rarity.Common,
        1 => Rarity.Uncommon,
        2 => Rarity.Rare,
        3 => Rarity.Epic,
        _ => Rarity.GOD
    };
}