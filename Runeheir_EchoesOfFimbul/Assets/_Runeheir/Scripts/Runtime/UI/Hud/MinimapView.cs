using System.Collections.Generic;
using Runeheir.Cameras;
using Runeheir.Field;
using Runeheir.Player;
using Runeheir.World;
using Runeheir.WorldBuilding;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// Ragnarok-style minimap (top-right): the whole map drawn once from its generated layout, turned with the camera so
    /// "up" on the map is "up" on screen, with warp portals (rose), NPCs (gold), the save point (blue), living bosses (red)
    /// and you (white, pointing where you face). Map name and coordinates on top. Ctrl+Tab: small, large, hidden.
    /// </summary>
    public sealed class MinimapView
    {
        private static readonly float[] Sizes = { 190f, 340f, 0f };

        private readonly PlayerCharacter _player;
        private readonly MapLayout _layout;
        private readonly RectTransform _frame;
        private readonly RectTransform _content;
        private readonly Text _title;
        private readonly RectTransform _playerMarker;
        private readonly List<(RectTransform marker, BossSpawner boss)> _bosses = new List<(RectTransform, BossSpawner)>();
        private IsometricCameraRig _rig;
        private int _size;
        private float _nextMarkerRefresh;

        public MinimapView(HudController hud, PlayerCharacter player, MapLayout layout)
        {
            _player = player;
            _layout = layout;

            var frameImage = UIFactory.CreateFramedPanel(hud.Canvas.transform, "Minimap", new Color(0.02f, 0.03f, 0.05f, 0.85f));
            frameImage.raycastTarget = false;
            _frame = frameImage.rectTransform;
            _frame.gameObject.AddComponent<RectMask2D>();

            var mapImage = UIFactory.CreateRect("Map", _frame);
            _content = mapImage;
            var raw = mapImage.gameObject.AddComponent<RawImage>();
            raw.texture = BuildTexture(layout);
            raw.raycastTarget = false;

            AddMarkers();
            _playerMarker = Marker(Color.white, 9f, "You");
            var nose = UIFactory.CreatePanel(_playerMarker, "Facing", Color.white, rounded: false, blocksRaycasts: false).rectTransform;
            nose.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(3f, 9f));

            _title = UIFactory.CreateText(hud.Canvas.transform, string.Empty, 13, UITheme.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.AddOutline(_title, new Color(0f, 0f, 0f, 0.9f), 1f);
            ApplySize();
        }

        /// <summary>Ctrl+Tab: small → large → hidden.</summary>
        public void CycleSize()
        {
            _size = (_size + 1) % Sizes.Length;
            ApplySize();
        }

        public void Tick()
        {
            if (_player == null || Sizes[_size] <= 0f)
            {
                return;
            }

            if (_rig == null && Camera.main != null)
            {
                _rig = Camera.main.GetComponent<IsometricCameraRig>();
            }

            float yaw = _rig != null ? _rig.Yaw : 45f;
            _content.localEulerAngles = new Vector3(0f, 0f, yaw);

            Vector3 position = _player.Position;
            Place(_playerMarker, position.x, position.z);
            _playerMarker.localEulerAngles = new Vector3(0f, 0f, -_player.transform.eulerAngles.y);

            if (Time.unscaledTime >= _nextMarkerRefresh)
            {
                _nextMarkerRefresh = Time.unscaledTime + 0.5f;
                foreach (var (marker, boss) in _bosses)
                {
                    bool alive = boss != null && boss.Boss != null && !boss.Boss.IsDead;
                    marker.gameObject.SetActive(alive);
                    if (alive)
                    {
                        Place(marker, boss.Boss.Position.x, boss.Boss.Position.z);
                    }
                }
            }

            float originX = _layout.OriginX;
            float originZ = _layout.OriginZ;
            _title.text = $"{FieldContext.MapName}  <color=#AED6F1>({position.x - originX:0}, {position.z - originZ:0})</color>";
        }

        private void ApplySize()
        {
            float size = Sizes[_size];
            bool visible = size > 0f;
            _frame.gameObject.SetActive(visible);
            _title.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            _frame.Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -34f), new Vector2(size, size));
            float side = size * 0.707f;
            _content.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(side, side));
            _title.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -10f), new Vector2(Mathf.Max(size, 260f), 20f));
        }

        private void AddMarkers()
        {
            foreach (var portal in _layout.Portals)
            {
                Place(Marker(new Color(1f, 0.45f, 0.65f), 7f, portal.Portal.Label), portal.At.X, portal.At.Z);
            }

            foreach (var npc in _layout.Npcs)
            {
                Place(Marker(new Color(1f, 0.85f, 0.35f), 6f, npc.Npc.Name), npc.At.X, npc.At.Z);
            }

            Place(Marker(new Color(0.45f, 0.7f, 1f), 6f, "Save point"), _layout.SavePoint.X, _layout.SavePoint.Z);
            foreach (var boss in Object.FindObjectsByType<BossSpawner>(FindObjectsSortMode.None))
            {
                _bosses.Add((Marker(new Color(1f, 0.25f, 0.2f), 9f, boss.Spawn?.MonsterId), boss));
            }
        }

        private RectTransform Marker(Color color, float size, string name)
        {
            var image = UIFactory.CreatePanel(_content, "Marker_" + name, color, rounded: true, blocksRaycasts: false);
            image.rectTransform.sizeDelta = new Vector2(size, size);
            UIFactory.AddOutline(image, new Color(0f, 0f, 0f, 0.8f), 1f);
            return image.rectTransform;
        }

        /// <summary>Pins a marker to a world X/Z on the map image (anchors in 0..1 across the layout).</summary>
        private void Place(RectTransform marker, float x, float z)
        {
            float width = _layout.Width * _layout.CellSize;
            float height = _layout.Height * _layout.CellSize;
            var at = new Vector2(Mathf.Clamp01((x - _layout.OriginX) / width), Mathf.Clamp01((z - _layout.OriginZ) / height));
            marker.anchorMin = at;
            marker.anchorMax = at;
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = Vector2.zero;
        }

        /// <summary>One texel per meter (at most 256 per side): ground colors by cell, water or rock in the gaps, scenery as dots.</summary>
        private static Texture2D BuildTexture(MapLayout layout)
        {
            var theme = WorldTheme.For(layout.Map.Theme);
            float extent = layout.Width * layout.CellSize;
            int size = Mathf.Clamp(Mathf.RoundToInt(extent), 64, 256);
            float metersPerTexel = extent / size;
            var pixels = new Color32[size * size];
            Color gap = theme.Water ?? (theme.Enclosed ? theme.RockTop * 1.6f : theme.Outer * 0.6f);
            gap.a = 1f;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = layout.OriginX + (px + 0.5f) * metersPerTexel;
                    float z = layout.OriginZ + (py + 0.5f) * metersPerTexel;
                    layout.CellOf(new GroundPoint(x, z), out int cx, out int cz);
                    Color color = layout.IsWalkable(cx, cz) ? Brighten(theme.ColorOf(layout.TagAt(cx, cz), false)) : gap;
                    pixels[py * size + px] = color;
                }
            }

            foreach (var prop in layout.Props)
            {
                if (!PropKinds.Blocks(prop.Kind))
                {
                    continue;
                }

                int px = Mathf.FloorToInt((prop.At.X - layout.OriginX) / metersPerTexel);
                int py = Mathf.FloorToInt((prop.At.Z - layout.OriginZ) / metersPerTexel);
                int radius = Mathf.Max(0, Mathf.RoundToInt(PropKinds.Radius(prop.Kind) * prop.Scale / metersPerTexel) - 1);
                Color dot = PropColor(prop.Kind);
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int x = px + dx;
                        int y = py + dy;
                        if (x >= 0 && y >= 0 && x < size && y < size && dx * dx + dy * dy <= radius * radius + 1)
                        {
                            pixels[y * size + x] = dot;
                        }
                    }
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Minimap_" + layout.Map.Id,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Color Brighten(Color color)
        {
            var c = Color.Lerp(color, Color.white, 0.12f);
            c.a = 1f;
            return c;
        }

        private static Color PropColor(PropKind kind)
        {
            switch (kind)
            {
                case PropKind.Tree:
                case PropKind.BirchTree:
                    return new Color(0.16f, 0.32f, 0.18f);
                case PropKind.House:
                case PropKind.MeadHall:
                case PropKind.GuildHall:
                case PropKind.ForgeHut:
                case PropKind.Stall:
                case PropKind.Tent:
                    return new Color(0.55f, 0.35f, 0.22f);
                case PropKind.Palisade:
                    return new Color(0.4f, 0.3f, 0.2f);
                default:
                    return new Color(0.35f, 0.36f, 0.4f);
            }
        }
    }
}
