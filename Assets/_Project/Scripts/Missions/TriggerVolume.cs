using UnityEngine;

namespace Campusano.Missions
{
    /// <summary>
    /// Invisible trigger box that advances the mission state machine when the
    /// player walks in. One-shot by default (no double-triggering).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class TriggerVolume : MonoBehaviour
    {
        [Tooltip("Mapped to an Ep1Step in MissionStateMachine.volumeSteps.")]
        [SerializeField] private string volumeId = "";
        [SerializeField] private bool oneShot = true;

        private bool _fired;

        private void Reset()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_fired && oneShot) return;
            if (!other.CompareTag("Player")) return;
            var machine = MissionStateMachine.Instance;
            if (machine == null) return;
            _fired = true;
            machine.TriggerStepFromVolume(volumeId);
        }
    }
}
