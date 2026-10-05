using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>Constant rotation (windmill sails, runestone glyphs).</summary>
    public sealed class Spinner : MonoBehaviour
    {
        [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f, 0f, 30f);

        public Vector3 DegreesPerSecond
        {
            get => degreesPerSecond;
            set => degreesPerSecond = value;
        }

        private void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
