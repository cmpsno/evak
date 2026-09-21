using UnityEngine;

namespace Campusano.Missions
{
    /// <summary>
    /// TICKET-EP1-01 v2. Invisible trigger box. One-shot by default.
    /// Routes through MissionStateMachine.AdvanceOnVolumeEntered(volumeId).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class TriggerVolume : MonoBehaviour
    {
        [Tooltip("Volume id mapped in MissionStateMachine StepTriggers.")]
        [SerializeField] private string volumeId = "";
        [SerializeField] private bool oneShot = true;

        private bool _fired;

        public void Configure(string id, bool oneShot = true)
        {
            volumeId = id;
            this.oneShot = oneShot;
        }

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
            machine.AdvanceOnVolumeEntered(volumeId);
        }
    }
}
