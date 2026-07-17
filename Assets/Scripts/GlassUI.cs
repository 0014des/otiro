using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// iOS 26風リキッドガラスUIを生成するためのランタイムヘルパー。
/// 極限まで透明感を高め、美しい微細な境界線とすりガラス風ハイライトを動的生成します。
/// </summary>
public static class GlassUI
{
    /// <summary>
    /// 角丸の四角形テクスチャを動的に生成（境界線と光沢グラデーション付き）
    /// </summary>
    public static Texture2D CreateRoundedRectTexture(int width, int height, int radius, Color fillColor, Color borderColor, int borderWidth = 1)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color transparent = new Color(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist = DistToRoundedRect(x, y, width, height, radius);

                if (dist <= -borderWidth)
                {
                    // 内部：極めて高い透明度ベースに、上部から下部へ繊細なリキッドハイライトを付与
                    float gradientT = (float)y / height;
                    
                    // iOS 26特有の、上部エッジ付近の極細ダブルハイライト反射効果
                    float highlight = 0f;
                    if (y > height * 0.85f)
                    {
                        highlight = Mathf.Lerp(0f, 0.08f, (y - height * 0.85f) / (height * 0.15f));
                    }
                    
                    Color c = fillColor;
                    c.r = Mathf.Min(1f, c.r + highlight);
                    c.g = Mathf.Min(1f, c.g + highlight);
                    c.b = Mathf.Min(1f, c.b + highlight);
                    c.a = Mathf.Lerp(fillColor.a * 0.7f, fillColor.a * 1.3f, gradientT); // 縦方向の透明度グラデーション
                    pixels[y * width + x] = c;
                }
                else if (dist <= 0)
                {
                    // 境界線：ネオンを含んだような超極細エッジ
                    float edgeAlpha = Mathf.Clamp01(1f - (dist + borderWidth) / (float)borderWidth);
                    Color c = Color.Lerp(fillColor, borderColor, edgeAlpha);
                    pixels[y * width + x] = c;
                }
                else if (dist <= 1.2f)
                {
                    // アンチエイリアス
                    float aa = 1f - Mathf.Clamp01(dist / 1.2f);
                    Color c = borderColor;
                    c.a *= aa;
                    pixels[y * width + x] = c;
                }
                else
                {
                    pixels[y * width + x] = transparent;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    private static float DistToRoundedRect(int px, int py, int w, int h, int r)
    {
        float cx = px - w * 0.5f;
        float cy = py - h * 0.5f;
        float hw = w * 0.5f - r;
        float hh = h * 0.5f - r;

        float dx = Mathf.Max(Mathf.Abs(cx) - hw, 0f);
        float dy = Mathf.Max(Mathf.Abs(cy) - hh, 0f);

        float cornerDist = Mathf.Sqrt(dx * dx + dy * dy) - r;
        float innerDist = Mathf.Max(Mathf.Abs(cx) - (w * 0.5f), Mathf.Abs(cy) - (h * 0.5f));

        if (dx == 0 && dy == 0)
            return innerDist;
        else
            return cornerDist;
    }

    /// <summary>
    /// ガラスパネル用スプライト（アルファ値をさらに下げて透明感を最大化）
    /// </summary>
    public static Sprite CreateGlassPanelSprite(Color tint, float alpha = 0.08f, int radius = 40)
    {
        Color fill = new Color(tint.r, tint.g, tint.b, alpha);
        Color border = new Color(1f, 1f, 1f, alpha + 0.18f); // 境界線も細く繊細に
        Texture2D tex = CreateRoundedRectTexture(256, 256, radius, fill, border, 1);

        return Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
    }

    /// <summary>
    /// ガラスボタン用スプライト
    /// </summary>
    public static Sprite CreateGlassButtonSprite(Color tint, float alpha = 0.12f, int radius = 30)
    {
        Color fill = new Color(tint.r, tint.g, tint.b, alpha);
        Color border = new Color(1f, 1f, 1f, alpha + 0.22f);
        Texture2D tex = CreateRoundedRectTexture(128, 128, radius, fill, border, 1);

        return Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
    }

    public static ColorBlock CreateGlassButtonColors()
    {
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 1.25f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.6f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.15f;
        return cb;
    }
}
