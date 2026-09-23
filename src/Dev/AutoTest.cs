#if DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using MyValheimMod.Example;
using UnityEngine;

namespace MyValheimMod.Dev
{
    /// <summary>
    /// Debug builds only. Plays a scripted session so features can be checked without a person:
    /// a throwaway character and world in their own save folder, a scenario, PASS/FAIL lines in
    /// the log, screenshots, then quit. Off unless AUTOTEST is set and AUTOTEST_MOD names this
    /// plugin (scripts/autotest.sh sets both), so two mods made from this template never both run.
    ///
    /// One partial file per area, never one giant file:
    /// <list type="bullet">
    /// <item>AutoTest.cs: the runner, the scenario list, the chain, the reset.</item>
    /// <item>AutoTest.Input.cs: keys, the wheel, mouse clicks, a made-up pad the game reads.</item>
    /// <item>AutoTest.World.cs: the flat build spot, the hammer, screenshots, small helpers.</item>
    /// <item>AutoTest.Probe.cs: "probe", measures the game into probe.txt.</item>
    /// <item>AutoTest.Gifs.cs: records README GIF frames.</item>
    /// <item>AutoTest.&lt;Feature&gt;.cs: one per feature, its scenarios (AutoTest.Example.cs).</item>
    /// </list>
    /// </summary>
    internal static partial class AutoTest
    {
        private const string TestName = "autotest";
        private const string WorldSeed = "AUTOTST1";

        /// <summary>
        /// Every scenario. "all" runs those marked InAll, in this order, in one game. Add a line
        /// here and in scripts/autotest.sh's header when you add a scenario.
        /// </summary>
        private static readonly Scenario[] Scenarios =
        {
            new Scenario("probe", Probe, true, "measures the game into .devtest/probe.txt: version, plugins, layers, every button binding, UI assets"),
            new Scenario("build_spot", TestBuildSpot, true, "finds and levels the flat build spot, equips the hammer"),
            new Scenario("example_window", TestExampleWindow, true, "the example window with the keyboard, the mouse and the pad"),
            new Scenario("readme_gifs", RecordReadmeGifs, false, "no test: records README GIF frames into .devtest/gifs/"),
        };

        private sealed class Scenario
        {
            public readonly string Name;
            public readonly Func<Player, IEnumerator> Run;
            public readonly bool InAll;
            public readonly string What;

            public Scenario(string name, Func<Player, IEnumerator> run, bool inAll, string what)
            {
                Name = name;
                Run = run;
                InAll = inAll;
                What = what;
            }
        }

        private static int _pass;
        private static int _fail;
        private static bool _worldStarted;
        private static bool _scenarioStarted;

        private static string ScenarioName => Environment.GetEnvironmentVariable("AUTOTEST");

        private static bool Enabled => !string.IsNullOrEmpty(ScenarioName)
            && Environment.GetEnvironmentVariable("AUTOTEST_MOD") == MyValheimModPlugin.PluginName;

        private static string OutDir => Environment.GetEnvironmentVariable("AUTOTEST_OUT")
            ?? Path.Combine(Paths.GameRootPath, MyValheimModPlugin.PluginName + "-autotest");

        public static void Init()
        {
            if (!Enabled)
            {
                return;
            }

            // Own save folder: the player's real characters and worlds are never touched.
            var saves = Path.Combine(OutDir, "saves");
            Directory.CreateDirectory(saves);
            Utils.SetSaveDataPath(saves);
            Application.runInBackground = true;

            // The test drives the game with real input events. Without this the input system
            // disables the keyboard the moment the window loses focus and swallows them.
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior =
                UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;

            Log($"scenario={ScenarioName} out={OutDir}");
        }

        private static void Log(string message)
        {
            MyValheimModPlugin.Log.LogInfo("AUTOTEST " + message);
        }

