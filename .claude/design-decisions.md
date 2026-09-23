# MyValheimMod: design decisions and rationale

Why the mod works the way it does, the full behaviour behind each rule in `CLAUDE.md`, the measured
numbers, and what was tried and dropped.

`CLAUDE.md` has the rules: what to do, what never to break. It is loaded on every turn. This file
has the reasons and the detail, and it is not. Read the section for an area before you change how
it behaves or undo a decision recorded here. Skip it for ordinary work.

**New reasons go here.** When a session settles how something works, the reason goes in the right
section below and the rule, in one line, goes to `CLAUDE.md`.

Sections 1 to 9 are general Valheim 1.0 modding knowledge, carried over from ValheimTomrer (an
in-game blueprint editor with its own window, pad support, hammer builds from chests and a
24-scenario autotest, built 19 to 23-09-2026) and from making this template (23-09-2026). Section 10
on is this mod's own. When this mod corrects a general fact, fix it here and say so.

Tags: **[any]** every mod, **[window]** a mod with its own window, **[hud]** a mod that adds to the
game's HUD, hint row or popups, **[pad]** controller, **[build]** placing, previewing or removing
pieces, **[items]** inventories and chests.

Sections:

1. The machine and the tools
2. Game 1.0 facts
3. Harmony and patch targets
4. Custom UI
5. The pad
6. Unity objects, rendering and the world
7. The autotest
8. Publishing
9. Plans, sub-agents and process
10. This mod

---

## 1. The machine and the tools

### Apple Silicon

Valheim 1.0 ships a **universal binary**. On native arm64, BepInEx 5's MonoMod detours die in
`DetourHelper.GetIdentifiable()` inside `HarmonyInteropFix.Apply()`, and **no plugins load and no
`LogOutput.log` is written at all**. It looks exactly like a normal vanilla launch. (BepInEx issue
#1303; fix PR #1402 still unmerged on 19-09-2026.)

`run_bepinex.sh` ships with `ARCHPREFERENCE="arm64,x86_64"`, which *causes* this, hence the patch to
`"x86_64,arm64"`. An outer `arch -x86_64` wrapper does **not** help: the script overrides it.

The quarantine is cleared after every reinstall, even when the new injector was copied over the old
one: macOS keeps xattrs on a file you `cp` over, so a stale quarantine flag can survive a
replacement and silently block injection.

### References come from the install

References resolve out of the local install, not NuGet, so compile-time and runtime versions can
never drift. `assembly_guiutils` is publicized too: the `GuiUtils` navigation helpers and
`Localization.AddWord` are private there.

### The debugger

UnityDoorstop can open a **Mono soft debugger**: real breakpoints, stepping and locals inside both
our code and Valheim's. Verified on the owner's machine.

The attach uses the **`vstuc`** debugger type from the *Visual Studio Tools for Unity* extension. Its
`endPoint` property accepts an arbitrary address, which is what makes it work against a
doorstop-hosted game rather than a Unity Editor. `ms-vscode.mono-debug` is not needed.

Portable PDBs are enabled in the csproj and deployed next to the DLL, so symbols resolve.
`--doorstop-mono-debug-suspend true` exists for breakpoints in `Awake` and plugin load, which
otherwise run too early to catch.

### Reading the game

- The metadata reader (`System.Reflection.Metadata` in a throwaway `dotnet run` project) gives
  names, signatures and enum values.
- `ilspycmd` wants .NET 8, hence `DOTNET_ROLL_FORWARD=Major` on a .NET 9 machine. A fresh install
  once failed with "Settings file 'DotnetToolSettings.xml' was not found in the package"; the
  metadata reader and the UTF-16 string scan are the two ways round it.
- The whole game decompiled (`-p -o`) is about 1 minute and 692 files.

### This machine

- A plain `grep -r` follows `.gitignore` (a gitignore-aware tool shadows it). ValheimTomrer ignored
  `.claude/`, and a rename once reported "no leftovers" while CLAUDE.md still had 12 hits. This
  template tracks `.claude/`, but `.devtest/`, `bin/` and `obj/` are still skipped.
- `/usr/local/bin/timeout` is Intel Homebrew coreutils. A universal binary started from it runs as
  x86_64 under Rosetta: Playwright tests took 50 s each under `timeout`, 2 s without.
- macOS grep prints `src//Dev/...` for `grep -rn ... src/`, so a `grep -v "src/Dev/"` after it
  filters nothing. The world-save grep names its folders for this reason.
- Screen 3456x2160, the game's GUI scale 1.8.

---

## 2. Game 1.0 facts

**[any]**

- Game 1.0.15, network version 40. Data versions: `c_WorldVersion = 41`, `c_PlayerVersion = 46`,
  `c_ItemDataVersion = 109`.
- Also shipped in `Managed/`: `Newtonsoft.Json` 13.0 (do not bundle it), `SoftReferenceableAssets`,
  `Splatform`/`PlayFabParty` (crossplay), `Unity.InputSystem` 1.19.
- Free layers: 3, 6, 7 and 30 (`probe` lists them). 29 is `InstanceRenderer`, 31 `smoke`. The main
  camera's `cullingMask` is 0xFFFFFFFF, so it draws any free layer too (ValheimTomrer's editor, §6).
