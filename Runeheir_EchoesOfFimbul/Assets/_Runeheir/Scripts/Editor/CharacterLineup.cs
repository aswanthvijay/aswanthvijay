using System;
using System.Collections.Generic;
using System.IO;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Visuals;
using Runeheir.World;
using Runeheir.WorldBuilding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Runeheir.EditorTools
{
    /// <summary>
    /// Renders the Blender-built characters through the game's toon shader, posed by the game's own procedural animation:
    /// Runeheir ▸ Art ▸ Render Character Lineup, or from the command line
    /// (<c>-executeMethod Runeheir.EditorTools.CharacterLineup.RenderFromCommandLine</c>, without -nographics).
    /// Writes Tools/Blender/Previews/unity_*.png in a throwaway scene: the outfits, every weapon in hand, attack filmstrips
    /// and the casting poses.
    /// </summary>
    public static class CharacterLineup
    {
        public const string OutputFolder = "Tools/Blender/Previews";

        private static readonly (string Key, JobId Job)[] Outfits =
        {
            ("common", JobId.Initiate), ("warrior", JobId.Warrior), ("berserker", JobId.Berserker),
            ("guardian", JobId.Guardian), ("einherjar", JobId.Einherjar), ("valkyrie", JobId.Valkyrie),
        };

        private static readonly int[] MaleHair = { 1, 7, 0, 1, 1, 2 };
        private static readonly int[] FemaleHair = { 3, 4, 2, 5, 2, 3 };
        private static readonly int[] MaleHairColor = { 2, 1, 0, 4, 2, 2 };
        private static readonly int[] FemaleHairColor = { 2, 3, 1, 0, 4, 5 };

        // Who carries each weapon in the weapon sheet: (job, weapon, gender, hair style, hair color).
        private static readonly (JobId Job, WeaponType Weapon, Gender Gender, int Hair, int HairColor)[] Armory =
        {
            (JobId.Scout, WeaponType.Dagger, Gender.Female, 3, 0), (JobId.Warrior, WeaponType.OneHandSword, Gender.Male, 1, 2),
            (JobId.Einherjar, WeaponType.TwoHandSword, Gender.Male, 1, 4), (JobId.Valkyrie, WeaponType.Spear, Gender.Female, 2, 5),
            (JobId.Devotee, WeaponType.Mace, Gender.Male, 0, 1), (JobId.Mystic, WeaponType.Staff, Gender.Female, 2, 6),
            (JobId.Huntsman, WeaponType.Bow, Gender.Male, 7, 3), (JobId.Monk, WeaponType.Knuckle, Gender.Male, 6, 0),
            (JobId.Assassin, WeaponType.Katar, Gender.Female, 4, 8), (JobId.Trader, WeaponType.Axe, Gender.Male, 7, 1),
            (JobId.Berserker, WeaponType.TwoHandAxe, Gender.Female, 2, 3), (JobId.Skald, WeaponType.Instrument, Gender.Male, 0, 2),
            (JobId.Seidkona, WeaponType.Whip, Gender.Female, 3, 3), (JobId.Sage, WeaponType.Book, Gender.Male, 0, 4),
            (JobId.Thunderer, WeaponType.ThunderRod, Gender.Female, 5, 0), (JobId.Nightraider, WeaponType.Huuma, Gender.Male, 1, 0),
            (JobId.FreyjasKin, WeaponType.CatStaff, Gender.Female, 4, 2),
        };

        // Attack filmstrips: one character per frame of a single swing (the hit lands at 0.5).
        private static readonly float[] StripTimes = { 0f, 0.18f, 0.3f, 0.42f, 0.5f, 0.64f, 0.85f };

        [MenuItem("Runeheir/Art/Render Character Lineup", priority = 50)]
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
                    Debug.Log("[Runeheir] Lineup written: " + file);
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
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetUpStage();
            var files = new List<string>();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputFolder));
            Directory.CreateDirectory(folder);

            // 1. Every outfit, men in front, women behind, relaxed.
            var lineup = new GameObject("Lineup").transform;
            for (int i = 0; i < Outfits.Length; i++)
            {
                float x = (i - (Outfits.Length - 1) * 0.5f) * 0.95f;
                Spawn(lineup, Look(i, Gender.Male), new Vector3(x, 0f, 0f), 180f, Rest());
                Spawn(lineup, Look(i, Gender.Female), new Vector3(x + 0.35f, 0f, 1.5f), 180f, Rest());
            }

            files.Add(Shoot(folder, "unity_lineup_front.png", new Vector3(0.75f, 1.35f, -6.3f), new Vector3(5f, -4f, 0f), 30f));
            files.Add(Shoot(folder, "unity_lineup_game.png", new Vector3(-5.6f, 8.6f, -5.0f), new Vector3(45f, 45f, 0f), 30f));
            UnityEngine.Object.DestroyImmediate(lineup.gameObject);

            // 1b. Worn cloaks from behind (each outfit's fitted cape in the item's color), mid-stride.
            var cloaks = new GameObject("Cloaks").transform;
            string[] garments = { "traveler_cloak", "wolfskin_mantle", "bear_pelt", "traveler_cloak", "valkyrian_manteau", "wolfskin_mantle" };
            for (int i = 0; i < Outfits.Length; i++)
            {
                var look = Look(i, i % 2 == 0 ? Gender.Male : Gender.Female);
                look.Garment = garments[i];
                var stride = Rest();
                stride.Speed = 5.5f;
                stride.StridePhase = 1.2f + i;
                Spawn(cloaks, look, new Vector3((i - (Outfits.Length - 1) * 0.5f) * 0.95f, 0f, 0f), 195f, stride);
            }

            files.Add(Shoot(folder, "unity_cloaks_back.png", new Vector3(-0.6f, 1.45f, 6.3f), new Vector3(6f, 184f, 0f), 30f));
            UnityEngine.Object.DestroyImmediate(cloaks.gameObject);

            // 2. Every weapon in hand, in the battle stance.
            var armory = new GameObject("Armory").transform;
            for (int i = 0; i < Armory.Length; i++)
            {
                int row = i < 9 ? 0 : 1;
                int col = row == 0 ? i : i - 9;
                float x = (col - 4f) * 0.95f + row * 0.45f;
                Spawn(armory, Armed(Armory[i]), new Vector3(x, 0f, row * 1.7f), 160f, Ready());
            }

            files.Add(Shoot(folder, "unity_weapons.png", new Vector3(0.6f, 1.5f, -6.9f), new Vector3(6f, -4f, 0f), 36f));
            UnityEngine.Object.DestroyImmediate(armory.gameObject);

            // 3. Attack filmstrips for the main styles, then the casting poses.
            Strip(folder, files, "unity_strip_sword.png", (JobId.Warrior, WeaponType.OneHandSword, Gender.Male, 1, 2), 0);
            Strip(folder, files, "unity_strip_sword_backhand.png", (JobId.Warrior, WeaponType.OneHandSword, Gender.Male, 1, 2), 1);
            Strip(folder, files, "unity_strip_greatsword.png", (JobId.Einherjar, WeaponType.TwoHandSword, Gender.Male, 1, 4), 0);
            Strip(folder, files, "unity_strip_spear.png", (JobId.Valkyrie, WeaponType.Spear, Gender.Female, 2, 5), 0);
            Strip(folder, files, "unity_strip_staff.png", (JobId.Mystic, WeaponType.Staff, Gender.Female, 2, 6), 0);
            Strip(folder, files, "unity_strip_bow.png", (JobId.Huntsman, WeaponType.Bow, Gender.Male, 7, 3), 0);

            var casters = new GameObject("Casters").transform;
            Spawn(casters, Armed((JobId.Mystic, WeaponType.Staff, Gender.Female, 2, 6)), new Vector3(-1.4f, 0f, 0f), 200f, Cast());
            Spawn(casters, Armed((JobId.Sage, WeaponType.Book, Gender.Male, 0, 4)), new Vector3(0f, 0f, 0f), 200f, Cast());
            Spawn(casters, Armed((JobId.Devotee, WeaponType.Mace, Gender.Female, 5, 2)), new Vector3(1.4f, 0f, 0f), 200f, Cast());
            files.Add(Shoot(folder, "unity_cast.png", new Vector3(0.2f, 1.45f, -4.6f), new Vector3(6f, 0f, 0f), 34f));
            UnityEngine.Object.DestroyImmediate(casters.gameObject);
            return files;
        }

        private static void Strip(string folder, List<string> files, string name, (JobId, WeaponType, Gender, int, int) who, int combo)
        {
            var strip = new GameObject("Strip").transform;
            for (int i = 0; i < StripTimes.Length; i++)
            {
                var m = Ready();
                m.AttackT = Mathf.Min(StripTimes[i], 0.999f);
                m.AttackIndex = combo;
                Spawn(strip, Armed(who), new Vector3((i - (StripTimes.Length - 1) * 0.5f) * 1.25f, 0f, 0f), 215f, m);
            }

            files.Add(Shoot(folder, name, new Vector3(0f, 1.6f, -7.6f), new Vector3(6f, 0f, 0f), 34f));
            UnityEngine.Object.DestroyImmediate(strip.gameObject);
        }

        private static AvatarLook Armed((JobId Job, WeaponType Weapon, Gender Gender, int Hair, int HairColor) who)
        {
            var job = JobDatabase.Get(who.Job);
            return new AvatarLook
            {
                Job = who.Job,
                Outfit = RuntimeMaterials.Hex(job.ColorHex),
                Skin = AvatarLook.DefaultSkin,
                Hair = AvatarLook.HairPalette[who.HairColor],
                HairStyle = who.Hair,
                Gender = who.Gender,
                Race = who.Job == JobId.FreyjasKin ? CharacterRace.Doram : CharacterRace.Human,
                Weapon = who.Weapon,
            };
        }

        private static AvatarLook Look(int index, Gender gender)
        {
            var (_, jobId) = Outfits[index];
            var job = JobDatabase.Get(jobId);
            bool female = gender == Gender.Female;
            return new AvatarLook
            {
                Job = jobId,
                Outfit = RuntimeMaterials.Hex(job.ColorHex),
                Skin = AvatarLook.DefaultSkin,
                Hair = AvatarLook.HairPalette[(female ? FemaleHairColor : MaleHairColor)[index]],
                HairStyle = (female ? FemaleHair : MaleHair)[index],
                Gender = gender,
                Weapon = job.StarterWeapon.Type,
            };
        }

        private static void Spawn(Transform parent, AvatarLook look, Vector3 position, float yaw, RigMotion pose)
        {
            var holder = new GameObject($"{look.Job} {look.Gender}").transform;
            holder.SetParent(parent, false);
            holder.localPosition = position;
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var avatar = PlaceholderAvatar.CreateHumanoid(holder, look);
            if (avatar.Rig == null)
            {
                throw new InvalidOperationException("No rigged body for " + look.Job + ": run Tools/Blender/build_characters.py.");
            }

            avatar.Rig.Animate(pose);
        }

        private static RigMotion Rest()
        {
            return new RigMotion { Time = 0.8f, AttackT = -1f, SkillT = -1f };
        }

        private static RigMotion Ready()
        {
            var m = Rest();
            m.Combat = 1f;
            return m;
        }

        private static RigMotion Cast()
        {
            var m = Ready();
            m.Cast = 1f;
            return m;
        }

        /// <summary>The fields' sun and sky (WorldBuilder.ApplyAtmosphere), on Vigrid Haven's paving.</summary>
        private static void SetUpStage()
        {
            var theme = WorldTheme.For(MapTheme.Town);
            WorldBuilder.ApplyAtmosphere(theme, null);
            RenderSettings.fog = false;

            // A new scene has no baked ambient probe: build the trilight one by hand.
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(theme.AmbientEquator);
            probe.AddDirectionalLight(Vector3.up, theme.AmbientSky - theme.AmbientEquator, 1f);
            probe.AddDirectionalLight(Vector3.down, theme.AmbientGround - theme.AmbientEquator, 1f);
            RenderSettings.ambientProbe = probe;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Lit(theme.ColorOf(CellTag.Paving, false), 0f);
        }

        private static string Shoot(string folder, string fileName, Vector3 position, Vector3 euler, float fov)
        {
            const int width = 2400;
            const int height = 1350;
            var go = new GameObject("LineupCamera");
            var camera = go.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            camera.fieldOfView = fov;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.66f, 0.71f, 0.8f);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            camera.targetTexture = target;
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            string path = Path.Combine(folder, fileName);
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(go);
            return path;
        }
    }
}
