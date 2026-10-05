using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Runeheir.Visuals
{
    /// <summary>
    /// Colored materials for placeholder art, created at runtime and cached per color + outline.
    /// Under URP they use the cel-shaded <c>Runeheir/Toon</c> shader (GDD Phase 2 Step 5: anime toon
    /// shading with ink outlines; it lives in a Resources folder so it ships in every build). Without
    /// it they fall back to the active pipeline's default lit shader.
    /// </summary>
    public static class RuntimeMaterials
    {
        public const string ToonShaderName = "Runeheir/Toon";

        /// <summary>Ink outline width (pixels at 1080p) for characters and monsters.</summary>
        public const float CharacterOutline = 2f;

        private static readonly Dictionary<(Color32, int), Material> LitCache = new Dictionary<(Color32, int), Material>();
        private static Shader s_litShader;
        private static Shader s_toonShader;
        private static bool s_toonResolved;
        private static Material s_unlit;

        /// <summary>True when the toon shader is available and the active pipeline is URP.</summary>
        public static bool ToonAvailable => ToonShader != null;

        public static Material Lit(Color color)
        {
            return Lit(color, CharacterOutline);
        }

        /// <param name="outlineWidth">Ink outline in pixels at 1080p (0 = none). Ignored without the toon shader.</param>
        public static Material Lit(Color color, float outlineWidth)
        {
            var key = ((Color32)color, Mathf.RoundToInt(outlineWidth * 100f));
            if (LitCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var toon = ToonShader;
            var material = new Material(toon != null ? toon : LitShader)
            {
                name = $"RH_{(toon != null ? "Toon" : "Lit")}_{ColorUtility.ToHtmlStringRGBA(color)}",
                color = color,
            };

            if (toon != null)
            {
                material.SetFloat("_OutlineWidth", outlineWidth);
            }
            else
            {
                SetIfPresent(material, "_Smoothness", 0.15f);
                SetIfPresent(material, "_Glossiness", 0.15f);
            }

            LitCache[key] = material;
            return material;
        }

        /// <summary>Unlit, vertex-colored material for LineRenderer rings and markers.</summary>
        public static Material Unlit
        {
            get
            {
                if (s_unlit == null)
                {
                    var shader = FindShader("Sprites/Default", "Universal Render Pipeline/Unlit");
                    s_unlit = new Material(shader != null ? shader : LitShader) { name = "RH_Unlit" };
                }

                return s_unlit;
            }
        }

        /// <summary>"#RRGGBB" / "#RRGGBBAA" to Color.</summary>
        public static Color Hex(string hex, Color fallback)
        {
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
        }

        public static Color Hex(string hex)
        {
            return Hex(hex, Color.gray);
        }

        private static Shader ToonShader
        {
            get
            {
                if (!s_toonResolved)
                {
                    s_toonResolved = true;
                    var pipeline = GraphicsSettings.currentRenderPipeline;
                    bool urp = pipeline != null && pipeline.GetType().Name.Contains("Universal");
                    var shader = urp ? Shader.Find(ToonShaderName) : null;
                    s_toonShader = shader != null && shader.isSupported ? shader : null;
                }

                return s_toonShader;
            }
        }

        private static Shader LitShader
        {
            get
            {
                if (s_litShader == null)
                {
                    var pipeline = GraphicsSettings.currentRenderPipeline;
                    if (pipeline != null)
                    {
                        // Ask the pipeline, never a probe primitive: outside the Editor URP has no default
                        // material, so a primitive can come back with built-in Standard, which URP cannot draw.
                        s_litShader = pipeline.defaultShader;
                        if (s_litShader == null || !s_litShader.isSupported)
                        {
                            s_litShader = FindShader("Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Universal Render Pipeline/Unlit");
                        }
                    }
                    else
                    {
                        var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        var renderer = probe.GetComponent<Renderer>();
                        s_litShader = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.shader : null;
                        Object.DestroyImmediate(probe);
                    }

                    if (s_litShader == null)
                    {
                        s_litShader = FindShader("Standard", "Sprites/Default");
                    }
                }

                return s_litShader;
            }
        }

        private static void SetIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static Shader FindShader(params string[] names)
        {
            foreach (string shaderName in names)
            {
                var shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    return shader;
                }
            }

            return null;
        }
    }
}
