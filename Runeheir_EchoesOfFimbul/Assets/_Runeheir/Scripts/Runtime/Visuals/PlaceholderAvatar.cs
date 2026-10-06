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
    /// Primitive-built stand-in model with procedural animation (walk bob, ASPD-timed weapon swing,
    /// cast pose, hit shake, death). Lets the whole Phase 2 loop be played and tuned before the
    /// Blender/Krita art lands; swap it for a real model + Animator via <see cref="CharacterAnimationBridge"/>.
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
        private bool _lungeAttack;
        private bool _flyer;
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

        public void SetLocomotion(float metersPerSecond)
        {
            _speed = metersPerSecond;
        }

        /// <summary>Plays one swing lasting <paramref name="duration"/> seconds (shorter at high ASPD).</summary>
        public void PlayAttack(float duration)
        {
            _attackStart = Time.time;
            _attackDuration = Mathf.Max(0.05f, duration);
        }

        public void SetCasting(bool casting)
        {
            _casting = casting;
        }

        public void SetDead(bool dead)
        {
            _dead = dead;
        }

        public void PlayHit()
        {
            _hitUntil = Time.time + 0.15f;
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
        }

        /// <summary>Poise broken: rock back for the stagger.</summary>
        public void PlayStagger()
        {
            _staggerUntil = Time.time + Combat.PoiseRules.StaggerSeconds;
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
                Destroy(_model.gameObject);
            }

            _wings.Clear();
            _gearWings.Clear();
            _weaponPivot = null;
            _model = new GameObject("Model").transform;
            _model.SetParent(transform, false);
        }

        // ------------------------------------------------------------------ humanoid
        private void BuildHumanoid(AvatarLook look)
        {
            ResetModel();
            _lungeAttack = false;
            _flyer = false;

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

            _weaponPivot = new GameObject("WeaponPivot").transform;
            _weaponPivot.SetParent(_model, false);
            _weaponPivot.localPosition = new Vector3(0.34f, 1.12f, 0.05f);
            Part(PrimitiveType.Capsule, _weaponPivot, "ArmR", new Vector3(0f, -0.22f, 0f), new Vector3(0.13f, 0.28f, 0.13f), dark);
            BuildWeapon(look.Weapon);
            _weaponPivot.localRotation = Quaternion.Euler(RestAngle, 0f, 0f);
            BuildGear(look);
        }

        // ------------------------------------------------------------------ worn gear (GDD §5 visible headgear, shields, wings)
        private void BuildGear(AvatarLook look)
        {
            BuildUpperHeadgear(ItemCatalog.Get(look.HeadUpper));
            BuildMidHeadgear(ItemCatalog.Get(look.HeadMid));
            BuildLowerHeadgear(ItemCatalog.Get(look.HeadLower));
            BuildShield(ItemCatalog.Get(look.Shield));
            BuildGarment(ItemCatalog.Get(look.Garment));
        }

        private static Color ViewColor(ItemDefinition item)
        {
            return RuntimeMaterials.Hex(item.ViewColorHex ?? item.IconColorHex ?? "#BDC3C7");
        }

        private void BuildUpperHeadgear(ItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            Color color = ViewColor(item);
            Color trim = Color.Lerp(color, Color.white, 0.4f);
            string id = item.Id;
            if (id.Contains("wizard_hat"))
            {
                Part(PrimitiveType.Cylinder, _model, "HatBrim", new Vector3(0f, 1.7f, 0f), new Vector3(0.7f, 0.02f, 0.7f), color);
                Part(PrimitiveType.Capsule, _model, "HatCone", new Vector3(0f, 1.98f, -0.04f), new Vector3(0.3f, 0.3f, 0.3f), color, new Vector3(-12f, 0f, 0f));
                Part(PrimitiveType.Sphere, _model, "HatTip", new Vector3(0f, 2.22f, -0.12f), Vector3.one * 0.1f, new Color(0.96f, 0.82f, 0.25f));
            }
            else if (id.Contains("crown") || id.Contains("circlet"))
            {
                Part(PrimitiveType.Cylinder, _model, "Band", new Vector3(0f, 1.7f, 0f), new Vector3(0.47f, 0.035f, 0.47f), color);
                if (id.Contains("antler"))
                {
                    Part(PrimitiveType.Cylinder, _model, "AntlerL", new Vector3(-0.2f, 1.9f, 0f), new Vector3(0.04f, 0.2f, 0.04f), color, new Vector3(0f, 0f, 30f));
                    Part(PrimitiveType.Cylinder, _model, "AntlerR", new Vector3(0.2f, 1.9f, 0f), new Vector3(0.04f, 0.2f, 0.04f), color, new Vector3(0f, 0f, -30f));
                }
                else
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        Part(PrimitiveType.Cube, _model, "Point", new Vector3(i * 0.12f, 1.77f, 0.2f), new Vector3(0.05f, 0.1f, 0.03f), trim, new Vector3(0f, 0f, 45f));
                    }
                }
            }
            else if (id.Contains("hood"))
            {
                Part(PrimitiveType.Sphere, _model, "Hood", new Vector3(0f, 1.6f, -0.04f), new Vector3(0.52f, 0.46f, 0.52f), color);
                if (id.Contains("wolf"))
                {
                    Part(PrimitiveType.Cube, _model, "EarL", new Vector3(-0.14f, 1.86f, 0f), new Vector3(0.08f, 0.14f, 0.05f), color);
                    Part(PrimitiveType.Cube, _model, "EarR", new Vector3(0.14f, 1.86f, 0f), new Vector3(0.08f, 0.14f, 0.05f), color);
                }
            }
            else if (id.Contains("beret") || id.Contains("cap") || id.Contains("bandana"))
            {
                Part(PrimitiveType.Sphere, _model, "Cap", new Vector3(0.03f, 1.74f, 0f), new Vector3(0.5f, 0.16f, 0.48f), color, new Vector3(0f, 0f, -8f));
                if (id.Contains("feather"))
                {
                    Part(PrimitiveType.Cube, _model, "Feather", new Vector3(-0.18f, 1.86f, -0.05f), new Vector3(0.03f, 0.22f, 0.06f), Color.white, new Vector3(-20f, 0f, 25f));
                }
            }
            else
            {
                // Helms and crests.
                Part(PrimitiveType.Sphere, _model, "Helm", new Vector3(0f, 1.63f, 0f), new Vector3(0.48f, 0.36f, 0.48f), color);
                if (id.Contains("horned") || id.Contains("viking") || id.Contains("awe"))
                {
                    Part(PrimitiveType.Capsule, _model, "HornL", new Vector3(-0.27f, 1.78f, 0f), new Vector3(0.07f, 0.16f, 0.07f), trim, new Vector3(0f, 0f, 40f));
                    Part(PrimitiveType.Capsule, _model, "HornR", new Vector3(0.27f, 1.78f, 0f), new Vector3(0.07f, 0.16f, 0.07f), trim, new Vector3(0f, 0f, -40f));
                }
                else if (id.Contains("winged"))
                {
                    Part(PrimitiveType.Cube, _model, "WingletL", new Vector3(-0.27f, 1.7f, -0.05f), new Vector3(0.04f, 0.16f, 0.24f), trim, new Vector3(-25f, 0f, 15f));
                    Part(PrimitiveType.Cube, _model, "WingletR", new Vector3(0.27f, 1.7f, -0.05f), new Vector3(0.04f, 0.16f, 0.24f), trim, new Vector3(-25f, 0f, -15f));
                }

                if ((item.Slots & EquipSlot.HeadMid) != 0)
                {
                    Part(PrimitiveType.Cube, _model, "NasalGuard", new Vector3(0f, 1.55f, 0.21f), new Vector3(0.04f, 0.14f, 0.03f), color);
                }
            }
        }

        private void BuildMidHeadgear(ItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            Color color = ViewColor(item);
            if (item.Id.Contains("eyepatch"))
            {
                Part(PrimitiveType.Sphere, _model, "Patch", new Vector3(0.08f, 1.55f, 0.2f), new Vector3(0.1f, 0.09f, 0.04f), color);
            }
            else if (item.Id.Contains("monocle"))
            {
                Part(PrimitiveType.Cylinder, _model, "Monocle", new Vector3(-0.08f, 1.55f, 0.21f), new Vector3(0.1f, 0.01f, 0.1f), color, new Vector3(90f, 0f, 0f));
            }
            else
            {
                Part(PrimitiveType.Cube, _model, "Eyewear", new Vector3(0f, 1.56f, 0.2f), new Vector3(0.34f, 0.07f, 0.04f), color);
            }
        }

        private void BuildLowerHeadgear(ItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            Color color = ViewColor(item);
            if (item.Id.Contains("pipe"))
            {
                Part(PrimitiveType.Cylinder, _model, "Pipe", new Vector3(0.06f, 1.42f, 0.27f), new Vector3(0.03f, 0.08f, 0.03f), color, new Vector3(80f, 0f, 0f));
                Part(PrimitiveType.Cylinder, _model, "Bowl", new Vector3(0.06f, 1.44f, 0.36f), new Vector3(0.07f, 0.05f, 0.07f), color);
            }
            else if (item.Id.Contains("beard"))
            {
                Part(PrimitiveType.Capsule, _model, "Beard", new Vector3(0f, 1.32f, 0.14f), new Vector3(0.22f, 0.16f, 0.12f), color);
            }
            else
            {
                Part(PrimitiveType.Cube, _model, "Mask", new Vector3(0f, 1.43f, 0.19f), new Vector3(0.26f, 0.1f, 0.05f), color);
            }
        }

        private void BuildShield(ItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            Color color = ViewColor(item);
            if (item.Id.Contains("kite"))
            {
                Part(PrimitiveType.Cube, _model, "Shield", new Vector3(-0.44f, 0.92f, 0.08f), new Vector3(0.04f, 0.5f, 0.32f), color);
            }
            else
            {
                Part(PrimitiveType.Cylinder, _model, "Shield", new Vector3(-0.44f, 0.95f, 0.08f), new Vector3(0.44f, 0.025f, 0.44f), color, new Vector3(0f, 0f, 90f));
                Part(PrimitiveType.Sphere, _model, "Boss", new Vector3(-0.47f, 0.95f, 0.08f), Vector3.one * 0.1f, Color.Lerp(color, Color.white, 0.5f));
            }
        }

        private void BuildGarment(ItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            Color color = ViewColor(item);
            if (item.Id.Contains("wings"))
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var pivot = new GameObject(side < 0 ? "WingPivotL" : "WingPivotR").transform;
                    pivot.SetParent(_model, false);
                    pivot.localPosition = new Vector3(side * 0.1f, 1.18f, -0.22f);
                    Part(PrimitiveType.Cube, pivot, "Wing", new Vector3(side * 0.36f, 0.1f, 0f), new Vector3(0.62f, 0.3f, 0.03f), color, new Vector3(0f, 0f, side * 18f));
                    Part(PrimitiveType.Cube, pivot, "WingTip", new Vector3(side * 0.62f, 0.28f, 0f), new Vector3(0.24f, 0.18f, 0.03f), Color.Lerp(color, Color.white, 0.3f), new Vector3(0f, 0f, side * 40f));
                    _gearWings.Add(pivot);
                }
            }
            else if (item.Id.Contains("muffler"))
            {
                Part(PrimitiveType.Cylinder, _model, "Scarf", new Vector3(0f, 1.28f, 0f), new Vector3(0.4f, 0.06f, 0.38f), color);
                Part(PrimitiveType.Cube, _model, "ScarfTail", new Vector3(0.1f, 1.08f, -0.2f), new Vector3(0.1f, 0.34f, 0.03f), color, new Vector3(10f, 0f, 8f));
            }
            else
            {
                Part(PrimitiveType.Cube, _model, "Cape", new Vector3(0f, 0.86f, -0.23f), new Vector3(0.52f, 0.86f, 0.04f), color, new Vector3(8f, 0f, 0f));
            }
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

        private void BuildWeapon(WeaponType weapon)
        {
            Color steel = new Color(0.78f, 0.8f, 0.85f);
            Color wood = new Color(0.45f, 0.3f, 0.16f);
            switch (weapon)
            {
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
            _flyer = definition.Shape == MonsterShape.Flyer;
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
                    _wings.Add(Part(PrimitiveType.Cube, _model, "WingL", new Vector3(-0.42f, 1.25f, -0.05f), new Vector3(0.55f, 0.03f, 0.3f), new Color(0.85f, 1f, 0.85f, 1f)));
                    _wings.Add(Part(PrimitiveType.Cube, _model, "WingR", new Vector3(0.42f, 1.25f, -0.05f), new Vector3(0.55f, 0.03f, 0.3f), new Color(0.85f, 1f, 0.85f, 1f)));
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

            float bob = _speed > 0.2f ? Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.06f : Mathf.Sin(_bobPhase) * 0.01f;
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

            _model.localPosition = new Vector3(0f, (bob + hover + jump) * (1f - _deadBlend) - _deadBlend * 0.15f, lunge);
            _model.localRotation = Quaternion.Euler(-85f * _deadBlend + staggerTilt, spin, shake);
            _model.localScale = Vector3.one * (_baseScale * scale);

            if (_weaponPivot != null)
            {
                _weaponPivot.localRotation = Quaternion.Euler(weaponOverride ?? WeaponAngle(attackT), 0f, 0f);
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
                Destroy(collider);
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
