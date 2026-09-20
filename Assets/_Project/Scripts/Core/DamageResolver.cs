using UnityEngine;

namespace CODClone.Core
{
    /// <summary>
    /// Single choke point for all damage. v0: just forwards to IDamageable.
    /// Later this becomes the server-authoritative hook (validation, falloff,
    /// armor, killfeed events) without touching weapon code.
    /// </summary>
    public static class DamageResolver
    {
        public static void ResolveHit(IDamageable target, float amount,
            Vector3 hitPoint, Vector3 hitNormal, GameObject source)
        {
            if (target == null) return;
            target.ApplyDamage(amount, hitPoint, hitNormal, source);
        }

        public static void ResolveHit(GameObject targetObject, float amount,
            Vector3 hitPoint, Vector3 hitNormal, GameObject source)
        {
            if (targetObject == null) return;
            var damageable = targetObject.GetComponent<IDamageable>();
            if (damageable != null)
                ResolveHit(damageable, amount, hitPoint, hitNormal, source);
        }
    }
}
