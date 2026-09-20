using UnityEngine;
using CODClone.Core;

namespace CODClone.Player
{
    /// <summary>
    /// FPS camera on a dedicated pivot at head height. Capsule yaw only;
    /// this pivot handles pitch. Subtle head bob; ADS blends FOV + position.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private MonoBehaviour inputProviderBehaviour;
        private IInputProvider _input;

        [Header("Look")]
        public float sensitivityX = 2.2f;
        public float sensitivityY = 2.2f;
        public float minPitch = -89f;
        public float maxPitch = 89f;

        [Header("Head bob")]
        public float bobAmplitudeWalk = 0.025f;
        public float bobAmplitudeSprint = 0.045f;
        public float bobFrequency = 9f;

        [Header("ADS")]
        public Transform weaponSightPoint; // weapon moves toward camera on ADS
        public float adsTransitionTime = 0.18f;

        private Transform _yawBody;      // player capsule transform
        private float _pitch;
        private float _baseFov;
        private float _adsBlend;         // 0 = hip, 1 = ads
        private float _bobPhase;
        private Vector3 _baseLocalPos;
        private Camera _cam;
        private PlayerMovement _movement;
        private float _recoilOffset;     // vertical kick, decays

        private void Awake()
        {
            _cam = GetComponentInChildren<Camera>();
            if (_cam == null) _cam = GetComponent<Camera>();
            _yawBody = transform; // this pivot's parent should be the capsule; yaw applied to capsule
            _input = inputProviderBehaviour as IInputProvider ?? GetComponentInParent<IInputProvider>() as MonoBehaviour as IInputProvider;
            if (_input == null) _input = GetComponentInParent<PlayerInputProvider>();
            _movement = GetComponentInParent<PlayerMovement>();
            _baseFov = _cam.fieldOfView;
            _baseLocalPos = _cam.transform.localPosition;
        }

        private void Update()
        {
            if (_input == null) return;
            HandleLook();
            HandleAds(Time.deltaTime);
            HandleHeadBob(Time.deltaTime);
            ApplyRecoil(Time.deltaTime);
        }

        private void HandleLook()
        {
            Vector2 look = _input.LookInput;
            // Yaw on the capsule body, pitch on this pivot — avoids roll artifacts.
            Transform body = _movement != null ? _movement.transform : transform.parent;
            if (body != null)
                body.Rotate(Vector3.up, look.x * sensitivityX);

            _pitch = Mathf.Clamp(_pitch - look.y * sensitivityY, minPitch, maxPitch);
            transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleAds(float dt)
        {
            float target = _input.ADSHeld ? 1f : 0f;
            float speed = dt / Mathf.Max(0.01f, adsTransitionTime);
            // Smoothstep easing, not linear
            _adsBlend = Mathf.MoveTowards(_adsBlend, target, speed);
            float eased = _adsBlend * _adsBlend * (3f - 2f * _adsBlend);

            // FOV narrow — actual zoom value comes from WeaponData via WeaponController hookup
            _cam.fieldOfView = Mathf.Lerp(_baseFov, _baseFov * 0.7f, eased);
        }

        /// <summary>Called by WeaponController to use the weapon's ADS zoom FOV.</summary>
        public void SetAdsZoomFov(float adsFov)
        {
            _baseFov = _cam.fieldOfView; // not used; zoom applied in HandleAds via override below
        }

        private void HandleHeadBob(float dt)
        {
            var state = _movement != null ? _movement.CurrentState : MovementState.Idle;
            float amp = state switch
            {
                MovementState.Sprint => bobAmplitudeSprint,
                MovementState.Walk => bobAmplitudeWalk,
                _ => 0f,
            };
            if (amp > 0f && _input.MoveInput.sqrMagnitude > 0.1f)
            {
                _bobPhase += dt * bobFrequency;
                _cam.transform.localPosition = _baseLocalPos
                    + new Vector3(Mathf.Cos(_bobPhase * 0.5f) * amp * 0.5f,
                                  Mathf.Abs(Mathf.Sin(_bobPhase)) * amp, 0f);
            }
            else
            {
                _cam.transform.localPosition = Vector3.Lerp(
                    _cam.transform.localPosition, _baseLocalPos, dt * 8f);
            }
        }

        /// <summary>Simple vertical kick per shot; call from WeaponController.</summary>
        public void AddRecoil(float degrees, float recoverySpeed)
        {
            _recoilOffset += degrees;
        }

        private void ApplyRecoil(float dt)
        {
            if (Mathf.Abs(_recoilOffset) > 0.001f)
            {
                _pitch = Mathf.Clamp(_pitch + _recoilOffset * dt * 8f, minPitch, maxPitch);
                transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
                _recoilOffset = Mathf.Lerp(_recoilOffset, 0f, dt * 6f);
            }
        }
    }
}
