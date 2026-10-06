using System;
using Runeheir.Accounts;
using Runeheir.Characters;
using Runeheir.Controls;
using Runeheir.Jobs;
using Runeheir.UI;
using Runeheir.Visuals;
using Runeheir.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runeheir.FrontEnd
{
    /// <summary>
    /// Ragnarok/XileRO character select: 9 slots (3x3), stat sheet on the left, turntable preview,
    /// Start / Create / Delete. Arrow keys move, Enter starts (or creates on an empty slot), Delete deletes.
    /// </summary>
    public sealed class CharacterSelectScreen : FrontEndScreen
    {
        private const int Columns = 3;
        private const float CardWidth = 210f;
        private const float CardHeight = 112f;
        private const float CardGap = 12f;

        private readonly CharacterRecord[] _bySlot = new CharacterRecord[AccountRules.MaxCharacterSlots];
        private readonly Card[] _cards = new Card[AccountRules.MaxCharacterSlots];
        private readonly Text _header;
        private readonly Text _infoName;
        private readonly Text _infoJob;
        private readonly Text _infoLeft;
        private readonly Text _infoRight;
        private readonly Text _status;
        private readonly Button _startButton;
        private readonly Button _deleteButton;
        private readonly RectTransform _deleteDialog;
        private readonly InputField _deleteInput;
        private readonly Text _deletePrompt;
        private int _selected;

        public CharacterSelectScreen(FrontEndController context) : base(context, "CharacterSelectScreen")
        {
            _header = UIFactory.CreateText(Frame, string.Empty, 20, UITheme.Frost, TextAnchor.MiddleCenter, FontStyle.Bold);
            _header.rectTransform.SetRect(0f, 196f, 1920f, 30f);
            UIFactory.AddShadow(_header, 1.5f);

            // --- Info sheet (left)
            var info = UIFactory.CreateFramedPanel(Frame, "InfoPanel", UITheme.WindowBg);
            info.rectTransform.SetRect(170f, 240f, 380f, 600f);
            var infoContent = UIFactory.CreateRect("Content", info.transform);
            infoContent.Stretch(22f, 18f, 22f, 18f);
            _infoName = UIFactory.CreateText(infoContent, string.Empty, 26, UITheme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            _infoName.rectTransform.SetRect(0f, 0f, 336f, 34f);
            _infoJob = UIFactory.CreateText(infoContent, string.Empty, 17, UITheme.Frost, TextAnchor.MiddleLeft, FontStyle.Italic);
            _infoJob.rectTransform.SetRect(0f, 36f, 336f, 24f);
            _infoLeft = UIFactory.CreateText(infoContent, string.Empty, 16, UITheme.TextDim, TextAnchor.UpperLeft);
            _infoLeft.rectTransform.SetRect(0f, 76f, 130f, 480f);
            _infoLeft.lineSpacing = 1.3f;
            _infoRight = UIFactory.CreateText(infoContent, string.Empty, 16, UITheme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            _infoRight.rectTransform.SetRect(130f, 76f, 206f, 480f);
            _infoRight.lineSpacing = 1.3f;

            // --- Preview (center)
            var previewFrame = UIFactory.CreateFramedPanel(Frame, "PreviewPanel", UITheme.WindowBg);
            previewFrame.rectTransform.SetRect(580f, 240f, 440f, 600f);
            var preview = UIFactory.CreateRect("Preview", previewFrame.transform).gameObject.AddComponent<RawImage>();
            preview.rectTransform.Stretch(10f, 10f, 10f, 64f);
            preview.texture = context.Preview.Texture;
            preview.raycastTarget = false;
            var rotateLeft = UIFactory.CreateButton(previewFrame.transform, "<", () => context.Preview.Rotate(-45f), 20);
            rotateLeft.GetComponent<RectTransform>().SetRect(120f, 548f, 90f, 40f);
            var rotateRight = UIFactory.CreateButton(previewFrame.transform, ">", () => context.Preview.Rotate(45f), 20);
            rotateRight.GetComponent<RectTransform>().SetRect(230f, 548f, 90f, 40f);

            // --- Slots (right)
            float gridX = 1050f;
            float gridY = 240f;
            for (int slot = 0; slot < _cards.Length; slot++)
            {
                int column = slot % Columns;
                int row = slot / Columns;
                _cards[slot] = CreateCard(slot, gridX + column * (CardWidth + CardGap), gridY + row * (CardHeight + CardGap));
            }

            float gridWidth = Columns * CardWidth + (Columns - 1) * CardGap;
            float buttonsY = gridY + 3 * (CardHeight + CardGap) + 8f;
            _startButton = UIFactory.CreateButton(Frame, "Start", StartSelected, 22);
            _startButton.GetComponent<RectTransform>().SetRect(gridX, buttonsY, gridWidth, 56f);

            float small = (gridWidth - 2 * CardGap) / 3f;
            var create = UIFactory.CreateButton(Frame, "Create", CreateOnSelected, 17);
            create.GetComponent<RectTransform>().SetRect(gridX, buttonsY + 68f, small, 42f);
            _deleteButton = UIFactory.CreateButton(Frame, "Delete", OpenDeleteDialog, 17);
            _deleteButton.GetComponent<RectTransform>().SetRect(gridX + small + CardGap, buttonsY + 68f, small, 42f);
            var back = UIFactory.CreateButton(Frame, "Logout", OnBack, 17);
            back.GetComponent<RectTransform>().SetRect(gridX + 2 * (small + CardGap), buttonsY + 68f, small, 42f);

            _status = CreateStatus(Frame, gridX, buttonsY + 120f, gridWidth);

            // --- Delete confirmation (modal)
            _deleteDialog = BuildDeleteDialog(out _deleteInput, out _deletePrompt);
        }

        public int PreferredSlot { get; set; } = -1;

        public override void HandleKeys()
        {
            if (Busy)
            {
                return;
            }

            if (_deleteDialog.gameObject.activeSelf)
            {
                if (GameInput.KeyDown(GameKey.Escape))
                {
                    _deleteDialog.gameObject.SetActive(false);
                }
                else if (GameInput.KeyDown(GameKey.Enter))
                {
                    ConfirmDelete();
                }

                return;
            }

            if (GameInput.KeyDown(GameKey.Escape))
            {
                OnBack();
            }
            else if (GameInput.KeyDown(GameKey.Enter))
            {
                if (_bySlot[_selected] != null)
                {
                    StartSelected();
                }
                else
                {
                    CreateOnSelected();
                }
            }
            else if (GameInput.KeyDown(GameKey.Delete))
            {
                OpenDeleteDialog();
            }
            else if (GameInput.KeyDown(GameKey.Left))
            {
                Select((_selected + _cards.Length - 1) % _cards.Length);
            }
            else if (GameInput.KeyDown(GameKey.Right))
            {
                Select((_selected + 1) % _cards.Length);
            }
            else if (GameInput.KeyDown(GameKey.Up))
            {
                Select((_selected + _cards.Length - Columns) % _cards.Length);
            }
            else if (GameInput.KeyDown(GameKey.Down))
            {
                Select((_selected + Columns) % _cards.Length);
            }
        }

        protected override async void OnShow()
        {
            _deleteDialog.gameObject.SetActive(false);
            Array.Clear(_bySlot, 0, _bySlot.Length);
            var session = Context.Session;
            _header.text = $"{session.Server?.Name ?? "Vigrid Haven"}  ·  {session.Username}";
            SetStatus(_status, "Loading characters...");
            int visit = Visit;
            Busy = true;
            try
            {
                var characters = await session.Accounts.GetCharactersAsync(session.Username);
                if (!IsCurrent(visit))
                {
                    return;
                }

                foreach (var character in characters)
                {
                    if (character.Slot >= 0 && character.Slot < _bySlot.Length)
                    {
                        _bySlot[character.Slot] = character;
                    }
                }

                SetStatus(_status, characters.Count == 0 ? "No characters yet — pick a slot and press Create." : string.Empty);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (!IsCurrent(visit))
                {
                    return;
                }

                SetStatus(_status, "Could not load characters.", error: true);
            }
            finally
            {
                if (IsCurrent(visit))
                {
                    Busy = false;
                }
            }

            RefreshCards();
            int initial = PreferredSlot >= 0 ? PreferredSlot : Array.FindIndex(_bySlot, c => c != null);
            Select(Mathf.Max(0, initial));
            Context.Preview.ResetRotation();
        }

        private Card CreateCard(int slot, float x, float y)
        {
            var card = new Card();
            card.Background = UIFactory.CreatePanel(Frame, "Slot" + (slot + 1), UITheme.SlotBg);
            card.Background.rectTransform.SetRect(x, y, CardWidth, CardHeight);
            card.Outline = card.Background.gameObject.AddComponent<Outline>();
            card.Outline.effectDistance = new Vector2(2f, -2f);

            card.Title = UIFactory.CreateText(card.Background.transform, string.Empty, 18, UITheme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            card.Title.rectTransform.Stretch(14f, 12f, 10f, 50f);
            card.Subtitle = UIFactory.CreateText(card.Background.transform, string.Empty, 14, UITheme.TextDim, TextAnchor.LowerLeft);
            card.Subtitle.rectTransform.Stretch(14f, 50f, 10f, 12f);
            var number = UIFactory.CreateText(card.Background.transform, (slot + 1).ToString(), 13, UITheme.TextDim, TextAnchor.UpperRight);
            number.rectTransform.Stretch(10f, 8f, 10f, 10f);

            var pointer = card.Background.gameObject.AddComponent<UIPointerHandler>();
            // Cards are not Selectables, so the Busy CanvasGroup doesn't block them: check Busy here.
            pointer.LeftClick = () =>
            {
                if (!Busy)
                {
                    Select(slot);
                }
            };
            pointer.DoubleClick = () =>
            {
                if (Busy)
                {
                    return;
                }

                Select(slot);
                if (_bySlot[slot] != null)
                {
                    StartSelected();
                }
                else
                {
                    CreateOnSelected();
                }
            };
            return card;
        }

        private void RefreshCards()
        {
            for (int slot = 0; slot < _cards.Length; slot++)
            {
                var record = _bySlot[slot];
                var card = _cards[slot];
                if (record != null)
                {
                    card.Title.text = record.Name;
                    card.Title.color = UITheme.Gold;
                    card.Subtitle.text = $"{JobDatabase.Get(record.Job).Name}\nLv {record.BaseLevel} / {record.JobLevel}";
                }
                else
                {
                    card.Title.text = "Empty Slot";
                    card.Title.color = UITheme.TextDim;
                    card.Subtitle.text = "+ Create Character";
                }
            }
        }

        private void Select(int slot)
        {
            _selected = Mathf.Clamp(slot, 0, _cards.Length - 1);
            for (int i = 0; i < _cards.Length; i++)
            {
                bool selected = i == _selected;
                _cards[i].Background.color = selected ? new Color(0.16f, 0.22f, 0.32f, 0.98f) : UITheme.SlotBg;
                _cards[i].Outline.effectColor = selected ? UITheme.Gold : new Color(0f, 0f, 0f, 0.3f);
            }

            var record = _bySlot[_selected];
            _startButton.interactable = record != null;
            _deleteButton.interactable = record != null;
            if (record == null)
            {
                _infoName.text = "Empty Slot";
                _infoJob.text = "Create a new Initiate to begin.";
                _infoLeft.text = string.Empty;
                _infoRight.text = string.Empty;
                Context.Preview.ShowEmpty();
                return;
            }

            var progression = new CharacterProgression(record);
            var map = MapCatalog.Get(record.MapId);
            _infoName.text = record.Name;
            _infoJob.text = progression.Job.Name;
            _infoLeft.text = "Base Lv\nJob Lv\nBase EXP\nJob EXP\nMap\n\nSTR\nAGI\nVIT\nINT\nDEX\nLUK";
            _infoRight.text =
                $"{record.BaseLevel}\n{record.JobLevel}\n{progression.BaseExpPercent:0.0}%\n{progression.JobExpPercent:0.0}%\n{map?.Name ?? record.MapId}\n\n" +
                $"{record.Stats.Str}\n{record.Stats.Agi}\n{record.Stats.Vit}\n{record.Stats.Int}\n{record.Stats.Dex}\n{record.Stats.Luk}";
            Context.Preview.ShowLook(AvatarLook.FromRecord(record));
        }

        private void StartSelected()
        {
            var record = _bySlot[_selected];
            if (record == null || Busy)
            {
                return;
            }

            SetStatus(_status, $"Entering {MapCatalog.Get(record.MapId)?.Name ?? "Midgard"}...");
            Context.Session.EnterWorld(record);
        }

        private void CreateOnSelected()
        {
            // While the list is loading every slot looks empty; don't open Create for a slot that may be taken.
            if (Busy)
            {
                return;
            }

            if (_bySlot[_selected] != null)
            {
                SetStatus(_status, "That slot is taken. Choose an empty slot to create a character.", error: true);
                return;
            }

            Context.ShowCharacterCreate(_selected);
        }

        private void OnBack()
        {
            Context.Session.Logout();
            Context.ShowLogin();
        }

        private RectTransform BuildDeleteDialog(out InputField input, out Text prompt)
        {
            var shade = UIFactory.CreatePanel(Root, "DeleteDialog", new Color(0f, 0f, 0f, 0.6f), rounded: false);
            shade.rectTransform.Stretch();
            var panel = UIFactory.CreateFramedPanel(shade.transform, "Panel", UITheme.WindowBg);
            panel.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 250f));
            var content = UIFactory.CreateRect("Content", panel.transform);
            content.Stretch(24f, 18f, 24f, 18f);

            CreateHeading(content, "Delete Character", 0f, 472f);
            prompt = UIFactory.CreateText(content, string.Empty, 15, UITheme.Text, TextAnchor.MiddleCenter);
            prompt.rectTransform.SetRect(0f, 40f, 472f, 44f);
            input = UIFactory.CreateInputField(content, "Character name", characterLimit: AccountRules.MaxCharacterNameLength);
            input.GetComponent<RectTransform>().SetRect(60f, 92f, 352f, 36f);

            var confirm = UIFactory.CreateButton(content, "Delete", ConfirmDelete, 17);
            confirm.GetComponent<RectTransform>().SetRect(60f, 146f, 168f, 42f);
            var cancel = UIFactory.CreateButton(content, "Cancel", () => _deleteDialog.gameObject.SetActive(false), 17);
            cancel.GetComponent<RectTransform>().SetRect(244f, 146f, 168f, 42f);
            shade.gameObject.SetActive(false);
            return shade.rectTransform;
        }

        private void OpenDeleteDialog()
        {
            var record = _bySlot[_selected];
            if (record == null)
            {
                return;
            }

            _deletePrompt.text = $"This cannot be undone. Type <b>{record.Name}</b> to delete this Lv {record.BaseLevel} {JobDatabase.Get(record.Job).Name}.";
            _deleteInput.text = string.Empty;
            _deleteDialog.gameObject.SetActive(true);
            _deleteDialog.SetAsLastSibling();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_deleteInput.gameObject);
            }

            _deleteInput.ActivateInputField();
        }

        private async void ConfirmDelete()
        {
            var record = _bySlot[_selected];
            if (record == null || Busy)
            {
                return;
            }

            int visit = Visit;
            Busy = true;
            try
            {
                var session = Context.Session;
                var result = await session.Accounts.DeleteCharacterAsync(session.Username, record.Slot, _deleteInput.text);
                if (!IsCurrent(visit))
                {
                    return;
                }

                if (!result.Success)
                {
                    _deletePrompt.text = $"<color=#FF7A6B>{result.Error}</color>";
                    return;
                }

                _deleteDialog.gameObject.SetActive(false);
                _bySlot[record.Slot] = null;
                RefreshCards();
                Select(record.Slot);
                SetStatus(_status, $"{record.Name} has gone to Valhalla.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (IsCurrent(visit))
                {
                    SetStatus(_status, "Delete failed.", error: true);
                }
            }
            finally
            {
                if (IsCurrent(visit))
                {
                    Busy = false;
                }
            }
        }

        private sealed class Card
        {
            public Image Background;
            public Outline Outline;
            public Text Title;
            public Text Subtitle;
        }
    }
}
