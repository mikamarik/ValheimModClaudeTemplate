using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MyValheimMod.Ui
{
    /// <summary>
    /// The one flag every input patch reads (<c>Patches/InputBlockPatches.cs</c>). A window of the
    /// mod sets <see cref="Open"/> while it shows and calls <see cref="MarkClosed"/> when it hides.
    /// Blocking stays true for one extra frame after closing, so the Esc that closed the window
    /// does not also open the pause menu.
    /// </summary>
    internal static class ModUi
    {
        private static int _closedFrame = -10;
        private static int _typedFrame = -10;
        private static int _escapeFrame = -10;

        public static bool Open;

        /// <summary>Hide and lock the cursor while a window is open (a 3D view that has the mouse).</summary>
        public static bool LockCursor;

        public static bool Blocking => Open || Time.frameCount - _closedFrame <= 1;

        /// <summary>
        /// True on the frame the mod used Esc for something of its own with no window open (it
        /// stopped a mode, closed a popup). The pause menu skips that frame, or the same Esc
        /// would pause the game too.
        /// </summary>
        public static bool TakesEscape => Time.frameCount - _escapeFrame <= 1;

        /// <summary>Call on the frame the mod takes an Esc that the pause menu must not see.</summary>
        public static void TakeEscape()
        {
            _escapeFrame = Time.frameCount;
        }

        /// <summary>
        /// A text box has the keyboard, so the mod's keys and Esc are not the mod's this frame.
        /// The box stays selected after Esc deactivates it, so the focus flag is what has to be
        /// read, not the selection.
        /// </summary>
        public static bool Typing
        {
            get
            {
                var system = EventSystem.current;
                var selected = system != null ? system.currentSelectedGameObject : null;
                var field = selected != null ? selected.GetComponent<TMP_InputField>() : null;
                return field != null && field.isFocused;
            }
        }

        /// <summary>
        /// A text box has the keyboard now, or had it last frame. The box reads its keys in the
        /// game's UI update, which may run before the mod's in the same frame: Esc there stops the
        /// typing first, and the mod must still take that Esc as the box's, not as "close the
        /// window". Read it once a frame, or the last frame is not known.
        /// </summary>
        public static bool JustTyping
        {
            get
            {
                if (Typing)
                {
                    _typedFrame = Time.frameCount;
                    return true;
                }

                return Time.frameCount - _typedFrame <= 1;
            }
        }

        /// <summary>The box that has the keyboard lets go of it and is no longer selected. Its text stays.</summary>
        public static void StopTyping()
        {
            var system = EventSystem.current;
            var selected = system != null ? system.currentSelectedGameObject : null;
            var field = selected != null ? selected.GetComponent<TMP_InputField>() : null;
            if (field == null)
            {
                return;
            }

            field.DeactivateInputField();
            system.SetSelectedGameObject(null);
        }

        public static void MarkClosed()
        {
            Open = false;
            LockCursor = false;
            _closedFrame = Time.frameCount;
        }

        /// <summary>
        /// The player is in the world and free to act: alive, not teleporting or in a cutscene, and
        /// no game menu (inventory, build menu, map, radial, trader, pause, a popup), chat, console,
        /// text entry or window of ours is up. Where a key or pad combo of the mod may open things.
        /// </summary>
        public static bool WorldFree
        {
            get
            {
                var player = Player.m_localPlayer;
                if (player == null || player.IsDead() || player.IsTeleporting() || player.InCutscene() || Blocking)
                {
                    return false;
                }

                if (InventoryGui.IsVisible() || Hud.IsPieceSelectionVisible() || Hud.InRadial()
                    || Minimap.IsOpen() || StoreGui.IsVisible() || Menu.IsVisible() || TextInput.IsVisible()
                    || Console.IsVisible())
                {
                    return false;
                }

                return Chat.instance == null || !Chat.instance.HasFocus();
            }
        }
    }
}
