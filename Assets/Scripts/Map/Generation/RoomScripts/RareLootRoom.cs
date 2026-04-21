using UnityEngine;

public class RareLootRoom : MonoBehaviour
{
    public GameObject lootPrefab;

    [HideInInspector] public RoomNode node;

    [Header("Loot Scaling")]
    [Tooltip("Number of module options offered (run modifier can add more).")]
    public int   lootOptionCount     = 3;
    [Tooltip("Base loot mean cost — should sit above a typical battle-room drop.")]
    public float lootBaseMean        = 100f;
    [Tooltip("Mean cost increase per floor.")]
    public float lootMeanPerFloor    = 25f;
    [Tooltip("Extra mean cost per boss already killed this run.")]
    public float lootMeanPerBossKill = 0f;
    [Tooltip("Base standard deviation of loot cost.")]
    public float lootBaseSd          = 30f;
    [Tooltip("SD increase per floor.")]
    public float lootSdPerFloor      = 5f;

    public void Init(Transform spawnPoint)
    {
        if (lootPrefab == null) return;

        var obj = Instantiate(lootPrefab, spawnPoint.position, spawnPoint.rotation);
        obj.GetComponent<RandomLoot>()?.Configure(BuildLootConfig());
    }

    LootConfig BuildLootConfig()
    {
        var rm      = RunManager.Instance;
        int floor   = rm?.CurrentFloor      ?? 1;

        float mean = lootBaseMean
                   + floor  * lootMeanPerFloor
                   + (rm?.EffectiveLootMeanBonus ?? 0f);

        float sd      = lootBaseSd + (floor - 1) * lootSdPerFloor;
        int   options = lootOptionCount + (rm?.EffectiveExtraLootOptions ?? 0);

        return new LootConfig { optionCount = options, meanCost = mean, sd = sd, allowDuplicates = false };
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RunManager.Instance?.OnEventRoomEntered();
            FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
        }
    }
}
