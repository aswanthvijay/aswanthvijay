using Runeheir.Combat;
using Runeheir.Player;
using Runeheir.Stats;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>
    /// GDD §4 AGI → ASPD → animation: reads the character's ASPD (150–197) and pushes the matching
    /// attack play rate (1.0x–3.0x) into the <see cref="CharacterAnimationBridge"/> whenever stats change.
    /// The inspector shows a live readout so designers can watch ASPD while tuning AGI/buffs.
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public sealed class AspdAnimationScaler : MonoBehaviour
    {
        [SerializeField] private CharacterAnimationBridge bridge;

        [Header("Live readout (play mode)")]
        [SerializeField] private float aspd;
        [SerializeField] private float attackPlayRate;
        [SerializeField] private float swingSeconds;
        [SerializeField] private float attackInterval;
        [SerializeField] private float attacksPerSecond;

        private CombatEntity _owner;
        private PlayerCharacter _player;

        public float Aspd => aspd;

        public float AttackPlayRate => attackPlayRate;

        public void Refresh()
        {
            if (_owner == null)
            {
                return;
            }

            aspd = _owner.Aspd;
            attackPlayRate = _owner.AttackPlayRate;
            swingSeconds = _owner.SwingDuration;
            attackInterval = _owner.AttackInterval;
            attacksPerSecond = attackInterval > 0f ? 1f / attackInterval : 0f;
            if (bridge != null)
            {
                bridge.SetAttackPlayRate(attackPlayRate);
            }
        }

        private void Awake()
        {
            _owner = GetComponent<CombatEntity>();
            _player = _owner as PlayerCharacter;
            if (bridge == null)
            {
                bridge = GetComponent<CharacterAnimationBridge>();
            }
        }

        private void OnEnable()
        {
            if (_player != null)
            {
                _player.StatsRecalculated += Refresh;
            }
        }

        private void OnDisable()
        {
            if (_player != null)
            {
                _player.StatsRecalculated -= Refresh;
            }
        }

        private void Start()
        {
            Refresh();
        }

        [ContextMenu("Log ASPD → play rate table")]
        private void LogTable()
        {
            var builder = new System.Text.StringBuilder("ASPD | play rate | swing s | interval s | hits/s\n");
            for (float value = StatFormulas.MinAspd; value <= StatFormulas.MaxAspd + 0.01f; value += 5f)
            {
                float a = Mathf.Min(value, StatFormulas.MaxAspd);
                float interval = StatFormulas.AttackInterval(a);
                builder.AppendLine($"{a,5:0} | {StatFormulas.AttackPlayRate(a),4:0.00}x | {StatFormulas.SwingDuration(a),5:0.000} | {interval,5:0.000} | {1f / interval,4:0.00}");
            }

            Debug.Log(builder.ToString());
        }
    }
}
