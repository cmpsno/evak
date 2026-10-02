using UnityEngine;

namespace Evak.Core
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
            // GetComponent<T>() requires T : Component, so interfaces need a manual scan.
            foreach (var mb in targetObject.GetComponents<MonoBehaviour>())
            {
                if (mb is IDamageable damageable)
                {
                    ResolveHit(damageable, amount, hitPoint, hitNormal, source);
                    return;
                }
            }
        }
    }
}
