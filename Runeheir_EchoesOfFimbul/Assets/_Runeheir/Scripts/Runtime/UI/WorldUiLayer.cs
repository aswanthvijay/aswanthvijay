using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Player;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// Screen-space overlay for things anchored in the world: Ragnarok-style floating damage numbers,
    /// HP/SP bars under the player, monster names/HP on hover or when hurt, overhead announcements.
    /// Nothing here blocks mouse clicks.
    /// </summary>
    public sealed class WorldUiLayer : MonoBehaviour
    {
        private const float HpBarShowSeconds = 5f;

        private readonly Dictionary<CombatEntity, Plate> _plates = new Dictionary<CombatEntity, Plate>();
        private readonly Dictionary<NpcActor, Text> _npcLabels = new Dictionary<NpcActor, Text>();
        private readonly Dictionary<WorldLabel, Text> _worldLabels = new Dictionary<WorldLabel, Text>();
        private readonly List<WorldLabel> _staleLabels = new List<WorldLabel>();
        private readonly List<CombatEntity> _stale = new List<CombatEntity>();
        private readonly List<NpcActor> _staleNpcs = new List<NpcActor>();
        private readonly List<Floater> _floaters = new List<Floater>();
        private readonly Stack<Floater> _pool = new Stack<Floater>();

        private Canvas _canvas;
        private RectTransform _plateRoot;
        private RectTransform _floaterRoot;
        private Camera _camera;
        private ClickToMoveController _localClicker;

        public static WorldUiLayer Instance { get; private set; }

        public static WorldUiLayer Create()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var canvas = UIFactory.CreateCanvas("WorldUI", 5);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            var layer = canvas.gameObject.AddComponent<WorldUiLayer>();
            layer._canvas = canvas;
            layer._plateRoot = UIFactory.CreateRect("Plates", canvas.transform);
            layer._plateRoot.Stretch();
            layer._floaterRoot = UIFactory.CreateRect("Floaters", canvas.transform);
            layer._floaterRoot.Stretch();
            return layer;
        }

        /// <summary>Free-floating text above an entity.</summary>
        public void ShowText(CombatEntity entity, string text, Color color, int size = 24, float duration = 1.2f, bool pop = false)
        {
            var floater = _pool.Count > 0 ? _pool.Pop() : CreateFloater();
            floater.Text.text = text;
            floater.Text.color = color;
            floater.Text.fontSize = size;
            floater.World = entity.Position + Vector3.up * (entity.Height + 0.2f);
            floater.Jitter = new Vector2(Random.Range(-18f, 18f), Random.Range(-6f, 6f));
            floater.Started = Time.time;
            floater.Duration = duration;
            floater.Pop = pop;
            floater.BaseColor = color;
            floater.Rect.gameObject.SetActive(true);
            _floaters.Add(floater);
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            CombatEntity.AnyDamaged += OnAnyDamaged;
            CombatEntity.AnyHealed += OnAnyHealed;
            WorldFeedback.Announced += OnAnnounced;
        }

        private void OnDisable()
        {
            CombatEntity.AnyDamaged -= OnAnyDamaged;
            CombatEntity.AnyHealed -= OnAnyHealed;
            WorldFeedback.Announced -= OnAnnounced;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnAnyDamaged(CombatEntity target, DamageResult result, CombatEntity attacker)
        {
            bool localIsVictim = target == PlayerCharacter.Local;
            if (_plates.TryGetValue(target, out var plate))
            {
                plate.ShowHpUntil = Time.time + HpBarShowSeconds;
            }

            if (result.IsDamageOverTime)
            {
                ShowText(target, result.Amount.ToString("N0"), new Color(0.75f, 0.45f, 0.95f), 20, 0.9f);
            }
            else if (result.IsMiss)
            {
                ShowText(target, "Miss", new Color(0.6f, 0.85f, 1f), 22, 0.9f);
            }
            else if (result.IsBlocked && result.Absorbed > 0)
            {
                ShowText(target, "Absorbed", new Color(0.6f, 0.85f, 1f), 22, 0.9f);
            }
            else if (result.IsBlocked)
            {
                ShowText(target, "Blocked", new Color(0.55f, 0.85f, 1f), 22, 0.9f);
            }
            else if (result.IsCritical)
            {
                ShowText(target, result.Amount.ToString("N0") + "!", new Color(1f, 0.85f, 0.2f), 36, 1.3f, pop: true);
            }
            else
            {
                Color color = localIsVictim ? new Color(1f, 0.35f, 0.32f) : Color.white;
                ShowText(target, result.Amount.ToString("N0"), color, localIsVictim ? 26 : 28);
            }
        }

        private void OnAnyHealed(CombatEntity target, int amount, bool isSp)
        {
            if (amount > 0)
            {
                ShowText(target, "+" + amount.ToString("N0"), isSp ? new Color(0.45f, 0.65f, 1f) : new Color(0.45f, 1f, 0.5f), 24);
            }
        }

        private void OnAnnounced(CombatEntity entity, string text, Color color)
        {
            ShowText(entity, text, color, 22, 1.6f);
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            if (_localClicker == null && PlayerCharacter.Local != null)
            {
                _localClicker = PlayerCharacter.Local.GetComponent<ClickToMoveController>();
            }

            SyncPlates();
            UpdatePlates();
            UpdateNpcLabels();
            UpdateWorldLabels();
            UpdateFloaters();
        }

        // ------------------------------------------------------------ plates
        private void SyncPlates()
        {
            foreach (var entity in CombatEntity.All)
            {
                if (!_plates.ContainsKey(entity))
                {
                    _plates[entity] = CreatePlate(entity);
                }
            }

            _stale.Clear();
            foreach (var pair in _plates)
            {
                if (pair.Key == null || !pair.Key.isActiveAndEnabled)
                {
                    _stale.Add(pair.Key);
                }
            }

            foreach (var entity in _stale)
            {
                if (_plates.TryGetValue(entity, out var plate))
                {
                    Destroy(plate.Root.gameObject);
                    _plates.Remove(entity);
                }
            }
        }

        private Plate CreatePlate(CombatEntity entity)
        {
            bool isPlayer = entity is PlayerCharacter;
            var root = UIFactory.CreateRect("Plate_" + entity.DisplayName, _plateRoot);
            root.anchorMin = root.anchorMax = Vector2.zero;
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(90f, 40f);

            var plate = new Plate { Entity = entity, Root = root, IsPlayer = isPlayer };
            plate.Name = UIFactory.CreateText(root, string.Empty, 15, UITheme.Text, TextAnchor.UpperCenter, FontStyle.Bold);
            plate.Name.rectTransform.SetRect(-30f, isPlayer ? 18f : 0f, 150f, 20f);
            plate.Name.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.AddOutline(plate.Name, new Color(0f, 0f, 0f, 0.9f), 1f);

            var monster = entity as Monster;
            plate.IsBoss = monster != null && monster.Definition != null && monster.Definition.IsBoss;
            plate.Hp = UIFactory.CreateBar(root, UITheme.Hp, 1);
            plate.Hp.Root.SetRect(plate.IsBoss ? -5f : 15f, isPlayer ? 0f : 20f, plate.IsBoss ? 100f : 60f, plate.IsBoss ? 9f : 7f);
            if (isPlayer)
            {
                plate.Sp = UIFactory.CreateBar(root, UITheme.Sp, 1);
                plate.Sp.Root.SetRect(15f, 8f, 60f, 7f);
            }

            if (monster != null)
            {
                // Monster skill casts: the bar fills toward the moment it goes off (Ragnarok's red cast bar).
                plate.Cast = UIFactory.CreateBar(root, new Color(1f, 0.45f, 0.3f, 1f), 11);
                plate.Cast.Root.SetRect(-10f, 31f, 110f, 13f);
                plate.Cast.Root.gameObject.SetActive(false);
            }

            return plate;
        }

        private void UpdatePlates()
        {
            var hovered = _localClicker != null ? _localClicker.HoveredEntity : null;
            foreach (var plate in _plates.Values)
            {
                var entity = plate.Entity;
                // Players: bars under the feet. Monsters: name + bar over the head.
                Vector3 anchor = plate.IsPlayer ? entity.Position - Vector3.up * 0.05f : entity.Position + Vector3.up * (entity.Height + 0.55f);
                Vector3 screen = _camera.WorldToScreenPoint(anchor);
                bool hoveredNow = entity == hovered;
                var monster = entity as Monster;
                bool casting = monster != null && monster.IsCasting;
                bool showHp = plate.IsPlayer || hoveredNow || plate.IsBoss || casting || Time.time < plate.ShowHpUntil;
                bool visible = screen.z > 0f && !entity.IsDead && (showHp || hoveredNow);
                plate.Root.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                plate.Root.position = new Vector3(screen.x, screen.y - (plate.IsPlayer ? 6f : 0f), 0f);

                float hpFraction = entity.MaxHp > 0 ? entity.Hp / (float)entity.MaxHp : 0f;
                plate.Hp.Root.gameObject.SetActive(showHp);
                plate.Hp.Set(hpFraction);
                plate.Hp.SetColor(hpFraction < 0.25f ? UITheme.HpLow : UITheme.Hp);
                if (plate.Sp != null)
                {
                    plate.Sp.Set(entity.MaxSp > 0 ? entity.Sp / (float)entity.MaxSp : 0f);
                }

                plate.Name.gameObject.SetActive(hoveredNow || plate.IsPlayer || plate.IsBoss || casting);
                if (plate.Name.gameObject.activeSelf)
                {
                    plate.Name.text = plate.IsPlayer ? string.Empty : $"{entity.DisplayName} <size=12>Lv {entity.Level}</size>";
                    plate.Name.color = plate.IsBoss
                        ? monster.Definition.IsMvp ? new Color(1f, 0.82f, 0.3f) : new Color(0.85f, 0.88f, 1f)
                        : monster != null && monster.Definition != null && monster.Definition.Aggressive
                            ? new Color(1f, 0.6f, 0.55f)
                            : UITheme.Text;
                }

                if (plate.Cast != null)
                {
                    plate.Cast.Root.gameObject.SetActive(casting && monster.CastingSkill.CastTime > 0f);
                    if (casting)
                    {
                        plate.Cast.Set(monster.CastProgress, monster.CastingSkill.Name);
                    }
                }
            }
        }

        // ------------------------------------------------------------ NPC names (always shown, Ragnarok style)
        private void UpdateNpcLabels()
        {
            foreach (var npc in NpcActor.All)
            {
                if (!_npcLabels.ContainsKey(npc))
                {
                    var label = UIFactory.CreateText(_plateRoot, $"{npc.DisplayName}\n<size=12><color=#EBC466>[{npc.Title}]</color></size>", 15,
                        new Color(0.75f, 0.9f, 1f), TextAnchor.LowerCenter, FontStyle.Bold);
                    label.horizontalOverflow = HorizontalWrapMode.Overflow;
                    label.verticalOverflow = VerticalWrapMode.Overflow;
                    label.rectTransform.anchorMin = label.rectTransform.anchorMax = Vector2.zero;
                    label.rectTransform.pivot = new Vector2(0.5f, 0f);
                    label.rectTransform.sizeDelta = new Vector2(220f, 40f);
                    UIFactory.AddOutline(label, new Color(0f, 0f, 0f, 0.9f), 1f);
                    _npcLabels[npc] = label;
                }
            }

            _staleNpcs.Clear();
            foreach (var pair in _npcLabels)
            {
                if (pair.Key == null || !pair.Key.isActiveAndEnabled)
                {
                    _staleNpcs.Add(pair.Key);
                    Destroy(pair.Value.gameObject);
                    continue;
                }

                Vector3 screen = _camera.WorldToScreenPoint(pair.Key.Position + Vector3.up * (pair.Key.Height + 0.35f));
                pair.Value.gameObject.SetActive(screen.z > 0f);
                pair.Value.rectTransform.position = new Vector3(screen.x, screen.y, 0f);
            }

            foreach (var npc in _staleNpcs)
            {
                _npcLabels.Remove(npc);
            }
        }

        // ------------------------------------------------------------ portal and tombstone labels
        private void UpdateWorldLabels()
        {
            foreach (var label in WorldLabel.All)
            {
                if (!_worldLabels.ContainsKey(label))
                {
                    var text = UIFactory.CreateText(_plateRoot, label.Text, label.FontSize, label.Color, TextAnchor.LowerCenter, FontStyle.Bold);
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                    text.rectTransform.anchorMin = text.rectTransform.anchorMax = Vector2.zero;
                    text.rectTransform.pivot = new Vector2(0.5f, 0f);
                    text.rectTransform.sizeDelta = new Vector2(260f, 40f);
                    UIFactory.AddOutline(text, new Color(0f, 0f, 0f, 0.9f), 1f);
                    _worldLabels[label] = text;
                }
            }

            _staleLabels.Clear();
            foreach (var pair in _worldLabels)
            {
                if (pair.Key == null || !pair.Key.isActiveAndEnabled)
                {
                    _staleLabels.Add(pair.Key);
                    Destroy(pair.Value.gameObject);
                    continue;
                }

                if (pair.Value.text != pair.Key.Text)
                {
                    pair.Value.text = pair.Key.Text;
                }

                pair.Value.color = pair.Key.Color;
                Vector3 screen = _camera.WorldToScreenPoint(pair.Key.transform.position + Vector3.up * pair.Key.Height);
                pair.Value.gameObject.SetActive(screen.z > 0f);
                pair.Value.rectTransform.position = new Vector3(screen.x, screen.y, 0f);
            }

            foreach (var label in _staleLabels)
            {
                _worldLabels.Remove(label);
            }
        }

        // ------------------------------------------------------------ floaters
        private Floater CreateFloater()
        {
            var text = UIFactory.CreateText(_floaterRoot, string.Empty, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = Vector2.zero;
            text.rectTransform.sizeDelta = new Vector2(200f, 40f);
            UIFactory.AddOutline(text, new Color(0f, 0f, 0f, 0.95f), 1.5f);
            return new Floater { Rect = text.rectTransform, Text = text };
        }

        private void UpdateFloaters()
        {
            float scale = _canvas.scaleFactor;
            for (int i = _floaters.Count - 1; i >= 0; i--)
            {
                var floater = _floaters[i];
                float t = (Time.time - floater.Started) / floater.Duration;
                if (t >= 1f)
                {
                    floater.Rect.gameObject.SetActive(false);
                    _floaters.RemoveAt(i);
                    _pool.Push(floater);
                    continue;
                }

                Vector3 screen = _camera.WorldToScreenPoint(floater.World);
                if (screen.z <= 0f)
                {
                    floater.Rect.gameObject.SetActive(false);
                    continue;
                }

                floater.Rect.gameObject.SetActive(true);
                float rise = (1f - (1f - t) * (1f - t)) * 70f;
                floater.Rect.position = new Vector3(screen.x + floater.Jitter.x * scale, screen.y + (rise + floater.Jitter.y) * scale, 0f);
                float popScale = floater.Pop ? 1f + Mathf.Max(0f, 0.6f - t * 3f) : 1f;
                floater.Rect.localScale = Vector3.one * popScale;
                var color = floater.BaseColor;
                color.a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                floater.Text.color = color;
            }
        }

        private sealed class Plate
        {
            public CombatEntity Entity;
            public RectTransform Root;
            public Text Name;
            public UIBar Hp;
            public UIBar Sp;
            public UIBar Cast;
            public bool IsPlayer;
            public bool IsBoss;
            public float ShowHpUntil;
        }

        private sealed class Floater
        {
            public RectTransform Rect;
            public Text Text;
            public Vector3 World;
            public Vector2 Jitter;
            public float Started;
            public float Duration;
            public bool Pop;
            public Color BaseColor;
        }
    }
}
