using UnityEngine;

public class ShopRoom : MonoBehaviour
{
    public GameObject shopStationPrefab;
    public GameObject costedHealPrefab;
    public GameObject costedUpgradePrefab;

    [HideInInspector] public RoomNode node;

    public void Init(Transform spawnCenter)
    {
        if (shopStationPrefab != null)
        {
            var shopObj = Instantiate(shopStationPrefab, spawnCenter.position, spawnCenter.rotation);
            var shop = shopObj.GetComponent<ShopInteractable>();
            if (shop != null)
            {
                var cfg = BuildShopConfig();
                shop.SetRandomizerSettings(
                    cfg.minCount,
                    cfg.maxCount,
                    cfg.meanCost,
                    cfg.sd,
                    cfg.allowDuplicates,
                    regenerate: true
                );
            }
        }

        if (costedHealPrefab != null)
            Instantiate(costedHealPrefab, spawnCenter.position + spawnCenter.right * 2f, spawnCenter.rotation);

        if (costedUpgradePrefab != null)
            Instantiate(costedUpgradePrefab, spawnCenter.position - spawnCenter.right * 2f, spawnCenter.rotation);
    }

    private ShopConfig BuildShopConfig()
    {
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        int roomsCleared = RunManager.Instance?.TotalRoomsCleared ?? 0;
        int bossKills = RunManager.Instance?.TotalBossKilled ?? 0;

        float meanCost = 80f + floor * 40f + roomsCleared * 6f + bossKills * 20f;
        float sd = 25f + floor * 5f;
        int minCount = 3;
        int maxCount = 6;

        return new ShopConfig
        {
            minCount = minCount,
            maxCount = maxCount,
            meanCost = meanCost,
            sd = sd,
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
        public int minCount;
        public int maxCount;
        public float meanCost;
        public float sd;
        public bool allowDuplicates;
    }
}