using System.Collections.Generic;
using UnityEngine;

public class ShopInteractable : MonoBehaviour, IInteractable
{
    [Header("leave empty to use Randomizer")]
    [SerializeField] private TestModuleEntry[] shopEntries;

    [Header("Randomizer Settings")]
    [SerializeField] private bool useRandomizer = true;
    [SerializeField] private int minCount = 3;
    [SerializeField] private int maxCount = 6;
    [SerializeField] private float meanCost = 100f;
    [SerializeField] private float sd = 30f;
    [SerializeField] private bool allowDuplicates = false;

    [Header("References")]
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private UIManager uiManager;

    private readonly HashSet<int> _soldIndices = new HashSet<int>();
    private TestModuleEntry[] _generatedEntries;

    private void Start()
    {
        GenerateEntries();
    }

    public void SetRandomizerSettings(
        int minCount,
        int maxCount,
        float meanCost,
        float sd,
        bool allowDuplicates = false,
        bool regenerate = true)
    {
        this.minCount = minCount;
        this.maxCount = maxCount;
        this.meanCost = meanCost;
        this.sd = sd;
        this.allowDuplicates = allowDuplicates;
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
            var rolled = Randomizer.Roll(minCount, maxCount, meanCost, sd, allowDuplicates);
            _generatedEntries = rolled.ToArray();
        }
        else
        {
            _generatedEntries = shopEntries;
        }
    }

    public void Interact(PlayerController playerController)
    {
        if (shopUI == null) { Debug.LogError("[ShopInteractable] ShopUI is missing!"); return; }
        if (uiManager == null) { Debug.LogError("[ShopInteractable] UIManager is missing!"); return; }

        uiManager.OpenShop(shopUI);
        shopUI.Populate(_generatedEntries, _soldIndices, this);
    }

    public void RegisterSold(int index)
    {
        _soldIndices.Add(index);
    }
}