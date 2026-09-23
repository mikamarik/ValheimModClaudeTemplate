#if DEBUG
using System.Collections;
using MyValheimMod.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using PadKey = UnityEngine.InputSystem.LowLevel.GamepadButton;

namespace MyValheimMod.Dev
{
    /// <summary>
    /// Real input for the tests, through Unity's input system, so the game's own ZInput and its UI
    /// module read it exactly like a person's. Never call a feature's method to fake a press: the
    /// press is what is under test.
    /// </summary>
    internal static partial class AutoTest
    {
        // ---------- frames and time ----------

        private static IEnumerator Frames(int count)
        {
            for (var frame = 0; frame < count; frame++)
            {
                yield return null;
            }
        }

        private static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        // ---------- keyboard ----------

        /// <summary>One press of a key: down for 4 frames, then up.</summary>
        private static IEnumerator PressKey(Key key)
        {
            return HoldKey(key, 0f);
        }

        /// <summary>The same, held down for a while, for keys that are read every frame.</summary>
        private static IEnumerator HoldKey(Key key, float seconds)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                Check(false, "no keyboard device to press " + key);
                yield break;
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return Frames(4);

            var until = Time.time + seconds;
            while (Time.time < until)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
                yield return null;
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Frames(2);
        }

        /// <summary>
        /// The input system's key for a KeyCode setting (the config uses KeyCode, the tests press
        /// Keys). Covers letters, digits and F-keys, which is what a mod's keys usually are.
        /// </summary>
        private static Key KeyOf(KeyCode code)
        {
            if (code >= KeyCode.A && code <= KeyCode.Z)
            {
                return Key.A + (code - KeyCode.A);
            }

            if (code >= KeyCode.Alpha0 && code <= KeyCode.Alpha9)
            {
                return code == KeyCode.Alpha0 ? Key.Digit0 : Key.Digit1 + (code - KeyCode.Alpha1);
            }

            if (code >= KeyCode.F1 && code <= KeyCode.F12)
            {
                return Key.F1 + (code - KeyCode.F1);
            }

            Check(false, "AutoTest.KeyOf does not know " + code + ", add it");
            return Key.None;
        }

        // ---------- mouse ----------

