using UnityEngine;

namespace Evak.Weapons
{
    public enum FireMode { Semi, Auto, Burst }

    /// <summary>
    /// Data-driven weapon definition. v0 ships ONE instance (hitscan rifle).
    /// v0.2 extends this same asset type — never subclass per weapon.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Evak/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "Rifle";

        [Header("Fire")]
        public FireMode fireMode = FireMode.Auto;
        public float damage = 25f;
        public float fireRate = 600f; // rounds per minute
        public int magazineSize = 30;
        public float range = 100f;

        [Header("Reload")]
        public float reloadTimeTactical = 2.0f;
        public float reloadTimeEmpty = 2.6f;

        [Header("Spread (degrees, cone half-angle)")]
        public float hipFireSpreadAngle = 2.5f;
        public float adsSpreadAngle = 0.4f;

        [Header("ADS")]
        public float adsZoomFOV = 45f;
        public float adsTransitionTime = 0.18f;

        [Header("Recoil (v0: simple kick; v0.2: authored patterns)")]
        public float recoilKickPerShot = 0.6f; // degrees vertical
        public float recoilRecoverySpeed = 6f;

        [Header("References")]
        public GameObject muzzleFlashPrefab;
        public GameObject impactDecalPrefab;
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip emptySound;
    }
}
