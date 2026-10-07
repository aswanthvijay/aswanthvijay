using System;
using System.Linq;
using NUnit.Framework;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Runeheir.Tests
{
    /// <summary>
    /// The Blender-built characters and weapons (Tools/Blender/build_characters.py → Resources/Characters): every outfit
    /// imports as a skinned body with the full skeleton, facing the game's way; every weapon has a model; the avatar wears
    /// hair, weapon and gear; the rig swings, draws a bow, casts and comes back to rest.
    /// </summary>
    public sealed class CharacterArtTests
    {
        private static readonly JobId[] WarriorLine = { JobId.Warrior, JobId.Berserker, JobId.Guardian, JobId.Einherjar, JobId.Valkyrie };

        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("CharacterArtTest");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Catalog_GivesTheWarriorLineTheirOwnOutfits_AndEveryoneElseTheCommonOne()
        {
            foreach (var job in WarriorLine)
            {
                var outfit = OutfitCatalog.ForJob(job);
                Assert.IsNotNull(outfit, job.ToString());
                Assert.AreEqual(job.ToString().ToLowerInvariant(), outfit.Key, job.ToString());
            }

            Assert.AreEqual(OutfitCatalog.CommonKey, OutfitCatalog.ForJob(JobId.Mystic).Key, "no Mystic outfit yet");
            Assert.AreEqual(OutfitCatalog.CommonKey, OutfitCatalog.ForJob(null).Key, "NPCs and humanoid monsters");
            Assert.AreEqual(AvatarLook.HairStyleNames.Length, OutfitCatalog.HairStyleCount, "a hair model for every style on the creation screen");
        }

        [Test]
        public void EveryOutfit_IsASkinnedBody_WithTheSkeleton_AndOnlyKnownSlots()
        {
            foreach (var outfit in OutfitCatalog.All.Values)
            {
                foreach (var gender in new[] { Gender.Male, Gender.Female })
                {
                    string name = outfit.Key + " " + gender;
                    var prefab = Resources.Load<GameObject>(OutfitCatalog.ModelPath(outfit.Key, gender));
                    Assert.IsNotNull(prefab, name);
                    var bones = prefab.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToList();
                    foreach (string bone in RiggedBody.BoneNames)
                    {
                        CollectionAssert.Contains(bones, bone, name);
                    }

                    var body = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == "Body");
                    Assert.IsNotNull(body, name + " has a skinned Body");
                    Assert.Greater(body.bones.Length, 10, name + " is skinned to the skeleton");
                    Assert.IsNotNull(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == "GarmentCape"),
                        name + " has a cape fitted for worn cloaks");
                    Assert.IsNull(prefab.GetComponentInChildren<Animator>(true), name + ": the procedural rig drives it, not an Animator");
                    foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    {
                        foreach (var material in renderer.sharedMaterials)
                        {
                            Assert.IsTrue(OutfitCatalog.IsKnownSlot(outfit, material.name), $"{name}: unknown material slot '{material.name}'");
                        }
                    }
                }
            }
        }

        [Test]
        public void EveryWeaponType_HasAModel_PaintedFromTheWeaponPalette()
        {
            foreach (WeaponType weapon in Enum.GetValues(typeof(WeaponType)))
            {
                var model = OutfitCatalog.WeaponModel(weapon);
                if (weapon == WeaponType.Unarmed)
                {
                    Assert.IsNull(model);
                    continue;
                }

                Assert.IsNotNull(model, weapon.ToString());
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        Assert.IsTrue(OutfitCatalog.IsKnownWeaponSlot(material.name), $"{weapon}: unknown weapon slot '{material.name}'");
                    }
                }
            }
        }

        [Test]
        public void Models_FaceForward_WithTheirLeftOnTheLeft_AtHumanHeight()
        {
            foreach (var gender in new[] { Gender.Male, Gender.Female })
            {
                var model = Object.Instantiate(Resources.Load<GameObject>(OutfitCatalog.ModelPath("warrior", gender)), _root.transform);
                Transform Find(string bone) => model.GetComponentsInChildren<Transform>().First(t => t.name == bone);
                Assert.Less(Find("LeftHand").position.x, -0.15f, "the left hand is on the character's left (-X when facing +Z)");
                Assert.Greater(Find("RightHand").position.x, 0.15f);
                Assert.Greater(Find("LeftToes").position.z, Find("LeftFoot").position.z, "toes point forward (+Z)");
                Assert.AreEqual(gender == Gender.Male ? 1.48f : 1.445f, Find("Head").position.y, 0.01f, "head joint height in meters");
                Assert.AreEqual(0f, Find("LeftToes").position.y, 0.05f, "standing on the ground");
            }
        }

        [Test]
        public void Avatar_WearsTheJobOutfit_WithHairWeaponAndGear()
        {
            var look = Look(JobId.Einherjar, Gender.Female);
            look.Weapon = WeaponType.TwoHandSword;
            look.HeadUpper = "grand_horned_viking_crest";
            look.Shield = "kite_shield";
            var avatar = PlaceholderAvatar.CreateHumanoid(_root.transform, look);

            Assert.IsNotNull(avatar.Rig, "the Blender-built body");
            Assert.AreEqual("einherjar", avatar.Rig.Outfit.Key);
            Assert.AreEqual(WeaponStyle.TwoHanded, avatar.Rig.Style);
            Assert.IsNotNull(avatar.Rig.Bone("Head").Find("Hair"), "hair rides on the head");
            Assert.AreSame(avatar.Rig.Bone("RightHand"), avatar.Rig.WeaponGrip.parent, "the greatsword is in the right fist");
            Assert.IsNotNull(avatar.Rig.WeaponGrip.Find("Weapon"), "the weapon model");
            Assert.Greater(avatar.Rig.HeadUpper.childCount, 0, "the helm sits on the head socket");
            Assert.Greater(avatar.Rig.ShieldArm.childCount, 0, "the shield is strapped to the left forearm");
            Assert.IsTrue(avatar.Rig.Root.Find("Cape").gameObject.activeSelf, "the Einherjar's own cape");
            Assert.IsFalse(avatar.Rig.Root.Find("GarmentCape").gameObject.activeSelf, "no cloak worn");

            // Every material is the toon shader's (or the URP fallback), none of the FBX's own.
            foreach (var renderer in avatar.GetComponentsInChildren<Renderer>())
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    StringAssert.StartsWith("RH_", material.name, renderer.name);
                }
            }
        }

        [Test]
        public void AWornCloak_IsTheOutfitsFittedCape_AndWingsAreNot()
        {
            var look = Look(JobId.Mystic, Gender.Male);
            look.Garment = "traveler_cloak";
            var cloaked = PlaceholderAvatar.CreateHumanoid(_root.transform, look);
            Assert.IsTrue(cloaked.Rig.Root.Find("GarmentCape").gameObject.activeSelf, "the fitted cape, skinned to the body");
            Assert.AreEqual(0, cloaked.Rig.Back.childCount, "no board-like primitive cape on the back");

            look.Garment = "valkyrian_feather_wings";
            var winged = PlaceholderAvatar.CreateHumanoid(_root.transform, look);
            Assert.IsFalse(winged.Rig.Root.Find("GarmentCape").gameObject.activeSelf);
            Assert.Greater(winged.Rig.Back.childCount, 0, "wings on the back socket");
        }

        [Test]
        public void Rig_SwingsTheWeaponArm_RunsCasts_AndReturnsToRest()
        {
            var avatar = PlaceholderAvatar.CreateHumanoid(_root.transform, Look(JobId.Warrior, Gender.Male, WeaponType.OneHandSword));
            Assert.AreEqual(WeaponStyle.OneHanded, avatar.Rig.Style);
            var hand = avatar.Rig.Bone("RightHand");
            var rest = new RigMotion { AttackT = -1f, SkillT = -1f };
            avatar.Rig.Animate(rest);
            Vector3 atRest = hand.position;
            Quaternion restRotation = avatar.Rig.Bone("RightUpperArm").localRotation;

            var windup = rest;
            windup.Combat = 1f;
            windup.AttackT = 0.28f;
            avatar.Rig.Animate(windup);
            Vector3 high = hand.position;
            Assert.Greater(high.y, atRest.y + 0.4f, "wound up: the sword hand is raised");

            var strike = windup;
            strike.AttackT = 0.5f;
            avatar.Rig.Animate(strike);
            Assert.Less(hand.position.y, high.y - 0.3f, "the cut comes down");
            Assert.Less(hand.position.x, high.x - 0.2f, "and across the body");

            var run = rest;
            run.Speed = 5.5f;
            run.StridePhase = Mathf.PI * 0.5f;
            avatar.Rig.Animate(run);
            Assert.Greater(avatar.Rig.Bone("LeftLowerLeg").position.z, avatar.Rig.Bone("RightLowerLeg").position.z + 0.2f, "mid-stride, left leg forward");

            foreach (var motion in new[] { SkillMotion.Spin, SkillMotion.Thrust, SkillMotion.Shoot, SkillMotion.Leap, SkillMotion.Cast, SkillMotion.Buff,
                         SkillMotion.Punch })
            {
                var skill = rest;
                skill.Skill = motion;
                skill.SkillT = 0.5f;
                avatar.Rig.Animate(skill);
            }

            avatar.Rig.Animate(rest);
            Assert.AreEqual(atRest.y, hand.position.y, 1e-3f, "back to rest");
            Assert.AreEqual(1f, Mathf.Abs(Quaternion.Dot(restRotation, avatar.Rig.Bone("RightUpperArm").localRotation)), 1e-4f);
        }

        [Test]
        public void Bow_IsHeldInTheLeftHand_DrawnToTheCheek_AndLoosedOnTheHit()
        {
            var avatar = PlaceholderAvatar.CreateHumanoid(_root.transform, Look(JobId.Huntsman, Gender.Male, WeaponType.Bow));
            Assert.AreEqual(WeaponStyle.Bow, avatar.Rig.Style);
            Assert.AreSame(avatar.Rig.Bone("LeftHand"), avatar.Rig.WeaponGrip.parent, "archers hold the bow in the left hand");
            var arrow = avatar.Rig.WeaponGrip.GetComponentsInChildren<Transform>(true).First(t => t.name == "Arrow");

            var drawn = new RigMotion { Combat = 1f, AttackT = 0.45f, SkillT = -1f };
            avatar.Rig.Animate(drawn);
            Assert.Greater(avatar.Rig.BowDraw, 0.9f, "fully drawn just before the hit frame");
            Assert.IsTrue(arrow.gameObject.activeSelf, "an arrow on the string");
            float span = Vector3.Distance(avatar.Rig.Bone("RightHand").position, avatar.Rig.Bone("LeftHand").position);
            Assert.Greater(span, 0.45f, "the drawing hand is pulled back from the bow");

            var loosed = drawn;
            loosed.AttackT = 0.6f;
            avatar.Rig.Animate(loosed);
            Assert.Less(avatar.Rig.BowDraw, 0.05f, "the string snapped back");
            Assert.IsFalse(arrow.gameObject.activeSelf, "the arrow flew (the combat code's projectile takes over)");
        }

        [Test]
        public void TwoHandedWeapons_PutTheOtherHandOnTheGrip()
        {
            var avatar = PlaceholderAvatar.CreateHumanoid(_root.transform, Look(JobId.Einherjar, Gender.Male, WeaponType.TwoHandSword));
            foreach (float t in new[] { -1f, 0.3f, 0.5f })
            {
                avatar.Rig.Animate(new RigMotion { Combat = 1f, AttackT = t, SkillT = -1f });
                float gap = Vector3.Distance(avatar.Rig.Bone("LeftHand").position, avatar.Rig.Bone("RightHand").position);
                Assert.Less(gap, 0.25f, $"both hands on the greatsword at t={t}");
            }
        }

        [Test]
        public void FreyjasKin_KeepTheirEarsAndTail_OnTheRiggedBody()
        {
            var look = Look(JobId.FreyjasKin, Gender.Female);
            look.Race = CharacterRace.Doram;
            var avatar = PlaceholderAvatar.CreateHumanoid(_root.transform, look);
            Assert.IsNotNull(avatar.Rig);
            Assert.IsNotNull(avatar.Rig.HeadUpper.Find("Ear"), "cat ears on the head");
            Assert.IsNotNull(avatar.Rig.Tail.Find("Tail"), "a tail at the hips");
        }

        [Test]
        public void TheGlimaLine_FightsWithKicks()
        {
            Assert.AreEqual(WeaponStyle.Kicks, RigPoses.StyleOf(WeaponType.Unarmed, JobId.GlimaFighter));
            Assert.AreEqual(WeaponStyle.Fists, RigPoses.StyleOf(WeaponType.Unarmed, JobId.Initiate));
            Assert.AreEqual(WeaponStyle.Fists, RigPoses.StyleOf(WeaponType.Knuckle, JobId.Monk));
        }

        private static AvatarLook Look(JobId job, Gender gender, WeaponType? weapon = null)
        {
            var info = JobDatabase.Get(job);
            return new AvatarLook
            {
                Job = job,
                Outfit = RuntimeMaterials.Hex(info.ColorHex),
                Skin = AvatarLook.DefaultSkin,
                Hair = AvatarLook.HairPalette[2],
                HairStyle = 1,
                Gender = gender,
                Weapon = weapon ?? info.StarterWeapon.Type,
            };
        }
    }
}
