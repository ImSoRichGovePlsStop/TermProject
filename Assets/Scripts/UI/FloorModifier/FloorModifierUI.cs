using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Overlay panel shown before a floor transition on trigger floors.
/// The component lives on the panel root — uses gameObject.SetActive() on itself.
/// Found via FindFirstObjectByType from RunManager's FloorTransitionRoutine.
/// </summary>
public class FloorModifierUI : MonoBehaviour
{
    [SerializeField] private Transform       cardRow;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject      cardPrefab;
    [SerializeField] private string          headerText = "Choose a Boon";

    private bool _picked;

    void Awake() => gameObject.SetActive(false);

    public IEnumerator ShowSelection(FloorModifierCard[] options)
    {
        if (options == null || options.Length == 0) yield break;
        if (cardPrefab == null)
        {
            Debug.LogWarning("[FloorModifierUI] cardPrefab not assigned.");
            yield break;
        }

        foreach (Transform child in cardRow) Destroy(child.gameObject);

        if (titleText != null) titleText.text = headerText;

        var spawnedCards = new FloorModifierCardUI[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            var go   = Instantiate(cardPrefab, cardRow);
            var card = go.GetComponent<FloorModifierCardUI>();
            card.Bind(options[i]);
            card.OnPicked += HandlePick;
            spawnedCards[i] = card;
        }

        _picked = false;
        gameObject.SetActive(true);

        yield return new WaitUntil(() => _picked);

        foreach (var c in spawnedCards) c.SetInteractable(false);

        yield return new WaitForSecondsRealtime(0.3f);

        gameObject.SetActive(false);
        foreach (Transform child in cardRow) Destroy(child.gameObject);
    }

    private void HandlePick(FloorModifierCardUI cardUI)
    {
        if (_picked) return;
        _picked = true;
        RunManager.Instance?.ApplyModifierCard(cardUI.Data);
    }
}
