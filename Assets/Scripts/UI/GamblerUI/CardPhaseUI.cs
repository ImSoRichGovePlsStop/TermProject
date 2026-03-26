using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CardPhaseUI : MonoBehaviour
{
    // Drop rate constants
    private const float NormalModeNormalWeight = 0.8f;
    private const float HighRollerNormalWeight = 0.5f;

    // Devil's Bet constants
    private const float BetSafeHpPercent = 0f;
    private const float BetSafeMultiplier = 1f;
    private const float BetRiskHpPercent = 0.4f;
    private const float BetRiskMultiplier = 2f;
    private const float BetDespairHpPercent = 0.8f;
    private const float BetDespairMultiplier = 3f;

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private List<CardUI> cards;
    [SerializeField] private GameObject continuePrompt;

    [Header("Card Distribution")]
    [SerializeField] private int positiveCount = 3;
    [SerializeField] private int negativeCount = 3;

    [Header("Permanent Buff")]
    [SerializeField] private bool givePermanentBuff = false;
    [SerializeField] private Sprite noBuffCardBackSprite;

    [Header("Card Backs")]
    [SerializeField] private List<PermanentBuffSlot> cardBackSlots;

    [Header("Aura")]
    [SerializeField] private bool showAura = false;

    [Header("Reroll")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollText;

    [Header("Peek (L5A)")]
    [SerializeField] private Button peekButton;
    [SerializeField] private TextMeshProUGUI peekText;

    [Header("Devil's Bet (L4B)")]
    [SerializeField] private GameObject devilsBetPanel;
    [SerializeField] private Button betSafeButton;
    [SerializeField] private Button betRiskButton;
    [SerializeField] private Button betDespairButton;

    [System.Serializable]
    public class PermanentBuffSlot
    {
        public Sprite backSprite;
        public StatModifier normalBuff;
        public StatModifier extremeBuff;

        [Tooltip("Set variance")]
        [Range(0f, 0.5f)] public float variance = 0.2f;

        public StatModifier GetRandomized(bool isExtreme)
        {
            var source = isExtreme ? extremeBuff : normalBuff;
            if (source == null) return null;

            return new StatModifier
            {
                health = Randomize(source.health, variance),
                damage = Randomize(source.damage, variance),
                attackSpeed = Randomize(source.attackSpeed, variance),
                moveSpeed = Randomize(source.moveSpeed, variance),
                critChance = Randomize(source.critChance, variance),
                critDamage = Randomize(source.critDamage, variance),
                evadeChance = Randomize(source.evadeChance, variance),
                damageTaken = Randomize(source.damageTaken, variance),
            };
        }

        private static float Randomize(float value, float variance)
        {
            if (value == 0f) return 0f;
            return value * UnityEngine.Random.Range(1f - variance, 1f + variance);
        }
    }

    private BuffCardData selectedCard;
    private CardUI selectedCardUI;
    private bool waitingForContinue = false;
    private int currentPositiveCount = 3;
    private int currentNegativeCount = 3;
    private int peekRemaining = 0;
    private bool peekMode = false;

    // Devil's Bet
    private bool hasDevilsBetUnlocked = false;
    private float betMultiplier = BetSafeMultiplier;
    private float betHpPercent = BetSafeHpPercent;

    private GamblerCardPhaseConfig activeConfig;
    private bool cardPhaseUnlocked = false;
    public bool CardPhaseUnlocked => cardPhaseUnlocked;
    public bool IsOpen { get; private set; }

    private List<BuffCardData> starterPool = new();
    private List<BuffCardData> extendedPool = new();

    public void Configure(GamblerCardPhaseConfig config)
    {
        activeConfig = config;
        cardPhaseUnlocked = config.cardPhaseEnabled;

        givePermanentBuff = config.givePermanentBuff || config.loadedDeckVariant;
        showAura = config.showAura;
        hasDevilsBetUnlocked = config.hasDevilsBet;

        LoadCardPools(config.useExtendedPool);

        if (peekButton != null)
            peekButton.gameObject.SetActive(config.hasPeek);

        if (devilsBetPanel != null)
            devilsBetPanel.SetActive(false);
    }

    public void AddCardsFromFolder(string folderPath)
    {
        var loaded = Resources.LoadAll<BuffCardData>(folderPath);
        starterPool.AddRange(loaded);
    }

    private void LoadCardPools(bool includeExtended)
    {
        starterPool.Clear();
        extendedPool.Clear();

        var starter = Resources.LoadAll<BuffCardData>("BuffCard/Starter");
        starterPool.AddRange(starter);

        if (includeExtended)
        {
            var extended = Resources.LoadAll<BuffCardData>("BuffCard/Extended");
            extendedPool.AddRange(extended);
        }
    }

    private void Awake()
    {
        gameObject.SetActive(false);
        panel.SetActive(false);
        continuePrompt.SetActive(false);

        if (rerollButton != null)
            rerollButton.onClick.AddListener(OnReroll);

        if (peekButton != null)
        {
            peekButton.onClick.AddListener(OnPeekClick);
            peekButton.gameObject.SetActive(false);
        }

        if (devilsBetPanel != null)
            devilsBetPanel.SetActive(false);

        if (betSafeButton != null)
            betSafeButton.onClick.AddListener(() => OnDevilsBetChosen(BetSafeHpPercent, BetSafeMultiplier));
        if (betRiskButton != null)
            betRiskButton.onClick.AddListener(() => OnDevilsBetChosen(BetRiskHpPercent, BetRiskMultiplier));
        if (betDespairButton != null)
            betDespairButton.onClick.AddListener(() => OnDevilsBetChosen(BetDespairHpPercent, BetDespairMultiplier));

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
        if (!cardPhaseUnlocked) return;
        IsOpen = true;

        LoadCardPools(activeConfig?.useExtendedPool == true);

        gameObject.SetActive(true);
        selectedCard = null;
        selectedCardUI = null;
        waitingForContinue = false;
        betMultiplier = BetSafeMultiplier;
        betHpPercent = BetSafeHpPercent;
        currentPositiveCount = activeConfig?.initialPositiveCount ?? positiveCount;
        currentNegativeCount = activeConfig?.initialNegativeCount ?? negativeCount;
        peekRemaining = activeConfig?.hasPeek == true ? activeConfig.peekCount : 0;
        peekMode = false;

        panel.SetActive(true);
        continuePrompt.SetActive(false);
        SetRerollButtonState(false);

        if (peekButton != null)
        {
            peekButton.gameObject.SetActive(activeConfig?.hasPeek == true);
            peekButton.interactable = peekRemaining > 0;
            UpdatePeekText();
        }

        if (hasDevilsBetUnlocked && devilsBetPanel != null)
            devilsBetPanel.SetActive(true);
        else
            DealCards();
    }

    public void Close()
    {
        IsOpen = false;
        panel.SetActive(false);
        continuePrompt.SetActive(false);
        SetRerollButtonState(false);
        if (devilsBetPanel != null) devilsBetPanel.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnDevilsBetChosen(float hpPercent, float multiplier)
    {
        betHpPercent = hpPercent;
        betMultiplier = multiplier;

        devilsBetPanel.SetActive(false);
        DealCards();
    }

    private void DealCards()
    {
        var fullPool = new List<BuffCardData>(starterPool);
        fullPool.AddRange(extendedPool);
        if (fullPool.Count == 0) return;

        var positivePool = new List<BuffCardData>();
        var negativePool = new List<BuffCardData>();
        var extremePosPool = new List<BuffCardData>();
        var extremeNegPool = new List<BuffCardData>();

        foreach (var card in fullPool)
        {
            switch (card.buffType)
            {
                case BuffType.ExtremePositive: extremePosPool.Add(card); break;
                case BuffType.Positive: positivePool.Add(card); break;
                case BuffType.Negative: negativePool.Add(card); break;
                case BuffType.ExtremeNegative: extremeNegPool.Add(card); break;
            }
        }

        float normalWeight = activeConfig?.highRollerMode == true
            ? HighRollerNormalWeight
            : NormalModeNormalWeight;

        int totalCards = currentPositiveCount + currentNegativeCount;
        var selected = new List<BuffCardData>();

        Shuffle(positivePool);
        Shuffle(negativePool);
        Shuffle(extremePosPool);
        Shuffle(extremeNegPool);

        int posIdx = 0, negIdx = 0, exPosIdx = 0, exNegIdx = 0;

        for (int i = 0; i < totalCards; i++)
        {
            bool isPositiveSide = i < currentPositiveCount;
            bool isExtreme = Random.value > normalWeight;

            BuffCardData card = null;

            if (isPositiveSide)
            {
                if (isExtreme && exPosIdx < extremePosPool.Count)
                    card = extremePosPool[exPosIdx++];
                else if (posIdx < positivePool.Count)
                    card = positivePool[posIdx++];
                else if (exPosIdx < extremePosPool.Count)
                    card = extremePosPool[exPosIdx++];
            }
            else
            {
                if (isExtreme && exNegIdx < extremeNegPool.Count)
                    card = extremeNegPool[exNegIdx++];
                else if (negIdx < negativePool.Count)
                    card = negativePool[negIdx++];
                else if (exNegIdx < extremeNegPool.Count)
                    card = extremeNegPool[exNegIdx++];
            }

            if (card != null)
                selected.Add(card);
        }

        Shuffle(selected);

        var slots = new List<PermanentBuffSlot>(cardBackSlots);
        Shuffle(slots);

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < selected.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Reset();

                Sprite backSprite;
                PermanentBuffSlot chosenSlot = null;

                if (givePermanentBuff && slots.Count > 0)
                {
                    chosenSlot = slots[i % slots.Count];
                    backSprite = chosenSlot.backSprite;
                }
                else
                {
                    backSprite = noBuffCardBackSprite;
                }

                cards[i].Setup(selected[i], backSprite, chosenSlot, showAura);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnPeekClick()
    {
        if (peekRemaining <= 0 || waitingForContinue) return;
        peekMode = true;
        peekRemaining--;
        UpdatePeekText();

        foreach (var card in cards)
            card.SetPeekMode(true);

        if (peekButton != null)
            peekButton.interactable = false;
    }

    private void UpdatePeekText()
    {
        if (peekText != null)
            peekText.text = $"Peek ({peekRemaining})";
    }

    private void OnCardSelected(CardUI card)
    {
        if (peekMode)
        {
            card.PeekReveal(() =>
            {
                foreach (var c in cards)
                    c.SetPeekMode(false);
                peekMode = false;
            });
            return;
        }

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
            SetRerollButtonState(true);
        });
    }

    private void OnReroll()
    {
        int basePos = activeConfig?.initialPositiveCount ?? positiveCount;
        int baseNeg = activeConfig?.initialNegativeCount ?? negativeCount;

        currentPositiveCount = Mathf.Max(1, currentPositiveCount - 1);
        currentNegativeCount = Mathf.Min(basePos + baseNeg - 1, currentNegativeCount + 1);

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
        bool hasReroll = activeConfig?.hasReroll == true;
        rerollButton.gameObject.SetActive(hasReroll);
        rerollButton.interactable = interactable;
        if (rerollText != null)
            rerollText.text = "Reroll";
    }

    private void OnContinueClick()
    {
        if (!waitingForContinue) return;
        waitingForContinue = false;

        if (betMultiplier > BetSafeMultiplier && selectedCard != null)
            ApplyDevilsBetResult(selectedCard.buffType);

        selectedCard?.Apply();

        if (givePermanentBuff)
            ApplyPermanentBuff();

        Close();
    }

    private void ApplyDevilsBetResult(BuffType type)
    {
        var stats = FindFirstObjectByType<PlayerStats>();
        if (stats == null) return;

        bool isPositive = type == BuffType.Positive || type == BuffType.ExtremePositive;

        if (isPositive)
        {
            Debug.Log($"[DevilsBet] Win! buff multiplier x{betMultiplier}");
        }
        else
        {
            float damage = stats.MaxHealth * betHpPercent;
            stats.TakeDamage(damage);
            Debug.Log($"[DevilsBet] Lose! TakeDamage {damage} ({betHpPercent * 100f}% MaxHP)");
        }
    }

    private void ApplyPermanentBuff()
    {
        if (selectedCardUI == null) return;

        var slot = selectedCardUI.GetPermanentBuffSlot();
        if (slot == null) return;

        var stats = FindFirstObjectByType<PlayerStats>();
        if (stats == null) return;

        bool isExtreme = selectedCard != null &&
            (selectedCard.buffType == BuffType.ExtremePositive ||
             selectedCard.buffType == BuffType.ExtremeNegative);

        var modifier = slot.GetRandomized(isExtreme);
        if (modifier == null) return;

        if (betMultiplier > BetSafeMultiplier)
        {
            bool isPositive = selectedCard.buffType == BuffType.Positive ||
                              selectedCard.buffType == BuffType.ExtremePositive;
            if (isPositive)
                modifier = MultiplyModifier(modifier, betMultiplier);
        }

        stats.AddFlatModifier(modifier);
        Debug.Log($"[PermanentBuff] Applied {(isExtreme ? "extreme" : "normal")} buff x{betMultiplier}");
    }

    private static StatModifier MultiplyModifier(StatModifier m, float multiplier)
    {
        return new StatModifier
        {
            health = m.health * multiplier,
            damage = m.damage * multiplier,
            attackSpeed = m.attackSpeed * multiplier,
            moveSpeed = m.moveSpeed * multiplier,
            critChance = m.critChance * multiplier,
            critDamage = m.critDamage * multiplier,
            evadeChance = m.evadeChance * multiplier,
            damageTaken = m.damageTaken * multiplier,
        };
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