using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class GameInput
{
    public const float Deadzone = 0.2f;
    public static float CursorSpeedScreenHeights = 1.1f;

    static Vector2 virtualPos, lastMouse;
    static int lastFrame = -1;
    static bool initialized;
    public static float CursorSlow = 1f;
    public static bool UsingGamepad { get; private set; }

    public static bool GamepadConnected
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Gamepad.current != null;
#else
            return false;
#endif
        }
    }
    public static Vector2 PointerPosition
    {
        get { Refresh(); return UsingGamepad ? virtualPos : (Vector2)Input.mousePosition; }
    }
    public static bool ClickDown => Input.GetMouseButtonDown(0) || PadClickDown;
    public static bool ActionDown => ClickDown || Input.GetKeyDown(KeyCode.Space);
    public static bool PauseDown
    {
        get
        {
            if (Input.GetKeyDown(KeyCode.Escape)) return true;
#if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            return gp != null && gp.startButton.wasPressedThisFrame;
#else
            return false;
#endif
        }
    }

    public static Vector2 PanStick
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            return gp != null ? Dead(gp.rightStick.ReadValue()) : Vector2.zero;
#else
            return Vector2.zero;
#endif
        }
    }

    public static float ZoomAxis
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            return gp != null ? gp.rightTrigger.ReadValue() - gp.leftTrigger.ReadValue() : 0f;
#else
            return 0f;
#endif
        }
    }

    static bool PadClickDown
    {
        get
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton0)) return true;
#if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            return gp != null && gp.buttonSouth.wasPressedThisFrame;
#else
        return false;
#endif
        }
    }

    static Vector2 CursorStick
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            return gp != null ? Dead(gp.leftStick.ReadValue()) : Vector2.zero;
#else
        return Vector2.zero;
#endif
        }
    }

    static Vector2 Dead(Vector2 v)
    {
        float m = v.magnitude;
        if (m < Deadzone) return Vector2.zero;
        float s = Mathf.Min(1f, (m - Deadzone) / (1f - Deadzone));
        return v / m * s;
    }

    static void Refresh()
    {
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;

        Vector2 mouse = Input.mousePosition;
        if (!initialized) { initialized = true; virtualPos = lastMouse = mouse; }

        bool mouseActive = (mouse - lastMouse).sqrMagnitude > 4f || Input.GetMouseButtonDown(0);
        lastMouse = mouse;

        Vector2 stick = CursorStick;
        if (mouseActive) { UsingGamepad = false; virtualPos = mouse; }
        else if (stick.sqrMagnitude > 0f || PadClickDown) UsingGamepad = true;

        if (UsingGamepad && stick.sqrMagnitude > 0f)
        {
            float mag = stick.magnitude;
            Vector2 dir = stick / mag;
            virtualPos += dir * (mag * mag) * Screen.height * CursorSpeedScreenHeights * CursorSlow * Time.unscaledDeltaTime;
            virtualPos.x = Mathf.Clamp(virtualPos.x, 0f, Screen.width);
            virtualPos.y = Mathf.Clamp(virtualPos.y, 0f, Screen.height);
        }
    }
    public static bool NavLeftDown
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            return gp != null && gp.dpad.left.wasPressedThisFrame;
#else
        return false;
#endif
        }
    }

    public static bool NavRightDown
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            return gp != null && gp.dpad.right.wasPressedThisFrame;
#else
        return false;
#endif
        }
    }

    public static bool TabPrevDown
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            return gp != null && gp.leftShoulder.wasPressedThisFrame;
#else
        return false;
#endif
        }
    }

    public static bool TabNextDown
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            return gp != null && gp.rightShoulder.wasPressedThisFrame;
#else
        return false;
#endif
        }
    }

    public static bool BackDown
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            return gp != null && gp.buttonEast.wasPressedThisFrame;
#else
        return false;
#endif
        }
    }
}