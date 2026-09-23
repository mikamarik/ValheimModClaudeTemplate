using HarmonyLib;
using MyValheimMod.Input;

namespace MyValheimMod.Patches
{
    /// <summary>
    /// Holds back from the game the pad buttons the mod's combos use (<see cref="GamePad.HoldWhile"/>).
    /// Every GetButton, GetButtonDown and GetButtonUp of the game comes through this one private
    /// method, in Update and in FixedUpdate alike, so the jump read in FixedUpdate is held back
    /// too. The keyboard does not come through here, it is never touched.
    ///
    /// The private method, not the public wrappers: those are small enough to be inlined.
    /// </summary>
    [HarmonyPatch(typeof(ZInput), "TryGetButtonState")]
    internal static class ZInputTryGetButtonStatePatch
    {
        private static bool Prefix(string name, ref bool __result)
        {
            if (!GamePad.HeldBack(name))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
