using UnityEngine;
using UnityEngine.Events;

namespace NightProtocol.Mechanics
{
    [DisallowMultipleComponent]
    public sealed class MechanicalInteractable : MonoBehaviour
    {
        public string displayName = "Switch";
        public string offAction = "Turn on";
        public string onAction = "Turn off";
        [Min(0.05f)] public float travelSeconds = 0.35f;
        public Animator[] animators;
        public Light[] controlledLights;
        public bool initiallyOn;
        public UnityEvent<bool> onStateChanged = new UnityEvent<bool>();
        public bool IsOn { get; private set; }
        public float Progress { get; private set; }
        public bool IsMoving => Mathf.Abs(Progress - (IsOn ? 1f : 0f)) > 0.0001f;
        public string Prompt => displayName + " — " + (IsOn ? onAction : offAction);
        static readonly int ProgressId = Animator.StringToHash("Progress");
        static readonly int IsOnId = Animator.StringToHash("IsOn");

        void Awake()
        {
            IsOn = initiallyOn;
            Progress = initiallyOn ? 1f : 0f;
            ApplyPose();
            ApplyLights();
        }
        void Update() { Advance(Time.deltaTime); }
        public void Toggle() { SetState(!IsOn); }
        public void SetState(bool on)
        {
            if (IsOn == on) return;
            IsOn = on;
            ApplyLights();
            onStateChanged.Invoke(on);
        }
        // Keeping progress separate from the target allows smooth reversal mid-motion.
        public void Advance(float deltaTime)
        {
            Progress = Mathf.MoveTowards(Progress, IsOn ? 1f : 0f, Mathf.Max(0f, deltaTime) / Mathf.Max(0.05f, travelSeconds));
            ApplyPose();
        }
        void ApplyPose()
        {
            if (animators == null) return;
            foreach (var a in animators)
            {
                if (!a || !a.runtimeAnimatorController) continue;
                a.SetBool(IsOnId, IsOn);
                a.SetFloat(ProgressId, Progress);
                a.Update(0f);
            }
        }
        void ApplyLights()
        {
            if (controlledLights == null) return;
            foreach (var lightSource in controlledLights) if (lightSource) lightSource.enabled = IsOn;
        }
    }
}
