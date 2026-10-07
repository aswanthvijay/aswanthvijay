using Runeheir.Items;

namespace Runeheir.Combat
{
    /// <summary>
    /// A player character as monsters see it: your own <see cref="Player.PlayerCharacter"/>, or (online) a
    /// <see cref="RemotePlayer"/> that mirrors someone else's. Monster kills pay out through these methods, so the realm
    /// can pass a remote player's EXP and loot on to their own game.
    /// </summary>
    public abstract class PlayerEntity : CombatEntity
    {
        public override Faction Faction => Faction.Player;

        /// <summary>EXP from a kill (rates already applied).</summary>
        public abstract void GrantExperience(long baseExp, long jobExp);

        /// <summary>A monster skill rolled to smash the worn weapon. True when it broke.</summary>
        public abstract bool TryBreakWeapon(float chancePercent);

        /// <summary>A drop from a kill (autoloot) or an MVP reward.</summary>
        public abstract void ReceiveLoot(ItemDefinition item, float baseChance, bool mvpReward);

        /// <summary>Most Valuable Player of a boss fight: the cheer and the bonus Base EXP.</summary>
        public abstract void ReceiveMvp(string monsterName, long bonusExp);
    }
}
