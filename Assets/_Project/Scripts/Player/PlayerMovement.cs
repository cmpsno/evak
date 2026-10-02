using UnityEngine;
using Evak.Core;

namespace Evak.Player
{
    public enum MovementState { Idle, Walk, Sprint, Crouch, Slide, Airborne }

    /// <summary>
    /// Dumb executor of IInputProvider. All physics in FixedUpdate.
    /// Slide: sprint + crouch press -> momentum burst, min duration + cooldown.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private MonoBehaviour inputProviderBehaviour;
        private IInputProvider _input;

        [Header("Speeds")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7.5f;
        public float crouchSpeed = 2.5f;

        [Header("Slide")]
        public float slideSpeed = 8.0f;
        public float slideDuration = 0.6f;
        public float slideCooldown = 0.8f;

        [Header("Jump / Gravity")]
        public float jumpHeight = 1.2f;
        public float gravity = -20f;
        public float airControlMultiplier = 0.5f;
        public float coyoteTime = 0.1f;

        [Header("Capsule")]
        public float standingHeight = 1.8f;
        public float crouchHeight = 1.0f;
        public float crouchTransitionSpeed = 8f;

        public MovementState CurrentState { get; private set; } = MovementState.Idle;

        private CharacterController _controller;
        private Vector3 _velocity;
        private float _slideTimer;
        private float _slideCooldownTimer;
        private float _coyoteTimer;
        private float _targetHeight;
        private bool _wasCrouchHeld;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = inputProviderBehaviour as IInputProvider
                ?? InputProviderLookup.FindOnSelf(this);
            _targetHeight = standingHeight;
        }

        private void FixedUpdate()
        {
            if (_input == null) return;

            // TICKET-EP1-01 v2: dialogue freezes the player via the IInputProvider
            // gate (DialogueController sets GameplayInputEnabled=false), so
            // MoveInput reads zero here. Gravity still applies.
            UpdateTimers();
            UpdateGroundState();
            HandleSlideTrigger();
            HandleJump();
            Move();
            UpdateCapsuleHeight();
            UpdateState();
        }

        private void UpdateTimers()
        {
            _slideCooldownTimer = Mathf.Max(0f, _slideCooldownTimer - Time.fixedDeltaTime);
            _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.fixedDeltaTime);
            if (CurrentState == MovementState.Slide)
            {
                _slideTimer -= Time.fixedDeltaTime;
                if (_slideTimer <= 0f) EndSlide();
            }
        }

        private bool IsGrounded()
        {
            // Spherecast is more reliable than isGrounded alone on slopes/edges.
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            return Physics.SphereCast(origin, _controller.radius * 0.9f,
                Vector3.down, out _, 0.25f, ~0, QueryTriggerInteraction.Ignore)
                || _controller.isGrounded;
        }

        private void UpdateGroundState()
        {
            if (IsGrounded())
            {
                _coyoteTimer = coyoteTime;
                if (_velocity.y < 0f) _velocity.y = -2f; // stick to ground
            }
        }

        private void HandleSlideTrigger()
        {
            bool crouchPressed = _input.CrouchHeld && !_wasCrouchHeld;
            _wasCrouchHeld = _input.CrouchHeld;

            bool sprinting = CurrentState == MovementState.Sprint;
            if (crouchPressed && sprinting && _slideCooldownTimer <= 0f && IsGrounded())
                StartSlide();
        }

        private void StartSlide()
        {
            CurrentState = MovementState.Slide;
            _slideTimer = slideDuration;
            _slideCooldownTimer = slideCooldown;
            _targetHeight = crouchHeight;
            // Burst along current move dir (or facing if idle)
            Vector3 dir = GetWishDir();
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            _velocity.x = dir.x * slideSpeed;
            _velocity.z = dir.z * slideSpeed;
        }

        private void EndSlide()
        {
            CurrentState = _input.CrouchHeld ? MovementState.Crouch : MovementState.Walk;
            _targetHeight = _input.CrouchHeld ? crouchHeight : standingHeight;
        }

        private void HandleJump()
        {
            if (_input.JumpPressed && (IsGrounded() || _coyoteTimer > 0f)
                && CurrentState != MovementState.Slide)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _coyoteTimer = 0f;
                if (_input.CrouchHeld) _targetHeight = crouchHeight;
            }
        }

        private Vector3 GetWishDir()
        {
            Vector2 m = _input.MoveInput;
            Vector3 dir = transform.TransformDirection(new Vector3(m.x, 0f, m.y));
            dir.y = 0f;
            return dir.normalized;
        }

        private void Move()
        {
            Vector3 wishDir = GetWishDir();
            float targetSpeed = walkSpeed;
            bool grounded = IsGrounded();

            if (CurrentState == MovementState.Slide)
            {
                // Decaying momentum, no steering during slide
                float decay = Mathf.Clamp01(_slideTimer / slideDuration);
                Vector3 horizontal = new Vector3(_velocity.x, 0f, _velocity.z);
                horizontal *= Mathf.Lerp(0.4f, 1f, decay);
                _velocity.x = horizontal.x;
                _velocity.z = horizontal.z;
            }
            else
            {
                bool sprinting = _input.SprintHeld && _input.MoveInput.y > 0.1f
                    && !(_input.CrouchHeld || _input.ADSHeld) && grounded;
                bool crouching = _input.CrouchHeld && grounded;

                targetSpeed = crouching ? crouchSpeed : sprinting ? sprintSpeed : walkSpeed;
                _targetHeight = crouching ? crouchHeight : standingHeight;

                float control = grounded ? 1f : airControlMultiplier;
                Vector3 horizontal = Vector3.Lerp(
                    new Vector3(_velocity.x, 0f, _velocity.z),
                    wishDir * targetSpeed, control * 10f * Time.fixedDeltaTime);
                _velocity.x = horizontal.x;
                _velocity.z = horizontal.z;
            }

            _velocity.y += gravity * Time.fixedDeltaTime;
            _controller.Move(_velocity * Time.fixedDeltaTime);
        }

        private void UpdateCapsuleHeight()
        {
            float h = Mathf.Lerp(_controller.height, _targetHeight,
                crouchTransitionSpeed * Time.fixedDeltaTime);
            float delta = h - _controller.height;
            _controller.height = h;
            _controller.center = new Vector3(0f, h / 2f, 0f);
            // Keep feet planted when height changes
            transform.position += Vector3.up * delta / 2f;
        }

        private void UpdateState()
        {
            if (CurrentState == MovementState.Slide) return; // timer ends it
            if (!IsGrounded()) { CurrentState = MovementState.Airborne; return; }

            Vector3 h = new Vector3(_velocity.x, 0f, _velocity.z);
            if (_input.CrouchHeld) { CurrentState = MovementState.Crouch; return; }
            if (h.magnitude < 0.5f) { CurrentState = MovementState.Idle; return; }
            bool sprinting = _input.SprintHeld && _input.MoveInput.y > 0.1f && !_input.ADSHeld;
            CurrentState = sprinting ? MovementState.Sprint : MovementState.Walk;
        }
    }
}
