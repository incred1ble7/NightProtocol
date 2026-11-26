using UnityEngine;

namespace NightProtocol.Mechanics
{
    [DisallowMultipleComponent]
    public sealed class SwitchIndicator : MonoBehaviour
    {
        public MechanicalInteractable source;
        public Renderer[] lamps;
        public int offMaterialIndex = 1;
        public int onMaterialIndex = 2;
        [ColorUsage(false, true)] public Color offColor = new Color(2f, 0f, 0f);
        [ColorUsage(false, true)] public Color onColor = new Color(0.15f, 2.5f, 0f);
        [Min(0f)] public float blinkFrequency = 1.1f;
        [Range(0f, 1f)] public float blinkDepth = 0.85f;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock block;
        float phase;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            phase = Random.value;
            Apply(0f);
        }

        void LateUpdate()
        {
            Apply(Time.time);
        }

        public void Apply(float time)
        {
            if (!source || lamps == null) return;
            float t = Mathf.SmoothStep(0f, 1f, source.Progress);
            float wave = 0.5f - 0.5f * Mathf.Cos((time * blinkFrequency + phase) * Mathf.PI * 2f);
            float pulse = 1f - blinkDepth * wave;
            Color off = offColor * ((1f - t) * pulse);
            Color on = onColor * t;
            foreach (var lamp in lamps)
            {
                if (!lamp) continue;
                SetEmission(lamp, offMaterialIndex, off);
                SetEmission(lamp, onMaterialIndex, on);
            }
        }

        void SetEmission(Renderer lamp, int index, Color color)
        {
            if (index < 0 || index >= lamp.sharedMaterials.Length) return;
            if (block == null) block = new MaterialPropertyBlock();
            lamp.GetPropertyBlock(block, index);
            block.SetColor(EmissionId, color);
            lamp.SetPropertyBlock(block, index);
        }
    }
}
