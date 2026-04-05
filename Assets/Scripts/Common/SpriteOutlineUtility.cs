using System.Collections.Generic;
using UnityEngine;

public static class SpriteOutlineUtility
{
    private static readonly Dictionary<(Texture2D, Color, int), Texture2D> cache = new();

    public static Texture2D GetOrCreate(Texture2D source, Color tint, int thickness)
    {
        var key = (source, tint, thickness);
        if (cache.TryGetValue(key, out var cached)) return cached;

        var result = CreateOutlineTexture(source, tint, thickness);
        if (result != null)
            cache[key] = result;
        return result;
    }

    private static Texture2D CreateOutlineTexture(Texture2D source, Color tint, int thickness)
    {
        const int maxSize = 256;

        float scale = Mathf.Min(1f, Mathf.Min((float)maxSize / source.width, (float)maxSize / source.height));
        int newW = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
        int newH = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));

        var rt = RenderTexture.GetTemporary(newW, newH, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        var workSrc = new Texture2D(newW, newH, TextureFormat.RGBA32, false);
        workSrc.ReadPixels(new Rect(0, 0, newW, newH), 0, 0);
        workSrc.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        int scaledThickness = Mathf.Max(1, Mathf.RoundToInt(thickness * scale));

        int w = workSrc.width;
        int h = workSrc.height;
        Color[] src = workSrc.GetPixels();
        Color[] dst = new Color[w * h];

        int outlinePixels = 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (src[y * w + x].a > 0.01f) continue;

                bool nearOpaque = false;
                for (int dy = -scaledThickness; dy <= scaledThickness && !nearOpaque; dy++)
                {
                    for (int dx = -scaledThickness; dx <= scaledThickness && !nearOpaque; dx++)
                    {
                        if (dx * dx + dy * dy > scaledThickness * scaledThickness) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        if (src[ny * w + nx].a > 0.01f) nearOpaque = true;
                    }
                }

                if (nearOpaque)
                {
                    dst[y * w + x] = new Color(tint.r, tint.g, tint.b, 1f);
                    outlinePixels++;
                }
            }
        }

        var result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.SetPixels(dst);
        result.Apply();
        return result;
    }

    public static Color RarityColor(Rarity r) => r switch
    {
        Rarity.Common => new Color(0.75f, 0.75f, 0.75f),
        Rarity.Uncommon => new Color(0.30f, 0.80f, 0.30f),
        Rarity.Rare => new Color(0.20f, 0.50f, 1.00f),
        Rarity.Epic => new Color(0.65f, 0.25f, 0.90f),
        Rarity.GOD => new Color(1.00f, 0.75f, 0.10f),
        _ => Color.white
    };
}