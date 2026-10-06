using Runeheir.Field;
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

        /// <summary>A warp is loading (portals and couriers ignore further requests until the new map is up).</summary>
        public static bool InTransit { get; private set; }

        /// <summary>True when generated maps can be loaded (RH_World is in Build Settings).</summary>
        public static bool WorldSceneAvailable => Application.CanStreamedLevelBeLoaded(MapCatalog.WorldScene);

        /// <summary>The portal the player is arriving through (once): null when arriving at the save point or a saved spot.</summary>
        public static string TakeArrivalPortal()
        {
            string portal = s_arrivalPortal;
            s_arrivalPortal = null;
            InTransit = false;
            return portal;
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

            InTransit = true;
            if (player != null)
            {
                player.WriteBackToRecord();
                player.Record.MapId = map.Id;
                player.Record.HasSavedPosition = false;
                GameSession.Instance.SaveActiveCharacter();
            }

            s_arrivalPortal = arrivalPortalId;
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

        internal static void ResetForNewGame()
        {
            s_arrivalPortal = null;
            InTransit = false;
        }
    }
}
