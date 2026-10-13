using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>The pictogram an item's icon shows.</summary>
    public enum IconShape
    {
        Pouch, Sword, Greatsword, Dagger, Spear, Axe, GreatAxe, Mace, Staff, Bow, Claws, Lyre, Whip, Book, Katar, Gun, Shuriken, CatStaff,
        Helm, Hat, Glasses, Mask, Armor, Shield, Cloak, Boots, Ring, Amulet,
        Potion, Herb, Food, Scroll, Chest, Ore, Gem, Rune, Card, Egg, Arrow, Bullet, Feather, Branch, Coin, Wings, Aura, Wig, Ears,
    }

    /// <summary>
    /// Phase 8 item icons: a pictogram per kind of item (a blade, a bow, a flask, a scroll, a helm, a card...), drawn at
    /// runtime from signed-distance shapes into small textures, outlined and lit from the top left, and tinted with the
    /// item's color over a darker plate. No icon art ships with the game; thousands of items share a few dozen shapes.
    /// </summary>
    public static class ItemIcons
    {
        private const int Size = 64;
        private static readonly Dictionary<IconShape, Sprite> Cache = new Dictionary<IconShape, Sprite>();

        // ------------------------------------------------------------------ which shape
        public static IconShape ShapeOf(ItemDefinition item)
        {
            if (item == null)
            {
                return IconShape.Pouch;
            }

            string name = item.Name?.ToLowerInvariant() ?? string.Empty;
            if (item.IsCard)
            {
                return IconShape.Card;
            }

            if (item.IsAmmo)
            {
                return item.AmmoFor == WeaponType.ThunderRod ? IconShape.Bullet : item.AmmoFor == WeaponType.Huuma ? IconShape.Shuriken : IconShape.Arrow;
            }

            if (item.IsCostume)
            {
                string look = item.LookAs ?? string.Empty;
                if (look.StartsWith("wings", StringComparison.Ordinal) || look == "head_wings") return IconShape.Wings;
                if (look.StartsWith("aura", StringComparison.Ordinal)) return IconShape.Aura;
                if (look.StartsWith("wig", StringComparison.Ordinal)) return IconShape.Wig;
                if (look.StartsWith("ears", StringComparison.Ordinal)) return IconShape.Ears;
                if (look == "cape" || look == "muffler" || look == "tail") return IconShape.Cloak;
                if (look == "glasses" || look == "eyepatch" || look == "monocle" || look == "mask_eyes") return IconShape.Glasses;
                if (look == "mask" || look == "scarf" || look == "pipe" || look == "beard") return IconShape.Mask;
                return look == "helm" ? IconShape.Helm : IconShape.Hat;
            }

            if (item.IsWeapon)
            {
                switch (item.WeaponType)
                {
                    case WeaponType.Dagger: return IconShape.Dagger;
                    case WeaponType.OneHandSword: return IconShape.Sword;
                    case WeaponType.TwoHandSword: return IconShape.Greatsword;
                    case WeaponType.Spear: return IconShape.Spear;
                    case WeaponType.Axe: return IconShape.Axe;
                    case WeaponType.TwoHandAxe: return IconShape.GreatAxe;
                    case WeaponType.Mace: return IconShape.Mace;
                    case WeaponType.Staff: return IconShape.Staff;
                    case WeaponType.Bow: return IconShape.Bow;
                    case WeaponType.Knuckle: return IconShape.Claws;
                    case WeaponType.Instrument: return IconShape.Lyre;
                    case WeaponType.Whip: return IconShape.Whip;
                    case WeaponType.Book: return IconShape.Book;
                    case WeaponType.Katar: return IconShape.Katar;
                    case WeaponType.ThunderRod: return IconShape.Gun;
                    case WeaponType.Huuma: return IconShape.Shuriken;
                    case WeaponType.CatStaff: return IconShape.CatStaff;
                    default: return IconShape.Sword;
                }
            }

            if (item.IsEquipment)
            {
                var slots = item.Slots;
                if ((slots & EquipSlot.HeadUpper) != 0) return Regex.IsMatch(name, "helm|crown|circlet|tiara|coronet") ? IconShape.Helm : IconShape.Hat;
                if ((slots & EquipSlot.HeadMid) != 0) return IconShape.Glasses;
                if ((slots & EquipSlot.HeadLower) != 0) return IconShape.Mask;
                if ((slots & EquipSlot.Armor) != 0) return IconShape.Armor;
                if ((slots & EquipSlot.Shield) != 0) return IconShape.Shield;
                if ((slots & EquipSlot.Garment) != 0) return Regex.IsMatch(name, "wing") ? IconShape.Wings : IconShape.Cloak;
                if ((slots & EquipSlot.Footgear) != 0) return IconShape.Boots;
                return Regex.IsMatch(name, "necklace|amulet|pendant|brooch|choker|talisman|charm|rosary") ? IconShape.Amulet : IconShape.Ring;
            }

            switch (item.Special)
            {
                case ItemSpecialEffect.HatchPet: return IconShape.Egg;
                case ItemSpecialEffect.TamePet: return IconShape.Food;
                case ItemSpecialEffect.PetArmor: return IconShape.Hat;
                case ItemSpecialEffect.HireMercenary: return IconShape.Scroll;
                case ItemSpecialEffect.ReturnToSavePoint:
                case ItemSpecialEffect.RandomTeleport: return IconShape.Feather;
                case ItemSpecialEffect.SummonMonster:
                case ItemSpecialEffect.SummonBoss: return IconShape.Branch;
            }

            if ((item.RandomGives != null && item.RandomGives.Length > 0) || (item.GivesItems != null && item.GivesItems.Length > 0)
                || Regex.IsMatch(name, @"\b(box|chest|crate|bundle|package|hoard)\b"))
            {
                return IconShape.Chest;
            }

            if (!string.IsNullOrEmpty(item.CastSkillId) || Regex.IsMatch(name, "scroll|contract|letter|page|map\\b|note\\b|tome|manual"))
            {
                return IconShape.Scroll;
            }

            if (Regex.IsMatch(name, "rune|glyph|sigil")) return IconShape.Rune;
            if (Regex.IsMatch(name, "coin|token|medal|zeny|ticket|badge")) return IconShape.Coin;
            if (Regex.IsMatch(name, "feather|plume|wing")) return IconShape.Feather;
            if (Regex.IsMatch(name, "branch|twig|stick|bough")) return IconShape.Branch;
            if (Regex.IsMatch(name, "gem|jewel|crystal|diamond|ruby|sapphire|emerald|topaz|amethyst|garnet|opal|pearl|zircon|aquamarine")) return IconShape.Gem;
            if (Regex.IsMatch(name, "ore|iron|steel|metal|starmetal|skystone|ingot|stone|coal|elunium|oridecon|mithril")) return IconShape.Ore;
            // Drinks first: a Lingonberry Tonic is a flask, not a berry.
            if (Regex.IsMatch(name, "potion|tonic|mead|elixir|draught|\\bsap\\b|water|\\bale\\b|wine|vial|flask|bottle|brew")) return IconShape.Potion;
            if (Regex.IsMatch(name, "herb|leaf|flower|root|grass|moss|petal|seed|blossom|lotus|clover|mushroom|fungus")) return IconShape.Herb;
            if (Regex.IsMatch(name, "meat|bread|apple|fruit|berry|cake|cheese|fish|egg\\b|honey|milk|juice|soup|pie|candy|cookie|carrot|potato|grape|banana|stew|rice")) return IconShape.Food;
            if (item.IsUsable)
            {
                return item.HealHpMax > 0 || item.HealSpMax > 0 || item.HealHpPercent > 0f || item.HealSpPercent > 0f || !string.IsNullOrEmpty(item.BuffId)
                    ? IconShape.Potion
                    : IconShape.Scroll;
            }

            return item.Kind == ItemKind.Material ? IconShape.Ore : IconShape.Pouch;
        }

        // ------------------------------------------------------------------ on a UI icon
        /// <summary>Shows <paramref name="item"/>'s pictogram on an icon plate made by <see cref="UIFactory.CreateIcon"/> (null hides it).</summary>
        public static void Apply(Image icon, ItemDefinition item, Text glyph = null)
        {
            if (icon == null)
            {
                return;
            }

            glyph = glyph != null ? glyph : icon.GetComponentInChildren<Text>(true);
            var picture = Pictogram(icon);
            if (item == null)
            {
                picture.enabled = false;
                return;
            }

            Color color = RuntimeMaterials.Hex(item.IconColorHex);
            icon.color = Color.Lerp(color, new Color(0.08f, 0.09f, 0.12f), 0.62f);
            picture.enabled = true;
            picture.sprite = SpriteFor(ShapeOf(item));
            picture.color = Color.Lerp(color, Color.white, 0.25f);
            if (glyph != null)
            {
                glyph.text = string.Empty;
            }
        }

        /// <summary>Back to a text glyph (a skill on the same hotkey slot): hides the pictogram.</summary>
        public static void Clear(Image icon)
        {
            var existing = icon != null ? icon.transform.Find("Pictogram") : null;
            if (existing != null)
            {
                existing.GetComponent<Image>().enabled = false;
            }
        }

        private static Image Pictogram(Image icon)
        {
            var existing = icon.transform.Find("Pictogram");
            if (existing != null)
            {
                return existing.GetComponent<Image>();
            }

            var go = new GameObject("Pictogram", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(icon.transform, false);
            rect.SetSiblingIndex(Math.Min(1, icon.transform.childCount - 1)); // over the shine, under any text
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(3f, 3f);
            rect.offsetMax = new Vector2(-3f, -3f);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        public static Sprite SpriteFor(IconShape shape)
        {
            if (Cache.TryGetValue(shape, out var cached) && cached != null)
            {
                return cached;
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "Icon_" + shape, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(Render(Shapes.Of(shape)));
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            Cache[shape] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------ drawing
        /// <summary>Signed-distance rendering: the union of the parts, the topmost part's tone, a dark outline and soft light.</summary>
        public static Color32[] Render(IReadOnlyList<Shapes.Part> parts)
        {
            var pixels = new Color32[Size * Size];
            const float outline = 0.045f;
            const float aa = 1.2f / Size;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                    float union = float.MaxValue;
                    float tone = 1f;
                    float toneDistance = float.MaxValue;
                    foreach (var part in parts)
                    {
                        float d = part.Distance(p);
                        union = Mathf.Min(union, d);
                        if (d < 0f)
                        {
                            tone = part.Tone; // later parts are drawn on top
                            toneDistance = -1f;
                        }
                        else if (toneDistance >= 0f && d < toneDistance)
                        {
                            tone = part.Tone;
                            toneDistance = d;
                        }
                    }

                    float fill = Mathf.Clamp01(0.5f - union / aa);
                    float edge = Mathf.Clamp01(0.5f - (union - outline) / aa);
                    float light = 0.82f + 0.18f * (p.y + (1f - p.x)) * 0.5f;
                    float value = Mathf.Clamp01(tone * light);
                    var inside = new Color(value, value, value, 1f);
                    var line = new Color(0.06f, 0.06f, 0.08f, 1f);
                    var color = Color.Lerp(line, inside, fill);
                    color.a = Mathf.Max(fill, edge * 0.95f);
                    pixels[y * Size + x] = color;
                }
            }

            return pixels;
        }
    }

    /// <summary>The pictograms, as unions of simple signed-distance parts in a 0–1 square (y up).</summary>
    public static class Shapes
    {
        public abstract class Part
        {
            public float Tone = 1f;

            public abstract float Distance(Vector2 p);
        }

        private sealed class Circle : Part
        {
            public Vector2 C;
            public Vector2 R;

            public override float Distance(Vector2 p)
            {
                var q = new Vector2((p.x - C.x) / R.x, (p.y - C.y) / R.y);
                return (q.magnitude - 1f) * Mathf.Min(R.x, R.y);
            }
        }

        private sealed class Ring : Part
        {
            public Vector2 C;
            public float R;
            public float W;

            public override float Distance(Vector2 p)
            {
                return Mathf.Abs((p - C).magnitude - R) - W;
            }
        }

        private sealed class Capsule : Part
        {
            public Vector2 A;
            public Vector2 B;
            public float R;

            public override float Distance(Vector2 p)
            {
                var pa = p - A;
                var ba = B - A;
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-6f, Vector2.Dot(ba, ba)));
                return (pa - ba * h).magnitude - R;
            }
        }

        private sealed class Box : Part
        {
            public Vector2 C;
            public Vector2 H;
            public float Angle;
            public float Round;

            public override float Distance(Vector2 p)
            {
                float s = Mathf.Sin(-Angle * Mathf.Deg2Rad);
                float c = Mathf.Cos(-Angle * Mathf.Deg2Rad);
                var d = p - C;
                var q = new Vector2(Mathf.Abs(d.x * c - d.y * s), Mathf.Abs(d.x * s + d.y * c)) - (H - new Vector2(Round, Round));
                return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - Round;
            }
        }

        private sealed class Polygon : Part
        {
            public Vector2[] V;

            public override float Distance(Vector2 p)
            {
                float d = Vector2.Dot(p - V[0], p - V[0]);
                float sign = 1f;
                for (int i = 0, j = V.Length - 1; i < V.Length; j = i, i++)
                {
                    var e = V[j] - V[i];
                    var w = p - V[i];
                    var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(1e-6f, Vector2.Dot(e, e)));
                    d = Mathf.Min(d, Vector2.Dot(b, b));
                    bool c1 = p.y >= V[i].y;
                    bool c2 = p.y < V[j].y;
                    bool c3 = e.x * w.y > e.y * w.x;
                    if ((c1 && c2 && c3) || (!c1 && !c2 && !c3))
                    {
                        sign = -sign;
                    }
                }

                return sign * Mathf.Sqrt(d);
            }
        }

        private static Part C(float x, float y, float r, float tone = 1f) => new Circle { C = new Vector2(x, y), R = new Vector2(r, r), Tone = tone };

        private static Part E(float x, float y, float rx, float ry, float tone = 1f) => new Circle { C = new Vector2(x, y), R = new Vector2(rx, ry), Tone = tone };

        private static Part O(float x, float y, float r, float w, float tone = 1f) => new Ring { C = new Vector2(x, y), R = r, W = w, Tone = tone };

        private static Part L(float ax, float ay, float bx, float by, float r, float tone = 1f) =>
            new Capsule { A = new Vector2(ax, ay), B = new Vector2(bx, by), R = r, Tone = tone };

        private static Part B(float x, float y, float hw, float hh, float angle = 0f, float round = 0.02f, float tone = 1f) =>
            new Box { C = new Vector2(x, y), H = new Vector2(hw, hh), Angle = angle, Round = round, Tone = tone };

        private static Part P(float tone, params float[] xy)
        {
            var v = new Vector2[xy.Length / 2];
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            }

            return new Polygon { V = v, Tone = tone };
        }

        private static IEnumerable<Part> Arc(float cx, float cy, float r, float from, float to, float width, float tone, int steps = 8)
        {
            for (int i = 0; i < steps; i++)
            {
                float a = Mathf.Lerp(from, to, i / (float)steps) * Mathf.Deg2Rad;
                float b = Mathf.Lerp(from, to, (i + 1) / (float)steps) * Mathf.Deg2Rad;
                yield return L(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, cx + Mathf.Cos(b) * r, cy + Mathf.Sin(b) * r, width, tone);
            }
        }

        public static IReadOnlyList<Part> Of(IconShape shape)
        {
            var parts = new List<Part>();
            switch (shape)
            {
                case IconShape.Sword:
                    parts.Add(P(1f, 0.33f, 0.27f, 0.27f, 0.33f, 0.76f, 0.82f, 0.86f, 0.86f, 0.82f, 0.76f));
                    parts.Add(L(0.20f, 0.42f, 0.42f, 0.20f, 0.04f, 0.55f));
                    parts.Add(L(0.17f, 0.17f, 0.31f, 0.31f, 0.035f, 0.45f));
                    parts.Add(C(0.15f, 0.15f, 0.05f, 0.6f));
                    break;
                case IconShape.Greatsword:
                    parts.Add(P(1f, 0.36f, 0.24f, 0.24f, 0.36f, 0.80f, 0.92f, 0.92f, 0.92f, 0.92f, 0.80f));
                    parts.Add(L(0.16f, 0.40f, 0.40f, 0.16f, 0.05f, 0.55f));
                    parts.Add(L(0.12f, 0.12f, 0.28f, 0.28f, 0.04f, 0.45f));
                    parts.Add(C(0.10f, 0.10f, 0.055f, 0.6f));
                    break;
                case IconShape.Dagger:
                    parts.Add(P(1f, 0.42f, 0.36f, 0.36f, 0.42f, 0.72f, 0.76f, 0.80f, 0.80f, 0.76f, 0.72f));
                    parts.Add(L(0.28f, 0.48f, 0.48f, 0.28f, 0.035f, 0.55f));
                    parts.Add(L(0.24f, 0.24f, 0.38f, 0.38f, 0.035f, 0.45f));
                    parts.Add(C(0.22f, 0.22f, 0.045f, 0.6f));
                    break;
                case IconShape.Spear:
                    parts.Add(L(0.12f, 0.12f, 0.72f, 0.72f, 0.03f, 0.5f));
                    parts.Add(P(1f, 0.90f, 0.90f, 0.66f, 0.80f, 0.70f, 0.70f, 0.80f, 0.66f));
                    parts.Add(L(0.62f, 0.70f, 0.70f, 0.62f, 0.03f, 0.6f));
                    break;
                case IconShape.Axe:
                    parts.Add(L(0.22f, 0.10f, 0.58f, 0.88f, 0.035f, 0.5f));
                    parts.Add(P(1f, 0.50f, 0.66f, 0.84f, 0.86f, 0.90f, 0.58f, 0.62f, 0.50f));
                    break;
                case IconShape.GreatAxe:
                    parts.Add(L(0.50f, 0.08f, 0.50f, 0.92f, 0.035f, 0.5f));
                    parts.Add(P(1f, 0.53f, 0.78f, 0.86f, 0.92f, 0.90f, 0.56f, 0.53f, 0.62f));
                    parts.Add(P(1f, 0.47f, 0.78f, 0.14f, 0.92f, 0.10f, 0.56f, 0.47f, 0.62f));
                    break;
                case IconShape.Mace:
                    parts.Add(L(0.18f, 0.18f, 0.56f, 0.56f, 0.035f, 0.5f));
                    parts.Add(C(0.66f, 0.66f, 0.15f));
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3f;
                        parts.Add(L(0.66f, 0.66f, 0.66f + Mathf.Cos(a) * 0.24f, 0.66f + Mathf.Sin(a) * 0.24f, 0.03f, 0.8f));
                    }

                    parts.Add(C(0.66f, 0.66f, 0.13f));
                    break;
                case IconShape.Staff:
                    parts.Add(L(0.18f, 0.08f, 0.64f, 0.72f, 0.035f, 0.5f));
                    parts.Add(O(0.70f, 0.80f, 0.11f, 0.03f, 0.6f));
                    parts.Add(C(0.70f, 0.80f, 0.08f));
                    break;
                case IconShape.Bow:
                    parts.AddRange(Arc(0.30f, 0.30f, 0.50f, -8f, 98f, 0.04f, 0.6f, 10));
                    parts.Add(L(0.80f, 0.23f, 0.23f, 0.80f, 0.012f, 0.95f));
                    parts.Add(L(0.30f, 0.30f, 0.78f, 0.78f, 0.022f, 0.9f));
                    parts.Add(P(1f, 0.86f, 0.86f, 0.72f, 0.80f, 0.80f, 0.72f));
                    break;
                case IconShape.Claws:
                    for (int i = 0; i < 3; i++)
                    {
                        parts.Add(L(0.22f + i * 0.14f, 0.18f, 0.40f + i * 0.14f, 0.84f, 0.035f));
                    }

                    parts.Add(B(0.44f, 0.24f, 0.26f, 0.08f, 0f, 0.04f, 0.55f));
                    break;
                case IconShape.Lyre:
                    parts.Add(L(0.26f, 0.22f, 0.20f, 0.82f, 0.05f, 0.65f));
                    parts.Add(L(0.74f, 0.22f, 0.80f, 0.82f, 0.05f, 0.65f));
                    parts.Add(L(0.20f, 0.78f, 0.80f, 0.78f, 0.035f, 0.55f));
                    parts.Add(E(0.50f, 0.24f, 0.28f, 0.12f, 0.75f));
                    for (int i = 0; i < 4; i++)
                    {
                        parts.Add(L(0.35f + i * 0.1f, 0.26f, 0.35f + i * 0.1f, 0.76f, 0.009f, 1f));
                    }

                    break;
                case IconShape.Whip:
                    parts.Add(L(0.12f, 0.12f, 0.30f, 0.30f, 0.045f, 0.5f));
                    for (int i = 0; i < 6; i++)
                    {
                        float t0 = i / 6f;
                        float t1 = (i + 1) / 6f;
                        parts.Add(L(0.30f + t0 * 0.56f, 0.30f + Mathf.Sin(t0 * 9f) * 0.12f + t0 * 0.4f, 0.30f + t1 * 0.56f,
                            0.30f + Mathf.Sin(t1 * 9f) * 0.12f + t1 * 0.4f, 0.025f, 0.9f));
                    }

                    break;
                case IconShape.Book:
                    parts.Add(B(0.50f, 0.50f, 0.32f, 0.38f, 0f, 0.04f, 0.6f));
                    parts.Add(B(0.54f, 0.50f, 0.24f, 0.33f, 0f, 0.02f, 1f));
                    parts.Add(B(0.22f, 0.50f, 0.04f, 0.38f, 0f, 0.02f, 0.45f));
                    parts.Add(C(0.56f, 0.55f, 0.08f, 0.7f));
                    break;
                case IconShape.Katar:
                    parts.Add(P(1f, 0.50f, 0.92f, 0.36f, 0.42f, 0.64f, 0.42f));
                    parts.Add(B(0.50f, 0.34f, 0.20f, 0.05f, 0f, 0.02f, 0.55f));
                    parts.Add(L(0.34f, 0.10f, 0.34f, 0.32f, 0.03f, 0.55f));
                    parts.Add(L(0.66f, 0.10f, 0.66f, 0.32f, 0.03f, 0.55f));
                    parts.Add(L(0.34f, 0.18f, 0.66f, 0.18f, 0.03f, 0.45f));
                    break;
                case IconShape.Gun:
                    parts.Add(B(0.52f, 0.62f, 0.36f, 0.08f, 0f, 0.03f, 0.8f));
                    parts.Add(B(0.30f, 0.40f, 0.08f, 0.18f, -18f, 0.03f, 0.5f));
                    parts.Add(O(0.42f, 0.48f, 0.06f, 0.015f, 0.6f));
                    parts.Add(C(0.86f, 0.62f, 0.05f, 1f));
                    break;
                case IconShape.Shuriken:
                    parts.Add(P(1f, 0.50f, 0.94f, 0.58f, 0.58f, 0.94f, 0.50f, 0.58f, 0.42f, 0.50f, 0.06f, 0.42f, 0.42f, 0.06f, 0.50f, 0.42f, 0.58f));
                    parts.Add(C(0.50f, 0.50f, 0.07f, 0.4f));
                    break;
                case IconShape.CatStaff:
                    parts.Add(L(0.22f, 0.08f, 0.60f, 0.66f, 0.035f, 0.55f));
                    parts.Add(C(0.68f, 0.76f, 0.13f));
                    parts.Add(C(0.60f, 0.92f, 0.045f));
                    parts.Add(C(0.70f, 0.94f, 0.045f));
                    parts.Add(C(0.80f, 0.88f, 0.045f));
                    break;
                case IconShape.Helm:
                    parts.Add(P(1f, 0.16f, 0.36f, 0.18f, 0.60f, 0.30f, 0.78f, 0.50f, 0.86f, 0.70f, 0.78f, 0.82f, 0.60f, 0.84f, 0.36f));
                    parts.Add(B(0.50f, 0.34f, 0.38f, 0.06f, 0f, 0.03f, 0.6f));
                    parts.Add(B(0.50f, 0.28f, 0.04f, 0.12f, 0f, 0.02f, 0.6f));
                    parts.Add(L(0.18f, 0.56f, 0.06f, 0.82f, 0.04f, 0.85f));
                    parts.Add(L(0.82f, 0.56f, 0.94f, 0.82f, 0.04f, 0.85f));
                    break;
                case IconShape.Hat:
                    parts.Add(E(0.50f, 0.36f, 0.42f, 0.11f, 0.65f));
                    parts.Add(P(1f, 0.30f, 0.38f, 0.36f, 0.72f, 0.50f, 0.82f, 0.64f, 0.72f, 0.70f, 0.38f));
                    parts.Add(B(0.50f, 0.42f, 0.21f, 0.04f, 0f, 0.01f, 0.5f));
                    break;
                case IconShape.Glasses:
                    parts.Add(O(0.30f, 0.50f, 0.15f, 0.035f));
                    parts.Add(O(0.70f, 0.50f, 0.15f, 0.035f));
                    parts.Add(E(0.30f, 0.50f, 0.115f, 0.115f, 0.45f));
                    parts.Add(E(0.70f, 0.50f, 0.115f, 0.115f, 0.45f));
                    parts.Add(L(0.45f, 0.53f, 0.55f, 0.53f, 0.025f));
                    break;
                case IconShape.Mask:
                    parts.Add(P(1f, 0.12f, 0.62f, 0.50f, 0.72f, 0.88f, 0.62f, 0.82f, 0.36f, 0.50f, 0.18f, 0.18f, 0.36f));
                    parts.Add(E(0.33f, 0.52f, 0.09f, 0.05f, 0.2f));
                    parts.Add(E(0.67f, 0.52f, 0.09f, 0.05f, 0.2f));
                    break;
                case IconShape.Armor:
                    parts.Add(P(1f, 0.20f, 0.84f, 0.38f, 0.90f, 0.50f, 0.80f, 0.62f, 0.90f, 0.80f, 0.84f, 0.88f, 0.62f, 0.76f, 0.58f, 0.74f, 0.12f,
                        0.26f, 0.12f, 0.24f, 0.58f, 0.12f, 0.62f));
                    parts.Add(B(0.50f, 0.32f, 0.25f, 0.045f, 0f, 0.01f, 0.5f));
                    parts.Add(L(0.50f, 0.42f, 0.50f, 0.76f, 0.012f, 0.7f));
                    break;
                case IconShape.Shield:
                    parts.Add(P(0.65f, 0.16f, 0.82f, 0.84f, 0.82f, 0.82f, 0.42f, 0.50f, 0.08f, 0.18f, 0.42f));
                    parts.Add(P(1f, 0.24f, 0.75f, 0.76f, 0.75f, 0.74f, 0.44f, 0.50f, 0.17f, 0.26f, 0.44f));
                    parts.Add(C(0.50f, 0.52f, 0.08f, 0.6f));
                    break;
                case IconShape.Cloak:
                    parts.Add(P(1f, 0.36f, 0.86f, 0.64f, 0.86f, 0.88f, 0.12f, 0.12f, 0.12f));
                    parts.Add(E(0.50f, 0.84f, 0.17f, 0.06f, 0.55f));
                    parts.Add(L(0.42f, 0.70f, 0.32f, 0.18f, 0.012f, 0.75f));
                    parts.Add(L(0.58f, 0.70f, 0.68f, 0.18f, 0.012f, 0.75f));
                    break;
                case IconShape.Boots:
                    parts.Add(P(1f, 0.30f, 0.88f, 0.56f, 0.88f, 0.56f, 0.36f, 0.86f, 0.28f, 0.88f, 0.12f, 0.26f, 0.12f));
                    parts.Add(B(0.43f, 0.82f, 0.15f, 0.06f, 0f, 0.02f, 0.6f));
                    parts.Add(B(0.57f, 0.15f, 0.31f, 0.04f, 0f, 0.01f, 0.45f));
                    break;
                case IconShape.Ring:
                    parts.Add(O(0.50f, 0.42f, 0.26f, 0.06f, 0.8f));
                    parts.Add(P(1f, 0.50f, 0.86f, 0.62f, 0.72f, 0.50f, 0.60f, 0.38f, 0.72f));
                    break;
                case IconShape.Amulet:
                    parts.AddRange(Arc(0.50f, 0.66f, 0.28f, 200f, 340f, 0.018f, 0.6f, 8));
                    parts.Add(P(1f, 0.50f, 0.62f, 0.66f, 0.40f, 0.50f, 0.14f, 0.34f, 0.40f));
                    parts.Add(C(0.50f, 0.40f, 0.07f, 0.6f));
                    break;
                case IconShape.Potion:
                    parts.Add(C(0.50f, 0.36f, 0.27f));
                    parts.Add(B(0.50f, 0.68f, 0.09f, 0.12f, 0f, 0.02f, 0.9f));
                    parts.Add(B(0.50f, 0.84f, 0.11f, 0.05f, 0f, 0.02f, 0.45f));
                    parts.Add(E(0.40f, 0.44f, 0.06f, 0.09f, 1.15f));
                    break;
                case IconShape.Herb:
                    parts.Add(L(0.50f, 0.10f, 0.50f, 0.60f, 0.025f, 0.55f));
                    parts.Add(new Box { C = new Vector2(0.34f, 0.54f), H = new Vector2(0.16f, 0.07f), Angle = 35f, Round = 0.07f, Tone = 1f });
                    parts.Add(new Box { C = new Vector2(0.66f, 0.62f), H = new Vector2(0.16f, 0.07f), Angle = -35f, Round = 0.07f, Tone = 0.85f });
                    parts.Add(new Box { C = new Vector2(0.50f, 0.78f), H = new Vector2(0.07f, 0.15f), Angle = 0f, Round = 0.07f, Tone = 0.95f });
                    break;
                case IconShape.Food:
                    parts.Add(C(0.44f, 0.42f, 0.28f));
                    parts.Add(C(0.58f, 0.44f, 0.24f));
                    parts.Add(L(0.52f, 0.66f, 0.56f, 0.82f, 0.025f, 0.45f));
                    parts.Add(new Box { C = new Vector2(0.66f, 0.80f), H = new Vector2(0.10f, 0.045f), Angle = 25f, Round = 0.045f, Tone = 0.7f });
                    break;
                case IconShape.Scroll:
                    parts.Add(B(0.50f, 0.50f, 0.28f, 0.30f, 0f, 0.02f, 1f));
                    parts.Add(E(0.50f, 0.82f, 0.34f, 0.07f, 0.65f));
                    parts.Add(E(0.50f, 0.18f, 0.34f, 0.07f, 0.65f));
                    for (int i = 0; i < 3; i++)
                    {
                        parts.Add(L(0.32f, 0.62f - i * 0.12f, 0.68f, 0.62f - i * 0.12f, 0.012f, 0.45f));
                    }

                    break;
                case IconShape.Chest:
                    parts.Add(B(0.50f, 0.36f, 0.38f, 0.22f, 0f, 0.03f, 0.75f));
                    parts.Add(E(0.50f, 0.60f, 0.38f, 0.16f, 0.9f));
                    parts.Add(B(0.50f, 0.56f, 0.40f, 0.03f, 0f, 0.01f, 0.5f));
                    parts.Add(B(0.50f, 0.48f, 0.06f, 0.08f, 0f, 0.02f, 1.1f));
                    break;
                case IconShape.Ore:
                    parts.Add(P(0.75f, 0.14f, 0.30f, 0.30f, 0.74f, 0.62f, 0.86f, 0.88f, 0.56f, 0.76f, 0.18f, 0.40f, 0.12f));
                    parts.Add(P(1f, 0.30f, 0.74f, 0.62f, 0.86f, 0.58f, 0.52f, 0.36f, 0.46f));
                    break;
                case IconShape.Gem:
                    parts.Add(P(0.8f, 0.14f, 0.62f, 0.30f, 0.82f, 0.70f, 0.82f, 0.86f, 0.62f, 0.50f, 0.12f));
                    parts.Add(P(1.1f, 0.30f, 0.82f, 0.70f, 0.82f, 0.60f, 0.62f, 0.40f, 0.62f));
                    parts.Add(P(0.65f, 0.40f, 0.62f, 0.60f, 0.62f, 0.50f, 0.12f));
                    break;
                case IconShape.Rune:
                    parts.Add(new Box { C = new Vector2(0.50f, 0.50f), H = new Vector2(0.28f, 0.38f), Angle = 0f, Round = 0.12f, Tone = 0.6f });
                    parts.Add(L(0.42f, 0.24f, 0.42f, 0.76f, 0.03f, 1.1f));
                    parts.Add(L(0.42f, 0.62f, 0.62f, 0.74f, 0.03f, 1.1f));
                    parts.Add(L(0.42f, 0.46f, 0.62f, 0.58f, 0.03f, 1.1f));
                    break;
                case IconShape.Card:
                    parts.Add(B(0.50f, 0.50f, 0.30f, 0.40f, -8f, 0.04f, 1f));
                    parts.Add(B(0.49f, 0.58f, 0.22f, 0.20f, -8f, 0.02f, 0.55f));
                    parts.Add(L(0.33f, 0.28f, 0.62f, 0.24f, 0.015f, 0.55f));
                    break;
                case IconShape.Egg:
                    parts.Add(E(0.50f, 0.46f, 0.28f, 0.37f));
                    parts.Add(C(0.40f, 0.40f, 0.05f, 0.7f));
                    parts.Add(C(0.60f, 0.60f, 0.06f, 0.7f));
                    parts.Add(C(0.56f, 0.28f, 0.04f, 0.7f));
                    break;
                case IconShape.Arrow:
                    parts.Add(L(0.18f, 0.18f, 0.76f, 0.76f, 0.022f, 0.7f));
                    parts.Add(P(1f, 0.90f, 0.90f, 0.66f, 0.80f, 0.80f, 0.66f));
                    parts.Add(P(0.85f, 0.12f, 0.30f, 0.24f, 0.24f, 0.30f, 0.12f, 0.20f, 0.12f, 0.12f, 0.20f));
                    break;
                case IconShape.Bullet:
                    parts.Add(B(0.50f, 0.40f, 0.12f, 0.24f, 0f, 0.03f, 0.7f));
                    parts.Add(E(0.50f, 0.66f, 0.12f, 0.16f, 1f));
                    parts.Add(B(0.50f, 0.18f, 0.14f, 0.03f, 0f, 0.01f, 0.5f));
                    break;
                case IconShape.Feather:
                    parts.Add(new Box { C = new Vector2(0.54f, 0.56f), H = new Vector2(0.11f, 0.34f), Angle = -35f, Round = 0.11f, Tone = 1f });
                    parts.Add(L(0.22f, 0.14f, 0.74f, 0.86f, 0.015f, 0.55f));
                    break;
                case IconShape.Branch:
                    parts.Add(L(0.20f, 0.10f, 0.56f, 0.86f, 0.045f, 0.65f));
                    parts.Add(L(0.40f, 0.52f, 0.74f, 0.66f, 0.03f, 0.65f));
                    parts.Add(L(0.34f, 0.38f, 0.16f, 0.58f, 0.025f, 0.65f));
                    parts.Add(E(0.78f, 0.70f, 0.08f, 0.05f, 1f));
                    parts.Add(E(0.60f, 0.90f, 0.05f, 0.08f, 1f));
                    break;
                case IconShape.Coin:
                    parts.Add(C(0.50f, 0.50f, 0.34f, 0.85f));
                    parts.Add(O(0.50f, 0.50f, 0.25f, 0.025f, 1.1f));
                    parts.Add(L(0.42f, 0.36f, 0.42f, 0.64f, 0.035f, 1.1f));
                    parts.Add(L(0.42f, 0.64f, 0.60f, 0.54f, 0.035f, 1.1f));
                    break;
                case IconShape.Wings:
                    parts.Add(P(1f, 0.48f, 0.42f, 0.10f, 0.84f, 0.18f, 0.58f, 0.06f, 0.60f, 0.20f, 0.34f, 0.48f, 0.30f));
                    parts.Add(P(1f, 0.52f, 0.42f, 0.90f, 0.84f, 0.82f, 0.58f, 0.94f, 0.60f, 0.80f, 0.34f, 0.52f, 0.30f));
                    parts.Add(C(0.50f, 0.36f, 0.06f, 0.6f));
                    break;
                case IconShape.Aura:
                    parts.Add(O(0.50f, 0.30f, 0.30f, 0.035f, 0.85f));
                    parts.Add(C(0.26f, 0.54f, 0.05f));
                    parts.Add(C(0.50f, 0.70f, 0.06f));
                    parts.Add(C(0.74f, 0.54f, 0.05f));
                    parts.Add(C(0.38f, 0.84f, 0.035f));
                    parts.Add(C(0.64f, 0.88f, 0.035f));
                    break;
                case IconShape.Wig:
                    parts.Add(C(0.50f, 0.62f, 0.30f));
                    parts.Add(E(0.26f, 0.36f, 0.10f, 0.26f, 0.9f));
                    parts.Add(E(0.74f, 0.36f, 0.10f, 0.26f, 0.9f));
                    parts.Add(E(0.50f, 0.50f, 0.17f, 0.15f, 0.35f));
                    break;
                case IconShape.Ears:
                    parts.Add(P(1f, 0.14f, 0.30f, 0.26f, 0.88f, 0.44f, 0.38f));
                    parts.Add(P(1f, 0.86f, 0.30f, 0.74f, 0.88f, 0.56f, 0.38f));
                    parts.Add(P(0.6f, 0.22f, 0.36f, 0.27f, 0.70f, 0.37f, 0.40f));
                    parts.Add(P(0.6f, 0.78f, 0.36f, 0.73f, 0.70f, 0.63f, 0.40f));
                    parts.Add(E(0.50f, 0.26f, 0.38f, 0.08f, 0.75f));
                    break;
                default:
                    parts.Add(P(1f, 0.24f, 0.62f, 0.18f, 0.20f, 0.82f, 0.20f, 0.76f, 0.62f));
                    parts.Add(E(0.50f, 0.66f, 0.20f, 0.07f, 0.6f));
                    parts.Add(L(0.40f, 0.76f, 0.60f, 0.76f, 0.03f, 0.5f));
                    break;
            }

            return parts;
        }
    }
}
