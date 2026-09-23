using HarmonyLib;

namespace MyValheimMod.Patches
{
    /// <summary>
    /// Smoke test. Proves the whole pipeline in one line of log output: the plugin loaded,
    /// Harmony patching works, and publicization exposed a private game member.
    ///
    /// FejdStartup.Awake runs on the main menu, so this fires about 13 s after launch without
    /// loading a world. Keep it: it is the cheapest check that a game update did not break loading.
    /// </summary>
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    internal static class FejdStartupAwakePatch
    {
        private static void Postfix()
        {
            if (!MyValheimModPlugin.ModEnabled.Value)
            {
                return;
            }

            // FejdStartup.m_instance is PRIVATE STATIC in assembly_valheim.
            // This line only compiles because Krafs.Publicizer publicized the reference.
            var publicizerWorks = FejdStartup.m_instance != null;

            MyValheimModPlugin.Log.LogInfo(
                $"main menu reached | harmony=OK | publicizer={(publicizerWorks ? "OK" : "returned null")}");
        }
    }
}
