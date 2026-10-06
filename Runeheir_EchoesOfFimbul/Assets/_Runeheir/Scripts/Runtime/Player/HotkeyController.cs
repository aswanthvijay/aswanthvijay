using System;
using Runeheir.Controls;
using Runeheir.Hotkeys;
using UnityEngine;

namespace Runeheir.Player
{
    /// <summary>
    /// F1–F10 hotkey bar (any slot holds a skill or an item). F12 flips between the 4 bar pages,
    /// so 40 shortcuts are reachable. Keys are the slotKeys / nextPageKey defaults below.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class HotkeyController : MonoBehaviour
    {
        [SerializeField] private GameKey[] slotKeys =
        {
            GameKey.F1, GameKey.F2, GameKey.F3, GameKey.F4, GameKey.F5,
            GameKey.F6, GameKey.F7, GameKey.F8, GameKey.F9, GameKey.F10,
        };

        [SerializeField] private GameKey nextPageKey = GameKey.F12;

        private PlayerCharacter _player;
        private SkillCaster _caster;
        private ClickToMoveController _clicker;

        public event Action PageChanged;

        /// <summary>(flat slot index) — the HUD flashes the slot.</summary>
        public event Action<int> SlotActivated;

        public int CurrentPage { get; private set; }

        public GameKey KeyForSlot(int slotInPage)
        {
            return slotInPage >= 0 && slotInPage < slotKeys.Length ? slotKeys[slotInPage] : GameKey.F1;
        }

        public void SetPage(int page)
        {
            int clamped = ((page % HotkeyLayout.PageCount) + HotkeyLayout.PageCount) % HotkeyLayout.PageCount;
            if (clamped == CurrentPage)
            {
                return;
            }

            CurrentPage = clamped;
            PageChanged?.Invoke();
        }

        /// <summary>Uses whatever is in the slot (keyboard, or clicking the slot in the HUD).</summary>
        public void Activate(int flatIndex)
        {
            if (_player.IsDead)
            {
                return;
            }

            var slot = _player.Hotkeys.Get(flatIndex);
            switch (slot.Kind)
            {
                case HotkeyKind.Skill:
                    _caster.RequestSkill(slot.Id, _clicker != null ? _clicker.HoveredEntity : null, _clicker != null ? _clicker.HoveredGround : null);
                    break;
                case HotkeyKind.Item:
                    _player.UseItem(slot.Id);
                    break;
                default:
                    return;
            }

            SlotActivated?.Invoke(flatIndex);
        }

        private void Awake()
        {
            _player = GetComponent<PlayerCharacter>();
            _caster = GetComponent<SkillCaster>();
            _clicker = GetComponent<ClickToMoveController>();
        }

        private void Update()
        {
            if (_player.Record == null || UIFocus.IsTyping)
            {
                return;
            }

            int count = Mathf.Min(slotKeys.Length, HotkeyLayout.SlotsPerPage);
            for (int i = 0; i < count; i++)
            {
                if (GameInput.KeyDown(slotKeys[i]))
                {
                    Activate(HotkeyLayout.ToIndex(CurrentPage, i));
                }
            }

            if (GameInput.KeyDown(nextPageKey))
            {
                SetPage(CurrentPage + 1);
            }
        }
    }
}
