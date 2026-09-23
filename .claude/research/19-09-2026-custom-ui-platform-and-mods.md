# Custom UI: platform facts and other mods

Date: 19-09-2026. Game: Valheim 1.0.15, Unity 6000.0.75f1, macOS (Metal). Overview: [19-09-2026-custom-ui.md](19-09-2026-custom-ui.md).
Generalized for the template on 23-09-2026 from ValheimTomrer's research and what it verified in game.

**Labels:**

| Label | Meaning |
|---|---|
| **[checked]** | Read from the installed game files, its code, or a mod's source |
| **[docs]** | From Unity or BepInEx docs |
| **[claim]** | From a changelog, issue or forum post |
| **(verified in ValheimTomrer)** | Built and run in ValheimTomrer, a mod built on this research |
| **(corrected: ...)** | Where the first research was wrong |

## Contents

1. [Input](#1-input)
2. [The four UI systems](#2-the-four-ui-systems)
3. [Where the game's UI assets live](#3-where-the-games-ui-assets-live)
4. [Asset bundles on macOS](#4-asset-bundles-on-macos)
5. [Images without bundles](#5-images-without-bundles)
6. [How Jötunn does it](#6-how-jötunn-does-it)
7. [Mods that build UI without Jötunn](#7-mods-that-build-ui-without-jötunn)
8. [UI changes up to 1.0](#8-ui-changes-up-to-10)
9. [Links](#9-links)

---

## 1. Input

**Input setting is "Both"** [checked].
- Where: `activeInputHandler = 2` in `Data/globalgamemanagers` (PlayerSettings), read with UnityPy.
- Also present: `UnityEngine.InputLegacyModule.dll` ships, and the old InputManager has 99 axes.
- No "You are trying to read Input using the UnityEngine.Input class" error appears in any log.

What works:

| API | Works? | Notes |
|---|---|---|
| `ZInput.GetKeyDown(KeyCode, false)` | Yes | **Use this.** The game uses it. It reads the new Input System. (verified in ValheimTomrer) |
| `ZInput.GetButtonDown("Use")`, `"JoyButtonB"`... | Yes | Named game actions. They follow the player's key bindings. |
| `UnityEngine.Input.GetKeyDown` | Yes | Works only because of "Both". It would throw if the game switched to "new only". |
| BepInEx `KeyboardShortcut.IsDown()` | Yes | `UnityInput` picks the old API when it does not throw (BepInEx 5.4.23). Good for key combos. |
| `TMP_InputField` typing | Yes | Reads the IMGUI event queue, alive because of "Both". The game's own `GuiInputField` is a `TMP_InputField`. (verified in ValheimTomrer) |

**Gotchas** [checked]:
- **Unsupported keys crash.** `ZInput` maps 109 keyboard `KeyCode`s: letters, digits, F1 to F12,
  arrows, keypad, punctuation, modifiers, Home/End/PgUp/PgDn and similar.
  - An unmapped one (F13 to F15, `Help`, `SysReq`, `Break`, shifted symbols like `Exclaim` or
    `Hash`) throws `ArgumentOutOfRangeException`.
  - Validate config keys, or wrap the call.
  - (verified in ValheimTomrer: `ZInput.GetKeyDown(KeyCode.Question)` throws "key: None", and the
    throw silently ended the rest of that `Update`. Try each key once and drop the ones that throw.
    `?` still works as Shift + Slash.)
- **No focus check.** `ZInput` does not check if a text field has focus. Before acting on a key,
  check `Chat.HasFocus`, `Console.IsVisible` and `TextInput.IsVisible` yourself, and our own text
  boxes (`TMP_InputField.isFocused` on the selected object).

**Mouse wheel in uGUI** (verified in ValheimTomrer): the game's UI module reports one wheel notch as
`PointerEventData.scrollDelta.y` = 1 (a browser reports 100), positive for wheel up. `ScrollRect`s
read it this way, so `ZInput.Internal_GetMouseScrollWheel` returning 0 does not stop them.

**The game's pad state** (verified in ValheimTomrer):
- `ZInput.IsGamepadActive()`: the pad was used last. `ZInput.IsMouseActive()`: the mouse was.
- `ZInput.OnInputLayoutChanged` fires on every switch between keyboard/mouse and pad, and when a pad
  is plugged in.
- `ZInput.instance.GetButtonDef(name)` gives the game's button object, so its own repeat applies.
  `ZInput.instance.m_buttons` holds them all.
- `JoyAltKeys` is the game's modifier button: L2 in the default layout, L1 in the alternative one.
  `ZInput.IsNonClassicFunctionality()` is true in the alternative layouts.
- `ZInput.InputLayout`, `ZInput.CurrentGlyph` and `ZInput.ConnectedGamepadType` change when the
  layout, the icon family or the pad changes.
- To hide one of the game's buttons from the game (for our own pad combo), prefix the private
  `ZInput.TryGetButtonState`: every `GetButton`, `GetButtonDown` and `GetButtonUp` goes through it,
  in Update and FixedUpdate. Patch the private method, not the public wrappers, which are small
  enough to be inlined. Group buttons by binding path, not by name: several game buttons share one
  pad button.

**Reading the pad yourself** (verified in ValheimTomrer, which reads it for its window):
- `Gamepad.current` from `Unity.InputSystem`. A DualSense shows up as `DualSenseGamepadHID`.
- **Read sticks with `ReadUnprocessedValue()`**, then apply ZInput's radial dead zone: 0.2,
  rescaled to 0..1, the way `ZInput.ReadValueDef` does. `ReadValue()` adds Unity's own stick dead
  zone, which the game sets to 0.4 to 0.75 (measured): the stick then does nothing up to about half
  way and is at full speed by three quarters. ValheimTomrer first used `ReadValue()`, and the look
  felt clunky.
- **Camera look like the game's** (`PlayerController.LateUpdate`):
  - pad: 110 degrees a second at full stick, times `PlayerController.m_gamepadSens`;
  - mouse: 0.05 degrees a pixel, times `PlayerController.m_mouseSens` (`m_switchMouseSens` when the
    game says a pad's mouse is in use);
  - plus the game's invert settings. The settings screen writes these statics live, so no mod
    setting is needed.
- **The UI module reads the same pad.** See the
  [game API reference, 2.7](19-09-2026-custom-ui-game-api.md#27-gamepad) for the double press trap.

## 2. The four UI systems

### 2.1 uGUI + TextMeshPro (recommended)

- **Same stack as the game** [checked]. Each scene (`start`, `main`, `loading`) has one `EventSystem`
  with `InputSystemUIInputModule`. There is no `StandaloneInputModule` and no UI Toolkit.
  (verified in ValheimTomrer: its whole window is built this way)

  | Scene | Images | TMP texts | Canvases with GuiScaler |
  |---|---|---|---|
  | start | 345 | 317 | 4 |
  | main | 679 | 567 | 25 |

- **TMP is the Unity 6 version** built into uGUI 2.0 [checked]:
  - Use `textWrappingMode` (for example `TextWrappingModes.NoWrap`) instead of `enableWordWrapping`,
    which is obsolete.
  - Use `fontFeatures` instead of `enableKerning`, which is obsolete.
- **Builders in code:** `TMP_DefaultControls.CreateButton/CreateText/CreateInputField/CreateDropdown`
  and `UnityEngine.UI.DefaultControls` are available [checked]. The template's kit has its own small
  builders in `src/Ui/UiBuild.cs`.
- **Default font is empty** [checked]. `TMP_Settings` has no default font, so every text made in code
  needs `.font` set.

### 2.2 IMGUI (`OnGUI`)

- **It runs.** `UnityEngine.IMGUIModule.dll` ships, and the game uses `OnGUI` itself (`Game.OnGUI`
  calls `ZInput.OnGUI`) [checked].
- **The default skin and GUI shaders are present** [checked].
- **Valheim fonts** can be loaded as legacy fonts:
  ```csharp
  Resources.Load<Font>("Fonts/Averia_Serif_Libre/AveriaSerifLibre-Regular")
  ```
  Also: `AveriaSansLibre-*`, `Fonts/Norse/Norse`, `Norsebold` [checked].
- **Retina:** it draws in real pixels (the log shows a 3456×2160 surface), so scale with `GUI.matrix`
  or text is tiny [checked].
- **It does not block the game.** `Event.Use()` and `Input.ResetInputAxes()` do not affect `ZInput`
  or game UI clicks. The cursor also stays locked unless patched [checked].
- **Unity:** "IMGUI is not generally intended to be used for normal in-game user interfaces" [docs].
- **Verdict:** fine for dev and debug overlays. Not for player UI.

### 2.3 UI Toolkit

**What ships** [checked]:
- `UnityEngine.UIElementsModule.dll`, with `PanelSettings`, `UIDocument`, `ThemeStyleSheet`.
- Its runtime shaders, except `Hidden/TextCore/Sprite` (only needed for icons inside text).

**What is missing** [checked]:
- **No runtime theme.** Buttons, fields and lists come out unstyled. Style sheets cannot be compiled
  at runtime, so all styling happens in code.
- **No default font.** Set it with `FontDefinition.FromFont(...)`.

**Input:** pointer and gamepad go through the EventSystem bridge [docs].

**Not checked:** sorting against game canvases, and typing in text fields.

**Verdict:** high effort, and it would not look like Valheim. Not recommended.

### 2.4 Asset bundle prefabs

Possible, see section 4. It needs a Unity 6000.0.x project and a macOS build of the bundle. Only
worth it for a large designed UI.

## 3. Where the game's UI assets live

- **Most content** sits in the game's own asset bundles: `Data/StreamingAssets/SoftRef/Bundles/`, 799
  files, about 4 GB.
- **The Resources folder** (`resources.assets`) is always loadable, even at plugin start.
- **Unloading:** a bundle that belongs to one scene is unloaded with that scene, and its objects are
  destroyed [checked].

**Bundles with UI content, and when they are loaded** [checked, bundle names change between patches]:

| Content | Main menu | World | Loading screen |
|---|---|---|---|
| **UIAtlas** SpriteAtlas (246 UI sprites) | yes | yes | yes |
| IconAtlas (1513 item and piece icons) | yes | yes | no |
| TMP font assets, legacy fonts, `litpanel` material, `GUIInputField` prefab | yes | yes | yes |
| Built-in UI shaders (`UI/Default`, `Sprites/Default`) | yes | yes | yes |
| Shared UI prefabs `GUIButton`, `GUIToggle`, `GUIDropDown`, `GUISlider`, `GUIStepper`, `GUITabButton`, `GUIText`, `KeyHint` | yes | yes | no |
| Main menu prefabs, menu mist materials | yes | no | no |
| In-world prefabs (HUD, Inventory, BuildUI), `piece_icon`, `gui_blur` materials | no | yes | no |

**Loaded in the world** (measured in ValheimTomrer): sprite atlases `UIAtlas` (247 entries, one name
twice), `IconAtlas` (1513), `xbox` (36), `PS5-Atlas` (18), `Switch2-Atlas` (46), and the TMP sprite
asset `gamepad_glyphs` with the controller icons.

**Fonts** [checked]:
- **TMP font assets:** `Valheim-AveriaSerifLibre`, `Valheim-AveriaSansLibre`, `Valheim-Norse`,
  `Valheim-Norsebold`, `Valheim-Prstartk`, `Valheim-Rune`, plus Noto fallbacks.
- **Two copies of each exist:** one in a bundle (used by the game UI) and one in `resources.assets`.
- **Legacy `Font` assets:** AveriaSansLibre and AveriaSerifLibre (Regular, Bold, Italic, Light...),
  Norse, Norsebold.

**Sprite lookup:** see the [game API reference, section 3.2](19-09-2026-custom-ui-game-api.md#32-sprites-uiatlas).

**UI shaders and materials** [checked]:

| Where | What |
|---|---|
| Built in | `UI/Default`, `Sprites/Default`, `Sprites/Mask` |
| Resources | `TextMeshPro/Distance Field`, `TextMeshPro/Mobile/Distance Field` |
| Font bundle | `Custom/LitGui`, used by materials `litpanel`, `lithud`, `item_icon`, `statuseffect_icon` |
| World bundle | `Custom/icon`, `Custom/UI_BGBlur` (`gui_blur`) |

- `Sprites/Default` has no depth test at all: `unity_GUIZTestMode` on it does nothing. (verified in
  ValheimTomrer)
- A runtime copy of a loaded material (`new Material(source)`) is fine: nothing goes to disk.
  (verified in ValheimTomrer, for its text materials)

## 4. Asset bundles on macOS

**What the game's own bundles look like** [checked]:
- Built with Unity 6000.0.75f1, target StandaloneOSX.
- Their shaders are compiled for **Metal only**.
- A leftover `StreamingAssets/tmp_fonts` bundle was built for Windows (Unity 2022.3). Its shaders are
  D3D11 and Vulkan only. This is what a Windows-built bundle contains.

**Unity rules** [docs]:
- Bundles are per platform.
- They cannot contain code.
- An older Unity cannot load a bundle from a newer Unity.
- **Shaders from a Windows bundle show pink on Mac.**

**If we ever make one:**

| Step | Why |
|---|---|
| Build with Unity **6000.0.75f1** for **StandaloneOSX**, and keep type trees | Matches the game's own bundles |
| Leave `Image` materials empty | They then use the default UI material at runtime, so no shader ships |
| Do not put fonts in the bundle. Assign the game's TMP fonts after loading | Fonts come with shaders |
| If a material must ship, fix it after loading: `mat.shader = Shader.Find(mat.shader.name)` | Uses the game's own compiled shader |
| Our own scripts in the bundle need the Unity project to have an assembly with exactly the mod's assembly name, loaded before the bundle | A bundle stores only a reference to a script, not its code |

**Guides:**
- Wiki Unity project guide (pins 6000.0.75f1, updated 14-09-2026): https://github.com/Valheim-Modding/Wiki/wiki/Valheim-Unity-Project-Guide
- Shaders in bundles on Mac vs Windows: https://support.unity.com/hc/en-us/articles/207482023-Shaders-in-AssetBundles-for-Desktop-platforms-Win-Mac-

**Building UI in code avoids all of this.** ValheimTomrer built a full window, its hints and its
popup from the game's own loaded font, sprites and icons, with no bundle and no file on disk.

## 5. Images without bundles

- **Everything needed ships** [checked]:
  - `UnityEngine.ImageConversionModule.dll`, with `ImageConversion.LoadImage`;
  - `Sprite.Create`.
- **The game does this itself** for profile pictures.
- **Pattern:**
  1. Embed a PNG in the DLL as an `EmbeddedResource`.
  2. `new Texture2D(2, 2).LoadImage(bytes)`.
  3. `Sprite.Create(tex, rect, pivot, 50, 0, SpriteMeshType.FullRect, border)`.
- **QuickStackStore** ships its icon this way.
- **Cutting a sprite out of a loaded sheet** works the same way with no PNG:
  `Sprite.Create(sheet, rect, pivot, ...)` on a texture the game already loaded. ValheimTomrer cuts
  controller icons out of the `gamepad_glyphs` sheet like this. Destroy such sprites when the UI is
  rebuilt.

## 6. How Jötunn does it

- **Source:** `JotunnLib/Managers/GUIManager.cs`, v2.30.1 (dev, 17-09-2026).
- **License:** MIT, so we can copy with attribution.
- **1.0 port:** it changed **nothing** in the GUI code. The 1.0 work only touched pieces and
  equipment.

**Canvases** `CustomGUIFront` (order 2000) and `CustomGUIBack` (order 0):
- **Parent:** `GuiRoot/GUI` in the main menu, `_GameMain/LoadingGUI` in the world.
- **Components:** `GuiPixelFix`, `Canvas`, `CanvasScaler` (reference pixels per unit 50),
  `GraphicRaycaster`.
- **Created** on scene load, and in postfixes on `FejdStartup.SetupGui` and `Game.Start`.
- **Known bug (issue #454, open):** the canvas does not follow the game's GUI scale setting. Adding
  `GuiScaler` fixes it (our checklist does).

**Assets:**
- **Lookup:** a cache built from `Resources.FindObjectsOfTypeAll(type)`. For sprites it tries
  `SpriteAtlas "UIAtlas"` first.
- **Names used:**

  | Kind | Names |
  |---|---|
  | Sprites | `woodpanel_trophys`, `button`, `text_field`, `checkbox`, `map_marker` |
  | Material | `litpanel` |
  | Sounds | `sfx_gui_button`, `sfx_gui_select` |

**Widgets:**
- Built with `DefaultControls`, then restyled.
- **All use the old `UnityEngine.UI.Text`**, not TMP, so they do not match the game's text exactly.

**`BlockInput(bool)`** is a counter, active only in the world scene:

| Patch | Effect |
|---|---|
| Postfix `PlayerController.TakeInput` to false | No walking or looking |
| Postfix `Player.TakeInput` to false | No attack, use, hotbar, build |
| Postfix `TextInput.IsVisible` to true | Frees the cursor, blocks Esc, chat and map |
| Transpiler on `InventoryGui.Update` and `GameCamera.UpdateCamera` | Blocks Tab and wheel zoom |
| On enable: `GameCamera.instance.m_mouseCapture = false` | Cursor frees at once |

- **Esc** no longer opens the pause menu while input is blocked. The window must close itself on
  Esc.
- **Not covered:** the gamepad D-pad hotbar (`HotkeyBar`). Our patch set adds it.

## 7. Mods that build UI without Jötunn

All patched methods below still exist in 1.0.15 [checked].

| Mod | Updated for 1.0 | How it builds UI | How it blocks input | License |
|---|---|---|---|---|
| **ValheimTomrer** (the source of this template's UI kit) | Yes (09-2026) | In code with uGUI + TMP: own canvas at order 950, the HUD's font with own material copies, UIAtlas sprites, own popup type, copies of the game's key hint entries. Full pad support. | Nine patches on one flag (see the [game API reference, 2.3](19-09-2026-custom-ui-game-api.md#23-block-player-input)) | MIT |
| **ColorfulPieces** and others in **ComfyMods** (redseiko) | Yes (09-2026) | In code with uGUI + TMP, via helper classes (`ComfyLib/UI`). Copies small game parts: popup text styles, the `TextInput` text field. Own `Canvas`, drag and resize. | `TextInput.IsVisible` postfix + `StoreGui.m_hiddenFrames = 0` each frame (stops zoom) + close on `ZInput.GetKeyDown(Escape)` | GPL-3.0 |
| **QuickStackStore** (Goldenrevolver) | Yes | Copies `InventoryGui.m_takeAllButton` (with `UIGamePad` off while copying). Copies the split dialog as a confirm box. PNG icon in the DLL. | Not needed, lives inside the inventory | MIT |
| **AzuEPI** (Azumatt) | Yes | Copies game backgrounds, grid layout in code, sprites by name | Not needed | none stated |
| **BetterUI** (Azumatt fork) | Yes | Copies HUD bars and the enemy label | Not needed | BSD-2 |
| **MyLittleUI**, **TradersExtended** (shudnal) | Yes | Copies the `TextInput` field, `StoreGui` panel, split dialog, hotbar. HUD blocks under `Hud.m_rootObject`. | `Chat.HasFocus` postfix while a filter field has focus | Unlicense |
| **Homestead** | Yes (1.0.14) | About 250 lines that redo Jötunn's GUI manager in one file: own canvas order 2000, `DefaultControls` with old `Text` | Counter. `PlayerController.TakeInput` false, `TextInput.IsVisible` true, zoom guard on `GameCamera.UpdateCamera` | GPL-3.0 |
| **ConfigurationManager** (shudnal fork 1.1.21) | Yes | IMGUI | **The most complete blocker**, see below | LGPL-3.0 |
| EpicLoot | Yes, but now needs Jötunn | Asset bundle next to `StoreGui` | `Minimap.IsOpen` true, `Menu.Show` skipped | none |
| Auga | **No** (last change 2024) | Full UI swap from an asset bundle | | skip |

**shudnal ConfigurationManager `ValheimInput.cs`**, what it blocks while open:

| Area | How |
|---|---|
| Cursor | Skips `GameCamera.UpdateMouseCapture`, `Menu.UpdateCursor` and `FejdStartup.UpdateCursor`. Then `ZCursor.Show()` and unlock. |
| Movement and game checks | `PlayerController.TakeInput` false, `TextInput.IsVisible` true |
| Keys | `ZInput` button and key reads return false. Axis, mouse delta and scroll read zero. |
| Clicks on game UI underneath | Skips the handlers of `Button`, `Toggle`, `Slider`, `ScrollRect`, `TMP_InputField` and the inventory grid |
| On close | `ZInput.ResetAllButtonStates()` and `PlayerController.SetTakeInputDelay(0.1f)` |

**The official BepInEx ConfigurationManager** (v19.0) does not block Valheim input, and its cursor
changes lose to `ZCursor`. The Azumatt builds are deprecated. Use shudnal's fork on 1.0.

**Best to learn from, in order:**
1. **The template's own UI kit** (from ValheimTomrer): tested on 1.0.15 with keyboard, mouse and pad.
2. **ComfyMods ColorfulPieces** and its `ComfyLib/UI`. Free window, updated for 1.0, TMP, small,
   3-line input block. GPL, so read it and write our own.
3. **Jötunn `GUIManager.cs`** (MIT, can copy) together with **Homestead `HomesteadUi.cs`** (same
   idea, no Jötunn, one file). Switch their old `Text` to TMP and add `GuiScaler`.
4. **shudnal `ValheimInput.cs`.** Only if we need stricter blocking or use IMGUI.

## 8. UI changes up to 1.0

Useful to judge how old a guide is.

| Version (date) | Change |
|---|---|
| ~0.214 (2023) | UI text moved to TextMeshPro [claim] |
| 0.217.36 (12-2023) | Settings menu rebuilt. **Game input moved to the Unity Input System** (`ZInput`) [claim] |
| 0.220.3 (03-2025) | UI rendering optimized. **Custom UI needs its own `Canvas`** [checked in Jötunn diff] |
| 0.221.3 / 0.221.4 (08-2025 to 09-2025) | **Moved to Unity 6.** Console became a setting. The UI scene can load late [checked in Jötunn diff] |
| 0.221.10 (02-2026) | Radial menu for emotes. Graphics settings reworked [claim] |
| **1.0** (09-09-2026) | **New build menu** (`BuildUi`), piece-author window on the HUD, Unity 6000.0.75 [claim + checked] |
| 1.0.14 (17-09-2026) | Rebind the radial wheel and hotbar [claim] |

**Old advice that no longer holds on 1.0:**

| Old advice | Now |
|---|---|
| `UnityEngine.UI.Text` | Use `TextMeshProUGUI` |
| Old `Input.GetKey*` | Works, but the game's own blocking ignores it. Use `ZInput`. |
| `ZInput.AddButton(name, KeyCode)` | Now private, and takes an Input System path |
| Set `Cursor.*` directly | Overwritten every frame by `ZCursor` / `GameCamera` |
| `Hud.m_pieceSelectionWindow`, `PieceCategory` tabs | Replaced by `BuildUi` and `UsageTagFlags` |
| Parent UI under the game's GUI root with no `Canvas` | No longer shows |

**Renamed members** that show up in old code:

| Old | New or status |
|---|---|
| `Minimap.m_instance` | `s_instance` |
| `InventoryGrid.Element` | `InventoryElement` |
| `InventoryGrid.OnRightClick` | removed |
| `Console.InputText` | `Terminal.InputText` |

**Changed signatures** (verified in ValheimTomrer, which patches them):
- `PlayerController.TakeInput(bool)` takes a `bool`.
- `InventoryGui.Show(Container, int)`: name the overload in the patch.

## 9. Links

**Jötunn:**
- GUI tutorial: https://valheim-modding.github.io/Jotunn/tutorials/gui.html
- Sprite list for 1.0.7: https://valheim-modding.github.io/Jotunn/data/gui/sprite-list.html
- GUIManager source: https://github.com/Valheim-Modding/Jotunn/blob/dev/JotunnLib/Managers/GUIManager.cs
- GUI scale issue #454: https://github.com/Valheim-Modding/Jotunn/issues/454

**Mods:**
- ComfyMods: https://github.com/redseiko/ComfyMods (ColorfulPieces: `ColorfulPieces/Core/Controllers/ColorPickerController.cs`, `ColorfulPieces/Patches/TextInputPatch.cs`)
- QuickStackStore: https://github.com/Goldenrevolver/QuickStackStore
- Homestead: https://github.com/sighsorry1029/Homestead
- shudnal ConfigurationManager: https://github.com/shudnal/ConfigurationManager (`ValheimInput.cs`)

**Wiki and docs:**
- Valheim-Modding wiki: https://github.com/Valheim-Modding/Wiki/wiki (Valheim-1.0-FAQ, Unity project guide, Best-Practices)
- Unity Input System 1.19, UI support and limits: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/UISupport.html
- Unity asset bundles: https://docs.unity3d.com/Manual/AssetBundlesIntro.html

No blog post or video about building Valheim UI without Jötunn turned up. Beyond mod source, the
Discords listed in the wiki are the place to ask.

**To redo the asset scan:** UnityPy 1.25 on `Data/globalgamemanagers` and
`Data/StreamingAssets/SoftRef/Bundles/*`.
