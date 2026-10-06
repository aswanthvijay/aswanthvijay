using System;
using System.Collections.Generic;
using Runeheir.Controls;
using Runeheir.Field;
using Runeheir.Items;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.World;
using Runeheir.WorldBuilding;
using UnityEngine;

namespace Runeheir.UI
{
    /// <summary>
    /// Builds and drives the in-game HUD. Keys (when not typing in chat):
    /// A or Alt+A status · S / Alt+S skills · E / Alt+E items · Q / Alt+Q equipment · Enter chat · Esc close / menu.
    /// Ctrl+Tab cycles the minimap. Also routes NPCs (shops, forge and repairs, courier storage/save/teleport, job master)
    /// to their windows.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        private PlayerCharacter _player;
        private SkillCaster _caster;
        private FieldBootstrap _field;

        private BasicInfoWindow _basicInfo;
        private HotkeyBarView _hotkeyBar;
        private StatusWindow _status;
        private SkillWindow _skills;
        private InventoryWindow _inventory;
        private EquipmentWindow _equipment;
        private JobChangeWindow _jobChange;
        private NpcDialogWindow _npcDialog;
        private ShopWindow _shop;
        private StorageWindow _storage;
        private ForgeWindow _forge;
        private CardCompoundWindow _compound;
        private ConfirmDialog _confirm;
        private NpcActor _activeNpc;
        private ChatWindow _chat;
        private CastBarView _castBar;
        private BuffTray _buffs;
        private MinimapView _minimap;
        private MapBanner _banner;
        private UIWindow _menu;
        private UIWindow _deathDialog;
        private bool _chatFocusedLastFrame;
        private bool _typingLastFrame;

        public Canvas Canvas { get; private set; }

        public static HudController Create(PlayerCharacter player, FieldBootstrap field)
        {
            var canvas = UIFactory.CreateCanvas("HUD", 10);
            var hud = canvas.gameObject.AddComponent<HudController>();
            hud.Canvas = canvas;
            hud._player = player;
            hud._caster = player.GetComponent<SkillCaster>();
            hud._field = field;
            hud.Build();
            return hud;
        }

        public void ToggleStatus()
        {
            _status.Window.Toggle();
        }

        public void ToggleSkills()
        {
            _skills.Window.Toggle();
        }

        public void ToggleInventory()
        {
            _inventory.Window.Toggle();
        }

        public void ToggleEquipment()
        {
            _equipment.Window.Toggle();
        }

        public void ShowInventory()
        {
            _inventory.Window.Show();
        }

        /// <summary>While set (storage open), a double-click in the inventory calls this instead of using the item.</summary>
        public Action<ItemStack> InventoryClickOverride { get; private set; }

        public void SetInventoryClickOverride(Action<ItemStack> handler)
        {
            InventoryClickOverride = handler;
            _inventory.Refresh();
        }

        public void ClearInventoryClickOverride(Action<ItemStack> handler)
        {
            if (InventoryClickOverride == handler)
            {
                InventoryClickOverride = null;
                _inventory.Refresh();
            }
        }

        public void OpenCardCompound(string cardId)
        {
            _compound.Open(cardId);
        }

        public void Confirm(string message, Action onYes, string yesLabel = "Yes")
        {
            _confirm.Ask(message, onYes, yesLabel);
        }

        private void OnNpcInteracted(NpcActor npc)
        {
            if (_player == null || _player.IsDead)
            {
                return;
            }

            CloseNpcWindows();
            _activeNpc = npc;
            var options = new List<KeyValuePair<string, Action>>();
            switch (npc.Kind)
            {
                case NpcKind.Merchant:
                    var store = ShopCatalog.Get(npc.ShopId) ?? ShopCatalog.Get(ShopCatalog.GeneralStore);
                    options.Add(Option("Buy", () => _shop.Open(store, selling: false)));
                    options.Add(Option("Sell", () => _shop.Open(store, selling: true)));
                    break;
                case NpcKind.Forge:
                    int repair = RepairRules.TotalCost(_player.Record);
                    if (repair > 0)
                    {
                        options.Add(Option($"Repair broken weapons ({repair:N0} z)", RepairWeapons));
                    }

                    options.Add(Option("Refine equipment", () => _forge.Open(ForgeWindow.Mode.Refine)));
                    options.Add(Option("Runic Fuller: carve glyphs", () => _forge.Open(ForgeWindow.Mode.Etch)));
                    options.Add(Option("Extract Soul Cards", () => _forge.Open(ForgeWindow.Mode.Extract)));
                    options.Add(Option("Buy ores, runes and glyphs", () => _shop.Open(ShopCatalog.Get(ShopCatalog.ForgeSupplies), selling: false)));
                    break;
                case NpcKind.JobMaster:
                    options.Add(Option("Change job", () => _jobChange.Window.Show()));
                    break;
                default:
                    options.Add(Option("Open storage", _storage.Open));
                    if (FieldContext.Map != null)
                    {
                        bool saved = _player.Record.SaveMapId == FieldContext.MapId;
                        options.Add(Option(saved ? $"Save point: {FieldContext.MapName} (current)" : $"Save my return point at {FieldContext.MapName}", SaveHere));
                        options.Add(Option("Teleport", () => OpenTeleportMenu(npc)));
                    }

                    break;
            }

            options.Add(Option("Goodbye", null));
            _npcDialog.Open(npc, options);
        }

