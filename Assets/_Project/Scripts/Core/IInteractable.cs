using UnityEngine;

namespace CODClone.Core
{
    /// <summary>
    /// Anything the player can press E on: NPCs, pickups, doors.
    /// Found via manual MonoBehaviour scan (interfaces can't use GetComponent).
    /// </summary>
    public interface IInteractable
    {
        string PromptText { get; }
        void Interact(GameObject interactor);
    }

    public static class InteractableLookup
    {
        public static IInteractable FindOn(GameObject go)
        {
            foreach (var mb in go.GetComponents<MonoBehaviour>())
                if (mb is IInteractable i) return i;
            return null;
        }
    }
}
