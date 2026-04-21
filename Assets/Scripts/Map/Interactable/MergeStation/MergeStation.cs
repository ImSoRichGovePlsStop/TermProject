using UnityEngine;

public class MergeStation : MonoBehaviour, IInteractable
{
    private MergeUI _mergeUI;
    private UIManager _uiManager;

    public ModuleInstance CachedOutput { get; set; }
    public bool HasOutput => CachedOutput != null;

    public string GetPromptText() => "[ E ]  Reforge Items";
    public InteractInfo GetInteractInfo() => new InteractInfo
    {
        name        = "Smelter",
        description = "Smelt your items to create a new one with combined values.",
        actionText  = "Smelt",
        cost        = null
    };

    private void Start()
    {
        _mergeUI = FindFirstObjectByType<MergeUI>(FindObjectsInactive.Include);
        _uiManager = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
    }

    public void Interact(PlayerController playerController)
    {
        if (_mergeUI == null) { Debug.LogError("[MergeStation] MergeUI is missing!"); return; }
        if (_uiManager == null) { Debug.LogError("[MergeStation] UIManager is missing!"); return; }
        _uiManager.OpenMerge(_mergeUI, this);
    }
}