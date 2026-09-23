using HarmonyLib;
using MyValheimMod.Ui;

namespace MyValheimMod.Patches
{
    /// <summary>
    /// The game fires "input layout changed" every time the player goes from the keyboard or mouse
    /// to the pad and back, and the build menu, alive behind any window, answers by clearing the
    /// UI's selection. In a window of the mod that selection can be a text box that is typing: it
    /// lost the keyboard on the first pad press (ValheimTomrer's Save as). While a window is up
    /// the selection is left alone; the menu's favourites list still closes.
    /// </summary>
    [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.OnLayoutChanged))]
    internal static class BuildUiOnLayoutChangedPatch
    {
        private static bool Prefix(BuildUi __instance)
        {
            if (!ModUi.Open)
            {
                return true;
            }

            if (__instance.m_favoritesDropdown != null)
            {
                __instance.m_favoritesDropdown.Close();
            }

            return false;
        }
    }
}