- The input setting is "Both": old `UnityEngine.Input`, IMGUI, BepInEx `KeyboardShortcut` and text
  fields work. The game itself reads through `ZInput` (the new input system).
- The game has 130 named buttons in the default layout (`probe`). `JoyAltKeys` (the pad modifier) is
  the left trigger by default. Taken modifier combos in the default layout: L3 (auto pickup), D-pad
  up and down (camera zoom), D-pad left and right (minimap zoom), select (chat), circle (dodge),
  start (hide HUD). In Alternative2 also L2 + R3 (`JoyAltPlace`, the hammer's alt place). Free in
  both: L2 + R1 (the template's example), L2 + square, L2 + triangle (ValheimTomrer uses the last
  two). Pick a combo from `probe.txt`, which lists the bindings with a pad connected.
- `World` identity for files kept per world: `ZNet.World.m_name` and `m_uid`.
- `ZoneSystem.instance.GetGroundHeight(p, out h)` is an exact ray on the terrain collider, about
  2 us a call; it sees terrain only, not rocks. `Heightmap.GetHeight` snaps to the nearest vertex.
- A world object counts as real only with a valid `ZNetView`. A Unity object that broke compares
  equal to null. Pieces farther than about 128 m may not be loaded.
- The player's body turns about 90 degrees a second toward `m_lookYaw`: set `transform.rotation`
  and `m_body.rotation` too when a facing is needed now.
- Swimming puts the hammer away and ends place mode.
- The console is off by default since 0.221.4.

**[build]** (ValheimTomrer, the hammer and blueprints)

- The build menu sorts pieces by `Piece.UsageTagFlags` (`[Flags]`, read by `ByUsagePieceList`):
  `Misc=1, Crafting=2, Building=4, Floor=8, Wall=16, Roof=32, Architecture=64, Furniture=128,
  Lighting=256, Decor=512, Storage=1024, Transport=2048, Food=4096, Meads=8192, Feasts=16384,
  Defense=32768, Stacks=65536, Stairs=131072, Doors=262144, Seasonal=524288`. `Piece.PieceCategory`
  is legacy. `Piece.ComfortGroup`: `Fire, Bed, Banner, Chair, Table, Carpet, Display, Decor, Garland,
  Lantern, Leisure`.
- The hammer has 398 pieces, 49 unlocked on a fresh character, 187 with no snap points; 394 use
  `LODGroup`, 12 a `SkinnedMeshRenderer`, none `InstanceRenderer`. No hammer piece has
  `m_groundPiece` in 1.0. `piece_repair` has no mesh, no collider, and is not in `ZNetScene`.
- Piece lists: `Player.GetBuildTool().m_pieces` (all), `.m_availablePieces` (unlocked). The hook for
  unlock changes is `Player.UpdateAvailablePiecesList`. `Player.InPlaceMode()` is also true for the
  hoe and the cultivator: check the tool is the hammer.
- Unlocked: `Player.m_knownRecipes` contains `piece.m_name`; skip the test when
  `m_noPlacementCost` or the world setting `AllPiecesUnlocked` is on. God mode unlocks nothing.
- `Player.PlacePiece(piece, pos, rot, doAttack, cheated)` is public, takes any prefab, sets the
  creator and plays the effect. It takes no materials and checks nothing. It calls
  `WearNTear.OnPlaced`, which ends the 30 s support grace, so a floating piece breaks at once.
- The vanilla flow: `HaveRequirements` -> `TryPlacePiece` -> `PlacePiece` -> `ConsumeResources` ->
  stamina, skill, tool wear. Remove is read on release (`GetButtonUp("Remove")`,
  `GetButtonUp("JoyRemove")`). To remove like the hammer, copy `Player.RemovePiece` call for call.
- Placement constants: turn step 22.5 degrees, snap distance 0.5 m, snap search radius 10 m. The aim
  (`Player.PieceRayTest`) hits rocks, bushes, pieces and the seabed within 5 m, not only the ground.
- Support: take the numbers from `WearNTear.GetMaterialProperties`, never type them in. The ground
  test grows each collider 0.15 m and overlaps it with the terrain's mesh collider (a surface, so a
  box wholly under the terrain gets no ground). Built all at once, pieces can keep a higher support
  value for good than built one by one. Only carts, ships and the ward skip the support check.
