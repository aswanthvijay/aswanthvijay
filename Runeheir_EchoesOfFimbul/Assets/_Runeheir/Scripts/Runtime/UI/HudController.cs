using System;
using System.Collections.Generic;
using Runeheir.Controls;
using Runeheir.Field;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Player;
using Runeheir.Online;
using Runeheir.Session;
using Runeheir.Social;
using Runeheir.World;
using Runeheir.WorldBuilding;
using UnityEngine;

namespace Runeheir.UI
{
    /// <summary>
    /// Builds and drives the in-game HUD. Keys (when not typing in chat):
    /// A or Alt+A status · S / Alt+S skills · E / Alt+E items · Q / Alt+Q equipment · Enter chat · Esc close / menu.
    /// Online (Phase 6): Z party · G guild · V street stall. Ctrl+Tab cycles the minimap. Also routes NPCs (shops, forge
    /// and repairs, courier storage/save/teleport, job master, Pushcart rental) to their windows.
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
        private CraftWindow _craft;
        private CardCompoundWindow _compound;
        private ConfirmDialog _confirm;
        private NpcActor _activeNpc;
        private ChatWindow _chat;
        private CastBarView _castBar;
        private BuffTray _buffs;
        private MinimapView _minimap;
        private MapBanner _banner;
        private SocialHud _social;
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

        /// <summary>Opens chat ready to whisper <paramref name="name"/>.</summary>
        public void StartWhisper(string name)
        {
            _chat.Prefill(name.Contains(" ") ? $"/w \"{name}\" " : $"/w {name} ");
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
                case NpcKind.Norns:
                    if (!_player.Record.Reborn)
                    {
                        options.Add(Option($"Ask for a new thread (rebirth, {RebirthRules.Fee:N0} z)", AskRebirth));
                    }

                    options.Add(Option("What is rebirth?", ExplainRebirth));
                    break;
                case NpcKind.CartMerchant:
                    if (!_player.Record.HasPushcart)
                    {
                        options.Add(Option($"Rent a Pushcart ({PushcartRules.RentalFee:N0} z)", RentPushcart));
                    }

                    options.Add(Option("Set up a street stall", () => _social.VendSetup.Toggle()));
                    if (_player.Record.Cart != null && _player.Record.Cart.Count > 0 && SocialHud.State?.MyStall == null)
                    {
                        options.Add(Option("Unload my Pushcart hold into my bag", () => _player.ReturnCartGoods()));
                    }

                    options.Add(Option("How does vending work?", ExplainVending));
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

        private void RentPushcart()
        {
            if (!IsAtNpc(NpcKind.CartMerchant))
            {
                return;
            }

            if (PushcartRules.TryRent(_player.Record, out string message))
            {
                _player.Recalculate(); // +8,000 weight
                _player.Inventory.NotifyChanged();
                ChatLog.System(message);
                _player.SaveNow();
            }
            else
            {
                ChatLog.Error(message);
            }
        }

        private void AskRebirth()
        {
            if (!IsAtNpc(NpcKind.Norns))
            {
                return;
            }

            var record = _player.Record;
            if (!RebirthRules.CanRebirth(record, out string reason))
            {
                ChatLog.Error(reason);
                return;
            }

            string goal = JobDatabase.TranscendentOf(record.Job)?.Name ?? "a transcendent job";
            Confirm($"Be reborn as a <b>High Initiate</b>?\nBase and Job Lv go back to 1 with {Stats.StatFormulas.StartingStatPoints + RebirthRules.BonusStatPoints} status points, " +
                    $"your skills are cleared, your gear goes to your bag and the Norns take {RebirthRules.Fee:N0} zeny.\n" +
                    $"Your new road leads to <b>{goal}</b>.", DoRebirth, "Be reborn");
        }

        private void DoRebirth()
        {
            if (!IsAtNpc(NpcKind.Norns))
            {
                return;
            }

            if (!_player.TryRebirth(out string reason))
            {
                ChatLog.Error(reason);
            }
        }

        private void ExplainRebirth()
        {
            var record = _player.Record;
            if (record.Reborn)
            {
                ChatLog.System($"Your thread is already rewoven. Keep climbing: Base Lv {RebirthRules.BaseLevelCap(record)}, and the transcendent jobs beyond your second job.");
                return;
            }

            ChatLog.System($"A second job (Berserker, Gothi, Skald...) at Base Lv {RebirthRules.NormalBaseLevelCap} and Job Lv {RebirthRules.MinJobLevel} can be reborn for " +
                           $"{RebirthRules.Fee:N0} zeny: you start again as a High Initiate with {Stats.StatFormulas.StartingStatPoints + RebirthRules.BonusStatPoints} status points, " +
                           $"+{RebirthRules.HpSpBonusPercent:0}% Max HP and SP and Base levels up to {Stats.StatFormulas.MaxBaseLevel}. Your road leads back through your first job " +
                           "to your second job's transcendent form: a Berserker becomes a High Warrior, then an Einherjar. Skills are cleared and gear goes to your bag.");
        }

        private static void ExplainVending()
        {
            ChatLog.System($"Rent a Pushcart (+{PushcartRules.WeightBonus:N0} weight), then press V anywhere in Vigrid Haven's streets: " +
                           $"put up to {VendingRules.MaxEntries} items on your stall with a price each and open it. Other players click you to buy; " +
                           "the zeny goes straight to you, and unsold goods come back when you close. Stalls need an online realm.");
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
                _buffs.SetRightInset(_minimap.OccupiedWidth);
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
            _craft = new CraftWindow(this, _player);
            _compound = new CardCompoundWindow(this, _player);
            _confirm = new ConfirmDialog(this);
            _social = new SocialHud(this, _player);
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
                // Our character's object was destroyed (an online warp brings a new one with its own HUD): don't linger.
                if (!ReferenceEquals(_player, null))
                {
                    Destroy(gameObject);
                }

                return;
            }

            _hotkeyBar.Tick();
            _castBar.Tick();
            _buffs.Tick();
            _minimap?.Tick();
            _banner?.Tick();
            _social.Tick();
            _chat.Tick();

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
                if (_minimap != null)
                {
                    _minimap.CycleSize();
                    _buffs.SetRightInset(_minimap.OccupiedWidth);
                }

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

            if (GameInput.KeyDown(GameKey.Z))
            {
                _social.Party.Toggle();
            }

            if (GameInput.KeyDown(GameKey.G))
            {
                _social.Guild.Toggle();
            }

            if (GameInput.KeyDown(GameKey.V))
            {
                _social.VendSetup.Toggle();
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
            var windows = new List<UIWindow>
            {
                _status.Window, _skills.Window, _inventory.Window, _equipment.Window, _jobChange.Window, _npcDialog.Window, _shop.Window,
                _storage.Window, _forge.Window, _compound.Window, _confirm.Window, _menu,
            };
            windows.AddRange(_social.Windows);
            foreach (var window in windows)
            {
                // The trade window closes through its Cancel button (both sides must know).
                if (window == _social.Trade.Window)
                {
                    continue;
                }

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
            _craft?.Dispose();
            _compound?.Dispose();
            NpcActor.Interacted -= OnNpcInteracted;
            _jobChange?.Dispose();
            _chat?.Dispose();
            _buffs?.Dispose();
            _social?.Dispose();
        }
    }
}