        /// <summary>A notch of the real wheel, with these keys held while it turns.</summary>
        private static IEnumerator WheelNotch(float direction, params Key[] held)
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null)
            {
                Check(false, "no mouse or keyboard device for the wheel");
                yield break;
            }

            if (held.Length > 0)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(held));
                yield return Frames(2);
            }

            InputSystem.QueueDeltaStateEvent(mouse.scroll, new Vector2(0f, direction));
            yield return Frames(4);

            if (held.Length > 0)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return Frames(2);
            }
        }

        private static IEnumerator ClickButton(UnityEngine.UI.Button button)
        {
            if (button == null)
            {
                Check(false, "no button to click");
                yield break;
            }

            yield return ClickScreen(ScreenBox((RectTransform)button.transform).center);
        }

        /// <summary>A left click at a screen point, through the mouse the game's UI module reads.</summary>
        private static IEnumerator ClickScreen(Vector2 at)
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                Check(false, "no mouse device to click with");
                yield break;
            }

            // WithButton changes the struct it is called on, so the press gets a state of its own.
            // Built from the "release" state, the release would still hold the button down.
            InputSystem.QueueStateEvent(mouse, new MouseState { position = at });
            yield return Frames(2);
            var down = new MouseState { position = at }.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(mouse, down);
            yield return Frames(2);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = at });
            yield return Frames(2);
        }

        // ---------- a pad the game reads ----------

        /// <summary>
        /// A controller of its own on the input system, so the game's ZInput and its UI module read
        /// its buttons exactly the way they read a real one's. Named like a DualSense, so
        /// <see cref="GamePad.PlayStation"/> calls it a PlayStation pad. Presses always go to this
        /// device, never through <c>InputSystem.FindControl</c>, which can pick a real pad that is
        /// plugged in.
        /// </summary>
        private static Gamepad _pad;

        private static ZInput.InputSource _padSource;

        private static void PadOn()
        {
            if (_pad != null)
            {
                return;
            }

            _padSource = ZInput.m_inputSource;
            _pad = InputSystem.AddDevice<Gamepad>("AutoTestPad DualSense");
            Log($"test pad added: '{_pad.name}', the game's modifier is {GamePad.ButtonOf(GamePad.Modifier)}, layout {ZInput.InputLayout}");
        }

        /// <summary>Lets go of every button, takes the pad away and gives the game back the input it had.</summary>
        private static IEnumerator PadOff()
        {
            if (_pad == null)
            {
                yield break;
            }

            PadDown(false);
            yield return Frames(2);
            InputSystem.RemoveDevice(_pad);
            _pad = null;
            ZInput.instance?.OnInput(_padSource, true);
            yield return null;
        }

        /// <summary>The pad's buttons from now on: the game's modifier (L2) when asked, these, nothing else.</summary>
        private static void PadDown(bool modifier, params PadButton[] buttons)
        {
            if (_pad == null)
            {
                Check(false, "PadDown before PadOn");
                return;
            }

            var state = new GamepadState();
            foreach (var button in buttons)
            {
                state = With(state, button);
            }

            if (modifier)
            {
                state = With(state, GamePad.ButtonOf(GamePad.Modifier) ?? PadButton.L2);
            }

            InputSystem.QueueStateEvent(_pad, state);
        }

        /// <summary>
        /// One press on the pad: the modifier first when asked, then the buttons for three frames
        /// (the D-pad's own repeat starts only after 0.3 s), then all of it let go.
        /// </summary>
        private static IEnumerator PadPress(bool modifier, params PadButton[] buttons)
        {
            PadOn();
            if (modifier)
            {
                PadDown(true);
                yield return Frames(2);
            }

            PadDown(modifier, buttons);
            yield return Frames(3);
            PadDown(modifier);
            yield return Frames(2);
            if (modifier)
            {
                PadDown(false);
                yield return Frames(2);
            }
        }

        /// <summary>Both sticks, -1..1 each way, until the next PadDown or PadSticks.</summary>
        private static void PadSticks(Vector2 left, Vector2 right)
        {
            PadOn();
            InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = left, rightStick = right });
        }

        private static GamepadState With(GamepadState state, PadButton button)
        {
            switch (button)
            {
                case PadButton.L2:
                    state.leftTrigger = 1f;
                    return state;
                case PadButton.R2:
                    state.rightTrigger = 1f;
                    return state;
                default:
                    return state.WithButton(PadKeyOf(button));
            }
        }

        private static PadKey PadKeyOf(PadButton button)
        {
            switch (button)
            {
                case PadButton.Cross: return PadKey.South;
                case PadButton.Circle: return PadKey.East;
                case PadButton.Square: return PadKey.West;
                case PadButton.Triangle: return PadKey.North;
                case PadButton.L1: return PadKey.LeftShoulder;
                case PadButton.R1: return PadKey.RightShoulder;
                case PadButton.Options: return PadKey.Start;
                case PadButton.L3: return PadKey.LeftStick;
                case PadButton.R3: return PadKey.RightStick;
                case PadButton.Up: return PadKey.DpadUp;
                case PadButton.Down: return PadKey.DpadDown;
                case PadButton.Left: return PadKey.DpadLeft;
                default: return PadKey.DpadRight;
            }
        }

        // ---------- a game button by its name ----------

        /// <summary>
        /// Presses whatever the game binds to this button name (the player may have changed it): a
        /// pad button goes to the test pad, a key or mouse button to the keyboard or mouse.
        /// </summary>
        private static IEnumerator PressBound(string gameButton)
        {
            var def = ZInput.instance != null ? ZInput.instance.GetButtonDef(gameButton) : null;
            var path = def != null && def.ButtonAction.bindings.Count > 0 ? def.ButtonAction.bindings[0].effectivePath : null;
            if (path != null && path.StartsWith("<Gamepad>"))
            {
                var button = GamePad.ButtonOf(gameButton);
                if (button == null)
                {
                    Check(false, $"a pad button is bound to {gameButton}: {path}");
                    yield break;
                }

                yield return PadPress(false, button.Value);
                Log($"pressed {gameButton} ({path}) on the test pad");
                yield break;
            }

            var control = path != null ? InputSystem.FindControl(path) as ButtonControl : null;
            if (control == null)
            {
                Check(false, $"a control is bound to {gameButton}: {path ?? "none"}");
                yield break;
            }

            QueueButton(control, 1f);
            yield return Frames(4);
            QueueButton(control, 0f);
            yield return Frames(3);
            Log($"pressed {gameButton} ({path})");
        }

        private static void QueueButton(ButtonControl control, float value)
        {
            using (StateEvent.From(control.device, out var eventPtr))
            {
                control.WriteValueIntoEvent(value, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }
    }
}
#endif
