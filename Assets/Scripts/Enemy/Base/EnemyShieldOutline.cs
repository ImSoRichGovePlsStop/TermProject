using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealthBase))]
public class EnemyShieldOutline : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer mainSpriteRenderer;

    [Header("Settings")]
    [SerializeField] private int thickness = 4;

    private static readonly Dictionary<Sprite, Sprite> _outlineCache = new();
    private static readonly Color ShieldYellow = new Color(1f, 0.85f, 0.1f, 1f);

    private EnemyHealthBase health;
    private SpriteRenderer outlineSr;
    private Sprite cachedSprite;

    private void Awake()
    {
        health = GetComponent<EnemyHealthBase>();

        if (mainSpriteRenderer == null)
        {
            var visual = transform.Find("Visual");
            if (visual != null)
                mainSpriteRenderer = visual.GetComponent<SpriteRenderer>();
        }

        CreateOutlineRenderer();
    }

    private void Start()
    {
        health.OnShieldChanged += RefreshOutline;
        RefreshOutline();
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnShieldChanged -= RefreshOutline;
    }

    private void CreateOutlineRenderer()
    {
        var parent = mainSpriteRenderer != null ? mainSpriteRenderer.transform : transform;

        var go = new GameObject("ShieldOutline");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;

        outlineSr = go.AddComponent<SpriteRenderer>();
        outlineSr.enabled = false;
        go.AddComponent<BillboardSprite>();
    }

    private void LateUpdate()
    {
        if (!health.HasShield || mainSpriteRenderer == null) return;
        if (mainSpriteRenderer.sprite == cachedSprite) return;
        RebuildOutlineSprite();
    }

    private void RefreshOutline()
    {
        if (outlineSr == null) return;
        outlineSr.enabled = health.HasShield;
        if (health.HasShield)
            RebuildOutlineSprite();
    }

    private void RebuildOutlineSprite()
    {
        if (mainSpriteRenderer == null || mainSpriteRenderer.sprite == null) return;

        var sprite = mainSpriteRenderer.sprite;
        cachedSprite = sprite;

        if (!_outlineCache.TryGetValue(sprite, out var outlineSprite))
        {
            var rect = sprite.textureRect;
            var croppedTex = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
            croppedTex.SetPixels(sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height));
            croppedTex.Apply();

            float pixelsPerUnit = croppedTex.width / sprite.bounds.size.x;
            int pixelThickness = Mathf.Max(1, Mathf.RoundToInt(thickness * pixelsPerUnit / sprite.pixelsPerUnit));

            var outlineTex = SpriteOutlineUtility.GetOrCreate(croppedTex, ShieldYellow, pixelThickness);
            if (outlineTex == null) return;

            float scaleRatio = Mathf.Min(
                (float)outlineTex.width / croppedTex.width,
                (float)outlineTex.height / croppedTex.height
            );
            float adjustedPPU = sprite.pixelsPerUnit * scaleRatio;

            outlineSprite = Sprite.Create(
                outlineTex,
                new Rect(0, 0, outlineTex.width, outlineTex.height),
                new Vector2(0.5f, 0.5f),
                adjustedPPU
            );
            _outlineCache[sprite] = outlineSprite;
        }

        outlineSr.sprite = outlineSprite;
        outlineSr.sortingLayerID = mainSpriteRenderer.sortingLayerID;
        outlineSr.sortingOrder = mainSpriteRenderer.sortingOrder - 1;
        outlineSr.color = Color.white;
        outlineSr.flipX = mainSpriteRenderer.flipX;
        outlineSr.flipY = mainSpriteRenderer.flipY;
    }
}