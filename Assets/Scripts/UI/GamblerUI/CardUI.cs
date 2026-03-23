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

    private float flipDuration = 0.3f;
    private float hoverScale = 1.08f;
    private float scaleSmoothing = 8f;
    private float auraPulseSpeed = 2f;
    private float auraMinAlpha = 0.1f;
    private float auraMaxAlpha = 0.8f;
    private Vector2 auraEffectDistance = new Vector2(6f, 6f);

    private BuffCardData data;
    private bool isFlipped = false;
    private bool isFlipping = false;
    private bool isHoverable = true;
    private bool auraEnabled = true;
    private PermanentBuffType permanentBuffType;

    private RectTransform rt;
    private Vector3 baseScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;

    private Outline outline;
    private bool isAuraActive = false;
    private Coroutine auraCoroutine;
    private static readonly Color AuraColor = new Color(0.5f, 0.3f, 1f);

    private static readonly Color ExPosBackground = new Color(0.10f, 0.18f, 0.08f);
    private static readonly Color ExPosHeader = new Color(0.16f, 0.28f, 0.08f);
    private static readonly Color ExPosIcon = new Color(0.73f, 0.46f, 0.09f);
    private static readonly Color ExPosName = new Color(1.00f, 0.78f, 0.30f);
    private static readonly Color ExPosDesc = new Color(0.98f, 0.88f, 0.65f);

    private static readonly Color PosBackground = new Color(0.06f, 0.16f, 0.12f);
    private static readonly Color PosHeader = new Color(0.08f, 0.24f, 0.17f);
    private static readonly Color PosIcon = new Color(0.11f, 0.62f, 0.46f);
    private static readonly Color PosName = new Color(0.62f, 0.88f, 0.79f);
    private static readonly Color PosDesc = new Color(0.75f, 0.93f, 0.87f);

    private static readonly Color NegBackground = new Color(0.16f, 0.12f, 0.25f);
    private static readonly Color NegHeader = new Color(0.22f, 0.16f, 0.34f);
    private static readonly Color NegIcon = new Color(0.50f, 0.47f, 0.87f);
    private static readonly Color NegName = new Color(0.80f, 0.78f, 0.97f);
    private static readonly Color NegDesc = new Color(0.69f, 0.66f, 0.93f);

    private static readonly Color ExNegBackground = new Color(0.16f, 0.06f, 0.06f);
    private static readonly Color ExNegHeader = new Color(0.24f, 0.08f, 0.08f);
    private static readonly Color ExNegIcon = new Color(0.88f, 0.29f, 0.29f);
    private static readonly Color ExNegName = new Color(0.97f, 0.70f, 0.70f);
    private static readonly Color ExNegDesc = new Color(0.94f, 0.58f, 0.58f);

    public event Action<CardUI> OnCardClicked;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        cardButton.onClick.AddListener(OnClick);
        cardFront.SetActive(false);
        cardBack.SetActive(true);

        var img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;
        }

        outline = GetComponent<Outline>();
        if (outline == null)
            outline = gameObject.AddComponent<Outline>();

        outline.enabled = false;
        outline.effectDistance = auraEffectDistance;
        outline.useGraphicAlpha = false;
    }

    private void Update()
    {
        rt.localScale = Vector3.Lerp(rt.localScale, targetScale, Time.deltaTime * scaleSmoothing);
    }

    public void Setup(BuffCardData buffData, Sprite backSprite = null, PermanentBuffType permBuff = PermanentBuffType.MaxHp, bool enableAura = true)
    {
        data = buffData;
        permanentBuffType = permBuff;
        auraEnabled = enableAura;
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
        Color bg, header, icon, name, desc;

        switch (type)
        {
            case BuffType.ExtremePositive:
                bg = ExPosBackground; header = ExPosHeader;
                icon = ExPosIcon; name = ExPosName; desc = ExPosDesc;
                break;
            case BuffType.Positive:
                bg = PosBackground; header = PosHeader;
                icon = PosIcon; name = PosName; desc = PosDesc;
                break;
            case BuffType.Negative:
                bg = NegBackground; header = NegHeader;
                icon = NegIcon; name = NegName; desc = NegDesc;
                break;
            case BuffType.ExtremeNegative:
                bg = ExNegBackground; header = ExNegHeader;
                icon = ExNegIcon; name = ExNegName; desc = ExNegDesc;
                break;
            default:
                bg = NegBackground; header = NegHeader;
                icon = NegIcon; name = NegName; desc = NegDesc;
                break;
        }

        if (cardFrontBackground != null) cardFrontBackground.color = bg;
        if (cardHeader != null) cardHeader.color = header;
        if (iconBackground != null) iconBackground.color = icon;
        if (nameText != null) nameText.color = name;
        if (descriptionText != null) descriptionText.color = desc;

        StopAura();
        if (auraEnabled && (type == BuffType.ExtremePositive || type == BuffType.ExtremeNegative))
            StartAura();
    }

    private void StartAura()
    {
        isAuraActive = true;
        outline.enabled = true;
        outline.effectDistance = auraEffectDistance;
        if (auraCoroutine != null) StopCoroutine(auraCoroutine);
        auraCoroutine = StartCoroutine(AuraRoutine());
    }

    private void StopAura()
    {
        isAuraActive = false;
        if (auraCoroutine != null)
        {
            StopCoroutine(auraCoroutine);
            auraCoroutine = null;
        }
        if (outline != null)
            outline.enabled = false;
    }

    private IEnumerator AuraRoutine()
    {
        while (isAuraActive)
        {
            float t = (Mathf.Sin(Time.time * auraPulseSpeed) + 1f) / 2f;
            float alpha = Mathf.Lerp(auraMinAlpha, auraMaxAlpha, t);
            float dist = Mathf.Lerp(auraEffectDistance.x, auraEffectDistance.x * 2f, t);
            outline.effectColor = new Color(AuraColor.r, AuraColor.g, AuraColor.b, alpha);
            outline.effectDistance = new Vector2(dist, dist);
            yield return null;
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!isHoverable) return;
        targetScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData e)
    {
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
        StopAura();
        isFlipped = false;
        isFlipping = false;
        isHoverable = true;
        auraEnabled = true;
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
    public void StopAuraPublic() => StopAura();
}