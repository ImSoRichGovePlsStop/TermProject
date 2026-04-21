using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tab panel that lists all active run/floor modifier cards in a single container.
/// Attach to the modifier tab's root panel.
/// Refreshes automatically each time the panel is enabled (tab opened).
/// </summary>
public class TabModifierUI : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private Transform container;
    [SerializeField] private GameObject emptyLabel;

    [Header("Card Prefab")]
    [Tooltip("Prefab root should have a TabModifierCardUI component, " +
             "or child objects named CardName / Description / Scope (TMP).")]
    [SerializeField] private GameObject modifierCardPrefab;

    [Header("Scope Colors")]
    [SerializeField] private Color wholeRunColor  = new Color(0.85f, 0.6f,  0.1f, 1f);
    [SerializeField] private Color nextFloorColor = new Color(0.2f,  0.6f,  0.9f, 1f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable() => Refresh();

    // ── Public API ────────────────────────────────────────────────────────────

    public void Refresh()
    {
        var rm = RunManager.Instance;

        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        var wholeRun  = rm != null ? rm.AppliedWholeRun  : new List<FloorModifierCard>();
        var nextFloor = rm != null ? rm.AppliedNextFloor : new List<FloorModifierCard>();

        bool anyCards = wholeRun.Count > 0 || nextFloor.Count > 0;
        if (emptyLabel != null) emptyLabel.SetActive(!anyCards);

        foreach (var card in wholeRun)  SpawnCard(card, wholeRunColor);
        foreach (var card in nextFloor) SpawnCard(card, nextFloorColor);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void SpawnCard(FloorModifierCard card, Color scopeColor)
    {
        if (container == null || modifierCardPrefab == null) return;

        var go = Instantiate(modifierCardPrefab, container);

        var cardUI = go.GetComponent<TabModifierCardUI>();
        if (cardUI != null)
        {
            cardUI.Bind(card, scopeColor);
            return;
        }

        // Fallback: drive child TMP texts by name convention.
        SetChildText(go, "CardName",    card.displayName);
        SetChildText(go, "Description", card.description);
        SetChildText(go, "Scope",       card.scope == ModifierScope.WholeRun ? "WHOLE RUN" : "THIS FLOOR");

        var scopeText = FindChildText(go, "Scope");
        if (scopeText != null) scopeText.color = scopeColor;

        var scopeBg = go.transform.Find("ScopeBadge")?.GetComponent<Image>();
        if (scopeBg != null) scopeBg.color = scopeColor;
    }

    static void SetChildText(GameObject root, string childName, string value)
    {
        var t = FindChildText(root, childName);
        if (t != null) t.text = value;
    }

    static TextMeshProUGUI FindChildText(GameObject root, string childName)
    {
        var tf = root.transform.Find(childName);
        return tf != null ? tf.GetComponent<TextMeshProUGUI>() : null;
    }
}
