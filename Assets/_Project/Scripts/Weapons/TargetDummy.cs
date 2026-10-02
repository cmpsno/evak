using UnityEngine;
using Evak.Core;

namespace Evak.Weapons
{
    /// <summary>
    /// v0 test-arena prop. Tag the GameObject "Target". Logs damage;
    /// no health bar yet (that's post-v0).
    /// </summary>
    public class TargetDummy : MonoBehaviour, IDamageable
    {
        public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, GameObject source)
        {
            Debug.Log($"[TargetDummy] {gameObject.name} took {amount} dmg at {hitPoint}");
            // Brief flash feedback
            var r = GetComponent<Renderer>();
            if (r != null) StartCoroutine(Flash(r));
        }

        private System.Collections.IEnumerator Flash(Renderer r)
        {
            var orig = r.material.color;
            r.material.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            r.material.color = orig;
        }
    }
}
