using System;
using System.Collections.Generic;
using MyValheimMod.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MyValheimMod.Input
{
    /// <summary>Buttons named the way a PlayStation pad shows them. The game's names are in <see cref="GamePad"/>.</summary>
    internal enum PadButton
    {
        Cross,
        Circle,
        Square,
        Triangle,
        L1,
        R1,
        L2,
        R2,
        Options,
        L3,
        R3,
        Up,
        Down,
        Left,
        Right,
    }

    /// <summary>
    /// The controller in the world, read through the game's own button names (ZInput), so the
    /// game's layout decides which button is which: "the modifier" is the game's JoyAltKeys, L2 in
    /// the default layout and L1 in the alternative one.
    ///
    /// It also holds buttons back from the game (<c>Patches/ZInputTryGetButtonStatePatch.cs</c>),
    /// so a press the mod uses does not do something else as well. A feature registers what it
    /// needs once (<see cref="HoldWhile"/>): "while L2 is held, the game must not see R1". A button
    /// held back stays held back until it is let go, so the rest of that press (the jump read in
    /// the next FixedUpdate) never reaches the game either.
    ///
    /// The mod's own reads (<see cref="Pressed"/>, <see cref="Held"/>) go to the game's button
    /// objects directly, never through the held-back path, so they always see the press.
    /// </summary>
    internal static class GamePad
    {
        /// <summary>The game's modifier: L2 in the default layout, L1 in the alternative one.</summary>
        public const string Modifier = "JoyAltKeys";

        public const string Cross = "JoyButtonA";
        public const string Circle = "JoyButtonB";
        public const string Square = "JoyButtonX";
        public const string Triangle = "JoyButtonY";
        public const string L1 = "JoyLBumper";
        public const string R1 = "JoyRBumper";
        public const string L3 = "JoyLStick";
        public const string R3 = "JoyRStick";
        public const string DpadLeft = "JoyDPadLeft";
        public const string DpadRight = "JoyDPadRight";
        public const string DpadUp = "JoyDPadUp";
        public const string DpadDown = "JoyDPadDown";

        private sealed class Rule
        {
            public Func<bool> When;
            public int Mask;
        }

        private static readonly List<Rule> Rules = new List<Rule>();

        /// <summary>Every game button bound to a pad button, whatever its name, by its binding path.</summary>
        private static readonly Dictionary<string, PadButton> Groups = new Dictionary<string, PadButton>();

        private static readonly Dictionary<string, PadButton> Paths = new Dictionary<string, PadButton>
        {
            { "<Gamepad>/buttonSouth", PadButton.Cross },
            { "<Gamepad>/buttonEast", PadButton.Circle },
            { "<Gamepad>/buttonWest", PadButton.Square },
            { "<Gamepad>/buttonNorth", PadButton.Triangle },
            { "<Gamepad>/leftShoulder", PadButton.L1 },
            { "<Gamepad>/rightShoulder", PadButton.R1 },
            { "<Gamepad>/leftTrigger", PadButton.L2 },
            { "<Gamepad>/rightTrigger", PadButton.R2 },
            { "<Gamepad>/start", PadButton.Options },
            { "<Gamepad>/leftStickPress", PadButton.L3 },
            { "<Gamepad>/rightStickPress", PadButton.R3 },
            { "<Gamepad>/dpad/up", PadButton.Up },
            { "<Gamepad>/dpad/down", PadButton.Down },
            { "<Gamepad>/dpad/left", PadButton.Left },
            { "<Gamepad>/dpad/right", PadButton.Right },
        };

        private static ZInput _builtFor;
        private static InputLayout _builtLayout;
        private static int _builtCount = -1;

        private static int _wantedFrame = -1;
        private static int _wanted;
        private static int _latched;

        /// <summary>
        /// True where the mod's world combos may work: the mod on and the player free to act
        /// (<see cref="ModUi.WorldFree"/>: no game menu, chat, console or window of ours up).
        /// </summary>
        public static bool Live
        {
            get
            {
                var enabled = MyValheimModPlugin.ModEnabled;
                return enabled != null && enabled.Value && ModUi.WorldFree;
            }
        }

        /// <summary>The game's modifier is held.</summary>
        public static bool ModifierHeld => Held(Modifier);

        /// <summary>The pad in hand is a PlayStation one (for the wording and icons of a hint).</summary>
        public static bool PlayStation
        {
            get
            {
                var pad = Gamepad.current;
                return pad != null && (Mentions(pad.name) || Mentions(pad.layout) || Mentions(pad.displayName));
            }
        }

        /// <summary>
        /// While <paramref name="when"/> is true, every game button bound to these pad buttons reads
        /// as not pressed, and stays so until the button is let go. Register once, at load.
        /// </summary>
        public static void HoldWhile(Func<bool> when, params PadButton[] buttons)
        {
            var mask = 0;
            foreach (var button in buttons)
            {
                mask |= Bit(button);
            }

            Rules.Add(new Rule { When = when, Mask = mask });
        }

        /// <summary>The game button went down this frame (the D-pad's own repeat included).</summary>
        public static bool Pressed(string name)
        {
            var def = Def(name);
            return def != null && def.Pressed;
        }

        public static bool Held(string name)
        {
            var def = Def(name);
            return def != null && def.Held;
        }

        /// <summary>The pad button the game binds to a name, or null.</summary>
        public static PadButton? ButtonOf(string gameButton)
        {
            var path = PathOf(Def(gameButton));
            return path != null && Paths.TryGetValue(path, out var button) ? button : (PadButton?)null;
        }

        /// <summary>Every game button name bound to this pad button in the current layout.</summary>
        public static List<string> NamesOf(PadButton button)
        {
            BuildGroups();
            var names = new List<string>();
            foreach (var pair in Groups)
            {
                if (pair.Value == button)
                {
                    names.Add(pair.Key);
                }
            }

            names.Sort(StringComparer.Ordinal);
            return names;
        }

        /// <summary>
        /// Once a frame, before anything reads the combos (Plugin.Update calls it first): the button
        /// list (again after a layout change), and which held-back buttons are still down.
        /// </summary>
        public static void Tick()
        {
            BuildGroups();
            _latched = Wanted() | (_latched & Down());
        }

        /// <summary>
        /// True when the game must not see this button now. Called for every button the game reads,
        /// many times a frame, so the common case is one dictionary miss.
        /// </summary>
        public static bool HeldBack(string name)
        {
            if (name == null || !Groups.TryGetValue(name, out var button))
            {
                return false;
            }

            return ((Wanted() | _latched) & Bit(button)) != 0;
        }

        /// <summary>What the rules hold back this frame, worked out once a frame.</summary>
        private static int Wanted()
        {
            if (_wantedFrame == Time.frameCount)
            {
                return _wanted;
            }

            _wantedFrame = Time.frameCount;
            _wanted = 0;
            var enabled = MyValheimModPlugin.ModEnabled;
            if (enabled == null || !enabled.Value)
            {
                return 0;
            }

            foreach (var rule in Rules)
            {
                try
                {
                    if (rule.When())
                    {
                        _wanted |= rule.Mask;
                    }
                }
                catch (Exception e)
                {
                    MyValheimModPlugin.Log.LogError("a pad hold-back rule threw: " + e);
                }
            }

            return _wanted;
        }

        /// <summary>The pad buttons physically down on any pad, whatever the game's own state says.</summary>
        private static int Down()
        {
            var down = 0;
            foreach (var pad in Gamepad.all)
            {
                foreach (PadButton button in Enum.GetValues(typeof(PadButton)))
                {
                    var control = Control(pad, button);
                    if (control != null && control.ReadValue() > 0.3f)
                    {
                        down |= Bit(button);
                    }
                }
            }

            return down;
        }

        private static int Bit(PadButton button) => 1 << (int)button;

        private static ZInput.ButtonDef Def(string name)
        {
            var input = ZInput.instance;
            return input != null ? input.GetButtonDef(name) : null;
        }

        private static string PathOf(ZInput.ButtonDef def)
        {
            if (def == null || def.ButtonAction == null || def.ButtonAction.bindings.Count == 0)
            {
                return null;
            }

            return def.GetActionPath();
        }

        /// <summary>
        /// Which game buttons sit on which pad button, by their binding, so every name the game reads
        /// a button by is held back (circle is JoyButtonB, JoyJump, JoyDodge and JoyRadialClose in the
        /// default layout). Built again when the game's layout or its button list changes.
        /// </summary>
        private static void BuildGroups()
        {
            var input = ZInput.instance;
            var count = input != null && input.m_buttons != null ? input.m_buttons.Count : -1;
            if (input == _builtFor && ZInput.InputLayout == _builtLayout && count == _builtCount)
            {
                return;
            }

            _builtFor = input;
            _builtLayout = ZInput.InputLayout;
            _builtCount = count;
            Groups.Clear();
            if (input == null || input.m_buttons == null)
            {
                return;
            }

            foreach (var pair in input.m_buttons)
            {
                var def = pair.Value;
                if (def == null || def.Source != ZInput.InputSource.Gamepad)
                {
                    continue;
                }

                var path = PathOf(def);
                if (path != null && Paths.TryGetValue(path, out var button))
                {
                    Groups[pair.Key] = button;
                }
            }
        }

        private static ButtonControl Control(Gamepad pad, PadButton button)
        {
            switch (button)
            {
                case PadButton.Cross: return pad.buttonSouth;
                case PadButton.Circle: return pad.buttonEast;
                case PadButton.Square: return pad.buttonWest;
                case PadButton.Triangle: return pad.buttonNorth;
                case PadButton.L1: return pad.leftShoulder;
                case PadButton.R1: return pad.rightShoulder;
                case PadButton.L2: return pad.leftTrigger;
                case PadButton.R2: return pad.rightTrigger;
                case PadButton.Options: return pad.startButton;
                case PadButton.L3: return pad.leftStickButton;
                case PadButton.R3: return pad.rightStickButton;
                case PadButton.Up: return pad.dpad.up;
                case PadButton.Down: return pad.dpad.down;
                case PadButton.Left: return pad.dpad.left;
                case PadButton.Right: return pad.dpad.right;
                default: return null;
            }
        }

        // Sony's USB vendor id is 054c. Not "Wireless Controller": the Xbox pad is called that too.
        private static bool Mentions(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            text = text.ToLowerInvariant();
            return text.Contains("dualsense") || text.Contains("dualshock")
                || text.Contains("playstation") || text.Contains("054c");
        }
    }
}