        private static void Check(bool ok, string what)
        {
            if (ok)
            {
                _pass++;
            }
            else
            {
                _fail++;
            }

            Log((ok ? "PASS " : "FAIL ") + what);
        }

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
        private static class FejdStartupStartPatch
        {
            private static void Postfix(FejdStartup __instance)
            {
                if (Enabled && !_worldStarted)
                {
                    _worldStarted = true;
                    __instance.StartCoroutine(StartWorld(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static class PlayerOnSpawnedPatch
        {
            private static void Postfix(Player __instance)
            {
                if (Enabled && !_scenarioStarted && __instance == Player.m_localPlayer)
                {
                    _scenarioStarted = true;
                    MyValheimModPlugin.Instance.StartCoroutine(RunScenario(__instance));
                }
            }
        }

        /// <summary>What the menu does on "Start", minus the clicks.</summary>
        private static IEnumerator StartWorld(FejdStartup menu)
        {
            yield return new WaitForSeconds(1f);

            if (!PlayerProfile.HaveProfile(TestName))
            {
                var profile = new PlayerProfile(TestName, FileHelpers.FileSource.Local);
                profile.SetName("Autotest");
                profile.m_firstSpawn = false; // no valkyrie intro
                profile.Save();
                Log("created test character");
            }

            if (!World.HaveWorld(TestName))
            {
                var created = new World(TestName, WorldSeed) { m_fileSource = FileHelpers.FileSource.Local };
                SaveSystem.SetSaveNumber(0u);
                created.SaveWorldFWLData(DateTime.Now);
                Log("created test world");
            }

            Game.SetProfile(TestName, FileHelpers.FileSource.Local);
            var world = World.GetCreateWorld(TestName, FileHelpers.FileSource.Local);
            ZNet.SetServer(true, false, false, TestName, "", world);
            ZNet.ResetServerHost();
            menu.m_startingWorld = true;
            Log("loading world");
            menu.LoadMainScene();
        }

        private static IEnumerator RunScenario(Player player)
        {
            yield return new WaitForSeconds(3f);
            Log($"spawned at {V(player.transform.position)}");
            player.SetGodMode(true);
            Log($"world set to quiet, {AutoTestPeace.Apply()} creatures removed");
            EnvMan.instance.m_debugTimeOfDay = true;
            EnvMan.instance.m_debugTime = 0.5f;
            EnvMan.instance.SetForceEnvironment("Clear");

            if (ScenarioName == "all")
            {
                yield return MyValheimModPlugin.Instance.StartCoroutine(Guard(RunAll(player)));
            }
            else
            {
                var scenario = Scenarios.FirstOrDefault(s => s.Name == ScenarioName);
                if (scenario == null)
                {
                    Check(false, "no such scenario: " + ScenarioName);
                }
                else
                {
                    yield return MyValheimModPlugin.Instance.StartCoroutine(Guard(scenario.Run(player)));
                }
            }

            // A run never leaves the player's config file changed, whatever the scenario flipped.
            yield return Reset(player);

            var summary = $"DONE pass={_pass} fail={_fail}";
            File.WriteAllText(Path.Combine(OutDir, "result.txt"), summary + "\n");
            Log(summary);
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }

        /// <summary>
        /// Runs a scenario and turns an exception into a FAIL instead of a silent stop. Nested steps
        /// (yield return SomeStep()) run here too, since Unity would stop the whole chain on their errors.
        /// </summary>
        private static IEnumerator Guard(IEnumerator scenario)
        {
            var steps = new Stack<IEnumerator>();
            steps.Push(scenario);
            while (steps.Count > 0)
            {
                object current;
                try
                {
                    if (!steps.Peek().MoveNext())
                    {
                        steps.Pop();
                        continue;
                    }

                    current = steps.Peek().Current;
                }
                catch (Exception e)
                {
                    Check(false, "scenario threw: " + e);
                    yield break;
                }

                if (current is IEnumerator nested)
                {
                    steps.Push(nested);
                    continue;
                }

                yield return current;
            }
        }

        /// <summary>
        /// Every scenario marked InAll, one after the other, in one game. A chained run is the only
        /// thing that catches a scenario leaving something behind for the next one, so this is the
        /// run that has to be green before shipping. It ends with the art guard.
        /// AUTOTEST_CHAIN="a,b" runs only those, in that order, to find the one that leaks.
        /// </summary>
        private static IEnumerator RunAll(Player player)
        {
            var chain = Environment.GetEnvironmentVariable("AUTOTEST_CHAIN");
            var names = chain != null
                ? chain.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).ToArray()
                : Scenarios.Where(s => s.InAll).Select(s => s.Name).ToArray();

            foreach (var name in names)
            {
                var scenario = Scenarios.FirstOrDefault(s => s.Name == name);
                if (scenario == null)
                {
                    Check(false, "no such scenario: " + name);
                    continue;
                }

                var wasPass = _pass;
                var wasFail = _fail;
                Log($"===== {name}");

                // Its own Guard, so a scenario that throws costs one FAIL and the chain runs on.
                yield return MyValheimModPlugin.Instance.StartCoroutine(Guard(scenario.Run(player)));
                Log($"===== {name}: pass={_pass - wasPass} fail={_fail - wasFail}");
                yield return Reset(player);
            }

            CheckNoArtWritten();
        }

        /// <summary>
        /// Back to a known state between two scenarios, whatever the last one left open. Every
        /// feature adds its own line here (close its window, stop its mode, forget its state).
        /// </summary>
        private static IEnumerator Reset(Player player)
        {
            ExampleWindow.Close();
            yield return PadOff();
            ResetSettings();
            if (player != null)
            {
                player.SetGodMode(true);
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// Every config entry of the mod back to its default. A scenario that flips one must not
        /// decide what the next one sees, and the run must not leave the player's config file changed.
        /// </summary>
        private static void ResetSettings()
        {
            foreach (var pair in MyValheimModPlugin.Instance.Config)
            {
                var entry = pair.Value;
                if (!Equals(entry.BoxedValue, entry.DefaultValue))
                {
                    entry.BoxedValue = entry.DefaultValue;
                }
            }
        }

        /// <summary>
        /// The art rule (CLAUDE.md, Scope): the mod writes no model, texture, sprite, material or
        /// bundle, anywhere. Walks every folder the mod may write to and fails on any art file.
        /// Add a folder here when the mod starts writing somewhere new.
        /// </summary>
        private static void CheckNoArtWritten()
        {
            var art = new HashSet<string>
            {
                ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".gif", ".psd", ".tif", ".tiff", ".dds",
                ".exr", ".hdr", ".webp", ".svg", ".ico", ".mat", ".fbx", ".obj", ".glb", ".gltf",
                ".dae", ".blend", ".mesh", ".asset", ".prefab", ".unity", ".bundle", ".assetbundle",
                ".shader", ".shadergraph", ".spriteatlas", ".ttf", ".otf", ".anim", ".controller",
            };

            var folders = new[]
            {
                Path.Combine(Paths.ConfigPath, MyValheimModPlugin.PluginName),
                Path.Combine(Paths.PluginPath, MyValheimModPlugin.PluginName),
            };

            var files = 0;
            var found = new List<string>();
            foreach (var folder in folders.Where(Directory.Exists))
            {
                foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                {
                    files++;
                    if (art.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    {
                        found.Add(file);
                    }
                }
            }

            Check(found.Count == 0, found.Count == 0
                ? $"art guard (in game): {files} files in the mod's folders, none is art"
                : "art guard (in game): art written: " + string.Join(", ", found));
        }
    }
}
#endif
