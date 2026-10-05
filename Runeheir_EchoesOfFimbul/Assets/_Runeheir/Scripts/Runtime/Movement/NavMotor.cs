using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Movement
{
    /// <summary>
    /// Thin, game-friendly wrapper around <see cref="NavMeshAgent"/> shared by players and monsters:
    /// snap clicks to the NavMesh, crisp manual turning (isometric games want instant facing),
    /// movement locks (casting / stun) and knockback.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NavMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float baseMoveSpeed = 5.5f;
        [SerializeField, Min(0.1f)] private float acceleration = 60f;
        [Tooltip("Degrees per second when turning to face a direction.")]
        [SerializeField, Min(30f)] private float turnSpeed = 1080f;
        [Tooltip("How far from a click we search for walkable ground.")]
        [SerializeField, Min(0.1f)] private float sampleRadius = 4f;

        private NavMeshAgent _agent;
        private float _speedMultiplier = 1f;
        private bool _locked;
        private bool _hasFaceDirection;
        private Vector3 _faceDirection;
        private Vector3 _knockbackVelocity;
        private float _knockbackUntil;

        public NavMeshAgent Agent => _agent;

        public bool IsReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;

        public bool IsLocked => _locked;

        public Vector3 Velocity => IsReady ? _agent.velocity : Vector3.zero;

        public bool IsMoving =>
            IsReady && !_agent.isStopped && (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance + 0.05f);

        public float BaseMoveSpeed
        {
            get => baseMoveSpeed;
            set
            {
                baseMoveSpeed = Mathf.Max(0.1f, value);
                ApplySpeed();
            }
        }

        /// <summary>From stats (Wild Boar card +10%, mounts +25%...).</summary>
        public float SpeedMultiplier
        {
            get => _speedMultiplier;
            set
            {
                _speedMultiplier = Mathf.Max(0.05f, value);
                ApplySpeed();
            }
        }

        public bool MoveTo(Vector3 worldPoint)
        {
            if (_locked || !IsReady)
            {
                return false;
            }

            if (!NavMesh.SamplePosition(worldPoint, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            {
                return false;
            }

            _hasFaceDirection = false;
            _agent.isStopped = false;
            return _agent.SetDestination(hit.position);
        }

        public void Stop()
        {
            if (IsReady)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        /// <summary>Movement lock while casting, stunned or frozen.</summary>
        public void SetLocked(bool locked)
        {
            _locked = locked;
            if (locked)
            {
                Stop();
            }
        }

        public void FaceTowards(Vector3 worldPoint, bool instant = false)
        {
            Vector3 direction = worldPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _faceDirection = direction.normalized;
            _hasFaceDirection = true;
            if (instant)
            {
                transform.rotation = Quaternion.LookRotation(_faceDirection, Vector3.up);
            }
        }

        /// <summary>Teleport onto the NavMesh (spawn, Raven Feather, Aether Snap).</summary>
        public bool Warp(Vector3 worldPoint)
        {
            if (_agent == null || !_agent.enabled)
            {
                transform.position = worldPoint;
                return false;
            }

            if (!NavMesh.SamplePosition(worldPoint, out NavMeshHit hit, sampleRadius * 2f, NavMesh.AllAreas))
            {
                return false;
            }

            bool warped = _agent.Warp(hit.position);
            if (warped && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }

            return warped;
        }

        /// <summary>Furthest reachable point along a straight line (for dashes), or the start if blocked.</summary>
        public Vector3 ClampToReachable(Vector3 destination)
        {
            if (!IsReady)
            {
                return transform.position;
            }

            Vector3 start = _agent.nextPosition;
            if (NavMesh.SamplePosition(destination, out NavMeshHit end, sampleRadius, NavMesh.AllAreas))
            {
                destination = end.position;
            }

            return NavMesh.Raycast(start, destination, out NavMeshHit blocked, NavMesh.AllAreas) ? blocked.position : destination;
        }

        /// <summary>Slides the agent along the NavMesh (Vortex Cleave launches).</summary>
        public void Knockback(Vector3 direction, float distance, float duration = 0.18f)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f || distance <= 0f || duration <= 0f)
            {
                return;
            }

            _knockbackVelocity = direction.normalized * (distance / duration);
            _knockbackUntil = Time.time + duration;
            Stop();
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _agent.acceleration = acceleration;
            _agent.autoBraking = true;
            _agent.stoppingDistance = 0.05f;
            ApplySpeed();
        }

        private void Update()
        {
            if (!IsReady)
            {
                return;
            }

            if (Time.time < _knockbackUntil)
            {
                _agent.Move(_knockbackVelocity * Time.deltaTime);
            }

            Vector3 velocity = _agent.velocity;
            velocity.y = 0f;
            Vector3 look;
            if (velocity.sqrMagnitude > 0.04f && !_agent.isStopped)
            {
                look = velocity.normalized;
            }
            else if (_hasFaceDirection)
            {
                look = _faceDirection;
            }
            else
            {
                return;
            }

            Quaternion wanted = Quaternion.LookRotation(look, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, turnSpeed * Time.deltaTime);
        }

        private void ApplySpeed()
        {
            if (_agent != null)
            {
                _agent.speed = baseMoveSpeed * _speedMultiplier;
            }
        }
    }
}
