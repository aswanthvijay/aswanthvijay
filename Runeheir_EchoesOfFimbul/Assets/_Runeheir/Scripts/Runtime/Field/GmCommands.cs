using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Monsters;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Skills;
using Runeheir.Stats;
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
        public static bool Enabled => Debug.isDebugBuild;

        public static void Execute(string input, PlayerCharacter player)
        {
            if (!Enabled)
            {
                ChatLog.Error("@commands are disabled in release builds.");
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
            var progression = player.Progression;

            switch (command)
            {
                case "help":
                case "commands":
                    ChatLog.Gm("@blvl <1-255>  @jlvl <1-120>  @job <name>  @jobs  @allstats <n>  @str|agi|vit|int|dex|luk <n>");
                    ChatLog.Gm("@reset  @heal  @item <id> [amount]  @items  @monster <id> [count]  @monsters  @aspd  @skills  @save  @where");
                    break;

                case "blvl":
                case "baselvl":
                case "baselevel":
                    if (TryInt(rest, out int baseLevel))
                    {
                        progression.SetBaseLevel(baseLevel);
                        player.Heal(player.MaxHp, showNumber: false);
                        player.RestoreSp(player.MaxSp, showNumber: false);
                        ChatLog.Gm($"Base Level set to {player.Record.BaseLevel}. Status points: {player.Record.StatPoints}.");
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
                        ChatLog.Gm($"Job changed to {player.Job.Name}. Open the skill window (Alt+S) to drag new skills onto F1–F10.");
                    }
                    else
                    {
                        ChatLog.Error($"Unknown job '{rest}'. Try @jobs.");
                    }

                    break;

                case "jobs":
                    ChatLog.Gm("Jobs: " + string.Join(", ", JobDatabase.All.OrderBy(j => j.Tier).Select(j => j.Name)));
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
                    ChatLog.Gm("Items: " + string.Join(", ", ItemCatalog.All.Select(i => i.Id)));
                    break;

                case "monster":
                case "spawn":
                    SpawnMonsters(player, parts);
                    break;

                case "monsters":
                    ChatLog.Gm("Monsters: " + string.Join(", ", MonsterCatalog.All.Select(m => $"{m.Id} (Lv {m.Level})")));
                    break;

                case "skills":
                    ChatLog.Gm("Skills: " + string.Join(", ", SkillCatalog.ForJob(player.Record.Job).Select(s => s.Name)));
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
                    ChatLog.Gm($"{FieldContext.MapName} ({p.x:0.0}, {p.z:0.0})");
                    break;

                default:
                    ChatLog.Error($"Unknown command @{command}. Type @help.");
                    break;
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
