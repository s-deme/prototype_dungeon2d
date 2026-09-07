using UnityEngine;

namespace LanternDepths.Presentation
{
    public sealed class PlayerInputAdapter
    {
        private readonly System.Func<KeyCode, bool> keyDown;
        private readonly System.Func<KeyCode, bool> keyHeld;
        private readonly System.Func<string, float> axis;
        private Vector2Int previousStick;
        public System.Func<KeyCode, KeyCode> Map { get; set; } = key => key;
        public bool InspectPressed => keyDown(KeyCode.Tab) || keyDown(KeyCode.JoystickButton9);
        public PlayerInputAdapter() : this(Input.GetKeyDown, Input.GetKey, Input.GetAxisRaw) { }
        public PlayerInputAdapter(System.Func<KeyCode, bool> keyDown, System.Func<KeyCode, bool> keyHeld, System.Func<string, float> axis)
        { this.keyDown = key => keyDown(Map(key)); this.keyHeld = key => keyHeld(Map(key)); this.axis = axis; }
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
            { command = new PlayerCommand(CommandKind.Descend); return true; }
            foreach (var d in MovementRules.Directions)
            {
                int number = (d.Y + 1) * 3 + d.X + 2;
                if (keyDown((KeyCode)((int)KeyCode.Keypad0 + number)))
                { command = new PlayerCommand(CommandKind.Move, d); return true; }
            }
            bool pressed = keyDown(KeyCode.UpArrow) || keyDown(KeyCode.DownArrow) || keyDown(KeyCode.LeftArrow) || keyDown(KeyCode.RightArrow) ||
                keyDown(KeyCode.W) || keyDown(KeyCode.A) || keyDown(KeyCode.S) || keyDown(KeyCode.D) ||
                keyDown(KeyCode.Q) || keyDown(KeyCode.E) || keyDown(KeyCode.Z) || keyDown(KeyCode.C);
            int x = 0, y = 0;
            if (pressed)
            {
                x = (Held(KeyCode.RightArrow, KeyCode.D) ? 1 : 0) - (Held(KeyCode.LeftArrow, KeyCode.A) ? 1 : 0);
                y = (Held(KeyCode.UpArrow, KeyCode.W) ? 1 : 0) - (Held(KeyCode.DownArrow, KeyCode.S) ? 1 : 0);
                if (keyDown(KeyCode.Q)) { x = -1; y = 1; }
                if (keyDown(KeyCode.E)) { x = 1; y = 1; }
                if (keyDown(KeyCode.Z)) { x = -1; y = -1; }
                if (keyDown(KeyCode.C)) { x = 1; y = -1; }
            }
            else if (stickChanged) { x = stick.x; y = stick.y; }
            if (x == 0 && y == 0) return false;
            command = new PlayerCommand(CommandKind.Move, new GridPosition(x, y)); return true;
        }
        private bool Held(KeyCode a, KeyCode b) => keyHeld(a) || keyHeld(b);
        private static int Quantize(float value) => value > 0.55f ? 1 : value < -0.55f ? -1 : 0;
    }
}
