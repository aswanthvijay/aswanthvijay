using Runeheir.Combat;
using Runeheir.Controls;
using Runeheir.Movement;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.Player
{
    /// <summary>
    /// GDD Phase 2 Step 3 — NavMesh click-to-move, XileRO style:
    ///  • Left-click ground: walk there (hold the button to keep steering toward the cursor).
    ///  • Left-click a monster: walk into range and auto-attack until it dies or you click elsewhere.
    ///  • While a skill's target cursor is up: left-click picks the target/spot, right-click cancels.
    /// Picking prefers monsters under the cursor and falls back to the nearest one within a few pixels,
    /// so small targets are easy to click at isometric zoom.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class ClickToMoveController : MonoBehaviour
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        [SerializeField, Min(10f)] private float maxRayDistance = 500f;
        [Tooltip("Screen-space radius (pixels) used to grab a monster when the click narrowly misses its collider.")]
        [SerializeField, Min(0f)] private float pickRadiusPixels = 28f;
        [Tooltip("Seconds between path updates while the mouse button is held down.")]
        [SerializeField, Min(0.02f)] private float holdRepathInterval = 0.12f;
        [Tooltip("Keep attacking after each hit (XileRO /noctrl). Off = one attack per click.")]
        [SerializeField] private bool continuousAttack = true;

        private PlayerCharacter _player;
        private NavMotor _motor;
        private AutoAttacker _attacker;
        private SkillCaster _caster;
        private Camera _camera;
        private bool _holdingMove;
        private float _nextHoldRepath;
        private GroundRing _clickMarker;
        private GroundRing _targetRing;
        private GroundRing _hoverRing;
        private GroundRing _aoePreview;

        /// <summary>Monster/player currently under the cursor (for nameplates and quick-cast).</summary>
        public CombatEntity HoveredEntity { get; private set; }

        /// <summary>Walkable point under the cursor, if any.</summary>
        public Vector3? HoveredGround { get; private set; }

        private void Awake()
        {
            _player = GetComponent<PlayerCharacter>();
            _motor = GetComponent<NavMotor>();
            _attacker = GetComponent<AutoAttacker>();
            _caster = GetComponent<SkillCaster>();
        }

        private void Start()
        {
            _clickMarker = GroundRing.Create("ClickMarker", new Color(1f, 0.85f, 0.35f, 1f), 0.45f, 0.07f);
            _clickMarker.Hide();
            _targetRing = GroundRing.Create("TargetRing", new Color(1f, 0.25f, 0.2f, 0.95f), 0.7f, 0.06f);
            _targetRing.SetSpin(90f);
            _targetRing.Hide();
            _hoverRing = GroundRing.Create("HoverRing", new Color(1f, 1f, 1f, 0.6f), 0.7f, 0.04f);
            _hoverRing.Hide();
            _aoePreview = GroundRing.Create("AoePreview", new Color(0.5f, 0.85f, 1f, 0.9f), 1f, 0.08f);
            _aoePreview.Hide();
        }

        private void OnDestroy()
        {
            foreach (var ring in new[] { _clickMarker, _targetRing, _hoverRing, _aoePreview })
            {
                if (ring != null)
                {
                    Destroy(ring.gameObject);
                }
            }
        }

        private void Update()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            bool overUI = UIFocus.PointerOverUI;
            UpdateHover(overUI);
            UpdateRings();

            if (_player.IsDead)
            {
                _holdingMove = false;
                return;
            }

            if (_caster.IsTargeting)
            {
                // The cursor owns the mouse now; a hold-to-walk must not resume after the skill is confirmed.
                _holdingMove = false;
                HandleTargeting(overUI);
                return;
            }

            if (GameInput.PrimaryDown && !overUI)
            {
                HandlePrimaryClick();
            }

            if (_holdingMove)
            {
                if (!GameInput.PrimaryHeld)
                {
                    _holdingMove = false;
                }
                else if (Time.time >= _nextHoldRepath && !overUI && HoveredGround.HasValue)
                {
                    _motor.MoveTo(HoveredGround.Value);
                    _nextHoldRepath = Time.time + holdRepathInterval;
                }
            }
        }

        private void HandlePrimaryClick()
        {
            if (HoveredEntity != null && _player.IsHostileTo(HoveredEntity))
            {
                _holdingMove = false;
                _caster.CancelApproach();
                _attacker.Engage(HoveredEntity, continuousAttack);
                return;
            }

            if (!HoveredGround.HasValue || _caster.IsCasting)
            {
                return;
            }

            _attacker.Disengage();
            _caster.CancelApproach();
            if (_motor.MoveTo(HoveredGround.Value))
            {
                _clickMarker.ShowAt(HoveredGround.Value);
                _clickMarker.Pulse(0.55f, 0.15f, 0.35f);
            }

            _holdingMove = true;
            _nextHoldRepath = Time.time + holdRepathInterval;
        }

        private void HandleTargeting(bool overUI)
        {
            if (GameInput.SecondaryDown)
            {
                _caster.CancelTargeting();
                return;
            }

            if (!GameInput.PrimaryDown || overUI)
            {
                return;
            }

            var skill = _caster.TargetingSkill;
            if (skill.Target == SkillTarget.Ground)
            {
                if (HoveredGround.HasValue)
                {
                    _caster.ConfirmGround(HoveredGround.Value);
                }
            }
            else if (HoveredEntity != null)
            {
                _caster.ConfirmTarget(HoveredEntity);
            }
            else if (skill.Target == SkillTarget.Friend && IsCursorNear(_player))
            {
                _caster.ConfirmTarget(_player);
            }
        }

        private void UpdateHover(bool overUI)
        {
            HoveredEntity = null;
            HoveredGround = null;
            if (overUI)
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(GameInput.PointerPosition);
            int count = Physics.RaycastNonAlloc(ray, Hits, maxRayDistance, ~0, QueryTriggerInteraction.Collide);
            float bestEntity = float.MaxValue;
            float bestGround = float.MaxValue;
            bool friendlySkill = _caster.IsTargeting && _caster.TargetingSkill.Target == SkillTarget.Friend;

            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i];
                var entity = hit.collider.GetComponentInParent<CombatEntity>();
                if (entity != null)
                {
                    // Friend skills (heals, Runic Aegis) only pick friends, so a monster in melee range never
                    // steals a self-heal click; everything else picks anyone but yourself.
                    bool selectable = !entity.IsDead && (friendlySkill ? !_player.IsHostileTo(entity) : entity != _player);
                    if (selectable && hit.distance < bestEntity)
                    {
                        HoveredEntity = entity;
                        bestEntity = hit.distance;
                    }

                    continue;
                }

                if (hit.distance < bestGround)
                {
                    HoveredGround = hit.point;
                    bestGround = hit.distance;
                }
            }

            if (HoveredEntity == null && !friendlySkill)
            {
                HoveredEntity = PickNearCursor();
            }
        }

        private CombatEntity PickNearCursor()
        {
            if (pickRadiusPixels <= 0f)
            {
                return null;
            }

            Vector2 pointer = GameInput.PointerPosition;
            CombatEntity best = null;
            float bestPixels = pickRadiusPixels;
            foreach (var entity in CombatEntity.All)
            {
                if (entity == _player || entity.IsDead || !_player.IsHostileTo(entity))
                {
                    continue;
                }

                Vector3 screen = _camera.WorldToScreenPoint(entity.Position + Vector3.up * (entity.Height * 0.5f));
                if (screen.z <= 0f)
                {
                    continue;
                }

                float pixels = Vector2.Distance(pointer, screen);
                if (pixels < bestPixels)
                {
                    best = entity;
                    bestPixels = pixels;
                }
            }

            return best;
        }

        private bool IsCursorNear(CombatEntity entity)
        {
            Vector3 screen = _camera.WorldToScreenPoint(entity.Position + Vector3.up * (entity.Height * 0.5f));
            return screen.z > 0f && Vector2.Distance(GameInput.PointerPosition, screen) < pickRadiusPixels * 2f;
        }

        private void UpdateRings()
        {
            var target = _attacker.Target;
            if (target != null && !target.IsDead)
            {
                _targetRing.SetRadius(target.Radius + 0.25f);
                _targetRing.Follow(target.transform);
            }
            else
            {
                _targetRing.Hide();
            }

            if (HoveredEntity != null && HoveredEntity != target)
            {
                _hoverRing.SetRadius(HoveredEntity.Radius + 0.2f);
                _hoverRing.Follow(HoveredEntity.transform);
            }
            else
            {
                _hoverRing.Hide();
            }

            var skill = _caster.TargetingSkill;
            if (skill != null && skill.Target == SkillTarget.Ground && HoveredGround.HasValue)
            {
                _aoePreview.SetRadius(skill.Radius > 0f ? skill.Radius : 0.6f);
                _aoePreview.ShowAt(HoveredGround.Value);
            }
            else
            {
                _aoePreview.Hide();
            }
        }
    }
}
