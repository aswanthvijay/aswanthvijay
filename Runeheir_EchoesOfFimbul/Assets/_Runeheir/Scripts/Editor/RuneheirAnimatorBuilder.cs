using System.Collections.Generic;
using System.IO;
using System.Linq;
using Runeheir.Skills;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Runeheir.EditorTools
{
    /// <summary>
    /// Phase 3 animation layer: builds an Animator Controller wired exactly the way <see cref="Visuals.CharacterAnimationBridge"/>
    /// drives it (locomotion blend, ASPD-scaled attack, cast loop, hit, stagger, death, one state per skill motion).
    /// The clips it creates are empty placeholders: drop your Blender/Mixamo clips onto the states, or make an
    /// Animator Override Controller per character and keep this one as the shared base.
    /// </summary>
    public static class RuneheirAnimatorBuilder
    {
        public const string DefaultFolder = RuneheirSetupWizard.RootFolder + "/Generated/Animation";
        public const string ControllerName = "RuneheirCharacter.controller";

        public const string Speed = "Speed";
        public const string AttackSpeed = "AttackSpeed";
        public const string Attack = "Attack";
        public const string Casting = "Casting";
        public const string Dead = "Dead";
        public const string Hit = "Hit";
        public const string Skill = "Skill";
        public const string SkillMotionParameter = "SkillMotion";
        public const string Stagger = "Stagger";

        /// <summary>Run speed (m/s) at which the locomotion blend reaches the Run clip.</summary>
        public const float RunThreshold = 4f;

        /// <summary>Every parameter the bridge looks for, with its type.</summary>
        public static readonly (string Name, AnimatorControllerParameterType Type)[] Parameters =
        {
            (Speed, AnimatorControllerParameterType.Float),
            (AttackSpeed, AnimatorControllerParameterType.Float),
            (Attack, AnimatorControllerParameterType.Trigger),
            (Casting, AnimatorControllerParameterType.Bool),
            (Dead, AnimatorControllerParameterType.Bool),
            (Hit, AnimatorControllerParameterType.Trigger),
            (Skill, AnimatorControllerParameterType.Trigger),
            (SkillMotionParameter, AnimatorControllerParameterType.Int),
            (Stagger, AnimatorControllerParameterType.Trigger),
        };

        [MenuItem("Runeheir/Animation/Create Character Animator Controller", priority = 30)]
        public static void CreateFromMenu()
        {
            string path = $"{DefaultFolder}/{ControllerName}";
            if (File.Exists(path) && !Application.isBatchMode &&
                !EditorUtility.DisplayDialog("Runeheir", $"{path} already exists. Rebuild it? Clips you assigned to its states will be replaced by placeholders.", "Rebuild", "Cancel"))
            {
                return;
            }

            var controller = Build(DefaultFolder);
            Selection.activeObject = controller;
            EditorGUIUtility.PingObject(controller);
            Debug.Log($"[Runeheir] Animator Controller created at {AssetDatabase.GetAssetPath(controller)}. " +
                      "Put it on your model's Animator and replace the placeholder clips (Docs/Phase3_Guide.md).");
        }

        [MenuItem("Runeheir/Setup/Create Settings Asset", priority = 10)]
        public static void CreateSettingsAsset()
        {
            var settings = EnsureSettingsAsset();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        /// <summary>Resources/RuneheirSettings.asset: player model, its controller and rates. Never overwritten once it exists.</summary>
        public static Field.RuneheirSettings EnsureSettingsAsset(string folder = RuneheirSetupWizard.RootFolder + "/Resources")
        {
            string path = $"{folder}/{Field.RuneheirSettings.ResourcePath}.asset";
            var settings = AssetDatabase.LoadAssetAtPath<Field.RuneheirSettings>(path);
            if (settings != null)
            {
                return settings;
            }

            EnsureFolder(folder);
            settings = ScriptableObject.CreateInstance<Field.RuneheirSettings>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            return settings;
        }

        [MenuItem("Runeheir/Animation/Check Selected Animator", priority = 31)]
        public static void CheckSelection()
        {
            var animator = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInChildren<Animator>() : null;
            var controller = animator != null ? animator.runtimeAnimatorController as AnimatorController : Selection.activeObject as AnimatorController;
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Runeheir", "Select a model with an Animator (or an Animator Controller asset) first.", "OK");
                return;
            }

            var missing = MissingParameters(controller);
            string message = missing.Count == 0
                ? $"{controller.name} has every parameter CharacterAnimationBridge uses."
                : $"{controller.name} is missing: {string.Join(", ", missing)}.\nThe bridge skips missing parameters, so those animations won't play.";
            if (!controller.layers.SelectMany(l => l.stateMachine.states).Any(s => s.state.name == "Attack"))
            {
                message += "\nNo state named \"Attack\": at high ASPD the bridge falls back to the Attack trigger.";
            }

            EditorUtility.DisplayDialog("Runeheir", message, "OK");
        }

        /// <summary>The parameters the bridge expects that <paramref name="controller"/> lacks (name and type must match).</summary>
        public static List<string> MissingParameters(AnimatorController controller)
        {
            var missing = new List<string>();
            foreach (var (name, type) in Parameters)
            {
                if (!controller.parameters.Any(p => p.name == name && p.type == type))
                {
                    missing.Add($"{name} ({type})");
                }
            }

            return missing;
        }

        /// <summary>Creates (or rebuilds) the controller and its placeholder clips under <paramref name="folder"/>.</summary>
        public static AnimatorController Build(string folder)
        {
            EnsureFolder(folder);
            EnsureFolder(folder + "/Clips");
            string path = $"{folder}/{ControllerName}";

            // Rebuild in place so prefabs and override controllers that reference it stay linked (same asset GUID).
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }
            else
            {
                Clear(controller);
            }

            foreach (var (name, type) in Parameters)
            {
                var parameter = new AnimatorControllerParameter { name = name, type = type };
                if (name == AttackSpeed)
                {
                    parameter.defaultFloat = 1f;
                }

                controller.AddParameter(parameter);
            }

            var machine = controller.layers[0].stateMachine;
            machine.anyStatePosition = new Vector3(40f, 260f, 0f);
            machine.entryPosition = new Vector3(40f, 80f, 0f);

            // Locomotion: idle → run by NavMesh speed.
            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var blend, 0);
            blend.blendParameter = Speed;
            blend.useAutomaticThresholds = false;
            blend.AddChild(Clip(folder, "Idle", 1f, loop: true), 0f);
            blend.AddChild(Clip(folder, "Run", 0.8f, loop: true), RunThreshold);
            machine.defaultState = locomotion;
            Move(machine, locomotion, 320f, 80f);

            // Death first: it outranks every other Any State transition, and every other Any State transition
            // requires !Dead, so a trigger left over from the killing blow can't pull the corpse out of Dead.
            var dead = AddState(machine, "Dead", Clip(folder, "Die", 1.2f), 620f, 380f);
            var toDead = machine.AddAnyStateTransition(dead);
            toDead.AddCondition(AnimatorConditionMode.If, 0f, Dead);
            Snap(toDead, 0.1f);
            toDead.canTransitionToSelf = false;
            var revive = dead.AddTransition(locomotion);
            revive.AddCondition(AnimatorConditionMode.IfNot, 0f, Dead);
            Snap(revive, 0.15f);

            // Basic attack: authored at 0.6 s with the hit on the 50% frame; AttackSpeed (1x–3x) is the ASPD play rate.
            var attack = AddState(machine, "Attack", Clip(folder, "Attack", Stats.StatFormulas.BaseSwingSeconds), 620f, 0f);
            attack.speedParameterActive = true;
            attack.speedParameter = AttackSpeed;
            var toAttack = machine.AddAnyStateTransition(attack);
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, Attack);
            toAttack.AddCondition(AnimatorConditionMode.IfNot, 0f, Dead);
            Snap(toAttack, 0.05f);
            toAttack.canTransitionToSelf = true;
            ExitTo(attack, locomotion);

            var stagger = AddState(machine, "Stagger", Clip(folder, "Stagger", Combat.PoiseRules.StaggerSeconds), 620f, 300f);
            var toStagger = machine.AddAnyStateTransition(stagger);
            toStagger.AddCondition(AnimatorConditionMode.If, 0f, Stagger);
            toStagger.AddCondition(AnimatorConditionMode.IfNot, 0f, Dead);
            Snap(toStagger, 0.05f);
            ExitTo(stagger, locomotion);

            var hit = AddState(machine, "Hit", Clip(folder, "Hit", 0.3f), 620f, 220f);
            var toHit = machine.AddAnyStateTransition(hit);
            toHit.AddCondition(AnimatorConditionMode.If, 0f, Hit);
            toHit.AddCondition(AnimatorConditionMode.IfNot, 0f, Dead);
            Snap(toHit, 0.05f);
            ExitTo(hit, locomotion);

            var cast = AddState(machine, "Cast", Clip(folder, "CastLoop", 1f, loop: true), 620f, 140f);
            var toCast = machine.AddAnyStateTransition(cast);
            toCast.AddCondition(AnimatorConditionMode.If, 0f, Casting);
            toCast.AddCondition(AnimatorConditionMode.IfNot, 0f, Dead);
            Snap(toCast, 0.12f);
            toCast.canTransitionToSelf = false;
            var endCast = cast.AddTransition(locomotion);
            endCast.AddCondition(AnimatorConditionMode.IfNot, 0f, Casting);
            Snap(endCast, 0.12f);

            // One state per skill motion, chosen by the SkillMotion int (CharacterAnimationBridge.PlaySkill).
            int row = 0;
            foreach (SkillMotion motion in System.Enum.GetValues(typeof(SkillMotion)))
            {
                if (motion == SkillMotion.None)
                {
                    continue;
                }

                bool melee = motion != SkillMotion.Cast && motion != SkillMotion.Buff;
                var state = AddState(machine, "Skill_" + motion, Clip(folder, "Skill_" + motion, melee ? 0.6f : 0.8f), 960f, row++ * 70f);
                if (melee)
                {
                    state.speedParameterActive = true;
                    state.speedParameter = AttackSpeed;
                }

                var toSkill = machine.AddAnyStateTransition(state);
                toSkill.AddCondition(AnimatorConditionMode.If, 0f, Skill);
                toSkill.AddCondition(AnimatorConditionMode.Equals, (int)motion, SkillMotionParameter);
                toSkill.AddCondition(AnimatorConditionMode.IfNot, 0f, Dead);
                Snap(toSkill, 0.05f);
                toSkill.canTransitionToSelf = true;
                ExitTo(state, locomotion);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void Clear(AnimatorController controller)
        {
            foreach (var parameter in controller.parameters)
            {
                controller.RemoveParameter(parameter);
            }

            while (controller.layers.Length > 1)
            {
                controller.RemoveLayer(controller.layers.Length - 1);
            }

            var machine = controller.layers[0].stateMachine;
            foreach (var transition in machine.anyStateTransitions)
            {
                machine.RemoveAnyStateTransition(transition);
            }

            foreach (var transition in machine.entryTransitions)
            {
                machine.RemoveEntryTransition(transition);
            }

            foreach (var child in machine.stateMachines)
            {
                machine.RemoveStateMachine(child.stateMachine);
            }

            foreach (var child in machine.states)
            {
                machine.RemoveState(child.state);
            }

            // Blend trees made by CreateBlendTreeInController are sub-assets that RemoveState leaves behind.
            foreach (var orphan in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)).OfType<BlendTree>())
            {
                Object.DestroyImmediate(orphan, true);
            }
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, float x, float y)
        {
            var state = machine.AddState(name, new Vector3(x, y, 0f));
            state.motion = motion;
            return state;
        }

        private static void Move(AnimatorStateMachine machine, AnimatorState state, float x, float y)
        {
            var states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state == state)
                {
                    states[i].position = new Vector3(x, y, 0f);
                }
            }

            machine.states = states;
        }

        private static void Snap(AnimatorStateTransition transition, float duration)
        {
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.hasFixedDuration = true;
        }

        private static void ExitTo(AnimatorState from, AnimatorState to)
        {
            var back = from.AddTransition(to);
            back.hasExitTime = true;
            back.exitTime = 0.95f;
            back.duration = 0.1f;
            back.hasFixedDuration = true;
        }

        /// <summary>A placeholder clip of the given length (an empty clip would have no length, so the states would never play out).</summary>
        private static AnimationClip Clip(string folder, string name, float seconds, bool loop = false)
        {
            string path = $"{folder}/Clips/RH_{name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = "RH_" + name, frameRate = 30f };
                // A flat curve on a property no model has: gives the clip its length and animates nothing.
                clip.SetCurve(string.Empty, typeof(Animator), "RuneheirPlaceholder", AnimationCurve.Constant(0f, Mathf.Max(0.05f, seconds), 0f));
                AssetDatabase.CreateAsset(clip, path);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
