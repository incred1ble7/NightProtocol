using UnityEngine;

namespace NightProtocol.Mechanics
{
    [DisallowMultipleComponent]
    public sealed class HingeFollower : MonoBehaviour
    {
        public MechanicalInteractable source;
        public Vector3 openEuler = new Vector3(0f, 100f, 0f);

        Quaternion closedRotation;

        void Awake() { closedRotation = transform.localRotation; }

        void LateUpdate()
        {
            if (!source) return;
            float t = Mathf.SmoothStep(0f, 1f, source.Progress);
            transform.localRotation = closedRotation * Quaternion.Euler(openEuler * t);
        }
    }
}
