using UnityEngine;
using UnityEngine.InputSystem;

namespace Evak.Core
{
    /// <summary>
    /// Local-player implementation of IInputProvider backed by the new Input System.
    /// Attach to the player object alongside PlayerMovement / WeaponController.
    /// Input Actions asset expected with actions: Move, Look, Jump, Sprint,
    /// Crouch, Fire, Reload, ADS. Wire in the Inspector or via generated C# class.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("Input Actions (from PlayerInput component)")]
        [SerializeField] private string moveAction = "Move";
        [SerializeField] private string lookAction = "Look";
        [SerializeField] private string jumpAction = "Jump";
        [SerializeField] private string sprintAction = "Sprint";
        [SerializeField] private string crouchAction = "Crouch";
        [SerializeField] private string fireAction = "Fire";
        [SerializeField] private string reloadAction = "Reload";
        [SerializeField] private string adsAction = "ADS";
        [SerializeField] private string interactAction = "Interact";
        [SerializeField] private string choice1Action = "Choice1";
        [SerializeField] private string choice2Action = "Choice2";
        [SerializeField] private string choice3Action = "Choice3";

        private PlayerInput _playerInput;

        public bool GameplayInputEnabled { get; set; } = true;

        public Vector2 MoveInput => _gated ? Vector2.zero : ReadVector(moveAction);
        public Vector2 LookInput => _gated ? Vector2.zero : ReadVector(lookAction);
        public bool JumpPressed => !_gated && ReadButtonPressed(jumpAction);
        public bool SprintHeld => !_gated && ReadButtonHeld(sprintAction);
        public bool CrouchHeld => !_gated && ReadButtonHeld(crouchAction);
        public bool FirePressed => !_gated && ReadButtonPressed(fireAction);
        public bool FireHeld => !_gated && ReadButtonHeld(fireAction);
        public bool ReloadPressed => !_gated && ReadButtonPressed(reloadAction);
        public bool ADSHeld => !_gated && ReadButtonHeld(adsAction);
        public bool InteractPressed => !_gated && ReadButtonPressed(interactAction);
        // Choice keys stay live during dialogue (the gate only kills gameplay input).
        public bool Choice1Pressed => ReadButtonPressed(choice1Action);
        public bool Choice2Pressed => ReadButtonPressed(choice2Action);
        public bool Choice3Pressed => ReadButtonPressed(choice3Action);

        private bool _gated => !GameplayInputEnabled;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        private Vector2 ReadVector(string actionName)
        {
            var action = _playerInput.actions.FindAction(actionName);
            return action != null ? action.ReadValue<Vector2>() : Vector2.zero;
        }

        private bool ReadButtonHeld(string actionName)
        {
            var action = _playerInput.actions.FindAction(actionName);
            return action != null && action.IsPressed();
        }

        private bool ReadButtonPressed(string actionName)
        {
            var action = _playerInput.actions.FindAction(actionName);
            return action != null && action.WasPressedThisFrame();
        }
    }
}
