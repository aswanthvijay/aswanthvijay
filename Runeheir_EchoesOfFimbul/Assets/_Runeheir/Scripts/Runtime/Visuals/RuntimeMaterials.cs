using System.Collections.Generic;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// Colored materials for placeholder art, created at runtime and cached per color.
    /// The lit shader is taken from the render pipeline's default material (URP Lit in a URP project),
    /// so it is always included in builds.
    /// </summary>
    public static class RuntimeMaterials
    {
        private static readonly Dictionary<Color32, Material> LitCache = new Dictionary<Color32, Material>();
        private static Shader s_litShader;
        private static Material s_unlit;

        public static Material Lit(Color color)
        {
            Color32 key = color;
            if (LitCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var material = new Material(LitShader) { name = $"RH_Lit_{ColorUtility.ToHtmlStringRGBA(color)}", color = color };
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.15f);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.15f);
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

        private static Shader LitShader
        {
            get
            {
                if (s_litShader == null)
                {
                    var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    var renderer = probe.GetComponent<Renderer>();
                    s_litShader = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.shader : null;
                    Object.DestroyImmediate(probe);

                    if (s_litShader == null)
                    {
                        s_litShader = FindShader("Universal Render Pipeline/Lit", "Standard");
                    }
                }

                return s_litShader;
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
