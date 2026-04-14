using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class HubStorageUI : MonoBehaviour
{
    [SerializeField] private GameObject    panel;
    [SerializeField] private StorageItemUI storageItemPrefab;

    private const string LabelToUpgrade = "Upgrade BagGrid";
    private const string LabelToStorage = "\u25C4  Storage";

    public bool IsOpen { get; private set; }
    private bool _isShowingUpgrade;
    private bool _pendingScrollReset;
    private int  _lastSwitchFrame = -1;

    private GameObject       Panel       => transform.Find("StoragePanel")?.gameObject ?? panel;
    private GameObject       ContentRoot => transform.Find("StoragePanel/StorageContentRoot")?.gameObject;
    private BagGridUpgradeUI UpgradePanel => transform.Find("StoragePanel/BagGridUpgradePanel")?.GetComponent<BagGridUpgradeUI>();
    private TextMeshProUGUI  SwitchLabel => transform.Find("StoragePanel/SwitchButton/Label")?.GetComponent<TextMeshProUGUI>();
    private ScrollRect       Scroll      => GetComponentInChildren<ScrollRect>(true);
    private Transform        Content     => transform.Find("StoragePanel/StorageContentRoot/Scroll/Viewport/Content");

    private void Awake()
    {
        Panel?.SetActive(false);
        UpgradePanel?.gameObject.SetActive(false);

        // Wire button in code so it always calls THIS instance regardless of Inspector setup
        var sbTf = transform.Find("StoragePanel/SwitchButton");
        var btn  = sbTf?.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnSwitchButtonClicked);
        }
    }

    private void Update()
    {
        if (IsOpen)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame && _isShowingUpgrade)
                    OnSwitchButtonClicked();
                else if (kb.rightArrowKey.wasPressedThisFrame && !_isShowingUpgrade)
                    OnSwitchButtonClicked();
            }
        }

        if (!_pendingScrollReset) return;
        Canvas.ForceUpdateCanvases();
        var sr = Scroll;
        if (sr != null) sr.verticalNormalizedPosition = 1f;
        _pendingScrollReset = false;
    }

    public void Open()
    {
        IsOpen = true;
        Panel?.SetActive(true);
        ShowStorageView();
        _pendingScrollReset = true;
    }

    public void Close()
    {
        IsOpen = false;
        Panel?.SetActive(false);
        _isShowingUpgrade = false;
    }

    public void OnSwitchButtonClicked()
    {
        if (Time.frameCount == _lastSwitchFrame) return;
        _lastSwitchFrame = Time.frameCount;
        if (_isShowingUpgrade) ShowStorageView();
        else                   ShowUpgradeView();
    }

    private void ShowStorageView()
    {
        _isShowingUpgrade = false;
        ContentRoot?.SetActive(true);
        UpgradePanel?.gameObject.SetActive(false);
        var lbl = SwitchLabel;
        if (lbl != null) lbl.text = LabelToUpgrade;
        Populate();
        _pendingScrollReset = true;
    }

    private void ShowUpgradeView()
    {
        _isShowingUpgrade = true;
        ContentRoot?.SetActive(false);
        UpgradePanel?.gameObject.SetActive(true);
        var lbl = SwitchLabel;
        if (lbl != null) lbl.text = LabelToStorage;
    }

    private void Populate()
    {
        var c = Content;
        if (c == null) return;

        foreach (Transform child in c)
            Destroy(child.gameObject);

        foreach (var kvp in MaterialStorage.Instance.GetAll())
        {
            var item = Instantiate(storageItemPrefab);
            item.transform.SetParent(c, false);
            item.Init(kvp.Key, kvp.Value);
        }
    }
}
