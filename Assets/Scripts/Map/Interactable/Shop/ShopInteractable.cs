using System.Collections.Generic;
using UnityEngine;

public class ShopInteractable : MonoBehaviour, IInteractable
{
    [Header("leave empty to use Randomizer")]
    [SerializeField] private TestModuleEntry[] shopEntries;

    [Header("Randomizer Settings")]
    [SerializeField] private bool useRandomizer = true;
    [SerializeField] private int count = 3;
    [SerializeField] private float midCost = 100f;
    [SerializeField] private float cheapSd = 30f;
    [SerializeField] private float expensiveSd = 30f;
    [SerializeField] private bool allowDuplicates = false;
    [SerializeField] private float currentModuleDupChance = 0.1f;

    [Header("References")]
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private UIManager uiManager;

    private readonly HashSet<int> _soldIndices = new HashSet<int>();
    private TestModuleEntry[] _generatedEntries;

    public string GetPromptText() => "[ E ]  Open Shop";

    private void Start()
    {
        GenerateEntries();
    }
    private void Update()
    {
        if (shopUI == null)
            shopUI = Object.FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        if (uiManager == null)
            uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
    }

    public void SetRandomizerSettings(
        int count,
        float midCost,
        float cheapSd,
        float expensiveSd,
        bool allowDuplicates = false,
        float currentModuleDupChance = 0.1f,
        bool regenerate = true)
    {
        this.count = count;
        this.midCost = midCost;
        this.cheapSd = cheapSd;
        this.expensiveSd = expensiveSd;
        this.allowDuplicates = allowDuplicates;
        this.currentModuleDupChance = currentModuleDupChance;
        this.useRandomizer = true;

        if (regenerate)
        {
            _soldIndices.Clear();
            GenerateEntries();
        }
    }

    public void Regenerate()
    {
        _soldIndices.Clear();
        GenerateEntries();
    }

    private void GenerateEntries()
    {
        if (useRandomizer)
        {
            var (cheap, mid, expensive) = Randomizer.ShopRoll(
                midCost, cheapSd, expensiveSd,
                count, allowDuplicates, currentModuleDupChance);

            var all = new List<TestModuleEntry>();
            all.AddRange(cheap);
            all.AddRange(mid);
            all.AddRange(expensive);
            _generatedEntries = all.ToArray();
        }
        else
        {
            _generatedEntries = shopEntries;
        }
    }

    public void Interact(PlayerController playerController)
    {

        uiManager.OpenShop(shopUI);
        shopUI.Populate(_generatedEntries, _soldIndices, this);
    }

    public void RegisterSold(int index)
    {
        _soldIndices.Add(index);
    }
}