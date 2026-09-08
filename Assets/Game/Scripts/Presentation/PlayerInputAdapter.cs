using UnityEngine;

namespace LanternDepths.Presentation
{
    public sealed class PlayerInputAdapter
    {
        private readonly System.Func<KeyCode, bool> keyDown;
        private readonly System.Func<KeyCode, bool> keyHeld;
        private readonly System.Func<string, float> axis;
        private readonly System.Func<float> clock;
        private Vector2Int previousStick;
        private GridPosition repeatedDirection;
        private float nextMoveAt;
        private const float InitialRepeatDelay = 0.25f;
        private const float RepeatInterval = 0.10f;
        public System.Func<KeyCode, KeyCode> Map { get; set; } = key => key;
        public bool InspectPressed => keyDown(KeyCode.Tab) || keyDown(KeyCode.JoystickButton9);
        public PlayerInputAdapter() : this(Input.GetKeyDown, Input.GetKey, Input.GetAxisRaw, () => Time.unscaledTime) { }
        public PlayerInputAdapter(System.Func<KeyCode, bool> keyDown, System.Func<KeyCode, bool> keyHeld, System.Func<string, float> axis, System.Func<float> clock = null)
        { this.keyDown = key => keyDown(Map(key)); this.keyHeld = key => keyHeld(Map(key)); this.axis = axis; this.clock = clock ?? (() => Time.unscaledTime); }
        public bool InventoryPressed => keyDown(KeyCode.I) || keyDown(KeyCode.JoystickButton3);
        public bool ClosePressed => keyDown(KeyCode.Escape) || keyDown(KeyCode.JoystickButton1);
        public bool MenuPressed => keyDown(KeyCode.Escape) || keyDown(KeyCode.JoystickButton7);
        public bool ConfirmPressed => keyDown(KeyCode.Return) || keyDown(KeyCode.KeypadEnter) || keyDown(KeyCode.JoystickButton0);
        public bool RestartPressed => keyDown(KeyCode.R) || keyDown(KeyCode.JoystickButton7);
        public bool UsePressed => keyDown(KeyCode.U) || keyDown(KeyCode.JoystickButton0);
        public bool EquipPressed => keyDown(KeyCode.E) || keyDown(KeyCode.JoystickButton2);
        public bool DropPressed => keyDown(KeyCode.D) || keyDown(KeyCode.JoystickButton5);
        public bool AnimationPressed => keyDown(KeyCode.F) || keyDown(KeyCode.JoystickButton6);
        public int SelectionDelta { get; private set; }
        public bool TryRead(bool blocked, out PlayerCommand command)
        {
            command = default;
            var stick = new Vector2Int(Quantize(axis("GamepadHorizontal")), -Quantize(axis("GamepadVertical")));
            var pad = new Vector2Int(Quantize(axis("DpadHorizontal")), Quantize(axis("DpadVertical")));
            if (pad != Vector2Int.zero) stick = pad;
            bool stickChanged = stick != Vector2Int.zero && previousStick == Vector2Int.zero; previousStick = stick;
            SelectionDelta = keyDown(KeyCode.DownArrow) ? 1 : keyDown(KeyCode.UpArrow) ? -1 :
                stickChanged && stick.y != 0 ? -stick.y : 0;
            if (blocked) return false;
            if (keyDown(KeyCode.Space) || keyDown(KeyCode.Keypad5) || keyDown(KeyCode.JoystickButton0))
            { command = new PlayerCommand(CommandKind.Wait); return true; }
            if (keyDown(KeyCode.G) || keyDown(KeyCode.JoystickButton2))
            { command = new PlayerCommand(CommandKind.PickUp); return true; }
            if (keyDown(KeyCode.Return) || keyDown(KeyCode.KeypadEnter) || keyDown(KeyCode.JoystickButton4))
            { command = new PlayerCommand(CommandKind.Context); return true; }
            var keyboard = KeyboardDirection();
            if (keyboard != default) return TryRepeat(keyboard, KeyboardPressed(), out command);
            if (stick != Vector2Int.zero) return TryRepeat(new GridPosition(stick.x, stick.y), stickChanged, out command);
            repeatedDirection = default; nextMoveAt = 0; return false;
        }
        private bool TryRepeat(GridPosition direction, bool initial, out PlayerCommand command)
        {
            command = default;
            if (initial)
            {
                repeatedDirection = direction;
                nextMoveAt = clock() + InitialRepeatDelay;
                command = new PlayerCommand(CommandKind.Move, direction); return true;
            }
            if (direction != repeatedDirection)
            {
                repeatedDirection = direction;
                nextMoveAt = clock() + InitialRepeatDelay;
                return false;
            }
            if (clock() < nextMoveAt) return false;
            nextMoveAt = clock() + RepeatInterval;
            command = new PlayerCommand(CommandKind.Move, direction); return true;
        }
        private GridPosition KeyboardDirection()
        {
            if (keyHeld(KeyCode.Q)) return new GridPosition(-1, 1);
            if (keyHeld(KeyCode.E)) return new GridPosition(1, 1);
            if (keyHeld(KeyCode.Z)) return new GridPosition(-1, -1);
            if (keyHeld(KeyCode.C)) return new GridPosition(1, -1);
            int x = (Held(KeyCode.RightArrow, KeyCode.D) ? 1 : 0) - (Held(KeyCode.LeftArrow, KeyCode.A) ? 1 : 0);
            int y = (Held(KeyCode.UpArrow, KeyCode.W) ? 1 : 0) - (Held(KeyCode.DownArrow, KeyCode.S) ? 1 : 0);
            if (x != 0 || y != 0) return new GridPosition(x, y);
            foreach (var direction in MovementRules.Directions)
            {
                if (keyHeld(DirectionKey(direction))) return direction;
            }
            return default;
        }
        private bool KeyboardPressed()
        {
            if (keyDown(KeyCode.UpArrow) || keyDown(KeyCode.DownArrow) || keyDown(KeyCode.LeftArrow) || keyDown(KeyCode.RightArrow) ||
                keyDown(KeyCode.W) || keyDown(KeyCode.A) || keyDown(KeyCode.S) || keyDown(KeyCode.D) ||
                keyDown(KeyCode.Q) || keyDown(KeyCode.E) || keyDown(KeyCode.Z) || keyDown(KeyCode.C)) return true;
            foreach (var direction in MovementRules.Directions)
            {
                if (keyDown(DirectionKey(direction))) return true;
            }
            return false;
        }
        private static KeyCode DirectionKey(GridPosition direction) =>
            (KeyCode)((int)KeyCode.Keypad0 + ((direction.Y + 1) * 3 + direction.X + 2));
        private bool Held(KeyCode a, KeyCode b) => keyHeld(a) || keyHeld(b);
        private static int Quantize(float value) => value > 0.55f ? 1 : value < -0.55f ? -1 : 0;
    }
}
