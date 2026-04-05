using UnityEngine;

[System.Serializable]
public class DropEntry
{
    public MaterialData material;
    public int weight = 1;
    public int minCount = 1;
    public int maxCount = 1;
}

public class MaterialDropHandler : MonoBehaviour
{
    [SerializeField] private DropEntry[] entries;
    [SerializeField] private int maxDropsPerKill = 1;
    [SerializeField] private GameObject groundPickupPrefab;

    public void TriggerDrop()
    {
        if (entries == null || groundPickupPrefab == null) return;

        int spawnIndex = 0;

        for (int i = 0; i < maxDropsPerKill; i++)
        {
            DropEntry entry = RollEntry();
            if (entry == null || entry.material == null) continue;

            int count = Random.Range(entry.minCount, entry.maxCount + 1);
            for (int j = 0; j < count; j++)
            {
                Vector3 spawnPos = transform.position + GetSpreadOffset(spawnIndex);
                GameObject obj = Instantiate(groundPickupPrefab, spawnPos, Quaternion.identity);
                obj.GetComponent<GroundMaterial>()?.Setup(entry.material);
                spawnIndex++;
            }
        }
    }

    private DropEntry RollEntry()
    {
        int total = 0;
        foreach (var e in entries) total += e.weight;
        if (total <= 0) return null;

        int roll = Random.Range(0, total);
        int cumulative = 0;

        foreach (var e in entries)
        {
            cumulative += e.weight;
            if (roll < cumulative) return e;
        }

        return null;
    }

    private Vector3 GetSpreadOffset(int index)
    {
        float angle = index * 137.5f * Mathf.Deg2Rad;
        float radius = 0.4f + index * 0.2f;
        return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }
}