using UnityEngine;
using UnityEngine.InputSystem;

namespace Tapshot
{
    /// <summary>
    /// Tracks whether the player is driving the UI with keyboard/controller (show focus rings)
    /// or with the mouse (hide them).
    /// </summary>
    public static class UIInputMode
    {
        public static bool Navigating { get; private set; }

        static int lastPolledFrame = -1;

        public static void Poll()
        {
            if (lastPolledFrame == Time.frameCount) return;
            lastPolledFrame = Time.frameCount;

            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.delta.ReadValue().sqrMagnitude > 4f))
                Navigating = false;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                Navigating = true;

            var pad = Gamepad.current;
            if (pad != null && (pad.dpad.ReadValue().sqrMagnitude > 0.25f
                                || pad.leftStick.ReadValue().sqrMagnitude > 0.25f
                                || pad.buttonSouth.wasPressedThisFrame
                                || pad.buttonEast.wasPressedThisFrame
                                || pad.startButton.wasPressedThisFrame))
                Navigating = true;
        }

        public static bool CancelPressed()
        {
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                   || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            Navigating = false;
            lastPolledFrame = -1;
        }
    }
}