- Placement and support use colliders, not render meshes; their bottoms differ by 1 to 11 cm.
- Stations: read a station's range from `m_buildRange` / `m_rangeBuild`; `GetStationBuildRange()`
  throws in `CraftingStation.GetExtensions` on a copied station. A copy of a workbench counts as a
  real station (its `Start` still registers it): strip `CraftingStation` from every copy.
- No-build zones: `Location.IsInsideNoBuildLocation(p)`.

**[items]**

- The game keeps no static list of containers: `Piece.GetAllPiecesInRadius` then
  `GetComponentInChildren<Container>` (a cart's or karve's hold is a child with no `Piece`).
- `Container` saves only on the ZDO owner: a take from a chest another client owns comes back on
  its next load. `Container.CheckAccess` reads the `Piece` on its own object.
- `Inventory.RemoveItem(name, amount, ...)` returns void: count first with `CountItems`. The item
  key is `m_itemData.m_shared.m_name` (the `$item_wood` token).

---

## 3. Harmony and patch targets

**[any]**

- **Patch the private method under an inlined wrapper.** `ZInput.GetButtonDown` and friends are small
  enough to be inlined, so a patch on them may never fire. Every `GetButton`, `GetButtonDown` and
  `GetButtonUp` goes through the private `ZInput.TryGetButtonState`, in Update and FixedUpdate
  alike. The wheel: `ZInput.Internal_GetMouseScrollWheel`, not `GetMouseScrollWheel`.
- **Skip a per-frame update with a prefix instead of fighting it with a postfix.** `KeyHints.UpdateHints`
  sets each hint group active every frame; a postfix that switched one off made
  `UIInputHint.OnEnable` (a layout rebuild) run every frame. A prefix that skips the update ran it 0
  times in 30 frames.
- **Block a button where the game reads it**, not at the UI method it calls later:
  `InventoryGui.Update` resets `JoyButtonY` before it calls `Show`, so blocking only `Show` also ate
  the mod's own press.
- `ZInput.GetKeyDown(KeyCode.Question)` throws `ArgumentOutOfRangeException` ("key: None"), and the
  throw silently killed the rest of that frame's tick. Try a watched KeyCode once, drop it if it throws.
- `Object.Destroy` waits for the end of the frame: a loop of removes in one frame sees the world as
  it was at its start. A layout still counts a destroyed child; detach it with `SetParent(null, false)`.
- A second `LayoutGroup` on the same object makes `AddComponent` return null: remove the first with
  `DestroyImmediate`.
- Read global hotkeys in the plugin's `Update`, not in a placement patch, so they work without a tool.
- The first call of a heavy method in a session costs 50 to 60 ms of JIT (0.5 ms after). Time
  everything twice.
- `float.ToString("F4")` rounds the float's shortest text: widen to double before formatting.

---

## 4. Custom UI

The research in `.claude/research/` has the methods and patch targets; this is what ValheimTomrer
learned building a full window (a 3D pane, panels, dialogs) and HUD additions.

### Why text read grey [any]

TMP multiplies the label's colour by the material's face colour and then draws the material's
outline and shadow over the glyph. The HUD's hover-name material (`Valheim-AveriaSerifLibre`, size
18) is made for big white names over a dark world, so at 12 to 16 point every label reads grey
whatever colour it is given. Four rounds of "the text is grey" were this, not the colour constant.

`UiTheme` keeps two copies instead, both white-faced: `FontMaterial` plain for text on a panel,
`FontOutlined` with a black edge for text over the world. Guard every `SetFloat`/`SetColor` on a
copied TMP material with `HasProperty`: the distance-field shaders come in variants. Widths that
worked: 0.2 black outline under white text, 0.06 white edge around dark text (the owner found 0.2
"too thick" there). A copy of a loaded material is fine, nothing is on disk.

### TMP traps [any]

- A new `TextMeshProUGUI` shows nothing until `.font` is set. It also measures nothing before its
  Awake, and Awake waits for an active object: build widgets switched on, hide them afterwards.
- `GetPreferredValues(text)` wraps at the rect's default 100 px: pass a width.
- A `ContentSizeFitter` on an inner column does not grow the panel around it: put the layout group
  and the fitter on the panel. A canvas switched on this frame reports a zero rect.
