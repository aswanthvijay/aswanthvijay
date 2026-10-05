using Runeheir.Controls;
using Runeheir.Session;
using Runeheir.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.FrontEnd
{
    /// <summary>
    /// The RH_Login scene: Login → Server Select → Character Select ⇄ Character Create → enter the field.
    /// Same flow as Ragnarok/XileRO clients; backed by <see cref="Accounts.IAccountService"/> (offline JSON for now).
    /// </summary>
    public sealed class FrontEndController : MonoBehaviour
    {
        [SerializeField] private string gameTitle = "RUNEHEIR";
        [SerializeField] private string gameSubtitle = "Echoes of Fimbul";
        [SerializeField] private string footerText = "Phase 2 Prototype · Offline realm (accounts stored on this PC)";

        private LoginScreen _login;
        private ServerSelectScreen _servers;
        private CharacterSelectScreen _select;
        private CharacterCreateScreen _create;
        private FrontEndScreen _active;

        public Canvas Canvas { get; private set; }

        public RectTransform ScreensRoot { get; private set; }

        public CharacterPreviewStage Preview { get; private set; }

        public GameSession Session => GameSession.Instance;

        public void ShowLogin()
        {
            Switch(_login);
        }

        public void ShowServerSelect()
        {
            Switch(_servers);
        }

        public void ShowCharacterSelect(int preferredSlot = -1)
        {
            _select.PreferredSlot = preferredSlot;
            Switch(_select);
        }

        public void ShowCharacterCreate(int slot)
        {
            _create.Slot = slot;
            Switch(_create);
        }

        private void Start()
        {
            EventSystemBootstrap.Ensure();
            Canvas = UIFactory.CreateCanvas("FrontEnd", 0);
            BuildBackdrop();

            ScreensRoot = UIFactory.CreateRect("Screens", Canvas.transform);
            ScreensRoot.Stretch();
            Preview = CharacterPreviewStage.Create();

            _login = new LoginScreen(this);
            _servers = new ServerSelectScreen(this);
            _select = new CharacterSelectScreen(this);
            _create = new CharacterCreateScreen(this);
            UITooltip.Create(Canvas);

            var session = Session;
            if (session.IsLoggedIn && session.OpenCharacterSelectOnLoad)
            {
                session.OpenCharacterSelectOnLoad = false;
                ShowCharacterSelect();
            }
            else
            {
                session.Logout();
                ShowLogin();
            }
        }

        private void Update()
        {
            _active?.HandleKeys();
        }

        private void Switch(FrontEndScreen next)
        {
            if (_active == next)
            {
                return;
            }

            _active?.Hide();
            _active = next;
            UITooltip.Hide();
            next.Show();
        }

        private void BuildBackdrop()
        {
            var background = UIFactory.CreatePanel(Canvas.transform, "Backdrop", Color.white, rounded: false);
            background.sprite = UITheme.VerticalGradient(new Color(0.03f, 0.05f, 0.11f), new Color(0.20f, 0.30f, 0.42f));
            background.rectTransform.Stretch();

            var aurora = UIFactory.CreatePanel(Canvas.transform, "Aurora", new Color(0.35f, 0.9f, 0.75f, 0.07f), rounded: true, blocksRaycasts: false);
            aurora.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1500f, 230f));

            var snow = UIFactory.CreateRect("Snow", Canvas.transform);
            snow.Stretch();
            snow.gameObject.AddComponent<UISnowfall>();

            var title = UIFactory.CreateText(Canvas.transform, gameTitle, 86, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1200f, 110f));
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.AddOutline(title, new Color(0.15f, 0.08f, 0.02f, 0.9f), 2.5f);
            UIFactory.AddShadow(title, 4f);

            var subtitle = UIFactory.CreateText(Canvas.transform, gameSubtitle, 32, UITheme.Frost, TextAnchor.MiddleCenter, FontStyle.Italic);
            subtitle.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(900f, 44f));
            UIFactory.AddShadow(subtitle, 2f);

            var footer = UIFactory.CreateText(Canvas.transform, footerText, 14, UITheme.TextDim, TextAnchor.MiddleCenter);
            footer.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1200f, 24f));
        }
    }

    /// <summary>Base for the login-scene screens. Content lives in a centered 1920x1080 frame.</summary>
    public abstract class FrontEndScreen
    {
        protected FrontEndScreen(FrontEndController context, string name)
        {
            Context = context;
            Root = UIFactory.CreateRect(name, context.ScreensRoot);
            Root.Stretch();
            Frame = UIFactory.CreateRect("Frame", Root);
            Frame.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            Root.gameObject.SetActive(false);
        }

        public RectTransform Root { get; }

        protected RectTransform Frame { get; }

        protected FrontEndController Context { get; }

        protected bool Busy { get; set; }

        public void Show()
        {
            Root.gameObject.SetActive(true);
            OnShow();
        }

        public void Hide()
        {
            Root.gameObject.SetActive(false);
            OnHide();
        }

        public virtual void HandleKeys()
        {
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnHide()
        {
        }

        protected static Text CreateStatus(Transform parent, float x, float y, float width)
        {
            var status = UIFactory.CreateText(parent, string.Empty, 15, UITheme.TextDim, TextAnchor.MiddleCenter);
            status.rectTransform.SetRect(x, y, width, 26f);
            return status;
        }

        protected static void SetStatus(Text status, string message, bool error = false)
        {
            status.text = message ?? string.Empty;
            status.color = error ? UITheme.Error : UITheme.TextDim;
        }

        protected static Text CreateHeading(Transform parent, string text, float y, float width)
        {
            var heading = UIFactory.CreateText(parent, text, 24, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            heading.rectTransform.SetRect(0f, y, width, 34f);
            UIFactory.AddShadow(heading, 1.5f);
            return heading;
        }
    }
}
