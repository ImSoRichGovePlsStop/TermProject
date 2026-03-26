using UnityEngine;

public class GamblerStation : MonoBehaviour, IInteractable
{
    [Header("Config")]
    [SerializeField] private BuildingData buildingData;
    [SerializeField] private GenericTreeConfig treeConfig;

    private UIManager uiManager;
    private GamblerManager manager;
    private BuildingLevelManager levelManager;

    private void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        manager = FindFirstObjectByType<GamblerManager>(FindObjectsInactive.Include);
        levelManager = FindFirstObjectByType<BuildingLevelManager>(FindObjectsInactive.Include);
    }

    public void Interact(PlayerController playerController)
    {
        if (uiManager == null || treeConfig == null) return;
        uiManager.OpenGambler(treeConfig, buildingData, this);
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

    public string GetPromptText() => "[ E ]  Open Gambler Station";
}