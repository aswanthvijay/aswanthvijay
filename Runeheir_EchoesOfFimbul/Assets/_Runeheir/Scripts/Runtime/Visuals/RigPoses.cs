using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Skills;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>How a weapon is held and fought with (Ragnarok's per-class attack motions, Norse kit).</summary>
    public enum WeaponStyle
    {
        Fists = 0,
        Kicks = 1,
        OneHanded = 2,
        TwoHanded = 3,
        Spear = 4,
        Staff = 5,
        Bow = 6,
        Gun = 7,
        Katar = 8,
        Book = 9,
        Instrument = 10,
    }

    /// <summary>
    /// The rig's motion library: per weapon style a relaxed idle, a battle-ready stance (Ragnarok's "standby", taken while
    /// fighting), attack combos, a casting pose and the skill motions, plus how the weapon sits in the hand.
    ///
    /// Every pose is a set of rotations (degrees) about the model's own axes, added to each bone's rest rotation:
    /// X+ swings a hanging limb back (an upright spine leans forward), X- raises it forward; Y+ turns the right shoulder
    /// back; Z+ moves a hanging hand toward the character's right (left arm in, right arm out).
    /// Attack clips run 0..1 over the ASPD-scaled swing and land their hit at 0.5, like the combat code.
    /// </summary>
    public static class RigPoses
    {
        public sealed class Pose
        {
            public readonly Dictionary<string, Vector3> Bones = new Dictionary<string, Vector3>();

            public Pose Set(string bone, float x, float y = 0f, float z = 0f)
            {
                Bones[bone] = new Vector3(x, y, z);
                return this;
            }
        }

        public sealed class Clip
        {
            public float[] Times;
            public Pose[] Poses;

            /// <summary>Weapon trail window (clip time), or none when From >= To.</summary>
            public float TrailFrom = 0.3f;
            public float TrailTo = 0.66f;

            /// <summary>How far the body steps in (meters) at the peak.</summary>
            public float Lunge;

            /// <summary>Bow: how far the string is drawn over the clip (0..1), sampled at the clip's times.</summary>
            public float[] Draw;

            /// <summary>Katars: the off hand's trail too.</summary>
            public bool BothHands;
        }

        /// <summary>Which hand(s) hold the weapon, and how it sits in the fist.</summary>
        public struct Hold
        {
            public bool LeftHand;
            public bool BothHands;

            /// <summary>Wrist angle (degrees about the hand's side axis) from the fist's channel: + points the weapon down.</summary>
            public float Tilt;

            /// <summary>Where the other hand grips, along the weapon from the main grip (meters), or NaN for a free hand.</summary>
            public float OffHandAt;

            /// <summary>Distance from the grip to the start and the tip of what leaves a trail (0 = no trail).</summary>
            public float TrailBase;
            public float TrailTip;
        }

        public sealed class Style
        {
            public Pose Idle;
            public Pose Ready;
            public Clip[] Attacks;
            public Pose Cast;
            public Hold Hold;

            /// <summary>How much the arms swing while running (0 for a weapon held in both hands).</summary>
            public float ArmSwingRight = 1f;
            public float ArmSwingLeft = 1f;
        }

        private static readonly Dictionary<WeaponStyle, Style> Styles = new Dictionary<WeaponStyle, Style>();
        private static readonly Dictionary<WeaponType, Hold> Holds = new Dictionary<WeaponType, Hold>();

        public static WeaponStyle StyleOf(WeaponType weapon, JobId? job)
        {
            switch (weapon)
            {
                case WeaponType.Dagger:
                case WeaponType.OneHandSword:
                case WeaponType.Axe:
                case WeaponType.Mace:
                case WeaponType.Whip:
                case WeaponType.Huuma:
                    return WeaponStyle.OneHanded;
                case WeaponType.TwoHandSword:
                case WeaponType.TwoHandAxe:
                    return WeaponStyle.TwoHanded;
                case WeaponType.Spear:
                    return WeaponStyle.Spear;
                case WeaponType.Staff:
                case WeaponType.CatStaff:
                    return WeaponStyle.Staff;
                case WeaponType.Bow:
                    return WeaponStyle.Bow;
                case WeaponType.ThunderRod:
                    return WeaponStyle.Gun;
                case WeaponType.Katar:
                    return WeaponStyle.Katar;
                case WeaponType.Book:
                    return WeaponStyle.Book;
                case WeaponType.Instrument:
                    return WeaponStyle.Instrument;
                default:
                    // Ragnarok's Taekwon line fights with its feet.
                    return job == JobId.GlimaFighter || job == JobId.SolGuardian || job == JobId.FylgjaCaller ? WeaponStyle.Kicks : WeaponStyle.Fists;
            }
        }

        public static Style Get(WeaponStyle style)
        {
            Build();
            return Styles[style];
        }

        public static Hold HoldOf(WeaponType weapon)
        {
            Build();
            return Holds.TryGetValue(weapon, out var hold) ? hold : new Hold { OffHandAt = float.NaN };
        }

        /// <summary>Picks the attack for this swing of the combo (Ragnarok alternates its swings).</summary>
        public static Clip Attack(WeaponStyle style, int index)
        {
            var attacks = Get(style).Attacks;
            return attacks[((index % attacks.Length) + attacks.Length) % attacks.Length];
        }

        /// <summary>The clip a skill motion plays for this style, or null for the generic pose.</summary>
        public static Clip SkillClip(WeaponStyle style, SkillMotion motion)
        {
            Build();
            switch (motion)
            {
                case SkillMotion.Swing:
                    return Get(style).Attacks[0];
                case SkillMotion.Shoot when style == WeaponStyle.Bow || style == WeaponStyle.Gun:
                    return Get(style).Attacks[0];
                case SkillMotion.Shoot:
                    return Throw;
                case SkillMotion.Thrust when style == WeaponStyle.Spear:
                    return Get(style).Attacks[0];
                case SkillMotion.Thrust when style == WeaponStyle.Katar:
                    return Get(style).Attacks[0];
                case SkillMotion.Thrust:
                    return Stab;
                case SkillMotion.Punch when style == WeaponStyle.Kicks:
                    return Get(style).Attacks[1];
                case SkillMotion.Punch:
                    return Get(WeaponStyle.Fists).Attacks[0];
                case SkillMotion.Leap:
                    return style == WeaponStyle.TwoHanded || style == WeaponStyle.Spear ? Get(WeaponStyle.TwoHanded).Attacks[0] : Chop;
                default:
                    return null;
            }
        }

        private static Clip Throw;
        private static Clip Stab;
        private static Clip Chop;

        private static Pose P()
        {
            return new Pose();
        }

        private static Clip C(float lunge, params (float t, Pose pose)[] keys)
        {
            var clip = new Clip { Times = new float[keys.Length], Poses = new Pose[keys.Length], Lunge = lunge };
            for (int i = 0; i < keys.Length; i++)
            {
                clip.Times[i] = keys[i].t;
                clip.Poses[i] = keys[i].pose;
            }

            return clip;
        }

        private static Pose Crouch(Pose p, float k = 1f)
        {
            return p.Set("LeftUpperLeg", -10f * k, 0f, -3f * k).Set("LeftLowerLeg", 16f * k).Set("RightUpperLeg", 6f * k, 0f, 4f * k)
                .Set("RightLowerLeg", 12f * k).Set("LeftFoot", -6f * k).Set("RightFoot", -8f * k).Set("Spine", 6f * k);
        }

        private static void Build()
        {
            if (Styles.Count > 0)
            {
                return;
            }

            float none = float.NaN;
            Holds[WeaponType.Dagger] = new Hold { Tilt = 15f, OffHandAt = none, TrailBase = 0.08f, TrailTip = 0.36f };
            Holds[WeaponType.OneHandSword] = new Hold { Tilt = 15f, OffHandAt = none, TrailBase = 0.12f, TrailTip = 0.86f };
            Holds[WeaponType.Axe] = new Hold { Tilt = 15f, OffHandAt = none, TrailBase = 0.42f, TrailTip = 0.66f };
            Holds[WeaponType.Mace] = new Hold { Tilt = 15f, OffHandAt = none, TrailBase = 0.42f, TrailTip = 0.62f };
            Holds[WeaponType.Whip] = new Hold { Tilt = 30f, OffHandAt = none, TrailBase = 0.2f, TrailTip = 0.55f };
            Holds[WeaponType.Huuma] = new Hold { Tilt = 0f, OffHandAt = none, TrailBase = 0.1f, TrailTip = 0.33f };
            Holds[WeaponType.TwoHandSword] = new Hold { Tilt = 20f, OffHandAt = -0.13f, TrailBase = 0.2f, TrailTip = 1.18f };
            Holds[WeaponType.TwoHandAxe] = new Hold { Tilt = 20f, OffHandAt = -0.2f, TrailBase = 0.85f, TrailTip = 1.25f };
            Holds[WeaponType.Spear] = new Hold { Tilt = 45f, OffHandAt = 0.42f, TrailBase = 1.15f, TrailTip = 1.6f };
            Holds[WeaponType.Staff] = new Hold { Tilt = 10f, OffHandAt = none, TrailBase = 0.9f, TrailTip = 1.18f };
            Holds[WeaponType.CatStaff] = new Hold { Tilt = 10f, OffHandAt = none, TrailBase = 0.75f, TrailTip = 0.97f };
            Holds[WeaponType.Bow] = new Hold { LeftHand = true, Tilt = 0f, OffHandAt = none };
            Holds[WeaponType.ThunderRod] = new Hold { Tilt = 120f, OffHandAt = 0.36f };
            Holds[WeaponType.Knuckle] = new Hold { Tilt = 90f, OffHandAt = none, BothHands = true };
            Holds[WeaponType.Katar] = new Hold { Tilt = 90f, OffHandAt = none, BothHands = true, TrailBase = 0.08f, TrailTip = 0.43f };
            Holds[WeaponType.Book] = new Hold { LeftHand = true, Tilt = -10f, OffHandAt = none };
            Holds[WeaponType.Instrument] = new Hold { LeftHand = true, Tilt = -10f, OffHandAt = none };

            // ---------------------------------------------------------------- one-handed blades, axes, maces
            var one = new Style
            {
                Idle = P().Set("RightUpperArm", -4f).Set("RightLowerArm", -14f).Set("RightHand", 38f).Set("LeftUpperArm", 0f, 0f, 3f),
                Ready = Crouch(P().Set("RightUpperArm", -28f, 0f, 6f).Set("RightLowerArm", -70f).Set("RightHand", 0f)
                    .Set("LeftUpperArm", -22f, 0f, -12f).Set("LeftLowerArm", -55f).Set("Chest", 0f, 12f, 0f).Set("Head", 0f, -10f, 0f)),
                Attacks = new[]
                {
                    // forehand: cocked high over the right shoulder, the arm thrown out across the body, the wrist rolling the
                    // blade forward so it meets the target at the hit
                    C(0.18f, (0f, P()),
                        (0.28f, P().Set("RightUpperArm", -122f, 0f, 34f).Set("RightLowerArm", -30f).Set("RightHand", -20f).Set("Chest", 0f, 28f, 0f).Set("Spine", -4f)),
                        (0.5f, P().Set("RightUpperArm", -56f, 0f, -30f).Set("RightLowerArm", 60f).Set("RightHand", 55f).Set("Chest", 4f, -30f, 0f).Set("Spine", 10f)
                            .Set("LeftUpperLeg", -18f).Set("LeftLowerLeg", 16f).Set("RightUpperLeg", 14f)),
                        (0.64f, P().Set("RightUpperArm", -28f, 0f, -60f).Set("RightLowerArm", 60f).Set("RightHand", 65f).Set("Chest", 4f, -38f, 0f).Set("Spine", 10f)
                            .Set("LeftUpperLeg", -18f).Set("LeftLowerLeg", 16f).Set("RightUpperLeg", 14f)),
                        (1f, P())),
                    // backhand: across to the left, then a flat sweep out to the right
                    C(0.12f, (0f, P()),
                        (0.28f, P().Set("RightUpperArm", -32f, 0f, -66f).Set("RightLowerArm", 10f).Set("RightHand", 40f).Set("Chest", 0f, -36f, 0f)),
                        (0.5f, P().Set("RightUpperArm", -47f, 0f, 39f).Set("RightLowerArm", 60f).Set("RightHand", 65f).Set("Chest", 0f, 28f, 0f).Set("Spine", 6f)
                            .Set("RightUpperLeg", -16f).Set("RightLowerLeg", 14f)),
                        (0.64f, P().Set("RightUpperArm", -37f, 0f, 59f).Set("RightLowerArm", 60f).Set("RightHand", 70f).Set("Chest", 0f, 34f, 0f).Set("Spine", 6f)
                            .Set("RightUpperLeg", -16f).Set("RightLowerLeg", 14f)),
                        (1f, P())),
                    // overhead chop
                    C(0.22f, (0f, P()),
                        (0.28f, P().Set("RightUpperArm", -137f, 0f, 4f).Set("RightLowerArm", -40f).Set("RightHand", -20f).Set("Spine", -10f).Set("Chest", -6f)),
                        (0.5f, P().Set("RightUpperArm", -32f, 0f, -6f).Set("RightLowerArm", 60f).Set("RightHand", 55f).Set("Spine", 16f).Set("Chest", 8f)
                            .Set("LeftUpperLeg", -22f).Set("LeftLowerLeg", 18f).Set("RightUpperLeg", 12f)),
                        (0.64f, P().Set("RightUpperArm", -7f, 0f, -6f).Set("RightLowerArm", 65f).Set("RightHand", 65f).Set("Spine", 14f).Set("Chest", 8f)
                            .Set("LeftUpperLeg", -22f).Set("LeftLowerLeg", 18f).Set("RightUpperLeg", 12f)),
                        (1f, P())),
                },
                Cast = P().Set("RightUpperArm", -40f, 0f, 10f).Set("RightLowerArm", -50f).Set("LeftUpperArm", -80f, 0f, 8f).Set("LeftLowerArm", -15f).Set("Chest", -3f),
                ArmSwingRight = 0.6f,
            };
            Styles[WeaponStyle.OneHanded] = one;

            // ---------------------------------------------------------------- two-handed swords and axes
            Styles[WeaponStyle.TwoHanded] = new Style
            {
                Idle = P().Set("RightUpperArm", -10f, 0f, -12f).Set("RightLowerArm", -36f).Set("RightHand", 30f).Set("LeftUpperArm", -10f, 0f, 14f)
                    .Set("LeftLowerArm", -40f),
                Ready = Crouch(P().Set("RightUpperArm", -26f, 0f, -16f).Set("RightLowerArm", -64f).Set("RightHand", 8f).Set("LeftUpperArm", -28f, 0f, 22f)
                    .Set("LeftLowerArm", -60f).Set("Chest", 0f, 18f, 0f).Set("Head", 0f, -14f, 0f), 1.2f),
                Attacks = new[]
                {
                    // the great diagonal: over the right shoulder and down through the target
                    C(0.25f, (0f, P()),
                        (0.3f, P().Set("RightUpperArm", -124f, 0f, 50f).Set("RightLowerArm", -36f).Set("RightHand", -23f).Set("Chest", -2f, 40f, 0f).Set("Spine", -6f)),
                        (0.5f, P().Set("RightUpperArm", -44f, 0f, -34f).Set("RightLowerArm", 54f).Set("RightHand", 47f).Set("Chest", 6f, -45f, 0f).Set("Spine", 14f)
                            .Set("LeftUpperLeg", -24f).Set("LeftLowerLeg", 22f).Set("RightUpperLeg", 16f)),
                        (0.66f, P().Set("RightUpperArm", -19f, 0f, -49f).Set("RightLowerArm", 59f).Set("RightHand", 57f).Set("Chest", 6f, -55f, 0f).Set("Spine", 14f)
                            .Set("LeftUpperLeg", -24f).Set("LeftLowerLeg", 22f).Set("RightUpperLeg", 16f)),
                        (1f, P())),
                    // rising: low on the left, up and away to the right
                    C(0.15f, (0f, P()),
                        (0.3f, P().Set("RightUpperArm", 6f, 0f, -39f).Set("RightLowerArm", 44f).Set("RightHand", 52f).Set("Chest", 4f, -40f, 0f).Set("Spine", 10f)),
                        (0.5f, P().Set("RightUpperArm", -114f, 0f, 46f).Set("RightLowerArm", 24f).Set("RightHand", -8f).Set("Chest", -4f, 34f, 0f).Set("Spine", -6f)
                            .Set("RightUpperLeg", -18f).Set("RightLowerLeg", 16f)),
                        (0.66f, P().Set("RightUpperArm", -134f, 0f, 51f).Set("RightLowerArm", 14f).Set("RightHand", -18f).Set("Chest", -4f, 40f, 0f).Set("Spine", -6f)
                            .Set("RightUpperLeg", -18f).Set("RightLowerLeg", 16f)),
                        (1f, P())),
                },
                Cast = P().Set("RightUpperArm", -30f, 0f, -10f).Set("RightLowerArm", -60f).Set("Chest", -3f),
                ArmSwingRight = 0.15f,
                ArmSwingLeft = 0.15f,
            };

            // ---------------------------------------------------------------- spears: held low, thrust and sweep
            Styles[WeaponStyle.Spear] = new Style
            {
                Idle = P().Set("RightUpperArm", -6f, 0f, 2f).Set("RightLowerArm", -46f).Set("RightHand", -62f),
                Ready = Crouch(P().Set("RightUpperArm", -18f, 0f, -4f).Set("RightLowerArm", -27f).Set("RightHand", -20f).Set("LeftUpperArm", -40f, 0f, 24f)
                    .Set("LeftLowerArm", -45f).Set("Chest", 0f, 26f, 0f).Set("Head", 0f, -20f, 0f).Set("Hips", 0f, 10f, 0f), 1.2f),
                Attacks = new[]
                {
                    // the thrust: drawn back, then driven straight out (the wrist keeps the point level)
                    C(0.38f, (0f, P()),
                        (0.3f, P().Set("RightUpperArm", 24f).Set("RightHand", -6f).Set("Chest", 0f, 34f, 0f).Set("Spine", -4f)),
                        (0.5f, P().Set("RightUpperArm", -52f, 0f, -4f).Set("RightLowerArm", 6f).Set("RightHand", 45f).Set("Chest", 0f, -12f, 0f).Set("Spine", 10f)
                            .Set("LeftUpperLeg", -26f).Set("LeftLowerLeg", 20f).Set("RightUpperLeg", 18f)),
                        (0.66f, P().Set("RightUpperArm", -52f, 0f, -4f).Set("RightLowerArm", 6f).Set("RightHand", 45f).Set("Chest", 0f, -12f, 0f).Set("Spine", 10f)
                            .Set("LeftUpperLeg", -26f).Set("LeftLowerLeg", 20f).Set("RightUpperLeg", 18f)),
                        (1f, P())),
                    // the sweep: the head of the spear swung flat across the front
                    C(0.12f, (0f, P()),
                        (0.3f, P().Set("RightUpperArm", -36f, 0f, 42f).Set("RightHand", 20f).Set("Chest", 0f, 36f, 0f)),
                        (0.5f, P().Set("RightUpperArm", -40f, 0f, -42f).Set("RightHand", 30f).Set("Chest", 0f, -40f, 0f).Set("Spine", 8f).Set("LeftUpperLeg", -16f)
                            .Set("LeftLowerLeg", 14f)),
                        (0.66f, P().Set("RightUpperArm", -36f, 0f, -52f).Set("RightHand", 30f).Set("Chest", 0f, -46f, 0f).Set("Spine", 8f).Set("LeftUpperLeg", -16f)
                            .Set("LeftLowerLeg", 14f)),
                        (1f, P())),
                },
                Cast = P().Set("RightUpperArm", -10f).Set("RightLowerArm", -50f).Set("LeftUpperArm", -80f, 0f, 6f).Set("LeftLowerArm", -12f),
                ArmSwingRight = 0.15f,
                ArmSwingLeft = 0.15f,
            };
            Styles[WeaponStyle.Spear].Attacks[0].TrailFrom = 1f; // a thrust leaves no arc

            // ---------------------------------------------------------------- staves: planted at rest, swung, raised to cast
            Styles[WeaponStyle.Staff] = new Style
            {
                Idle = P().Set("RightUpperArm", -8f, 0f, 4f).Set("RightLowerArm", -84f),
                Ready = Crouch(P().Set("RightUpperArm", -24f, 0f, -4f).Set("RightLowerArm", -74f).Set("RightHand", 14f).Set("LeftUpperArm", -32f, 0f, 6f)
                    .Set("LeftLowerArm", -42f).Set("Chest", 0f, 10f, 0f), 0.6f),
                Attacks = new[]
                {
                    C(0.16f, (0f, P()),
                        (0.3f, P().Set("RightUpperArm", -108f, 0f, 22f).Set("RightLowerArm", -38f).Set("Chest", -2f, 22f, 0f).Set("Spine", -6f)),
                        // the head comes down onto the target at chest height, not into the ground
                        (0.5f, P().Set("RightUpperArm", 2f, 0f, -14f).Set("RightLowerArm", 60f).Set("RightHand", -14f).Set("Chest", 6f, -22f, 0f).Set("Spine", 8f)
                            .Set("LeftUpperLeg", -16f).Set("LeftLowerLeg", 14f)),
                        (0.64f, P().Set("RightUpperArm", 0f, 0f, -16f).Set("RightLowerArm", 60f).Set("RightHand", -16f).Set("Chest", 6f, -26f, 0f).Set("Spine", 8f)
                            .Set("LeftUpperLeg", -16f).Set("LeftLowerLeg", 14f)),
                        (1f, P())),
                    C(0.1f, (0f, P()),
                        (0.3f, P().Set("RightUpperArm", -40f, 0f, -48f).Set("RightLowerArm", 22f).Set("Chest", 0f, -30f, 0f)),
                        (0.5f, P().Set("RightUpperArm", -52f, 0f, 52f).Set("RightLowerArm", 30f).Set("Chest", 0f, 30f, 0f).Set("Spine", 6f)),
                        (0.64f, P().Set("RightUpperArm", -48f, 0f, 64f).Set("RightLowerArm", 34f).Set("Chest", 0f, 36f, 0f).Set("Spine", 6f)),
                        (1f, P())),
                },
                // Ragnarok's casting: the staff raised toward the target, the free hand open, eyes up.
                Cast = P().Set("RightUpperArm", -96f, 0f, 8f).Set("RightLowerArm", -20f).Set("RightHand", 20f).Set("LeftUpperArm", -78f, 0f, 10f)
                    .Set("LeftLowerArm", -14f).Set("LeftHand", -30f).Set("Chest", -4f).Set("Head", -6f),
                ArmSwingRight = 0.3f,
            };

            // ---------------------------------------------------------------- bows: drawn to the cheek, loosed on the hit frame
            var draw = C(0f, (0f, P()),
                (0.14f, P().Set("LeftUpperArm", -52f, -12f, -14f).Set("LeftLowerArm", 50f).Set("Chest", 0f, 28f, 0f).Set("Hips", 0f, 14f, 0f).Set("Head", 0f, -34f, 0f)),
                (0.46f, P().Set("LeftUpperArm", -52f, -12f, -14f).Set("LeftLowerArm", 50f).Set("Chest", -2f, 32f, 0f).Set("Hips", 0f, 14f, 0f).Set("Head", 0f, -36f, 0f)),
                (0.52f, P().Set("LeftUpperArm", -50f, -12f, -14f).Set("LeftLowerArm", 50f).Set("Chest", -2f, 32f, 0f).Set("Hips", 0f, 14f, 0f).Set("Head", 0f, -36f, 0f)
                    .Set("RightUpperArm", -62f, 0f, 44f).Set("RightLowerArm", -96f)),
                (0.8f, P().Set("LeftUpperArm", -48f, -12f, -14f).Set("LeftLowerArm", 50f).Set("Chest", 0f, 26f, 0f).Set("Hips", 0f, 12f, 0f).Set("Head", 0f, -30f, 0f)
                    .Set("RightUpperArm", -30f, 0f, 30f).Set("RightLowerArm", -80f)),
                (1f, P()));
            draw.TrailFrom = 1f;
            draw.Draw = new[] { 0f, 0.15f, 1f, 0f, 0f, 0f };
            Styles[WeaponStyle.Bow] = new Style
            {
                Idle = P().Set("LeftUpperArm", -10f, 0f, 8f).Set("LeftLowerArm", -55f),
                Ready = Crouch(P().Set("LeftUpperArm", -38f, 0f, 2f).Set("LeftLowerArm", -42f).Set("RightUpperArm", -22f, 0f, -8f).Set("RightLowerArm", -60f)
                    .Set("Chest", 0f, 14f, 0f), 0.6f),
                Attacks = new[] { draw },
                Cast = P().Set("LeftUpperArm", -20f, 0f, 6f).Set("LeftLowerArm", -60f).Set("RightUpperArm", -90f, 0f, 10f).Set("RightLowerArm", -20f),
                ArmSwingLeft = 0.25f,
            };

            // ---------------------------------------------------------------- thunder-rods: shouldered, a kick on the shot
            var aim = P().Set("RightUpperArm", -58f, 0f, 30f).Set("RightLowerArm", -72f).Set("Chest", 0f, 22f, 0f).Set("Head", 0f, -16f, 4f);
            Styles[WeaponStyle.Gun] = new Style
            {
                Idle = P().Set("RightUpperArm", -4f).Set("RightLowerArm", -18f),
                Ready = Crouch(P().Set("RightUpperArm", -58f, 0f, 30f).Set("RightLowerArm", -72f).Set("Chest", 0f, 22f, 0f).Set("Head", 0f, -16f, 4f), 0.7f),
                Attacks = new[]
                {
                    C(-0.06f, (0f, P()), (0.46f, P()),
                        (0.5f, P().Set("RightUpperArm", 14f).Set("RightLowerArm", -8f).Set("Chest", -8f).Set("Spine", -4f)),
                        (0.7f, P().Set("RightUpperArm", 4f).Set("Chest", -2f)),
                        (1f, P())),
                },
                Cast = aim,
                ArmSwingRight = 0.15f,
                ArmSwingLeft = 0.15f,
            };
            Styles[WeaponStyle.Gun].Attacks[0].TrailFrom = 1f;

            // ---------------------------------------------------------------- fists: a boxer's guard, jab, cross, hook
            Styles[WeaponStyle.Fists] = new Style
            {
                Idle = P().Set("RightLowerArm", -12f).Set("LeftLowerArm", -12f),
                Ready = Crouch(P().Set("RightUpperArm", -26f, 0f, -14f).Set("RightLowerArm", -112f).Set("LeftUpperArm", -34f, 0f, 16f).Set("LeftLowerArm", -114f)
                    .Set("Chest", 0f, 14f, 0f).Set("Head", 4f, -10f, 0f)),
                Attacks = new[]
                {
                    C(0.16f, (0f, P()), (0.3f, P().Set("RightUpperArm", 10f).Set("Chest", 0f, 12f, 0f)),
                        (0.5f, P().Set("RightUpperArm", -60f, 0f, 12f).Set("RightLowerArm", 104f).Set("Chest", 0f, -30f, 0f).Set("Spine", 8f).Set("RightFoot", -10f)),
                        (0.62f, P().Set("RightUpperArm", -60f, 0f, 12f).Set("RightLowerArm", 104f).Set("Chest", 0f, -30f, 0f).Set("Spine", 8f)),
                        (1f, P())),
                    C(0.12f, (0f, P()), (0.3f, P().Set("LeftUpperArm", 8f)),
                        (0.5f, P().Set("LeftUpperArm", -54f, 0f, -14f).Set("LeftLowerArm", 104f).Set("Chest", 0f, 16f, 0f).Set("Spine", 6f)),
                        (0.62f, P().Set("LeftUpperArm", -54f, 0f, -14f).Set("LeftLowerArm", 104f).Set("Chest", 0f, 16f, 0f)),
                        (1f, P())),
                    C(0.14f, (0f, P()), (0.3f, P().Set("RightUpperArm", -40f, 0f, 46f).Set("RightLowerArm", 40f).Set("Chest", 0f, 22f, 0f)),
                        (0.5f, P().Set("RightUpperArm", -64f, 0f, -24f).Set("RightLowerArm", 64f).Set("Chest", 0f, -38f, 0f).Set("Spine", 8f)),
                        (0.62f, P().Set("RightUpperArm", -60f, 0f, -30f).Set("RightLowerArm", 64f).Set("Chest", 0f, -42f, 0f)),
                        (1f, P())),
                },
                Cast = P().Set("RightUpperArm", -80f, 0f, 8f).Set("RightLowerArm", -15f).Set("LeftUpperArm", -80f, 0f, -8f).Set("LeftLowerArm", -15f),
            };
            foreach (var clip in Styles[WeaponStyle.Fists].Attacks)
            {
                clip.TrailFrom = 1f;
            }

            // ---------------------------------------------------------------- Glíma: roundhouse, front kick, heel drop
            Styles[WeaponStyle.Kicks] = new Style
            {
                Idle = Styles[WeaponStyle.Fists].Idle,
                Ready = Crouch(P().Set("RightUpperArm", -22f, 0f, -10f).Set("RightLowerArm", -100f).Set("LeftUpperArm", -30f, 0f, 14f).Set("LeftLowerArm", -104f)
                    .Set("Chest", 0f, 10f, 0f), 1.1f),
                Attacks = new[]
                {
                    C(0.1f, (0f, P()),
                        (0.3f, P().Set("RightUpperLeg", -48f, 0f, 30f).Set("RightLowerLeg", 72f).Set("Spine", -6f).Set("LeftUpperArm", 0f, 0f, -20f)),
                        (0.5f, P().Set("RightUpperLeg", -78f, 0f, 52f).Set("RightLowerLeg", 6f).Set("RightFoot", 30f).Set("Spine", -12f).Set("Chest", 0f, -22f, 0f)
                            .Set("LeftUpperArm", 0f, 0f, -30f).Set("RightUpperArm", 20f, 0f, 30f)),
                        (0.64f, P().Set("RightUpperLeg", -70f, 0f, 46f).Set("RightLowerLeg", 20f).Set("Spine", -10f).Set("Chest", 0f, -22f, 0f)),
                        (1f, P())),
                    C(0.16f, (0f, P()),
                        (0.3f, P().Set("LeftUpperLeg", -66f).Set("LeftLowerLeg", 96f).Set("Spine", -4f)),
                        (0.5f, P().Set("LeftUpperLeg", -86f).Set("LeftLowerLeg", 4f).Set("LeftFoot", 40f).Set("Spine", -12f).Set("RightUpperLeg", 6f).Set("RightLowerLeg", 10f)),
                        (0.62f, P().Set("LeftUpperLeg", -80f).Set("LeftLowerLeg", 14f).Set("Spine", -10f)),
                        (1f, P())),
                    C(0.12f, (0f, P()),
                        (0.3f, P().Set("RightUpperLeg", -118f, 0f, 8f).Set("RightLowerLeg", 6f).Set("Spine", -14f).Set("LeftLowerLeg", 10f)),
                        (0.5f, P().Set("RightUpperLeg", -30f).Set("RightLowerLeg", 8f).Set("RightFoot", 20f).Set("Spine", 14f).Set("LeftLowerLeg", 22f)),
                        (0.64f, P().Set("RightUpperLeg", -24f).Set("RightLowerLeg", 10f).Set("Spine", 12f).Set("LeftLowerLeg", 22f)),
                        (1f, P())),
                },
                Cast = Styles[WeaponStyle.Fists].Cast,
            };
            foreach (var clip in Styles[WeaponStyle.Kicks].Attacks)
            {
                clip.TrailFrom = 1f;
            }

            // ---------------------------------------------------------------- katars: twin stabs from a low crouch
            Styles[WeaponStyle.Katar] = new Style
            {
                Idle = P().Set("RightLowerArm", -20f).Set("LeftLowerArm", -20f),
                Ready = Crouch(P().Set("RightUpperArm", -36f, 0f, -8f).Set("RightLowerArm", -58f).Set("LeftUpperArm", -36f, 0f, 8f).Set("LeftLowerArm", -58f)
                    .Set("Spine", 10f).Set("Head", -8f), 1.4f),
                Attacks = new[]
                {
                    C(0.28f, (0f, P()),
                        (0.3f, P().Set("RightUpperArm", 14f).Set("RightLowerArm", -40f).Set("LeftUpperArm", 14f).Set("LeftLowerArm", -40f)),
                        (0.5f, P().Set("RightUpperArm", -40f).Set("RightLowerArm", 52f).Set("LeftUpperArm", -40f).Set("LeftLowerArm", 52f).Set("Spine", 10f)),
                        (0.62f, P().Set("RightUpperArm", -40f).Set("RightLowerArm", 52f).Set("LeftUpperArm", -40f).Set("LeftLowerArm", 52f).Set("Spine", 10f)),
                        (1f, P())),
                    C(0.2f, (0f, P()),
                        (0.3f, P().Set("RightUpperArm", -60f, 0f, 40f).Set("RightLowerArm", 20f).Set("Chest", 0f, 26f, 0f)),
                        (0.5f, P().Set("RightUpperArm", -50f, 0f, -36f).Set("RightLowerArm", 40f).Set("Chest", 0f, -26f, 0f).Set("LeftUpperArm", -30f, 0f, -30f)),
                        (0.66f, P().Set("LeftUpperArm", -50f, 0f, 36f).Set("LeftLowerArm", 40f).Set("Chest", 0f, 22f, 0f)),
                        (1f, P())),
                },
                Cast = Styles[WeaponStyle.Fists].Cast,
            };
            foreach (var clip in Styles[WeaponStyle.Katar].Attacks)
            {
                clip.BothHands = true;
            }

            // ---------------------------------------------------------------- tomes: the book held up, a palm loosed
            Styles[WeaponStyle.Book] = new Style
            {
                Idle = P().Set("LeftUpperArm", -10f, 0f, 10f).Set("LeftLowerArm", -60f),
                Ready = Crouch(P().Set("LeftUpperArm", -26f, 0f, 12f).Set("LeftLowerArm", -72f).Set("RightUpperArm", -30f, 0f, -6f).Set("RightLowerArm", -54f), 0.5f),
                Attacks = new[]
                {
                    C(0.12f, (0f, P()), (0.3f, P().Set("RightUpperArm", 14f).Set("RightLowerArm", -30f).Set("Chest", 0f, 16f, 0f)),
                        (0.5f, P().Set("RightUpperArm", -56f, 0f, -8f).Set("RightLowerArm", 50f).Set("RightHand", -40f).Set("Chest", 0f, -16f, 0f).Set("Spine", 6f)),
                        (0.64f, P().Set("RightUpperArm", -56f, 0f, -8f).Set("RightLowerArm", 50f).Set("RightHand", -40f).Set("Chest", 0f, -16f, 0f)),
                        (1f, P())),
                },
                Cast = P().Set("LeftUpperArm", -28f, 0f, 12f).Set("LeftLowerArm", -76f).Set("RightUpperArm", -112f, 0f, 12f).Set("RightLowerArm", -18f)
                    .Set("RightHand", -30f).Set("Head", -6f),
            };
            Styles[WeaponStyle.Book].Attacks[0].TrailFrom = 1f;

            // ---------------------------------------------------------------- lyres: held to the chest and struck
            Styles[WeaponStyle.Instrument] = new Style
            {
                Idle = P().Set("LeftUpperArm", -8f, 0f, 12f).Set("LeftLowerArm", -70f),
                Ready = Crouch(P().Set("LeftUpperArm", -20f, 0f, 16f).Set("LeftLowerArm", -80f).Set("RightUpperArm", -32f, 0f, -22f).Set("RightLowerArm", -70f), 0.4f),
                Attacks = new[]
                {
                    C(0.08f, (0f, P()), (0.3f, P().Set("RightUpperArm", -10f, 0f, 18f).Set("RightLowerArm", -10f)),
                        (0.5f, P().Set("RightUpperArm", 6f, 0f, -16f).Set("RightLowerArm", 26f).Set("Spine", 6f).Set("Chest", 0f, -10f, 0f)),
                        (0.64f, P().Set("RightUpperArm", 6f, 0f, -16f).Set("RightLowerArm", 26f)), (1f, P())),
                },
                Cast = P().Set("LeftUpperArm", -20f, 0f, 16f).Set("LeftLowerArm", -80f).Set("RightUpperArm", -32f, 0f, -22f).Set("RightLowerArm", -70f),
            };
            Styles[WeaponStyle.Instrument].Attacks[0].TrailFrom = 1f;

            // ---------------------------------------------------------------- skill motions shared by every style
            Throw = C(0.08f, (0f, P()),
                (0.3f, P().Set("RightUpperArm", -150f, 0f, 20f).Set("RightLowerArm", -60f).Set("Chest", 0f, 30f, 0f).Set("Spine", -6f)),
                (0.5f, P().Set("RightUpperArm", -60f, 0f, -10f).Set("RightLowerArm", 10f).Set("Chest", 0f, -28f, 0f).Set("Spine", 12f).Set("LeftUpperLeg", -16f)),
                (0.66f, P().Set("RightUpperArm", -40f, 0f, -16f).Set("RightLowerArm", 14f).Set("Chest", 0f, -32f, 0f).Set("Spine", 12f)),
                (1f, P()));
            Throw.TrailFrom = 1f;
            Stab = C(0.36f, (0f, P()),
                (0.3f, P().Set("RightUpperArm", 18f).Set("RightLowerArm", -36f).Set("RightHand", -30f).Set("Chest", 0f, 24f, 0f)),
                (0.5f, P().Set("RightUpperArm", -58f).Set("RightLowerArm", 56f).Set("RightHand", 64f).Set("Chest", 0f, -18f, 0f).Set("Spine", 12f)
                    .Set("LeftUpperLeg", -26f).Set("LeftLowerLeg", 22f).Set("RightUpperLeg", 18f)),
                (0.66f, P().Set("RightUpperArm", -58f).Set("RightLowerArm", 56f).Set("RightHand", 64f).Set("Chest", 0f, -18f, 0f).Set("Spine", 12f)
                    .Set("LeftUpperLeg", -26f).Set("LeftLowerLeg", 22f).Set("RightUpperLeg", 18f)),
                (1f, P()));
            Stab.TrailFrom = 1f;
            Chop = one.Attacks[2];
        }

        /// <summary>Adds clip <paramref name="clip"/> at time <paramref name="t"/>, eased between keys (strikes snap).</summary>
        public static void Sample(Clip clip, float t, float weight, System.Action<string, Vector3> add)
        {
            if (clip == null || weight <= 0f || t < 0f || t > 1f)
            {
                return;
            }

            int i = 0;
            while (i < clip.Times.Length - 2 && t > clip.Times[i + 1])
            {
                i++;
            }

            float t0 = clip.Times[i], t1 = clip.Times[i + 1];
            float u = t1 > t0 ? Mathf.Clamp01((t - t0) / (t1 - t0)) : 1f;
            // Into the hit frame the motion snaps (fast out, slow in); everything else eases both ways.
            u = t1 >= 0.5f && t0 < 0.5f ? 1f - (1f - u) * (1f - u) * (1f - u) : u * u * (3f - 2f * u);
            var a = clip.Poses[i].Bones;
            var b = clip.Poses[i + 1].Bones;
            foreach (var pair in a)
            {
                b.TryGetValue(pair.Key, out var to);
                add(pair.Key, Vector3.Lerp(pair.Value, to, u) * weight);
            }

            foreach (var pair in b)
            {
                if (!a.ContainsKey(pair.Key))
                {
                    add(pair.Key, pair.Value * (u * weight));
                }
            }
        }

        /// <summary>A clip's lunge (meters forward) at <paramref name="t"/>: up to the hit, held, then back.</summary>
        public static float LungeAt(Clip clip, float t)
        {
            if (clip == null || clip.Lunge == 0f || t < 0f || t > 1f)
            {
                return 0f;
            }

            float k = t < 0.5f ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.5f, t)) : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.66f, 1f, t));
            return clip.Lunge * k;
        }

        /// <summary>Bow draw (0..1) at <paramref name="t"/>.</summary>
        public static float DrawAt(Clip clip, float t)
        {
            if (clip?.Draw == null || t < 0f || t > 1f)
            {
                return 0f;
            }

            int i = 0;
            while (i < clip.Times.Length - 2 && t > clip.Times[i + 1])
            {
                i++;
            }

            float u = Mathf.Clamp01((t - clip.Times[i]) / Mathf.Max(1e-4f, clip.Times[i + 1] - clip.Times[i]));
            return Mathf.Lerp(clip.Draw[i], clip.Draw[i + 1], u);
        }

        public static void AddPose(Pose pose, float weight, System.Action<string, Vector3> add)
        {
            if (pose == null || weight <= 0f)
            {
                return;
            }

            foreach (var pair in pose.Bones)
            {
                add(pair.Key, pair.Value * weight);
            }
        }
    }
}
