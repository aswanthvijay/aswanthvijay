using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Monsters;
using Runeheir.Online;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Skills;
using Runeheir.Stats;
using Runeheir.World;
using Runeheir.WorldBuilding;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Field
{
    /// <summary>
    /// Ragnarok-style @commands typed into chat, for testing the stat engine and combat quickly.
    /// Enabled only in the editor and development builds; Phase 6 moves them server-side behind GM levels.
    /// </summary>
    public static class GmCommands
    {
        /// <summary>Offline: editor and development builds. Online: whatever the realm allows (its realm.json).</summary>
        public static bool Enabled => OnlineSession.Current != null ? OnlineSession.Current.AllowGmCommands : Debug.isDebugBuild;

        public static void Execute(string input, PlayerCharacter player)
        {
            if (!Enabled)
            {
                ChatLog.Error(OnlineSession.Current != null ? "This realm doesn't allow @commands." : "@commands are disabled in release builds.");
                return;
            }

            if (player == null || player.Record == null)
            {
                ChatLog.Error("No character loaded.");
                return;
            }

            string[] parts = input.TrimStart('@').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return;
            }

            string command = parts[0].ToLowerInvariant();
            string rest = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : string.Empty;

            // On someone else's realm the monsters and bosses live on their machine: only the host can change them.
            if (OnlineSession.IsRemoteClient && command is "monster" or "spawn" or "bossrespawn" or "bosstime")
            {
                ChatLog.Error("That command changes the realm's world: only the host can use it.");
                return;
            }

            var progression = player.Progression;

            switch (command)
            {
                case "help":
                case "commands":
                    ChatLog.Gm("@blvl <1-255>  @jlvl <1-120>  @job <name>  @jobs  @rebirth  @allstats <n>  @str|agi|vit|int|dex|luk <n>");
                    ChatLog.Gm("@reset  @heal  @item <id> [amount]  @items  @monster <id> [count]  @monsters  @aspd  @save  @where");
                    ChatLog.Gm("@skills  @allskills  @learn <skill> [lv]  @skillpoint <n>  @skillreset  @status <name> [seconds]  @statuses  @cleanse");
                    ChatLog.Gm("@zeny <amount>  @items [weapons|gear|cards|text]  @refine <0-20> (worn weapon)");
                    ChatLog.Gm("@maps  @warp <map> [portal]  @bosses  @bossrespawn  @bosstime <scale>  @breakweapon");
                    break;

                case "blvl":
                case "baselvl":
                case "baselevel":
                    if (TryInt(rest, out int baseLevel))
                    {
                        progression.SetBaseLevel(baseLevel);
                        player.Heal(player.MaxHp, showNumber: false);
                        player.RestoreSp(player.MaxSp, showNumber: false);
                        string capped = player.Record.BaseLevel < baseLevel ? $" (Base {RebirthRules.BaseLevelCap(player.Record)} is the cap until rebirth: @rebirth)" : string.Empty;
                        ChatLog.Gm($"Base Level set to {player.Record.BaseLevel}{capped}. Status points: {player.Record.StatPoints}.");
                    }

                    break;

                case "jlvl":
                case "joblvl":
                case "joblevel":
                    if (TryInt(rest, out int jobLevel))
                    {
                        progression.SetJobLevel(jobLevel);
                        ChatLog.Gm($"Job Level set to {player.Record.JobLevel} (max {player.Job.MaxJobLevel} for {player.Job.Name}).");
                    }

                    break;

                case "job":
                case "jobchange":
                    if (JobDatabase.TryParse(rest, out var job))
                    {
                        progression.ForceChangeJob(job);
                        ChatLog.Gm($"Job changed to {player.Job.Name}. Learn skills in the Skill window (Alt+S), or type @allskills.");
                    }
                    else
                    {
                        ChatLog.Error($"Unknown job '{rest}'. Try @jobs.");
                    }

                    break;

                case "jobs":
                    ChatLog.Gm("Jobs: " + string.Join(", ", JobDatabase.All.OrderBy(j => j.Tier).Select(j => j.Name)));
                    break;

                case "rebirth":
                case "reborn":
                    // Meets the Norns' requirements (Base 99, Job 50, the fee) and rebirths at once; needs a second job.
                    if (player.Job.Family == JobFamily.Normal && player.Job.Tier == 2 && !player.Record.Reborn)
                    {
                        progression.SetBaseLevel(RebirthRules.NormalBaseLevelCap);
                        progression.SetJobLevel(Math.Max(player.Record.JobLevel, RebirthRules.MinJobLevel));
                        player.Record.Zeny = Math.Max(player.Record.Zeny, RebirthRules.Fee);
                    }

                    if (player.TryRebirth(out string rebirthError))
                    {
                        ChatLog.Gm($"Reborn as {JobDatabase.NameFor(player.Record)}. The road leads to {JobDatabase.TranscendentOf(player.Record.RebirthPath)?.Name ?? "?"}.");
                    }
                    else
                    {
                        ChatLog.Error(rebirthError);
                    }

                    break;

                case "allstats":
                    if (TryInt(rest, out int all))
                    {
                        progression.SetAllStats(all);
                        ChatLog.Gm($"All stats set to {player.Record.Stats.Str}. Status points left: {player.Record.StatPoints}.");
                    }

                    break;

                case "str":
                case "agi":
                case "vit":
                case "int":
                case "dex":
                case "luk":
                    if (StatTypes.TryParse(command, out var stat) && TryInt(rest, out int value))
                    {
                        progression.SetStat(stat, value);
                        ChatLog.Gm($"{StatTypes.Label(stat)} set to {player.Record.Stats[stat]}. ASPD {player.Stats.Aspd:0.0}, cast x{player.Stats.CastTimeMultiplier:0.00}.");
                    }

                    break;

                case "reset":
                case "statreset":
                    progression.ResetStats();
                    ChatLog.Gm($"Stats reset. {player.Record.StatPoints} status points available.");
                    break;

                case "heal":
                    player.Heal(player.MaxHp);
                    player.RestoreSp(player.MaxSp);
                    break;

                case "item":
                    GiveItem(player, parts);
                    break;

                case "items":
                    ListItems(rest);
                    break;

                case "zeny":
                    if (TryInt(rest, out int zeny))
                    {
                        player.Record.Zeny = Math.Max(0, Math.Min(2000000000L, player.Record.Zeny + zeny));
                        player.Inventory.NotifyChanged();
                        ChatLog.Gm($"Zeny: {player.Record.Zeny:N0}.");
                    }

                    break;

                case "refine":
                    if (TryInt(rest, out int refine))
                    {
                        var weapon = player.Equipment.Get(EquipPosition.Weapon);
                        if (weapon == null)
                        {
                            ChatLog.Error("Equip a weapon first.");
                            break;
                        }

                        weapon.Refine = Mathf.Clamp(refine, 0, RefineRules.MaxRefine);
                        player.Equipment.NotifyChanged();
                        ChatLog.Gm($"{weapon.DisplayName}: ATK {player.Stats.WeaponAtk}.");
                    }

                    break;

                case "monster":
                case "spawn":
                    SpawnMonsters(player, parts);
                    break;

                case "monsters":
                    ChatLog.Gm("Monsters: " + string.Join(", ", MonsterCatalog.All.Select(m => $"{m.Id} (Lv {m.Level})")));
                    break;

                case "skills":
                    ChatLog.Gm($"Skill points: {player.Record.SkillPoints}. " + string.Join(", ", SkillCatalog.ForJob(player.Record.Job)
                        .Select(s => $"{s.Name} {player.SkillBook.GetLevel(s.Id)}/{s.MaxLevel} ({s.Id})")));
                    break;

                case "allskills":
                    player.SkillBook.LearnEverything();
                    ChatLog.Gm($"Every {player.Job.Name}-line skill learned at max level (skill points unchanged).");
                    break;

                case "learn":
                    LearnSkill(player, parts);
                    break;

                case "skillpoint":
                case "skillpoints":
                    if (TryInt(rest, out int points))
                    {
                        player.SkillBook.SetPoints(points);
                        ChatLog.Gm($"Skill points set to {player.Record.SkillPoints}.");
                    }

                    break;

                case "skillreset":
                    ChatLog.Gm($"Skills reset: {player.SkillBook.ResetAll()} points refunded ({player.Record.SkillPoints} available).");
                    break;

                case "status":
                    ApplyStatus(player, parts);
                    break;

                case "statuses":
                    ChatLog.Gm("Statuses: " + string.Join(", ", Enum.GetNames(typeof(Combat.StatusEffect)).Skip(1)));
                    break;

                case "cleanse":
                    player.Cleanse();
                    ChatLog.Gm("Statuses and debuffs removed.");
                    break;

                case "aspd":
                    PrintAspd(player);
                    break;

                case "save":
                    if (GameSession.Instance.IsTemporaryCharacter)
                    {
                        ChatLog.Gm("This is a temporary character (field played directly) — start from RH_Login to save.");
                        break;
                    }

                    ReportSave(player.SaveNow());
                    break;

                case "where":
                    var p = player.Position;
                    ChatLog.Gm($"{FieldContext.MapName} [{FieldContext.MapId}] ({p.x:0.0}, {p.z:0.0}) · save map: {player.Record.SaveMapId}");
                    break;

                case "maps":
                    ChatLog.Gm("Maps: " + string.Join(", ", MapCatalog.All.Select(m => $"{m.Id} ({m.LevelLabel})")));
                    break;

                case "warp":
                case "go":
                    Warp(player, parts);
                    break;

                case "bosses":
                    ListBosses();
                    break;

                case "bossrespawn":
                    WorldState.Bosses.ResetAll();
                    ChatLog.Gm("Every boss timer cleared: dead bosses return within a second on their maps.");
                    break;

                case "bosstime":
                    if (float.TryParse(rest, NumberStyles.Float, CultureInfo.InvariantCulture, out float scale) && scale >= 0f)
                    {
                        WorldState.Bosses.RespawnScale = scale;
                        ChatLog.Gm($"Boss respawn times x{scale:0.###} (1 = GDD: MVPs 1 h, mini-bosses 2 h).");
                    }
                    else
                    {
                        ChatLog.Error("Usage: @bosstime <scale>, e.g. @bosstime 0.01 for 36-second MVPs.");
                    }

                    break;

                case "breakweapon":
                    if (!player.TryBreakWeapon(100f))
                    {
                        ChatLog.Error("Nothing to break (no weapon, already broken, or unbreakable).");
                    }

                    break;

                default:
                    ChatLog.Error($"Unknown command @{command}. Type @help.");
                    break;
            }
        }

        private static void Warp(PlayerCharacter player, string[] parts)
        {
            var map = parts.Length > 1 ? MapCatalog.Get(parts[1]) ?? MapCatalog.All.FirstOrDefault(m => m.Name.StartsWith(parts[1], StringComparison.OrdinalIgnoreCase)) : null;
            if (map == null)
            {
                ChatLog.Error("Usage: @warp <map id> [portal id]. Maps: @maps.");
                return;
            }

            string portal = parts.Length > 2 ? parts[2] : null;
            var target = portal != null ? map.Portal(portal) : null;
            if (portal != null && target == null)
            {
                ChatLog.Error($"{map.Name} has no portal '{portal}': {string.Join(", ", map.Portals.Select(x => x.Id))}.");
                return;
            }

            WorldTravel.Warp(player, map.Id, target?.Id, $"Warping to {map.Name}...");
        }

        private static void ListBosses()
        {
            double now = WorldState.Now;
            foreach (var map in MapCatalog.All)
            {
                foreach (var boss in map.Bosses)
                {
                    var definition = MonsterCatalog.Get(boss.MonsterId);
                    var status = WorldState.Bosses.Status(map.Id, boss.MonsterId);
                    string state = status == null || status.Alive || now >= status.RespawnAt
                        ? "up"
                        : $"returns in {BossTracker.FormatDuration(status.RespawnAt - now)} (slain by {status.KilledBy ?? "someone"})";
                    ChatLog.Gm($"{definition?.Name ?? boss.MonsterId} · {map.Name}: {state}");
                }
            }
        }

        private static void GiveItem(PlayerCharacter player, string[] parts)
        {
            if (parts.Length < 2)
            {
                ChatLog.Error("Usage: @item <id> [amount]");
                return;
            }

            var item = ItemCatalog.Get(parts[1]);
            if (item == null)
            {
                ChatLog.Error($"Unknown item '{parts[1]}'. Try @items.");
                return;
            }

            int amount = 1;
            if (parts.Length > 2)
            {
                if (!TryInt(parts[2], out int parsed))
                {
                    return;
                }

                amount = Mathf.Clamp(parsed, 1, Inventory.MaxStack);
            }

            int added = player.Inventory.Add(item.Id, amount);
            ChatLog.Loot($"You got {item.Name} ({added}).");
        }

        /// <summary>@items alone prints the categories; @items weapons|gear|cards|consumables|etc or any text filters ids and names.</summary>
        private static void ListItems(string filter)
        {
            filter = filter.Trim().ToLowerInvariant();
            if (filter.Length == 0)
            {
                ChatLog.Gm($"{ItemCatalog.All.Count()} items. Try @items weapons, @items gear, @items cards, @items consumables, @items etc, or @items <text>.");
                return;
            }

            System.Func<ItemDefinition, bool> match;
            switch (filter)
            {
                case "weapon":
                case "weapons": match = i => i.IsWeapon; break;
                case "gear":
                case "armor":
                case "armour": match = i => i.IsEquipment && !i.IsWeapon; break;
                case "card":
                case "cards": match = i => i.IsCard; break;
                case "consumable":
                case "consumables":
                case "potions": match = i => i.Kind == ItemKind.Consumable; break;
                case "etc": match = i => !i.IsEquipment && !i.IsCard && i.Kind != ItemKind.Consumable; break;
                default: match = i => i.Id.Contains(filter) || i.Name.ToLowerInvariant().Contains(filter); break;
            }

            var found = ItemCatalog.All.Where(match).Select(i => i.Id).ToList();
            ChatLog.Gm(found.Count == 0 ? "No items match." : $"{found.Count}: " + string.Join(", ", found));
        }

        private static void SpawnMonsters(PlayerCharacter player, string[] parts)
        {
            var definition = parts.Length > 1 ? MonsterCatalog.Get(parts[1]) : null;
            if (definition == null)
            {
                ChatLog.Error("Usage: @monster <id> [count]. Try @monsters.");
                return;
            }

            int count = 1;
            if (parts.Length > 2)
            {
                if (!TryInt(parts[2], out int parsed))
                {
                    return;
                }

                count = Mathf.Clamp(parsed, 1, 30);
            }

            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 4f + Vector2.one * 1.5f;
                Vector3 point = player.Position + new Vector3(offset.x, 0f, offset.y);
                if (NavMesh.SamplePosition(point, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    EntityFactory.CreateMonster(definition, hit.position, UnityEngine.Random.Range(0f, 360f));
                    spawned++;
                }
            }

            ChatLog.Gm($"Spawned {spawned}x {definition.Name} (Lv {definition.Level}).");
        }

        private static void LearnSkill(PlayerCharacter player, string[] parts)
        {
            var skill = parts.Length > 1 ? SkillCatalog.All.FirstOrDefault(s => !s.Hidden && (s.Id == parts[1].ToLowerInvariant()
                || string.Equals(s.Name.Replace(" ", string.Empty).Replace("'", string.Empty), parts[1].Replace("'", string.Empty), StringComparison.OrdinalIgnoreCase))) : null;
            if (skill == null)
            {
                ChatLog.Error("Usage: @learn <skill id or name without spaces> [level]. See @skills.");
                return;
            }

            int level = skill.MaxLevel;
            if (parts.Length > 2 && !TryInt(parts[2], out level))
            {
                return;
            }

            player.SkillBook.SetLevel(skill.Id, level);
            ChatLog.Gm($"{skill.Name} set to Lv {player.SkillBook.GetLevel(skill.Id)}" +
                       (SkillCatalog.CanUse(player.Record.Job, skill.Id) ? "." : $" (usable once you are in the {JobDatabase.Get(skill.Job).Name} line)."));
        }

        private static void ApplyStatus(PlayerCharacter player, string[] parts)
        {
            // Names only: Enum.TryParse would also accept numbers ("3") and lists ("stun,sleep").
            Combat.StatusEffect status = Combat.StatusEffect.None;
            bool named = parts.Length >= 2 && parts[1].All(char.IsLetter)
                         && Enum.TryParse(parts[1], true, out status) && Combat.StatusRules.Get(status) != null;
            if (!named)
            {
                ChatLog.Error("Usage: @status <name> [seconds]. See @statuses.");
                return;
            }

            if (player.IsDead)
            {
                ChatLog.Error("You are dead.");
                return;
            }

            float seconds = 5f;
            if (parts.Length > 2 && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                seconds = Mathf.Clamp(parsed, 0.5f, 120f);
            }

            ChatLog.Gm(player.ApplyStatus(status, seconds, ignoreImmunity: true)
                ? $"{Combat.StatusRules.Get(status).Name} for {seconds:0.#}s."
                : "Blocked (Sowilo's ward makes you immune).");
        }

        private static async void ReportSave(System.Threading.Tasks.Task<Accounts.OpResult> save)
        {
            // GameSession already reports failures in chat; only confirm a save that really landed.
            var result = await save;
            if (result.Success)
            {
                ChatLog.Gm("Character saved.");
            }
        }

        private static void PrintAspd(PlayerCharacter player)
        {
            var s = player.Stats;
            var weapon = player.Weapon;
            var builder = new StringBuilder();
            builder.Append($"ASPD {s.Aspd:0.0} [{weapon.Type} base {Combat.WeaponRules.BaseAspd(weapon.Type):0}, AGI {s.Total.Agi}, DEX {s.Total.Dex}]");
            builder.Append($" → anim x{s.AttackPlayRate:0.00}, swing {s.SwingDuration:0.000}s, interval {s.AttackInterval:0.000}s ({s.AttacksPerSecond:0.00} hits/s)");
            ChatLog.Gm(builder.ToString());
        }

        private static bool TryInt(string text, out int value)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            ChatLog.Error("Expected a number.");
            return false;
        }
    }
}
