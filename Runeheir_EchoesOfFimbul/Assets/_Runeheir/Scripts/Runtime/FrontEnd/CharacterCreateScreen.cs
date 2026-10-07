using System;
using Runeheir.Accounts;
using Runeheir.Characters;
using Runeheir.Controls;
using Runeheir.Jobs;
using Runeheir.Stats;
using Runeheir.UI;
using Runeheir.Visuals;
using Runeheir.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runeheir.FrontEnd
{
    /// <summary>
    /// New character: name, gender, hair style and hair color with a live turntable preview.
    /// Everyone starts as an Initiate with all stats at 1 and 48 status points (spent in-game, Alt+A).
    /// </summary>
    public sealed class CharacterCreateScreen : FrontEndScreen
    {
        private const float PanelWidth = 560f;

        private readonly InputField _name;
        private readonly Button _male;
        private readonly Button _female;
        private readonly Button _human;
        private readonly Button _doram;
        private readonly Text _info;
        private readonly Text _hairStyleLabel;
        private readonly Text _hairColorLabel;
        private readonly Image _hairSwatch;
        private readonly Text _status;
        private readonly Button _createButton;

        private Gender _gender = Gender.Female;
        private CharacterRace _race = CharacterRace.Human;
        private int _hairStyle;
        private int _hairColor;

        public CharacterCreateScreen(FrontEndController context) : base(context, "CharacterCreateScreen")
        {
            var previewFrame = UIFactory.CreateFramedPanel(Frame, "PreviewPanel", UITheme.WindowBg);
            previewFrame.rectTransform.SetRect(380f, 240f, 440f, 600f);
            var preview = UIFactory.CreateRect("Preview", previewFrame.transform).gameObject.AddComponent<RawImage>();
            preview.rectTransform.Stretch(10f, 10f, 10f, 64f);
            preview.texture = context.Preview.Texture;
            preview.raycastTarget = false;
            var rotateLeft = UIFactory.CreateButton(previewFrame.transform, "<", () => context.Preview.Rotate(-45f), 20);
            rotateLeft.GetComponent<RectTransform>().SetRect(120f, 548f, 90f, 40f);
            var rotateRight = UIFactory.CreateButton(previewFrame.transform, ">", () => context.Preview.Rotate(45f), 20);
            rotateRight.GetComponent<RectTransform>().SetRect(230f, 548f, 90f, 40f);

            var panel = UIFactory.CreateFramedPanel(Frame, "FormPanel", UITheme.WindowBg);
            panel.rectTransform.SetRect(860f, 240f, PanelWidth, 600f);
            var content = UIFactory.CreateRect("Content", panel.transform);
            content.Stretch(28f, 20f, 28f, 20f);
            float inner = PanelWidth - 56f;

            CreateHeading(content, "Create Character", 0f, inner);

            Label(content, "Name", 62f);
            _name = UIFactory.CreateInputField(content, $"{AccountRules.MinCharacterNameLength}-{AccountRules.MaxCharacterNameLength} letters, numbers, spaces",
                characterLimit: AccountRules.MaxCharacterNameLength);
            _name.GetComponent<RectTransform>().SetRect(140f, 58f, inner - 140f, 38f);

            Label(content, "Kin", 118f);
            _human = UIFactory.CreateButton(content, "Human", () => SetRace(CharacterRace.Human), 16);
            _human.GetComponent<RectTransform>().SetRect(140f, 114f, 160f, 38f);
            _doram = UIFactory.CreateButton(content, "Freyja's Kin", () => SetRace(CharacterRace.Doram), 16);
            _doram.GetComponent<RectTransform>().SetRect(312f, 114f, 160f, 38f);

            Label(content, "Gender", 174f);
            _male = UIFactory.CreateButton(content, "Male", () => SetGender(Gender.Male), 16);
            _male.GetComponent<RectTransform>().SetRect(140f, 170f, 160f, 38f);
            _female = UIFactory.CreateButton(content, "Female", () => SetGender(Gender.Female), 16);
            _female.GetComponent<RectTransform>().SetRect(312f, 170f, 160f, 38f);

            Label(content, "Hair Style", 230f);
            _hairStyleLabel = Stepper(content, 226f, () => StepHairStyle(-1), () => StepHairStyle(1));

            Label(content, "Hair Color", 286f);
            _hairColorLabel = Stepper(content, 282f, () => StepHairColor(-1), () => StepHairColor(1));
            _hairSwatch = UIFactory.CreatePanel(content, "Swatch", Color.white, rounded: true, blocksRaycasts: false);
            _hairSwatch.rectTransform.SetRect(inner - 30f, 288f, 26f, 26f);
            UIFactory.AddOutline(_hairSwatch, Color.black, 1f);

            _info = UIFactory.CreateText(content, string.Empty, 14, UITheme.TextDim, TextAnchor.UpperLeft);
            _info.rectTransform.SetRect(0f, 336f, inner, 100f);
            _info.horizontalOverflow = HorizontalWrapMode.Wrap;

            _status = CreateStatus(content, 0f, 440f, inner);

            _createButton = UIFactory.CreateButton(content, "Create", OnCreate, 20);
            _createButton.GetComponent<RectTransform>().SetRect(0f, 482f, inner / 2f - 8f, 52f);
            var cancel = UIFactory.CreateButton(content, "Cancel", () => Context.ShowCharacterSelect(Slot), 18);
            cancel.GetComponent<RectTransform>().SetRect(inner / 2f + 8f, 482f, inner / 2f - 8f, 52f);
        }

        public int Slot { get; set; }

        public override void HandleKeys()
        {
            if (Busy)
            {
                return;
            }

            if (GameInput.KeyDown(GameKey.Escape))
            {
                Context.ShowCharacterSelect(Slot);
            }
            else if (GameInput.KeyDown(GameKey.Enter))
            {
                OnCreate();
            }
        }

        protected override void OnShow()
        {
            _name.text = string.Empty;
            _hairStyle = UnityEngine.Random.Range(0, CharacterFactory.HairStyleCount);
            _hairColor = UnityEngine.Random.Range(0, CharacterFactory.HairColorCount);
            SetStatus(_status, $"Creating in slot {Slot + 1}.");
            Context.Preview.ResetRotation();
            SetRace(_race);
            SetGender(_gender);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_name.gameObject);
            }

            _name.ActivateInputField();
        }

        private static void Label(Transform parent, string text, float y)
        {
            var label = UIFactory.CreateText(parent, text, 16, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.rectTransform.SetRect(0f, y, 130f, 30f);
        }

        private static Text Stepper(Transform parent, float y, UnityEngine.Events.UnityAction previous, UnityEngine.Events.UnityAction next)
        {
            var left = UIFactory.CreateButton(parent, "<", previous, 18);
            left.GetComponent<RectTransform>().SetRect(140f, y, 44f, 38f);
            var label = UIFactory.CreateText(parent, string.Empty, 16, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.SetRect(190f, y, 230f, 38f);
            var right = UIFactory.CreateButton(parent, ">", next, 18);
            right.GetComponent<RectTransform>().SetRect(426f, y, 44f, 38f);
            return label;
        }

        private void SetGender(Gender gender)
        {
            _gender = gender;
            Highlight(_male, gender == Gender.Male);
            Highlight(_female, gender == Gender.Female);
            RefreshPreview();
        }

        private void SetRace(CharacterRace race)
        {
            _race = race;
            Highlight(_human, race == CharacterRace.Human);
            Highlight(_doram, race == CharacterRace.Doram);
            string where = MapCatalog.Get(MapCatalog.StartingMapId).Name;
            string points = $"All stats start at 1 with <b>{StatFormulas.StartingStatPoints}</b> status points: spend them in-game (Alt+A).";
            _info.text = race == CharacterRace.Doram
                ? $"You awaken as <b>{JobDatabase.Get(JobId.FreyjasKin).Name}</b> in <b>{where}</b>: cat-folk of Freyja's chariot, " +
                  "summoners who call on the spirits of land, sea and life. Your kin never changes job and grows to Base Lv 255.\n" + points
                : $"You awaken as an <b>{JobDatabase.Get(JobId.Initiate).Name}</b> in <b>{where}</b>.\n{points}\n" +
                  "At Job Lv 10 choose Warrior, Scout, Mystic, Devotee, Huntsman or Trader, or an expanded path: Glíma Fighter, Thunderer, Nightraider or Wanderer.";
            RefreshPreview();
        }

        private static void Highlight(Button button, bool selected)
        {
            var colors = button.colors;
            colors.normalColor = selected ? new Color(0.62f, 0.47f, 0.18f, 1f) : UITheme.Button;
            colors.highlightedColor = selected ? new Color(0.74f, 0.58f, 0.25f, 1f) : UITheme.ButtonHover;
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
        }

        private void StepHairStyle(int delta)
        {
            _hairStyle = CharacterFactory.Wrap(_hairStyle + delta, CharacterFactory.HairStyleCount);
            RefreshPreview();
        }

        private void StepHairColor(int delta)
        {
            _hairColor = CharacterFactory.Wrap(_hairColor + delta, CharacterFactory.HairColorCount);
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            _hairStyleLabel.text = $"{AvatarLook.HairStyleNames[_hairStyle]}  ({_hairStyle + 1}/{CharacterFactory.HairStyleCount})";
            _hairColorLabel.text = $"{AvatarLook.HairColorNames[_hairColor]}  ({_hairColor + 1}/{CharacterFactory.HairColorCount})";
            _hairSwatch.color = AvatarLook.HairPalette[_hairColor];
            Context.Preview.ShowLook(AvatarLook.FromRecord(new CharacterRecord
            {
                Gender = _gender,
                HairStyle = _hairStyle,
                HairColor = _hairColor,
                Race = _race,
                Job = _race == CharacterRace.Doram ? JobId.FreyjasKin : JobId.Initiate,
            }));
        }

        private async void OnCreate()
        {
            if (Busy)
            {
                return;
            }

            if (!AccountRules.ValidateCharacterName(_name.text, out string error))
            {
                SetStatus(_status, error, error: true);
                return;
            }

            int visit = Visit;
            Busy = true;
            SetStatus(_status, "Weaving your fate with the Norns...");
            try
            {
                var session = Context.Session;
                var request = new CharacterCreateRequest { Name = _name.text, Gender = _gender, HairStyle = _hairStyle, HairColor = _hairColor, Race = _race };
                var result = await session.Accounts.CreateCharacterAsync(session.Username, Slot, request);
                if (!IsCurrent(visit))
                {
                    return;
                }

                if (!result.Success)
                {
                    SetStatus(_status, result.Error, error: true);
                    return;
                }

                Context.ShowCharacterSelect(result.Value.Slot);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (IsCurrent(visit))
                {
                    SetStatus(_status, "Could not create the character.", error: true);
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
    }
}
