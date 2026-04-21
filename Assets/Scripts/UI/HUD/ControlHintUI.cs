using TMPro;
using UnityEngine;

/// <summary>
/// Displays context-sensitive control hints near the inventory/shop panels.
/// Attach anywhere in the HUD. Wire up the two text references in the Inspector.
///
/// contextHintText  — changes based on open panel  ([RMB] Split Stack / [RMB] Sell Item)
/// dragHintText     — shown only while dragging     ([R] Rotate)
/// </summary>
public class ControlHintUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI contextHintText;
    [SerializeField] private TextMeshProUGUI dragHintText;

    [Header("Hint Strings")]
    [SerializeField] private string splitStackHint = "[RMB]  Split Stack";
    [SerializeField] private string sellItemHint   = "[RMB]  Sell Item";
    [SerializeField] private string rotateHint     = "[R]  Rotate";

    private void Awake()
    {
        if (dragHintText    != null) dragHintText.text    = rotateHint;
        if (contextHintText != null) contextHintText.text = splitStackHint;
        SetEnabled(contextHintText, false);
        SetEnabled(dragHintText,    false);
    }

    private void Update()
    {
        var ui = UIManager.Instance;
        if (ui == null) { Hide(); return; }

        bool invOpen  = ui.IsInventoryOpen;
        bool shopOpen = ui.IsShopOpen;
        bool anyOpen  = invOpen || shopOpen;
        bool dragging = ModuleItemUI.AnyDragging;

        
        if (contextHintText != null)
        {
            bool showContext = anyOpen;
            if (invOpen && !shopOpen)
                showContext = HasSplittableStack();

            contextHintText.text = shopOpen ? sellItemHint : splitStackHint;
            SetEnabled(contextHintText, showContext);
        }

        
        SetEnabled(dragHintText, anyOpen && dragging);
    }

    void Hide()
    {
        SetEnabled(contextHintText, false);
        SetEnabled(dragHintText,    false);
    }

    static void SetEnabled(TMPro.TMP_Text text, bool value)
    {
        if (text != null) text.enabled = value;
    }

    static bool HasSplittableStack()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return false;
        foreach (var inst in inv.BagGrid.GetAllModules())
            if (inst is MaterialInstance mat && mat.StackCount > 1)
                return true;
        return false;
    }
}
