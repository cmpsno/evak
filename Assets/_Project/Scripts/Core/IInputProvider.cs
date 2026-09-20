using UnityEngine;

namespace CODClone.Core
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
    }
}
