using Runeheir.Field;
using Runeheir.Online;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.World;
using UnityEngine;

namespace Runeheir.WorldBuilding
{
    /// <summary>
    /// World state that outlives a map change: the boss timers (Phase 6 moves them to the map server) and a steady clock
    /// for them. Reset when the game starts, so an Editor without domain reload doesn't carry timers between Play sessions.
    /// </summary>
    public static class WorldState
    {
        public static BossTracker Bosses { get; private set; } = new BossTracker();

        /// <summary>Seconds since the game started; keeps counting across map loads.</summary>
        public static double Now => Time.realtimeSinceStartupAsDouble;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Bosses = new BossTracker();
            WorldTravel.ResetForNewGame();
        }
    }

    /// <summary>
    /// Moving between maps: every map is generated into the one RH_World scene, so a warp saves the character, notes the
    /// portal to arrive at, and reloads the scene for the new map.
    /// </summary>
    public static class WorldTravel
    {
        private static string s_arrivalPortal;
        private static bool s_arriving;

        // Online warps wait for the realm's answer; a refusal puts the record back as it was.
        private static PlayerCharacter s_warpingPlayer;
        private static string s_previousMapId;
        private static bool s_previousHadPosition;

        /// <summary>A warp is loading (portals and couriers ignore further requests until the new map is up).</summary>
        public static bool InTransit { get; private set; }

        /// <summary>True when generated maps can be loaded (RH_World is in Build Settings).</summary>
        public static bool WorldSceneAvailable => Application.CanStreamedLevelBeLoaded(MapCatalog.WorldScene);

        /// <summary>
        /// Called once by the map that just loaded: true when this load is the end of a warp, with the portal the player
        /// arrives through (null = the save point).
        /// </summary>
        public static bool TakeArrival(out string portal)
        {
            s_warpingPlayer = null;
            bool arriving = s_arriving;
            portal = s_arrivalPortal;
            s_arrivalPortal = null;
            s_arriving = false;
            InTransit = false;
            return arriving;
        }

        /// <summary>
        /// Leaves for <paramref name="mapId"/>: through <paramref name="arrivalPortalId"/> when given, otherwise to that map's
        /// save point. Returns false when the map is unknown or the world scene can't be loaded.
        /// </summary>
        public static bool Warp(PlayerCharacter player, string mapId, string arrivalPortalId, string message = null)
        {
            var map = MapCatalog.Get(mapId);
            if (map == null || InTransit)
            {
                return false;
            }

            if (!WorldSceneAvailable)
            {
                ChatLog.Error($"Can't travel from this scene: add {MapCatalog.WorldScene} with Runeheir ▸ Setup ▸ Build Prototype Scenes.");
                return false;
            }

            // Write the destination into the record, then freeze it: until the new map loads, WriteBackToRecord leaves
            // the map and position alone, so an autosave or level-up save this frame can't undo the warp. The new map
            // saves the character once it's up (after a Raven Feather has been used up, a courier fee paid...).
            if (player != null)
            {
                player.WriteBackToRecord();
                s_warpingPlayer = player;
                s_previousMapId = player.Record.MapId;
                s_previousHadPosition = player.Record.HasSavedPosition;
                player.Record.MapId = map.Id;
                player.Record.HasSavedPosition = false;
            }

            InTransit = true;
            s_arriving = true;
            s_arrivalPortal = arrivalPortalId;

            // Online: the realm moves the character between its maps first, then the new map loads here.
            var online = OnlineSession.Current;
            if (online != null)
            {
                if (!online.Warp(player, map.Id, arrivalPortalId, message))
                {
                    AbortWarp(null);
                    return false;
                }

                return true;
            }

            if (!string.IsNullOrEmpty(message))
            {
                ChatLog.System(message);
            }

            SceneFlow.Load(map.SceneName);
            return true;
        }

        /// <summary>Raven Feather and revival: to the save point of the map you last saved on, crossing maps when needed.</summary>
        public static void ToSavePoint(PlayerCharacter player, string message)
        {
            string saveMap = player.Record.SaveMapId;
            // Hand-built scenes (no generated layout) can't be left: revive on their own save point.
            if (string.IsNullOrEmpty(saveMap) || saveMap == FieldContext.MapId || MapCatalog.Get(saveMap) == null || FieldContext.Layout == null
                || !WorldSceneAvailable)
            {
                player.TeleportTo(FieldContext.SavePoint, message);
                return;
            }

            Warp(player, saveMap, null, message);
        }

        /// <summary>The realm refused an online warp: stay here, as if nothing happened.</summary>
        public static void AbortWarp(string reason)
        {
            if (s_warpingPlayer != null && s_warpingPlayer.Record != null)
            {
                s_warpingPlayer.Record.MapId = s_previousMapId ?? s_warpingPlayer.Record.MapId;
                s_warpingPlayer.Record.HasSavedPosition = s_previousHadPosition;
            }

            s_warpingPlayer = null;
            s_arrivalPortal = null;
            s_arriving = false;
            InTransit = false;
            if (!string.IsNullOrEmpty(reason))
            {
                ChatLog.Error(reason);
            }
        }

        internal static void ResetForNewGame()
        {
            s_warpingPlayer = null;
            s_arrivalPortal = null;
            s_arriving = false;
            InTransit = false;
        }
    }
}
