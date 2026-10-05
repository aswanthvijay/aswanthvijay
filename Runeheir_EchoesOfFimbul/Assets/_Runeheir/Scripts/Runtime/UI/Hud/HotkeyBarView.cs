using Runeheir.Hotkeys;
using Runeheir.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// The F1–F10 bar. Drop a skill (Alt+S) or item (Alt+E) on a slot to bind it, drag between slots to
    /// swap, drag off the bar or right-click to clear, click a slot to use it. Shows item counts and cooldowns.
    /// </summary>
    public sealed class HotkeyBarView
    {
        private const float SlotSize = 56f;
        private const float Gap = 5f;

        private readonly PlayerCharacter _player;
        private readonly HotkeyController _controller;
        private readonly SkillCaster _caster;
        private readonly SlotView[] _slots = new SlotView[HotkeyLayout.SlotsPerPage];
        private readonly Text _pageLabel;

        public HotkeyBarView(HudController hud, PlayerCharacter player)
        {
            _player = player;
            _controller = player.GetComponent<HotkeyController>();
            _caster = player.GetComponent<SkillCaster>();

            float width = HotkeyLayout.SlotsPerPage * (SlotSize + Gap) + 60f;
            var frame = UIFactory.CreateFramedPanel(hud.Canvas.transform, "HotkeyBar", UITheme.WindowBg);
            frame.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(width, SlotSize + 18f));

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = CreateSlot(frame.transform, i, 10f + i * (SlotSize + Gap), 9f);
            }

            var pageButton = UIFactory.CreateButton(frame.transform, "1", () => _controller.SetPage(_controller.CurrentPage + 1), 16);
            pageButton.GetComponent<RectTransform>().SetRect(width - 50f, 9f, 40f, SlotSize);
            _pageLabel = pageButton.GetComponentInChildren<Text>();
            var pageHint = pageButton.gameObject.AddComponent<UIPointerHandler>();
            pageHint.PointerEnter = () => UITooltip.Show("Hotkey page (F12 cycles pages 1–4)");
            pageHint.PointerExit = UITooltip.Hide;

            player.Hotkeys.Changed += Refresh;
            player.Inventory.Changed += Refresh;
            player.Progression.JobChanged += Refresh;
            _controller.PageChanged += Refresh;
            _controller.SlotActivated += OnSlotActivated;
            Refresh();
        }

        public void Dispose()
        {
            _player.Hotkeys.Changed -= Refresh;
            _player.Inventory.Changed -= Refresh;
            _player.Progression.JobChanged -= Refresh;
            _controller.PageChanged -= Refresh;
            _controller.SlotActivated -= OnSlotActivated;
        }

        public void Refresh()
        {
            _pageLabel.text = $"{_controller.CurrentPage + 1}";
            for (int i = 0; i < _slots.Length; i++)
            {
                var view = _slots[i];
                var slot = _player.Hotkeys.Get(_controller.CurrentPage, i);
                view.KeyLabel.text = _controller.KeyForSlot(i).ToString();
                bool empty = slot.IsEmpty;
                view.Icon.gameObject.SetActive(!empty);
                view.Amount.text = string.Empty;
                if (empty)
                {
                    continue;
                }

                view.Icon.color = HudIcons.Tint(slot);
                view.IconText.text = HudIcons.Glyph(slot);
                bool usable = true;
                if (slot.Kind == HotkeyKind.Item)
                {
                    int count = _player.Inventory.Count(slot.Id);
                    view.Amount.text = count.ToString();
                    usable = count > 0;
                }
                else if (slot.Kind == HotkeyKind.Skill)
                {
                    usable = Skills.SkillCatalog.CanUse(_player.Record.Job, slot.Id);
                }

                var tint = view.Icon.color;
                tint.a = usable ? 1f : 0.3f;
                view.Icon.color = tint;
            }
        }

        public void Tick()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var view = _slots[i];
                var slot = _player.Hotkeys.Get(_controller.CurrentPage, i);
                float remaining = 0f;
                float total = 0f;
                if (slot.Kind == HotkeyKind.Skill)
                {
                    remaining = _caster.GetCooldown(slot.Id, out total);
                }

                bool cooling = remaining > 0.01f && total > 0.01f;
                view.Cooldown.gameObject.SetActive(cooling);
                view.CooldownText.gameObject.SetActive(cooling);
                if (cooling)
                {
                    view.Cooldown.rectTransform.anchorMax = new Vector2(1f, Mathf.Clamp01(remaining / total));
                    view.CooldownText.text = remaining >= 1f ? remaining.ToString("0") : remaining.ToString("0.0");
                }

                if (view.FlashUntil > Time.time)
                {
                    float alpha = (view.FlashUntil - Time.time) / 0.25f;
                    view.Flash.color = new Color(1f, 0.9f, 0.5f, 0.45f * alpha);
                }
                else if (view.Flash.color.a > 0f)
                {
                    view.Flash.color = Color.clear;
                }
            }
        }

        private void OnSlotActivated(int flatIndex)
        {
            int page = flatIndex / HotkeyLayout.SlotsPerPage;
            if (page == _controller.CurrentPage)
            {
                _slots[flatIndex % HotkeyLayout.SlotsPerPage].FlashUntil = Time.time + 0.25f;
            }
        }

        private SlotView CreateSlot(Transform parent, int slotInPage, float x, float y)
        {
            var view = new SlotView();
            var background = UIFactory.CreatePanel(parent, $"Slot{slotInPage + 1}", UITheme.SlotBg);
            background.rectTransform.SetRect(x, y, SlotSize, SlotSize);
            UIFactory.AddOutline(background, new Color(UITheme.Gold.r, UITheme.Gold.g, UITheme.Gold.b, 0.35f), 1f);

            view.Icon = UIFactory.CreateIcon(background.transform, string.Empty, Color.gray, 16);
            view.Icon.rectTransform.Stretch(4f, 4f, 4f, 4f);
            view.IconText = view.Icon.GetComponentInChildren<Text>();

            view.Cooldown = UIFactory.CreatePanel(background.transform, "Cooldown", new Color(0f, 0f, 0f, 0.65f), rounded: false, blocksRaycasts: false);
            view.Cooldown.rectTransform.anchorMin = Vector2.zero;
            view.Cooldown.rectTransform.anchorMax = Vector2.one;
            view.Cooldown.rectTransform.offsetMin = new Vector2(4f, 4f);
            view.Cooldown.rectTransform.offsetMax = new Vector2(-4f, -4f);
            view.CooldownText = UIFactory.CreateText(background.transform, string.Empty, 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            view.CooldownText.rectTransform.Stretch();
            UIFactory.AddOutline(view.CooldownText, Color.black, 1f);

            view.Flash = UIFactory.CreatePanel(background.transform, "Flash", Color.clear, rounded: true, blocksRaycasts: false);
            view.Flash.rectTransform.Stretch();

            view.KeyLabel = UIFactory.CreateText(background.transform, string.Empty, 11, UITheme.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            view.KeyLabel.rectTransform.Stretch(4f, 1f, 2f, 2f);
            UIFactory.AddOutline(view.KeyLabel, Color.black, 1f);

            view.Amount = UIFactory.CreateText(background.transform, string.Empty, 13, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
            view.Amount.rectTransform.Stretch(2f, 2f, 5f, 3f);
            UIFactory.AddOutline(view.Amount, Color.black, 1f);

            int index = slotInPage;
            var drop = background.gameObject.AddComponent<UIDropTarget>();
            drop.Dropped = payload => OnDrop(index, payload);

            var drag = background.gameObject.AddComponent<UIDragSource>();
            drag.PayloadProvider = () => CreatePayload(index);
            drag.Ended = accepted =>
            {
                if (!accepted)
                {
                    _player.Hotkeys.Clear(FlatIndex(index));
                }
            };

            var pointer = background.gameObject.AddComponent<UIPointerHandler>();
            pointer.LeftClick = () => _controller.Activate(FlatIndex(index));
            pointer.RightClick = () => _player.Hotkeys.Clear(FlatIndex(index));
            pointer.PointerEnter = () => UITooltip.Show(HudIcons.Tooltip(_player.Hotkeys.Get(FlatIndex(index))));
            pointer.PointerExit = UITooltip.Hide;
            return view;
        }

        private int FlatIndex(int slotInPage)
        {
            return HotkeyLayout.ToIndex(_controller.CurrentPage, slotInPage);
        }

        private DragPayload CreatePayload(int slotInPage)
        {
            var slot = _player.Hotkeys.Get(FlatIndex(slotInPage));
            if (slot.IsEmpty)
            {
                return null;
            }

            return new DragPayload
            {
                Slot = slot,
                SourceHotkeyIndex = FlatIndex(slotInPage),
                Glyph = HudIcons.Glyph(slot),
                Color = HudIcons.Tint(slot),
            };
        }

        private void OnDrop(int slotInPage, DragPayload payload)
        {
            int target = FlatIndex(slotInPage);
            if (payload.SourceHotkeyIndex >= 0)
            {
                _player.Hotkeys.Swap(payload.SourceHotkeyIndex, target);
            }
            else
            {
                _player.Hotkeys.Assign(target, payload.Slot);
            }
        }

        private sealed class SlotView
        {
            public Image Icon;
            public Text IconText;
            public Image Cooldown;
            public Text CooldownText;
            public Image Flash;
            public Text KeyLabel;
            public Text Amount;
            public float FlashUntil;
        }
    }
}
