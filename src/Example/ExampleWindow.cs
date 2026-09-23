using BepInEx.Configuration;
using MyValheimMod.Input;
using MyValheimMod.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyValheimMod.Example
{
    /// <summary>
    /// EXAMPLE FEATURE. It shows the whole pattern once: a config key and its pad twin, a window
    /// of our own built from the game's font and sprites, the input takeover, Esc and circle that
    /// close without pausing or jumping, and the game's own UI module driving the pad inside the
    /// window. Its test is <c>Dev/AutoTest.Example.cs</c>.
    ///
    /// Delete this folder, that test, the <c>Example</c> lines in Plugin.cs and the README rows
    /// when the first real feature lands.
    ///
    /// Keyboard and mouse: F9 opens and closes, Esc closes, click the buttons.
    /// Pad: L2 + R1 (the game's modifier + the right shoulder) opens and closes, the D-pad moves
    /// between the buttons, cross presses, circle closes.
    /// </summary>
    internal static class ExampleWindow
    {
        internal static ConfigEntry<KeyCode> Key;

        private static GameObject _root;

        /// <summary>The two buttons, for the test. Null until the window was built in this world.</summary>
        internal static Button HelloButton { get; private set; }

        internal static Button CloseButton { get; private set; }

        /// <summary>The window's panel, for the README clip's crop.</summary>
        internal static RectTransform Window { get; private set; }

        /// <summary>How many times "Say hello" was pressed since load, for the test.</summary>
        internal static int Hellos { get; private set; }

        public static bool IsOpen => ModUi.Open && _root != null && _root.activeSelf;

        public static void Bind(ConfigFile config)
        {
            Key = config.Bind(
                "Example",
                "Key",
                KeyCode.F9,
                "Opens and closes the example window. On a pad: L2 + R1 (the game's modifier and the right shoulder button).");

            // L2 + R1 opens the window. The game gives L2 + R1 no meaning in the default layout or in
            // Alternative2 (probe.txt), but R1 alone is the secondary attack, the hammer's Remove or
            // Place: with the modifier held, the game must not see it. (L2 + R3 was the first pick;
            // in Alternative2 it is the hammer's alt place.)
            GamePad.HoldWhile(() => GamePad.Live && GamePad.ModifierHeld, PadButton.R1);

            // Circle closes the window. The game reads the jump in the next FixedUpdate, when the
            // window no longer blocks it, so circle stays held back from the game until let go.
            GamePad.HoldWhile(() => IsOpen, PadButton.Circle);
        }

        /// <summary>Once a frame, from Plugin.Update.</summary>
        public static void Tick()
        {
            if (!MyValheimModPlugin.ModEnabled.Value)
            {
                if (IsOpen)
                {
                    Close();
                }

                return;
            }

            if (IsOpen)
            {
                TickOpen();
                return;
            }

            if (!ModUi.WorldFree)
            {
                return;
            }

            if (Keys.Down(Key.Value) || PadCombo())
            {
                Open();
            }
        }

        private static void TickOpen()
        {
            var player = Player.m_localPlayer;
            if (player == null || player.IsDead() || player.IsTeleporting() || player.InCutscene())
            {
                Close();
                return;
            }

            // A text box that has the keyboard owns Esc and the letters this frame.
            if (ModUi.JustTyping)
            {
                return;
            }

            if (Keys.Down(KeyCode.Escape) || Keys.Down(Key.Value) || GamePad.Pressed(GamePad.Circle) || PadCombo())
            {
                Close();
            }
        }

        /// <summary>The game's modifier held, then R1. Read through the game's button names.</summary>
        private static bool PadCombo() => GamePad.ModifierHeld && GamePad.Pressed(GamePad.R1);

        public static void Open()
        {
            if (!UiTheme.Ensure())
            {
                return;
            }

            // The canvas dies with the world: Unity's null says so.
            if (_root == null && !Build())
            {
                return;
            }

            _root.SetActive(true);
            ModUi.Open = true;

            // The game's UIGroupHandler selects the default button once the pad is in use. Nothing
            // is selected for the mouse, so no button looks pressed.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(ZInput.IsGamepadActive() ? HelloButton.gameObject : null);
            }

            MyValheimModPlugin.Log.LogInfo("example window opened");
        }

        public static void Close()
        {
            if (_root != null)
            {
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
                    && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(_root.transform))
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }

                _root.SetActive(false);
            }

            if (ModUi.Open)
            {
                ModUi.MarkClosed();
                PlayerController.SetTakeInputDelay(0.2f);
                MyValheimModPlugin.Log.LogInfo("example window closed");
            }
        }

        public static void Destroy()
        {
            Close();
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            _root = null;
            HelloButton = CloseButton = null;
            Window = null;
        }

        private static bool Build()
        {
            var parent = Hud.instance != null ? Hud.instance.transform.parent : null;
            if (parent == null)
            {
                return false;
            }

            _root = UiBuild.Canvas("MyValheimModExample", parent);
            var root = (RectTransform)_root.transform;

            // Dims the world and eats clicks that miss the window.
            UiBuild.Stretch(UiBuild.Panel("Backdrop", root, null, UiTheme.Backdrop).rectTransform);

            var window = Window = UiBuild.Panel("Window", root, UiTheme.Panel).rectTransform;
            window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(520f, 290f);

            // Children take the column's whole width: the labels wrap and centre, the buttons fill it.
            var column = UiBuild.Stretch(UiBuild.Column("Column", window, 14f, 32), 0f, 0f, 0f, 0f);
            column.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
            UiBuild.Label("Title", column, "Example window", 26f, TextAlignmentOptions.Center, UiTheme.Accent);
            UiBuild.Label(
                "Text",
                column,
                "Made at runtime from the game's own font and sprites.",
                18f,
                TextAlignmentOptions.Center);

            HelloButton = UiBuild.Button("Hello", column, "Say hello", SayHello);
            CloseButton = UiBuild.Button("Close", column, "Close", Close);
            UiBuild.LinkColumn(new Selectable[] { HelloButton, CloseButton });

            _root.GetComponent<UIGroupHandler>().m_defaultElement = HelloButton.gameObject;
            return true;
        }

        private static void SayHello()
        {
            Hellos++;
            // Local only. Never MessageHud.MessageAll or Chat.SendText: those go over the network.
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, $"Hello from {MyValheimModPlugin.PluginName} ({Hellos})");
        }
    }
}
