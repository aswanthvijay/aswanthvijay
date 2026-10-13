"""Ragnarok skills (rAthena aegis names) -> Runeheir skills.

Runeheir's 282 skills are its own Norse take on Ragnarok's trees (Docs/Phase7_Classes.md), so most of Ragnarok's job skills
have a Runeheir counterpart: Fire Bolt is Muspel Bolt, Heal is Eir's Blessing, Lex Aeterna is Norns' Doom. Item scripts
that cast, grant or boost a skill go through this table. Skills with no counterpart (monster-only NPC_ skills, renewal
third-job skills, a few utility skills) keep a Norse-flavoured English label for tooltips and are noted, not active.
"""

RUNEHEIR = {
    # Novice / Initiate
    "NV_BASIC": "basic_training", "NV_FIRSTAID": "first_aid", "NV_TRICKDEAD": "feign_death",
    # Swordman / Warrior
    "SM_SWORD": "sword_mastery", "SM_TWOHAND": "sword_mastery", "SM_RECOVERY": "iron_constitution", "SM_BASH": "bash",
    "SM_PROVOKE": "provoke", "SM_MAGNUM": "magnum_break", "SM_ENDURE": "endure", "SM_SELFPROVOKE": "provoke",
    # Knight / Berserker, Lord Knight / Einherjar
    "KN_SPEARMASTERY": "spear_mastery", "KN_SPEARSTAB": "spear_stab", "KN_BRANDISHSPEAR": "brandish_spear",
    "KN_TWOHANDQUICKEN": "battle_frenzy", "KN_AUTOCOUNTER": "riposte", "KN_BOWLINGBASH": "cleaving_strike",
    "KN_CHARGEATK": "blood_rush", "LK_AURABLADE": "valhallas_edge", "LK_PARRYING": "parry", "LK_BERSERK": "berserkergang",
    "LK_SPIRALPIERCE": "gungnir_thrust", "LK_FURY": "berserkergang",
    # Crusader / Guardian, Paladin / Valkyrie
    "CR_AUTOGUARD": "guardians_oath", "CR_SHIELDCHARGE": "shield_bash", "CR_HOLYCROSS": "sacred_cross",
    "CR_GRANDCROSS": "radiant_cross", "CR_DEFENDER": "holdfast", "CR_TRUST": "faith", "PA_PRESSURE": "spear_of_light",
    "PA_SHIELDCHAIN": "shield_barrage",
    # Mage / Mystic, Wizard / Sorcerer, High Wizard / Archmage
    "MG_FIREBOLT": "muspel_bolt", "MG_COLDBOLT": "frost_spike", "MG_LIGHTNINGBOLT": "thunder_rune", "MG_SOULSTRIKE": "soul_lance",
    "MG_FROSTDIVER": "frost_lock", "MG_SRECOVERY": "mana_focus", "MG_FIREBALL": "surtrs_orb", "MG_STONECURSE": "troll_stone",
    "MG_ENERGYCOAT": "seidr_coat", "MG_SAFETYWALL": "rampart_of_runar", "MG_FIREWALL": "muspel_wall",
    "WZ_METEOR": "muspel_rain", "WZ_JUPITEL": "storm_lance", "WZ_FROSTNOVA": "frost_nova", "WZ_EARTHSPIKE": "earthen_spikes",
    "WZ_HEAVENDRIVE": "earthen_spikes", "WZ_STORMGUST": "glacial_tempest", "WZ_VERMILION": "heavens_wrath",
    "WZ_QUAGMIRE": "bog_of_niflheim", "HW_MAGICPOWER": "rune_amplify",
    # Sage, Professor / Chronomancer
    "SA_ADVANCEDBOOK": "rune_study", "SA_FREECAST": "free_cast", "SA_AUTOSPELL": "auto_rune", "SA_VOLCANO": "mystic_volcano",
    # Archer / Huntsman, Hunter / Ranger, Sniper / Deadeye
    "AC_OWL": "owls_eye", "AC_VULTURE": "vultures_eye", "AC_CONCENTRATION": "hawk_focus", "AC_DOUBLE": "double_strafe",
    "AC_SHOWER": "arrow_shower", "AC_CHARGEARROW": "gust_arrow", "HT_ANKLESNARE": "ankle_snare", "HT_BLITZBEAT": "raven_strike",
    "HT_STEELCROW": "raven_training", "HT_LANDMINE": "rune_mine", "HT_BLASTMINE": "muspel_mine", "HT_FREEZINGTRAP": "rime_trap",
    "HT_SANDMAN": "thorn_of_sleep", "SN_SHARPSHOOTING": "sharp_shot", "SN_FALCONASSAULT": "raven_assault", "SN_SIGHT": "true_sight",
    "SN_WINDWALK": "gale_step",
    # Bard / Skald, Dancer / Seidkona, Clown / Thul, Gypsy / Volva
    "BA_MUSICALLESSON": "bragis_lessons", "BA_MUSICALSTRIKE": "lay_strike", "BA_DISSONANCE": "dissonance",
    "BA_WHISTLE": "whistle_of_heimdall", "BA_ASSASSINCROSS": "sunset_lay", "BA_POEMBRAGI": "bragis_verse",
    "BA_APPLEIDUN": "iduns_apple", "DC_DANCINGLESSON": "seidr_lessons", "DC_THROWARROW": "slinging_lash", "DC_SCREAM": "heids_shriek",
    "DC_HUMMING": "spinning_hum", "DC_DONTFORGETME": "forgetful_dance", "DC_FORTUNEKISS": "freyjas_kiss",
    "DC_SERVICEFORYOU": "seidr_service", "DC_WINKCHARM": "seidr_charm", "CG_ARROWVULCAN": "bragis_volley",
    "CG_TAROTCARD": "skulds_verdict",
    # Acolyte / Devotee, Priest / Gothi, High Priest / High Gothi
    "AL_HEAL": "eirs_blessing", "AL_DP": "divine_shelter", "AL_BLESSING": "odins_blessing", "AL_INCAGI": "swift_wind",
    "AL_HOLYLIGHT": "holy_light", "AL_CURE": "purify", "AL_DECAGI": "heavy_limbs", "PR_SANCTUARY": "sanctuary",
    "PR_MAGNUS": "holy_judgment", "ALL_RESURRECTION": "return_from_hel", "PR_MAGNIFICAT": "galdr_of_eir", "PR_GLORIA": "glory_of_baldr",
    "PR_KYRIE": "svalinns_shield", "PR_IMPOSITIO": "tyrs_hand", "PR_SUFFRAGIUM": "galdr_of_haste", "PR_LEXDIVINA": "silencing_rune",
    "PR_LEXAETERNA": "norns_doom", "PR_TURNUNDEAD": "banishing_rite", "PR_STRECOVERY": "purify", "HP_ASSUMPTIO": "divine_bulwark",
    "HP_MEDITATIO": "hof_meditation", "HP_BASILICA": "hof_sanctum", "AB_JUDEX": "light_of_baldr",
    # Monk, Champion
    "MO_IRONHAND": "iron_fist", "MO_TRIPLEATTACK": "storm_fists", "MO_CALLSPIRITS": "spirit_call", "CH_SOULCOLLECT": "spirit_call",
    "MO_INVESTIGATE": "occult_strike", "MO_FINGEROFFENSIVE": "spirit_barrage", "MO_STEELBODY": "diamond_skin",
    "MO_EXTREMITYFIST": "fist_of_odin", "MO_BODYRELOCATION": "aether_snap", "MO_CHAINCOMBO": "thunder_palm",
    "MO_COMBOFINISH": "thunder_palm", "CH_PALMSTRIKE": "thunder_palm", "CH_TIGERFIST": "thunder_palm", "MO_SPIRITSRECOVERY": "zen_breath",
    # Merchant / Trader, Blacksmith / Runesmith, Whitesmith / Forgelord
    "MC_INCCARRY": "strong_back", "MC_DISCOUNT": "haggle", "MC_OVERCHARGE": "silver_tongue", "MC_MAMMONITE": "gold_strike",
    "MC_CARTREVOLUTION": "cart_charge", "MC_LOUD": "market_shout", "BS_WEAPONRESEARCH": "weaponry_research",
    "BS_SKINTEMPER": "skin_tempering", "BS_ADRENALINE": "dwarfs_fervor", "BS_WEAPONPERFECT": "true_edge", "BS_OVERTHRUST": "hammer_rhythm",
    "BS_MAXIMIZE": "full_swing", "BS_HAMMERFALL": "hammerfall", "WS_CARTTERMINATION": "cart_termination", "WS_MELTDOWN": "molten_edge",
    "WS_CARTBOOST": "wheel_rush", "WS_OVERTHRUSTMAX": "overthrust",
    # Alchemist / Brewmaster, Creator / Lifeweaver
    "AM_AXEMASTERY": "axe_mastery", "AM_LEARNINGPOTION": "potion_research", "AM_PHARMACY": "brewing", "AM_ACIDTERROR": "acid_flask",
    "AM_DEMONSTRATION": "firebomb", "AM_CANNIBALIZE": "mandrake_patch", "AM_POTIONPITCHER": "potion_toss",
    "AM_CP_WEAPON": "hallowed_coating", "AM_CP_SHIELD": "hallowed_coating", "AM_CP_ARMOR": "hallowed_coating",
    "AM_CP_HELM": "hallowed_coating", "CR_ACIDDEMONSTRATION": "acid_bloom", "CR_FULLPROTECTION": "full_coating",
    "CR_SLIMPITCHER": "bountiful_toss",
    # Thief / Scout, Assassin, Assassin Cross / Shadow Walker, Rogue / Outlaw, Stalker / Vargr
    "TF_DOUBLE": "keen_edge", "TF_MISS": "evasion_drills", "TF_STEAL": "pilfer", "TF_HIDING": "hide_in_shadows", "TF_POISON": "envenom",
    "TF_DETOXIFY": "purge_venom", "TF_SPRINKLESAND": "dust_kick", "AS_KATAR": "katar_mastery", "AS_CLOAKING": "shadow_cloak",
    "AS_SONICBLOW": "phantom_barrage", "AS_GRIMTOOTH": "underfang", "AS_VENOMDUST": "venom_dust", "AS_SPLASHER": "vipers_burst",
    "ASC_METEORASSAULT": "night_squall", "ASC_BREAKER": "soul_breaker", "ASC_EDP": "miasma_weapon", "RG_SNATCHER": "light_fingers",
    "RG_BACKSTAP": "knife_in_the_back", "RG_RAID": "ambush_raid", "RG_STEALCOIN": "cut_purse", "RG_STRIPHELM": "divest_helm",
    "RG_STRIPSHIELD": "divest_shield", "RG_STRIPARMOR": "divest_armor", "RG_STRIPWEAPON": "divest_weapon",
    "RG_PLAGIARISM": "lokis_mimicry", "ST_FULLSTRIP": "wolf_strip", "ST_CHASEWALK": "wolfs_prowl", "ST_PRESERVE": "preserve",
    "ST_REJECTSWORD": "turn_the_blade",
    # Taekwon / Glima Fighter, Star Gladiator / Sol Guardian, Soul Linker / Fylgja Caller
    "TK_RUN": "sprint", "TK_STORMKICK": "whirlwind_kick", "TK_DOWNKICK": "heel_drop", "TK_TURNKICK": "roundhouse",
    "TK_COUNTER": "glima_throw", "TK_JUMPKICK": "leaping_kick", "TK_DODGE": "tumbling", "TK_HPTIME": "warriors_rest",
    "TK_SPTIME": "warriors_rest", "SG_SUN_WARM": "sols_warmth", "SG_MOON_WARM": "manis_chill", "SG_STAR_WARM": "stars_glow",
    "SG_STAR_COMFORT": "comfort_of_stars", "SG_SUN_COMFORT": "sols_protection", "SG_FUSION": "union", "SL_SMA": "fylgja_strike",
    "SL_STIN": "spirit_push", "SL_STUN": "spirit_stun", "SL_KAIZEL": "hamingja", "SL_KAAHI": "fylgjas_mend", "SL_KAUPE": "spirit_dodge",
    "SL_KAITE": "spirit_mirror",
    # Gunslinger / Thunderer
    "GS_GLITTERING": "thors_coin", "GS_SNAKEEYE": "snake_eye", "GS_TRIPLEACTION": "triple_thunder", "GS_BULLSEYE": "thors_eye",
    "GS_RAPIDSHOWER": "rapid_thunder", "GS_DESPERADO": "storm_spin", "GS_TRACKING": "tracking", "GS_DISARM": "disarm",
    "GS_PIERCINGSHOT": "rending_shot", "GS_SPREADATTACK": "spread_shot", "GS_DUST": "dust_blast", "GS_GATLINGFEVER": "thunder_fever",
    "GS_INCREASING": "steady_aim", "GS_MADNESSCANCEL": "mjolnirs_stillness", "GS_ADJUSTMENT": "weather_eye",
    # Ninja / Nightraider
    "NJ_NINPOU": "night_training", "NJ_SYURIKEN": "throw_shuriken", "NJ_KUNAI": "throw_kunai", "NJ_HUUMA": "huuma_storm",
    "NJ_ZENYNAGE": "hurl_gold", "NJ_TATAMIGAESHI": "shield_flip", "NJ_KASUMIKIRI": "mist_slash", "NJ_SHADOWJUMP": "shadow_leap",
    "NJ_KIRIKAGE": "shadow_slash", "NJ_UTSUSEMI": "shed_skin", "NJ_BUNSINJYUTSU": "mirror_image", "NJ_KAENSIN": "muspel_blossom",
    "NJ_RAIGEKISAI": "thunder_jolt", "NJ_HYOUSENSOU": "hail_of_niflheim",
    # Summoner / Freyja's Kin
    "SU_BITE": "cats_bite", "SU_SCAROFTAROU": "bygul_claws", "SU_LUNATICCARROTBEAT": "root_smash", "SU_SV_STEMSPEAR": "silvervine_spear",
    "SU_CN_METEOR": "catnip_meteor", "SU_PICKYPECK": "falcons_of_freyja", "SU_TUNAPARTY": "fish_feast", "SU_HISS": "hiss",
    "SU_PURRING": "purr_of_comfort", "SU_CHATTERING": "chattering", "SU_POWEROFSEA": "gift_of_njord", "SU_SPIRITOFLIFE": "spirit_of_life",
    "SU_ARCLOUSEDASH": "pounce",
    # Party versions of the cash-shop scrolls
    "CASH_BLESSING": "odins_blessing", "CASH_INCAGI": "swift_wind", "CASH_ASSUMPTIO": "divine_bulwark",
}

