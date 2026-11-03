using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    [DefaultExecutionOrder(50)]
    public sealed class TachometerUI : MonoBehaviour
    {
        public GlobeController globe;
        public Transform needle;
        public Renderer gaugeFace;
        public Text speedText;
        MaterialPropertyBlock properties;
        Color restingEmission;
        public float Ratio => globe ? Mathf.Clamp01((float)(globe.CurrentVelocity / globe.MaximumVelocity)) : 0;
        void Awake()
        {
            properties = new MaterialPropertyBlock();
            if (gaugeFace && gaugeFace.sharedMaterial && gaugeFace.sharedMaterial.HasProperty("_EmissionColor")) restingEmission = gaugeFace.sharedMaterial.GetColor("_EmissionColor");
        }
        void LateUpdate()
        {
            if (!globe) return;
            if (needle) needle.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(135, -135, Ratio));
            if (gaugeFace)
            {
                properties ??= new MaterialPropertyBlock();   // lost when scripts recompile during Play
                gaugeFace.GetPropertyBlock(properties);
                float pulse = globe.IsRedline ? .20f + .12f * Mathf.Sin(Time.unscaledTime * 11) : 0;
                properties.SetColor("_EmissionColor", restingEmission + new Color(pulse, pulse * .035f, 0));
                gaugeFace.SetPropertyBlock(properties);
            }
            if (speedText) speedText.text = globe.IsRedline ? "MAX / REDLINE" :
                (globe.CurrentVelocity / 6).ToString("0.0") + " / " + (globe.MaximumVelocity / 6).ToString("0.0") + " RPM";
        }
        void OnDisable()
        {
            if (gaugeFace && properties != null) { gaugeFace.GetPropertyBlock(properties); properties.SetColor("_EmissionColor", restingEmission); gaugeFace.SetPropertyBlock(properties); }
        }
    }
}
