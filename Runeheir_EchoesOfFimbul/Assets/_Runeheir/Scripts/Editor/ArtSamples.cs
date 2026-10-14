using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Runeheir.Characters;
using Runeheir.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Runeheir.EditorTools
{
    /// <summary>
    /// Art direction samples: one forest camp built from Poly Haven's CC0 scans (Assets/_Runeheir/ThirdParty/PolyHaven,
    /// fetched with Tools/ArtSamples/fetch_polyhaven.py and kept out of git) and rendered from the game camera two ways:
    /// <list type="bullet">
    /// <item><b>Modern</b>: physically based (URP Lit with normal maps, a height-blended terrain), lit by an HDRI sky,
    /// ACES tonemapping and grading, SSAO and soft shadows (the Lost Ark / Diablo IV direction).</item>
    /// <item><b>Anime</b>: the same world cel-shaded and painted (Runeheir/ToonFoliage, Runeheir/ToonSplat), brighter and
    /// more saturated, with the toon-shaded, ink-outlined hero (the Genshin direction).</item>
    /// </list>
    /// Runeheir ▸ Art ▸ Render Style Samples, or <c>-executeMethod Runeheir.EditorTools.ArtSamples.RenderFromCommandLine</c>.
    /// Writes Builds/Previews/sample_modern*.png and sample_anime*.png.
    /// </summary>
    public static class ArtSamples
    {
        public enum Style
        {
            Modern,
            Anime,
        }

        private enum Kind
        {
            Solid,
            Foliage,
            Grass,
            Glass,
        }

        public const string Root = "Assets/_Runeheir/ThirdParty/PolyHaven";
        private const string Generated = Root + "/Generated";
        private const string SkyName = "kloofendal_48d_partly_cloudy_puresky";
        private const string Grass = "grass_medium_01";
        private const float TerrainSize = 96f;
        private const float TerrainHeight = 12f;
        private const float BaseHeight = 3f;

        private static readonly string[] GroundLayers = { "leafy_grass", "forest_ground_04", "grass_path_2", "aerial_rocks_02" };
        private static readonly float[] GroundTiles = { 3f, 3.5f, 3f, 4f };

        // The game camera looks along yaw 45°: positions below are (across the screen, into the screen) in meters.
        private static readonly Vector3 ScreenRight = new Vector3(0.7071f, 0f, -0.7071f);
        private static readonly Vector3 ScreenUp = new Vector3(0.7071f, 0f, 0.7071f);

        private static readonly Vector2 FirePit = new Vector2(-2.4f, 1.8f);
        private static readonly Vector2 HeroSpot = new Vector2(0.7f, -0.2f);
        private static readonly Vector2 FoeA = new Vector2(2.9f, 1.9f);
        private static readonly Vector2 FoeB = new Vector2(3.9f, -0.5f);
        private static readonly Vector2[] PathPoints = { new Vector2(-3f, -18f), new Vector2(-0.6f, -7f), new Vector2(0.9f, 0f), new Vector2(3.4f, 5f), new Vector2(5.2f, 10f), new Vector2(9f, 19f) };

        // (across, into, scale)
        private static readonly Vector3[] Trees =
        {
            new Vector3(-7.5f, 8.5f, 1.3f), new Vector3(-2.5f, 10.5f, 1.4f), new Vector3(2f, 9.8f, 1.25f), new Vector3(8.5f, 8f, 1.35f),
            new Vector3(-5f, 14f, 1.5f), new Vector3(3f, 15.5f, 1.5f), new Vector3(11.5f, 12.5f, 1.4f), new Vector3(-11f, 12f, 1.4f),
            new Vector3(-10.5f, 3f, 1.3f), new Vector3(-11.5f, -2.5f, 1.25f), new Vector3(11f, 3.5f, 1.3f), new Vector3(12f, -2f, 1.35f),
            new Vector3(-9f, -6f, 1.35f), new Vector3(10f, -6.5f, 1.3f), new Vector3(-15f, 6f, 1.4f), new Vector3(15.5f, 7f, 1.4f),
        };

        private static readonly Vector3[] Firs =
        {
            new Vector3(-6f, 6.6f, 1.6f), new Vector3(6.6f, 6.8f, 1.5f), new Vector3(9.4f, 1.6f, 1.4f), new Vector3(-8.8f, 0.6f, 1.6f), new Vector3(-0.6f, 7.8f, 1.3f),
            new Vector3(8.2f, -4.2f, 1.5f), new Vector3(-7.4f, -3.4f, 1.4f),
        };

        private static readonly Vector3[] Rocks =
        {
            new Vector3(7f, 5.6f, 1.3f), new Vector3(-7.6f, -4.2f, 0.9f), new Vector3(5.8f, -5.2f, 0.8f), new Vector3(-5.2f, 6.6f, 0.75f), new Vector3(8.8f, -6.8f, 0.9f),
            new Vector3(-9.2f, -6.2f, 0.7f), new Vector3(2.6f, -9f, 0.6f), new Vector3(-2.5f, 13f, 1.1f),
        };

        private static readonly Vector3[] Bushes =
        {
            new Vector3(-5.2f, 8.6f, 1f), new Vector3(4.6f, 8.8f, 1.1f), new Vector3(11.2f, 4.8f, 1f), new Vector3(-11.4f, 3.4f, 1.1f), new Vector3(-9.8f, -3.2f, 0.9f),
            new Vector3(9.8f, -5.6f, 1f), new Vector3(-7.4f, -8.4f, 1f), new Vector3(7.6f, -8.6f, 0.9f), new Vector3(0.5f, 12.8f, 1.1f),
            new Vector3(-3.6f, 7.4f, 0.9f), new Vector3(5.8f, 6.8f, 1f), new Vector3(-12.6f, 6.2f, 1.1f), new Vector3(13.2f, 5.2f, 1f),
            new Vector3(-12.8f, -4.2f, 1f), new Vector3(13.6f, -4.8f, 1.1f), new Vector3(-6.6f, -5.4f, 0.85f), new Vector3(6.9f, -5.9f, 0.9f),
        };

        private static readonly Dictionary<string, Material> s_materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, GameObject> s_models = new Dictionary<string, GameObject>();
        private static Terrain s_terrain;

        [MenuItem("Runeheir/Art/Render Style Samples", priority = 53)]
        public static void RenderMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var files = Render();
            EditorUtility.RevealInFinder(files[0]);
        }

        public static void RenderFromCommandLine()
        {
            try
            {
                foreach (string file in Render())
                {
                    Debug.Log("[Runeheir] Style sample written: " + file);
                }

                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static List<string> Render()
        {
            if (!AssetDatabase.IsValidFolder(Root + "/Models"))
            {
                throw new InvalidOperationException("The CC0 sample assets are missing: run Tools/ArtSamples/fetch_polyhaven.py first (see Docs/Art_Direction.md).");
            }

            PrepareImports();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", MapPreview.OutputFolder));
            Directory.CreateDirectory(folder);
            var files = new List<string>();
            foreach (var style in new[] { Style.Modern, Style.Anime })
            {
                files.AddRange(RenderStyle(style, folder));
            }

            return files;
        }

        private static List<string> RenderStyle(Style style, string folder)
        {
            s_materials.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var random = new System.Random(1207);
            var root = new GameObject("ArtSample").transform;
            var camera = new GameObject("SampleCamera").AddComponent<Camera>();
            Lighting(style, camera);
            s_terrain = BuildTerrain(style, root);
            Forest(style, root, random);
            Camp(style, root);
            Meadow(style, root, random);
            Characters(style, root);

            string tag = style == Style.Modern ? "modern" : "anime";
            var restore = Occlusion(style == Style.Modern ? 1.2f : 0.5f, style == Style.Modern ? 0.6f : 0.3f);
            try
            {
                return new List<string>
                {
                    Shoot(camera, folder, "sample_" + tag + ".png", OnGround(new Vector2(0.4f, 1.6f)), 22f, 48f),
                    Shoot(camera, folder, "sample_" + tag + "_close.png", OnGround(HeroSpot + new Vector2(1.2f, 0.9f)), 9.5f, 32f),
                };
            }
            finally
            {
                restore();
            }
        }

        /// <summary>
        /// Sets the PC renderer's SSAO strength and radius for this shot (the project's renderer asset is left as it was
        /// afterwards: call the returned action).
        /// </summary>
        private static Action Occlusion(float intensity, float radius)
        {
            try
            {
                const BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
                var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                var renderers = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", hidden)?.GetValue(asset) as ScriptableRendererData[];
                foreach (var data in renderers ?? new ScriptableRendererData[0])
                {
                    foreach (var feature in data != null ? data.rendererFeatures : new List<ScriptableRendererFeature>())
                    {
                        if (feature == null || feature.GetType().Name != "ScreenSpaceAmbientOcclusion")
                        {
                            continue;
                        }

                        var settings = feature.GetType().GetField("m_Settings", hidden)?.GetValue(feature);
                        var strength = settings?.GetType().GetField("Intensity");
                        var reach = settings?.GetType().GetField("Radius");
                        if (strength == null || reach == null)
                        {
                            continue;
                        }

                        object oldStrength = strength.GetValue(settings);
                        object oldReach = reach.GetValue(settings);
                        strength.SetValue(settings, intensity);
                        reach.SetValue(settings, radius);
                        return () =>
                        {
                            strength.SetValue(settings, oldStrength);
                            reach.SetValue(settings, oldReach);
                        };
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Runeheir] Could not adjust SSAO for the sample: " + e.Message);
            }

            return () => { };
        }

        // ------------------------------------------------------------------ assets

        private static void PrepareImports()
        {
            if (!AssetDatabase.IsValidFolder(Generated))
            {
                AssetDatabase.CreateFolder(Root, "Generated");
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(Generated) || !(AssetImporter.GetAtPath(path) is TextureImporter importer))
                {
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(path);
                bool dirty = false;
                if (name.Contains("_nor_gl") && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    dirty = true;
                }

                bool data = name.Contains("_rough") || name.Contains("_metal") || name.Contains("_alpha") || name.Contains("_opacity") || name.Contains("_mask") ||
                            name.Contains("_disp");
                if (data && importer.sRGBTexture)
                {
                    importer.sRGBTexture = false;
                    dirty = true;
                }

                if (name.Contains("_diff") && path.EndsWith(".png") && importer.DoesSourceTextureHaveAlpha() && !importer.mipMapsPreserveCoverage)
                {
                    importer.alphaIsTransparency = true;
                    importer.mipMapsPreserveCoverage = true;
                    importer.alphaTestReferenceValue = 0.5f;
                    dirty = true;
                }

                if (name == SkyName + "_2k" && importer.textureShape != TextureImporterShape.TextureCube)
                {
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.textureShape = TextureImporterShape.TextureCube;
                    settings.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
                    settings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
                    importer.SetTextureSettings(settings);
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Root + "/Models" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is ModelImporter model && (model.importCameras || model.importLights || model.animationType != ModelImporterAnimationType.None))
                {
                    model.importCameras = false;
                    model.importLights = false;
                    model.importAnimation = false;
                    model.animationType = ModelImporterAnimationType.None;
                    model.SaveAndReimport();
                }
            }

            // A readable lat-long copy of the sky, for the ambient light.
            string latlong = Generated + "/sky_latlong.hdr";
            if (!File.Exists(latlong))
            {
                File.Copy(Root + "/Sky/" + SkyName + "_2k.hdr", latlong);
                AssetDatabase.ImportAsset(latlong);
                var importer = (TextureImporter)AssetImporter.GetAtPath(latlong);
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static GameObject Model(string name)
        {
            if (!s_models.TryGetValue(name, out var model) || model == null)
            {
                model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + name + "/" + name + ".fbx");
                if (model == null)
                {
                    throw new InvalidOperationException("Missing sample model " + name);
                }

                s_models[name] = model;
            }

            return model;
        }

        /// <summary>The model's separate pieces (each variant of a set; only the most detailed level of a LOD chain).</summary>
        private static List<string> Parts(string model)
        {
            var parts = new List<string>();
            foreach (Transform child in Model(model).transform)
            {
                if (child.GetComponent<Renderer>() != null && (!child.name.Contains("_LOD") || child.name.EndsWith("_LOD0")))
                {
                    parts.Add(child.name);
                }
            }

            return parts;
        }

        private static Texture2D Decode(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        /// <summary>A leaf texture with its separate alpha folded in (for alpha clipping), mipmapped to keep its coverage.</summary>
        private static Texture2D Cutout(string diffusePath, string alphaPath)
        {
            string path = Generated + "/" + Path.GetFileNameWithoutExtension(diffusePath) + "_cutout.png";
            if (!File.Exists(path))
            {
                var color = Decode(diffusePath);
                var alpha = Decode(alphaPath);
                var c = color.GetPixels32();
                var a = alpha.GetPixels32();
                if (a.Length != c.Length)
                {
                    throw new InvalidOperationException(alphaPath + " does not match " + diffusePath);
                }

                for (int i = 0; i < c.Length; i++)
                {
                    c[i].a = a[i].a < 255 ? a[i].a : a[i].r;
                }

                var output = new Texture2D(color.width, color.height, TextureFormat.RGBA32, false);
                output.SetPixels32(c);
                File.WriteAllBytes(path, output.EncodeToPNG());
                Object.DestroyImmediate(color);
                Object.DestroyImmediate(alpha);
                Object.DestroyImmediate(output);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.alphaIsTransparency = true;
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = 0.5f;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>A terrain mask map (R metal, G occlusion, B height, A smoothness): height from the layer's brightness.</summary>
        private static Texture2D TerrainMask(string layer)
        {
            string path = Generated + "/" + layer + "_mask.png";
            if (!File.Exists(path))
            {
                string folder = Root + "/Textures/" + layer + "/" + layer;
                var diffuse = Decode(folder + "_diff_2k.jpg");
                var rough = Decode(folder + "_rough_2k.jpg");
                var d = diffuse.GetPixels32();
                var r = rough.GetPixels32();
                var luma = new float[d.Length];
                var sample = new List<float>();
                for (int i = 0; i < d.Length; i++)
                {
                    luma[i] = (0.2126f * d[i].r + 0.7152f * d[i].g + 0.0722f * d[i].b) / 255f;
                    if (i % 97 == 0)
                    {
                        sample.Add(luma[i]);
                    }
                }

                sample.Sort();
                float lo = sample[sample.Count / 50];
                float hi = sample[sample.Count - 1 - sample.Count / 50];
                var mask = new Color32[d.Length];
                for (int i = 0; i < d.Length; i++)
                {
                    float height = Mathf.Clamp01((luma[i] - lo) / Mathf.Max(hi - lo, 0.01f));
                    byte smooth = r.Length == d.Length ? (byte)(255 - r[i].r) : (byte)60;
                    mask[i] = new Color32(0, 255, (byte)(height * 255f), smooth);
                }

                var output = new Texture2D(diffuse.width, diffuse.height, TextureFormat.RGBA32, false);
                output.SetPixels32(mask);
                File.WriteAllBytes(path, output.EncodeToPNG());
                Object.DestroyImmediate(diffuse);
                Object.DestroyImmediate(rough);
                Object.DestroyImmediate(output);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static string FindTexture(string folder, List<string> stems, string suffix)
        {
            foreach (string stem in stems)
            {
                foreach (string extension in new[] { ".png", ".jpg", ".exr" })
                {
                    string path = folder + stem + suffix + extension;
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        // ------------------------------------------------------------------ materials

        private static Material MaterialFor(Style style, string model, Material source)
        {
            string slot = source != null ? source.name : model;
            string key = style + "/" + model + "/" + slot;
            if (s_materials.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            string folder = Root + "/Models/" + model + "/textures/";
            var stems = new List<string> { slot, slot.Replace("_branches", "_branch"), slot.Replace("_trunk", ""), slot.Replace("_glass", "_brass"), model };
            string diffuse = FindTexture(folder, stems, "_diff_1k");
            string alpha = FindTexture(folder, stems, "_alpha_1k");
            Kind kind = slot.EndsWith("_glass") ? Kind.Glass : model == Grass ? Kind.Grass : Kind.Solid;
            Texture2D baseMap = null;
            if (kind != Kind.Glass && diffuse != null)
            {
                if (alpha != null)
                {
                    baseMap = Cutout(diffuse, alpha);
                    kind = kind == Kind.Grass ? Kind.Grass : Kind.Foliage;
                }
                else
                {
                    baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse);
                    if (kind == Kind.Solid && diffuse.EndsWith(".png") && ((TextureImporter)AssetImporter.GetAtPath(diffuse)).DoesSourceTextureHaveAlpha())
                    {
                        kind = Kind.Foliage;
                    }
                }
            }

            Material material;
            if (style == Style.Modern)
            {
                string normal = FindTexture(folder, stems, "_nor_gl_1k");
                string metal = FindTexture(folder, stems, "_metallic_1k") ?? FindTexture(folder, stems, "_metal_1k");
                material = ModernMaterial(kind, slot, baseMap, normal != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(normal) : null,
                    metal != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(metal) : null);
            }
            else
            {
                material = AnimeMaterial(kind, slot, baseMap);
            }

            s_materials[key] = material;
            return material;
        }

        private static Material ModernMaterial(Kind kind, string slot, Texture2D baseMap, Texture2D normal, Texture2D metal)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = slot + " (modern)", enableInstancing = true };
            if (kind == Kind.Glass)
            {
                material.SetColor("_BaseColor", new Color(1f, 0.78f, 0.45f));
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(5f, 2.6f, 0.9f));
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                return material;
            }

            material.SetTexture("_BaseMap", baseMap);
            material.SetTexture("_MainTex", baseMap);
            material.SetFloat("_Smoothness", 0.22f);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            if (metal != null)
            {
                material.SetTexture("_MetallicGlossMap", metal);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Smoothness", 0.4f);
            }

            if (kind == Kind.Solid)
            {
                return material;
            }

            // Leaves, grass and ferns: two-sided PBR, shaded as one volume like foliage in modern games.
            material.shader = Shader.Find("Runeheir/LitFoliage");
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.SetFloat("_Smoothness", 0.18f);
            material.SetFloat("_SphereNormals", kind == Kind.Grass ? 0.65f : 0.45f);
            material.SetFloat("_BaseShade", kind == Kind.Grass ? 0.3f : 0.25f);
            material.SetFloat("_Translucency", kind == Kind.Grass ? 0.6f : 0.4f);
            material.SetColor("_BaseColor", kind == Kind.Grass ? new Color(1.25f, 1.35f, 1f) : Color.white);
            return material;
        }

        private static Material AnimeMaterial(Kind kind, string slot, Texture2D baseMap)
        {
            var material = new Material(Shader.Find("Runeheir/ToonFoliage")) { name = slot + " (anime)", enableInstancing = true };
            if (kind == Kind.Glass)
            {
                material.SetColor("_BaseColor", new Color(1f, 0.85f, 0.55f));
                material.SetColor("_EmissionColor", new Color(3.2f, 1.9f, 0.8f));
                return material;
            }

            material.SetTexture("_BaseMap", baseMap);
            material.SetFloat("_Saturation", 1.15f);
            if (kind == Kind.Solid)
            {
                material.SetFloat("_MipBias", 1.2f);
                material.SetFloat("_RampSmoothness", 0.04f);
                material.SetFloat("_Saturation", 1.1f);
                return material;
            }

            material.SetFloat("_AlphaClip", 1f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cutoff", 0.5f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.SetFloat("_Translucency", 0.45f);
            material.SetFloat("_SphereNormals", 0.75f);
            material.SetFloat("_BaseShade", 0.25f);
            material.SetFloat("_RampSmoothness", 0.1f);
            material.SetColor("_BaseColor", new Color(1f, 1.04f, 0.9f));
            if (kind == Kind.Grass)
            {
                material.SetFloat("_SphereNormals", 0.9f);
                material.SetFloat("_BaseShade", 0.3f);
                material.SetColor("_BaseColor", new Color(1.2f, 1.3f, 0.98f));
            }

            return material;
        }

        /// <summary>Toon foliage is lit as one mass around its middle (grass: around a point far below, so as the ground).</summary>
        private static void FoliageCenters(GameObject go, bool grass)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var bounds = renderer.bounds;
                var center = grass ? bounds.center - Vector3.up * 40f : bounds.center + Vector3.up * (bounds.extents.y * 0.25f);
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetVector("_SphereCenterWS", center);
                block.SetFloat("_BaseShadeHeight", grass ? Mathf.Max(0.1f, bounds.size.y) : Mathf.Max(0.3f, bounds.size.y * 0.6f));
                renderer.SetPropertyBlock(block);
            }
        }

        /// <summary>The modern sample's characters: the toon materials swapped for URP Lit with the same colors.</summary>
        private static void Relight(GameObject root)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var copies = new Dictionary<Material, Material>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null || source.shader == null || source.shader.name != "Runeheir/Toon")
                    {
                        continue;
                    }

                    if (!copies.TryGetValue(source, out var copy))
                    {
                        copy = new Material(lit) { name = source.name + " (modern)" };
                        copy.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                        copy.SetTextureScale("_BaseMap", source.GetTextureScale("_BaseMap"));
                        copy.SetTextureOffset("_BaseMap", source.GetTextureOffset("_BaseMap"));
                        copy.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                        copy.SetFloat("_Smoothness", 0.38f);
                        var emission = source.GetColor("_EmissionColor");
                        if (emission.maxColorComponent > 0.01f)
                        {
                            copy.EnableKeyword("_EMISSION");
                            copy.SetColor("_EmissionColor", emission);
                        }

                        copies[source] = copy;
                    }

                    materials[i] = copy;
                }

                renderer.sharedMaterials = materials;
            }
        }

        // ------------------------------------------------------------------ placing things

        private static Vector3 World(Vector2 screen)
        {
            return ScreenRight * screen.x + ScreenUp * screen.y;
        }

        private static Vector3 OnGround(Vector2 screen)
        {
            var p = World(screen);
            p.y = s_terrain.SampleHeight(p) + s_terrain.transform.position.y;
            return p;
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(go.transform.position, Vector3.zero);
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                var deeper = FindChild(child, name);
                if (deeper != null)
                {
                    return deeper;
                }
            }

            return null;
        }

        /// <summary>
        /// Places a model (or one piece of a set) standing on the ground at <paramref name="at"/>: its footprint centered,
        /// its bottom sunk by <paramref name="sink"/> of its height, painted for the style.
        /// </summary>
        private static GameObject Spawn(Style style, string model, string part, Vector3 at, float yaw, float scale, Transform parent, float sink = 0.02f)
        {
            var prefab = Model(model);
            var source = prefab;
            if (part != null)
            {
                var child = FindChild(prefab.transform, part);
                if (child == null)
                {
                    throw new InvalidOperationException(model + " has no piece " + part);
                }

                source = child.gameObject;
            }

            var holder = new GameObject(part ?? model).transform;
            holder.SetParent(parent, false);
            var instance = Object.Instantiate(source, holder);
            instance.name = source.name;
            if (part != null)
            {
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.Inverse(prefab.transform.rotation) * source.transform.rotation;
                instance.transform.localScale = source.transform.lossyScale;
            }

            var group = instance.GetComponent<LODGroup>();
            if (group != null)
            {
                var lods = group.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                {
                    foreach (var renderer in lods[i].renderers)
                    {
                        if (renderer != null)
                        {
                            renderer.gameObject.SetActive(false);
                        }
                    }
                }

                Object.DestroyImmediate(group);
            }

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = MaterialFor(style, model, materials[i]);
                }

                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }

            var bounds = BoundsOf(instance);
            instance.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y + sink * bounds.size.y, bounds.center.z) - holder.position;
            holder.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            holder.localScale = Vector3.one * scale;
            FoliageCenters(instance, model == Grass);
            return instance;
        }

        private static float Next(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        // ------------------------------------------------------------------ the ground

        private static float GroundHeight(float x, float z)
        {
            float rolling = Mathf.PerlinNoise(x * 0.03f + 11.3f, z * 0.03f + 7.1f) * 3.2f + Mathf.PerlinNoise(x * 0.1f + 3.7f, z * 0.1f + 5.9f) * 0.6f - 1.9f;
            float open = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6f, 18f, new Vector2(x, z).magnitude));
            return BaseHeight + rolling * open + 0.12f * Mathf.PerlinNoise(x * 0.45f + 0.7f, z * 0.45f + 2.3f);
        }

        private static float DistanceToPath(Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 1; i < PathPoints.Length; i++)
            {
                var a = PathPoints[i - 1];
                var ab = PathPoints[i] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }

            return best;
        }

        private static float Falloff(float distance, float inner, float outer)
        {
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, outer, distance));
        }

        /// <summary>How much of each ground layer covers a spot: grass, forest floor, the trodden path and camp, stone.</summary>
        private static Vector4 GroundWeights(float x, float z)
        {
            var p = new Vector3(x, 0f, z);
            var screen = new Vector2(Vector3.Dot(p, ScreenRight), Vector3.Dot(p, ScreenUp));
            float wobble = Mathf.PerlinNoise(x * 0.35f + 1.3f, z * 0.35f + 8.1f);
            float path = Falloff(DistanceToPath(screen), 0.6f, 1.4f + 0.6f * wobble);
            float camp = Falloff(Vector2.Distance(screen, FirePit + new Vector2(0.8f, -0.4f)), 2f, 4f + 1.2f * wobble);
            float forest = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.7f, Mathf.PerlinNoise(x * 0.07f + 4.1f, z * 0.07f + 2.7f)));
            foreach (var tree in Trees)
            {
                forest = Mathf.Max(forest, Falloff(Vector2.Distance(screen, tree), 2f, 5f + 1.5f * wobble));
            }

            float rock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.74f, 0.8f, Mathf.PerlinNoise(x * 0.12f + 9.4f, z * 0.12f + 1.2f)));
            foreach (var stone in Rocks)
            {
                rock = Mathf.Max(rock, Falloff(Vector2.Distance(screen, stone), 0.5f * stone.z, (1.6f + wobble) * stone.z));
            }

            float trail = Mathf.Max(path, camp * 0.9f) * (1f - rock);
            float floor = Mathf.Max(0f, forest * (1f - trail - rock));
            float grass = Mathf.Max(0f, 1f - rock - trail - floor);
            return new Vector4(grass, floor, trail, rock);
        }

        private static Terrain BuildTerrain(Style style, Transform parent)
        {
            var data = new TerrainData { heightmapResolution = 257, alphamapResolution = 512, baseMapResolution = 1024 };
            data.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
            float half = TerrainSize * 0.5f;
            int res = data.heightmapResolution;
            var heights = new float[res, res];
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    heights[z, x] = GroundHeight(-half + x * TerrainSize / (res - 1), -half + z * TerrainSize / (res - 1)) / TerrainHeight;
                }
            }

            data.SetHeights(0, 0, heights);
            var layers = new TerrainLayer[GroundLayers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                string folder = Root + "/Textures/" + GroundLayers[i] + "/" + GroundLayers[i];
                layers[i] = new TerrainLayer
                {
                    name = GroundLayers[i],
                    diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "_diff_2k.jpg"),
                    tileSize = new Vector2(GroundTiles[i], GroundTiles[i]),
                    smoothness = 0.1f,
                };
                if (style == Style.Modern)
                {
                    layers[i].normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "_nor_gl_2k.jpg");
                    layers[i].maskMapTexture = TerrainMask(GroundLayers[i]);
                    layers[i].maskMapRemapMax = new Vector4(1f, 1f, 1f, 0.5f);
                    // The scanned grass is late-summer pale: green it and pull it down so the clumps carry the meadow.
                    layers[i].diffuseRemapMax = i == 0 ? new Vector4(0.74f, 0.88f, 0.58f, 1f)
                        : i == 1 ? new Vector4(0.84f, 0.8f, 0.76f, 1f)
                        : i == 2 ? new Vector4(0.78f, 0.72f, 0.64f, 1f)
                        : Vector4.one;
                }
            }

            data.terrainLayers = layers;
            int alphaRes = data.alphamapResolution;
            var maps = new float[alphaRes, alphaRes, layers.Length];
            for (int z = 0; z < alphaRes; z++)
            {
                for (int x = 0; x < alphaRes; x++)
                {
                    var w = GroundWeights(-half + (x + 0.5f) * TerrainSize / alphaRes, -half + (z + 0.5f) * TerrainSize / alphaRes);
                    for (int l = 0; l < layers.Length; l++)
                    {
                        maps[z, x, l] = w[l];
                    }
                }
            }

            data.SetAlphamaps(0, 0, maps);
            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Ground";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-half, 0f, -half);
            var terrain = go.GetComponent<Terrain>();
            terrain.drawInstanced = false;
            terrain.heightmapPixelError = 2f;
            terrain.basemapDistance = 500f;
            if (style == Style.Modern)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")) { name = "Ground (modern)" };
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_MASKMAP");
                material.EnableKeyword("_TERRAIN_BLEND_HEIGHT");
                material.SetFloat("_EnableHeightBlend", 1f);
                material.SetFloat("_HeightTransition", 0.25f);
                terrain.materialTemplate = material;
            }
            else
            {
                var material = new Material(Shader.Find("Runeheir/ToonSplat")) { name = "Ground (anime)" };
                material.SetTexture("_Control", data.alphamapTextures[0]);
                for (int i = 0; i < layers.Length; i++)
                {
                    material.SetTexture("_Splat" + i, layers[i].diffuseTexture);
                }

                material.SetVector("_TileSizes", new Vector4(GroundTiles[0], GroundTiles[1], GroundTiles[2], GroundTiles[3]));
                material.SetColor("_Tint0", new Color(0.8f, 0.96f, 0.66f));
                material.SetColor("_Tint1", new Color(1.02f, 0.95f, 0.86f));
                material.SetColor("_Tint2", new Color(0.96f, 0.86f, 0.74f));
                material.SetColor("_Tint3", new Color(0.98f, 0.98f, 1f));
                material.SetFloat("_Saturation", 1.1f);
                terrain.materialTemplate = material;
            }

            return terrain;
        }

        // ------------------------------------------------------------------ the scene

        private static void Forest(Style style, Transform root, System.Random random)
        {
            foreach (var tree in Trees)
            {
                Spawn(style, "tree_small_02", null, OnGround(tree), Next(random, 0f, 360f), tree.z * Next(random, 0.92f, 1.08f), root);
            }

            var firs = Parts("fir_sapling");
            foreach (var fir in Firs)
            {
                Spawn(style, "fir_sapling", firs[random.Next(firs.Count)], OnGround(fir), Next(random, 0f, 360f), fir.z, root);
            }

            var stones = Parts("rock_moss_set_01");
            for (int i = 0; i < Rocks.Length; i++)
            {
                var spot = Rocks[i];
                if (i % 3 == 0)
                {
                    Spawn(style, "boulder_01", null, OnGround(spot), Next(random, 0f, 360f), spot.z, root, 0.12f);
                }
                else
                {
                    Spawn(style, "rock_moss_set_01", stones[random.Next(stones.Count)], OnGround(spot), Next(random, 0f, 360f), spot.z, root, 0.15f);
                }
            }

            var bushes = Parts("shrub_02");
            foreach (var bush in Bushes)
            {
                Spawn(style, "shrub_02", bushes[random.Next(bushes.Count)], OnGround(bush), Next(random, 0f, 360f), bush.z, root, 0.05f);
            }

            // A fallen trunk at the clearing's edge.
            Spawn(style, "dead_tree_trunk", null, OnGround(new Vector2(7.4f, -2.6f)), 70f, 1.6f, root, 0.12f);
            Spawn(style, "tree_stump_01", null, OnGround(new Vector2(-6.8f, 3.2f)), 40f, 1f, root, 0.1f);

            // Undergrowth: small shrubs and ferns where the forest floor shows.
            var shrubs = Parts("shrub_01");
            var ferns = Parts("fern_02");
            for (int placed = 0, tries = 0; placed < 170 && tries < 6000; tries++)
            {
                var screen = new Vector2(Next(random, -18f, 18f), Next(random, -13f, 17f));
                var world = World(screen);
                var w = GroundWeights(world.x, world.z);
                if (w.z > 0.2f || Vector2.Distance(screen, FirePit) < 4f || Vector2.Distance(screen, HeroSpot) < 2.5f || random.NextDouble() > w.y * 0.9f + w.x * 0.08f)
                {
                    continue;
                }

                bool fern = random.NextDouble() < 0.45;
                if (fern)
                {
                    Spawn(style, "fern_02", ferns[random.Next(ferns.Count)], OnGround(screen), Next(random, 0f, 360f), Next(random, 1.4f, 2f), root);
                }
                else
                {
                    Spawn(style, "shrub_01", shrubs[random.Next(shrubs.Count)], OnGround(screen), Next(random, 0f, 360f), Next(random, 2.6f, 3.6f), root);
                }

                placed++;
            }
        }

        private static void Camp(Style style, Transform root)
        {
            var pit = OnGround(FirePit);
            Spawn(style, "stone_fire_pit", null, pit, 20f, 1.1f, root, 0.25f);
            Spawn(style, "dead_tree_trunk", null, pit + Vector3.up * 0.02f, 30f, 0.24f, root, 0.2f);
            Spawn(style, "dead_tree_trunk", null, pit + Vector3.up * 0.06f, 115f, 0.22f, root, 0.2f);
            Flames(style, pit + Vector3.up * 0.12f, root);

            // Gear by the fire, a stump to sit on and a lantern on another.
            Spawn(style, "tree_stump_01", null, OnGround(FirePit + new Vector2(-1.9f, 0.3f)), 120f, 0.55f, root, 0.2f);
            var chest = Spawn(style, "treasure_chest", null, OnGround(FirePit + new Vector2(-2.6f, 2.9f)), 205f, 1f, root);
            chest.name = "Chest";
            Spawn(style, "wooden_crate_01", null, OnGround(FirePit + new Vector2(-3.7f, 1.3f)), 160f, 1.5f, root);
            Spawn(style, "wooden_crate_01", null, OnGround(FirePit + new Vector2(-3.5f, 2.1f)), 120f, 1.35f, root);
            var stump = Spawn(style, "tree_stump_01", null, OnGround(FirePit + new Vector2(1.8f, 1.4f)), 0f, 0.7f, root, 0.15f);
            var top = BoundsOf(stump);
            var lantern = Spawn(style, "Lantern_01", null, new Vector3(top.center.x, top.max.y - 0.03f, top.center.z), 30f, 1.6f, root, 0f);
            var glow = new GameObject("LanternLight").AddComponent<Light>();
            glow.transform.SetParent(root, false);
            glow.transform.position = BoundsOf(lantern).center;
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.66f, 0.32f);
            glow.range = 4.5f;
            glow.intensity = style == Style.Modern ? 2.2f : 1.5f;
            glow.shadows = LightShadows.None;
        }

        private static void Meadow(Style style, Transform root, System.Random random)
        {
            // Real grass clumps are 15-35 cm; games grow them so they read from 20 m up.
            var blades = Parts(Grass).FindAll(part => part.Contains("_large_") || part.Contains("_mid_") || part.Contains("_tall_"));
            for (float across = -18f; across <= 18f; across += 0.42f)
            {
                for (float into = -13f; into <= 17f; into += 0.42f)
                {
                    var screen = new Vector2(across + Next(random, -0.2f, 0.2f), into + Next(random, -0.2f, 0.2f));
                    var world = World(screen);
                    var w = GroundWeights(world.x, world.z);
                    float chance = w.x * 0.9f + w.y * 0.15f + w.z * 0.05f;
                    if (Vector2.Distance(screen, FirePit) < 1.4f || random.NextDouble() > chance)
                    {
                        continue;
                    }

                    Spawn(style, Grass, blades[random.Next(blades.Count)], OnGround(screen), Next(random, 0f, 360f), Next(random, 2f, 2.9f), root, 0.04f);
                }
            }
        }

        private static void Characters(Style style, Transform root)
        {
            var heroAt = OnGround(HeroSpot);
            var foeA = OnGround(FoeA);
            var hero = new GameObject("Hero").transform;
            hero.SetParent(root, false);
            var toFoe = foeA - heroAt;
            hero.SetPositionAndRotation(heroAt, Quaternion.LookRotation(new Vector3(toFoe.x, 0f, toFoe.z)));
            var look = new AvatarLook
            {
                Job = Jobs.JobId.Valkyrie,
                Gender = Gender.Male,
                HairStyle = 1,
                Hair = AvatarLook.HairPalette[2],
                Skin = AvatarLook.DefaultSkin,
                Outfit = RuntimeMaterials.Hex(Jobs.JobDatabase.Get(Jobs.JobId.Valkyrie).ColorHex),
                Weapon = Combat.WeaponType.TwoHandSword,
            };
            var avatar = PlaceholderAvatar.CreateHumanoid(hero, look);
            avatar.Rig?.Animate(new RigMotion { Time = 0.8f, Combat = 1f, AttackT = 0.3f, AttackIndex = 1, SkillT = -1f });

            var outlaws = new[]
            {
                new Monsters.MonsterDefinition
                {
                    Id = "sample_outlaw_axe", Name = "Outlaw", Shape = Monsters.MonsterShape.Humanoid, ColorHex = "#5B4636", SkinHex = "#E2B58F",
                    LookWeapon = Combat.WeaponType.Axe,
                },
                new Monsters.MonsterDefinition
                {
                    Id = "sample_outlaw_spear", Name = "Outlaw", Shape = Monsters.MonsterShape.Humanoid, ColorHex = "#44513B", SkinHex = "#D9A982",
                    LookWeapon = Combat.WeaponType.Spear,
                },
            };
            var spots = new[] { foeA, OnGround(FoeB) };
            for (int i = 0; i < outlaws.Length; i++)
            {
                var spot = spots[i];
                var holder = new GameObject("Outlaw").transform;
                holder.SetParent(root, false);
                var toHero = heroAt - spot;
                holder.SetPositionAndRotation(spot, Quaternion.LookRotation(new Vector3(toHero.x, 0f, toHero.z)));
                var foe = PlaceholderAvatar.CreateMonster(holder, outlaws[i]);
                foe.Rig?.Animate(new RigMotion { Time = 0.4f + i, Combat = 1f, AttackT = -1f, SkillT = -1f });
                if (style == Style.Modern)
                {
                    Relight(holder.gameObject);
                }
            }

            if (style == Style.Modern)
            {
                Relight(hero.gameObject);
            }
        }

        // ------------------------------------------------------------------ fire

        private static void Flames(Style style, Vector3 at, Transform root)
        {
            bool anime = style == Style.Anime;
            var fire = new GameObject("Flames").transform;
            fire.SetParent(root, false);
            fire.position = at;

            // Teardrop tongues rising and shrinking, red at the edges and yellow-white where they overlap; the painted
            // fire keeps crisp edges and solid color, with a pale inner flame drawn over it.
            var flame = Ramp(
                anime ? new[] { new Color(1f, 0.62f, 0.12f), new Color(1f, 0.38f, 0.06f), new Color(0.85f, 0.14f, 0.04f) }
                      : new[] { new Color(1f, 0.6f, 0.22f), new Color(1f, 0.32f, 0.06f), new Color(0.6f, 0.08f, 0.02f) },
                anime ? new[] { 0f, 1f, 1f, 0f } : new[] { 0f, 0.8f, 0.5f, 0f });
            Particles(fire, "Flames", FireMaterial(!anime, true, !anime, anime ? 1.5f : 1.3f), anime ? 26f : 45f, new Vector2(0.5f, 0.8f), new Vector2(0.6f, 1.1f),
                new Vector2(0.55f, 0.9f), 0.22f, flame, 0.3f, 0.35f, false, 0.3f);
            var inner = Ramp(
                anime ? new[] { new Color(1f, 0.97f, 0.7f), new Color(1f, 0.85f, 0.35f), new Color(1f, 0.6f, 0.15f) }
                      : new[] { new Color(1f, 0.9f, 0.6f), new Color(1f, 0.7f, 0.3f), new Color(1f, 0.45f, 0.1f) },
                anime ? new[] { 0f, 1f, 1f, 0f } : new[] { 0f, 0.7f, 0.4f, 0f });
            var innerFlames = Particles(fire, "InnerFlames", FireMaterial(!anime, true, !anime, anime ? 1.7f : 1.4f), anime ? 18f : 30f, new Vector2(0.35f, 0.55f),
                new Vector2(0.4f, 0.8f), new Vector2(0.3f, 0.5f), 0.12f, inner, 0.3f, 0.25f, false, 0.25f);
            innerFlames.GetComponent<ParticleSystemRenderer>().sortingFudge = -20f;

            var core = Ramp(new[] { new Color(1f, 0.6f, 0.25f), new Color(1f, 0.45f, 0.12f), new Color(1f, 0.3f, 0.08f) }, new[] { 0f, 0.35f, 0.25f, 0f });
            Particles(fire, "Glow", FireMaterial(true, false, true, 1f), 10f, new Vector2(0.3f, 0.5f), new Vector2(0.05f, 0.15f), new Vector2(1.6f, 2f), 0.1f,
                core, 0.9f, 0f, false, 0f);

            if (!anime)
            {
                var smoke = Ramp(new[] { new Color(0.32f, 0.3f, 0.3f), new Color(0.4f, 0.4f, 0.42f), new Color(0.5f, 0.5f, 0.52f) }, new[] { 0f, 0.3f, 0.18f, 0f });
                var plume = Particles(fire, "Smoke", FireMaterial(true, false, false, 1f), 9f, new Vector2(2.5f, 3.5f), new Vector2(0.6f, 1f), new Vector2(0.5f, 0.8f),
                    0.2f, smoke, 3.2f, 0.4f, false, 6.3f);
                plume.localPosition = new Vector3(0f, 0.7f, 0f);
            }

            var ember = Ramp(new[] { new Color(1f, 0.85f, 0.45f), new Color(1f, 0.55f, 0.15f), new Color(1f, 0.3f, 0.06f) }, new[] { 1f, 1f, 1f, 0f });
            Particles(fire, "Embers", FireMaterial(false, false, true, 3f), 16f, new Vector2(1.2f, 2.4f), new Vector2(1.2f, 2.6f), new Vector2(0.03f, 0.06f), 0.35f, ember,
                0.5f, 1.2f, false, 6.3f);

            var light = new GameObject("FireLight").AddComponent<Light>();
            light.transform.SetParent(fire, false);
            light.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.22f);
            light.range = 9f;
            light.intensity = anime ? 3f : 4.5f;
            light.shadows = anime ? LightShadows.None : LightShadows.Soft;
        }

        /// <summary>Three colors over a particle's life, with alpha keys at 0, 15%, 60% and 100%.</summary>
        private static Gradient Ramp(Color[] colors, float[] alpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(colors[0], 0f), new GradientColorKey(colors[1], 0.4f), new GradientColorKey(colors[2], 1f) },
                new[] { new GradientAlphaKey(alpha[0], 0f), new GradientAlphaKey(alpha[1], 0.15f), new GradientAlphaKey(alpha[2], 0.6f), new GradientAlphaKey(alpha[3], 1f) });
            return gradient;
        }

        private static Transform Particles(Transform parent, string name, Material material, float rate, Vector2 life, Vector2 speed, Vector2 size, float radius,
            Gradient color, float endSize, float noise, bool tongues, float spin)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.duration = 2f;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(-spin, spin);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            var emission = system.emission;
            emission.rateOverTime = rate;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = radius;
            var tint = system.colorOverLifetime;
            tint.enabled = true;
            tint.color = new ParticleSystem.MinMaxGradient(color);
            var scale = system.sizeOverLifetime;
            scale.enabled = true;
            scale.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, endSize));
            var turbulence = system.noise;
            turbulence.enabled = noise > 0f;
            turbulence.strength = noise;
            turbulence.frequency = 1.5f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = tongues ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            renderer.velocityScale = 0.12f;
            renderer.lengthScale = 1.7f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            system.Simulate(4f, true, true, false);
            return go.transform;
        }

        private static Material FireMaterial(bool soft, bool tear, bool additive, float intensity)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = additive ? "Fire (additive)" : "Fire" };
            material.SetTexture("_BaseMap", FireTexture(soft, tear));
            material.SetColor("_BaseColor", new Color(intensity, intensity, intensity, 1f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        /// <summary>
        /// A flame sprite, soft for the modern fire and crisp-edged for the painted one: a round glow, or a teardrop tongue
        /// (round below, tapering to a tip at the top) that billboards keep upright on screen.
        /// </summary>
        private static Texture2D FireTexture(bool soft, bool tear)
        {
            const int n = 64;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = (soft ? "Soft" : "Crisp") + (tear ? "Tongue" : "Glow") };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    float v = (y + 0.5f) / n;
                    float d;
                    if (tear)
                    {
                        float width = v < 0.32f
                            ? Mathf.Sqrt(Mathf.Max(0f, 0.09f - (v - 0.32f) * (v - 0.32f))) / 0.3f * 0.6f
                            : 0.6f * Mathf.Pow(Mathf.Clamp01((0.97f - v) / 0.65f), 1.3f);
                        d = width > 1e-3f ? Mathf.Abs(u - 0.5f) * 2f / width : 2f;
                    }
                    else
                    {
                        d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
                    }

                    float a = soft ? Mathf.Pow(Mathf.Clamp01(1f - d), tear ? 0.9f : 1.6f) : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.82f, 1f, d));
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }

        // ------------------------------------------------------------------ light and grade

        private static void Lighting(Style style, Camera camera)
        {
            bool modern = style == Style.Modern;
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = modern ? 1f : 0.9f;
            float elevation = modern ? 30f : 50f;
            sun.transform.rotation = Quaternion.Euler(elevation, 104f, 0f);
            sun.color = modern ? new Color(1f, 0.8f, 0.6f) : new Color(1f, 0.96f, 0.88f);
            sun.intensity = modern ? 2.8f : 1.2f;
            RenderSettings.sun = sun;

            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(Root + "/Sky/" + SkyName + "_2k.hdr");
            var sky = new Material(Shader.Find("Skybox/Cubemap"));
            sky.SetTexture("_Tex", cube);
            sky.SetFloat("_Exposure", modern ? 1f : 1.2f);
            RenderSettings.skybox = sky;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cube;
            RenderSettings.reflectionIntensity = modern ? 1f : 0.6f;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            float sunHeight = Mathf.Sin(elevation * Mathf.Deg2Rad);
            RenderSettings.ambientProbe = SkyProbe(modern ? 0.5f : 0.78f, modern ? new Color(0.2f, 0.24f, 0.14f) : new Color(0.3f, 0.38f, 0.24f),
                sun.color * (sun.intensity * sunHeight));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = modern ? 0.0065f : 0.005f;
            RenderSettings.fogColor = modern ? new Color(0.74f, 0.72f, 0.7f) : new Color(0.72f, 0.84f, 0.96f);

            camera.clearFlags = CameraClearFlags.Skybox;
            camera.allowHDR = true;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.renderShadows = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            Grade(style);
        }

        /// <summary>
        /// The ambient light from the HDRI: the sky's upper half (the sun's disc clipped, since the sun is its own light), a
        /// sunlit ground below, scaled so a surface facing up gets <paramref name="upBrightness"/>.
        /// </summary>
        private static SphericalHarmonicsL2 SkyProbe(float upBrightness, Color groundAlbedo, Color sunOnGround)
        {
            var latlong = AssetDatabase.LoadAssetAtPath<Texture2D>(Generated + "/sky_latlong.hdr");
            const int columns = 128;
            const int rows = 64;
            var sh = new SphericalHarmonicsL2();
            var skyOnGround = Color.black;
            for (int pass = 0; pass < 2; pass++)
            {
                var ground = groundAlbedo * (skyOnGround + sunOnGround);
                for (int j = 0; j < rows; j++)
                {
                    float v = (j + 0.5f) / rows;
                    float theta = (1f - v) * Mathf.PI;
                    float y = Mathf.Cos(theta);
                    float sinT = Mathf.Sin(theta);
                    if ((pass == 0) != (y > 0f))
                    {
                        continue;
                    }

                    float solid = (2f * Mathf.PI / columns) * (Mathf.PI / rows) * sinT;
                    for (int i = 0; i < columns; i++)
                    {
                        float u = (i + 0.5f) / columns;
                        float phi = (0.5f - u) * 2f * Mathf.PI;
                        var direction = new Vector3(sinT * Mathf.Cos(phi), y, sinT * Mathf.Sin(phi));
                        Color radiance;
                        if (pass == 0)
                        {
                            var c = latlong.GetPixelBilinear(u, v);
                            radiance = new Color(Mathf.Min(c.r, 3f), Mathf.Min(c.g, 3f), Mathf.Min(c.b, 3f));
                            skyOnGround += radiance * (y * solid / Mathf.PI);
                        }
                        else
                        {
                            radiance = ground;
                        }

                        sh.AddDirectionalLight(direction, radiance, solid / Mathf.PI);
                    }
                }
            }

            var up = new Color[1];
            sh.Evaluate(new[] { Vector3.up }, up);
            float brightness = up[0].grayscale;
            if (brightness > 1e-4f)
            {
                sh *= upBrightness / brightness;
            }

            return sh;
        }

        private static void Grade(Style style)
        {
            bool modern = style == Style.Modern;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = modern ? "Sample Grade (modern)" : "Sample Grade (anime)";
            profile.Add<Tonemapping>(true).mode.Override(modern ? TonemappingMode.ACES : TonemappingMode.Neutral);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(modern ? 1.1f : 0.9f);
            bloom.intensity.Override(modern ? 0.35f : 0.7f);
            bloom.scatter.Override(0.7f);
            var grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(modern ? 0.25f : 0.1f);
            grade.contrast.Override(modern ? 14f : 8f);
            grade.saturation.Override(modern ? 0f : 8f);
            var tones = profile.Add<ShadowsMidtonesHighlights>(true);
            tones.shadows.Override(modern ? new Vector4(0.94f, 0.98f, 1.08f, 0f) : new Vector4(0.9f, 0.94f, 1.12f, 0f));
            tones.highlights.Override(modern ? new Vector4(1.04f, 1f, 0.94f, 0f) : new Vector4(1.03f, 1f, 0.96f, 0f));
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(modern ? 0.3f : 0.2f);
            vignette.smoothness.Override(0.45f);

            var volume = new GameObject("Grade").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 50f;
            volume.sharedProfile = profile;
        }

        /// <summary>Renders at twice the size and averages down, so leaves and grass blades stay smooth.</summary>
        private static string Shoot(Camera camera, string folder, string fileName, Vector3 focus, float distance, float pitch)
        {
            const int width = 1920;
            const int height = 1080;
            var rotation = Quaternion.Euler(pitch, 45f, 0f);
            camera.transform.SetPositionAndRotation(focus + Vector3.up + rotation * new Vector3(0f, 0f, -distance), rotation);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 300f;
            var big = new RenderTexture(width * 2, height * 2, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = big;
            camera.Render();
            camera.targetTexture = null;

            var small = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            big.filterMode = FilterMode.Bilinear;
            Graphics.Blit(big, small);
            var previous = RenderTexture.active;
            RenderTexture.active = small;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            string path = Path.Combine(folder, fileName);
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(big);
            Object.DestroyImmediate(small);
            return path;
        }
    }
}
