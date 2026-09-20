using UnityEngine;

namespace CODClone.Core
{
    /// <summary>
    /// Anything that can take damage implements this. All weapon hits route
    /// through DamageResolver, which calls this — never call ApplyDamage
    /// directly from weapon code.
    /// </summary>
    public interface IDamageable
    {
        void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, GameObject source);
    }
}
