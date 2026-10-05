using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && RUNEHEIR_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace Runeheir.Controls
{
    public enum GameKey
    {
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        Escape,
        Enter,
        Tab,
        Alt,
        Shift,
        Ctrl,
        A,
        E,
        S,
        Q,
        Insert,
        Delete,
        Space,
        Up,
        Down,
        Left,
        Right,
    }

    /// <summary>
    /// One input facade for the whole game. Works with the new Input System package
    /// (Unity 6 default) and with the legacy Input Manager, chosen at compile time.
    /// </summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM && RUNEHEIR_INPUT_SYSTEM
        public const bool UsesInputSystemPackage = true;

        public static Vector2 PointerPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        public static bool PrimaryDown => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        public static bool PrimaryHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;

        public static bool PrimaryUp => Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;

        public static bool SecondaryDown => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

        public static bool SecondaryHeld => Mouse.current != null && Mouse.current.rightButton.isPressed;

        public static bool SecondaryUp => Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame;

        /// <summary>Wheel notches this frame: +1 up, -1 down (magnitude ignored; differs per platform).</summary>
        public static float ScrollSteps
        {
            get
            {
                if (Mouse.current == null)
                {
                    return 0f;
                }

                float y = Mouse.current.scroll.ReadValue().y;
                return Mathf.Abs(y) < 0.01f ? 0f : Mathf.Sign(y);
            }
        }

        public static bool KeyDown(GameKey key)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            switch (key)
            {
                case GameKey.Enter: return keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
                case GameKey.Alt: return keyboard.leftAltKey.wasPressedThisFrame || keyboard.rightAltKey.wasPressedThisFrame;
                case GameKey.Shift: return keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame;
                case GameKey.Ctrl: return keyboard.leftCtrlKey.wasPressedThisFrame || keyboard.rightCtrlKey.wasPressedThisFrame;
                default: return keyboard[ToKey(key)].wasPressedThisFrame;
            }
        }

        public static bool KeyHeld(GameKey key)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            switch (key)
            {
                case GameKey.Enter: return keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed;
                case GameKey.Alt: return keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;
                case GameKey.Shift: return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                case GameKey.Ctrl: return keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
                default: return keyboard[ToKey(key)].isPressed;
            }
        }

        private static Key ToKey(GameKey key)
        {
            switch (key)
            {
                case GameKey.F1: return Key.F1;
                case GameKey.F2: return Key.F2;
                case GameKey.F3: return Key.F3;
                case GameKey.F4: return Key.F4;
                case GameKey.F5: return Key.F5;
                case GameKey.F6: return Key.F6;
                case GameKey.F7: return Key.F7;
                case GameKey.F8: return Key.F8;
                case GameKey.F9: return Key.F9;
                case GameKey.F10: return Key.F10;
                case GameKey.F11: return Key.F11;
                case GameKey.F12: return Key.F12;
                case GameKey.Escape: return Key.Escape;
                case GameKey.Tab: return Key.Tab;
                case GameKey.A: return Key.A;
                case GameKey.E: return Key.E;
                case GameKey.S: return Key.S;
                case GameKey.Q: return Key.Q;
                case GameKey.Insert: return Key.Insert;
                case GameKey.Delete: return Key.Delete;
                case GameKey.Space: return Key.Space;
                case GameKey.Up: return Key.UpArrow;
                case GameKey.Down: return Key.DownArrow;
                case GameKey.Left: return Key.LeftArrow;
                case GameKey.Right: return Key.RightArrow;
                default: return Key.None;
            }
        }
#else
        public const bool UsesInputSystemPackage = false;

        public static Vector2 PointerPosition => Input.mousePosition;

        public static bool PrimaryDown => Input.GetMouseButtonDown(0);

        public static bool PrimaryHeld => Input.GetMouseButton(0);

        public static bool PrimaryUp => Input.GetMouseButtonUp(0);

        public static bool SecondaryDown => Input.GetMouseButtonDown(1);

        public static bool SecondaryHeld => Input.GetMouseButton(1);

        public static bool SecondaryUp => Input.GetMouseButtonUp(1);

        public static float ScrollSteps
        {
            get
            {
                float y = Input.mouseScrollDelta.y;
                return Mathf.Abs(y) < 0.01f ? 0f : Mathf.Sign(y);
            }
        }

        public static bool KeyDown(GameKey key)
        {
            switch (key)
            {
                case GameKey.Enter: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
                case GameKey.Alt: return Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt);
                case GameKey.Shift: return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
                case GameKey.Ctrl: return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
                default: return Input.GetKeyDown(ToKeyCode(key));
            }
        }

        public static bool KeyHeld(GameKey key)
        {
            switch (key)
            {
                case GameKey.Enter: return Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter);
                case GameKey.Alt: return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                case GameKey.Shift: return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                case GameKey.Ctrl: return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                default: return Input.GetKey(ToKeyCode(key));
            }
        }

        private static KeyCode ToKeyCode(GameKey key)
        {
            switch (key)
            {
                case GameKey.F1: return KeyCode.F1;
                case GameKey.F2: return KeyCode.F2;
                case GameKey.F3: return KeyCode.F3;
                case GameKey.F4: return KeyCode.F4;
                case GameKey.F5: return KeyCode.F5;
                case GameKey.F6: return KeyCode.F6;
                case GameKey.F7: return KeyCode.F7;
                case GameKey.F8: return KeyCode.F8;
                case GameKey.F9: return KeyCode.F9;
                case GameKey.F10: return KeyCode.F10;
                case GameKey.F11: return KeyCode.F11;
                case GameKey.F12: return KeyCode.F12;
                case GameKey.Escape: return KeyCode.Escape;
                case GameKey.Tab: return KeyCode.Tab;
                case GameKey.A: return KeyCode.A;
                case GameKey.E: return KeyCode.E;
                case GameKey.S: return KeyCode.S;
                case GameKey.Q: return KeyCode.Q;
                case GameKey.Insert: return KeyCode.Insert;
                case GameKey.Delete: return KeyCode.Delete;
                case GameKey.Space: return KeyCode.Space;
                case GameKey.Up: return KeyCode.UpArrow;
                case GameKey.Down: return KeyCode.DownArrow;
                case GameKey.Left: return KeyCode.LeftArrow;
                case GameKey.Right: return KeyCode.RightArrow;
                default: return KeyCode.None;
            }
        }
#endif
    }

    /// <summary>Answers "is the mouse over a window?" and "is the player typing?".</summary>
    public static class UIFocus
    {
        public static bool PointerOverUI
        {
            get
            {
                var eventSystem = EventSystem.current;
                return eventSystem != null && eventSystem.IsPointerOverGameObject();
            }
        }

        public static bool IsTyping
        {
            get
            {
                var eventSystem = EventSystem.current;
                if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
                {
                    return false;
                }

                var field = eventSystem.currentSelectedGameObject.GetComponent<InputField>();
                return field != null && field.isFocused;
            }
        }
    }

    /// <summary>Creates an EventSystem with the input module that matches the active input backend.</summary>
    public static class EventSystemBootstrap
    {
        public static void Ensure()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && RUNEHEIR_INPUT_SYSTEM
            var module = go.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null)
            {
                module.AssignDefaultActions();
            }
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
