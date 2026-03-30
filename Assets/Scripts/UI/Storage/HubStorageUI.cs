using UnityEngine;
using UnityEngine.UI;

public class HubStorageUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private StorageItemUI storageItemPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private ScrollRect scrollRect;

    public bool IsOpen { get; private set; }
    private bool pendingScrollReset;

    private void Awake()
    {
        panel.SetActive(false);
    }

    private void Update()
    {
        if (!pendingScrollReset) return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
        pendingScrollReset = false;
    }

    public void Open()
    {
        IsOpen = true;
        panel.SetActive(true);
        Populate();
        pendingScrollReset = true;
    }

    public void Close()
    {
        IsOpen = false;
        panel.SetActive(false);
    }

    private void Populate()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
        foreach (var kvp in MaterialStorage.Instance.GetAll())
            Instantiate(storageItemPrefab, content).Init(kvp.Key, kvp.Value);
    }
}
