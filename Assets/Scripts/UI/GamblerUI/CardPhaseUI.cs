using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CardPhaseUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private List<CardUI> cards;
    [SerializeField] private GameObject continuePrompt;

    [Header("Card Distribution")]
    [SerializeField] private int positiveCount = 3;
    [SerializeField] private int negativeCount = 3;

    [Header("Permanent Buff")]
    [SerializeField] private bool givePermanentBuff = true;
    [SerializeField] private Sprite noBuffCardBackSprite;

    [Header("Card Pool")]
    [SerializeField] private List<BuffCardData> cardPool;

    [Header("Card Backs")]
    [SerializeField] private List<PermanentBuffSlot> cardBackSlots;

    [Header("Aura")]
    [SerializeField] private bool showAura = true;

    [Header("Reroll")]
    [SerializeField] private int maxReroll = 2;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollText;

    [System.Serializable]
    public class PermanentBuffSlot
    {
        public PermanentBuffType buffType;
        public Sprite backSprite;
    }

    private BuffCardData selectedCard;
    private CardUI selectedCardUI;
    private bool waitingForContinue = false;
    private int rerollCount = 0;

    private void Awake()
    {
        gameObject.SetActive(false);
        panel.SetActive(false);
        continuePrompt.SetActive(false);

        if (rerollButton != null)
            rerollButton.onClick.AddListener(OnReroll);

        foreach (var card in cards)
            card.OnCardClicked += OnCardSelected;
    }

    private void Update()
    {
        if (!waitingForContinue) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (rerollButton != null && rerollButton.interactable)
        {
            var pointerData = new UnityEngine.EventSystems.PointerEventData(
                UnityEngine.EventSystems.EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };
            var results = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                if (result.gameObject == rerollButton.gameObject)
                    return;
            }
        }

        OnContinueClick();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        selectedCard = null;
        selectedCardUI = null;
        waitingForContinue = false;
        rerollCount = maxReroll;

        panel.SetActive(true);
        continuePrompt.SetActive(false);

        SetRerollButtonState(false);
        DealCards();
    }

    public void Close()
    {
        panel.SetActive(false);
        continuePrompt.SetActive(false);
        SetRerollButtonState(false);
        gameObject.SetActive(false);
    }

    private void DealCards()
    {
        var positivePool = new List<BuffCardData>();
        var negativePool = new List<BuffCardData>();
        foreach (var card in cardPool)
        {
            if (card.buffType == BuffType.ExtremePositive || card.buffType == BuffType.Positive)
                positivePool.Add(card);
            else
                negativePool.Add(card);
        }
        Shuffle(positivePool);
        Shuffle(negativePool);

        var selected = new List<BuffCardData>();
        for (int i = 0; i < positiveCount && i < positivePool.Count; i++)
            selected.Add(positivePool[i]);
        for (int i = 0; i < negativeCount && i < negativePool.Count; i++)
            selected.Add(negativePool[i]);
        Shuffle(selected);

        var slots = new List<PermanentBuffSlot>(cardBackSlots);

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < selected.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Reset();

                Sprite backSprite;
                PermanentBuffType buffType = PermanentBuffType.MaxHp;

                if (givePermanentBuff)
                {
                    var slot = slots.Count > 0 ? slots[Random.Range(0, slots.Count)] : null;
                    backSprite = slot?.backSprite;
                    buffType = slot != null ? slot.buffType : PermanentBuffType.MaxHp;
                }
                else
                {
                    backSprite = noBuffCardBackSprite;
                }

                cards[i].Setup(selected[i], backSprite, buffType, showAura);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnCardSelected(CardUI card)
    {
        if (waitingForContinue) return;

        selectedCard = card.GetData();
        selectedCardUI = card;

        foreach (var c in cards)
            c.SetInteractable(false);

        foreach (var c in cards)
            if (c != card)
            {
                c.DimUnselected();
                c.StopAuraPublic();
            }

        card.SetHoverable(true);

        card.Flip(() =>
        {
            card.ScaleUp();
            waitingForContinue = true;
            continuePrompt.SetActive(true);
            SetRerollButtonState(rerollCount > 0);
        });
    }

    private void OnReroll()
    {
        if (rerollCount <= 0) return;

        rerollCount--;
        waitingForContinue = false;
        selectedCard = null;
        selectedCardUI = null;

        continuePrompt.SetActive(false);
        SetRerollButtonState(false);

        DealCards();
    }

    private void SetRerollButtonState(bool interactable)
    {
        if (rerollButton == null) return;
        rerollButton.gameObject.SetActive(true);
        rerollButton.interactable = interactable;
        if (rerollText != null)
            rerollText.text = rerollCount > 0 ? $"Reroll ({rerollCount})" : "No Reroll";
    }

    private void OnContinueClick()
    {
        if (!waitingForContinue) return;
        waitingForContinue = false;
        selectedCard?.Apply();
        if (givePermanentBuff)
            ApplyPermanentBuff();
        Close();
    }

    private void ApplyPermanentBuff()
    {
        if (selectedCardUI == null) return;

        bool isExtreme = selectedCard != null &&
            (selectedCard.buffType == BuffType.ExtremePositive ||
             selectedCard.buffType == BuffType.ExtremeNegative);

        string prefix = isExtreme ? "+Extreme" : "+";

        switch (selectedCardUI.GetPermanentBuffType())
        {
            case PermanentBuffType.MaxHp:
                Debug.Log($"[PermanentBuff] {prefix}MaxHp");
                break;
            case PermanentBuffType.AttackDamage:
                Debug.Log($"[PermanentBuff] {prefix}AttackDamage");
                break;
            case PermanentBuffType.AttackSpeed:
                Debug.Log($"[PermanentBuff] {prefix}AttackSpeed");
                break;
            case PermanentBuffType.MoveSpeed:
                Debug.Log($"[PermanentBuff] {prefix}MoveSpeed");
                break;
            case PermanentBuffType.CritChance:
                Debug.Log($"[PermanentBuff] {prefix}CritChance");
                break;
            case PermanentBuffType.CritDamage:
                Debug.Log($"[PermanentBuff] {prefix}CritDamage");
                break;
            case PermanentBuffType.EvadeChance:
                Debug.Log($"[PermanentBuff] {prefix}EvadeChance");
                break;
            default:
                Debug.Log($"[PermanentBuff] Unknown: {selectedCardUI.GetPermanentBuffType()}");
                break;
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}