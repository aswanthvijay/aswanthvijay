using UnityEngine;

namespace Runeheir.Field
{
    /// <summary>
    /// Project settings that survive scene rebuilds: the generated scenes (Build Prototype Scenes, every build, CI) are
    /// recreated from code, so your player model, its Animator Controller and the server rates live here instead.
    /// Create it with Runeheir ▸ Setup ▸ Create Settings Asset; it is loaded from Resources/RuneheirSettings.
    /// </summary>
    [CreateAssetMenu(menuName = "Runeheir/Settings", fileName = "RuneheirSettings")]
    public sealed class RuneheirSettings : ScriptableObject
    {
        public const string ResourcePath = "RuneheirSettings";

        [Header("Player model")]
        [Tooltip("Character model (FBX/prefab with an Animator). Empty = the procedural placeholder avatar.")]
        public GameObject playerVisualPrefab;

        [Tooltip("Assigned to the model's Animator, e.g. Generated/Animation/RuneheirCharacter.controller. Empty = keep the prefab's own.")]
        public RuntimeAnimatorController playerAnimatorController;

        [Header("Server rates (override FieldBootstrap)")]
        public bool overrideRates;

        [Min(0f)] public float baseExpRate = 50f;
        [Min(0f)] public float jobExpRate = 50f;
        [Min(0f)] public float dropRate = 5f;
        [Min(0f)] public float cardDropRate = 1f;

        private static RuneheirSettings s_cached;
        private static bool s_loaded;

        /// <summary>The settings asset, or null when the project has none (defaults apply).</summary>
        public static RuneheirSettings Instance
        {
            get
            {
                if (!s_loaded || (s_cached == null && !ReferenceEquals(s_cached, null)))
                {
                    s_cached = Resources.Load<RuneheirSettings>(ResourcePath);
                    s_loaded = true;
                }

                return s_cached;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            s_cached = null;
            s_loaded = false;
        }
    }
}
