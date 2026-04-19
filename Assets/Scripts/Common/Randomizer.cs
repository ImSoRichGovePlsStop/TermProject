using System.Collections.Generic;
using System.Linq;
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
        bool allowDuplicates = false,
        float currentModuleDupChance = 0.5f)
    {
        var result = new List<TestModuleEntry>();
        var pool = GetPool();

        if (pool.Length == 0)
            return result;

        int returnCount = Random.Range(minCount, maxCount + 1);
        int candidateCount = maxCount * 2;

        var candidates = new List<(TestModuleEntry entry, float delta)>();
        var seen = new HashSet<ModuleData>();

        var currentSet = new HashSet<ModuleData>();
        var mgr = InventoryManager.Instance;
        if (mgr != null)
        {
            foreach (var m in mgr.BagGrid.GetAllModules())
                if (m.Data != null) currentSet.Add(m.Data);
            foreach (var m in mgr.WeaponGrid.GetAllModules())
                if (m.Data != null) currentSet.Add(m.Data);
        }

        for (int i = 0; i < candidateCount; i++)
        {
            int j = Random.Range(0, pool.Length);
            var data = pool[j].data;

            if (seen.Contains(data)) continue;
//            if(currentSet.Any(x => x.moduleName == data.moduleName)) Debug.Log($"Duplicate found: {data.moduleName}");
            if (!allowDuplicates && currentSet.Any(x => x == data) && Random.value > currentModuleDupChance)
            {
//                Debug.Log($"Skipping duplicate: {data.moduleName}");
                continue;
            }

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

    public static (List<TestModuleEntry> cheap, List<TestModuleEntry> mid, List<TestModuleEntry> expensive) ShopRoll(
        float midCost,
        float cheapSd,
        float expensiveSd,
        int count,
        bool allowDuplicates = false,
        float currentModuleDupChance = 0.1f)
    {

        float separationFactor = 2.0f;   // increase = less overlap
        float spreadFactor = 0.6f;       // decrease = tighter distribution

        float cheapMean = midCost - (cheapSd * separationFactor);
        float expensiveMean = midCost + (expensiveSd * separationFactor);

        float midSd = (cheapSd + expensiveSd) * 0.5f * spreadFactor;
        cheapSd *= spreadFactor;
        expensiveSd *= spreadFactor;

        var cheap = new List<TestModuleEntry>();
        var mid = new List<TestModuleEntry>();
        var expensive = new List<TestModuleEntry>();

        if (count > 0)
        {
            mid.AddRange(Roll(1, 1, midCost, midSd, allowDuplicates, currentModuleDupChance));
            count--;
        }

        if (count > 0)
        {
            int half = count / 2;

            if (half > 0)
                cheap.AddRange(Roll(half, half, cheapMean, cheapSd, allowDuplicates, currentModuleDupChance));

            if (half > 0)
                expensive.AddRange(Roll(half, half, expensiveMean, expensiveSd, allowDuplicates, currentModuleDupChance));

            count -= (half * 2);
        }

        if (count > 0)
        {
            bool pickCheap = Random.value < 0.5f;

            if (pickCheap)
                cheap.AddRange(Roll(1, 1, cheapMean, cheapSd, allowDuplicates, currentModuleDupChance));
            else
                expensive.AddRange(Roll(1, 1, expensiveMean, expensiveSd, allowDuplicates, currentModuleDupChance));
        }

        return (cheap, mid, expensive);
    }

    public static TestModuleEntry RollInRange(float minCost, float maxCost, bool allowDuplicates = false, int minRarityIndex = 0)
    {
        var pool = GetPool();
        float mean = (minCost + maxCost) * 0.5f;

        var currentSet = new HashSet<ModuleData>();
        var mgr = InventoryManager.Instance;
        if (mgr != null)
        {
            foreach (var m in mgr.BagGrid.GetAllModules())
                if (m.Data != null) currentSet.Add(m.Data);
            foreach (var m in mgr.WeaponGrid.GetAllModules())
                if (m.Data != null) currentSet.Add(m.Data);
        }

        var candidates = new List<(TestModuleEntry entry, float weight)>();

        foreach (var item in pool)
        {
            var data = item.data;
            if (!allowDuplicates && currentSet.Contains(data)) continue;

            // Find the rarity tier whose cost best fits the range
            int   bestRarity = -1;
            float bestDelta  = float.MaxValue;

            for (int r = 0; r < data.cost.Length; r++)
            {
                float c = data.cost[r];
                if (c <= 0 || c < minCost || c > maxCost) continue;

                float delta = Mathf.Abs(c - mean);
                if (delta < bestDelta) { bestDelta = delta; bestRarity = r; }
            }

            if (bestRarity == -1) continue;
            if (bestRarity < minRarityIndex) continue;

            float weight = 1f / (bestDelta + 1f);
            candidates.Add((new TestModuleEntry { data = data, rarity = IndexToRarity(bestRarity) }, weight));
        }

        if (candidates.Count == 0) return default;

        // Weighted random pick
        float totalWeight = 0f;
        foreach (var c in candidates) totalWeight += c.weight;

        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        foreach (var (entry, weight) in candidates)
        {
            cumulative += weight;
            if (roll <= cumulative) return entry;
        }
        return candidates[candidates.Count - 1].entry;
    }

    public static (Rarity min, Rarity max) GetRarityRange(float minCost, float maxCost)
    {
        var pool = GetPool();
        int minIdx = int.MaxValue;
        int maxIdx = int.MinValue;

        foreach (var item in pool)
        {
            for (int r = 0; r < item.data.cost.Length; r++)
            {
                float c = item.data.cost[r];
                if (c <= 0 || c < minCost || c > maxCost) continue;
                if (r < minIdx) minIdx = r;
                if (r > maxIdx) maxIdx = r;
            }
        }

        if (minIdx == int.MaxValue) return (Rarity.Common, Rarity.Common);
        return (IndexToRarity(minIdx), IndexToRarity(maxIdx));
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