# Labels for Ragnarok skills with no Runeheir counterpart, in the game's voice (tooltips only).
LABELS = {
    "AL_TELEPORT": "Wind-Step", "AL_ANGELUS": "Angelus", "AL_CRUCIS": "Sign of the Hammer", "AL_RUWACH": "Ljós Reveal",
    "AL_PNEUMA": "Wind Ward", "AL_HOLYWATER": "Blessed Water", "PR_ASPERSIO": "Holy Weapon", "PR_MACEMASTERY": "Mace Mastery",
    "MG_NAPALMBEAT": "Spirit Burst", "MG_THUNDERSTORM": "Thunderstorm", "MG_SIGHT": "Torch-Sight", "WZ_FIREPILLAR": "Fire Pillar",
    "WZ_WATERBALL": "Water Ball", "WZ_ESTIMATION": "Sense", "KN_PIERCE": "Pierce", "LK_CONCENTRATION": "Battle Focus",
    "LK_JOINTBEAT": "Vital Strike", "CR_SHIELDBOOMERANG": "Shield Throw", "BA_FROSTJOKER": "Frost Jest", "GS_GROUNDDRIFT": "Ground Drift",
    "NJ_ISSEN": "Final Strike", "ITEM_ENCHANTARMS": "Weapon Enchantment", "ITM_TOMAHAWK": "Throw Tomahawk", "TF_THROWSTONE": "Stone Fling",
    "TF_BACKSLIDING": "Back Slide", "TF_PICKSTONE": "Find Stone", "MC_IDENTIFY": "Appraisal", "MO_EXPLOSIONSPIRITS": "Fury",
    "NPC_CRITICALWOUND": "Critical Wounds", "NPC_HELLPOWER": "Hel's Power", "NPC_VAMPIRE_GIFT": "Draugr's Gift",
    "NPC_WIDEBLEEDING": "Wide Bleeding", "NPC_HELLJUDGEMENT": "Hel's Judgement", "NPC_WIDESOULDRAIN": "Wide Soul Drain",
    "NPC_STONESKIN": "Stone Skin", "NPC_ANTIMAGIC": "Anti-Galdr", "NPC_DRAGONFEAR": "Dragon Fear", "NPC_WIDECURSE": "Wide Curse",
    "NPC_WIDECONFUSE": "Wide Confusion", "NPC_WIDESILENCE": "Wide Silence", "NPC_MAGICMIRROR": "Magic Mirror",
    "NPC_EARTHQUAKE": "Earthquake", "NPC_DARKSTRIKE": "Shadow Lance", "NPC_PULSESTRIKE": "Pulse Strike", "NPC_SLOWCAST": "Slow Cast",
    "GM_SANDMAN": "Deep Sleep", "ALL_WEWISH": "Yule Carol", "ALL_REVERSEORCISH": "Troll-Speech", "ALL_PARTYFLEE": "Party Flee",
    "WE_MALE": "I Will Protect You", "WE_FEMALE": "I Look Up to You", "WE_CALLPARTNER": "I Miss You", "SA_CASTCANCEL": "Cast Cancel",
    "SA_SPELLBREAKER": "Spell Breaker", "SA_LANDPROTECTOR": "Earth Ward", "SA_DISPELL": "Dispel", "SA_DELUGE": "Deluge",
    "SA_FLAMELAUNCHER": "Fire Endow", "SA_FROSTWEAPON": "Frost Endow", "SA_LIGHTNINGLOADER": "Wind Endow",
    "SA_SEISMICWEAPON": "Earth Endow", "SA_DRAGONOLOGY": "Dragon Lore", "RG_GANGSTER": "Slyness", "RG_INTIMIDATE": "Snatch",
    "HT_FLASHER": "Flasher", "AC_OWL": "Owl's Eye", "GD_ITEMEMERGENCYCALL": "Emergency Call",
}


def runeheir_skill(aegis):
    """The Runeheir skill id for a Ragnarok skill, or None."""
    return RUNEHEIR.get(str(aegis).strip('"'))
