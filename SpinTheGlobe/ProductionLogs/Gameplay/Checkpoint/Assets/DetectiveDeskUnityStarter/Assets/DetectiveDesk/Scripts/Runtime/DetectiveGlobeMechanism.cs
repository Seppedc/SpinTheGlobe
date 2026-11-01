using UnityEngine;

namespace DetectiveDesk
{
    /// <summary>Globe-only presentation. DetectiveGame remains the owner of gameplay and sphere rotation.</summary>
    [DisallowMultipleComponent]
    public sealed class DetectiveGlobeMechanism : MonoBehaviour
    {
        public Transform rotatingSphere;
        public Transform[] gears;
        public float[] gearRatios;
        public Transform rpmNeedle;
        public float gaugeMaximumRpm = 5f;
        public float standaloneDegreesPerSecond = 7f;
        public Renderer[] feedbackRenderers;

        DeskInteractable interaction;
        MaterialPropertyBlock properties;
        Color[] restingEmission;
        bool hovered;
        float feedback;
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            interaction = GetComponent<DeskInteractable>();
            properties = new MaterialPropertyBlock();
            restingEmission = new Color[feedbackRenderers.Length];
            for (int i = 0; i < feedbackRenderers.Length; i++)
            {
                var material = feedbackRenderers[i] ? feedbackRenderers[i].sharedMaterial : null;
                restingEmission[i] = material && material.HasProperty(Emission)
                    ? material.GetColor(Emission) : Color.black;
            }
        }

        void OnMouseEnter() { hovered = true; }
        void OnMouseExit() { hovered = false; }

        void Update()
        {
            var game = interaction ? interaction.game : null;
            float degreesPerSecond = game ? game.globeSpeed : standaloneDegreesPerSecond;
            // The scene's existing gameplay script already rotates this transform.
            if (rotatingSphere && (!game || game.globeRotor != rotatingSphere))
                rotatingSphere.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);

            for (int i = 0; i < gears.Length; i++)
                if (gears[i]) gears[i].Rotate(0, 0,
                    degreesPerSecond * (i < gearRatios.Length ? gearRatios[i] : 1f) * Time.deltaTime,
                    Space.Self);

            if (rpmNeedle)
            {
                float rpm = Mathf.Abs(degreesPerSecond) / 6f;
                float angle = Mathf.Lerp(135f, -135f, Mathf.Clamp01(rpm / gaugeMaximumRpm));
                rpmNeedle.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        void LateUpdate()
        {
            feedback = Mathf.MoveTowards(feedback, hovered ? 1f : 0f, Time.deltaTime * 5f);
            ApplyFeedback(feedback);
        }

        void ApplyFeedback(float amount)
        {
            if (properties == null || restingEmission == null) return;
            for (int i = 0; i < feedbackRenderers.Length; i++)
            {
                var renderer = feedbackRenderers[i];
                if (!renderer) continue;
                renderer.GetPropertyBlock(properties);
                // Restore the material's resting emission after DeskInteractable's mouse events.
                properties.SetColor(Emission, restingEmission[i] + new Color(.045f, .028f, .008f) * amount);
                renderer.SetPropertyBlock(properties);
            }
        }

        void OnDisable()
        {
            hovered = false;
            feedback = 0f;
            ApplyFeedback(0f);
        }
    }
}
