using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Skills;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>What the rig should be doing this frame (filled by <see cref="PlaceholderAvatar"/>).</summary>
    public struct RigMotion
    {
        public float Time;
        public float Speed;
        public float StridePhase;

        /// <summary>0 relaxed, 1 in the battle stance (Ragnarok's "standby", taken while fighting).</summary>
        public float Combat;

        /// <summary>0..1 through the current basic attack, or outside that range when not attacking.</summary>
        public float AttackT;

        /// <summary>Which swing of the combo this is.</summary>
        public int AttackIndex;

        public float Cast;
        public SkillMotion Skill;

        /// <summary>0..1 through the current skill motion, or outside that range.</summary>
        public float SkillT;

        public float Hit;
        public float Stagger;
        public float Dead;
    }

    /// <summary>
    /// A Blender-built rigged character (Resources/Characters, see <see cref="OutfitCatalog"/>) inside the avatar's Model
    /// transform. It paints its material slots through the toon shader, puts on the hair and the weapon (in the fist, the
    /// bow in the left hand), and gives worn gear somewhere to hang (sockets that keep the gear builders' coordinates).
    /// Each frame it poses the skeleton from <see cref="RigPoses"/>: stance, run, attack combo, cast, skill motions,
    /// flinch, stagger and death, then two-bone IK puts the second hand on two-handed weapons and draws the bowstring.
    ///
    /// Poses are rotations about the model's own axes (X right, Y up, Z forward) applied on top of each bone's rest pose,
    /// so they don't depend on how Blender oriented the bones. A real Animator Controller can replace this later
    /// (CharacterAnimationBridge already drives one).
    /// </summary>
    public sealed class RiggedBody
    {
        public static readonly string[] BoneNames =
        {
            "Hips", "Spine", "Chest", "Neck", "Head",
            "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
            "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
            "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
            "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes",
        };

        // The gear builders' doll: head centre, eye and mouth points, cape top, shield, neck and tail (the old capsule model).
        private static readonly Vector3 LegacyHead = new Vector3(0f, 1.52f, 0f);
        private static readonly Vector3 LegacyEyes = new Vector3(0f, 1.555f, 0.21f);
        private static readonly Vector3 LegacyMouth = new Vector3(0f, 1.43f, 0.19f);
        private static readonly Vector3 LegacyBack = new Vector3(0f, 1.29f, -0.23f);
        private static readonly Vector3 LegacyNeck = new Vector3(0f, 1.28f, 0f);
        private static readonly Vector3 LegacyShield = new Vector3(-0.44f, 0.95f, 0.08f);
        private static readonly Vector3 LegacyTail = new Vector3(0f, 0.62f, -0.2f);

        /// <summary>The new skull is about half the old sphere's width.</summary>
        public const float HeadGearScale = 0.56f;

        /// <summary>Where a weapon's grip sits along the old weapon builders' -Y axis.</summary>
        private const float LegacyGripOffset = 0.42f;

        /// <summary>How far behind the bow the drawing hand comes (a full draw to the cheek).</summary>
        private const float DrawLength = 0.6f;

        private readonly Transform _frame;
        private readonly Dictionary<string, Joint> _bones = new Dictionary<string, Joint>();
        private readonly List<Joint> _boneList = new List<Joint>();
        private readonly List<WeaponTrail> _trails = new List<WeaponTrail>();
        private RigPoses.Style _style;
        private RigPoses.Hold _hold;
        private Transform _mainGrip;
        private BowParts _bow;

        private sealed class Joint
        {
            public Transform T;
            public Quaternion RestLocal;
            public Quaternion RestModel;
            public Quaternion RestModelInverse;
            public Vector3 Euler;
        }

        private sealed class BowParts
        {
            public Transform Root;
            public GameObject String;
            public Transform Arrow;
            public Quaternion ArrowRestRotation;
            public Vector3 TipA;
            public Vector3 TipB;
            public Vector3 Nock;
            public LineRenderer Drawn;
        }

        private RiggedBody(Transform frame, Transform root)
        {
            _frame = frame;
            Root = root;
        }

        public Transform Root { get; }

        public OutfitCatalog.Outfit Outfit { get; private set; }

        public WeaponStyle Style { get; private set; }

        public Transform HeadUpper { get; private set; }
        public Transform HeadMid { get; private set; }
        public Transform HeadLower { get; private set; }
        public Transform Back { get; private set; }
        public Transform NeckRing { get; private set; }
        public Transform ShieldArm { get; private set; }
        public Transform Tail { get; private set; }

        /// <summary>The weapon's grip in the main hand (its +Y runs to the weapon's tip), or null for bare hands.</summary>
        public Transform WeaponGrip => _mainGrip;

        /// <summary>How far the bow is drawn right now (0..1).</summary>
        public float BowDraw { get; private set; }

        public Transform Bone(string name)
        {
            return _bones.TryGetValue(name, out var bone) ? bone.T : null;
        }

        /// <summary>Builds the body for <paramref name="look"/> under <paramref name="frame"/>, or null when the models aren't there.</summary>
        public static RiggedBody Create(Transform frame, AvatarLook look)
        {
            var outfit = OutfitCatalog.ForJob(look.Job);
            var prefab = OutfitCatalog.Model(outfit, look.Gender);
            if (prefab == null)
            {
                return null;
            }

            var root = Object.Instantiate(prefab, frame, false);
            root.name = "Body";
            var body = new RiggedBody(frame, root.transform) { Outfit = outfit };
            if (!body.FindBones())
            {
                Debug.LogWarning($"[Runeheir] {prefab.name} is missing bones; using the placeholder doll.");
                Discard(root);
                return null;
            }

            // A worn cloak is drawn with the outfit's fitted GarmentCape and replaces the outfit's own cape; wings and
            // mufflers are built from primitives on the back socket.
            bool cloak = OutfitCatalog.IsCloak(look.Garment);
            SetActive(root.transform.Find("Cape"), string.IsNullOrEmpty(look.Garment));
            SetActive(root.transform.Find("GarmentCape"), cloak);

            body.Style = RigPoses.StyleOf(look.Weapon, look.Job);
            body._style = RigPoses.Get(body.Style);
            body._hold = RigPoses.HoldOf(look.Weapon);
            body.Paint(root, look);
            body.PutOnHair(look);
            body.MakeSockets();
            return body;
        }

        private static void SetActive(Transform t, bool active)
        {
            if (t != null)
            {
                t.gameObject.SetActive(active);
            }
        }

        private bool FindBones()
        {
            var all = new Dictionary<string, Transform>();
            foreach (var t in Root.GetComponentsInChildren<Transform>(true))
            {
                all[t.name] = t;
            }

            var inverseFrame = Quaternion.Inverse(_frame.rotation);
            foreach (string name in BoneNames)
            {
                if (!all.TryGetValue(name, out var t))
                {
                    return false;
                }

                var rest = inverseFrame * t.rotation;
                var bone = new Joint { T = t, RestLocal = t.localRotation, RestModel = rest, RestModelInverse = Quaternion.Inverse(rest) };
                _bones[name] = bone;
                _boneList.Add(bone);
            }

            return true;
        }

        private void Paint(GameObject root, AvatarLook look)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                PaintRenderer(renderer, look);
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    // Swings and leaps reach past the rest pose's bounds; don't let the camera cull them.
                    var bounds = skinned.localBounds;
                    bounds.Expand(0.8f);
                    skinned.localBounds = bounds;
                    skinned.updateWhenOffscreen = false;
                }
            }
        }

        private void PaintRenderer(Renderer renderer, AvatarLook look)
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = OutfitCatalog.SlotMaterial(Outfit, SlotName(materials[i]), look);
            }

            renderer.sharedMaterials = materials;
        }

        private static string SlotName(Material material)
        {
            return material != null ? material.name.Replace(" (Instance)", string.Empty) : string.Empty;
        }

        private void PutOnHair(AvatarLook look)
        {
            var prefab = OutfitCatalog.Hair(look.HairStyle);
            var head = Bone("Head");
            if (prefab == null || head == null)
            {
                return;
            }

            // Hair is modelled around the Head joint, facing the model's front. A one-mesh FBX imports with the mesh as its
            // root, carrying the axis conversion in its rotation: keep that.
            var hair = Object.Instantiate(prefab, _frame, false);
            hair.name = "Hair";
            hair.transform.localPosition = _frame.InverseTransformPoint(head.position) + prefab.transform.localPosition;
            hair.transform.localRotation = prefab.transform.localRotation;
            foreach (var renderer in hair.GetComponentsInChildren<Renderer>(true))
            {
                PaintRenderer(renderer, look);
            }

            hair.transform.SetParent(head, true);
        }

        private void MakeSockets()
        {
            Vector3 head = Local("Head");
            HeadUpper = Socket("HeadUpper", "Head", LegacyHead, head + new Vector3(0f, 0.165f, -0.004f), HeadGearScale);
            HeadMid = Socket("HeadMid", "Head", LegacyEyes, head + new Vector3(0f, 0.098f, 0.095f), HeadGearScale);
            HeadLower = Socket("HeadLower", "Head", LegacyMouth, head + new Vector3(0f, 0.029f, 0.075f), HeadGearScale);

            Vector3 neck = Local("Neck");
            Back = Socket("Back", "Chest", LegacyBack, new Vector3(0f, neck.y - 0.03f, -0.125f), 1f);
            NeckRing = Socket("NeckRing", "Chest", LegacyNeck, new Vector3(0f, neck.y - 0.01f, neck.z), 0.62f);

            Vector3 forearm = (Local("LeftLowerArm") + Local("LeftHand")) * 0.5f;
            ShieldArm = Socket("ShieldArm", "LeftLowerArm", LegacyShield, forearm + new Vector3(-0.075f, 0f, 0.02f), 1f);

            Vector3 hips = Local("Hips");
            Tail = Socket("Tail", "Hips", LegacyTail, new Vector3(0f, hips.y - 0.05f, -0.11f), 1f);
        }

        private Vector3 Local(string bone)
        {
            return _frame.InverseTransformPoint(Bone(bone).position);
        }

        /// <summary>A transform on <paramref name="bone"/> whose local space is the old doll's, so the gear builders' coordinates land on this body.</summary>
        private Transform Socket(string name, string bone, Vector3 legacyAnchor, Vector3 anchor, float scale)
        {
            var socket = new GameObject(name).transform;
            socket.SetParent(_frame, false);
            socket.localPosition = anchor - legacyAnchor * scale;
            socket.localRotation = Quaternion.identity;
            socket.localScale = Vector3.one * scale;
            socket.SetParent(Bone(bone), true);
            return socket;
        }

        // ------------------------------------------------------------------ weapons
        /// <summary>
        /// Puts the weapon model in hand (both hands for knuckles and katars, the left for bows, tomes and lyres) and its trail
        /// on the blade. False when there is no model for it (bare hands, or the models aren't in the project).
        /// Call while the body is still at rest.
        /// </summary>
        public bool AttachWeapon(WeaponType weapon)
        {
            var prefab = OutfitCatalog.WeaponModel(weapon);
            if (prefab == null)
            {
                return false;
            }

            bool left = _hold.LeftHand;
            _mainGrip = Grip(left ? "LeftHand" : "RightHand", _hold.Tilt);
            var main = PlaceWeapon(prefab, _mainGrip);
            AddTrail(_mainGrip);
            if (_hold.BothHands)
            {
                var off = Grip("LeftHand", _hold.Tilt);
                PlaceWeapon(prefab, off);
                AddTrail(off);
            }

            if (weapon == WeaponType.Bow)
            {
                PrepareBow(main.transform);
            }

            return true;
        }

        /// <summary>The fist's channel: a pole through the closed hand runs forward when the arm hangs; tilt turns it with the wrist.</summary>
        private Transform Grip(string hand, float tilt)
        {
            var bone = Bone(hand);
            string forearmName = hand.StartsWith("Left") ? "LeftLowerArm" : "RightLowerArm";
            Vector3 wrist = Local(hand);
            Vector3 along = (wrist - Local(forearmName)).normalized;
            Vector3 fist = wrist + along * 0.05f;
            var turn = Quaternion.Euler(tilt, 0f, 0f);
            Vector3 dir = turn * Vector3.forward;
            Vector3 edge = turn * Vector3.down;

            var grip = new GameObject("WeaponGrip").transform;
            grip.SetParent(_frame, false);
            grip.localPosition = fist;
            grip.localRotation = Quaternion.LookRotation(Vector3.Cross(edge, dir), dir);
            grip.SetParent(bone, true);
            return grip;
        }

        private GameObject PlaceWeapon(GameObject prefab, Transform grip)
        {
            var weapon = Object.Instantiate(prefab, grip, false);
            weapon.name = "Weapon";
            foreach (var renderer in weapon.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = OutfitCatalog.WeaponMaterial(SlotName(materials[i]));
                }

                renderer.sharedMaterials = materials;
            }

            return weapon;
        }

        private void AddTrail(Transform grip)
        {
            if (_hold.TrailTip <= 0f || !Application.isPlaying)
            {
                return;
            }

            _trails.Add(WeaponTrail.Create(grip, new Vector3(0f, _hold.TrailBase, 0f), new Vector3(0f, _hold.TrailTip, 0f),
                OutfitCatalog.TrailColor(Outfit)));
        }

        private void PrepareBow(Transform root)
        {
            var bow = new BowParts { Root = root };
            var stringTransform = FindChild(root, "String");
            var arrow = FindChild(root, "Arrow");
            if (stringTransform == null || arrow == null)
            {
                return;
            }

            // The string's two ends, in the bow's own space (its +X faces the target). Read from the mesh's bounds: imported
            // meshes aren't readable in play mode or in builds, so their vertices are off limits.
            var filter = stringTransform.GetComponent<MeshFilter>();
            var mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
            {
                return;
            }

            var bounds = mesh.bounds;
            Vector3 e = bounds.extents;
            Vector3 axis = e.x >= e.y && e.x >= e.z ? new Vector3(e.x, 0f, 0f) : e.y >= e.z ? new Vector3(0f, e.y, 0f) : new Vector3(0f, 0f, e.z);
            Vector3 lo = root.InverseTransformPoint(stringTransform.TransformPoint(bounds.center - axis));
            Vector3 hi = root.InverseTransformPoint(stringTransform.TransformPoint(bounds.center + axis));
            Vector3 up = root.InverseTransformDirection(_mainGrip.up);
            if (Vector3.Dot(hi - lo, up) < 0f)
            {
                (lo, hi) = (hi, lo);
            }

            bow.String = stringTransform.gameObject;
            bow.Arrow = arrow;
            bow.ArrowRestRotation = arrow.localRotation;
            bow.TipA = lo;
            bow.TipB = hi;
            bow.Nock = (lo + hi) * 0.5f;
            var drawn = new GameObject("DrawnString");
            drawn.transform.SetParent(root, false);
            bow.Drawn = drawn.AddComponent<LineRenderer>();
            bow.Drawn.useWorldSpace = true;
            bow.Drawn.positionCount = 3;
            bow.Drawn.widthMultiplier = 0.006f;
            bow.Drawn.sharedMaterial = RuntimeMaterials.Unlit;
            bow.Drawn.startColor = bow.Drawn.endColor = new Color(0.93f, 0.9f, 0.81f);
            bow.Drawn.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bow.Drawn.enabled = false;
            arrow.gameObject.SetActive(false);
            _bow = bow;
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>
        /// The old doll's weapon pivot in the right hand (the weapon along -Y, its grip at -0.42), for weapons without a
        /// model: the primitive builders still draw those.
        /// </summary>
        public Transform CreateWeaponPivot(WeaponType weapon)
        {
            var hand = Bone("RightHand");
            Vector3 wrist = Local("RightHand");
            Vector3 along = (wrist - Local("RightLowerArm")).normalized;
            Vector3 grip = wrist + along * 0.05f;

            Vector3 dir = new Vector3(0f, -0.55f, 1f).normalized;
            Vector3 x = Vector3.ProjectOnPlane(Vector3.up, dir).normalized;
            Vector3 y = -dir;
            Vector3 z = Vector3.Cross(x, y);

            var pivot = new GameObject("WeaponPivot").transform;
            pivot.SetParent(_frame, false);
            pivot.localRotation = Quaternion.LookRotation(z, y);
            pivot.localPosition = grip - dir * LegacyGripOffset;
            pivot.SetParent(hand, true);
            return pivot;
        }

        /// <summary>Destroy, or DestroyImmediate outside play mode (the editor's lineup renderer builds avatars too).</summary>
        public static void Discard(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

        // ------------------------------------------------------------------ procedural pose
        /// <summary>Poses the skeleton; returns how far the body steps forward (meters) for the attack in progress.</summary>
        public float Animate(in RigMotion m)
        {
            foreach (var bone in _boneList)
            {
                bone.Euler = Vector3.zero;
            }

            float live = 1f - m.Dead;
            float cast = Mathf.Clamp01(m.Cast) * live;
            float combat = Mathf.Clamp01(m.Combat);
            RigPoses.AddPose(_style.Idle, (1f - combat) * (1f - cast) * live, Add);
            RigPoses.AddPose(_style.Ready, combat * (1f - cast) * live, Add);
            RigPoses.AddPose(_style.Cast, cast, Add);
            Locomotion(m);

            float lunge = 0f;
            bool trail = false;
            float draw = 0f;
            RigPoses.Clip attack = null;
            float attackT = -1f;
            if (m.AttackT >= 0f && m.AttackT <= 1f && live > 0f)
            {
                attack = RigPoses.Attack(Style, m.AttackIndex);
                attackT = m.AttackT;
            }
            else if (m.SkillT >= 0f && m.SkillT <= 1f && live > 0f)
            {
                attack = RigPoses.SkillClip(Style, m.Skill);
                attackT = m.SkillT;
                if (attack == null)
                {
                    GenericSkill(m);
                }
                else if (m.Skill == SkillMotion.Leap)
                {
                    float a = Mathf.Sin(attackT * Mathf.PI);
                    Add("LeftUpperLeg", new Vector3(-35f * a, 0f, 0f));
                    Add("RightUpperLeg", new Vector3(-20f * a, 0f, 0f));
                    Add("LeftLowerLeg", new Vector3(50f * a, 0f, 0f));
                    Add("RightLowerLeg", new Vector3(50f * a, 0f, 0f));
                }
            }

            if (attack != null)
            {
                RigPoses.Sample(attack, attackT, 1f, Add);
                lunge = RigPoses.LungeAt(attack, attackT);
                trail = attackT >= attack.TrailFrom && attackT <= attack.TrailTo;
                draw = RigPoses.DrawAt(attack, attackT);
            }

            Reactions(m);

            foreach (var bone in _boneList)
            {
                bone.T.localRotation = bone.Euler == Vector3.zero
                    ? bone.RestLocal
                    : bone.RestLocal * (bone.RestModelInverse * Quaternion.Euler(bone.Euler) * bone.RestModel);
            }

            // The other hand on the grip of a two-handed weapon (from the battle stance on for spears and thunder-rods).
            if (_mainGrip != null && !float.IsNaN(_hold.OffHandAt) && !_hold.LeftHand)
            {
                float weight = Style == WeaponStyle.TwoHanded ? 1f : combat;
                weight *= live * (1f - cast);
                ReachWith("Left", _mainGrip.TransformPoint(new Vector3(0f, _hold.OffHandAt, 0f)), weight);
            }

            UpdateBow(draw, attackT);
            for (int i = 0; i < _trails.Count; i++)
            {
                _trails[i].Emitting = trail && (i == 0 || attack.BothHands);
            }

            return lunge;
        }

        private void UpdateBow(float draw, float t)
        {
            BowDraw = draw;
            if (_bow == null)
            {
                return;
            }

            bool drawing = draw > 0.01f;
            Vector3 nockRest = _bow.Root.TransformPoint(_bow.Nock);
            Vector3 anchor = _bow.Root.TransformPoint(_bow.Nock + Vector3.left * DrawLength);
            if (drawing)
            {
                ReachWith("Right", anchor, Mathf.Clamp01(draw * 1.4f));
            }

            Vector3 nock = Vector3.Lerp(nockRest, anchor, draw);
            _bow.String.SetActive(!drawing);
            _bow.Drawn.enabled = drawing;
            if (drawing)
            {
                _bow.Drawn.SetPosition(0, _bow.Root.TransformPoint(_bow.TipA));
                _bow.Drawn.SetPosition(1, nock);
                _bow.Drawn.SetPosition(2, _bow.Root.TransformPoint(_bow.TipB));
            }

            // The arrow sits on the string until the release (the hit frame), then flies as the combat code's projectile.
            bool nocked = drawing && t < 0.5f;
            _bow.Arrow.gameObject.SetActive(nocked);
            if (nocked)
            {
                Vector3 nockLocal = _bow.Root.InverseTransformPoint(nock);
                var turn = Quaternion.FromToRotation(Vector3.right, (Vector3.zero - nockLocal).normalized + Vector3.right * 0.0001f);
                _bow.Arrow.localRotation = turn * _bow.ArrowRestRotation;
                _bow.Arrow.localPosition = nockLocal - turn * _bow.Nock;
            }
        }

        /// <summary>Two-bone IK: bends shoulder and elbow so the <paramref name="side"/> wrist reaches <paramref name="target"/>.</summary>
        private void ReachWith(string side, Vector3 target, float weight)
        {
            if (weight <= 0.001f)
            {
                return;
            }

            var upper = Bone(side + "UpperArm");
            var lower = Bone(side + "LowerArm");
            var hand = Bone(side + "Hand");
            Vector3 a = upper.position, b = lower.position, c = hand.position;
            float l1 = (b - a).magnitude, l2 = (c - b).magnitude;
            Vector3 goal = Vector3.Lerp(c, target, weight);
            Vector3 toGoal = goal - a;
            float d = Mathf.Clamp(toGoal.magnitude, Mathf.Abs(l1 - l2) + 0.01f, l1 + l2 - 0.005f);
            Vector3 dir = toGoal.normalized;

            // The elbow points down, back and out from the body.
            float outward = side == "Left" ? -1f : 1f;
            Vector3 pole = _frame.TransformPoint(_frame.InverseTransformPoint(a) + new Vector3(outward * 0.35f, -0.45f, -0.25f));
            Vector3 bend = Vector3.ProjectOnPlane(pole - a, dir);
            if (bend.sqrMagnitude < 1e-6f)
            {
                bend = Vector3.ProjectOnPlane(-_frame.up, dir);
            }

            bend.Normalize();
            float cosA = Mathf.Clamp((l1 * l1 + d * d - l2 * l2) / (2f * l1 * d), -1f, 1f);
            Vector3 elbow = a + dir * (l1 * cosA) + bend * (l1 * Mathf.Sqrt(1f - cosA * cosA));

            upper.rotation = Quaternion.FromToRotation(b - a, elbow - a) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, a + dir * d - lower.position) * lower.rotation;
        }

        private void Add(string bone, Vector3 euler)
        {
            if (_bones.TryGetValue(bone, out var b))
            {
                b.Euler += euler;
            }
        }

        private void Add(string bone, float x, float y, float z)
        {
            Add(bone, new Vector3(x, y, z));
        }

        /// <summary>Run cycle (legs, counter-swinging free arms, a twist through the chest) fading to an idle breath.</summary>
        private void Locomotion(in RigMotion m)
        {
            float a = m.Speed < 0.15f || m.Dead > 0f ? 0f : Mathf.Clamp01(m.Speed / 4.5f);
            float s = Mathf.Sin(m.StridePhase);
            float c = Mathf.Cos(m.StridePhase);
            float leg = 36f * a;
            Add("LeftUpperLeg", -s * leg, 0f, 0f);
            Add("RightUpperLeg", s * leg, 0f, 0f);
            Add("LeftLowerLeg", Mathf.Max(0f, c) * leg * 1.6f, 0f, 0f);
            Add("RightLowerLeg", Mathf.Max(0f, -c) * leg * 1.6f, 0f, 0f);
            Add("LeftFoot", -Mathf.Max(0f, c) * leg * 0.35f, 0f, 0f);
            Add("RightFoot", -Mathf.Max(0f, -c) * leg * 0.35f, 0f, 0f);
            Add("LeftUpperArm", s * 26f * a * _style.ArmSwingLeft, 0f, 0f);
            Add("RightUpperArm", -s * 26f * a * _style.ArmSwingRight, 0f, 0f);
            Add("LeftLowerArm", -30f * a * _style.ArmSwingLeft, 0f, 0f);
            Add("RightLowerArm", -30f * a * _style.ArmSwingRight, 0f, 0f);
            Add("Spine", 7f * a, 0f, 0f);
            Add("Chest", 0f, s * 7f * a, 0f);
            Add("Hips", 0f, -s * 4f * a, 0f);

            float idle = (1f - a) * (1f - m.Dead);
            Add("Chest", Mathf.Sin(m.Time * 1.9f) * 1.6f * idle, 0f, 0f);
            Add("Head", Mathf.Sin(m.Time * 1.9f - 0.6f) * 1.2f * idle, Mathf.Sin(m.Time * 0.37f) * 4f * idle * (1f - m.Combat), 0f);
            // The battle stance bounces on its toes.
            float bounce = Mathf.Sin(m.Time * 5.2f) * m.Combat * idle;
            Add("LeftLowerLeg", 4f * bounce, 0f, 0f);
            Add("RightLowerLeg", 4f * bounce, 0f, 0f);
        }

        private void GenericSkill(in RigMotion m)
        {
            float a = Mathf.Sin(Mathf.Clamp01(m.SkillT) * Mathf.PI);
            switch (m.Skill)
            {
                case SkillMotion.Spin:
                    Add("RightUpperArm", -15f * a, 0f, 80f * a);
                    Add("RightLowerArm", 40f * a, 0f, 0f);
                    Add("LeftUpperArm", -10f * a, 0f, -60f * a);
                    break;
                case SkillMotion.Cast:
                    RigPoses.AddPose(_style.Cast, a, Add);
                    Add("Chest", -4f * a, 0f, 0f);
                    break;
                case SkillMotion.Buff:
                    Add("LeftUpperArm", -35f * a, 0f, -65f * a);
                    Add("RightUpperArm", -35f * a, 0f, 65f * a);
                    Add("LeftLowerArm", -20f * a, 0f, 0f);
                    Add("RightLowerArm", -20f * a, 0f, 0f);
                    Add("Chest", -9f * a, 0f, 0f);
                    Add("Spine", -4f * a, 0f, 0f);
                    break;
            }
        }

        private void Reactions(in RigMotion m)
        {
            if (m.Hit > 0f)
            {
                Add("Chest", -9f * m.Hit, 0f, 0f);
                Add("Head", -7f * m.Hit, 0f, 0f);
            }

            if (m.Stagger > 0f)
            {
                float s = m.Stagger;
                Add("Chest", -18f * s, 0f, 0f);
                Add("Spine", -8f * s, 0f, 0f);
                Add("Head", -10f * s, 0f, 0f);
                Add("LeftUpperArm", 0f, 0f, -28f * s);
                Add("RightUpperArm", 0f, 0f, 28f * s);
                Add("LeftLowerArm", -30f * s, 0f, 0f);
                Add("RightLowerArm", -30f * s, 0f, 0f);
            }

            if (m.Dead > 0f)
            {
                float d = m.Dead;
                Add("LeftUpperArm", -20f * d, 0f, -45f * d);
                Add("RightUpperArm", -20f * d, 0f, 45f * d);
                Add("LeftLowerArm", -20f * d, 0f, 0f);
                Add("RightLowerArm", -20f * d, 0f, 0f);
                Add("Head", -12f * d, 0f, 0f);
                Add("LeftLowerLeg", 12f * d, 0f, 0f);
                Add("RightLowerLeg", 18f * d, 0f, 0f);
            }
        }
    }
}
