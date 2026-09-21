using UnityEngine;
using UnityEngine.InputSystem;
using CODClone.Core;

namespace CODClone.Diagnostics
{
    /// <summary>
    /// TEMP verification probe (delete before ship). Logs Input System device
    /// status + action states at startup so headless runs can diagnose dead input.
    /// </summary>
    public class DebugInputProbe : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log($"[InputProbe] devices={InputSystem.devices.Count} " +
                      $"keyboard={Keyboard.current != null} mouse={Mouse.current != null}");
            var pi = GetComponent<PlayerInput>();
            if (pi == null) { Debug.Log("[InputProbe] NO PlayerInput on player!"); return; }
            var actions = pi.actions;
            Debug.Log($"[InputProbe] PlayerInput.enabled={pi.enabled} actions={(actions != null ? "non-null" : "NULL")}");
            if (actions != null)
            {
                foreach (var map in actions.actionMaps)
                    Debug.Log($"[InputProbe] map={map.name} enabled={map.enabled} actions={map.actions.Count}");
                var move = actions.FindAction("Move");
                Debug.Log($"[InputProbe] FindAction(Move)={(move != null ? "found enabled=" + move.enabled : "NULL")}");
            }
            var provider = GetComponent<PlayerInputProvider>();
            if (provider != null)
                Debug.Log($"[InputProbe] gate={provider.GameplayInputEnabled}");
        }

        private float _t;
        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (_t >= 3f)
            {
                _t = 0f;
                var provider = GetComponent<PlayerInputProvider>();
                // Dual-system diagnosis: old Input Manager vs new Input System device vs action.
                bool oldW = Input.GetKey(KeyCode.W);
                bool newW = Keyboard.current != null && Keyboard.current.wKey.isPressed;
                var pi = GetComponent<PlayerInput>();
                string actionW = "n/a";
                if (pi != null && pi.actions != null)
                {
                    var mv = pi.actions.FindAction("Move");
                    if (mv != null) actionW = mv.ReadValue<Vector2>().ToString();
                }
                Debug.Log($"[InputProbe] DUAL old_GetKey(W)={oldW} new_Keyboard.w={newW} action_Move={actionW}");
                if (provider != null)
                    Debug.Log($"[InputProbe] live move={provider.MoveInput} look={provider.LookInput} " +
                              $"interact={provider.InteractPressed} gate={provider.GameplayInputEnabled}");
            }
        }
    }
}
