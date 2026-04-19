using UnityEngine;

public class ShopRoom : MonoBehaviour
{
    public GameObject shopStationPrefab;
    public GameObject costedHealPrefab;
    public GameObject costedUpgradePrefab;

    [HideInInspector] public RoomNode node;

    [Header("Shop Config")]
    public int   shopMinCount         = 3;
    public int   shopMaxCount         = 4;
    public float shopBaseMean         = 80f;
    public float shopMeanPerFloor     = 40f;
    public float shopMeanPerRoom      = 6f;
    public float shopMeanPerBossKill  = 20f;
    public float shopBaseSd           = 25f;
    public float shopSdPerFloor       = 5f;
    [Tooltip("Duplicate chance for current modules in shop rolls.")]
    public float shopDupChance        = 0.1f;

    const float SideStationOffset = 2f;

    public void Init(Transform spawnCenter)
    {
        if (shopStationPrefab != null)
        {
            var shopObj = Instantiate(shopStationPrefab, spawnCenter.position, spawnCenter.rotation);
            var shop    = shopObj.GetComponent<ShopInteractable>();
            if (shop != null)
            {
                var cfg = BuildShopConfig();
                shop.SetRandomizerSettings(
                    cfg.count,
                    cfg.midCost,
                    cfg.cheapSd,
                    cfg.expensiveSd,
                    cfg.allowDuplicates,
                    shopDupChance,
                    regenerate: true
                );
            }
        }

        if (costedHealPrefab != null)
            Instantiate(costedHealPrefab,    spawnCenter.position + spawnCenter.right * SideStationOffset,  spawnCenter.rotation);

        if (costedUpgradePrefab != null)
            Instantiate(costedUpgradePrefab, spawnCenter.position - spawnCenter.right * SideStationOffset, spawnCenter.rotation);
    }

    private ShopConfig BuildShopConfig()
    {
        int floor        = RunManager.Instance?.CurrentFloor ?? 1;
        int roomsCleared = RunManager.Instance?.TotalRoomsCleared ?? 0;
        int bossKills    = RunManager.Instance?.TotalBossKilled ?? 0;
        int extraPool = RunManager.Instance?.EffectiveShopPool ?? 0;

        float midCost = shopBaseMean + floor * shopMeanPerFloor + roomsCleared * shopMeanPerRoom + bossKills * shopMeanPerBossKill;
        float sd      = shopBaseSd   + floor * shopSdPerFloor;

        return new ShopConfig
        {
            count          = Random.Range(shopMinCount+extraPool, shopMaxCount + 1 + extraPool),
            midCost        = midCost,
            cheapSd        = sd,
            expensiveSd    = sd,
            allowDuplicates = false,
        };
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RunManager.Instance?.OnEventRoomEntered();
            FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
        }
    }

    private struct ShopConfig
    {
        public int   count;
        public float midCost;
        public float cheapSd;
        public float expensiveSd;
        public bool  allowDuplicates;
    }
}