- `enableAutoSizing` shrinks a wrapped label instead of clipping it.
- The font has ○ □ △ but no ✕ (U+2715 draws an empty box): use × (U+00D7). Check any new glyph on
  screen.

### The window recipe [window]

- The game's own (SessionPlayerList): `Canvas` with `overrideSorting`, sort order 950 (over the
  inventory and the store, under the centre messages, text entry at 1100, the pause menu and popups),
  `CanvasScaler` (ConstantPixelSize, reference pixels per unit 50) added before `GuiScaler` (it reads
  the scaler in Awake), `GraphicRaycaster`, `CanvasGroup` added before `UIGroupHandler` (priority 5).
  Parent: `Hud.instance.transform.parent` (IngameGui). GUI scale 1.8 on a 3456x2160 screen.
- The input takeover is nine patches on one flag (`InputBlockPatches.cs`), measured against 1.0.15.
  The research's first list had four; the other five were found one symptom at a time.
- Closing keeps blocking for one extra frame, then `PlayerController.SetTakeInputDelay(0.2f)`
  (ValheimTomrer also called `ZInput.ResetAllButtonStates()`), so the closing key never reaches the
  game.
- Everything built in the game scene dies on a world change: check with Unity's null and rebuild. A
  theme keyed on the live `Hud` object rebuilds itself with no patch. `SpriteAtlas.GetSprite` hands
  out a copy: destroy it on rebuild, or it leaks.
- UIAtlas has 247 sprites. The chrome used: `woodpanel_trophys`, `panel_bkg`,
  `woodpanel_400_tileable`, `button`, `button_highlight`, `button_pressed`, `text_field`,
  `item_background`, `sunken`. Draw them Sliced with `pixelsPerUnitMultiplier` 1. `sunken` has a
  see-through middle; `item_background` is pale, so a row with white text on it is tinted dark.
- A `Button` on a tinted image tints it again through `ColorTint`, and `CrossFadeColor` clamps to 1,
  so it can only darken: use `Transition.None`.
- uGUI sends `PointerClick` after a drag: swallow the click that ends a drag.
- `UITooltip` needs a game prefab: own buttons go without it.
- A camera rendering into a `RenderTexture` on a `RawImage` works for a 3D pane: size it to the
  pane's real pixels, make a new one on a GUI-scale change, release it on close.
