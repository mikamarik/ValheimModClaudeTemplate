#if DEBUG
using System.Collections;
using System.Linq;
using MyValheimMod.Example;
using MyValheimMod.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MyValheimMod.Dev
{
    /// <summary>
    /// EXAMPLE FEATURE's tests: delete with src/Example. The shape to copy for a real feature:
    /// every input the feature has, keyboard, mouse and pad, pressed for real; what the game must
    /// NOT do with the same press (pause, jump, sheathe); a screenshot per look.
    /// </summary>
    internal static partial class AutoTest
    {
        private static IEnumerator TestExampleWindow(Player player)
        {
            Check(!ExampleWindow.IsOpen, "the example window is closed at the start");
            var key = KeyOf(ExampleWindow.Key.Value);

            // ---- keyboard ----
            yield return PressKey(key);
            Check(ExampleWindow.IsOpen, $"{ExampleWindow.Key.Value} opens the window");
            Check(TextInput.IsVisible() && !player.TakeInput(), "the game stops taking input while it is open");
            Check(ExampleWindow.HelloButton != null && OnScreen((RectTransform)ExampleWindow.HelloButton.transform), "its buttons are on screen");
            yield return Screenshot("example-1-keyboard");

            yield return PressKey(Key.Escape);
            yield return Frames(3);
            Check(!ExampleWindow.IsOpen, "Esc closes it");
            Check(!Menu.IsVisible(), "the same Esc did not open the pause menu");
            yield return Wait(0.3f);
            Check(player.TakeInput(), "the game takes input again after it closed");

            // ---- mouse ----
            yield return PressKey(key);
            var hellos = ExampleWindow.Hellos;
            yield return ClickButton(ExampleWindow.HelloButton);
            Check(ExampleWindow.Hellos == hellos + 1, $"a click on Say hello says hello once ({ExampleWindow.Hellos - hellos})");
            yield return ClickButton(ExampleWindow.CloseButton);
            Check(!ExampleWindow.IsOpen, "a click on Close closes it");

            // ---- pad: L2 + R1 opens, the D-pad walks, cross presses, circle closes ----
            PadOn();
            yield return Wait(0.3f);
            var r1 = GamePad.NamesOf(PadButton.R1);
            var seen = false;
            yield return Watching(PadPress(true, PadButton.R1), () => seen |= r1.Any(ZInput.GetButton));
            Check(ExampleWindow.IsOpen, "L2 + R1 opens it");
            Check(!seen, $"the game never saw the combo's R1 ({string.Join(", ", r1)})");
            yield return Frames(5);
            var events = EventSystem.current;
            Check(events.currentSelectedGameObject == ExampleWindow.HelloButton.gameObject,
                $"the pad starts on Say hello ({Selected(events)})");
            yield return Screenshot("example-2-pad");

            yield return PadPress(false, PadButton.Down);
            Check(events.currentSelectedGameObject == ExampleWindow.CloseButton.gameObject, $"D-pad down goes to Close ({Selected(events)})");
            yield return PadPress(false, PadButton.Up);
            Check(events.currentSelectedGameObject == ExampleWindow.HelloButton.gameObject, $"D-pad up goes back ({Selected(events)})");

            hellos = ExampleWindow.Hellos;
            yield return PadPress(false, PadButton.Cross);
            yield return Frames(2);
            Check(ExampleWindow.Hellos == hellos + 1 && ExampleWindow.IsOpen,
                $"cross presses Say hello once, the window stays open ({ExampleWindow.Hellos - hellos})");

            // Circle is the jump in the default layout and the build menu in the alternative ones:
            // whatever it is, the game must not see the circle that closed the window.
            var circle = GamePad.NamesOf(PadButton.Circle);
            seen = false;
            yield return Watching(PadPress(false, PadButton.Circle), () => seen |= circle.Any(ZInput.GetButton));
            Check(!ExampleWindow.IsOpen, "circle closes it");
            Check(!Menu.IsVisible(), "the same circle did not open the pause menu");
            Check(!seen, $"the game never saw the circle that closed it ({string.Join(", ", circle)})");

            // Nothing is held back once the window is gone: the game sees circle again, and the
            // jump (whatever the layout binds it to) jumps.
            seen = false;
            yield return Watching(PadPress(false, PadButton.Circle), () => seen |= circle.Any(ZInput.GetButton));
            Check(seen, "with the window closed, the game sees circle again");
            yield return Wait(0.5f);
            var rise = 0f;
            yield return PressBound("JoyJump");
            yield return Rise(player, 1.5f, r => rise = r);
            Check(rise > 0.5f, $"with the window closed, the pad's jump ({GamePad.ButtonOf("JoyJump")}) jumps (rose {rise:0.00} m)");

            // ---- no opening over a game menu ----
            InventoryGui.instance.Show(null);
            yield return Frames(3);
            yield return PressKey(key);
            yield return PadPress(true, PadButton.R1);
            Check(!ExampleWindow.IsOpen, "neither the key nor L2 + R1 opens it over the inventory");
            InventoryGui.instance.Hide();
            yield return Wait(0.5f);
        }

        /// <summary>
        /// Runs a step and calls <paramref name="look"/> on every frame of it. Nested steps run here
        /// too, or the look would only come between them.
        /// </summary>
        private static IEnumerator Watching(IEnumerator step, System.Action look)
        {
            var steps = new System.Collections.Generic.Stack<IEnumerator>();
            steps.Push(step);
            while (steps.Count > 0)
            {
                if (!steps.Peek().MoveNext())
                {
                    steps.Pop();
                    continue;
                }

                if (steps.Peek().Current is IEnumerator nested)
                {
                    steps.Push(nested);
                    continue;
                }

                yield return steps.Peek().Current;
                look();
            }
        }

        /// <summary>How high the player got over the next seconds, from where they stand now.</summary>
        private static IEnumerator Rise(Player player, float seconds, System.Action<float> result)
        {
            var start = player.transform.position.y;
            var top = start;
            var until = Time.time + seconds;
            while (Time.time < until)
            {
                top = Mathf.Max(top, player.transform.position.y);
                yield return null;
            }

            result(top - start);
        }

        private static string Selected(EventSystem events)
        {
            var selected = events != null ? events.currentSelectedGameObject : null;
            return selected != null ? selected.name : "nothing selected";
        }

        /// <summary>The README clip: open the window, say hello, close it. Cropped to the window.</summary>
        private static IEnumerator GifExample(Player player)
        {
            StartGif("example");
            yield return Wait(0.5f);
            yield return PressKey(KeyOf(ExampleWindow.Key.Value));
            GifCrop(ExampleWindow.Window);
            yield return Wait(0.8f);
            yield return ClickButton(ExampleWindow.HelloButton);
            yield return Wait(1.2f);
            yield return ClickButton(ExampleWindow.CloseButton);
            yield return Wait(1f);
        }
    }
}
#endif
