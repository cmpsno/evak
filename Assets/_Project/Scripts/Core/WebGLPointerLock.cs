using UnityEngine;
using UnityEngine.InputSystem;

namespace Evak.Core
{
    /// <summary>
    /// WebGL pointer lock. Browsers only grant mouse-look capture (pointer lock)
    /// from a user click on the canvas, so the first click locks the cursor;
    /// Esc releases it (handled by the browser). No-op outside WebGL builds.
    /// Attach to the player object.
    /// </summary>
    public class WebGLPointerLock : MonoBehaviour
    {
        private void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Cursor.lockState != CursorLockMode.Locked
                && Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
#endif
        }

        private void OnDisable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
#endif
        }
    }
}
