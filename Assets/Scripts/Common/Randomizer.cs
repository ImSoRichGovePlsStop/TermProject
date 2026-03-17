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
            
            return System.Array.Empty<TestModuleEntry>();
        }

        _cachedPool = new TestModuleEntry[data.Length];
        for (int i = 0; i < data.Length; i++)
            _cachedPool[i] = new TestModuleEntry { data = data[i] };

        
        return _cachedPool;
    }

    public static List<TestModuleEntry> Roll(
        int minCount,
        int maxCount,
        float meanCost,
        float sd,
        bool allowDuplicates = false)
    {
        var result = new List<TestModuleEntry>();
        var pool = GetPool();

        if (pool.Length == 0)
            return result;

        int returnCount = Random.Range(minCount, maxCount + 1);
        int candidateCount = maxCount * 2;

        var candidates = new List<(TestModuleEntry entry, float delta)>();
        var seen = new HashSet<ModuleData>();

        for (int i = 0; i < candidateCount; i++)
        {
            int j = Random.Range(0, pool.Length);
            var data = pool[j].data;

            if (!allowDuplicates && seen.Contains(data)) continue;

            float sampledCost = SampleGaussian(meanCost, sd);

            int bestIndex = -1;
            float bestDelta = float.MaxValue;

            for (int r = 0; r < data.cost.Length; r++)
            {
                if (data.cost[r] <= 0) continue;

                float delta = Mathf.Abs(data.cost[r] - sampledCost);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    bestIndex = r;
                }
            }

            if (bestIndex == -1) continue;

            seen.Add(data);

            candidates.Add((new TestModuleEntry
            {
                data = data,
                rarity = IndexToRarity(bestIndex)
            }, bestDelta));
        }


        candidates.Sort((a, b) => a.delta.CompareTo(b.delta));

        for (int i = 0; i < Mathf.Min(returnCount, candidates.Count); i++)
            result.Add(candidates[i].entry);

        return result;
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