- Closing hides the window and keeps its state (ValheimTomrer's owner wanted "open again finds it
  all as it was"); only the mouse is not kept.

### Text boxes [window]

A typing text box is the one widget the EventSystem really selects, so the game's own UI module
(`InputSystemUIInputModule`, which reads the real pad) sends it events: cross as Submit, circle as
Cancel, the D-pad as Move. A plain `TMP_InputField` fires `onSubmit` on Submit: ValheimTomrer's Save
as saved its first name and closed on one cross. On Cancel it stopped typing and put the old text
back before the mod's tick saw the press, which then closed the dialog too. So every box is a
`TextBox` that ignores all three; typed keys still work (Enter submits, Esc puts the text back).

Two more things threw a typing box out of the keyboard:

- The game fires `ZInput.OnInputLayoutChanged` on every switch between keyboard/mouse and pad. The
  build menu, alive behind any window, answers with `SetSelectedGameObject(null)`.
  `BuildUiOnLayoutChangedPatch` skips that while a window is up.
- The box reads its keys in the game's UI update, which may come before the mod's tick in the same
  frame. `ModUi.JustTyping` (typing now or last frame) makes that key the box's: an Esc the box took
  cannot also close the dialog, and the Enter that submitted a name cannot also press "Replace?".

On the pad, circle leaves a typing box and keeps its text, a D-pad step leaves it and steps on: the
pad has no keys to type with, so a box must never hold the pad.

### The game's own surfaces [hud]

- **Messages:** `MessageHud` TopLeft for results, Center for refusals (ValheimTomrer's convention).
  The top-left message sits 124 to 154 HUD units down; a status line at (28, -170) clears it.
- **The popup:** `UnifiedPopup` blocks all input with no patch: while the pause menu is shut,
  `Menu.IsVisible()` returns `UnifiedPopup.WasVisibleThisFrame()` (measured: `Player.TakeInput`
  false, W held 0.6 s moved 0 m). It has one row for two buttons. For three, push a popup of an own
  `PopupType` (100): the game shows panel, background and title and hides all its buttons; add copies
  of `buttonLeft`. Restore the width and buttons whenever it is not the popup on top. A copied
  button's hint reads `MISSING BUTTON DEF "ButtonB"` because the game re-translates only its own
  texts: set `$KEY_JoyButtonB` / `$KEY_JoyButtonA` yourself.
- **The key hint row** (`KeyHints`, bottom right, 1825 x 60 units): groups `BuildHints`,
  `CombatHints`, `InventoryHints`, `RadialHints`, `FishingHints`, `BarberHints`, each with a
  `Keyboard` and a `Gamepad` row. A keyboard entry (`Place`) is a TMP `Text` plus a `key_bkg` cap; the
  wheel icon is `mousew_icon`; a pad entry is one TMP with `<sprite="xbox" name="button_rt">`. Copy
  the game's entries; take pad texts from `Localization.GetBoundKeyString("JoyPlace")` and key names
  from `ZInput.KeyCodeToDisplayName`. While the keyboard is in use, the game's hidden pad row reads
  `MISSING BUTTON DEF "Place"`: compare pad icons only while `ZInput.IsGamepadActive()`.
- **The build card:** `Hud.SetupPieceInfo` runs every frame in place mode, so a postfix can fill
  it and has nothing to undo. `Hud.m_requirementItems` is 6 slots. `SelectedInfo` is 410 x 162.6 at
  scale 1.25, background `Bkg2` at 50 % black. In build mode the stamina and eitr bars move to y 320
  and 285, just over the card: put nothing above it.
- `InventoryGui.IsVisible()` stays true for one frame after hiding.
- Adding items shows "New item" / "New crafting recipe" popups, drawn above sort order 950.

---

## 5. The pad

The owner plays on a pad, so every feature works on one (the rule is in `CLAUDE.md`, Scope).

### In the world: the game's button names [pad]

- Reading the game's button objects (`ZInput.instance.GetButtonDef(name).Pressed`) follows the
  player's layout and keeps the D-pad's own repeat, and it skips the hold-back prefix, so the mod
  always sees its own press.
- "L2" is the game's modifier `JoyAltKeys`: L2 in the default layout, L1 in Alternative1. R2 is a
  trigger: read `JoyPlace` as an axis.
- **The layout is the player's, and it switches when a pad connects** (template, 23-09-2026). With no
  pad, `ZInput.InputLayout` read Default and circle was `JoyJump`. Once the test pad connected, the
  game applied the layout saved in its settings, Alternative2 on the owner's machine: cross is
  `JoyJump`, circle `JoyBuildMenu`, `JoyRadialClose` and (with L2) `JoyDodge`, R3 `JoyCrouch` and
  `JoyNextSnap` (with L2 `JoyAltPlace`), L1 `JoyRadial`, `JoyHide` and `JoyRemove`. The layout read
  Default in two runs and Alternative2 in a third before any pad input, so do not trust the value
  until a pad has connected. A test that assumed "circle jumps" failed. So tests press game buttons by name
  (`PressBound("JoyJump")`) and check a hold-back on every name bound to the button
  (`GamePad.NamesOf`), and `probe` lists the bindings with and without a pad.
- **Held back from the game** by binding path, not name: in the default layout circle is
  `JoyButtonB`, `JoyJump`, `JoyDodge` and `JoyRadialClose`; the D-pad is the hotbar, the forsaken
  power, camera and minimap zoom. A button stays held back until let go, so the jump read in the
  next FixedUpdate never sees the circle that closed something.
- **The closing press:** without the reset, the circle that closed ValheimTomrer's remove window was
  a 1.38 m jump in the next physics step.
- Hints in the game's own icon family: xbox, ps5, switch2 (`Localization.GetBoundKeyString`). The
  game's hints follow its glyph setting (Xbox with no real pad); icons picked from the pad's name can
  disagree. The xbox asset has `dpad_leftright`, the others may not: use two single icons.

### In a window [pad] [window]

- The game's UI runs on `InputSystemUIInputModule`, which reads every gamepad. A DualSense shows as
  `DualSenseGamepadHID`. Submit is Enter and cross, Cancel Esc and circle, Move the arrows, D-pad and
  left stick (Switch Pro swaps two face buttons).
- **Double fire:** a Button selected in the EventSystem gets pressed by the module and by the mod's
  own pad code in the same frame. ValheimTomrer's editor kept the selection null, pressed with
  `onClick.Invoke()`, and made grid tiles plain Images. The template's example lets the module own
  the pad instead and binds no cross itself. Pick one per window.
- When a window opens, the EventSystem already holds a vanilla selection (`Content/ALL`, a
  build-menu button): clear or set it, or the first cross presses that button.
- **The module owning the pad works** (template's `example_window`, 23-09-2026): the window sets the
  selection on open, buttons have explicit up/down links (`UiBuild.LinkColumn`), the mod binds no
  cross. Through the test pad (a real input device the module reads): the D-pad moved the selection,
  one cross pressed "Say hello" exactly once, circle closed it and no game button bound to circle saw
  the press.
- `UIGroupHandler`: when it is the highest active group and the pad is in exclusive use, it selects
  `m_defaultElement` if nothing is selected. Leaving `m_defaultElement` null is safe (it returns
  early). Its `CanvasGroup.interactable` is false while a higher group is up.
- The game's popup buttons use `UIGamePad` hotkeys (A = Yes, B = No) with navigation None. With
  three or more buttons, give explicit left/right navigation and keep `UIGamePad` only on Cancel.
- A pad cannot scroll a region that has nothing selectable: give the right stick the scroll.
- Ignore the pad while `Application.isFocused` is false (a real DualSense is read whenever the game
  window has focus).

### Sticks and look [pad]

- **Read raw** (`ReadUnprocessedValue`), then ZInput's radial dead zone (0.2, rescaled to 0..1), the
  way `ZInput.ReadValueDef` does. `ReadValue()` adds Unity's own stick filter, which the game sets to
  0.4 to 0.75: with it the stick did nothing up to half way and was at full speed by three quarters
  (raw 0.5 read 0.11, raw 0.7 read 0.82; the game reads 0.375 and 0.625). That was the "clunky" look.
- Unity reports stick up as positive: do not invert Y.
- The game's camera turns 110 degrees a second at full stick times
  `PlayerController.m_gamepadSens`, with its invert X and Y settings; the mouse 0.05 degrees a pixel
  times `m_mouseSens`. Use the game's statics; the settings screen writes them live. BepInEx keeps
  an unknown config line forever, so a removed setting needs its line dropped from the file.
- ValheimTomrer's own reader values: trigger down above 0.5 and up below 0.3; repeats 0.25 s then
  0.08 s for turning, 0.3 s then 0.1 s for menus.
- The game's wheel is raw scroll x 0.15 x `ZInput.GetScrollModifier()` (6.67 without precise
  scrolling), threshold 0.1. uGUI reports one notch as 1.
- Ctrl is the game's crouch key, so it is no world modifier; many Mac mice have no Mouse4 or Mouse5.

---

## 6. Unity objects, rendering and the world

**[any]**

- Shaders in the build, found with `Shader.Find` (ship none): `Standard` (lit), `Sprites/Default`
  (unlit, no ZTest), `Hidden/Internal-Colored` (real `_ZTest`), `Unlit/Color`.
- `Graphics.DrawMeshInstanced` needs a shader variant the built player may not have: instanced dots
  came out invisible. One mesh per colour worked.
- A mesh with no vertex colours reads as white in those shaders, so `_Color` alone tints it; many
  copies each get their own colour through a property block.
- **Glowing a world piece** the way `WearNTear.Highlight` does:
  `MaterialMan.instance.SetValue(go, ShaderProps._Color, c)` and `_EmissionColor` (c x 0.4), then
  `ResetValue` both. Every exit path (cancel, mod off, window open, world change) must reset, or the
  piece glows for good. Cost: 400 pieces 11.6 to 12.6 ms the first time, 1.7 to 2.6 ms again; it
  grows faster than linear, so re-apply only what changed. The hammer's own hover highlight resets
  the aimed piece 0.2 s after the aim leaves: re-apply on pieces aimed at in the last 2 s.
- Ghost materials are nearly opaque: a change of alpha alone does not show. Tint instead (the
  game's red is `Piece.SetInvalidPlacementHeightlight`).
- `GetComponentsInChildren` skips inactive objects, so bounds measured under a switched-off root
  come out zero. Hide with each renderer's `forceRenderingOff` instead.
- Textures cannot be read directly: blit through a temporary `RenderTexture`.
- World positions about 400 m from the origin are good to about 1e-4 m.
- Reading prefab assets loads nothing on demand. Walking all 398 hammer pieces (components, bounds,
  colliders, snaps) takes 36 ms.

**[build]** Prefab copies

- Instantiate under `ZNetView.m_forceDisableInit = true` and `TerrainOp.m_forceDisableTerrainOps =
  true` (as `Player.SetupPlacementGhost` does), then strip joints, rigidbodies, lights, audio,
  particles, `WispSpawner`, `CircleProjector` and `CraftingStation`.
- **Instantiate with no parent, then `SetParent`.** Under a switched-off parent the copy's `ZNetView`
  woke later, after `m_forceDisableInit` was off, and became a real saved piece: the test world held
  140 such pieces 8000 m down, and the ghost lost its model two frames later.
- `CircleProjector` spawns 80 `Circle_section(Clone)` children in its Update, after the strip: destroy
  the component.
- A second scene on a free layer: clear the bit on the player camera (`GameCamera.Awake`), and patch
  `EnvMan.Awake` too, or the world's sun lights it. `Camera.Render()` on your own camera must put
  `RenderSettings` fog and ambient back after the call.

---

## 7. The autotest

### Why a harness in the game

There is no other way to know a Valheim mod works than to run the game. The harness plays a
scripted session in Debug builds: its own character and world in `.devtest/saves`, real input,
PASS/FAIL lines, screenshots, then quit. ValheimTomrer's `editor_all` grew to 24 scenarios and 1425
checks in 12 minutes, and caught most regressions before the owner saw them. It also missed what
only a person notices: its editor shipped at 519 passes, and the owner found four problems on first
use. Screenshots read by the agent close part of that gap.

### What made runs flaky, and the fixes

| Cause | Fix |
|---|---|
| A greydwarf wandered in and hit the structure under test (17 creatures at spawn on one run) | `AutoTestPeace`: no AI (`BaseAI.UpdateAI` prefix), no spawns, no raids, despawn once |
| Each scenario moved the character, the next build spot was a metre off, a piece lost support | one shared spot, fixed facing, levelled flat with a `TerrainOp` (meadows roll 1 to 2 m over 16 m) |
| The test character is saved on quit: its hammer wore down to 0, then build mode never started | `EquipHammer` repairs it every time |
| A teleport into water: swimming put the hammer away | the spot search takes dry meadow only; re-equip after every teleport |
| One run left a setting on in the player's real config; later scenarios saw 397 pieces, not 49 | `ResetSettings` between scenarios and at the end |
| Leftover files in the player's real folders changed a result | point every file folder the mod uses at `.devtest/` in tests |
| The test world keeps junk between runs (145 objects under -1000 m from old bugs) | compare counts before and after, never absolute counts |
| A pad press one frame early acted on the old aim; 7 checks after it failed | wait for a condition, not a fixed number of frames |
| The template's seed AUTOTST1 had no 16 m square under 1 m of rise within 400 m: the spot search fell back to (0, 0, 0), in the sea | the coarse filter allows 3 m (the levelling does the rest), the search goes to 600 m, and it fails loudly instead of teleporting to a default |
| A test assumed circle is the jump; on the owner's machine (Alternative2) cross is | press game buttons by name, never by a guessed pad button |
| One-off failures (4 checks once, a terrain edge once) | re-run once before believing a single failure |

### Real input

- Keys: `InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key))`, hold 4 frames,
  release. `backgroundBehavior = IgnoreFocus` so keys land while the game window is not in front.
- `MouseState.WithButton` changes the struct it is called on, so a "release" built from the same
  variable kept the button down. Build each state on its own.
- The pad: a device of the test's own (`InputSystem.AddDevice<Gamepad>("AutoTestPad DualSense")`),
  which ZInput and the game's UI module both read. `InputSystem.FindControl("<Gamepad>/...")` can
  pick a real DualSense instead. A generic Gamepad name gets xbox icons.
- A fake state fed to the mod's own reader (ValheimTomrer's `PadReader.Fake`) is invisible to the
  game's UI module, so a double fire can only be checked with a real device.

### Screenshots

The world differs by about 39 of 255 per byte between identical runs, so pixels are not compared.
The agent reads the PNG and says what it sees. `autotest.sh` deletes every PNG first.

### Guards that prove themselves

ValheimTomrer's art guard failed when it saw zero of the files it expected, proven by pointing it at
the wrong folder. A guard that can pass on an empty walk proves nothing.

### Running it

- A world load takes about 40 s; the first run generates the test world.
- `dotnet build` auto-deploys over the DLL a running game has loaded: never build during a run.
- bash reads a script as it goes: editing `autotest.sh` during a run killed it with a syntax error.
- `pgrep -f autotest.sh` in a wait loop matches the loop itself: wait on output files instead.

---

## 8. Publishing

- `zip.sh` follows Thunderstore's rules (wiki.thunderstore.io/mods/creating-a-package): the files at
  the zip's root, `manifest.json` with `name` (`[A-Za-z0-9_]`), `version_number` (x.y.z),
  `website_url`, `description` (250 characters at most), `dependencies`; `icon.png` 256x256;
  README.md; CHANGELOG.md.
- Thunderstore asks mods made with AI to say so: the csproj's `AI_Assisted_Creation` and
  `AI_Model_Vendor` assembly metadata, and a line in the README.
- The icon is the one image the art guard allows (that one path). `zip.sh` deletes its staged copy
  after zipping, or the repo walk would find it in `thunderstore/build/`.
- `zip -X` leaves out macOS extra attributes.
- The Release DLL must not hold the autotest: `zip.sh` greps it for `AutoTestPeace`.
- README GIFs: `readme_gifs` records JPG frames at a fixed game step (`Time.captureFramerate`), so a
  clip is smooth however slow the capture; `make-gifs.py` keeps unchanged 8 x 8 blocks from the last
  frame (the world shimmers) and makes one palette per clip. Needs Pillow, numpy and ffmpeg.

---

## 9. Plans, sub-agents and process

What ValheimTomrer's five phased plans (19 to 23-09-2026) taught about running them:

- **The orchestrator never reads source files.** It reads the plan, the last handoff entry and each
  sub-agent's report (15 lines at most), fires one sub-agent per phase, one at a time, runs
  `dotnet build` itself and commits each green phase.
- **The handoff block** per phase, at most 40 lines: Files added/changed, Public API the next phase
  will call, Decisions made, Measured, Gotchas found, Plan was wrong, and the exact test command and
  result.
- **Phase 0 measures.** The plan lists the facts already checked in the code, so the probe does not
  measure them again. Each later prompt carries a "context from earlier phases, do not re-measure"
  block and names the neighbour scenarios to re-run.
- **Every plan had wrong facts** (the table below). The sub-agent fixes the fact, proves it with a
  test and logs it. A test that asserts what the plan said, when the game says otherwise, is worse
  than no test.
- **Claims in a prompt can be false.** "The region scrolls" (it never did), "if phase 3 already did
  this" (it had not). Verify before building on them.
- If a test fails twice for the same reason, stop and report. Do not widen the scope. A bug outside
  the phase is reported with a one-line fix and gets its own follow-up.
- A round shipped "build only", without the chain, left 9 failing checks for the next task. Run
  `all` before calling a round done, or say plainly that it was not run.
- "The text is still grey" was fixed wrong three times: the cause was the material, not the colour.
  Find the mechanism, and look at the screen after each change.
- For visual work, show the owner the screenshot and wait for an OK before the next phase.
- Don't rewrite a finished handoff: add an "Overturned later" table at its end.
- Check the branch base before Phase 0 (one feature branch sat on the wrong base).
- Every report lists what was not checked by hand (a real pad, other layouts) and states deviations
  from the ask ("Differs from the ask: nothing").
- Locked decisions go in the plan's Decisions table and are not reopened; a change mid-plan is
  recorded there with the reason.

### What ValheimTomrer's plans got wrong (general rows)

| Plan said | Truth |
|---|---|
| Only the player camera needs the layer cleared | the sun lights every layer: patch `EnvMan.Awake` too |
| `piece_workbench` is a ground piece | no hammer piece has `m_groundPiece` in 1.0 |
| Instanced dots with `DrawMeshInstanced` | invisible (shader variant missing); one mesh per colour |
| A float 0.00005 formats as 0.0001 | it is just under half, so 0: widen to double first |
| `ZDOVars.s_items` is a string | a byte array |
| Cart and karve holds via `GetComponent<Container>` | `GetComponentInChildren`; the hold has no `Piece` |
| Read a station's range with `GetStationBuildRange()` | throws on a copied station; read `m_buildRange` |
| Status text at (28, -116) | overlaps the game's "Built X" line; (28, -170) |
| Re-apply every glow each refresh | only what changed, plus pieces aimed at in the last 2 s: 5 to 7 times cheaper |
| Build a ghost under a switched-off root | bounds skip inactive renderers; use `forceRenderingOff` |
| Remove is read with `GetButtonDown` | `GetButtonUp`; on this Mac it is Left Command |
| Put extra UI above the build card | the stamina and eitr bars move there in build mode |
| `grep -rn ... src/ \| grep -v "src/Dev/"` | macOS prints `src//Dev/`; name the folders |
| Nothing is selected when a window opens | a vanilla button (`Content/ALL`) is |
| Text is grey because of the colour constant | the shared HUD TMP material |
| A click on a 3D pane may capture the mouse | it hid the cursor and swung the view; moved to a key |
| The pad need not reach the panels, or the capture | overturned twice: every feature works on a pad |
| "If phase 3 already did this" | it had not: check claims about earlier phases |

---

## 10. This mod

<The mod's own decisions start here, one numbered section per area. Each rule in CLAUDE.md that is
about this mod points to its section.>
