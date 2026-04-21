using UnityEngine;

public class TradeSystem : MonoBehaviour, IInteractable
{
    [Header("Cost")]
    [SerializeField] private MaterialData boneMaterial;
    [SerializeField] private int boneCost = 10;

    [Header("Reward Pool")]
    [SerializeField] private MaterialData[] materialPool;
    [SerializeField] private GameObject groundMaterialPrefab;
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0f, 1f);
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float dropSpreadLeft = -1.5f;
    [SerializeField] private float dropSpreadRight = 1.5f;

    [Header("Module Roll Settings")]
    [SerializeField] private float moduleMeanCost = 150f;
    [SerializeField] private float moduleSd = 300f;
    [SerializeField] private bool allowDuplicates = false;

    [Header("Message Settings")]
    [SerializeField] private float messageFontSize = 5f;
    [SerializeField] private float messageHeightOffset = 0.4f;

    private const float pMat = 0.20f;
    private const float pMod = 0.90f;
    private const float pLocked = 0.90f;

    private bool modGiven = false;
    private UIManager ui;

    private static readonly Color colItem = new Color(0f, 1f, 0.4f);
    private static readonly Color colNone = Color.black;
    private static readonly Color colErr = new Color(1f, 0.35f, 0.35f);

    public string GetPromptText() => $"[ E ]  Offer {boneCost} Bone";
    public InteractInfo GetInteractInfo() => new InteractInfo
    {
        name = "Cerberus",
        description = $"Offer <color=#88FF88>{boneCost}</color> bones to Cerberus in exchange for a reward.",
        actionText = "Offer Bones",
        cost = null
    };

    private void Start()
    {
        ui = FindFirstObjectByType<UIManager>();
    }

    public void Interact(PlayerController playerController)
    {
        if (UIManager.IsRightPanelOpen) return;
        if (ui != null && ui.IsInventoryOpen) return;

        if (boneMaterial == null)
        {
            Debug.LogWarning("[TradeSystem] boneMaterial not assigned.");
            return;
        }

        if (!MaterialStorage.Instance.HasEnough(boneMaterial, boneCost))
        {
            Msg($"Need {boneCost} Bone!", colErr);
            return;
        }

        MaterialStorage.Instance.TryRemove(boneMaterial, boneCost);

        float roll = Random.value;

        if (modGiven)
        {
            if (roll < pLocked) GrantMaterialReward();
            else SpawnNothing();
        }
        else
        {
            if (roll < pMat) GrantMaterialReward();
            else if (roll < pMod) GrantModuleReward();
            else SpawnNothing();
        }
    }

    private void GrantMaterialReward()
    {
        if (materialPool == null || materialPool.Length == 0)
        {
            Debug.LogWarning("[TradeSystem] materialPool is empty.");
            SpawnNothing(); return;
        }
        if (groundMaterialPrefab == null)
        {
            Debug.LogWarning("[TradeSystem] groundMaterialPrefab not assigned.");
            SpawnNothing(); return;
        }

        var mat = materialPool[Random.Range(0, materialPool.Length)];
        if (mat == null) { SpawnNothing(); return; }

        Vector3 spread = new Vector3(Random.Range(dropSpreadLeft, dropSpreadRight), 0f, 0f);
        Vector3 rawPos = transform.position + transform.TransformDirection(dropOffset + spread);
        float groundY = rawPos.y;
        if (Physics.Raycast(rawPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f))
            groundY = hit.point.y;
        float floatOffset = floatAmplitude * 1f;
        Vector3 spawnPos = new Vector3(rawPos.x, groundY - floatOffset, rawPos.z);
        var obj = Instantiate(groundMaterialPrefab, spawnPos, Quaternion.identity);
        var gm = obj.GetComponent<GroundMaterial>();
        gm?.Setup(mat);
        gm?.DelayShow();
        Msg($"Received: {mat.moduleName}!", colItem);
    }

    private void GrantModuleReward()
    {
        if (ui == null)
            ui = FindFirstObjectByType<UIManager>();

        var rolled = Randomizer.Roll(1, 1, moduleMeanCost, moduleSd, allowDuplicates);
        if (rolled == null || rolled.Count == 0)
        {
            Debug.LogWarning("[TradeSystem] Randomizer.Roll returned empty.");
            SpawnNothing();
            return;
        }

        var cfg = new LootConfig { optionCount = 1, meanCost = moduleMeanCost, sd = moduleSd, allowDuplicates = allowDuplicates };
        ui.OpenRewardLoot(cfg, rolled);
        modGiven = true;
        Msg("A Gift from Cerberus!", colItem);
    }

    private void SpawnNothing()
    {
        Msg("Cerberus ignores you...", colNone);
    }

    private void Msg(string text, Color color)
    {
        DamageNumberSpawner.Instance?.SpawnMessage(transform.position, text, color, messageHeightOffset, messageFontSize);
    }
}