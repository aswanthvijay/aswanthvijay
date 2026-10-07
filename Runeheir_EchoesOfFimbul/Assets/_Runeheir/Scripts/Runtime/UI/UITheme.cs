using System;
using Runeheir.Session;
using UnityEngine;

namespace Runeheir.UI
{
    /// <summary>
    /// Colors, font and generated sprites for the prototype UI (frost-blue panels with gold trim).
    /// Everything is generated in code so the UI works with zero imported assets; art can replace it later.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Backdrop = new Color(0.04f, 0.06f, 0.10f, 1f);
        public static readonly Color WindowBg = new Color(0.08f, 0.11f, 0.16f, 0.95f);
        public static readonly Color WindowBorder = new Color(0.62f, 0.50f, 0.26f, 0.95f);
        public static readonly Color TitleBar = new Color(0.13f, 0.19f, 0.29f, 1f);
        public static readonly Color PanelInner = new Color(0.05f, 0.08f, 0.12f, 0.85f);
        public static readonly Color Gold = new Color(0.93f, 0.77f, 0.40f, 1f);
        public static readonly Color Frost = new Color(0.68f, 0.86f, 1f, 1f);
        public static readonly Color Text = new Color(0.93f, 0.94f, 0.96f, 1f);
        public static readonly Color TextDim = new Color(0.62f, 0.68f, 0.77f, 1f);
        public static readonly Color Button = new Color(0.19f, 0.29f, 0.43f, 1f);
        public static readonly Color ButtonHover = new Color(0.27f, 0.40f, 0.58f, 1f);
        public static readonly Color ButtonPressed = new Color(0.13f, 0.20f, 0.31f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.20f, 0.22f, 0.26f, 0.6f);
        public static readonly Color InputBg = new Color(0.03f, 0.05f, 0.09f, 1f);
        public static readonly Color SlotBg = new Color(0.05f, 0.07f, 0.11f, 0.92f);
        public static readonly Color Selected = new Color(0.93f, 0.77f, 0.40f, 0.30f);
        public static readonly Color BarBg = new Color(0.02f, 0.03f, 0.05f, 0.92f);
        public static readonly Color Hp = new Color(0.32f, 0.82f, 0.36f, 1f);
        public static readonly Color HpLow = new Color(0.92f, 0.28f, 0.22f, 1f);
        public static readonly Color Sp = new Color(0.30f, 0.55f, 0.98f, 1f);
        public static readonly Color BaseExp = new Color(0.93f, 0.77f, 0.40f, 1f);
        public static readonly Color JobExp = new Color(0.36f, 0.84f, 0.84f, 1f);
        public static readonly Color Error = new Color(1f, 0.47f, 0.42f, 1f);
        public static readonly Color Success = new Color(0.52f, 1f, 0.62f, 1f);

        private static Font s_font;
        private static Sprite s_rounded;
        private static Sprite s_white;

        public static Font Font
        {
            get
            {
                if (s_font == null)
                {
                    try
                    {
                        s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                    catch (ArgumentException)
                    {
                        s_font = null;
                    }

                    if (s_font == null)
                    {
                        s_font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica", "Liberation Sans" }, 16);
                    }
                }

                return s_font;
            }
        }

        /// <summary>9-sliced rounded rectangle (white, tint with Image.color).</summary>
        public static Sprite Rounded => s_rounded != null ? s_rounded : s_rounded = CreateRoundedSprite(32, 9);

        public static Sprite White
        {
            get
            {
                if (s_white == null)
                {
                    var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "RH_White" };
                    var pixels = new Color32[16];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = new Color32(255, 255, 255, 255);
                    }

                    texture.SetPixels32(pixels);
                    texture.Apply();
                    s_white = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
                }

                return s_white;
            }
        }

        public static Color ChatColor(ChatKind kind)
        {
            switch (kind)
            {
                case ChatKind.System: return new Color(1f, 0.92f, 0.55f);
                case ChatKind.Notice: return new Color(0.55f, 0.92f, 1f);
                case ChatKind.Error: return Error;
                case ChatKind.Loot: return new Color(0.55f, 1f, 0.55f);
                case ChatKind.Gm: return new Color(1f, 0.65f, 1f);
                case ChatKind.Party: return new Color(1f, 0.72f, 0.82f);
                case ChatKind.Guild: return new Color(0.62f, 1f, 0.62f);
                case ChatKind.Whisper: return new Color(1f, 1f, 0.45f);
                case ChatKind.Shout: return new Color(1f, 0.62f, 0.35f);
                default: return Text;
            }
        }

        /// <summary>Vertical gradient for full-screen backgrounds.</summary>
        public static Sprite VerticalGradient(Color top, Color bottom)
        {
            const int height = 128;
            var texture = new Texture2D(1, height, TextureFormat.RGBA32, false)
            {
                name = "RH_Gradient",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(0, y, Color.Lerp(bottom, top, y / (float)(height - 1)));
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateRoundedSprite(int size, int radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RH_Rounded",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float cx = Mathf.Clamp(px, radius, size - radius);
                    float cy = Mathf.Clamp(py, radius, size - radius);
                    float distance = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            float border = radius + 1;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }
}
