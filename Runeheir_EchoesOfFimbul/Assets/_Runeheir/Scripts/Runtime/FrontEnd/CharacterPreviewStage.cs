using Runeheir.Visuals;
using UnityEngine;

namespace Runeheir.FrontEnd
{
    /// <summary>
    /// Off-screen turntable that renders the selected/new character into a RenderTexture shown on
    /// character select and creation (Ragnarok shows the character standing in its slot).
    /// </summary>
    public sealed class CharacterPreviewStage : MonoBehaviour
    {
        private static readonly Vector3 StageOrigin = new Vector3(0f, -500f, 0f);

        private RenderTexture _texture;
        private Transform _turntable;
        private float _yaw;

        public RenderTexture Texture => _texture;

        public static CharacterPreviewStage Create()
        {
            var root = new GameObject("CharacterPreviewStage");
            root.transform.position = StageOrigin;
            var stage = root.AddComponent<CharacterPreviewStage>();
            stage.Build();
            return stage;
        }

        public void ShowLook(AvatarLook look)
        {
            Clear();
            PlaceholderAvatar.CreateHumanoid(_turntable, look);
        }

        public void ShowEmpty()
        {
            Clear();
        }

        public void Rotate(float degrees)
        {
            _yaw += degrees;
        }

        public void ResetRotation()
        {
            _yaw = 0f;
        }

        private void Build()
        {
            _texture = new RenderTexture(512, 640, 24) { name = "RH_CharacterPreview", antiAliasing = 4 };
            _texture.Create();

            var cameraGo = new GameObject("PreviewCamera");
            cameraGo.transform.SetParent(transform, false);
            cameraGo.transform.localPosition = new Vector3(0f, 1.25f, 5.4f);
            cameraGo.transform.LookAt(transform.position + new Vector3(0f, 0.9f, 0f));
            var previewCamera = cameraGo.AddComponent<Camera>();
            previewCamera.targetTexture = _texture;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.05f, 0.08f, 0.13f, 1f);
            previewCamera.fieldOfView = 26f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 30f;

            var keyLight = new GameObject("KeyLight").AddComponent<Light>();
            keyLight.transform.SetParent(transform, false);
            keyLight.transform.localPosition = new Vector3(1.6f, 2.6f, 2.4f);
            keyLight.type = LightType.Point;
            keyLight.range = 9f;
            keyLight.intensity = 2.2f;
            keyLight.color = new Color(1f, 0.92f, 0.8f);

            var rimLight = new GameObject("RimLight").AddComponent<Light>();
            rimLight.transform.SetParent(transform, false);
            rimLight.transform.localPosition = new Vector3(-1.8f, 2.2f, -1.6f);
            rimLight.type = LightType.Point;
            rimLight.range = 8f;
            rimLight.intensity = 1.6f;
            rimLight.color = new Color(0.55f, 0.75f, 1f);

            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            Destroy(pedestal.GetComponent<Collider>());
            pedestal.transform.SetParent(transform, false);
            pedestal.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            pedestal.transform.localScale = new Vector3(1.7f, 0.05f, 1.7f);
            pedestal.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.Lit(new Color(0.22f, 0.25f, 0.3f));

            var ring = GroundRing.Create("RuneCircle", new Color(0.93f, 0.77f, 0.4f, 0.9f), 0.8f, 0.04f);
            ring.transform.SetParent(transform, true);
            ring.ShowAt(transform.position);
            ring.SetSpin(20f);

            _turntable = new GameObject("Turntable").transform;
            _turntable.SetParent(transform, false);
        }

        private void Clear()
        {
            for (int i = _turntable.childCount - 1; i >= 0; i--)
            {
                Destroy(_turntable.GetChild(i).gameObject);
            }
        }

        private void Update()
        {
            float sway = Mathf.Sin(Time.time * 0.7f) * 10f;
            _turntable.localRotation = Quaternion.Euler(0f, _yaw + sway, 0f);
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }
    }
}
