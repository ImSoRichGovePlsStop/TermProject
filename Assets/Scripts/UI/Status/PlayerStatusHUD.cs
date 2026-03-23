using System.Collections.Generic;
using UnityEngine;

public class PlayerStatusHUD : MonoBehaviour
{
    public static PlayerStatusHUD Instance { get; private set; }

    [SerializeField] private StatusIconUI iconPrefab;
    [SerializeField] private Transform iconContainer;

    private Dictionary<string, StatusEntry> registry = new();
    private Dictionary<string, StatusIconUI> icons = new();

    private void Awake() => Instance = this;

    public void Register(StatusEntry entry) => registry[entry.id] = entry;

    public void Unregister(string id)
    {
        registry.Remove(id);
        if (icons.TryGetValue(id, out var ui))
        {
            Destroy(ui.gameObject);
            icons.Remove(id);
        }
    }

    public void Refresh(string id)
    {
        if (!registry.TryGetValue(id, out var entry)) return;

        if (!icons.TryGetValue(id, out var ui))
        {
            ui = Instantiate(iconPrefab, iconContainer);
            icons[id] = ui;
        }
        ui.UpdateUI(entry);
    }
}