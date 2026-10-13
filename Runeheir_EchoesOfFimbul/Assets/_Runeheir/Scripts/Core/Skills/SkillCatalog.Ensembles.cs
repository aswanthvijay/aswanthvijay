using Runeheir.Combat;
using Runeheir.Jobs;

namespace Runeheir.Skills
{
    /// <summary>
    /// Ragnarok's ensembles (Bard + Dancer duets) as the Skald's and Seidkona's duets: both performers learn the duet,
    /// stand side by side in one party, and play it together (<see cref="SkillSpecial.Ensemble"/>). Each duet exists once
    /// per side; the two halves share an <see cref="SkillDefinition.EnsembleKey"/>.
    /// </summary>
    public static partial class SkillCatalog
    {
        private const float EnsembleRadius = 5f;
        private const float EnsembleSeconds = 60f;

        private static void RegisterEnsembles()
        {
            Duet("norns_lullaby", "Lullaby of the Norns", "Ragnarok's Lullaby: the Norns hum the world to sleep. Foes in the circle may fall asleep.",
                "LUL", "#7FB3D5", status: StatusEffect.Sleep, statusChance: 6f, statusPerLevel: 3f);
            Duet("mead_of_plenty", "Mead of Plenty", "Ragnarok's Mr. Kim A Rich Man: a toast from Ægir's hall. Allies in the circle earn +20% EXP, +10% per level.",
                "MKR", "#F7DC6F", buffId: SkillBuffs.MeadOfPlenty);
            Duet("ginnungagap", "Ginnungagap", "Ragnarok's Eternal Chaos: the yawning void before creation. Foes in the circle lose their DEF.",
                "ETC", "#4A235A", debuffId: SkillBuffs.Ginnungagap);
            Duet("shield_wall_drums", "Drums of the Shield-Wall", "Ragnarok's A Drum on the Battlefield: the war-drums of the line. Allies gain ATK and DEF.",
                "DRM", "#A04000", buffId: SkillBuffs.ShieldWallDrums);
            Duet("ring_of_andvari", "Ring of Andvari", "Ragnarok's The Ring of Nibelungen: the dwarf's cursed gold. Allies gain +75 ATK, +25 per level.",
                "RON", "#D4AC0D", buffId: SkillBuffs.RingOfAndvari);
            Duet("lokis_veil", "Loki's Veil", "Ragnarok's Loki's Veil: the trickster's mist. Foes in the circle are silenced and can't use skills.",
                "LKV", "#5D6D7E", status: StatusEffect.Silence, statusChance: 40f, statusPerLevel: 10f);
            Duet("mimirs_well", "Mímir's Well", "Ragnarok's Into the Abyss: a sip from the well of wisdom. Allies' skills cost 30% less SP, 5% more per level.",
                "ITA", "#2E86C1", buffId: SkillBuffs.MimirsWell);
            Duet("sigurds_hide", "Sigurd's Hide", "Ragnarok's Invulnerable Siegfried: bathed in dragon's blood. Allies take 15% less damage, 3% less per level.",
                "INV", "#1E8449", buffId: SkillBuffs.SigurdsHide);
        }

        /// <summary>One duet, registered for both sides: the Skald's half and the Seidkona's half.</summary>
        private static void Duet(string key, string name, string description, string icon, string color, string buffId = null, string debuffId = null,
            StatusEffect status = StatusEffect.None, float statusChance = 0f, float statusPerLevel = 0f)
        {
            foreach (var (job, lessons, weapons) in new[] { (JobId.Skald, "bragis_lessons", WeaponMask.Instrument), (JobId.Seidkona, "seidr_lessons", WeaponMask.Whip) })
            {
                Register(new SkillDefinition
                {
                    Id = (job == JobId.Skald ? "skald_" : "seidkona_") + key, Name = name, Job = job, MaxLevel = 5, Requires = Req(lessons, 5),
                    EnsembleKey = key,
                    Description = description + " A duet: needs a " + (job == JobId.Skald ? "Seidkona or Völva" : "Skald or Thul")
                                  + " of your party at your side who knows it too. Neither of you can move while you play.",
                    IconLabel = icon, IconColorHex = color, Weapons = weapons,
                    Target = SkillTarget.Self, Special = SkillSpecial.Ensemble, Motion = SkillMotion.Cast,
                    Radius = EnsembleRadius, ZoneDuration = EnsembleSeconds, ZoneTick = 1f,
                    BuffId = buffId, BuffDuration = SkillBuffs.PerformanceLinger,
                    DebuffId = debuffId, DebuffDuration = SkillBuffs.PerformanceLinger,
                    Status = status, StatusChance = L(statusChance, statusPerLevel), StatusDuration = 6f,
                    SpCost = L(40f, 10f), AfterCastDelay = 2f,
                });
            }
        }
    }

    /// <summary>Who can play a duet with whom.</summary>
    public static class EnsembleRules
    {
        /// <summary>How close the two performers must stand (meters, center to center).</summary>
        public const float PartnerRange = 3f;

        public static bool IsSkaldSide(JobId job) => job == JobId.Skald || job == JobId.Thul;

        public static bool IsSeidkonaSide(JobId job) => job == JobId.Seidkona || job == JobId.Volva;

        /// <summary>A Skald (or Thul) and a Seidkona (or Völva), in either order.</summary>
        public static bool ArePartners(JobId a, JobId b)
        {
            return (IsSkaldSide(a) && IsSeidkonaSide(b)) || (IsSeidkonaSide(a) && IsSkaldSide(b));
        }

        /// <summary>The other side's half of a duet, by its <see cref="SkillDefinition.EnsembleKey"/>.</summary>
        public static SkillDefinition HalfFor(JobId partnerJob, string ensembleKey)
        {
            string prefix = IsSkaldSide(partnerJob) ? "skald_" : IsSeidkonaSide(partnerJob) ? "seidkona_" : null;
            return prefix == null ? null : SkillCatalog.Get(prefix + ensembleKey);
        }
    }
}
