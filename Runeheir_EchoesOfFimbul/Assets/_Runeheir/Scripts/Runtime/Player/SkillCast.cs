using Runeheir.Combat;
using Runeheir.Skills;
using UnityEngine;

namespace Runeheir.Player
{
    /// <summary>One use of a skill: who cast it, at what level, on what. Passed through the effect coroutines and ground zones.</summary>
    public sealed class SkillCast
    {
        public PlayerCharacter Caster;
        public SkillDefinition Skill;
        public int Level = 1;

        /// <summary>The clicked entity (Enemy/Friend skills), the caster for Self skills, or null for ground skills.</summary>
        public CombatEntity Target;

        public Vector3 Point;

        /// <summary>Rune Amplify's bonus, captured (and the buff consumed) when the spell went off.</summary>
        public float BonusMagicPercent;

        /// <summary>Shadow Veil was broken by this skill: skills that can crit, crit.</summary>
        public bool Ambush;

        /// <summary>A free auto-cast from a proc (Storm Fists, Keen Edge, Auto Rune).</summary>
        public bool IsProc;

        /// <summary>Hit count decided at cast time (Spirit Barrage: one per sphere spent). 0 = the skill's Hits.</summary>
        public int HitsOverride;

        public int Hits => HitsOverride > 0 ? HitsOverride : Mathf.Max(1, Skill.Hits.AtInt(Level));
    }
}
