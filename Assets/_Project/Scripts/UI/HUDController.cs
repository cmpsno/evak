using UnityEngine;
using UnityEngine.UI;

namespace Evak.HUD
{
    /// <summary>
    /// Minimal v0 HUD: dynamic crosshair, ammo text, hitmarker flash.
    /// Plain uGUI Text/Image — no menus, health bar, or minimap (post-v0).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Crosshair")]
        public RectTransform crosshair;
        public float hipSpreadPx = 14f;
        public float adsSpreadPx = 4f;

        [Header("Ammo")]
        public Text ammoText;

        [Header("Hitmarker")]
        public Image hitmarker;
        public float hitmarkerTime = 0.12f;

        [Header("Reload")]
        public Text reloadText;

        private float _hitmarkerTimer;
        private bool _ads;

        public void SetAds(bool ads) => _ads = ads;

        public void SetAmmo(int mag, int reserve)
        {
            if (ammoText != null) ammoText.text = $"{mag} / {reserve}";
        }

        public void SetReloading(bool reloading)
        {
            if (reloadText != null) reloadText.gameObject.SetActive(reloading);
        }

        /// <summary>confirmedHit=true when ray hit a "Target"-tagged object.</summary>
        public void ShowHitmarker(bool confirmedHit)
        {
            if (hitmarker == null) return;
            hitmarker.color = confirmedHit ? Color.red : Color.white;
            hitmarker.gameObject.SetActive(true);
            _hitmarkerTimer = hitmarkerTime;
        }

        private void Update()
        {
            if (_hitmarkerTimer > 0f)
            {
                _hitmarkerTimer -= Time.deltaTime;
                if (_hitmarkerTimer <= 0f && hitmarker != null)
                    hitmarker.gameObject.SetActive(false);
            }
            if (crosshair != null)
            {
                float spread = _ads ? adsSpreadPx : hipSpreadPx;
                crosshair.localScale = Vector3.one * (1f + spread / 50f);
            }
        }
    }
}
