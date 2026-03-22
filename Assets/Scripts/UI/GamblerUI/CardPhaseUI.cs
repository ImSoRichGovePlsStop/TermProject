using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardPhaseUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private List<CardUI> cards;
    [SerializeField] private GameObject continuePrompt;

    [Header("Card Pool")]
    [SerializeField] private List<BuffCardData> cardPool;

    [Header("Card Backs")]
    [SerializeField] private List<PermanentBuffSlot> cardBackSlots;

    [System.Serializable]
    public class PermanentBuffSlot
    {
        public PermanentBuffType buffType;
        public Sprite backSprite;
    }

    private BuffCardData selectedCard;
    private CardUI selectedCardUI;
    private bool waitingForContinue = false;

    private void Awake()
    {
        gameObject.SetActive(false);
        panel.SetActive(false);
        continuePrompt.SetActive(false);

        foreach (var card in cards)
            card.OnCardClicked += OnCardSelected;
    }


    private void Update()
    {
        if (!waitingForContinue) return;
        if (Mouse.current.leftButton.wasPressedThisFrame)
            OnContinueClick();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        selectedCard = null;
        selectedCardUI = null;
        waitingForContinue = false;

        panel.SetActive(true);
        continuePrompt.SetActive(false);

        DealCards();
    }

    public void Close()
    {
        panel.SetActive(false);
        continuePrompt.SetActive(false);
        gameObject.SetActive(false);
    }

    private void DealCards()
    {
        var pool = new List<BuffCardData>(cardPool);
        Shuffle(pool);

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < pool.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Reset();

                var slot = cardBackSlots.Count > 0
                    ? cardBackSlots[Random.Range(0, cardBackSlots.Count)]
                    : null;

                Sprite backSprite = slot?.backSprite;
                PermanentBuffType buffType = slot != null ? slot.buffType : PermanentBuffType.MaxHp;
                cards[i].Setup(pool[i], backSprite, buffType);
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
            if (c != card) c.DimUnselected();

        card.SetHoverable(true);

        card.Flip(() =>
        {
            card.ScaleUp();
            waitingForContinue = true;
            continuePrompt.SetActive(true);
        });
    }

    private void OnContinueClick()
    {
        if (!waitingForContinue) return;
        waitingForContinue = false;
        selectedCard?.Apply();
        ApplyPermanentBuff();
        Close();
    }

    private void ApplyPermanentBuff()
    {
        if (selectedCardUI == null) return;
        switch (selectedCardUI.GetPermanentBuffType())
        {
            case PermanentBuffType.MaxHp:
                Debug.Log("[PermanentBuff] +MaxHp");
                break;
            case PermanentBuffType.AttackDamage:
                Debug.Log("[PermanentBuff] +AttackDamage");
                break;
            case PermanentBuffType.AttackSpeed:
                Debug.Log("[PermanentBuff] +AttackSpeed");
                break;
            case PermanentBuffType.MoveSpeed:
                Debug.Log("[PermanentBuff] +MoveSpeed");
                break;
            case PermanentBuffType.CritChance:
                Debug.Log("[PermanentBuff] +CritChance");
                break;
            case PermanentBuffType.CritDamage:
                Debug.Log("[PermanentBuff] +CritDamage");
                break;
            case PermanentBuffType.EvadeChance:
                Debug.Log("[PermanentBuff] +EvadeChance");
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