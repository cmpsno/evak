using UnityEngine;
using CODClone.Core;
using CODClone.UI;

namespace CODClone.Interaction
{
    /// <summary>
    /// Attach to the player. Each frame finds the nearest IInteractable within
    /// range in front of the camera, shows its prompt, and routes E presses.
    /// While a dialogue box is open, E belongs to the dialogue — no re-trigger.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputProviderBehaviour;
        [SerializeField] private float interactRange = 2.2f;
        [SerializeField] private float forwardDotMin = 0.4f;

        private IInputProvider _input;
        private IInteractable _current;

        private void Awake()
        {
            _input = inputProviderBehaviour as IInputProvider
                ?? InputProviderLookup.FindOnSelf(this);
        }

        private void Update()
        {
            if (DialogueUI.Instance != null && DialogueUI.Instance.IsDialogueOpen)
            {
                _current = null;
                return; // E advances dialogue; don't start new interactions
            }

            _current = FindNearest();
            if (_current != null && DialogueUI.Instance != null)
                DialogueUI.Instance.ShowPrompt("E — " + _current.PromptText);
            else if (DialogueUI.Instance != null)
                DialogueUI.Instance.HidePrompt();

            if (_current != null && _input != null && _input.InteractPressed)
            {
                if (DialogueUI.Instance != null) DialogueUI.Instance.HidePrompt();
                _current.Interact(gameObject);
                _current = null;
            }
        }

        private IInteractable FindNearest()
        {
            var cam = Camera.main;
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 forward = cam != null ? cam.transform.forward : transform.forward;

            IInteractable best = null;
            float bestDist = interactRange;
            foreach (var hit in Physics.OverlapSphere(origin, interactRange))
            {
                var interactable = InteractableLookup.FindOn(hit.gameObject);
                if (interactable == null) continue;
                Vector3 to = hit.transform.position - origin;
                float dist = to.magnitude;
                if (dist > bestDist) continue;
                if (Vector3.Dot(to.normalized, forward) < forwardDotMin) continue;
                best = interactable;
                bestDist = dist;
            }
            return best;
        }
    }
}
