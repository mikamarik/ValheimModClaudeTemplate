#if DEBUG
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Bootstrap;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;

namespace MyValheimMod.Dev
{
    /// <summary>
    /// "probe": measures the game instead of guessing, into .devtest/probe.txt. The start of every
    /// plan's Phase 0: copy this scenario, add the measurements the plan guessed at (counts, the
    /// timing of a call it assumed was cheap, whether the risky mechanism works at all).
    /// </summary>
    internal static partial class AutoTest
    {
        private static IEnumerator Probe(Player player)
        {
            var text = new StringBuilder();
            text.AppendLine($"# probe, {System.DateTime.Now:dd-MM-yyyy HH:mm}");
            text.AppendLine();

            text.AppendLine("## Versions");
            text.AppendLine($"game {Version.GetVersionString()} | network {Version.c_networkVersion} | unity {Application.unityVersion}");
            text.AppendLine($"platform {Application.platform} | screen {Screen.width}x{Screen.height} | gpu {SystemInfo.graphicsDeviceName}");
            text.AppendLine();

            text.AppendLine("## Plugins loaded");
            foreach (var plugin in Chainloader.PluginInfos.Values.OrderBy(p => p.Metadata.Name))
            {
                text.AppendLine($"{plugin.Metadata.Name} {plugin.Metadata.Version} ({plugin.Metadata.GUID})");
            }

            Check(Chainloader.PluginInfos.ContainsKey(MyValheimModPlugin.PluginGuid), "this plugin is loaded");
            text.AppendLine();

            text.AppendLine("## Layers (an empty name is a free layer)");
            for (var layer = 0; layer < 32; layer++)
            {
                text.AppendLine($"{layer,2} {LayerMask.LayerToName(layer)}");
            }

            text.AppendLine();

            // Every button the game reads, keyboard and pad, so a new key or combo is picked from
            // facts: a combo the game already uses (JoyAltKeys + a button with altKey) is taken.
            text.AppendLine($"## Buttons, layout {ZInput.InputLayout} (name | source | binding)");
            var buttons = ZInput.instance.m_buttons;
            foreach (var pair in buttons.OrderBy(p => p.Value.Source).ThenBy(p => p.Key))
            {
                var def = pair.Value;
                var binding = def.ButtonAction != null && def.ButtonAction.bindings.Count > 0 ? def.GetActionPath() : "-";
                text.AppendLine($"{pair.Key} | {def.Source} | {binding}{(def.AltKey ? " | with the modifier" : "")}");
            }

            Check(buttons.ContainsKey(Input.GamePad.Modifier), $"the game's pad modifier {Input.GamePad.Modifier} exists ({buttons.Count} buttons)");
            text.AppendLine();

            // With no pad connected the game lists its default pad layout. Once a pad connects it
            // switches to the layout the player saved (Settings > Controls), so list the pad buttons
            // again with the test pad in: these are the ones a test presses.
            var before = ZInput.InputLayout;
            PadOn();
            yield return PadPress(false, Input.PadButton.Down);
            yield return Wait(0.5f);
            text.AppendLine($"## Pad buttons with a pad connected, layout {ZInput.InputLayout} (was {before} without one)");
            foreach (var pair in ZInput.instance.m_buttons.Where(p => p.Value.Source == ZInput.InputSource.Gamepad).OrderBy(p => p.Key))
            {
                var def = pair.Value;
                var binding = def.ButtonAction != null && def.ButtonAction.bindings.Count > 0 ? def.GetActionPath() : "-";
                text.AppendLine($"{pair.Key} | {binding}{(def.AltKey ? " | with the modifier" : "")}");
            }

            yield return PadOff();
            text.AppendLine();

            text.AppendLine("## Game data");
            var hammer = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();
            var pieces = hammer != null ? hammer.m_itemData.m_shared.m_buildPieces.m_pieces.Count : -1;
            text.AppendLine($"items {ObjectDB.instance.m_items.Count} | prefabs {ZNetScene.instance.m_prefabs.Count} | hammer pieces {pieces}");
            Check(pieces > 0, $"the hammer has {pieces} pieces");

            var watch = Stopwatch.StartNew();
            var around = new System.Collections.Generic.List<Piece>();
            Piece.GetAllPiecesInRadius(player.transform.position, 30f, around);
            text.AppendLine($"Piece.GetAllPiecesInRadius 30 m: {around.Count} pieces in {watch.Elapsed.TotalMilliseconds:0.00} ms");
            text.AppendLine();

            text.AppendLine("## UI assets (all taken at runtime, nothing on disk)");
            var atlas = Resources.FindObjectsOfTypeAll<SpriteAtlas>().FirstOrDefault(a => a.name == "UIAtlas");
            text.AppendLine($"UIAtlas: {(atlas != null ? atlas.spriteCount + " sprites" : "not found")}");
            // The pad icons: the game's texts use <sprite="xbox" name="button_a">, one TMP sprite asset
            // per pad family. PadGlyphs finds the one that holds the button names.
            foreach (var asset in Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>().OrderBy(a => a.name))
            {
                text.AppendLine($"TMP sprite asset '{asset.name}': {asset.spriteCharacterTable?.Count ?? 0} icons");
            }

            var font = Hud.instance != null && Hud.instance.m_hoverName != null ? Hud.instance.m_hoverName.font : null;
            text.AppendLine($"HUD font: {(font != null ? font.name : "not found")}");
            Check(atlas != null && font != null, $"UIAtlas ({atlas?.spriteCount}) and the HUD font ({font?.name}) are there");
            Check(Ui.UiTheme.Ensure(), "the UI theme builds from them");
            var cross = Ui.PadGlyphs.Of(Input.PadButton.Cross);
            text.AppendLine($"PadGlyphs: cross icon {(cross != null ? cross.name + " " + cross.rect.size : "not found")}");
            Check(cross != null, "PadGlyphs finds the game's cross icon");

            var path = Path.Combine(OutDir, "probe.txt");
            File.WriteAllText(path, text.ToString());
            Log("wrote " + path);
            yield break;
        }
    }
}
#endif
