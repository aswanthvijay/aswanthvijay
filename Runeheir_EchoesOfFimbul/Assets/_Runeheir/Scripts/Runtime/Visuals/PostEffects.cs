using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Runeheir.Visuals
{
    /// <summary>
    /// Art Pass 3: the game's look after rendering, built in code (the project's own volume and pipeline assets are left
    /// alone). A neutral tonemap keeps the toon colors honest, a soft bloom lets runes, torches and lava glow, the
    /// grading adds a little contrast and saturation, the highlights take the map's sunlight color, and a light vignette
    /// draws the eye to the middle of the screen. One global volume, retuned for each map's theme.
    /// </summary>
    public static class PostEffects
    {
        private static Volume s_volume;
        private static VolumeProfile s_profile;
        private static Bloom s_bloom;
        private static ColorAdjustments s_grade;
        private static Vignette s_vignette;
        private static ShadowsMidtonesHighlights s_tones;

        /// <param name="sunColor">The map's sunlight (warm fields, cold caverns), echoed in the highlights.</param>
        /// <param name="glow">How much bright things bloom (more in dark dungeons, where torches and runes carry the light).</param>
        public static void Apply(Camera camera, Color sunColor, float glow)
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            {
                return;
            }

            if (camera != null)
            {
                var data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            }

            EnsureVolume();
            s_bloom.intensity.Override(Mathf.Lerp(0.25f, 0.8f, Mathf.Clamp01(glow)));
            var highlight = Color.Lerp(Color.white, sunColor, 0.35f);
            s_tones.highlights.Override(new Vector4(highlight.r, highlight.g, highlight.b, 0f));
        }

        private static void EnsureVolume()
        {
            if (s_volume != null)
            {
                return;
            }

            s_profile = ScriptableObject.CreateInstance<VolumeProfile>();
            s_profile.name = "RH_PostEffects";

            var tonemap = s_profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.Neutral);

            s_bloom = s_profile.Add<Bloom>(true);
            s_bloom.threshold.Override(0.95f);
            s_bloom.scatter.Override(0.62f);
            s_bloom.intensity.Override(0.35f);

            s_grade = s_profile.Add<ColorAdjustments>(true);
            s_grade.postExposure.Override(0f);
            s_grade.contrast.Override(10f);
            s_grade.saturation.Override(4f);

            s_tones = s_profile.Add<ShadowsMidtonesHighlights>(true);
            s_tones.shadows.Override(new Vector4(0.92f, 0.96f, 1.08f, 0f));

            s_vignette = s_profile.Add<Vignette>(true);
            s_vignette.intensity.Override(0.24f);
            s_vignette.smoothness.Override(0.45f);
            s_vignette.color.Override(new Color(0.08f, 0.06f, 0.1f));

            var go = new GameObject("RH Post Effects");
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(go);
            }

            s_volume = go.AddComponent<Volume>();
            s_volume.isGlobal = true;
            s_volume.priority = 10f;
            s_volume.sharedProfile = s_profile;
        }
    }
}
