using System;
using UnityEngine;

// Keyboard, mouse and touch input for the playable UltraBlade 2 slice.
// Desktop: WASD/arrows, Space or left mouse attack, LeftShift dash, Escape/P pause.
// Mobile: left-half floating joystick, right-bottom attack (upper) and dash (lower).
public static class GameInput
{
    private const float ReferenceHeight = 720f;

    public static Vector2 Move;
    public static bool AttackPressed;
    public static bool DashPressed;
    public static bool PausePressed;
    public static bool ClickPressed;
    public static Vector2 ClickPosition;
    public static bool TouchActive;

    private static int joystickTouchId = -1;
    private static Vector2 joystickOrigin;
    private static bool attackHeld;
    private static bool dashHeld;

    private static Texture2D circleTexture;
    private static GUIStyle buttonLabel;
    private static int drawStyleKey = int.MinValue;

    public static void Update()
    {
        Move = Vector2.zero;
        AttackPressed = false;
        DashPressed = false;
        PausePressed = false;
        ClickPressed = false;
        ClickPosition = Input.mousePosition;
        TouchActive = false;

        Vector2 keyboardMove = ReadKeyboard();
        Vector2 touchMove = Vector2.zero;

        Touch[] touches = null;
        try
        {
            touches = Input.touches;
        }
        catch (Exception)
        {
            touches = null;
        }

        if (touches != null && touches.Length > 0)
        {
            touchMove = ReadTouches(touches);
            TouchActive = true;
        }
        else
        {
            joystickTouchId = -1;
            attackHeld = false;
            dashHeld = false;
            ReadMouse();
        }

        Move = touchMove.sqrMagnitude > 0.0001f ? touchMove : keyboardMove;
        if (Move.sqrMagnitude > 1f)
        {
            Move = Move.normalized;
        }
    }

    public static void DrawTouchControls(GameSettings settings)
    {
        if (settings != null && !settings.touchControls)
        {
            return;
        }
        // Touch affordances are only drawn where they can be used: on mobile, or while
        // an actual touch is present. This keeps the keyboard-only desktop HUD free of
        // dead on-screen controls.
        if (!Application.isMobilePlatform && Input.touchCount == 0)
        {
            return;
        }
        try
        {
            EnsureDrawStyles();
            if (circleTexture == null || buttonLabel == null)
            {
                return;
            }

            bool stickActive = joystickTouchId >= 0;
            Vector2 baseCenter = stickActive ? joystickOrigin : JoystickHome;
            float radius = JoystickRadius;
            DrawDisc(baseCenter, radius, new Color(0.18f, 0.88f, 1f, stickActive ? 0.78f : 0.45f));
            DrawDisc(baseCenter, Mathf.Max(1f, radius - 3f * UiScale),
                new Color(0.012f, 0.035f, 0.05f, 0.55f));

            Vector2 knob = stickActive
                ? baseCenter + Vector2.ClampMagnitude(Move, 1f) * radius
                : baseCenter;
            DrawDisc(knob, 40f * UiScale, new Color(0.3f, 0.88f, 1f, 0.32f));
            DrawDisc(knob, 33f * UiScale, new Color(0.72f, 0.96f, 1f, 0.7f));

            DrawActionButton(AttackCenter, "ATTACK", new Color(1f, 0.3f, 0.58f),
                attackHeld || AttackPressed);
            DrawActionButton(DashCenter, "DASH", new Color(0.25f, 0.9f, 1f),
                dashHeld || DashPressed);
        }
        catch (Exception)
        {
            // Drawing must never break the match loop.
        }
    }

    // ---------------------------------------------------------------- reading

    private static Vector2 ReadKeyboard()
    {
        float x = 0f;
        float y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            x -= 1f;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            x += 1f;
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            y += 1f;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            y -= 1f;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AttackPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            DashPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            PausePressed = true;
        }

