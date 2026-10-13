using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Monsters;
using Runeheir.Skills;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// The procedurally animated avatar (walk, ASPD-timed weapon swing, cast pose, skill motions, hit shake, stagger, death).
    /// Humanoids wear the Blender-built rigged body for their job (<see cref="RiggedBody"/>, <see cref="OutfitCatalog"/>)
    /// and fall back to the primitive doll when those models aren't in the project; monsters are still primitives. Worn
    /// gear is built from primitives on either body. Swap in an Animator via <see cref="CharacterAnimationBridge"/>.
    /// </summary>
    public sealed class PlaceholderAvatar : MonoBehaviour
    {
        // Weapon arc in degrees around the shoulder's X axis (see PlayAttack).
        private const float RestAngle = 340f;
        private const float WindupAngle = 160f;
        private const float StrikeAngle = 300f;

        private readonly List<Transform> _wings = new List<Transform>();

        // Garment wings (Valkyrian Feather Wings...): slow glide flap, unlike a flyer monster's buzz.
        private readonly List<Transform> _gearWings = new List<Transform>();
        private Transform _model;
        private Transform _weaponPivot;
        private RiggedBody _rig;
        private int _attackIndex;
        private float _combatUntil = -100f;
        private float _combatBlend;
        private CastCircle _castCircle;

        // How long the battle stance (Ragnarok's standby) is held after the last swing, spell or blow.
        private const float CombatStanceSeconds = 5f;
        private bool _lungeAttack;
        private bool _flyer;
        private bool _squash;
        private float _speed;
        private float _bobPhase;
        private float _attackStart = -10f;
        private float _attackDuration = 0.6f;
        private float _hitUntil;
        private bool _casting;
        private float _castBlend;
        private bool _dead;
        private float _deadBlend;
        private SkillMotion _skillMotion;
        private float _skillStart = -10f;
        private float _skillDuration = 0.4f;
        private float _staggerUntil;

        public static PlaceholderAvatar CreateHumanoid(Transform parent, AvatarLook look)
        {
            var avatar = CreateRoot(parent);
            avatar.BuildHumanoid(look);
            return avatar;
        }

        public static PlaceholderAvatar CreateMonster(Transform parent, MonsterDefinition definition)
        {
            var avatar = CreateRoot(parent);
            avatar.BuildMonster(definition);
            return avatar;
        }

        public void RebuildHumanoid(AvatarLook look)
        {
            BuildHumanoid(look);
        }

        /// <summary>The Blender-built body, or null for the primitive doll and monsters.</summary>
        public RiggedBody Rig => _rig;

        public void SetLocomotion(float metersPerSecond)
        {
            _speed = metersPerSecond;
        }

        /// <summary>Plays one swing lasting <paramref name="duration"/> seconds (shorter at high ASPD).</summary>
        public void PlayAttack(float duration)
        {
            _attackStart = Time.time;
            _attackDuration = Mathf.Max(0.05f, duration);
            _attackIndex++;
            EnterCombat();
        }

        public void SetCasting(bool casting)
        {
            _casting = casting;
            if (casting)
            {
                EnterCombat();
            }
        }

        public void SetDead(bool dead)
        {
            _dead = dead;
        }

        public void PlayHit()
        {
            _hitUntil = Time.time + 0.15f;
            EnterCombat();
        }

        /// <summary>Take (or keep) the battle stance for a few seconds.</summary>
        public void EnterCombat()
        {
            _combatUntil = Time.time + CombatStanceSeconds;
        }

        /// <summary>Skill motion (spin, thrust, cast, shoot, punch, leap, buff) lasting <paramref name="duration"/> seconds.</summary>
        public void PlaySkill(SkillMotion motion, float duration)
        {
            if (motion == SkillMotion.Swing)
            {
                PlayAttack(duration);
                return;
            }

            _skillMotion = motion;
            _skillStart = Time.time;
            _skillDuration = Mathf.Max(0.1f, motion == SkillMotion.Spin || motion == SkillMotion.Leap ? duration * 1.4f : duration);
            EnterCombat();
        }

        /// <summary>Poise broken: rock back for the stagger.</summary>
        public void PlayStagger()
        {
            _staggerUntil = Time.time + Combat.PoiseRules.StaggerSeconds;
            EnterCombat();
        }

        private void OnDestroy()
        {
            if (_castCircle != null)
            {
                RiggedBody.Discard(_castCircle.gameObject);
            }
        }

        private static PlaceholderAvatar CreateRoot(Transform parent)
        {
            var root = new GameObject("PlaceholderAvatar");
            root.transform.SetParent(parent, false);
            return root.AddComponent<PlaceholderAvatar>();
        }

        private float _baseScale = 1f;

        private void ResetModel()
        {
            _baseScale = 1f;
            if (_model != null)
            {
                RiggedBody.Discard(_model.gameObject);
            }

            _wings.Clear();
            _gearWings.Clear();
            _squash = false;
            _weaponPivot = null;
            _rig = null;
            _model = new GameObject("Model").transform;
            _model.SetParent(transform, false);
        }

        // ------------------------------------------------------------------ humanoid
        private void BuildHumanoid(AvatarLook look)
        {
            ResetModel();
            _lungeAttack = false;
            _flyer = false;

            _rig = RiggedBody.Create(_model, look);
            BuildMount(look.Mount);
            if (_rig != null)
            {
                // The Blender-built weapon in the fist; the primitive one only when a type has no model.
                if (!_rig.AttachWeapon(look.Weapon) && look.Weapon != WeaponType.Unarmed)
                {
                    _weaponPivot = _rig.CreateWeaponPivot(look.Weapon);
                    BuildWeapon(look.Weapon);
                }

                if (look.Race == CharacterRace.Doram)
                {
                    BuildCatFolk(look.Hair, _rig.HeadUpper, _rig.Tail);
                }

                BuildGear(look, _rig.HeadUpper, _rig.HeadMid, _rig.HeadLower, _rig.ShieldArm, _rig.Back, _rig.NeckRing);
                return;
            }

            bool female = look.Gender == Gender.Female;
            Color dark = look.Outfit * 0.55f;
            dark.a = 1f;
            Color boots = new Color(0.22f, 0.16f, 0.12f);

            Part(PrimitiveType.Cylinder, _model, "LegL", new Vector3(-0.12f, 0.22f, 0f), new Vector3(0.16f, 0.22f, 0.16f), boots);
            Part(PrimitiveType.Cylinder, _model, "LegR", new Vector3(0.12f, 0.22f, 0f), new Vector3(0.16f, 0.22f, 0.16f), boots);
            Part(PrimitiveType.Capsule, _model, "Torso", new Vector3(0f, 0.86f, 0f), new Vector3(female ? 0.44f : 0.5f, 0.45f, 0.36f), look.Outfit);
            if (female)
            {
                Part(PrimitiveType.Cylinder, _model, "Skirt", new Vector3(0f, 0.5f, 0f), new Vector3(0.52f, 0.12f, 0.46f), dark);
            }

            Part(PrimitiveType.Cylinder, _model, "Belt", new Vector3(0f, 0.68f, 0f), new Vector3(0.48f, 0.035f, 0.38f), new Color(0.45f, 0.32f, 0.15f));
            Part(PrimitiveType.Capsule, _model, "ArmL", new Vector3(-0.32f, 0.98f, 0f), new Vector3(0.13f, 0.28f, 0.13f), dark, new Vector3(0f, 0f, -8f));
            Part(PrimitiveType.Sphere, _model, "Head", new Vector3(0f, 1.52f, 0f), Vector3.one * 0.42f, look.Skin);
            Part(PrimitiveType.Sphere, _model, "EyeL", new Vector3(-0.08f, 1.55f, 0.18f), Vector3.one * 0.07f, new Color(0.12f, 0.12f, 0.18f));
            Part(PrimitiveType.Sphere, _model, "EyeR", new Vector3(0.08f, 1.55f, 0.18f), Vector3.one * 0.07f, new Color(0.12f, 0.12f, 0.18f));
            BuildHair(look.HairStyle, look.Hair);
            if (look.Race == CharacterRace.Doram)
            {
                BuildCatFolk(look.Hair, _model, _model);
            }

            _weaponPivot = new GameObject("WeaponPivot").transform;
            _weaponPivot.SetParent(_model, false);
            _weaponPivot.localPosition = new Vector3(0.34f, 1.12f, 0.05f);
            Part(PrimitiveType.Capsule, _weaponPivot, "ArmR", new Vector3(0f, -0.22f, 0f), new Vector3(0.13f, 0.28f, 0.13f), dark);
            BuildWeapon(look.Weapon);
            _weaponPivot.localRotation = Quaternion.Euler(RestAngle, 0f, 0f);
            BuildGear(look, _model, _model, _model, _model, _model, _model);
        }

        // ------------------------------------------------------------------ worn gear (GDD §5 visible headgear, shields, wings)
        // Every piece is laid out in the primitive doll's coordinates under the given parent: the doll's Model itself, or a
        // RiggedBody socket that maps those coordinates onto the rigged body (head, forearm, back, neck).
        private void BuildGear(AvatarLook look, Transform upper, Transform mid, Transform lower, Transform shield, Transform back, Transform neck)
        {
            BuildUpperHeadgear(ItemCatalog.Get(look.HeadUpper), upper);
            BuildMidHeadgear(ItemCatalog.Get(look.HeadMid), mid);
            BuildLowerHeadgear(ItemCatalog.Get(look.HeadLower), lower);
            BuildShield(ItemCatalog.Get(look.Shield), shield);
            BuildGarment(ItemCatalog.Get(look.Garment), back, neck);
            BuildAura(ItemCatalog.Get(look.Aura));
        }

        // ------------------------------------------------------------------ mounts (Phase 8)
        private Transform _mount;
        private float _mountHeight;
        private readonly List<Transform> _mountLegs = new List<Transform>();

        /// <summary>The steed under a rider: the rider sits higher, the mount walks with the rider's speed.</summary>
        private void BuildMount(string mount)
        {
            _mountLegs.Clear();
            _mountHeight = 0f;
            if (_mount != null)
            {
                Destroy(_mount.gameObject);
                _mount = null;
            }

            if (string.IsNullOrEmpty(mount))
            {
                return;
            }

            bool boar = mount == MountRules.Boar;
            bool foal = mount == MountRules.Warhorse;
            Color hide = boar ? new Color(0.86f, 0.66f, 0.22f) : foal ? new Color(0.78f, 0.80f, 0.84f) : new Color(0.62f, 0.46f, 0.30f);
            Color dark = hide * 0.6f;
            dark.a = 1f;
            _mount = new GameObject("Mount").transform;
            _mount.SetParent(transform, false);
            float height = boar ? 0.52f : 0.6f;
            Part(PrimitiveType.Capsule, _mount, "Body", new Vector3(0f, boar ? 0.72f : 0.95f, 0f), new Vector3(boar ? 0.75f : 0.6f, boar ? 0.75f : 0.85f, boar ? 0.75f : 0.6f), hide,
                new Vector3(90f, 0f, 0f));
            Part(PrimitiveType.Capsule, _mount, "Neck", new Vector3(0f, boar ? 0.85f : 1.3f, boar ? 0.6f : 0.62f), new Vector3(0.28f, 0.34f, 0.28f), hide,
                new Vector3(boar ? 70f : 35f, 0f, 0f));
            Part(PrimitiveType.Capsule, _mount, "Head", new Vector3(0f, boar ? 0.72f : 1.55f, boar ? 0.95f : 0.92f), new Vector3(0.24f, 0.3f, 0.24f), hide,
                new Vector3(boar ? 100f : 75f, 0f, 0f));
            if (boar)
            {
                Part(PrimitiveType.Capsule, _mount, "TuskL", new Vector3(-0.1f, 0.62f, 1.18f), new Vector3(0.04f, 0.1f, 0.04f), new Color(0.96f, 0.92f, 0.8f), new Vector3(-40f, 0f, 20f));
                Part(PrimitiveType.Capsule, _mount, "TuskR", new Vector3(0.1f, 0.62f, 1.18f), new Vector3(0.04f, 0.1f, 0.04f), new Color(0.96f, 0.92f, 0.8f), new Vector3(-40f, 0f, -20f));
                Part(PrimitiveType.Cube, _mount, "Bristles", new Vector3(0f, 1.1f, 0f), new Vector3(0.08f, 0.12f, 0.9f), new Color(1f, 0.85f, 0.35f));
            }
            else
            {
                Part(PrimitiveType.Cube, _mount, "Mane", new Vector3(0f, 1.48f, 0.5f), new Vector3(0.06f, 0.35f, 0.3f), dark, new Vector3(35f, 0f, 0f));
                Part(PrimitiveType.Capsule, _mount, "Tail", new Vector3(0f, 0.95f, -0.85f), new Vector3(0.1f, 0.3f, 0.1f), dark, new Vector3(-30f, 0f, 0f));
                Part(PrimitiveType.Cube, _mount, "Saddle", new Vector3(0f, 1.27f, -0.05f), new Vector3(0.48f, 0.08f, 0.55f), new Color(0.45f, 0.18f, 0.12f));
            }

            // Sleipnir's foal has eight legs, like its sire.
            int pairs = foal ? 4 : 2;
            for (int i = 0; i < pairs; i++)
            {
                float z = Mathf.Lerp(0.45f, -0.45f, pairs == 1 ? 0.5f : i / (float)(pairs - 1));
                foreach (float x in new[] { -0.17f, 0.17f })
                {
                    var hip = new GameObject("Leg").transform;
                    hip.SetParent(_mount, false);
                    hip.localPosition = new Vector3(x, boar ? 0.6f : 0.8f, z);
                    Part(PrimitiveType.Capsule, hip, "Shank", new Vector3(0f, boar ? -0.28f : -0.38f, 0f), new Vector3(0.12f, boar ? 0.3f : 0.4f, 0.12f), dark);
                    _mountLegs.Add(hip);
                }
            }

            // The rider sits in the saddle.
            _mountHeight = height;
        }

        private void AnimateMount()
        {
            if (_mount == null)
            {
                return;
            }

            float swing = _speed > 0.1f ? Mathf.Sin(Time.time * 9f) * 28f : 0f;
            for (int i = 0; i < _mountLegs.Count; i++)
            {
                _mountLegs[i].localRotation = Quaternion.Euler(((i / 2) % 2 == 0 ^ i % 2 == 0 ? swing : -swing), 0f, 0f);
            }

            _mount.localPosition = new Vector3(0f, _speed > 0.1f ? Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.05f : 0f, 0f);
        }

        /// <summary>The template a piece is drawn with: its costume look, else its own id.</summary>
        private static string LookOf(ItemDefinition item)
        {
            return item.LookAs ?? item.Id;
        }

        /// <summary>Phase 8.4 costume auras: a glowing ring at the feet with motes circling it.</summary>
        private void BuildAura(ItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            Color color = ViewColor(item);
            var aura = new GameObject("Aura").transform;
            aura.SetParent(_model, false);
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "AuraRing";
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(aura, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            ring.transform.localScale = new Vector3(1.3f, 0.005f, 1.3f);
            var renderer = ring.GetComponent<Renderer>();
            renderer.sharedMaterial = RuntimeMaterials.Translucent(new Color(color.r, color.g, color.b, 0.35f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var orbit = new GameObject("Motes").transform;
            orbit.SetParent(aura, false);
            orbit.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 70f, 0f);
            bool rising = (item.LookAs ?? string.Empty).Contains("rise");
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                var mote = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mote.name = "Mote";
                Destroy(mote.GetComponent<Collider>());
                mote.transform.SetParent(orbit, false);
                mote.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.6f, rising ? 0.3f + i * 0.28f : 0.25f + (i % 2) * 0.5f, Mathf.Sin(angle) * 0.6f);
                mote.transform.localScale = Vector3.one * 0.07f;
                var moteRenderer = mote.GetComponent<Renderer>();
                moteRenderer.sharedMaterial = RuntimeMaterials.Glow(color, color * 1.6f);
                moteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static Color ViewColor(ItemDefinition item)
        {
            return RuntimeMaterials.Hex(item.ViewColorHex ?? item.IconColorHex ?? "#BDC3C7");
        }

        private void BuildUpperHeadgear(ItemDefinition item, Transform parent)
        {
            if (item == null)
            {
                return;
            }

            Color color = ViewColor(item);
            Color trim = Color.Lerp(color, Color.white, 0.4f);
            string id = LookOf(item);
            if (BuildCostumeHead(id, parent, color, trim))
            {
                return;
            }

            if (id.Contains("wizard_hat"))
            {
                Part(PrimitiveType.Cylinder, parent, "HatBrim", new Vector3(0f, 1.7f, 0f), new Vector3(0.7f, 0.02f, 0.7f), color);
                Part(PrimitiveType.Capsule, parent, "HatCone", new Vector3(0f, 1.98f, -0.04f), new Vector3(0.3f, 0.3f, 0.3f), color, new Vector3(-12f, 0f, 0f));
                Part(PrimitiveType.Sphere, parent, "HatTip", new Vector3(0f, 2.22f, -0.12f), Vector3.one * 0.1f, new Color(0.96f, 0.82f, 0.25f));
            }
            else if (id.Contains("crown") || id.Contains("circlet"))
            {
                Part(PrimitiveType.Cylinder, parent, "Band", new Vector3(0f, 1.7f, 0f), new Vector3(0.47f, 0.035f, 0.47f), color);
                if (id.Contains("antler"))
                {
                    Part(PrimitiveType.Cylinder, parent, "AntlerL", new Vector3(-0.2f, 1.9f, 0f), new Vector3(0.04f, 0.2f, 0.04f), color, new Vector3(0f, 0f, 30f));
                    Part(PrimitiveType.Cylinder, parent, "AntlerR", new Vector3(0.2f, 1.9f, 0f), new Vector3(0.04f, 0.2f, 0.04f), color, new Vector3(0f, 0f, -30f));
                }
                else
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        Part(PrimitiveType.Cube, parent, "Point", new Vector3(i * 0.12f, 1.77f, 0.2f), new Vector3(0.05f, 0.1f, 0.03f), trim, new Vector3(0f, 0f, 45f));
                    }
                }
            }
            else if (id.Contains("hood"))
            {
                Part(PrimitiveType.Sphere, parent, "Hood", new Vector3(0f, 1.6f, -0.04f), new Vector3(0.52f, 0.46f, 0.52f), color);
                if (id.Contains("wolf"))
                {
                    Part(PrimitiveType.Cube, parent, "EarL", new Vector3(-0.14f, 1.86f, 0f), new Vector3(0.08f, 0.14f, 0.05f), color);
                    Part(PrimitiveType.Cube, parent, "EarR", new Vector3(0.14f, 1.86f, 0f), new Vector3(0.08f, 0.14f, 0.05f), color);
                }
            }
            else if (id.Contains("beret") || id.Contains("cap") || id.Contains("bandana"))
            {
                Part(PrimitiveType.Sphere, parent, "Cap", new Vector3(0.03f, 1.74f, 0f), new Vector3(0.5f, 0.16f, 0.48f), color, new Vector3(0f, 0f, -8f));
                if (id.Contains("feather"))
                {
                    Part(PrimitiveType.Cube, parent, "Feather", new Vector3(-0.18f, 1.86f, -0.05f), new Vector3(0.03f, 0.22f, 0.06f), Color.white, new Vector3(-20f, 0f, 25f));
                }
            }
            else
            {
                // Helms and crests.
                Part(PrimitiveType.Sphere, parent, "Helm", new Vector3(0f, 1.63f, 0f), new Vector3(0.48f, 0.36f, 0.48f), color);
                if (id.Contains("horned") || id.Contains("viking") || id.Contains("awe"))
                {
                    Part(PrimitiveType.Capsule, parent, "HornL", new Vector3(-0.27f, 1.78f, 0f), new Vector3(0.07f, 0.16f, 0.07f), trim, new Vector3(0f, 0f, 40f));
                    Part(PrimitiveType.Capsule, parent, "HornR", new Vector3(0.27f, 1.78f, 0f), new Vector3(0.07f, 0.16f, 0.07f), trim, new Vector3(0f, 0f, -40f));
                }
                else if (id.Contains("winged"))
                {
                    Part(PrimitiveType.Cube, parent, "WingletL", new Vector3(-0.27f, 1.7f, -0.05f), new Vector3(0.04f, 0.16f, 0.24f), trim, new Vector3(-25f, 0f, 15f));
                    Part(PrimitiveType.Cube, parent, "WingletR", new Vector3(0.27f, 1.7f, -0.05f), new Vector3(0.04f, 0.16f, 0.24f), trim, new Vector3(-25f, 0f, -15f));
                }

                if ((item.Slots & EquipSlot.HeadMid) != 0)
                {
                    Part(PrimitiveType.Cube, parent, "NasalGuard", new Vector3(0f, 1.55f, 0.21f), new Vector3(0.04f, 0.14f, 0.03f), color);
                }
            }
        }

        private void BuildMidHeadgear(ItemDefinition item, Transform parent)
        {
            if (item == null)
            {
                return;
            }

            Color color = ViewColor(item);
            string id = LookOf(item);
            if (id.Contains("head_wings"))
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Part(PrimitiveType.Cube, parent, "HeadWing", new Vector3(side * 0.26f, 1.62f, -0.04f), new Vector3(0.03f, 0.14f, 0.26f), color, new Vector3(-30f, 0f, side * -20f));
                }
            }
            else if (id.Contains("mask_eyes"))
            {
                Part(PrimitiveType.Cube, parent, "Domino", new Vector3(0f, 1.56f, 0.2f), new Vector3(0.36f, 0.1f, 0.04f), color);
            }
            else if (id.Contains("eyepatch"))
            {
                Part(PrimitiveType.Sphere, parent, "Patch", new Vector3(0.08f, 1.55f, 0.2f), new Vector3(0.1f, 0.09f, 0.04f), color);
            }
            else if (id.Contains("monocle"))
            {
                Part(PrimitiveType.Cylinder, parent, "Monocle", new Vector3(-0.08f, 1.55f, 0.21f), new Vector3(0.1f, 0.01f, 0.1f), color, new Vector3(90f, 0f, 0f));
            }
            else
            {
                Part(PrimitiveType.Cube, parent, "Eyewear", new Vector3(0f, 1.56f, 0.2f), new Vector3(0.34f, 0.07f, 0.04f), color);
            }
        }

        private void BuildLowerHeadgear(ItemDefinition item, Transform parent)
        {
            if (item == null)
            {
                return;
            }

            Color color = ViewColor(item);
            string id = LookOf(item);
            if (id.Contains("scarf"))
            {
                Part(PrimitiveType.Cylinder, parent, "FaceScarf", new Vector3(0f, 1.4f, 0.04f), new Vector3(0.44f, 0.08f, 0.42f), color);
            }
            else if (id.Contains("pipe"))
            {
                Part(PrimitiveType.Cylinder, parent, "Pipe", new Vector3(0.06f, 1.42f, 0.27f), new Vector3(0.03f, 0.08f, 0.03f), color, new Vector3(80f, 0f, 0f));
                Part(PrimitiveType.Cylinder, parent, "Bowl", new Vector3(0.06f, 1.44f, 0.36f), new Vector3(0.07f, 0.05f, 0.07f), color);
            }
            else if (id.Contains("beard"))
            {
                Part(PrimitiveType.Capsule, parent, "Beard", new Vector3(0f, 1.32f, 0.14f), new Vector3(0.22f, 0.16f, 0.12f), color);
            }
            else
            {
                Part(PrimitiveType.Cube, parent, "Mask", new Vector3(0f, 1.43f, 0.19f), new Vector3(0.26f, 0.1f, 0.05f), color);
            }
        }

        private void BuildShield(ItemDefinition item, Transform parent)
        {
            if (item == null)
            {
                return;
            }

            Color color = ViewColor(item);
            if (item.Id.Contains("kite"))
            {
                Part(PrimitiveType.Cube, parent, "Shield", new Vector3(-0.44f, 0.92f, 0.08f), new Vector3(0.04f, 0.5f, 0.32f), color);
            }
            else
            {
                Part(PrimitiveType.Cylinder, parent, "Shield", new Vector3(-0.44f, 0.95f, 0.08f), new Vector3(0.44f, 0.025f, 0.44f), color, new Vector3(0f, 0f, 90f));
                Part(PrimitiveType.Sphere, parent, "Boss", new Vector3(-0.47f, 0.95f, 0.08f), Vector3.one * 0.1f, Color.Lerp(color, Color.white, 0.5f));
            }
        }

        private void BuildGarment(ItemDefinition item, Transform parent, Transform neck)
        {
            // On the rigged body a cloak is the outfit's fitted GarmentCape (RiggedBody), not a board on the back.
            if (item == null || (_rig != null && OutfitCatalog.IsCloak(item.Id)))
            {
                return;
            }

            Color color = ViewColor(item);
            string look = LookOf(item);
            if (look.Contains("tail"))
            {
                Part(PrimitiveType.Capsule, parent, "Tail", new Vector3(0f, 0.62f, -0.32f), new Vector3(0.14f, 0.32f, 0.14f), color, new Vector3(-55f, 0f, 0f));
                Part(PrimitiveType.Sphere, parent, "TailTip", new Vector3(0f, 0.86f, -0.6f), Vector3.one * 0.15f, Color.Lerp(color, Color.white, 0.6f));
            }
            else if (look.Contains("wings_bat"))
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var pivot = new GameObject(side < 0 ? "WingPivotL" : "WingPivotR").transform;
                    pivot.SetParent(parent, false);
                    pivot.localPosition = new Vector3(side * 0.1f, 1.2f, -0.22f);
                    Part(PrimitiveType.Cube, pivot, "Wing", new Vector3(side * 0.3f, 0.12f, 0f), new Vector3(0.5f, 0.12f, 0.03f), color * 0.8f, new Vector3(0f, 0f, side * 25f));
                    for (int f = 0; f < 3; f++)
                    {
                        Part(PrimitiveType.Cube, pivot, "Web", new Vector3(side * (0.18f + f * 0.16f), -0.05f, 0f), new Vector3(0.15f, 0.3f, 0.02f), color, new Vector3(0f, 0f, side * (5f + f * 12f)));
                    }

                    _gearWings.Add(pivot);
                }
            }
            else if (look.Contains("wings_fairy"))
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var pivot = new GameObject(side < 0 ? "WingPivotL" : "WingPivotR").transform;
                    pivot.SetParent(parent, false);
                    pivot.localPosition = new Vector3(side * 0.08f, 1.12f, -0.22f);
                    var upper = Part(PrimitiveType.Sphere, pivot, "WingUpper", new Vector3(side * 0.26f, 0.16f, 0f), new Vector3(0.42f, 0.3f, 0.02f), color, new Vector3(0f, 0f, side * 20f));
                    upper.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Translucent(new Color(color.r, color.g, color.b, 0.6f));
                    var lowerWing = Part(PrimitiveType.Sphere, pivot, "WingLower", new Vector3(side * 0.2f, -0.12f, 0f), new Vector3(0.28f, 0.22f, 0.02f), color, new Vector3(0f, 0f, side * -25f));
                    lowerWing.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Translucent(new Color(color.r, color.g, color.b, 0.6f));
                    _gearWings.Add(pivot);
                }
            }
            else if (look.Contains("wings"))
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var pivot = new GameObject(side < 0 ? "WingPivotL" : "WingPivotR").transform;
                    pivot.SetParent(parent, false);
                    pivot.localPosition = new Vector3(side * 0.1f, 1.18f, -0.22f);
                    Part(PrimitiveType.Cube, pivot, "Wing", new Vector3(side * 0.36f, 0.1f, 0f), new Vector3(0.62f, 0.3f, 0.03f), color, new Vector3(0f, 0f, side * 18f));
                    Part(PrimitiveType.Cube, pivot, "WingTip", new Vector3(side * 0.62f, 0.28f, 0f), new Vector3(0.24f, 0.18f, 0.03f), Color.Lerp(color, Color.white, 0.3f), new Vector3(0f, 0f, side * 40f));
                    _gearWings.Add(pivot);
                }
            }
            else if (look.Contains("muffler"))
            {
                Part(PrimitiveType.Cylinder, neck, "Scarf", new Vector3(0f, 1.28f, 0f), new Vector3(0.4f, 0.06f, 0.38f), color);
                Part(PrimitiveType.Cube, neck, "ScarfTail", new Vector3(0.1f, 1.08f, -0.2f), new Vector3(0.1f, 0.34f, 0.03f), color, new Vector3(10f, 0f, 8f));
            }
            else
            {
                Part(PrimitiveType.Cube, parent, "Cape", new Vector3(0f, 0.86f, -0.23f), new Vector3(0.52f, 0.86f, 0.04f), color, new Vector3(8f, 0f, 0f));
            }
        }

        /// <summary>Phase 8.4 costume headwear: ears, halos, flowers, ribbons, top hats, horns, headphones. False when not one of these.</summary>
        private bool BuildCostumeHead(string id, Transform parent, Color color, Color trim)
        {
            if (id.Contains("ears_bunny"))
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Part(PrimitiveType.Capsule, parent, "Ear", new Vector3(side * 0.1f, 1.98f, -0.02f), new Vector3(0.08f, 0.2f, 0.04f), color, new Vector3(0f, 0f, side * -10f));
                    Part(PrimitiveType.Capsule, parent, "EarInner", new Vector3(side * 0.1f, 1.98f, 0f), new Vector3(0.045f, 0.15f, 0.02f), trim, new Vector3(0f, 0f, side * -10f));
                }

                return true;
            }

            if (id.Contains("ears_cat") || id.Contains("ears_fox"))
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Part(PrimitiveType.Cube, parent, "Ear", new Vector3(side * 0.15f, 1.83f, 0f), new Vector3(0.1f, 0.13f, 0.04f), color, new Vector3(0f, 0f, 45f));
                }

                return true;
            }

            if (id.Contains("halo"))
            {
                var halo = Part(PrimitiveType.Cylinder, parent, "Halo", new Vector3(0f, 2.02f, 0f), new Vector3(0.36f, 0.012f, 0.36f), color);
                halo.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Glow(color, color * 1.5f);
                return true;
            }

            if (id.Contains("flower"))
            {
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * Mathf.PI * 2f / 5f;
                    Part(PrimitiveType.Sphere, parent, "Petal", new Vector3(0.17f + Mathf.Cos(angle) * 0.05f, 1.76f + Mathf.Sin(angle) * 0.05f, 0.08f), Vector3.one * 0.06f, color);
                }

                Part(PrimitiveType.Sphere, parent, "Heart", new Vector3(0.17f, 1.76f, 0.1f), Vector3.one * 0.04f, new Color(0.98f, 0.85f, 0.3f));
                return true;
            }

            if (id.Contains("ribbon"))
            {
                Part(PrimitiveType.Cube, parent, "BowL", new Vector3(-0.07f, 1.86f, -0.08f), new Vector3(0.12f, 0.08f, 0.03f), color, new Vector3(0f, 0f, 20f));
                Part(PrimitiveType.Cube, parent, "BowR", new Vector3(0.07f, 1.86f, -0.08f), new Vector3(0.12f, 0.08f, 0.03f), color, new Vector3(0f, 0f, -20f));
                Part(PrimitiveType.Sphere, parent, "Knot", new Vector3(0f, 1.86f, -0.08f), Vector3.one * 0.05f, trim);
                return true;
            }

            if (id.Contains("tophat"))
            {
                Part(PrimitiveType.Cylinder, parent, "Brim", new Vector3(0f, 1.74f, 0f), new Vector3(0.56f, 0.015f, 0.56f), color);
                Part(PrimitiveType.Cylinder, parent, "Crown", new Vector3(0f, 1.9f, 0f), new Vector3(0.36f, 0.16f, 0.36f), color);
                Part(PrimitiveType.Cylinder, parent, "Band", new Vector3(0f, 1.78f, 0f), new Vector3(0.37f, 0.025f, 0.37f), trim);
                return true;
            }

            if (id.Contains("horns"))
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Part(PrimitiveType.Capsule, parent, "Horn", new Vector3(side * 0.17f, 1.84f, 0f), new Vector3(0.06f, 0.14f, 0.06f), color, new Vector3(0f, 0f, side * -30f));
                }

                return true;
            }

            if (id.Contains("headphones"))
            {
                Part(PrimitiveType.Cube, parent, "Band", new Vector3(0f, 1.8f, 0f), new Vector3(0.5f, 0.04f, 0.06f), trim);
                for (int side = -1; side <= 1; side += 2)
                {
                    Part(PrimitiveType.Cylinder, parent, "Cup", new Vector3(side * 0.24f, 1.58f, 0f), new Vector3(0.14f, 0.04f, 0.14f), color, new Vector3(0f, 0f, 90f));
                }

                return true;
            }

            return false;
        }

        private void BuildHair(int style, Color hair)
        {
            var cap = Part(PrimitiveType.Sphere, _model, "HairCap", new Vector3(0f, 1.62f, -0.02f), new Vector3(0.46f, 0.32f, 0.46f), hair);
            switch (style)
            {
                case 1: // Spiky
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = -60f + i * 30f;
                        Part(PrimitiveType.Cube, _model, "Spike", new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 0.14f, 1.8f, -0.04f),
                            new Vector3(0.07f, 0.22f, 0.07f), hair, new Vector3(-15f, 0f, -angle * 0.6f));
                    }

                    break;
                case 2: // Long
                    Part(PrimitiveType.Capsule, _model, "HairBack", new Vector3(0f, 1.32f, -0.15f), new Vector3(0.42f, 0.34f, 0.16f), hair);
                    break;
                case 3: // Ponytail
                    Part(PrimitiveType.Capsule, _model, "Ponytail", new Vector3(0f, 1.42f, -0.27f), new Vector3(0.12f, 0.24f, 0.12f), hair, new Vector3(30f, 0f, 0f));
                    break;
                case 4: // Twin tails
                    Part(PrimitiveType.Capsule, _model, "TailL", new Vector3(-0.25f, 1.38f, -0.05f), new Vector3(0.11f, 0.24f, 0.11f), hair, new Vector3(0f, 0f, -12f));
                    Part(PrimitiveType.Capsule, _model, "TailR", new Vector3(0.25f, 1.38f, -0.05f), new Vector3(0.11f, 0.24f, 0.11f), hair, new Vector3(0f, 0f, 12f));
                    break;
                case 5: // Bun
                    Part(PrimitiveType.Sphere, _model, "Bun", new Vector3(0f, 1.84f, -0.07f), Vector3.one * 0.2f, hair);
                    break;
                case 6: // Mohawk
                    cap.localScale = new Vector3(0.44f, 0.22f, 0.44f);
                    Part(PrimitiveType.Cube, _model, "Mohawk", new Vector3(0f, 1.78f, -0.02f), new Vector3(0.08f, 0.2f, 0.46f), hair);
                    break;
                case 7: // Viking braids
                    Part(PrimitiveType.Capsule, _model, "BraidL", new Vector3(-0.17f, 1.3f, 0.1f), new Vector3(0.07f, 0.2f, 0.07f), hair);
                    Part(PrimitiveType.Capsule, _model, "BraidR", new Vector3(0.17f, 1.3f, 0.1f), new Vector3(0.07f, 0.2f, 0.07f), hair);
                    break;
            }
        }

        /// <summary>Freyja's Kin (Ragnarok's Doram): a head shorter, pointed cat ears and a long tail in the hair color.</summary>
        private void BuildCatFolk(Color fur, Transform head, Transform tail)
        {
            _baseScale = 0.85f;
            Color inner = Color.Lerp(fur, new Color(1f, 0.75f, 0.75f), 0.5f);
            foreach (float side in new[] { -1f, 1f })
            {
                Part(PrimitiveType.Cube, head, "Ear", new Vector3(side * 0.14f, 1.82f, -0.02f), new Vector3(0.13f, 0.17f, 0.05f), fur, new Vector3(0f, 0f, side * -18f + 45f));
                Part(PrimitiveType.Cube, head, "EarInner", new Vector3(side * 0.14f, 1.81f, 0.01f), new Vector3(0.07f, 0.1f, 0.02f), inner, new Vector3(0f, 0f, side * -18f + 45f));
            }

            Part(PrimitiveType.Capsule, tail, "Tail", new Vector3(0f, 0.6f, -0.32f), new Vector3(0.09f, 0.24f, 0.09f), fur, new Vector3(-55f, 0f, 0f));
            Part(PrimitiveType.Capsule, tail, "TailTip", new Vector3(0f, 0.86f, -0.52f), new Vector3(0.09f, 0.18f, 0.09f), fur, new Vector3(-15f, 0f, 0f));
        }

        private void BuildWeapon(WeaponType weapon)
        {
            Color steel = new Color(0.78f, 0.8f, 0.85f);
            Color wood = new Color(0.45f, 0.3f, 0.16f);
            switch (weapon)
            {
                case WeaponType.Axe:
                    Part(PrimitiveType.Cylinder, _weaponPivot, "Haft", new Vector3(0f, -0.62f, 0f), new Vector3(0.05f, 0.36f, 0.05f), wood);
                    Part(PrimitiveType.Cube, _weaponPivot, "AxeHead", new Vector3(0.09f, -0.9f, 0f), new Vector3(0.2f, 0.18f, 0.03f), steel);
                    break;
                case WeaponType.TwoHandAxe:
                    Part(PrimitiveType.Cylinder, _weaponPivot, "Haft", new Vector3(0f, -0.8f, 0f), new Vector3(0.06f, 0.6f, 0.06f), wood);
                    Part(PrimitiveType.Cube, _weaponPivot, "AxeHead", new Vector3(0.14f, -1.3f, 0f), new Vector3(0.32f, 0.3f, 0.04f), steel);
                    Part(PrimitiveType.Cube, _weaponPivot, "Beard", new Vector3(-0.08f, -1.3f, 0f), new Vector3(0.12f, 0.14f, 0.04f), steel);
                    break;
                case WeaponType.Instrument:
                    Part(PrimitiveType.Cube, _weaponPivot, "Soundbox", new Vector3(0f, -0.62f, 0.05f), new Vector3(0.24f, 0.3f, 0.06f), wood);
                    Part(PrimitiveType.Cylinder, _weaponPivot, "ArmL", new Vector3(-0.1f, -0.88f, 0.05f), new Vector3(0.03f, 0.14f, 0.03f), wood);
                    Part(PrimitiveType.Cylinder, _weaponPivot, "ArmR", new Vector3(0.1f, -0.88f, 0.05f), new Vector3(0.03f, 0.14f, 0.03f), wood);
                    Part(PrimitiveType.Cube, _weaponPivot, "Strings", new Vector3(0f, -0.82f, 0.08f), new Vector3(0.16f, 0.28f, 0.005f), new Color(0.95f, 0.9f, 0.7f));
                    break;
                case WeaponType.Whip:
                    for (int i = 0; i < 4; i++)
                    {
                        Part(PrimitiveType.Capsule, _weaponPivot, "Lash", new Vector3(0.04f * i, -0.55f - i * 0.22f, 0f), new Vector3(0.04f, 0.13f, 0.04f),
                            i == 0 ? wood : new Color(0.35f, 0.22f, 0.15f), new Vector3(0f, 0f, i * 8f));
                    }

                    break;
                case WeaponType.Book:
                    Part(PrimitiveType.Cube, _weaponPivot, "Tome", new Vector3(0f, -0.55f, 0.08f), new Vector3(0.24f, 0.3f, 0.08f), new Color(0.45f, 0.18f, 0.12f));
                    Part(PrimitiveType.Cube, _weaponPivot, "Pages", new Vector3(0.01f, -0.55f, 0.08f), new Vector3(0.22f, 0.28f, 0.06f), new Color(0.95f, 0.92f, 0.8f));
                    break;
                case WeaponType.ThunderRod:
                    Part(PrimitiveType.Cylinder, _weaponPivot, "Barrel", new Vector3(0f, -0.85f, 0.05f), new Vector3(0.05f, 0.45f, 0.05f), steel);
                    Part(PrimitiveType.Cube, _weaponPivot, "Stock", new Vector3(0f, -0.48f, 0.05f), new Vector3(0.08f, 0.22f, 0.1f), wood);
                    Part(PrimitiveType.Sphere, _weaponPivot, "Spark", new Vector3(0f, -1.32f, 0.05f), Vector3.one * 0.08f, new Color(0.6f, 0.85f, 1f));
                    break;
                case WeaponType.Huuma:
                    Part(PrimitiveType.Cylinder, _weaponPivot, "Hub", new Vector3(0f, -0.7f, 0.05f), new Vector3(0.12f, 0.02f, 0.12f), steel, new Vector3(90f, 0f, 0f));
                    for (int i = 0; i < 4; i++)
                    {
                        Part(PrimitiveType.Cube, _weaponPivot, "Blade", new Vector3(0f, -0.7f, 0.05f), new Vector3(0.6f, 0.07f, 0.015f), steel, new Vector3(0f, 0f, i * 45f));
                    }

                    break;
                case WeaponType.CatStaff:
                    Part(PrimitiveType.Cylinder, _weaponPivot, "Staff", new Vector3(0f, -0.6f, 0f), new Vector3(0.04f, 0.6f, 0.04f), wood);
                    Part(PrimitiveType.Sphere, _weaponPivot, "Paw", new Vector3(0f, -1.25f, 0f), Vector3.one * 0.15f, new Color(0.98f, 0.85f, 0.55f));
                    Part(PrimitiveType.Cube, _weaponPivot, "EarL", new Vector3(-0.06f, -1.34f, 0f), new Vector3(0.05f, 0.06f, 0.02f), new Color(0.98f, 0.85f, 0.55f), new Vector3(0f, 0f, 45f));
                    Part(PrimitiveType.Cube, _weaponPivot, "EarR", new Vector3(0.06f, -1.34f, 0f), new Vector3(0.05f, 0.06f, 0.02f), new Color(0.98f, 0.85f, 0.55f), new Vector3(0f, 0f, 45f));
                    break;
                case WeaponType.TwoHandSword:
                    Part(PrimitiveType.Cube, _weaponPivot, "Blade", new Vector3(0f, -0.95f, 0f), new Vector3(0.09f, 1.1f, 0.03f), steel);
                    Part(PrimitiveType.Cube, _weaponPivot, "Guard", new Vector3(0f, -0.4f, 0f), new Vector3(0.3f, 0.05f, 0.06f), wood);
                    break;
                case WeaponType.OneHandSword:
                    Part(PrimitiveType.Cube, _weaponPivot, "Blade", new Vector3(0f, -0.75f, 0f), new Vector3(0.07f, 0.7f, 0.03f), steel);
                    break;
                case WeaponType.Spear:
                    Part(PrimitiveType.Cylinder, _weaponPivot, "Shaft", new Vector3(0f, -0.7f, 0f), new Vector3(0.05f, 0.8f, 0.05f), wood);
                    Part(PrimitiveType.Cube, _weaponPivot, "Tip", new Vector3(0f, -1.55f, 0f), new Vector3(0.08f, 0.22f, 0.03f), steel);
                    break;
                case WeaponType.Mace:
                    Part(PrimitiveType.Cylinder, _weaponPivot, "Handle", new Vector3(0f, -0.6f, 0f), new Vector3(0.05f, 0.3f, 0.05f), wood);
                    Part(PrimitiveType.Sphere, _weaponPivot, "Head", new Vector3(0f, -0.95f, 0f), Vector3.one * 0.2f, steel);
                    break;
                case WeaponType.Staff:
                    Part(PrimitiveType.Cylinder, _weaponPivot, "Staff", new Vector3(0f, -0.7f, 0f), new Vector3(0.05f, 0.75f, 0.05f), wood);
                    Part(PrimitiveType.Sphere, _weaponPivot, "Orb", new Vector3(0f, -1.48f, 0f), Vector3.one * 0.16f, new Color(0.45f, 0.75f, 1f));
                    break;
                case WeaponType.Bow:
                    Part(PrimitiveType.Cube, _weaponPivot, "Bow", new Vector3(0f, -0.5f, 0.05f), new Vector3(0.04f, 0.9f, 0.05f), wood, new Vector3(0f, 0f, 0f));
                    break;
                case WeaponType.Knuckle:
                    Part(PrimitiveType.Sphere, _weaponPivot, "Fist", new Vector3(0f, -0.48f, 0f), Vector3.one * 0.16f, steel);
                    break;
                case WeaponType.Katar:
                    Part(PrimitiveType.Cube, _weaponPivot, "Katar", new Vector3(0f, -0.62f, 0f), new Vector3(0.12f, 0.36f, 0.02f), steel);
                    break;
                case WeaponType.Dagger:
                    Part(PrimitiveType.Cube, _weaponPivot, "Blade", new Vector3(0f, -0.56f, 0f), new Vector3(0.05f, 0.28f, 0.02f), steel);
                    break;
            }
        }

        // ------------------------------------------------------------------ monsters
        private void BuildMonster(MonsterDefinition definition)
        {
            ResetModel();
            _lungeAttack = true;
            _flyer = definition.Shape == MonsterShape.Flyer || definition.Shape == MonsterShape.Fish
                                                           || (definition.Shape == MonsterShape.Slime && definition.Variant == "ghost");
            Color main = RuntimeMaterials.Hex(definition.ColorHex);
            Color dark = main * 0.6f;
            dark.a = 1f;
            Color eye = new Color(0.1f, 0.05f, 0.05f);

            switch (definition.Shape)
            {
                case MonsterShape.Blob:
                    Part(PrimitiveType.Cylinder, _model, "Stem", new Vector3(0f, 0.25f, 0f), new Vector3(0.36f, 0.25f, 0.36f), new Color(0.95f, 0.9f, 0.78f));
                    Part(PrimitiveType.Sphere, _model, "Cap", new Vector3(0f, 0.62f, 0f), new Vector3(0.9f, 0.5f, 0.9f), main);
                    Part(PrimitiveType.Sphere, _model, "Spot1", new Vector3(0.18f, 0.8f, 0.22f), Vector3.one * 0.12f, Color.white);
                    Part(PrimitiveType.Sphere, _model, "Spot2", new Vector3(-0.22f, 0.78f, 0.12f), Vector3.one * 0.1f, Color.white);
                    Part(PrimitiveType.Sphere, _model, "EyeL", new Vector3(-0.08f, 0.32f, 0.17f), Vector3.one * 0.07f, eye);
                    Part(PrimitiveType.Sphere, _model, "EyeR", new Vector3(0.08f, 0.32f, 0.17f), Vector3.one * 0.07f, eye);
                    break;

                case MonsterShape.Quadruped:
                    Part(PrimitiveType.Capsule, _model, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(0.55f, 0.55f, 0.55f), main, new Vector3(90f, 0f, 0f));
                    Part(PrimitiveType.Sphere, _model, "Head", new Vector3(0f, 0.82f, 0.62f), Vector3.one * 0.42f, main);
                    Part(PrimitiveType.Sphere, _model, "Snout", new Vector3(0f, 0.75f, 0.84f), new Vector3(0.2f, 0.16f, 0.2f), dark);
                    Part(PrimitiveType.Sphere, _model, "EyeL", new Vector3(-0.12f, 0.9f, 0.8f), Vector3.one * 0.07f, eye);
                    Part(PrimitiveType.Sphere, _model, "EyeR", new Vector3(0.12f, 0.9f, 0.8f), Vector3.one * 0.07f, eye);
                    foreach (var leg in new[] { new Vector2(-0.2f, 0.35f), new Vector2(0.2f, 0.35f), new Vector2(-0.2f, -0.35f), new Vector2(0.2f, -0.35f) })
                    {
                        Part(PrimitiveType.Cylinder, _model, "Leg", new Vector3(leg.x, 0.2f, leg.y), new Vector3(0.13f, 0.2f, 0.13f), dark);
                    }

                    if (definition.Id.Contains("grazer"))
                    {
                        Part(PrimitiveType.Cylinder, _model, "HornL", new Vector3(-0.16f, 1.05f, 0.6f), new Vector3(0.06f, 0.16f, 0.06f), new Color(0.92f, 0.88f, 0.78f), new Vector3(-20f, 0f, 30f));
                        Part(PrimitiveType.Cylinder, _model, "HornR", new Vector3(0.16f, 1.05f, 0.6f), new Vector3(0.06f, 0.16f, 0.06f), new Color(0.92f, 0.88f, 0.78f), new Vector3(-20f, 0f, -30f));
                    }
                    else if (definition.Id.Contains("wolf"))
                    {
                        Part(PrimitiveType.Cube, _model, "EarL", new Vector3(-0.12f, 1.06f, 0.56f), new Vector3(0.08f, 0.16f, 0.05f), dark);
                        Part(PrimitiveType.Cube, _model, "EarR", new Vector3(0.12f, 1.06f, 0.56f), new Vector3(0.08f, 0.16f, 0.05f), dark);
                    }

                    QuadrupedVariant(definition.Variant, main, dark);
                    break;

                case MonsterShape.Insect:
                    BuildInsect(definition.Variant, main, dark);
                    break;

                case MonsterShape.Plant:
                    BuildPlant(definition.Variant, main, dark);
                    break;

                case MonsterShape.Slime:
                    BuildSlime(definition.Variant, main, dark);
                    break;

                case MonsterShape.Bird:
                    BuildBird(definition.Variant, main, dark);
                    break;

                case MonsterShape.Fish:
                    BuildFish(definition.Variant, main, dark);
                    break;

                case MonsterShape.Biped:
                    Part(PrimitiveType.Cylinder, _model, "LegL", new Vector3(-0.12f, 0.22f, 0f), new Vector3(0.16f, 0.22f, 0.16f), dark);
                    Part(PrimitiveType.Cylinder, _model, "LegR", new Vector3(0.12f, 0.22f, 0f), new Vector3(0.16f, 0.22f, 0.16f), dark);
                    Part(PrimitiveType.Capsule, _model, "Torso", new Vector3(0f, 0.86f, 0f), new Vector3(0.55f, 0.45f, 0.42f), main);
                    Part(PrimitiveType.Sphere, _model, "Head", new Vector3(0f, 1.5f, 0f), Vector3.one * 0.44f, main);
                    Part(PrimitiveType.Sphere, _model, "EyeL", new Vector3(-0.09f, 1.54f, 0.19f), Vector3.one * 0.08f, new Color(1f, 0.8f, 0.2f));
                    Part(PrimitiveType.Sphere, _model, "EyeR", new Vector3(0.09f, 1.54f, 0.19f), Vector3.one * 0.08f, new Color(1f, 0.8f, 0.2f));
                    Part(PrimitiveType.Cylinder, _model, "HornL", new Vector3(-0.14f, 1.74f, 0f), new Vector3(0.05f, 0.12f, 0.05f), dark, new Vector3(0f, 0f, 25f));
                    Part(PrimitiveType.Cylinder, _model, "HornR", new Vector3(0.14f, 1.74f, 0f), new Vector3(0.05f, 0.12f, 0.05f), dark, new Vector3(0f, 0f, -25f));
                    Part(PrimitiveType.Capsule, _model, "ArmL", new Vector3(-0.36f, 0.95f, 0.05f), new Vector3(0.15f, 0.3f, 0.15f), dark);
                    Part(PrimitiveType.Capsule, _model, "ArmR", new Vector3(0.36f, 0.95f, 0.05f), new Vector3(0.15f, 0.3f, 0.15f), dark);
                    break;

                case MonsterShape.Flyer:
                    Part(PrimitiveType.Sphere, _model, "Body", new Vector3(0f, 1.2f, 0f), Vector3.one * 0.55f, main);
                    Part(PrimitiveType.Sphere, _model, "EyeL", new Vector3(-0.1f, 1.27f, 0.24f), Vector3.one * 0.08f, eye);
                    Part(PrimitiveType.Sphere, _model, "EyeR", new Vector3(0.1f, 1.27f, 0.24f), Vector3.one * 0.08f, eye);
                    Color wing = Color.Lerp(main, Color.white, 0.35f);
                    wing.a = 1f;
                    _wings.Add(Part(PrimitiveType.Cube, _model, "WingL", new Vector3(-0.42f, 1.25f, -0.05f), new Vector3(0.55f, 0.03f, 0.3f), wing));
                    _wings.Add(Part(PrimitiveType.Cube, _model, "WingR", new Vector3(0.42f, 1.25f, -0.05f), new Vector3(0.55f, 0.03f, 0.3f), wing));
                    break;

                case MonsterShape.Serpent:
                    BuildSerpent(definition, main, dark);
                    break;

                case MonsterShape.Golem:
                    BuildGolem(definition, main, dark);
                    break;

                case MonsterShape.Wraith:
                    BuildWraith(definition, main, dark);
                    break;

                case MonsterShape.Humanoid:
                    BuildHumanoid(MonsterLook(definition));
                    _lungeAttack = true;
                    if (definition.Race == Race.Undead || definition.Element == Element.Undead || definition.Element == Element.Shadow)
                    {
                        Color glow = definition.Element == Element.Ghost ? new Color(0.6f, 0.85f, 1f) : new Color(0.55f, 0.9f, 1f);
                        var face = _rig != null ? _rig.HeadMid : _model;
                        GlowPart(PrimitiveType.Sphere, face, "EyeGlowL", new Vector3(-0.08f, 1.55f, 0.2f), Vector3.one * 0.08f, glow);
                        GlowPart(PrimitiveType.Sphere, face, "EyeGlowR", new Vector3(0.08f, 1.55f, 0.2f), Vector3.one * 0.08f, glow);
                    }

                    break;

                default: // Training dummy
                    Color straw = new Color(0.86f, 0.74f, 0.42f);
                    Part(PrimitiveType.Cylinder, _model, "Post", new Vector3(0f, 0.8f, 0f), new Vector3(0.14f, 0.8f, 0.14f), main);
                    Part(PrimitiveType.Cube, _model, "Arms", new Vector3(0f, 1.25f, 0f), new Vector3(1.0f, 0.1f, 0.1f), main);
                    Part(PrimitiveType.Capsule, _model, "Body", new Vector3(0f, 1.15f, 0f), new Vector3(0.45f, 0.4f, 0.35f), straw);
                    Part(PrimitiveType.Sphere, _model, "Head", new Vector3(0f, 1.78f, 0f), Vector3.one * 0.42f, straw);
                    Part(PrimitiveType.Cylinder, _model, "Target", new Vector3(0f, 1.18f, 0.18f), new Vector3(0.3f, 0.01f, 0.3f), new Color(0.8f, 0.15f, 0.12f), new Vector3(90f, 0f, 0f));
                    break;
            }

            _baseScale = Mathf.Max(0.2f, definition.Scale);
            _model.localScale = Vector3.one * _baseScale;

            if (definition.IsBoss)
            {
                // A ring of light at a boss's feet: gold for MVPs, silver for mini-bosses.
                var ring = GroundRing.Create("BossRing", definition.IsMvp ? new Color(1f, 0.8f, 0.25f, 0.9f) : new Color(0.85f, 0.88f, 1f, 0.85f),
                    0.75f * _baseScale, 0.08f);
                ring.ShowAt(transform.position);
                ring.transform.SetParent(transform, true);
                ring.SetSpin(25f);
            }
        }

        /// <summary>Humanoid monsters wear their kit through the player model's gear builder.</summary>
        private static AvatarLook MonsterLook(MonsterDefinition definition)
        {
            int hash = 0;
            foreach (char c in definition.Id)
            {
                hash = hash * 31 + c;
            }

            hash = Mathf.Abs(hash);
            return new AvatarLook
            {
                Outfit = RuntimeMaterials.Hex(definition.ColorHex),
                Skin = RuntimeMaterials.Hex(definition.SkinHex, AvatarLook.DefaultSkin),
                Hair = AvatarLook.HairPalette[hash % 3 == 0 ? 3 : hash % 3 == 1 ? 0 : 4],
                HairStyle = definition.LookFemale ? 2 + hash % 3 : 6 + hash % 2,
                Gender = definition.LookFemale ? Gender.Female : Gender.Male,
                Weapon = definition.LookWeapon,
                HeadUpper = ItemCatalog.Get(definition.LookHead)?.Id,
                HeadLower = ItemCatalog.Get(definition.LookLower)?.Id,
                Shield = ItemCatalog.Get(definition.LookShield)?.Id,
                Garment = ItemCatalog.Get(definition.LookGarment)?.Id,
            };
        }

        // ------------------------------------------------------------ Phase 8 monster shapes
        private static readonly Color MonsterEye = new Color(0.08f, 0.05f, 0.05f);
        private static readonly Color Bone = new Color(0.93f, 0.9f, 0.82f);
        private static readonly Color Leafy = new Color(0.32f, 0.58f, 0.26f);
        private static readonly Color Beak = new Color(0.95f, 0.66f, 0.2f);

        /// <summary>A capsule from <paramref name="from"/> to <paramref name="to"/> (legs, horns, tails, stems).</summary>
        private Transform Limb(string name, Vector3 from, Vector3 to, float thickness, Color color, Transform parent = null)
        {
            var dir = to - from;
            var rotation = Quaternion.FromToRotation(Vector3.up, dir.sqrMagnitude > 1e-6f ? dir : Vector3.up);
            return Part(PrimitiveType.Capsule, parent ?? _model, name, (from + to) * 0.5f, new Vector3(thickness, dir.magnitude * 0.5f, thickness), color,
                rotation.eulerAngles);
        }

        private void Eyes(Vector3 center, float spacing, float size, Color color, bool glow = false)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                var at = center + new Vector3(side * spacing, 0f, 0f);
                if (glow)
                {
                    GlowPart(PrimitiveType.Sphere, _model, "Eye", at, Vector3.one * size, color);
                }
                else
                {
                    Part(PrimitiveType.Sphere, _model, "Eye", at, Vector3.one * size, color);
                }
            }
        }

        /// <summary>Details on a four-legged beast from its name: tusks, horns, long ears, pointed ears, a shell, a low body.</summary>
        private void QuadrupedVariant(string variant, Color main, Color dark)
        {
            Color light = Color.Lerp(main, Color.white, 0.45f);
            switch (variant)
            {
                case "tusks":
                    foreach (float s in new[] { -1f, 1f })
                    {
                        Limb("Tusk", new Vector3(s * 0.09f, 0.7f, 0.9f), new Vector3(s * 0.13f, 0.86f, 1.0f), 0.05f, Bone);
                    }

                    Part(PrimitiveType.Cube, _model, "Bristles", new Vector3(0f, 0.92f, 0.1f), new Vector3(0.12f, 0.12f, 0.7f), dark);
                    break;
                case "horns":
                    foreach (float s in new[] { -1f, 1f })
                    {
                        var root = new Vector3(s * 0.12f, 1.0f, 0.58f);
                        var mid = root + new Vector3(s * 0.12f, 0.16f, -0.08f);
                        Limb("Horn", root, mid, 0.07f, Bone);
                        Limb("HornTip", mid, mid + new Vector3(s * 0.02f, 0.1f, -0.14f), 0.05f, Bone);
                    }

                    break;
                case "long_ears":
                    foreach (float s in new[] { -1f, 1f })
                    {
                        Limb("Ear", new Vector3(s * 0.08f, 1.0f, 0.56f), new Vector3(s * 0.14f, 1.38f, 0.5f), 0.1f, main);
                        Limb("EarInner", new Vector3(s * 0.08f, 1.03f, 0.585f), new Vector3(s * 0.135f, 1.33f, 0.53f), 0.05f, new Color(0.95f, 0.7f, 0.72f));
                    }

                    Part(PrimitiveType.Sphere, _model, "Puff", new Vector3(0f, 0.68f, -0.6f), Vector3.one * 0.2f, light);
                    return;
                case "ears":
                    foreach (float s in new[] { -1f, 1f })
                    {
                        Part(PrimitiveType.Cube, _model, "Ear", new Vector3(s * 0.13f, 1.04f, 0.58f), new Vector3(0.1f, 0.14f, 0.05f), dark, new Vector3(0f, 0f, s * -20f));
                    }

                    Limb("Tail", new Vector3(0f, 0.7f, -0.55f), new Vector3(0f, 0.95f, -0.95f), 0.12f, main);
                    return;
                case "shell":
                    Part(PrimitiveType.Sphere, _model, "Shell", new Vector3(0f, 0.72f, -0.05f), new Vector3(0.95f, 0.6f, 1.1f), dark);
                    Part(PrimitiveType.Cylinder, _model, "Rim", new Vector3(0f, 0.55f, -0.05f), new Vector3(0.98f, 0.03f, 1.12f), light);
                    for (int i = 0; i < 3; i++)
                    {
                        Part(PrimitiveType.Cylinder, _model, "Scute", new Vector3(0f, 0.98f, -0.3f + i * 0.25f), new Vector3(0.22f, 0.04f, 0.22f), main * 0.8f);
                    }

                    return;
                case "low":
                    Part(PrimitiveType.Sphere, _model, "Belly", new Vector3(0f, 0.38f, 0.05f), new Vector3(0.7f, 0.32f, 0.9f), light);
                    Eyes(new Vector3(0f, 0.98f, 0.66f), 0.14f, 0.13f, new Color(1f, 0.85f, 0.25f));
                    Limb("Tail", new Vector3(0f, 0.45f, -0.5f), new Vector3(0f, 0.25f, -1.05f), 0.12f, main);
                    return;
            }

            Limb("Tail", new Vector3(0f, 0.72f, -0.55f), new Vector3(0f, 0.9f, -0.85f), 0.07f, dark);
        }

        /// <summary>Ants and beetles; spiders (eight legs), scorpions (a stinging tail), crabs (claws) and mantises (blades).</summary>
        private void BuildInsect(string variant, Color main, Color dark)
        {
            Color shine = Color.Lerp(main, Color.white, 0.3f);
            switch (variant)
            {
                case "spider":
                    Part(PrimitiveType.Sphere, _model, "Body", new Vector3(0f, 0.45f, 0.22f), new Vector3(0.48f, 0.34f, 0.48f), dark);
                    Part(PrimitiveType.Sphere, _model, "Abdomen", new Vector3(0f, 0.58f, -0.35f), new Vector3(0.78f, 0.62f, 0.85f), main);
                    Part(PrimitiveType.Cube, _model, "Mark", new Vector3(0f, 0.9f, -0.35f), new Vector3(0.12f, 0.02f, 0.3f), shine, new Vector3(0f, 45f, 0f));
                    for (int i = 0; i < 4; i++)
                    {
                        InsectLeg(0.22f + 0.12f * i - 0.18f, 0.2f, 0.62f, 0.07f, dark, -40f + i * 25f);
                    }

                    GlowPart(PrimitiveType.Sphere, _model, "EyeL", new Vector3(-0.08f, 0.55f, 0.44f), Vector3.one * 0.07f, new Color(1f, 0.25f, 0.2f));
                    GlowPart(PrimitiveType.Sphere, _model, "EyeR", new Vector3(0.08f, 0.55f, 0.44f), Vector3.one * 0.07f, new Color(1f, 0.25f, 0.2f));
                    GlowPart(PrimitiveType.Sphere, _model, "EyeL2", new Vector3(-0.13f, 0.5f, 0.42f), Vector3.one * 0.045f, new Color(1f, 0.25f, 0.2f));
                    GlowPart(PrimitiveType.Sphere, _model, "EyeR2", new Vector3(0.13f, 0.5f, 0.42f), Vector3.one * 0.045f, new Color(1f, 0.25f, 0.2f));
                    return;

                case "crab":
                    Part(PrimitiveType.Sphere, _model, "Shell", new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.4f, 0.7f), main);
                    for (int i = 0; i < 3; i++)
                    {
                        InsectLeg(-0.2f + i * 0.17f, 0.3f, 0.55f, 0.07f, dark, 30f - i * 30f);
                    }

                    foreach (float s in new[] { -1f, 1f })
                    {
                        var shoulder = new Vector3(s * 0.35f, 0.45f, 0.25f);
                        var wrist = new Vector3(s * 0.48f, 0.55f, 0.6f);
                        Limb("Arm", shoulder, wrist, 0.1f, dark);
                        Part(PrimitiveType.Sphere, _model, "Claw", wrist + new Vector3(0f, 0.04f, 0.12f), new Vector3(0.24f, 0.2f, 0.3f), main);
                        Limb("Pincer", wrist + new Vector3(s * 0.04f, 0.1f, 0.2f), wrist + new Vector3(s * 0.02f, 0.14f, 0.38f), 0.07f, shine);
                        Limb("Stalk", new Vector3(s * 0.12f, 0.6f, 0.25f), new Vector3(s * 0.14f, 0.78f, 0.3f), 0.04f, dark);
                        Part(PrimitiveType.Sphere, _model, "Eye", new Vector3(s * 0.14f, 0.8f, 0.3f), Vector3.one * 0.08f, MonsterEye);
                    }

                    return;

                case "scorpion":
                    Part(PrimitiveType.Sphere, _model, "Body", new Vector3(0f, 0.38f, 0.05f), new Vector3(0.6f, 0.3f, 0.95f), main);
                    for (int i = 0; i < 4; i++)
                    {
                        InsectLeg(-0.25f + i * 0.15f, 0.22f, 0.5f, 0.06f, dark, 20f - i * 15f);
                    }

                    var tail = new Vector3(0f, 0.42f, -0.45f);
                    var segments = new[] { new Vector3(0f, 0.62f, -0.7f), new Vector3(0f, 0.92f, -0.78f), new Vector3(0f, 1.18f, -0.62f), new Vector3(0f, 1.28f, -0.38f) };
                    foreach (var next in segments)
                    {
                        Limb("Tail", tail, next, 0.15f, main);
                        tail = next;
                    }

                    Limb("Stinger", tail, tail + new Vector3(0f, -0.12f, 0.16f), 0.07f, Bone);
                    foreach (float s in new[] { -1f, 1f })
                    {
                        Limb("Arm", new Vector3(s * 0.2f, 0.4f, 0.45f), new Vector3(s * 0.32f, 0.42f, 0.78f), 0.09f, dark);
                        Part(PrimitiveType.Sphere, _model, "Claw", new Vector3(s * 0.32f, 0.44f, 0.9f), new Vector3(0.18f, 0.12f, 0.24f), main);
                    }

                    Eyes(new Vector3(0f, 0.5f, 0.5f), 0.08f, 0.06f, MonsterEye);
                    return;

                case "mantis":
                    Part(PrimitiveType.Capsule, _model, "Abdomen", new Vector3(0f, 0.5f, -0.35f), new Vector3(0.3f, 0.4f, 0.3f), main, new Vector3(70f, 0f, 0f));
                    Limb("Thorax", new Vector3(0f, 0.55f, -0.05f), new Vector3(0f, 1.25f, 0.2f), 0.16f, main);
                    Part(PrimitiveType.Sphere, _model, "Head", new Vector3(0f, 1.35f, 0.28f), new Vector3(0.3f, 0.22f, 0.22f), shine);
                    Eyes(new Vector3(0f, 1.38f, 0.32f), 0.12f, 0.1f, new Color(0.9f, 0.85f, 0.2f));
                    for (int i = 0; i < 2; i++)
                    {
                        InsectLeg(-0.25f - i * 0.15f, 0.3f, 0.5f, 0.05f, dark, -20f);
                    }

                    foreach (float s in new[] { -1f, 1f })
                    {
                        var elbow = new Vector3(s * 0.2f, 1.25f, 0.45f);
                        Limb("Arm", new Vector3(s * 0.08f, 1.05f, 0.2f), elbow, 0.08f, main);
                        Part(PrimitiveType.Cube, _model, "Blade", elbow + new Vector3(0f, -0.2f, 0.05f), new Vector3(0.05f, 0.45f, 0.1f), Bone, new Vector3(-20f, 0f, 0f));
                    }

                    _wings.Add(Part(PrimitiveType.Cube, _model, "WingL", new Vector3(-0.15f, 0.75f, -0.3f), new Vector3(0.3f, 0.02f, 0.65f), shine));
                    _wings.Add(Part(PrimitiveType.Cube, _model, "WingR", new Vector3(0.15f, 0.75f, -0.3f), new Vector3(0.3f, 0.02f, 0.65f), shine));
                    return;
            }

            // Ants and beetles.
            bool beetle = variant == "beetle";
            Part(PrimitiveType.Sphere, _model, "Head", new Vector3(0f, 0.5f, 0.5f), Vector3.one * 0.34f, dark);
            Part(PrimitiveType.Sphere, _model, "Thorax", new Vector3(0f, 0.45f, 0.18f), new Vector3(0.36f, 0.3f, 0.38f), main);
            Part(PrimitiveType.Sphere, _model, "Abdomen", new Vector3(0f, 0.5f, -0.32f), beetle ? new Vector3(0.7f, 0.5f, 0.8f) : new Vector3(0.52f, 0.44f, 0.7f), main);
            if (beetle)
            {
                Part(PrimitiveType.Cube, _model, "Seam", new Vector3(0f, 0.76f, -0.32f), new Vector3(0.02f, 0.02f, 0.62f), dark);
                Limb("Horn", new Vector3(0f, 0.6f, 0.62f), new Vector3(0f, 0.92f, 0.8f), 0.08f, dark);
            }

            for (int i = 0; i < 3; i++)
            {
                InsectLeg(0.28f - i * 0.16f, 0.2f, 0.5f, 0.06f, dark, 35f - i * 35f);
            }

            foreach (float s in new[] { -1f, 1f })
            {
                Limb("Antenna", new Vector3(s * 0.06f, 0.62f, 0.6f), new Vector3(s * 0.2f, 0.95f, 0.78f), 0.035f, dark);
                Limb("Mandible", new Vector3(s * 0.08f, 0.42f, 0.64f), new Vector3(s * 0.02f, 0.38f, 0.8f), 0.05f, Bone);
            }

            Eyes(new Vector3(0f, 0.56f, 0.64f), 0.1f, 0.07f, shine);
        }

        /// <summary>A jointed leg on each side at <paramref name="z"/>: out and up to the knee, then down to the ground.</summary>
        private void InsectLeg(float z, float height, float reach, float thickness, Color color, float sweep)
        {
            foreach (float s in new[] { -1f, 1f })
            {
                var swing = Quaternion.Euler(0f, s * sweep, 0f);
                var hip = new Vector3(s * 0.15f, height + 0.25f, z);
                var knee = hip + swing * new Vector3(s * reach * 0.55f, 0.22f, 0f);
                var foot = new Vector3(hip.x, 0f, hip.z) + swing * new Vector3(s * reach, 0f, 0f);
                Limb("Leg", hip, knee, thickness, color);
                Limb("Shin", knee, foot, thickness * 0.8f, color);
            }
        }

        /// <summary>A plant with a face: flowers, snapping flytraps, walking trees, cacti, or a leafy bulb.</summary>
        private void BuildPlant(string variant, Color main, Color dark)
        {
            // Leaves at the root.
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 45f;
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Part(PrimitiveType.Sphere, _model, "Leaf", dir * 0.32f + new Vector3(0f, 0.06f, 0f), new Vector3(0.22f, 0.05f, 0.5f), Leafy * 0.85f, new Vector3(-12f, a, 0f));
            }

            switch (variant)
            {
                case "flower":
                    Limb("Stem", Vector3.zero, new Vector3(0f, 0.95f, 0f), 0.1f, Leafy);
                    Part(PrimitiveType.Sphere, _model, "LeafL", new Vector3(-0.18f, 0.5f, 0f), new Vector3(0.3f, 0.06f, 0.14f), Leafy, new Vector3(0f, 0f, 25f));
                    Part(PrimitiveType.Sphere, _model, "LeafR", new Vector3(0.18f, 0.6f, 0f), new Vector3(0.3f, 0.06f, 0.14f), Leafy, new Vector3(0f, 0f, -25f));
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 45f;
                        var dir = Quaternion.Euler(0f, 0f, a) * Vector3.up;
                        Part(PrimitiveType.Sphere, _model, "Petal", new Vector3(0f, 1.15f, 0.08f) + dir * 0.28f, new Vector3(0.22f, 0.34f, 0.06f), main, new Vector3(0f, 0f, a));
                    }

                    Part(PrimitiveType.Sphere, _model, "Face", new Vector3(0f, 1.15f, 0.12f), new Vector3(0.36f, 0.36f, 0.16f), new Color(0.98f, 0.82f, 0.3f));
                    Eyes(new Vector3(0f, 1.2f, 0.2f), 0.07f, 0.06f, MonsterEye);
                    return;

                case "jaws":
                    Limb("Stem", Vector3.zero, new Vector3(0f, 0.8f, 0.05f), 0.14f, Leafy);
                    Color mouth = new Color(0.75f, 0.15f, 0.2f);
                    Part(PrimitiveType.Sphere, _model, "JawTop", new Vector3(0f, 1.08f, 0.15f), new Vector3(0.6f, 0.26f, 0.6f), main, new Vector3(-22f, 0f, 0f));
                    Part(PrimitiveType.Sphere, _model, "JawBottom", new Vector3(0f, 0.86f, 0.15f), new Vector3(0.58f, 0.22f, 0.58f), main, new Vector3(18f, 0f, 0f));
                    Part(PrimitiveType.Sphere, _model, "Maw", new Vector3(0f, 0.97f, 0.22f), new Vector3(0.48f, 0.12f, 0.48f), mouth);
                    for (int i = 0; i < 5; i++)
                    {
                        float x = -0.2f + i * 0.1f;
                        Limb("Tooth", new Vector3(x, 1.0f, 0.42f), new Vector3(x, 0.92f, 0.44f), 0.035f, Bone);
                    }

                    Eyes(new Vector3(0f, 1.2f, 0.3f), 0.12f, 0.08f, new Color(1f, 0.9f, 0.3f), glow: true);
                    return;

                case "tree":
                    Color bark = new Color(0.42f, 0.3f, 0.2f);
                    Part(PrimitiveType.Cylinder, _model, "Trunk", new Vector3(0f, 0.7f, 0f), new Vector3(0.42f, 0.7f, 0.42f), bark);
                    foreach (float s in new[] { -1f, 1f })
                    {
                        Limb("Root", new Vector3(s * 0.15f, 0.15f, 0f), new Vector3(s * 0.4f, 0f, 0.1f), 0.12f, bark);
                        Limb("Branch", new Vector3(s * 0.15f, 1.0f, 0f), new Vector3(s * 0.55f, 1.25f, 0.15f), 0.1f, bark);
                        Part(PrimitiveType.Sphere, _model, "Bough", new Vector3(s * 0.55f, 1.35f, 0.15f), Vector3.one * 0.38f, Color.Lerp(Leafy, main, 0.35f));
                    }

                    Part(PrimitiveType.Sphere, _model, "Canopy", new Vector3(0f, 1.65f, 0f), new Vector3(0.95f, 0.75f, 0.95f), Color.Lerp(Leafy, main, 0.35f));
                    Eyes(new Vector3(0f, 1.05f, 0.2f), 0.09f, 0.08f, new Color(1f, 0.85f, 0.3f), glow: true);
                    Part(PrimitiveType.Cube, _model, "Mouth", new Vector3(0f, 0.85f, 0.21f), new Vector3(0.14f, 0.05f, 0.02f), MonsterEye);
                    return;

                case "cactus":
                    Part(PrimitiveType.Capsule, _model, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(0.45f, 0.6f, 0.45f), Leafy);
                    Limb("ArmL", new Vector3(-0.2f, 0.6f, 0f), new Vector3(-0.42f, 0.95f, 0f), 0.16f, Leafy);
                    Limb("ArmR", new Vector3(0.2f, 0.75f, 0f), new Vector3(0.4f, 1.0f, 0f), 0.14f, Leafy);
                    Part(PrimitiveType.Sphere, _model, "Bloom", new Vector3(0f, 1.2f, 0f), new Vector3(0.22f, 0.12f, 0.22f), main);
                    Eyes(new Vector3(0f, 0.85f, 0.22f), 0.08f, 0.07f, MonsterEye);
                    return;
            }

            // A leafy bulb with a face.
            Part(PrimitiveType.Sphere, _model, "Bulb", new Vector3(0f, 0.42f, 0f), new Vector3(0.62f, 0.58f, 0.62f), main);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Limb("Sprout", new Vector3(0f, 0.65f, 0f), new Vector3(0f, 1.0f, 0f) + dir * 0.2f, 0.06f, Leafy);
                Part(PrimitiveType.Sphere, _model, "SproutLeaf", new Vector3(0f, 1.02f, 0f) + dir * 0.26f, new Vector3(0.18f, 0.05f, 0.3f), Leafy, new Vector3(-30f, a, 0f));
            }

            Eyes(new Vector3(0f, 0.5f, 0.27f), 0.09f, 0.08f, MonsterEye);
        }

        /// <summary>Ragnarok's Poring family: a glossy jelly with a face that squashes as it hops.</summary>
        private void BuildSlime(string variant, Color main, Color dark)
        {
            _squash = true;
            Color body = variant == "ghost" ? Color.Lerp(main, Color.white, 0.6f) : main;
            Part(PrimitiveType.Sphere, _model, "Jelly", new Vector3(0f, 0.36f, 0f), new Vector3(0.82f, 0.68f, 0.8f), body);
            Part(PrimitiveType.Sphere, _model, "Shine", new Vector3(-0.17f, 0.56f, 0.24f), new Vector3(0.14f, 0.1f, 0.08f), Color.white, new Vector3(0f, 0f, 30f));
            Eyes(new Vector3(0f, 0.4f, 0.36f), 0.11f, 0.09f, MonsterEye);
            Part(PrimitiveType.Sphere, _model, "Mouth", new Vector3(0f, 0.27f, 0.38f), new Vector3(0.1f, 0.05f, 0.04f), new Color(0.75f, 0.25f, 0.3f));
            switch (variant)
            {
                case "angel":
                    GlowPart(PrimitiveType.Cylinder, _model, "Halo", new Vector3(0f, 0.92f, 0f), new Vector3(0.36f, 0.015f, 0.36f), new Color(1f, 0.92f, 0.5f));
                    _wings.Add(Part(PrimitiveType.Sphere, _model, "WingL", new Vector3(-0.5f, 0.5f, -0.05f), new Vector3(0.4f, 0.06f, 0.24f), Color.white));
                    _wings.Add(Part(PrimitiveType.Sphere, _model, "WingR", new Vector3(0.5f, 0.5f, -0.05f), new Vector3(0.4f, 0.06f, 0.24f), Color.white));
                    break;
                case "devil":
                    Limb("HornL", new Vector3(-0.15f, 0.62f, 0.05f), new Vector3(-0.22f, 0.86f, 0f), 0.07f, dark);
                    Limb("HornR", new Vector3(0.15f, 0.62f, 0.05f), new Vector3(0.22f, 0.86f, 0f), 0.07f, dark);
                    _wings.Add(Part(PrimitiveType.Cube, _model, "WingL", new Vector3(-0.5f, 0.5f, -0.1f), new Vector3(0.38f, 0.02f, 0.24f), new Color(0.2f, 0.12f, 0.18f)));
                    _wings.Add(Part(PrimitiveType.Cube, _model, "WingR", new Vector3(0.5f, 0.5f, -0.1f), new Vector3(0.38f, 0.02f, 0.24f), new Color(0.2f, 0.12f, 0.18f)));
                    Limb("Tail", new Vector3(0f, 0.2f, -0.35f), new Vector3(0f, 0.45f, -0.65f), 0.04f, dark);
                    break;
                case "ghost":
                    Limb("Wisp", new Vector3(0f, 0.12f, -0.2f), new Vector3(0f, -0.05f, -0.55f), 0.18f, body);
                    break;
            }
        }

        /// <summary>A walking bird: chicks and fowl, or tall striders like the Peco Peco.</summary>
        private void BuildBird(string variant, Color main, Color dark)
        {
            bool tall = variant == "tall";
            float legTop = tall ? 0.9f : 0.42f;
            Color light = Color.Lerp(main, Color.white, 0.4f);
            var body = new Vector3(0f, legTop + 0.25f, 0f);
            Part(PrimitiveType.Sphere, _model, "Body", body, tall ? new Vector3(0.75f, 0.62f, 0.95f) : new Vector3(0.6f, 0.55f, 0.7f), main);
            Part(PrimitiveType.Sphere, _model, "Breast", body + new Vector3(0f, -0.04f, 0.2f), tall ? new Vector3(0.6f, 0.5f, 0.6f) : new Vector3(0.45f, 0.42f, 0.42f), light);
            var head = tall ? body + new Vector3(0f, 0.75f, 0.42f) : body + new Vector3(0f, 0.32f, 0.26f);
            if (tall)
            {
                Limb("Neck", body + new Vector3(0f, 0.15f, 0.3f), head, 0.2f, main);
            }

            Part(PrimitiveType.Sphere, _model, "Head", head, Vector3.one * (tall ? 0.34f : 0.32f), main);
            Limb("Beak", head + new Vector3(0f, -0.02f, 0.12f), head + new Vector3(0f, -0.06f, tall ? 0.4f : 0.3f), tall ? 0.1f : 0.08f, Beak);
            Eyes(head + new Vector3(0f, 0.05f, 0.12f), 0.1f, 0.07f, MonsterEye);
            Limb("Crest", head + new Vector3(0f, 0.12f, 0f), head + new Vector3(0f, 0.3f, -0.12f), 0.06f, dark);
            foreach (float s in new[] { -1f, 1f })
            {
                Part(PrimitiveType.Sphere, _model, "Wing", body + new Vector3(s * (tall ? 0.36f : 0.29f), 0.02f, -0.05f), new Vector3(0.1f, 0.34f, 0.55f), dark, new Vector3(-15f, 0f, 0f));
                var hip = new Vector3(s * 0.13f, legTop + 0.05f, 0f);
                var foot = new Vector3(s * 0.15f, 0.02f, 0.06f);
                Limb("Leg", hip, foot, tall ? 0.08f : 0.06f, Beak * 0.9f);
                Part(PrimitiveType.Cube, _model, "Foot", foot + new Vector3(0f, 0f, 0.07f), new Vector3(0.1f, 0.03f, 0.16f), Beak * 0.9f);
            }

            for (int i = -1; i <= 1; i++)
            {
                Limb("Tail", body + new Vector3(i * 0.08f, 0.05f, -0.35f), body + new Vector3(i * 0.16f, 0.4f, -0.62f), 0.08f, i == 0 ? dark : main);
            }
        }

        /// <summary>A fish swimming through the air: forked tail, fins, big eyes; sharks and swordfish by variant.</summary>
        private void BuildFish(string variant, Color main, Color dark)
        {
            Color belly = Color.Lerp(main, Color.white, 0.5f);
            bool shark = variant == "shark";
            var body = new Vector3(0f, 0.8f, 0f);
            Part(PrimitiveType.Sphere, _model, "Body", body, shark ? new Vector3(0.48f, 0.5f, 1.25f) : new Vector3(0.42f, 0.62f, 0.95f), main);
            Part(PrimitiveType.Sphere, _model, "Belly", body + new Vector3(0f, -0.1f, 0.03f), shark ? new Vector3(0.4f, 0.36f, 1.1f) : new Vector3(0.36f, 0.45f, 0.82f), belly);
            float back = shark ? -0.62f : -0.5f;
            Part(PrimitiveType.Cube, _model, "TailTop", body + new Vector3(0f, 0.16f, back - 0.12f), new Vector3(0.04f, 0.38f, 0.22f), dark, new Vector3(40f, 0f, 0f));
            Part(PrimitiveType.Cube, _model, "TailBottom", body + new Vector3(0f, -0.16f, back - 0.12f), new Vector3(0.04f, 0.38f, 0.22f), dark, new Vector3(-40f, 0f, 0f));
            Part(PrimitiveType.Cube, _model, "Dorsal", body + new Vector3(0f, shark ? 0.36f : 0.36f, -0.05f), shark ? new Vector3(0.04f, 0.42f, 0.3f) : new Vector3(0.03f, 0.25f, 0.42f), dark,
                new Vector3(-30f, 0f, 0f));
            foreach (float s in new[] { -1f, 1f })
            {
                Part(PrimitiveType.Sphere, _model, "Fin", body + new Vector3(s * 0.22f, -0.12f, 0.12f), new Vector3(0.2f, 0.04f, 0.14f), dark, new Vector3(0f, s * 30f, s * -25f));
                Part(PrimitiveType.Sphere, _model, "EyeWhite", body + new Vector3(s * 0.17f, 0.1f, shark ? 0.42f : 0.32f), Vector3.one * 0.13f, Color.white);
                Part(PrimitiveType.Sphere, _model, "Eye", body + new Vector3(s * 0.21f, 0.1f, shark ? 0.44f : 0.34f), Vector3.one * 0.07f, MonsterEye);
            }

            if (variant == "sword")
            {
                Limb("Bill", body + new Vector3(0f, 0.05f, 0.4f), body + new Vector3(0f, 0.08f, 1.1f), 0.05f, Bone);
            }
        }

        // ------------------------------------------------------------ Phase 5 monster shapes
        /// <summary>Nagas rise from a coiled tail; drakes, wyrms and the World Serpent's brood rear a long neck; leeches are fat worms.</summary>
        private void BuildSerpent(MonsterDefinition definition, Color main, Color dark)
        {
            string id = definition.Id;
            Color belly = Color.Lerp(main, Color.white, 0.35f);
            belly.a = 1f;
            Color eye = id.Contains("brood") ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.95f, 0.6f);
            if (id.Contains("leech"))
            {
                for (int i = 0; i < 4; i++)
                {
                    float size = 0.62f - i * 0.08f;
                    Part(PrimitiveType.Sphere, _model, "Segment", new Vector3(Mathf.Sin(i * 0.8f) * 0.1f, size * 0.45f, 0.3f - i * 0.38f), new Vector3(size, size * 0.85f, size), i % 2 == 0 ? main : dark);
                }

                Part(PrimitiveType.Cylinder, _model, "Sucker", new Vector3(0f, 0.32f, 0.62f), new Vector3(0.36f, 0.04f, 0.36f), new Color(0.55f, 0.1f, 0.15f), new Vector3(90f, 0f, 0f));
                GlowPart(PrimitiveType.Sphere, _model, "EyeL", new Vector3(-0.15f, 0.5f, 0.5f), Vector3.one * 0.07f, new Color(0.8f, 1f, 0.4f));
                GlowPart(PrimitiveType.Sphere, _model, "EyeR", new Vector3(0.15f, 0.5f, 0.5f), Vector3.one * 0.07f, new Color(0.8f, 1f, 0.4f));
                return;
            }

            // The tail: a curling line of shrinking coils behind the body.
            for (int i = 0; i < 6; i++)
            {
                float size = 0.5f - i * 0.055f;
                Part(PrimitiveType.Sphere, _model, "Coil", new Vector3(Mathf.Sin(i * 0.95f) * 0.32f, size * 0.42f, -0.1f - i * 0.3f), new Vector3(size, size * 0.8f, size), i % 2 == 0 ? main : dark);
            }

            if (id.Contains("naga"))
            {
                Part(PrimitiveType.Capsule, _model, "Torso", new Vector3(0f, 0.85f, 0.1f), new Vector3(0.45f, 0.42f, 0.35f), main);
                Part(PrimitiveType.Capsule, _model, "Belly", new Vector3(0f, 0.75f, 0.2f), new Vector3(0.3f, 0.3f, 0.2f), belly);
                Part(PrimitiveType.Sphere, _model, "Head", new Vector3(0f, 1.38f, 0.12f), Vector3.one * 0.38f, belly);
                Part(PrimitiveType.Sphere, _model, "Hair", new Vector3(0f, 1.45f, 0.02f), new Vector3(0.42f, 0.36f, 0.42f), dark);
                Part(PrimitiveType.Capsule, _model, "ArmL", new Vector3(-0.32f, 0.92f, 0.15f), new Vector3(0.12f, 0.26f, 0.12f), main, new Vector3(20f, 0f, -12f));
                Part(PrimitiveType.Sphere, _model, "EyeL", new Vector3(-0.07f, 1.4f, 0.29f), Vector3.one * 0.06f, eye);
                Part(PrimitiveType.Sphere, _model, "EyeR", new Vector3(0.07f, 1.4f, 0.29f), Vector3.one * 0.06f, eye);
                _weaponPivot = new GameObject("WeaponPivot").transform;
                _weaponPivot.SetParent(_model, false);
                _weaponPivot.localPosition = new Vector3(0.32f, 1.05f, 0.12f);
                Part(PrimitiveType.Capsule, _weaponPivot, "ArmR", new Vector3(0f, -0.2f, 0f), new Vector3(0.12f, 0.26f, 0.12f), main);
                if (definition.LookWeapon != WeaponType.Unarmed)
                {
                    BuildWeapon(definition.LookWeapon);
                }

                _weaponPivot.localRotation = Quaternion.Euler(RestAngle, 0f, 0f);
                if (id.Contains("queen"))
                {
                    Part(PrimitiveType.Cylinder, _model, "Crown", new Vector3(0f, 1.62f, 0.08f), new Vector3(0.36f, 0.06f, 0.36f), new Color(0.95f, 0.8f, 0.3f));
                    for (int i = -1; i <= 1; i++)
                    {
                        Part(PrimitiveType.Cube, _model, "CrownPoint", new Vector3(i * 0.1f, 1.7f, 0.22f), new Vector3(0.05f, 0.12f, 0.03f), new Color(0.95f, 0.8f, 0.3f), new Vector3(0f, 0f, 45f));
                    }
                }

                return;
            }

            // Drakes, wyrms and the brood: a long neck rearing up to a horned head.
            Part(PrimitiveType.Capsule, _model, "Body", new Vector3(0f, 0.45f, 0.05f), new Vector3(0.65f, 0.55f, 0.65f), main, new Vector3(80f, 0f, 0f));
            Part(PrimitiveType.Capsule, _model, "Neck", new Vector3(0f, 0.95f, 0.38f), new Vector3(0.32f, 0.45f, 0.32f), main, new Vector3(30f, 0f, 0f));
            Part(PrimitiveType.Capsule, _model, "NeckBelly", new Vector3(0f, 0.93f, 0.46f), new Vector3(0.22f, 0.4f, 0.18f), belly, new Vector3(30f, 0f, 0f));
            Part(PrimitiveType.Sphere, _model, "Head", new Vector3(0f, 1.4f, 0.62f), new Vector3(0.42f, 0.34f, 0.6f), main);
            Part(PrimitiveType.Sphere, _model, "Jaw", new Vector3(0f, 1.28f, 0.74f), new Vector3(0.3f, 0.14f, 0.42f), dark);
            Part(PrimitiveType.Sphere, _model, "EyeL", new Vector3(-0.14f, 1.48f, 0.78f), Vector3.one * 0.07f, eye);
            Part(PrimitiveType.Sphere, _model, "EyeR", new Vector3(0.14f, 1.48f, 0.78f), Vector3.one * 0.07f, eye);
            Part(PrimitiveType.Capsule, _model, "HornL", new Vector3(-0.12f, 1.6f, 0.45f), new Vector3(0.06f, 0.16f, 0.06f), belly, new Vector3(-50f, 0f, 15f));
            Part(PrimitiveType.Capsule, _model, "HornR", new Vector3(0.12f, 1.6f, 0.45f), new Vector3(0.06f, 0.16f, 0.06f), belly, new Vector3(-50f, 0f, -15f));
            if (id.Contains("brood"))
            {
                for (int i = 0; i < 4; i++)
                {
                    Part(PrimitiveType.Cube, _model, "Fin", new Vector3(0f, 0.85f - i * 0.05f, 0.1f - i * 0.32f), new Vector3(0.04f, 0.32f - i * 0.05f, 0.24f), dark, new Vector3(-20f, 0f, 0f));
                }
            }

            if (id.Contains("wyrm"))
            {
                _wings.Add(Part(PrimitiveType.Cube, _model, "WingL", new Vector3(-0.7f, 0.95f, -0.1f), new Vector3(1f, 0.03f, 0.55f), belly));
                _wings.Add(Part(PrimitiveType.Cube, _model, "WingR", new Vector3(0.7f, 0.95f, -0.1f), new Vector3(1f, 0.03f, 0.55f), belly));
            }
        }

        /// <summary>Stacked blocks of ice (Ice Golem) or rune-cut stone (Ancient Golem).</summary>
        private void BuildGolem(MonsterDefinition definition, Color main, Color dark)
        {
            if (definition.SiegeObject)
            {
                BuildSiegeObject(definition, main, dark);
                return;
            }

            bool ancient = definition.Id.Contains("ancient");
            Color rune = ancient ? new Color(0.45f, 0.85f, 1f) : new Color(0.75f, 0.95f, 1f);
            Part(PrimitiveType.Cube, _model, "LegL", new Vector3(-0.22f, 0.26f, 0f), new Vector3(0.3f, 0.52f, 0.35f), dark);
            Part(PrimitiveType.Cube, _model, "LegR", new Vector3(0.22f, 0.26f, 0f), new Vector3(0.3f, 0.52f, 0.35f), dark);
            Part(PrimitiveType.Cube, _model, "Torso", new Vector3(0f, 0.95f, 0f), new Vector3(0.9f, 0.8f, 0.6f), main, new Vector3(0f, 0f, 2f));
            Part(PrimitiveType.Cube, _model, "Head", new Vector3(0f, 1.55f, 0.06f), new Vector3(0.4f, 0.34f, 0.4f), main * 0.95f);
            Part(PrimitiveType.Cube, _model, "ArmL", new Vector3(-0.62f, 0.95f, 0.05f), new Vector3(0.3f, 0.75f, 0.32f), dark, new Vector3(0f, 0f, -6f));
            Part(PrimitiveType.Cube, _model, "ArmR", new Vector3(0.62f, 0.95f, 0.05f), new Vector3(0.3f, 0.75f, 0.32f), dark, new Vector3(0f, 0f, 6f));
            Part(PrimitiveType.Cube, _model, "FistL", new Vector3(-0.66f, 0.48f, 0.1f), new Vector3(0.36f, 0.3f, 0.36f), main);
            Part(PrimitiveType.Cube, _model, "FistR", new Vector3(0.66f, 0.48f, 0.1f), new Vector3(0.36f, 0.3f, 0.36f), main);
            GlowPart(PrimitiveType.Cube, _model, "EyeL", new Vector3(-0.09f, 1.58f, 0.27f), new Vector3(0.08f, 0.05f, 0.02f), rune);
            GlowPart(PrimitiveType.Cube, _model, "EyeR", new Vector3(0.09f, 1.58f, 0.27f), new Vector3(0.08f, 0.05f, 0.02f), rune);
            if (ancient)
            {
                GlowPart(PrimitiveType.Cube, _model, "RuneV", new Vector3(0f, 1f, 0.31f), new Vector3(0.06f, 0.5f, 0.02f), rune);
                GlowPart(PrimitiveType.Cube, _model, "RuneH", new Vector3(0f, 1.08f, 0.31f), new Vector3(0.36f, 0.05f, 0.02f), rune);
                Part(PrimitiveType.Sphere, _model, "Moss", new Vector3(0.25f, 1.38f, 0.05f), new Vector3(0.35f, 0.12f, 0.3f), new Color(0.3f, 0.45f, 0.25f));
            }
            else
            {
                Color shard = Color.Lerp(main, Color.white, 0.4f);
                shard.a = 1f;
                Part(PrimitiveType.Capsule, _model, "ShardL", new Vector3(-0.42f, 1.48f, -0.05f), new Vector3(0.14f, 0.28f, 0.14f), shard, new Vector3(0f, 0f, 25f));
                Part(PrimitiveType.Capsule, _model, "ShardR", new Vector3(0.42f, 1.5f, -0.05f), new Vector3(0.16f, 0.32f, 0.16f), shard, new Vector3(0f, 0f, -25f));
                Part(PrimitiveType.Capsule, _model, "ShardBack", new Vector3(0f, 1.35f, -0.32f), new Vector3(0.14f, 0.3f, 0.14f), shard, new Vector3(-30f, 0f, 0f));
            }
        }

        /// <summary>A hooded robe floating over the ground with glowing eyes; banshees trail long hair.</summary>
        private void BuildWraith(MonsterDefinition definition, Color main, Color dark)
        {
            _flyer = true;
            Color glow = definition.Element == Element.Ghost ? new Color(0.6f, 0.9f, 1f) : new Color(0.85f, 0.6f, 1f);
            Part(PrimitiveType.Capsule, _model, "Robe", new Vector3(0f, 1.05f, 0f), new Vector3(0.62f, 0.62f, 0.5f), main);
            Part(PrimitiveType.Capsule, _model, "Tail", new Vector3(0f, 0.5f, -0.12f), new Vector3(0.4f, 0.4f, 0.32f), dark, new Vector3(-18f, 0f, 0f));
            Part(PrimitiveType.Capsule, _model, "Tatter", new Vector3(0.12f, 0.22f, -0.22f), new Vector3(0.16f, 0.24f, 0.12f), dark, new Vector3(-30f, 0f, 20f));
            Part(PrimitiveType.Sphere, _model, "Hood", new Vector3(0f, 1.62f, 0f), new Vector3(0.48f, 0.5f, 0.48f), dark);
            Part(PrimitiveType.Sphere, _model, "Face", new Vector3(0f, 1.58f, 0.12f), new Vector3(0.3f, 0.32f, 0.25f), new Color(0.04f, 0.04f, 0.08f));
            GlowPart(PrimitiveType.Sphere, _model, "EyeL", new Vector3(-0.07f, 1.62f, 0.25f), Vector3.one * 0.07f, glow);
            GlowPart(PrimitiveType.Sphere, _model, "EyeR", new Vector3(0.07f, 1.62f, 0.25f), Vector3.one * 0.07f, glow);
            Part(PrimitiveType.Capsule, _model, "ArmL", new Vector3(-0.36f, 1.12f, 0.18f), new Vector3(0.1f, 0.3f, 0.1f), main, new Vector3(55f, 0f, -10f));
            Part(PrimitiveType.Capsule, _model, "ArmR", new Vector3(0.36f, 1.12f, 0.18f), new Vector3(0.1f, 0.3f, 0.1f), main, new Vector3(55f, 0f, 10f));
            if (definition.Id.Contains("banshee"))
            {
                Color hair = new Color(0.92f, 0.9f, 0.98f);
                for (int i = -2; i <= 2; i++)
                {
                    Part(PrimitiveType.Cube, _model, "Hair", new Vector3(i * 0.08f, 1.3f, -0.18f), new Vector3(0.05f, 0.7f, 0.03f), hair, new Vector3(-12f, 0f, i * 4f));
                }
            }
        }

        /// <summary>
        /// Phase 8 sieges: a Jarlstone is a golden rune-gem over a stepped plinth with glyphs circling it; a Castle Hoard is an
        /// iron-bound chest with a glowing lock.
        /// </summary>
        private void BuildSiegeObject(MonsterDefinition definition, Color main, Color dark)
        {
            Color stone = new Color(0.36f, 0.35f, 0.38f);
            if (definition.Id == Social.SiegeRules.JarlstoneId)
            {
                Part(PrimitiveType.Cylinder, _model, "Plinth", new Vector3(0f, 0.12f, 0f), new Vector3(1.2f, 0.12f, 1.2f), stone);
                Part(PrimitiveType.Cylinder, _model, "Step", new Vector3(0f, 0.32f, 0f), new Vector3(0.85f, 0.1f, 0.85f), stone * 1.15f);
                Part(PrimitiveType.Cylinder, _model, "Socket", new Vector3(0f, 0.5f, 0f), new Vector3(0.45f, 0.08f, 0.45f), dark);

                // A cube stood on its corner and stretched upwards reads as a cut gem.
                var gem = new GameObject("Gem").transform;
                gem.SetParent(_model, false);
                gem.localPosition = new Vector3(0f, 1.25f, 0f);
                gem.localScale = new Vector3(0.62f, 1.25f, 0.62f);
                gem.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 20f, 0f);
                var facet = GlowPart(PrimitiveType.Cube, gem, "Facet", Vector3.zero, Vector3.one * 0.62f, main);
                facet.localEulerAngles = new Vector3(45f, 0f, 35.26f);

                var orbit = new GameObject("Glyphs").transform;
                orbit.SetParent(_model, false);
                orbit.localPosition = new Vector3(0f, 1.2f, 0f);
                orbit.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, -45f, 0f);
                Color glyph = new Color(1f, 0.93f, 0.6f);
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * 0.5f;
                    GlowPart(PrimitiveType.Cube, orbit, "GlyphV", new Vector3(Mathf.Cos(angle) * 0.75f, 0f, Mathf.Sin(angle) * 0.75f), new Vector3(0.04f, 0.26f, 0.04f), glyph);
                    GlowPart(PrimitiveType.Cube, orbit, "GlyphD", new Vector3(Mathf.Cos(angle) * 0.75f, 0.06f, Mathf.Sin(angle) * 0.75f), new Vector3(0.04f, 0.14f, 0.04f), glyph)
                        .localEulerAngles = new Vector3(0f, -i * 90f, 40f);
                }

                var ring = GroundRing.Create("JarlRing", new Color(1f, 0.82f, 0.3f, 0.9f), 1.1f, 0.08f);
                ring.ShowAt(transform.position);
                ring.transform.SetParent(transform, true);
                ring.SetSpin(15f);
                return;
            }

            Color iron = new Color(0.25f, 0.25f, 0.28f);
            Part(PrimitiveType.Cube, _model, "Chest", new Vector3(0f, 0.3f, 0f), new Vector3(0.95f, 0.6f, 0.62f), main);
            Part(PrimitiveType.Cylinder, _model, "Lid", new Vector3(0f, 0.6f, 0f), new Vector3(0.6f, 0.475f, 0.6f), main * 1.1f, new Vector3(0f, 0f, 90f));
            foreach (float x in new[] { -0.32f, 0.32f })
            {
                Part(PrimitiveType.Cube, _model, "Band", new Vector3(x, 0.3f, 0f), new Vector3(0.07f, 0.62f, 0.64f), iron);
                Part(PrimitiveType.Cylinder, _model, "LidBand", new Vector3(x, 0.6f, 0f), new Vector3(0.62f, 0.035f, 0.62f), iron, new Vector3(0f, 0f, 90f));
            }

            Part(PrimitiveType.Cube, _model, "Plate", new Vector3(0f, 0.55f, 0.32f), new Vector3(0.18f, 0.2f, 0.03f), iron);
            GlowPart(PrimitiveType.Cube, _model, "Lock", new Vector3(0f, 0.53f, 0.34f), new Vector3(0.07f, 0.09f, 0.02f), new Color(1f, 0.8f, 0.3f));
        }

        private static Transform GlowPart(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var part = Part(type, parent, name, localPosition, localScale, color);
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = RuntimeMaterials.Glow(color, color * 1.4f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return part;
        }

        // ------------------------------------------------------------------ animation
        private void Update()
        {
            if (_model == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            _bobPhase += dt * Mathf.Lerp(2f, 11f, Mathf.Clamp01(_speed / 6f));
            _castBlend = Mathf.MoveTowards(_castBlend, _casting ? 1f : 0f, dt * 6f);
            _deadBlend = Mathf.MoveTowards(_deadBlend, _dead ? 1f : 0f, dt * 3f);

            // The rigged body's legs carry the stride, so its hop is smaller than the doll's.
            float bob = _speed > 0.2f ? Mathf.Abs(Mathf.Sin(_bobPhase)) * (_rig != null ? 0.035f : 0.06f) : Mathf.Sin(_bobPhase) * 0.01f;
            float hover = _flyer ? 0.15f + Mathf.Sin(Time.time * 3f) * 0.08f : 0f;

            float attackT = (Time.time - _attackStart) / _attackDuration;
            float lunge = _lungeAttack && attackT >= 0f && attackT <= 1f ? Mathf.Sin(attackT * Mathf.PI) * 0.35f : 0f;

            float shake = Time.time < _hitUntil ? Mathf.Sin(Time.time * 90f) * 6f : 0f;

            // Stagger: rock back and wobble.
            float stagger = Time.time < _staggerUntil ? (_staggerUntil - Time.time) / Combat.PoiseRules.StaggerSeconds : 0f;
            float staggerTilt = -18f * stagger;
            shake += stagger > 0f ? Mathf.Sin(Time.time * 40f) * 8f * stagger : 0f;

            // Skill motions.
            float skillT = (Time.time - _skillStart) / _skillDuration;
            bool skillActive = skillT >= 0f && skillT <= 1f && _deadBlend <= 0f;
            float spin = 0f, jump = 0f, scale = 1f;
            float? weaponOverride = null;
            if (skillActive)
            {
                float arc = Mathf.Sin(skillT * Mathf.PI);
                switch (_skillMotion)
                {
                    case SkillMotion.Spin:
                        spin = 360f * Smooth(skillT);
                        weaponOverride = 270f;
                        break;
                    case SkillMotion.Thrust:
                        lunge += arc * 0.5f;
                        weaponOverride = 270f;
                        break;
                    case SkillMotion.Punch:
                        lunge += arc * 0.4f;
                        weaponOverride = Mathf.Lerp(RestAngle, 280f, arc);
                        break;
                    case SkillMotion.Shoot:
                        lunge -= arc * 0.12f;
                        weaponOverride = 270f;
                        break;
                    case SkillMotion.Leap:
                        jump = arc * 0.9f;
                        weaponOverride = Mathf.Lerp(WindupAngle, StrikeAngle, Smooth(skillT));
                        break;
                    case SkillMotion.Cast:
                        weaponOverride = Mathf.Lerp(RestAngle, 200f, arc);
                        scale = 1f + arc * 0.04f;
                        break;
                    case SkillMotion.Buff:
                        scale = 1f + arc * 0.1f;
                        break;
                }
            }

            float fighting = Time.time < _combatUntil && !_dead ? 1f : 0f;
            _combatBlend = Mathf.MoveTowards(_combatBlend, fighting, dt * 4f);
            if (_rig != null)
            {
                // The weapon sits in the rigged body's hand; the arm swings it, and each attack takes its own step in.
                lunge = _rig.Animate(new RigMotion
                {
                    Time = Time.time,
                    Speed = _speed,
                    StridePhase = _bobPhase,
                    Combat = _combatBlend,
                    AttackT = _deadBlend > 0f ? -1f : attackT,
                    AttackIndex = _attackIndex,
                    Cast = _castBlend,
                    Skill = skillActive ? _skillMotion : SkillMotion.None,
                    SkillT = skillActive ? skillT : -1f,
                    Hit = Time.time < _hitUntil ? (_hitUntil - Time.time) / 0.15f : 0f,
                    Stagger = stagger,
                    Dead = _deadBlend,
                });
            }
            else if (_weaponPivot != null)
            {
                _weaponPivot.localRotation = Quaternion.Euler(weaponOverride ?? WeaponAngle(attackT), 0f, 0f);
            }

            AnimateMount();
            _model.localPosition = new Vector3(0f, _mountHeight + (bob + hover + jump) * (1f - _deadBlend) - _deadBlend * 0.15f, lunge);
            _model.localRotation = Quaternion.Euler(-85f * _deadBlend + staggerTilt, spin, shake);
            // Slimes squash and stretch as they hop.
            float squash = _squash ? Mathf.Sin(_bobPhase * 2f) * (_speed > 0.2f ? 0.12f : 0.035f) * (1f - _deadBlend) : 0f;
            _model.localScale = new Vector3(1f + squash, 1f - squash, 1f + squash) * (_baseScale * scale);

            // Ragnarok's casting circle at the caster's feet.
            if (_castBlend > 0.01f && _castCircle == null)
            {
                _castCircle = CastCircle.Create(transform, new Color(0.55f, 0.85f, 1f, 0.85f));
            }

            if (_castCircle != null)
            {
                _castCircle.SetStrength(_castBlend * (1f - _deadBlend));
            }

            for (int i = 0; i < _wings.Count; i++)
            {
                float flap = Mathf.Sin(Time.time * 18f) * 35f;
                _wings[i].localRotation = Quaternion.Euler(0f, 0f, i == 0 ? flap : -flap);
            }

            float glide = Mathf.Sin(Time.time * (_speed > 0.1f ? 6f : 2.2f)) * 14f;
            for (int i = 0; i < _gearWings.Count; i++)
            {
                float side = i == 0 ? 1f : -1f;
                _gearWings[i].localRotation = Quaternion.Euler(0f, -side * (22f + glide), side * glide * 0.5f); // swept back, gently beating
            }
        }

        /// <summary>Overhead slash: wind up (0–30%), strike (30–55%, impact lands at 50%), recover.</summary>
        private float WeaponAngle(float t)
        {
            if (t < 0f || t > 1f)
            {
                // Casting raises the weapon overhead (staff / mace held up).
                return Mathf.Lerp(RestAngle, 200f, _castBlend);
            }

            if (t < 0.3f)
            {
                return Mathf.Lerp(RestAngle, WindupAngle, Smooth(t / 0.3f));
            }

            if (t < 0.55f)
            {
                return Mathf.Lerp(WindupAngle, StrikeAngle, Smooth((t - 0.3f) / 0.25f));
            }

            return Mathf.Lerp(StrikeAngle, RestAngle, Smooth((t - 0.55f) / 0.45f));
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static Transform Part(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                RiggedBody.Discard(collider);
            }

            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.Euler(euler);
            t.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Lit(color);
            return t;
        }
    }
}
