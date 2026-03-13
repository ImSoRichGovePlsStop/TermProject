using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PassiveNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI nodeNameText;

    [Header("Colors")]
    [SerializeField] private Color lockedColor = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] private Color availableColor = new Color(1f, 0.85f, 0f);
    [SerializeField] private Color unlockedColor = new Color(0.4f, 0.4f, 0.4f);

    private PassiveNode node;
    private PassiveTree tree;
    private WeaponPassiveManager manager;
    private PassiveScreenUI screenUI;
    private WeaponPassiveData passiveData;

    public void Setup(PassiveNode node, PassiveTree tree, WeaponPassiveManager manager, PassiveScreenUI screenUI, WeaponPassiveData data)
    {
        this.node = node;
        this.tree = tree;
        this.manager = manager;
        this.screenUI = screenUI;
        this.passiveData = data;

        nodeNameText.text = node.nodeName;
        button.onClick.AddListener(OnClick);
        Refresh();
    }

    public void Refresh()
    {
        bool unlocked = manager.GetState(passiveData).IsUnlocked(node);
        bool available = manager.GetState(passiveData).CanUnlock(node, tree);

        if (unlocked)
            background.color = unlockedColor;
        else if (available)
            background.color = availableColor;
        else
            background.color = lockedColor;

        button.interactable = available;
    }

    private void OnClick()
    {
        if (manager.TryUnlock(node, tree, passiveData))
        {
            screenUI.RefreshAll();
            screenUI.RefreshPoints();
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        bool treeUnlocked = node.layer == 1 || manager.GetState(passiveData).IsTreeUnlocked(tree);
        if (!treeUnlocked) return;
        PassiveTooltipUI.Instance?.Show(node);
    }

    public void OnPointerExit(PointerEventData e)
    {
        PassiveTooltipUI.Instance?.Hide();
    }
}