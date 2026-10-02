using UnityEngine;
using Evak.Core;
using Evak.Player;

namespace Evak.Weapons
{
    /// <summary>
    /// Generic hitscan weapon driver. Reads WeaponData, never hardcodes stats.
    /// Raycast from camera center; visual trail from muzzle socket.
    /// Fire input respects FireMode (Auto/Semi; Burst stubbed for v0.2).
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [Header("Data")]
        public WeaponData weaponData;

        [Header("References")]
        [SerializeField] private MonoBehaviour inputProviderBehaviour;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform muzzleSocket;
        [SerializeField] private HUD.HUDController hud;

        private IInputProvider _input;
        private PlayerCamera _playerCam;
        private int _ammoInMag;
        private int _reserveAmmo = 120; // v0 placeholder; inventory system is v0.8
        private float _nextFireTime;
        private bool _reloading;
        private float _reloadEndTime;
        private bool _triggerHeld; // for semi-auto edge detection

        private void Awake()
        {
            _input = inputProviderBehaviour as IInputProvider
                ?? InputProviderLookup.FindOnSelfOrParents(this);
            _playerCam = GetComponentInParent<PlayerCamera>();
            if (playerCamera == null && _playerCam != null)
                playerCamera = _playerCam.GetComponentInChildren<Camera>();
            if (weaponData != null) _ammoInMag = weaponData.magazineSize;
        }

        private void Update()
        {
            if (_input == null || weaponData == null) return;

            if (_reloading && Time.time >= _reloadEndTime)
                FinishReload();

            if (_input.ReloadPressed && !_reloading && _ammoInMag < weaponData.magazineSize)
                StartReload();

            if (_reloading) return;

            bool wantFire = weaponData.fireMode switch
            {
                FireMode.Auto => _input.FireHeld,
                FireMode.Semi => _input.FirePressed,
                FireMode.Burst => _input.FirePressed, // stub: single shot until v0.2
                _ => false,
            };

            if (wantFire && Time.time >= _nextFireTime)
            {
                if (_ammoInMag <= 0)
                {
                    DryFire();
                    _nextFireTime = Time.time + 0.25f;
                }
                else
                {
                    Fire();
                    _nextFireTime = Time.time + 60f / weaponData.fireRate;
                }
            }

            hud?.SetAmmo(_ammoInMag, _reserveAmmo);
        }

        private void Fire()
        {
            _ammoInMag--;

            bool ads = _input.ADSHeld;
            float spreadDeg = ads ? weaponData.adsSpreadAngle : weaponData.hipFireSpreadAngle;

            Vector3 origin = playerCamera.transform.position;
            Vector3 dir = playerCamera.transform.forward;
            // Random cone spread
            dir = Quaternion.Euler(
                Random.Range(-spreadDeg, spreadDeg),
                Random.Range(-spreadDeg, spreadDeg), 0f) * dir;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, weaponData.range,
                ~0, QueryTriggerInteraction.Ignore))
            {
                SpawnImpact(hit.point, hit.normal);
                // Route ALL damage through the resolver — never direct.
                DamageResolver.ResolveHit(hit.collider.gameObject,
                    weaponData.damage, hit.point, hit.normal, gameObject);
                hud?.ShowHitmarker(hit.collider.CompareTag("Target"));
            }

            SpawnMuzzleFlash();
            PlayFireSound();
            _playerCam?.AddRecoil(weaponData.recoilKickPerShot, weaponData.recoilRecoverySpeed);

            if (_ammoInMag == 0)
                StartReload(); // auto reload on empty
        }

        private void DryFire()
        {
            if (weaponData.emptySound != null)
                AudioSource.PlayClipAtPoint(weaponData.emptySound, transform.position);
            hud?.ShowHitmarker(false);
        }

        private void StartReload()
        {
            if (_reserveAmmo <= 0 || _ammoInMag >= weaponData.magazineSize) return;
            _reloading = true;
            bool empty = _ammoInMag == 0;
            float t = empty ? weaponData.reloadTimeEmpty : weaponData.reloadTimeTactical;
            _reloadEndTime = Time.time + t;
            hud?.SetReloading(true);
            if (weaponData.reloadSound != null)
                AudioSource.PlayClipAtPoint(weaponData.reloadSound, transform.position);
        }

        private void FinishReload()
        {
            int needed = weaponData.magazineSize - _ammoInMag;
            int taken = Mathf.Min(needed, _reserveAmmo);
            _ammoInMag += taken;
            _reserveAmmo -= taken;
            _reloading = false;
            hud?.SetReloading(false);
        }

        private void SpawnMuzzleFlash()
        {
            if (weaponData.muzzleFlashPrefab != null && muzzleSocket != null)
            {
                var flash = Instantiate(weaponData.muzzleFlashPrefab,
                    muzzleSocket.position, muzzleSocket.rotation);
                Destroy(flash, 0.06f);
            }
        }

        private void SpawnImpact(Vector3 point, Vector3 normal)
        {
            GameObject prefab = weaponData.impactDecalPrefab;
            GameObject decal = prefab != null
                ? Instantiate(prefab, point + normal * 0.01f, Quaternion.LookRotation(normal))
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (prefab == null)
            {
                decal.transform.localScale = Vector3.one * 0.05f;
                var r = decal.GetComponent<Renderer>();
                if (r != null) r.material.color = Color.yellow;
                Destroy(decal.GetComponent<Collider>());
            }
            Destroy(decal, 8f);
        }

        private void PlayFireSound()
        {
            if (weaponData.fireSound != null)
                AudioSource.PlayClipAtPoint(weaponData.fireSound, transform.position);
        }
    }
}