        private void RepairWeapons()
        {
            if (!IsAtNpc(NpcKind.Forge))
            {
                return;
            }

            if (RepairRules.TryRepairAll(_player.Record, out _, out string message))
            {
                _player.Equipment.NotifyChanged();
                _player.Inventory.NotifyChanged(); // zeny
                ChatLog.System(message);
                _player.SaveNow();
            }
            else
            {
                ChatLog.Error(message);
            }
        }

        private void SaveHere()
        {
            if (!IsAtNpc(NpcKind.Storage) || FieldContext.Map == null)
            {
                return;
            }

            _player.Record.SaveMapId = FieldContext.MapId;
            _player.SaveNow();
            ChatLog.System($"Your return point is now {FieldContext.MapName}. You'll revive here, and Raven Feathers bring you back.");
        }

        private void OpenTeleportMenu(NpcActor npc)
        {
            var options = new List<KeyValuePair<string, Action>>();
            foreach (var destination in MapCatalog.TeleportDestinations)
            {
                var map = MapCatalog.Get(destination.MapId);
                if (map == null || map.Id == FieldContext.MapId)
                {
                    continue;
                }

                var target = destination;
                string level = map.MinLevel > 0 ? $" · {map.LevelLabel}" : string.Empty;
                options.Add(Option($"{map.Name}{level} ({destination.Zeny:N0} z)", () => Teleport(target)));
            }

            options.Add(Option("Back", () => OnNpcInteracted(npc)));
            _activeNpc = npc;
            _npcDialog.Open(npc, options);
        }

        private void Teleport(TeleportDestination destination)
        {
            if (!IsAtNpc(NpcKind.Storage))
            {
                return;
            }

            if (_player.Record.Zeny < destination.Zeny)
            {
                ChatLog.Error($"The trip costs {destination.Zeny:N0} zeny.");
                return;
            }

            var map = MapCatalog.Get(destination.MapId);
            _player.Record.Zeny -= destination.Zeny;
            _player.Inventory.NotifyChanged(); // zeny
            if (!WorldTravel.Warp(_player, destination.MapId, null, $"The Norn Courier carries you to {map.Name}."))
            {
                _player.Record.Zeny += destination.Zeny; // couldn't travel: refund
                _player.Inventory.NotifyChanged();
            }
        }

        /// <summary>The big title when a map loads ("Whispering Woods · Lv 61–120").</summary>
        public void ShowMapBanner(MapDefinition map)
        {
            _banner?.Show(map);
        }

        private static KeyValuePair<string, Action> Option(string label, Action action)
        {
            return new KeyValuePair<string, Action>(label, action);
        }

        /// <summary>The player is still talking to (and within reach of) an NPC of this kind.</summary>
        public bool IsAtNpc(NpcKind kind)
        {
            return _activeNpc != null && _activeNpc.Kind == kind && _player != null && !_player.IsDead
                   && _activeNpc.EdgeDistanceTo(_player) <= NpcActor.LeaveRange;
        }

        private void CloseNpcWindows()
        {
            _npcDialog.Window.Hide();
            _shop.Window.Hide();
            _storage.Window.Hide();
            _forge.Window.Hide();
            _confirm.Window.Hide();
        }

        public void ToggleMenu()
        {
            _menu.Toggle();
        }

        public void ToggleJobChange()
        {
            _jobChange.Window.Toggle();
        }

        private void Build()
        {
            _chat = new ChatWindow(this, _player);
            _basicInfo = new BasicInfoWindow(this, _player);
            _hotkeyBar = new HotkeyBarView(this, _player);
            _castBar = new CastBarView(this, _player);
            _buffs = new BuffTray(this, _player);
            _banner = new MapBanner(this);
            if (FieldContext.Layout != null)
            {
                _minimap = new MinimapView(this, _player, FieldContext.Layout);
            }
            _status = new StatusWindow(this, _player);
            _skills = new SkillWindow(this, _player);
            _inventory = new InventoryWindow(this, _player);
            _equipment = new EquipmentWindow(this, _player);
            _jobChange = new JobChangeWindow(this, _player);
            _npcDialog = new NpcDialogWindow(this);
            _shop = new ShopWindow(this, _player);
            _storage = new StorageWindow(this, _player);
            _forge = new ForgeWindow(this, _player);
            _compound = new CardCompoundWindow(this, _player);
            _confirm = new ConfirmDialog(this);
            NpcActor.Interacted += OnNpcInteracted;
            _menu = BuildMenu();
            _deathDialog = BuildDeathDialog();
            UITooltip.Create(Canvas);
        }

