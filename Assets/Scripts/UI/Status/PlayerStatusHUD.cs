using System.Collections.Generic;
using UnityEngine;

public class PlayerStatusHUD : MonoBehaviour
{
    public static PlayerStatusHUD Instance { get; private set; }

    [SerializeField] private StatusIconUI iconPrefab;
    [SerializeField] private Transform iconContainer;

    private Dictionary<string, StatusEntry> _registry = new();
    private Dictionary<string, StatusIconUI> _icons = new();

    private void Awake() => Instance = this;

    public void Register(StatusEntry entry) => _registry[entry.id] = entry;

    public void Refresh(string id)
    {
        if (!_registry.TryGetValue(id, out var entry)) return;

        if (!_icons.TryGetValue(id, out var ui))
        {
            ui = Instantiate(iconPrefab, iconContainer);
            _icons[id] = ui;
        }
        ui.UpdateUI(entry);
    }
}