        var move = new Vector2(x, y);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }
        return move;
    }

    private static void ReadMouse()
    {
        ClickPosition = Input.mousePosition;
        if (Input.GetMouseButtonDown(0))
        {
            ClickPressed = true;
            AttackPressed = true;
        }
    }

    private static Vector2 ReadTouches(Touch[] touches)
    {
        attackHeld = false;
        dashHeld = false;
        float stickRadius = JoystickRadius;
        float buttonRadius = ButtonRadius;
        float buttonRadiusSqr = buttonRadius * buttonRadius;
        Vector2 attackCenter = AttackCenter;
        Vector2 dashCenter = DashCenter;
        int assigned = joystickTouchId;
        bool stickHeld = false;
        Vector2 move = Vector2.zero;

        for (int i = 0; i < touches.Length; i++)
        {
            Touch touch = touches[i];
            Vector2 position = touch.position;
            bool active = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            bool onAttack = (position - attackCenter).sqrMagnitude <= buttonRadiusSqr;
            bool onDash = (position - dashCenter).sqrMagnitude <= buttonRadiusSqr;

            if (onAttack && active)
            {
                attackHeld = true;
                if (touch.phase == TouchPhase.Began)
                {
                    AttackPressed = true;
                }
            }
            if (onDash && active)
            {
                dashHeld = true;
                if (touch.phase == TouchPhase.Began)
                {
                    DashPressed = true;
                }
            }

            if (assigned >= 0 && touch.fingerId == assigned && active)
            {
                stickHeld = true;
                Vector2 delta = position - joystickOrigin;
                float magnitude = delta.magnitude;
                if (magnitude > stickRadius)
                {
                    Vector2 direction = delta / Mathf.Max(0.0001f, magnitude);
                    joystickOrigin = position - direction * stickRadius;
                    delta = direction * stickRadius;
                    magnitude = stickRadius;
                }
                if (magnitude > stickRadius * 0.12f)
                {
                    move = delta / Mathf.Max(0.0001f, stickRadius);
                }
            }
            else if (assigned < 0 && touch.phase == TouchPhase.Began && !onAttack && !onDash &&
                     position.x < Screen.width * 0.5f)
            {
                assigned = touch.fingerId;
                joystickOrigin = position;
                stickHeld = true;
                move = Vector2.zero;
            }
        }

        joystickTouchId = stickHeld ? assigned : -1;
        if (!stickHeld)
        {
            return Vector2.zero;
        }
        return Vector2.ClampMagnitude(move, 1f);
    }

    // --------------------------------------------------------------- drawing

    private static float UiScale
    {
        get { return Mathf.Clamp(Screen.height / ReferenceHeight, 0.5f, 3f); }
    }

    private static float JoystickRadius
    {
        get { return 92f * UiScale; }
    }

    private static float ButtonRadius
    {
        get { return 58f * UiScale; }
    }

    private static Vector2 JoystickHome
    {
        get
        {
            float s = UiScale;
            return new Vector2(150f * s, Screen.height - 150f * s);
        }
    }

    private static Vector2 AttackCenter
    {
        get
        {
            float s = UiScale;
            return new Vector2(Screen.width - 130f * s, Screen.height - 240f * s);
        }
    }

    private static Vector2 DashCenter
    {
        get
        {
            float s = UiScale;
            return new Vector2(Screen.width - 130f * s, Screen.height - 100f * s);
        }
    }

    private static void DrawActionButton(Vector2 center, string label, Color color, bool pressed)
    {
        float radius = ButtonRadius;
        float s = UiScale;
        DrawDisc(center, radius, new Color(color.r, color.g, color.b, pressed ? 0.92f : 0.52f));
        DrawDisc(center, Mathf.Max(1f, radius - 4f * s),
            pressed
                ? new Color(color.r, color.g, color.b, 0.48f)
                : new Color(0.01f, 0.03f, 0.045f, 0.68f));
        Rect labelRect = new Rect(center.x - radius, center.y - 12f * s, radius * 2f, 24f * s);
        GUI.Label(labelRect, label, buttonLabel);
    }

    private static void DrawDisc(Vector2 center, float radius, Color color)
    {
        if (radius <= 0f || color.a <= 0f || circleTexture == null)
        {
            return;
        }
        Rect rect = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, circleTexture, ScaleMode.StretchToFill, true);
        GUI.color = previous;
    }

    private static void EnsureDrawStyles()
    {
        int key = Mathf.RoundToInt(UiScale * 100f);
        bool created = false;
        if (circleTexture == null)
        {
            circleTexture = BuildCircleTexture(96);
            created = true;
        }
        if (!created && key == drawStyleKey && buttonLabel != null)
        {
            return;
        }

        GUIStyle source = GUI.skin != null ? GUI.skin.label : null;
        buttonLabel = source != null ? new GUIStyle(source) : new GUIStyle();
        buttonLabel.fontSize = Mathf.Max(10, Mathf.RoundToInt(16f * UiScale));
        buttonLabel.fontStyle = FontStyle.Bold;
        buttonLabel.alignment = TextAnchor.MiddleCenter;
        buttonLabel.normal.textColor = Color.white;
        buttonLabel.hover.textColor = Color.white;
        buttonLabel.active.textColor = Color.white;
        buttonLabel.focused.textColor = Color.white;
        buttonLabel.wordWrap = false;
        buttonLabel.richText = false;
        buttonLabel.clipping = TextClipping.Clip;
        buttonLabel.padding = new RectOffset(0, 0, 0, 0);
        buttonLabel.margin = new RectOffset(0, 0, 0, 0);
        buttonLabel.border = new RectOffset(0, 0, 0, 0);
        drawStyleKey = key;
    }

    private static Texture2D BuildCircleTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(center - distance + 1f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }
}
