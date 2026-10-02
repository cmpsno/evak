using UnityEngine;

namespace Evak.Core
{
    /// <summary>
    /// Abstraction over all input sources. Movement/weapon code must ONLY
    /// read through this interface — never touch InputSystem directly.
    /// Lets bots and future networked players drive the same controller.
    /// </summary>
    public interface IInputProvider
    {
        Vector2 MoveInput { get; }
        Vector2 LookInput { get; }
        bool JumpPressed { get; }
        bool SprintHeld { get; }
        bool CrouchHeld { get; }
        bool FirePressed { get; }
        bool FireHeld { get; }
        bool ReloadPressed { get; }
        bool ADSHeld { get; }
        bool InteractPressed { get; }
        // Dialogue choice shortcuts (1/2/3). Mobile uses the on-screen buttons.
        bool Choice1Pressed { get; }
        bool Choice2Pressed { get; }
        bool Choice3Pressed { get; }

        /// <summary>
        /// TICKET-EP1-01 v2 input gate. When false, all GAMEPLAY input
        /// (move/look/jump/sprint/crouch/fire/reload/ads/interact) reads zero.
        /// Choice1/2/3 stay live so dialogue choices remain selectable.
        /// Written only by DialogueController (and future CinematicPlayer).
        /// </summary>
        bool GameplayInputEnabled { get; set; }
    }

    /// <summary>
    /// Unity's GetComponent&lt;T&gt; requires T : Component, so interfaces need
    /// a manual scan. Use these helpers instead of GetComponent&lt;IInputProvider&gt;.
    /// </summary>
    public static class InputProviderLookup
    {
        public static IInputProvider FindOnSelf(Component c)
        {
            foreach (var mb in c.GetComponents<MonoBehaviour>())
                if (mb is IInputProvider p) return p;
            return null;
        }

        public static IInputProvider FindOnSelfOrParents(Component c)
        {
            foreach (var mb in c.GetComponentsInParent<MonoBehaviour>(true))
                if (mb is IInputProvider p) return p;
            return null;
        }
    }
}
