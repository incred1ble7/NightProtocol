using UnityEngine;

namespace NightProtocol.Mechanics
{
    /// <summary>
    /// Rotates this transform around its local pivot following a MechanicalInteractable's Progress (0 = closed, 1 = open).
    /// Lets any static mesh become a working door without an Animator.
    /// </summary>
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
