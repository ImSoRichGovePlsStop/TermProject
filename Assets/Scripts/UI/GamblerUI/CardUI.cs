using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private GameObject cardBack;
    [SerializeField] private GameObject cardFront;
    [SerializeField] private Image cardFrontBackground;
    [SerializeField] private Image cardHeader;
    [SerializeField] private Image iconBackground;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button cardButton;

    [Header("Flip Settings")]
    [SerializeField] private float flipDuration = 0.3f;

    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float scaleSmoothing = 8f;

    private BuffCardData data;
    private bool isFlipped = false;
    private bool isFlipping = false;
    private bool isHoverable = true;
    private bool isHovered = false;
    private PermanentBuffType permanentBuffType;

    private RectTransform rt;
    private Canvas canvas;
    private Vector3 baseScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;

    private static readonly Color ExPosBackground = new Color(0.10f, 0.18f, 0.08f);
    private static readonly Color ExPosHeader = new Color(0.16f, 0.28f, 0.08f);
    private static readonly Color ExPosBorder = new Color(0.73f, 0.46f, 0.09f);
    private static readonly Color ExPosIcon = new Color(0.73f, 0.46f, 0.09f);
    private static readonly Color ExPosName = new Color(1.00f, 0.78f, 0.30f);
    private static readonly Color ExPosDesc = new Color(0.98f, 0.88f, 0.65f);

    private static readonly Color PosBackground = new Color(0.06f, 0.16f, 0.12f);
    private static readonly Color PosHeader = new Color(0.08f, 0.24f, 0.17f);
    private static readonly Color PosBorder = new Color(0.11f, 0.62f, 0.46f);
    private static readonly Color PosIcon = new Color(0.11f, 0.62f, 0.46f);
    private static readonly Color PosName = new Color(0.62f, 0.88f, 0.79f);
    private static readonly Color PosDesc = new Color(0.75f, 0.93f, 0.87f);

    private static readonly Color NegBackground = new Color(0.16f, 0.12f, 0.25f);
    private static readonly Color NegHeader = new Color(0.22f, 0.16f, 0.34f);
    private static readonly Color NegBorder = new Color(0.50f, 0.47f, 0.87f);
    private static readonly Color NegIcon = new Color(0.50f, 0.47f, 0.87f);
    private static readonly Color NegName = new Color(0.80f, 0.78f, 0.97f);
    private static readonly Color NegDesc = new Color(0.69f, 0.66f, 0.93f);

    private static readonly Color ExNegBackground = new Color(0.16f, 0.06f, 0.06f);
    private static readonly Color ExNegHeader = new Color(0.24f, 0.08f, 0.08f);
    private static readonly Color ExNegBorder = new Color(0.64f, 0.17f, 0.17f);
    private static readonly Color ExNegIcon = new Color(0.88f, 0.29f, 0.29f);
    private static readonly Color ExNegName = new Color(0.97f, 0.70f, 0.70f);
    private static readonly Color ExNegDesc = new Color(0.94f, 0.58f, 0.58f);

    public event Action<CardUI> OnCardClicked;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        cardButton.onClick.AddListener(OnClick);
        cardFront.SetActive(false);
        cardBack.SetActive(true);
    }

    private void Update()
    {
        rt.localScale = Vector3.Lerp(rt.localScale, targetScale, Time.deltaTime * scaleSmoothing);
    }

    public void Setup(BuffCardData buffData, Sprite backSprite = null, PermanentBuffType permBuff = PermanentBuffType.MaxHp)
    {
        data = buffData;
        permanentBuffType = permBuff;
        isFlipped = false;
        isFlipping = false;
        isHoverable = true;

        cardFront.SetActive(false);
        cardBack.SetActive(true);

        var backImg = cardBack.GetComponent<Image>();
        if (backImg != null && backSprite != null)
            backImg.sprite = backSprite;

        ApplyTheme(buffData.buffType);

        if (iconImage != null)
        {
            iconImage.sprite = buffData.icon;
            iconImage.color = buffData.icon != null ? Color.white : Color.clear;
        }

        if (nameText != null)
        {
            nameText.text = buffData.buffName;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontSize = 18;
        }

        if (descriptionText != null)
        {
            descriptionText.text = buffData.description;
            descriptionText.fontStyle = FontStyles.Normal;
            descriptionText.alignment = TextAlignmentOptions.Center;
            descriptionText.fontSize = 13;
        }

        cardButton.interactable = true;
        baseScale = Vector3.one;
        targetScale = Vector3.one;
    }

    private void ApplyTheme(BuffType type)
    {
        Color bg, header, border, icon, name, desc;

        switch (type)
        {
            case BuffType.ExtremePositive:
                bg = ExPosBackground; header = ExPosHeader; border = ExPosBorder;
                icon = ExPosIcon; name = ExPosName; desc = ExPosDesc;
                break;
            case BuffType.Positive:
                bg = PosBackground; header = PosHeader; border = PosBorder;
                icon = PosIcon; name = PosName; desc = PosDesc;
                break;
            case BuffType.Negative:
                bg = NegBackground; header = NegHeader; border = NegBorder;
                icon = NegIcon; name = NegName; desc = NegDesc;
                break;
            case BuffType.ExtremeNegative:
                bg = ExNegBackground; header = ExNegHeader; border = ExNegBorder;
                icon = ExNegIcon; name = ExNegName; desc = ExNegDesc;
                break;
            default:
                bg = NegBackground; header = NegHeader; border = NegBorder;
                icon = NegIcon; name = NegName; desc = NegDesc;
                break;
        }

        if (cardFrontBackground != null) cardFrontBackground.color = bg;
        if (cardHeader != null) cardHeader.color = header;
        if (borderImage != null) borderImage.color = border;
        if (iconBackground != null) iconBackground.color = icon;
        if (nameText != null) nameText.color = name;
        if (descriptionText != null) descriptionText.color = desc;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!isHoverable) return;
        isHovered = true;
        targetScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData e)
    {
        isHovered = false;
        if (!isHoverable) return;
        targetScale = baseScale;
    }

    public void SetInteractable(bool interactable)
    {
        if (!isFlipped)
            cardButton.interactable = interactable;
    }

    public void SetHoverable(bool hoverable)
    {
        isHoverable = hoverable;
        if (!hoverable)
            targetScale = baseScale;
    }

    private void OnClick()
    {
        if (isFlipped || isFlipping) return;
        OnCardClicked?.Invoke(this);
    }

    public void Flip(Action onComplete = null)
    {
        if (isFlipped || isFlipping) return;
        StartCoroutine(FlipRoutine(onComplete));
    }

    private IEnumerator FlipRoutine(Action onComplete)
    {
        isFlipping = true;
        cardButton.interactable = false;

        float half = flipDuration / 2f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            rt.localScale = new Vector3(Mathf.Lerp(baseScale.x, 0f, t / half), baseScale.y, 1f);
            yield return null;
        }
        rt.localScale = new Vector3(0f, baseScale.y, 1f);

        cardBack.SetActive(false);
        cardFront.SetActive(true);

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            rt.localScale = new Vector3(Mathf.Lerp(0f, baseScale.x, t / half), baseScale.y, 1f);
            yield return null;
        }
        rt.localScale = baseScale;

        isFlipped = true;
        isFlipping = false;
        onComplete?.Invoke();
    }

    public void DimUnselected()
    {
        SetHoverable(false);
        StartCoroutine(DimRoutine());
    }

    private IEnumerator DimRoutine()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        float t = 0f;
        float duration = 0.3f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0.3f, t / duration);
            yield return null;
        }
        cg.alpha = 0.3f;
    }

    public void ScaleUp()
    {
        float scaleUpValue = 1.1f;
        baseScale = new Vector3(scaleUpValue, scaleUpValue, 1f);
        StartCoroutine(ScaleRoutine(scaleUpValue, 0.2f));
    }

    private IEnumerator ScaleRoutine(float targetScaleVal, float duration)
    {
        float t = 0f;
        Vector3 startScale = rt.localScale;
        Vector3 endScale = new Vector3(targetScaleVal, targetScaleVal, 1f);
        while (t < duration)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(startScale, endScale, t / duration);
            yield return null;
        }
        rt.localScale = endScale;
        targetScale = endScale;
    }

    public void Reset()
    {
        isFlipped = false;
        isFlipping = false;
        isHoverable = true;
        isHovered = false;
        cardFront.SetActive(false);
        cardBack.SetActive(true);
        cardButton.interactable = true;
        baseScale = Vector3.one;
        targetScale = Vector3.one;
        rt.localScale = Vector3.one;

        var cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;
    }

    public BuffCardData GetData() => data;
    public PermanentBuffType GetPermanentBuffType() => permanentBuffType;
}