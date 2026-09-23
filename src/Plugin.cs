using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MyValheimMod.Example;
using MyValheimMod.Input;
using MyValheimMod.Ui;

namespace MyValheimMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class MyValheimModPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.yourname.myvalheimmod";
        public const string PluginName = "MyValheimMod";

        /// <summary>Keep equal to the csproj's Version and thunderstore/manifest.json (scripts/zip.sh checks).</summary>
        public const string PluginVersion = "0.1.0";

        internal static MyValheimModPlugin Instance;
        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> ModEnabled;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            ModEnabled = Config.Bind(
                "General",
                "Enabled",
                true,
                "Master switch. Turn off to neutralise the mod without uninstalling it.");

            ExampleWindow.Bind(Config);

#if DEBUG
            Dev.AutoTest.Init();
#endif

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        /// <summary>
        /// One tick for the whole mod. The pad goes first: it decides which buttons the game must
        /// not see this frame, before any feature reads them.
        /// </summary>
        private void Update()
        {
            GamePad.Tick();
            ExampleWindow.Tick();
        }

        /// <summary>
        /// Harmony patches outlive the plugin object, so an unpatch here keeps
        /// ScriptEngine hot-reloads from stacking duplicates.
        /// </summary>
        private void OnDestroy()
        {
            ExampleWindow.Destroy();
            UiTheme.Clear();
            _harmony?.UnpatchSelf();
            Log?.LogInfo($"{PluginName} unloaded.");
        }
    }
}