        private UIWindow BuildMenu()
        {
            var menu = UIWindow.Create(Canvas.transform, "Menu", 810f, 380f, 300f, 230f);
            menu.CenterOnShow = true;
            string[] labels = { "Character Select", "Exit Game", "Return to Game" };
            UnityEngine.Events.UnityAction[] actions =
            {
                () => _field.ReturnToCharacterSelect(),
                GameSession.QuitGame,
                () => menu.Hide(),
            };
            for (int i = 0; i < labels.Length; i++)
            {
                var button = UIFactory.CreateButton(menu.Content, labels[i], actions[i], 17);
                button.GetComponent<RectTransform>().SetRect(10f, 10f + i * 52f, 260f, 42f);
            }

            menu.Hide();
            return menu;
        }

        private UIWindow BuildDeathDialog()
        {
            var dialog = UIWindow.Create(Canvas.transform, "Fallen", 760f, 420f, 400f, 170f, closable: false);
            dialog.CenterOnShow = true;
            var text = UIFactory.CreateText(dialog.Content, "You have fallen in the Fimbulwinter...", 18, UITheme.Error, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.SetRect(0f, 4f, 380f, 40f);
            var button = UIFactory.CreateButton(dialog.Content, "Return to Save Point", () => _player.RespawnAtSavePoint(), 17);
            button.GetComponent<RectTransform>().SetRect(70f, 60f, 240f, 44f);
            dialog.Hide();
            return dialog;
        }

        private void Update()
        {
            if (_player == null)
            {
                return;
            }

            _hotkeyBar.Tick();
            _castBar.Tick();
            _buffs.Tick();
            _minimap?.Tick();
            _banner?.Tick();

            if (_player.IsDead != _deathDialog.IsOpen)
            {
                if (_player.IsDead)
                {
                    _deathDialog.Show();
                }
                else
                {
                    _deathDialog.Hide();
                }
            }

            // Walking away from (or dying near) an NPC closes its windows, like Ragnarok.
            if (!ReferenceEquals(_activeNpc, null) && (_activeNpc == null || _player.IsDead || _activeNpc.EdgeDistanceTo(_player) > NpcActor.LeaveRange))
            {
                _activeNpc = null;
                CloseNpcWindows();
            }

            HandleKeys();
            _chatFocusedLastFrame = _chat.IsFocused;
            _typingLastFrame = UIFocus.IsTyping;
        }

        private void HandleKeys()
        {
            if (GameInput.KeyDown(GameKey.Escape))
            {
                HandleEscape();
                return;
            }

            if (_chat.IsFocused || UIFocus.IsTyping)
            {
                return;
            }

            // Enter that just finished typing in another field (a shop or storage amount) must not also open chat.
            if (GameInput.KeyDown(GameKey.Enter) && _chat.LastSubmitFrame != Time.frameCount && !_typingLastFrame)
            {
                _chat.Focus();
                return;
            }

            if (GameInput.KeyDown(GameKey.Tab) && GameInput.KeyHeld(GameKey.Ctrl))
            {
                _minimap?.CycleSize();
                return;
            }

            if (GameInput.KeyDown(GameKey.A))
            {
                ToggleStatus();
            }

            if (GameInput.KeyDown(GameKey.S))
            {
                ToggleSkills();
            }

            if (GameInput.KeyDown(GameKey.E))
            {
                ToggleInventory();
            }

            if (GameInput.KeyDown(GameKey.Q))
            {
                ToggleEquipment();
            }
        }

        private void HandleEscape()
        {
            // The InputField may already have dropped focus on this Escape press.
            if (_chat.IsFocused || _chatFocusedLastFrame)
            {
                _chat.Blur();
                return;
            }

            if (_caster.IsTargeting)
            {
                _caster.CancelTargeting();
                return;
            }

            // Close the top-most open window first (Ragnarok behaviour), then toggle the menu.
            UIWindow top = null;
            foreach (var window in new[]
                     {
                         _status.Window, _skills.Window, _inventory.Window, _equipment.Window, _jobChange.Window, _npcDialog.Window, _shop.Window,
                         _storage.Window, _forge.Window, _compound.Window, _confirm.Window, _menu,
                     })
            {
                if (window.IsOpen && (top == null || window.transform.GetSiblingIndex() > top.transform.GetSiblingIndex()))
                {
                    top = window;
                }
            }

            if (top != null)
            {
                top.Hide();
            }
            else
            {
                _menu.Show();
            }
        }

        private void OnDestroy()
        {
            _basicInfo?.Dispose();
            _hotkeyBar?.Dispose();
            _status?.Dispose();
            _skills?.Dispose();
            _inventory?.Dispose();
            _equipment?.Dispose();
            _shop?.Dispose();
            _forge?.Dispose();
            _compound?.Dispose();
            NpcActor.Interacted -= OnNpcInteracted;
            _jobChange?.Dispose();
            _chat?.Dispose();
            _buffs?.Dispose();
        }
    }
}
