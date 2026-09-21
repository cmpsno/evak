using UnityEngine;
using UnityEngine.InputSystem;

namespace CODClone.Core
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

        public Vector2 MoveInput => ReadVector(moveAction);
        public Vector2 LookInput => ReadVector(lookAction);
        public bool JumpPressed => ReadButtonPressed(jumpAction);
        public bool SprintHeld => ReadButtonHeld(sprintAction);
        public bool CrouchHeld => ReadButtonHeld(crouchAction);
        public bool FirePressed => ReadButtonPressed(fireAction);
        public bool FireHeld => ReadButtonHeld(fireAction);
        public bool ReloadPressed => ReadButtonPressed(reloadAction);
        public bool ADSHeld => ReadButtonHeld(adsAction);
        public bool InteractPressed => ReadButtonPressed(interactAction);
        public bool Choice1Pressed => ReadButtonPressed(choice1Action);
        public bool Choice2Pressed => ReadButtonPressed(choice2Action);
        public bool Choice3Pressed => ReadButtonPressed(choice3Action);

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
