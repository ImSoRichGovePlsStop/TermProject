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
    private Color unlockedColor = new Color(1f, 0.85f, 0f);

    private PassiveNode node;
    private PassiveTree tree;
    private WeaponPassiveManager manager;
    private PassiveScreenUI screenUI;
    private WeaponPassiveData passiveData;
    private bool tooltipAnchorLeft;

    public void Setup(PassiveNode node, PassiveTree tree, WeaponPassiveManager manager, PassiveScreenUI screenUI, WeaponPassiveData data, bool tooltipAnchorLeft = false)
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
        bool unlocked = manager.GetState(passiveData).IsUnlocked(node);
        bool available = manager.GetState(passiveData).CanUnlock(node, tree);

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
        var state = manager.GetState(passiveData);
        if (state.IsUnlocked(node))
        {
            if (manager.TryRefund(node, tree, passiveData))
            {
                screenUI.RefreshAll();
                screenUI.RefreshPoints();
            }
        }
        else if (manager.TryUnlock(node, tree, passiveData))
        {
            screenUI.RefreshAll();
            screenUI.RefreshPoints();
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        PassiveTooltipUI.Instance?.Show(node, GetComponent<RectTransform>(), tooltipAnchorLeft, tree.treeColor);
    }

    public void OnPointerExit(PointerEventData e)
    {
        PassiveTooltipUI.Instance?.Hide();
    }
}