using System.Collections;
using UnityEngine;

namespace NightProtocol.Mechanics
{
    [DisallowMultipleComponent]
    public sealed class MasterSwitch : MonoBehaviour
    {
        public MechanicalInteractable master;
        public MechanicalInteractable[] targets;
        [Min(0f)] public float cascadeDelay = 0.08f;

        Coroutine routine;

        void OnEnable()
        {
            if (master) master.onStateChanged.AddListener(OnMasterChanged);
        }

        void OnDisable()
        {
            if (master) master.onStateChanged.RemoveListener(OnMasterChanged);
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        void OnMasterChanged(bool isOn)
        {
            if (isOn) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(SwitchAllOff());
        }

        IEnumerator SwitchAllOff()
        {
            if (targets != null)
            {
                foreach (var target in targets)
                {
                    if (!target || !target.IsOn) continue;
                    target.SetState(false);
                    if (cascadeDelay > 0f) yield return new WaitForSeconds(cascadeDelay);
                }
            }
            routine = null;
        }
    }
}
