using UnityEngine;

// TEMPORARY verification aid. Logs player position + nearest interactable to the
// browser console once per second. DELETE before shipping.
public class DebugPositionLogger : MonoBehaviour
{
    private float _t;

    void Update()
    {
        _t += Time.deltaTime;
        if (_t < 1f) return;
        _t = 0f;
        var p = transform.position;
        var fwd = transform.forward;
        Debug.Log($"[DBG] pos=({p.x:F1},{p.y:F1},{p.z:F1}) fwd=({fwd.x:F1},{fwd.z:F1})");
    }
}
