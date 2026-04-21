using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PassiveNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image background;

    private Color lockedColor = new Color(0.3f, 0.3f, 0.3f);
    private Color availableColor = new Color(0.7f, 0.7f, 0.7f);

    private GenericTreeNode node;
    private GenericTreeData tree;
    private WeaponPassiveManager manager;
    private IGenericTreeScreenUI screenUI;
    private WeaponPassiveData passiveData;
    private bool tooltipAnchorLeft;

    public void Setup(GenericTreeNode node, GenericTreeData tree, WeaponPassiveManager manager,
                  IGenericTreeScreenUI screenUI, WeaponPassiveData data, bool tooltipAnchorLeft = false)
    {
        this.node = node;
        this.tree = tree;
        this.manager = manager;
        this.screenUI = screenUI;
        this.passiveData = data;
        this.tooltipAnchorLeft = tooltipAnchorLeft;

        button.onClick.AddListener(OnClick);
        Refresh();
    }

    public void Refresh()
    {
        int pts = manager.GetAvailablePoints(passiveData);
        var state = manager.GetState(passiveData, tree);
        bool unlocked = state.IsUnlocked(node);
        bool available = state.CanUnlock(node, pts);

        if (unlocked)
            background.color = tree.treeColor;
        else if (available)
            background.color = availableColor;
        else
            background.color = lockedColor;

        button.interactable = available || unlocked;
    }

    private void OnClick()
    {
        var state = manager.GetState(passiveData, tree);
        bool changed;

        if (state.IsUnlocked(node))
            changed = manager.TryRefund(node, tree, passiveData);
        else
            changed = manager.TryUnlock(node, tree, passiveData);

        if (changed)
        {
            screenUI.RefreshAll();
            screenUI.RefreshPoints();
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        GenericTreeTooltipUI.Instance?.Show(node, GetComponent<RectTransform>(), tooltipAnchorLeft, tree.treeColor);
    }

    public void OnPointerExit(PointerEventData e)
    {
        GenericTreeTooltipUI.Instance?.Hide();
    }
}
