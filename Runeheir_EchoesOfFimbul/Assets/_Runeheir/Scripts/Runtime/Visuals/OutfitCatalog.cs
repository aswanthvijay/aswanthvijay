using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Jobs;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// The Blender-built character models (Tools/Blender/build_characters.py → Resources/Characters): one rigged body per
    /// outfit and gender, the hair styles, and the palettes in outfits.json. Jobs without an outfit of their own wear the
    /// "common" one, tinted with their job color.
    /// </summary>
    public static class OutfitCatalog
    {
        public const string Folder = "Characters/";
        public const string CommonKey = "common";

        /// <summary>Material slots that are the character's own colors rather than the outfit's.</summary>
        public static readonly string[] CharacterSlots = { "Skin", "Hair", "Brow", "Eye", "EyeWhite", "Lash", "Highlight", "Mouth" };

        /// <summary>Slots drawn without the ink outline (the face's features are ink themselves; glows shine).</summary>
        private static readonly HashSet<string> NoOutline = new HashSet<string> { "Brow", "Eye", "EyeWhite", "Lash", "Highlight", "Mouth" };

        private static readonly HashSet<string> Glowing = new HashSet<string> { "Glow", "Highlight" };

        private static readonly Color EyeColor = new Color(0.20f, 0.38f, 0.58f);
        private static readonly Color EyeWhiteColor = new Color(0.985f, 0.98f, 0.965f);
        private static readonly Color LashColor = new Color(0.14f, 0.10f, 0.11f);
        private static readonly Color MouthColor = new Color(0.63f, 0.32f, 0.29f);

        private static Dictionary<string, Outfit> s_byKey;
        private static Dictionary<JobId, Outfit> s_byJob;
        private static readonly Dictionary<string, string> s_weaponSlots = new Dictionary<string, string>();

        public sealed class Outfit
        {
            public string Key;
            public readonly List<JobId> Jobs = new List<JobId>();
            public readonly Dictionary<string, string> Slots = new Dictionary<string, string>();
        }

        [Serializable]
        private sealed class SlotData
        {
            public string slot;
            public string color;
        }

        [Serializable]
        private sealed class OutfitData
        {
            public string key;
            public string[] jobs;
            public SlotData[] slots;
        }

        [Serializable]
        private sealed class CatalogData
        {
            public OutfitData[] outfits;
            public int hairStyles;
            public SlotData[] weaponSlots;
        }

        public static IReadOnlyDictionary<string, Outfit> All
        {
            get
            {
                Load();
                return s_byKey;
            }
        }

        public static int HairStyleCount { get; private set; }

        /// <summary>The outfit a job wears (its own, or the common one). Null job: NPCs and humanoid monsters.</summary>
        public static Outfit ForJob(JobId? job)
        {
            Load();
            if (job.HasValue && s_byJob.TryGetValue(job.Value, out var own))
            {
                return own;
            }

            s_byKey.TryGetValue(CommonKey, out var common);
            return common;
        }

        public static string ModelPath(string key, Gender gender)
        {
            return Folder + key + (gender == Gender.Female ? "_f" : "_m");
        }

        /// <summary>The rigged body for an outfit, or null when the models aren't in the project.</summary>
        public static GameObject Model(Outfit outfit, Gender gender)
        {
            if (outfit == null)
            {
                return null;
            }

            var model = Resources.Load<GameObject>(ModelPath(outfit.Key, gender));
            return model != null || outfit.Key == CommonKey ? model : Resources.Load<GameObject>(ModelPath(CommonKey, gender));
        }

        public static GameObject Hair(int style)
        {
            return Resources.Load<GameObject>(Folder + "hair_" + Mathf.Max(0, style));
        }

        /// <summary>The color a material slot gets for this outfit and look.</summary>
        public static Color SlotColor(Outfit outfit, string slot, AvatarLook look)
        {
            switch (slot)
            {
                case "Skin": return look.Skin;
                case "Hair": return look.Hair;
                case "Brow": return Shade(look.Hair, 0.78f);
                case "Eye": return EyeColor;
                case "EyeWhite": return EyeWhiteColor;
                case "Lash": return LashColor;
                case "Highlight": return Color.white;
                case "Mouth": return MouthColor;
                case "Garment": return GarmentColor(look);
                case "GarmentDark": return Shade(GarmentColor(look), 0.6f);
            }

            if (outfit != null && outfit.Slots.TryGetValue(slot, out string value))
            {
                if (value == "$outfit")
                {
                    return look.Outfit;
                }

                if (value == "$outfitDark")
                {
                    return Shade(look.Outfit, 0.62f);
                }

                return RuntimeMaterials.Hex(value);
            }

            return new Color(1f, 0f, 1f); // an unknown slot shows up loudly
        }

        /// <summary>The toon material for a slot (shared and cached per color).</summary>
        public static Material SlotMaterial(Outfit outfit, string slot, AvatarLook look)
        {
            var color = SlotColor(outfit, slot, look);
            if (Glowing.Contains(slot))
            {
                return RuntimeMaterials.Glow(color, color * (slot == "Glow" ? 1.6f : 0.9f));
            }

            return RuntimeMaterials.Lit(color, NoOutline.Contains(slot) ? 0f : RuntimeMaterials.CharacterOutline);
        }

        public static bool IsKnownSlot(Outfit outfit, string slot)
        {
            return Array.IndexOf(CharacterSlots, slot) >= 0 || slot == "Garment" || slot == "GarmentDark" || (outfit != null && outfit.Slots.ContainsKey(slot));
        }

        // ------------------------------------------------------------------ weapons and worn cloaks
        public const string WeaponFolder = Folder + "Weapons/";

        /// <summary>The weapon model for a type (Tools/Blender/rh_weapons.py), or null when there is none (bare hands).</summary>
        public static GameObject WeaponModel(WeaponType weapon)
        {
            return weapon == WeaponType.Unarmed ? null : Resources.Load<GameObject>(WeaponFolder + weapon);
        }

        public static bool IsKnownWeaponSlot(string slot)
        {
            Load();
            return s_weaponSlots.ContainsKey(slot);
        }

        /// <summary>A weapon material slot (Steel, Wood, Gem...) through the toon shader; gems glow, strings carry no ink.</summary>
        public static Material WeaponMaterial(string slot)
        {
            Load();
            var color = s_weaponSlots.TryGetValue(slot, out string hex) ? RuntimeMaterials.Hex(hex) : new Color(1f, 0f, 1f);
            if (slot == "Gem")
            {
                return RuntimeMaterials.Glow(color, color * 1.4f);
            }

            return RuntimeMaterials.Lit(color, slot == "String" ? 0f : RuntimeMaterials.CharacterOutline * 0.75f);
        }

        /// <summary>A cloak (any garment but wings and mufflers) is drawn as the outfit's fitted "GarmentCape".</summary>
        public static bool IsCloak(string garmentId)
        {
            return !string.IsNullOrEmpty(garmentId) && !garmentId.Contains("wings") && !garmentId.Contains("muffler");
        }

        public static Color GarmentColor(AvatarLook look)
        {
            var item = ItemCatalog.Get(look.Garment);
            return item == null ? new Color(0.47f, 0.26f, 0.07f) : RuntimeMaterials.Hex(item.ViewColorHex ?? item.IconColorHex ?? "#784212");
        }

        /// <summary>The color of this outfit's weapon trails: its rune glow, washed toward white.</summary>
        public static Color TrailColor(Outfit outfit)
        {
            var glow = outfit != null && outfit.Slots.TryGetValue("Glow", out string hex) ? RuntimeMaterials.Hex(hex) : new Color(0.5f, 0.82f, 1f);
            var color = Color.Lerp(Color.white, glow, 0.55f);
            color.a = 0.75f;
            return color;
        }

        private static Color Shade(Color color, float k)
        {
            return new Color(color.r * k, color.g * k, color.b * k, 1f);
        }

        private static void Load()
        {
            if (s_byKey != null)
            {
                return;
            }

            s_byKey = new Dictionary<string, Outfit>();
            s_byJob = new Dictionary<JobId, Outfit>();
            var text = Resources.Load<TextAsset>(Folder + "outfits");
            if (text == null)
            {
                return;
            }

            var data = JsonUtility.FromJson<CatalogData>(text.text);
            HairStyleCount = data.hairStyles;
            foreach (var slot in data.weaponSlots ?? Array.Empty<SlotData>())
            {
                s_weaponSlots[slot.slot] = slot.color;
            }

            foreach (var entry in data.outfits ?? Array.Empty<OutfitData>())
            {
                var outfit = new Outfit { Key = entry.key };
                foreach (var slot in entry.slots ?? Array.Empty<SlotData>())
                {
                    outfit.Slots[slot.slot] = slot.color;
                }

                foreach (string job in entry.jobs ?? Array.Empty<string>())
                {
                    if (Enum.TryParse(job, out JobId id))
                    {
                        outfit.Jobs.Add(id);
                        s_byJob[id] = outfit;
                    }
                    else
                    {
                        Debug.LogWarning($"[Runeheir] outfits.json: unknown job '{job}' for outfit '{entry.key}'.");
                    }
                }

                s_byKey[entry.key] = outfit;
            }
        }
    }
}
