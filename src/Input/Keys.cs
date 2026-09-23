using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyValheimMod.Input
{
    /// <summary>
    /// The mod's keyboard keys, read through the game's ZInput. Always through here, never
    /// <c>ZInput.GetKeyDown</c> directly: it throws <c>ArgumentOutOfRangeException</c> for a KeyCode it
    /// cannot map (<c>KeyCode.Question</c>: "key: None"), and in ValheimTomrer that throw silently
    /// killed the rest of the frame's tick. A key the player sets in the config can be any KeyCode.
    /// A key that throws once is dropped, with one warning.
    /// </summary>
    internal static class Keys
    {
        private static readonly HashSet<KeyCode> Bad = new HashSet<KeyCode>();

        /// <summary>The key went down this frame.</summary>
        public static bool Down(KeyCode key) => Read(key, true);

        /// <summary>The key is held.</summary>
        public static bool Held(KeyCode key) => Read(key, false);

        private static bool Read(KeyCode key, bool down)
        {
            if (key == KeyCode.None || Bad.Contains(key))
            {
                return false;
            }

            try
            {
                return down ? ZInput.GetKeyDown(key, false) : ZInput.GetKey(key, false);
            }
            catch (Exception e)
            {
                Bad.Add(key);
                MyValheimModPlugin.Log.LogWarning($"the game cannot read the key {key}, pick another one ({e.GetType().Name})");
                return false;
            }
        }
    }
}
