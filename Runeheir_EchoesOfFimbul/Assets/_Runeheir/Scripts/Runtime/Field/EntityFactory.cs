using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Monsters;
using Runeheir.Movement;
using Runeheir.Online;
using Runeheir.Player;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Field
{
    /// <summary>
    /// Assembles player and monster GameObjects from components. Built inactive and activated once
    /// complete, so every Awake can find its sibling components.
    /// </summary>
    public static class EntityFactory
    {
        /// <param name="visualPrefab">Optional real model (with Animator). Null = placeholder avatar.</param>
        /// <param name="animatorController">Optional controller for the model's Animator (RuneheirSettings).</param>
        /// <param name="beforeActivate">Online play: adds the network components while the object is still asleep.</param>
        public static PlayerCharacter CreatePlayer(CharacterRecord record, Vector3 position, GameObject visualPrefab = null,
            RuntimeAnimatorController animatorController = null, System.Action<GameObject> beforeActivate = null)
        {
            var go = new GameObject($"Player [{record.Name}]");
            go.SetActive(false);
            go.transform.position = position;

            var collider = go.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.height = 1.8f;
            collider.radius = 0.4f;

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.avoidancePriority = 30;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            go.AddComponent<NavMotor>();
            if (visualPrefab != null)
            {
                var model = Object.Instantiate(visualPrefab, go.transform, false);
                var animator = model.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    // Gameplay moves the character (NavMesh); clips must not.
                    animator.applyRootMotion = false;
                    if (animatorController != null)
                    {
                        animator.runtimeAnimatorController = animatorController;
                    }
                }
            }
            else
            {
                PlaceholderAvatar.CreateHumanoid(go.transform, AvatarLook.FromRecord(record));
            }

            go.AddComponent<CharacterAnimationBridge>();
            go.AddComponent<AutoAttacker>();
            var player = go.AddComponent<PlayerCharacter>();
            go.AddComponent<SkillCaster>();
            go.AddComponent<AspdAnimationScaler>();
            go.AddComponent<ClickToMoveController>();
            go.AddComponent<HotkeyController>();
            beforeActivate?.Invoke(go);

            go.SetActive(true);
            player.Initialize(record);
            go.GetComponent<NavMotor>().Warp(position);
            return player;
        }

        /// <summary>
        /// Online play: another player's character (or, on the realm server, anyone's): their look, moved and animated by
        /// the network. <paramref name="beforeActivate"/> adds the network components.
        /// </summary>
        public static RemotePlayer CreateRemotePlayer(string lookCode, Vector3 position, System.Action<GameObject> beforeActivate)
        {
            var go = new GameObject("Player [remote]");
            go.SetActive(false);
            go.transform.position = position;

            var collider = go.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.height = 1.8f;
            collider.radius = 0.4f;

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.enabled = false; // moved by the network, never by its own agent

            go.AddComponent<NavMotor>();
            PlaceholderAvatar.CreateHumanoid(go.transform, AvatarLook.FromCode(lookCode));
            go.AddComponent<CharacterAnimationBridge>();
            var player = go.AddComponent<RemotePlayer>();
            beforeActivate?.Invoke(go);

            go.SetActive(true);
            player.MirrorLook(lookCode);
            return player;
        }

        /// <param name="mirror">
        /// Online client: a copy of a monster the realm runs (<paramref name="beforeActivate"/> adds its network parts).
        /// Otherwise a realm server hands the new monster to the network through <see cref="RealmHooks"/>.
        /// </param>
        public static Monster CreateMonster(MonsterDefinition definition, Vector3 position, float yawDegrees = 0f, bool mirror = false,
            System.Action<GameObject> beforeActivate = null)
        {
            float scale = Mathf.Max(0.5f, definition.Scale);
            var go = new GameObject($"Monster [{definition.Name}]");
            go.SetActive(false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));

            var collider = go.AddComponent<CapsuleCollider>();
            collider.radius = 0.5f * scale;
            collider.height = 1.7f * scale;
            collider.center = new Vector3(0f, collider.height * 0.5f, 0f);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f * scale;
            agent.height = 1.6f * scale;
            agent.avoidancePriority = 50;
            agent.obstacleAvoidanceType = definition.Stationary
                ? ObstacleAvoidanceType.NoObstacleAvoidance
                : ObstacleAvoidanceType.MedQualityObstacleAvoidance;

            go.AddComponent<NavMotor>();
            PlaceholderAvatar.CreateMonster(go.transform, definition);
            go.AddComponent<CharacterAnimationBridge>();
            go.AddComponent<AutoAttacker>();
            var monster = go.AddComponent<Monster>();
            beforeActivate?.Invoke(go);
            if (!mirror)
            {
                RealmHooks.MonsterBuilding?.Invoke(go);
            }

            go.SetActive(true);
            monster.Initialize(definition, position);
            if (!mirror)
            {
                RealmHooks.MonsterCreated?.Invoke(monster);
            }

            return monster;
        }
    }
}
