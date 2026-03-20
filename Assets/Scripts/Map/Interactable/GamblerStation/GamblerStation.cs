using UnityEngine;

public class GamblerStation : MonoBehaviour, IInteractable
{
    [Header("Config")]
    [SerializeField] private BuildingData buildingData;
    [SerializeField] private GenericTreeConfig treeConfig;

    private GamblerScreenUI screenUI;
    private GamblerManager manager;
    private BuildingLevelManager levelManager;

    private void Start()
    {
        screenUI = FindFirstObjectByType<GamblerScreenUI>(FindObjectsInactive.Include);
        manager = FindFirstObjectByType<GamblerManager>(FindObjectsInactive.Include);
        levelManager = FindFirstObjectByType<BuildingLevelManager>(FindObjectsInactive.Include);
    }

    public void Interact(PlayerController playerController)
    {
        if (screenUI == null || treeConfig == null) return;
        screenUI.Open(treeConfig, buildingData, this);
    }

    public void TryLevelUp()
    {
        if (levelManager == null || buildingData == null) return;
        levelManager.TryLevelUp(buildingData);
    }

    public int GetLevel()
    {
        if (levelManager == null || buildingData == null) return 1;
        return levelManager.GetLevel(buildingData);
    }

    public bool CanLevelUp()
    {
        if (levelManager == null || buildingData == null) return false;
        return levelManager.CanLevelUp(buildingData);
    }

    public string GetPromptText()
    {
        return $"Open {buildingData?.buildingName ?? "Gambler's Den"}";
    }
}
