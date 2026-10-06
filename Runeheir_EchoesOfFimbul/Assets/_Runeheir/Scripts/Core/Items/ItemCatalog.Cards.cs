using Runeheir.Combat;
using Runeheir.Stats;

namespace Runeheir.Items
{
    /// <summary>
    /// GDD §6: the 35 monster Soul Cards. Each compounds into one socket of the equipment family in brackets.
    /// Drop rates (0.5%–1%, MVPs 0.01%) live on the monsters (<c>MonsterCatalog</c>).
    /// </summary>
    public static partial class ItemCatalog
    {
        private static void RegisterCards()
        {
            // ---- Tier 1: starter cards (Lv 1–60)
            Card("rune_spore_card", "Rune Spore Card", EquipKind.Headgear, 1, "+100 Max HP, +5 HP regen.",
                new EquipEffect { Modifiers = new StatModifiers { MaxHp = 100, HpRegenFlat = 5 } });
            Card("toxic_spore_card", "Toxic Spore Card", EquipKind.Weapon, 1, "5% chance on melee hit to Poison the target. +5 ATK.",
                new EquipEffect { Modifiers = new StatModifiers { Atk = 5 } }.Proc(new OnHitEffect { Status = StatusEffect.Poison, ChancePercent = 5f, Duration = 10f }));
            Card("forest_imp_card", "Forest Imp Card", EquipKind.Accessory, 1, "+3 AGI, +2 FLEE.",
                new EquipEffect { Modifiers = new StatModifiers { Flee = 2 }.SetStat(StatType.Agi, 3) });
            Card("horned_grazer_card", "Horned Grazer Card", EquipKind.Shield, 1, "Take 30% less damage from Beasts.", new EquipEffect().From(Race.Beast, -30f));
            Card("field_beetle_card", "Field Beetle Card", EquipKind.Armor, 1, "+10% Max HP, +5 DEF.",
                new EquipEffect { Modifiers = new StatModifiers { MaxHpPercent = 10f, Def = 5 } });
            Card("wood_sprite_card", "Wood Sprite Card", EquipKind.Weapon, 1, "+20% physical damage against Water monsters.", new EquipEffect().Vs(Element.Water, 20f));

            // ---- Tier 2: forest and crypt (Lv 61–150)
            Card("wild_boar_card", "Wild Boar Card", EquipKind.Footgear, 2, "+10% movement speed, +3 STR.",
                new EquipEffect { Modifiers = new StatModifiers { MoveSpeedPercent = 10f }.SetStat(StatType.Str, 3) });
            Card("dire_wolf_card", "Dire Wolf Card", EquipKind.Weapon, 2, "+20% critical damage, +5 CRIT.",
                new EquipEffect { Modifiers = new StatModifiers { CritDamagePercent = 20f, Crit = 5f } });
            Card("forest_outlaw_card", "Forest Outlaw Card", EquipKind.Weapon, 2, "+20% physical damage against Demi-Humans and players.", new EquipEffect().Vs(Race.DemiHuman, 20f));
            Card("draugr_footman_card", "Draugr Footman Card", EquipKind.Shield, 2, "Take 30% less damage from Demi-Humans and players.", new EquipEffect().From(Race.DemiHuman, -30f));
            Card("crypt_bat_card", "Crypt Bat Card", EquipKind.Weapon, 2, "Physical attacks heal you for 5% of the damage dealt.",
                new EquipEffect { Modifiers = new StatModifiers { LifeStealPercent = 5f } });
            Card("cave_crawler_card", "Cave Crawler Card", EquipKind.Armor, 2, "Your armor becomes Earth element. +10% resistance to Stone Curse.",
                new EquipEffect { ArmorElement = Element.Earth, ResistStatus = StatusEffect.StoneCurse, ResistPercent = 10f });
            Card("ghoul_card", "Ghoul Card", EquipKind.Weapon, 2, "+20% physical damage against Undead.", new EquipEffect().Vs(Race.Undead, 20f));
            Card("fjord_harpy_card", "Fjord Harpy Card", EquipKind.Garment, 2, "+20 FLEE, +3 AGI.",
                new EquipEffect { Modifiers = new StatModifiers { Flee = 20 }.SetStat(StatType.Agi, 3) });

            // ---- Tier 3: fjord and catacomb (Lv 151–220)
            Card("frost_wolf_card", "Frost Wolf Card", EquipKind.Armor, 3, "Your armor becomes Water element. Immune to Freeze.",
                new EquipEffect { ArmorElement = Element.Water }.Immune(StatusEffect.Freeze));
            Card("sea_drake_card", "Sea Drake Card", EquipKind.Weapon, 3, "+20% physical damage against Large monsters.", new EquipEffect().Vs(Size.Large, 20f));
            Card("runic_berserker_card", "Runic Berserker Card", EquipKind.Weapon, 3, "+15% physical damage against Medium monsters.", new EquipEffect().Vs(Size.Medium, 15f));
            Card("ice_golem_card", "Ice Golem Card", EquipKind.Shield, 3, "Immune to Freeze. +5 DEF.",
                new EquipEffect { Modifiers = new StatModifiers { Def = 5 } }.Immune(StatusEffect.Freeze));
            Card("crypt_wraith_card", "Crypt Wraith Card", EquipKind.Garment, 3, "Take 20% less Neutral damage.", new EquipEffect().From(Element.Neutral, -20f));
            Card("banshee_card", "Banshee Card", EquipKind.Headgear, 3, "Immune to Silence. +3 INT.",
                new EquipEffect { Modifiers = new StatModifiers().SetStat(StatType.Int, 3) }.Immune(StatusEffect.Silence));
            Card("corrupted_einherjar_card", "Corrupted Einherjar Card", EquipKind.Weapon, 3, "Physical attacks restore SP equal to 3% of the damage dealt.",
                new EquipEffect { Modifiers = new StatModifiers { SpStealPercent = 3f } });
            Card("naga_scout_card", "Naga Scout Card", EquipKind.Accessory, 3, "Your casting can't be interrupted, but casts take 15% longer.",
                new EquipEffect { Modifiers = new StatModifiers { UninterruptibleCasting = true, CastTimePercent = 15f } });

            // ---- Tier 4: endgame tundra and abyss (Lv 221–255)
            Card("jotun_brawler_card", "Jotun Brawler Card", EquipKind.Armor, 4, "+10 STR. With base STR 180+: +20 ATK and +5% physical damage.",
                new EquipEffect
                {
                    Modifiers = new StatModifiers().SetStat(StatType.Str, 10),
                    Condition = new EquipCondition
                    {
                        Stat = StatType.Str, MinBaseValue = 180,
                        Bonus = new EquipEffect { Modifiers = new StatModifiers { Atk = 20, PhysicalDamagePercent = 5f } },
                    },
                });
            Card("snow_harpy_card", "Snow Harpy Card", EquipKind.Footgear, 4, "+10% Max HP and Max SP.",
                new EquipEffect { Modifiers = new StatModifiers { MaxHpPercent = 10f, MaxSpPercent = 10f } });
            Card("frost_wyrm_card", "Frost Wyrm Card", EquipKind.Weapon, 4, "+15% magic damage; spells ignore 10% of the target's MDEF.",
                new EquipEffect { Modifiers = new StatModifiers { MagicDamagePercent = 15f, MdefBypassPercent = 10f } });
            Card("abyssal_leech_card", "Abyssal Leech Card", EquipKind.Accessory, 4, "Each hit drains up to 5 SP from the target into you. (PvP: crits burn 10% SP, Phase 6.)",
                new EquipEffect { Modifiers = new StatModifiers { SpDrainOnHit = 5 } });
            Card("frozen_revenant_card", "Frozen Revenant Card", EquipKind.Armor, 4, "Your armor becomes Undead element. Immune to Freeze and Stone Curse.",
                new EquipEffect { ArmorElement = Element.Undead }.Immune(StatusEffect.Freeze, StatusEffect.StoneCurse));
            Card("hels_executioner_card", "Hel's Executioner Card", EquipKind.Weapon, 4, "5% chance on melee hit of a Mortal Stagger (breaks poise outright).",
                new EquipEffect().Proc(new OnHitEffect { Kind = OnHitKind.PoiseBreak, ChancePercent = 5f }));

            // ---- Mini-bosses (2-hour field spawns)
            Card("elder_direwolf_card", "Elder Direwolf Card", EquipKind.Headgear, 5, "+5 all stats, +10 FLEE.",
                new EquipEffect { Modifiers = new StatModifiers { Flee = 10 }.AddAllStats(5) });
            Card("draugr_warlord_card", "Draugr Warlord Card", EquipKind.Shield, 5, "Reflects 10% of physical melee damage back at the attacker. +10 DEF.",
                new EquipEffect { Modifiers = new StatModifiers { ReflectMeleePercent = 10f, Def = 10 } });
            Card("ancient_golem_card", "Ancient Golem Card", EquipKind.Weapon, 5, "Your weapon becomes unbreakable. +25 ATK.",
                new EquipEffect { Modifiers = new StatModifiers { Atk = 25 }, Unbreakable = true });
            Card("naga_queen_card", "Naga Queen Card", EquipKind.Accessory, 5, "Skill cooldowns 10% shorter. +5 INT.",
                new EquipEffect { Modifiers = new StatModifiers { CooldownPercent = -10f }.SetStat(StatType.Int, 5) });

            // ---- MVP world bosses (the Holy Trinity, 0.01%)
            Card("fenrir_card", "Fenrir Card", EquipKind.Garment, 6, "+10 ASPD, +50 FLEE. 5% chance on melee hit to enter Berserk Wolf Form for 10s (CRIT doubled, no stagger).",
                new EquipEffect { Modifiers = new StatModifiers { AspdFlat = 10f, Flee = 50 } }
                    .Proc(new OnHitEffect { Kind = OnHitKind.SelfBuff, BuffId = BuffCatalog.WolfForm, ChancePercent = 5f, Duration = 10f }));
            Card("hels_vanguard_card", "Hel's Vanguard Card", EquipKind.Armor, 6, "Your armor becomes Ghost element: Neutral attacks deal only 25%.",
                new EquipEffect { ArmorElement = Element.Ghost });
            Card("jormungandrs_brood_card", "Jormungandr's Brood Card", EquipKind.Weapon, 6,
                "Attacks ignore 40% of DEF and MDEF. Melee hits inflict Frostbite for 5s (half speed and ASPD; unblockable).",
                new EquipEffect { Modifiers = new StatModifiers { DefBypassPercent = 40f, MdefBypassPercent = 40f } }
                    .Proc(new OnHitEffect { Status = StatusEffect.Frostbite, ChancePercent = 100f, Duration = 5f, Guaranteed = true }));
        }

        /// <param name="tier">1–4 GDD tiers, 5 mini-boss, 6 MVP (sets the sell price).</param>
        private static void Card(string id, string name, EquipKind target, int tier, string description, EquipEffect effect)
        {
            int[] prices = { 0, 10000, 30000, 80000, 160000, 400000, 2000000 };
            Register(new ItemDefinition
            {
                Id = id, Name = name, Kind = ItemKind.Card, CardTarget = target, Weight = 1, Price = prices[tier],
                Description = $"[{EquipKinds.Label(target)}] {description}", Effect = effect,
                IconLabel = "CRD", IconColorHex = tier >= 6 ? "#F1C40F" : tier == 5 ? "#E67E22" : "#BB8FCE",
            });
        }
    }
}
