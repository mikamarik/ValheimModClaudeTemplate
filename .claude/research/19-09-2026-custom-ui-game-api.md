# Custom UI: game API reference

Date: 19-09-2026. Game: Valheim 1.0.15. Overview and recommendations: [19-09-2026-custom-ui.md](19-09-2026-custom-ui.md).
Generalized for the template on 23-09-2026 from ValheimTomrer's research and what it verified in game.

**How to read this file**

- `File.cs:123` points into the decompiled game code (`assembly_valheim`). `guiutils/` and `utils/`
  point into `assembly_guiutils` and `assembly_utils`. To get the code again:
  `ilspycmd -p -o <dir> "$MANAGED/assembly_valheim.dll"`.
- "Scene data" means the fact was read from the game's asset files (scene layout, component
  settings), not from code.
- **(guess)** marks what was inferred and not read directly.
- **(verified in ValheimTomrer)** marks what was built and run there. **(corrected: ...)** marks
  where the first research was wrong. The rest has not been run in the game.
- Code samples assume the publicizer, so private members are reachable. The template publicizes
  `assembly_valheim`, `assembly_utils` and `assembly_guiutils`.
- `$mymod_...` in the samples stands for your own localization keys.

## Contents

1. [Show info without a window](#1-show-info-without-a-window)
2. [Own windows](#2-own-windows)
3. [Style kit: fonts, sprites, sounds, tooltips](#3-style-kit)
4. [Localization](#4-localization)
5. [Network safety](#5-network-safety)

---

## 1. Show info without a window

| Surface | What the player sees | Effort |
|---|---|---|
| `MessageHud.ShowMessage` | Short fading text, top left or center | Easy |
| `MessageHud.QueueUnlockMsg` | Card with icon, title and text | Easy |
| `MessageHud.ShowBiomeFoundMsg` | Big title in the middle | Easy |
| Hover text patch | Extra lines under the crosshair | Easy |
| Status row icon (display only) | Icon next to Rested, Cozy... | Easy to Medium |
| Event banner / action bar takeover | Top banner, or a labelled progress bar | Easy |
| Own element under `Hud.m_rootObject` | Anything | Medium |
| `Chat.SetNpcText` | Speech bubble over an object | Easy |
| `DamageText.AddInworldText` | Rising number or text | Easy |
| Own label that follows a world point | Label or bar over a position | Medium |
| Map pin + radius circle | Icon and circle on the map | Easy to Medium |
| `TextViewer.ShowText` | Parchment text window | Easy |
| Compendium page | Page in Inventory > Texts | Easy |
| Sleep text | Our line during sleep | Easy |
| Key hints row | Our controls in the game's row, bottom right | Medium |
| Console command | `mymod` in console, `/mymod` in chat | Easy |
| The build card | Our name, icon, text and widgets on the game's card in build mode | Easy to Medium |
| Radial menu slot | Slot in the gamepad wheel | Hard, not worth it |

### 1.1 Messages (`MessageHud`)

```csharp
// MessageHud.cs:156
public void ShowMessage(MessageType type, string text, int amount = 0, Sprite icon = null,
                        bool showDespiteHiddenHUD = false, bool log = true)
// MessageHud.cs:289
public void QueueUnlockMsg(Sprite icon, string topic, string description)
// MessageHud.cs:281
public void ShowBiomeFoundMsg(string text, bool playStinger)
```

**`MessageType.TopLeft`:**
- The queue has no size limit.
- At most one new message appears per second. Each fades out in 4 s.
- The same text and icon within 4 s merge into "xN".
- The line sits about 124 to 154 reference pixels down from the top of the screen (measured in
  ValheimTomrer). Own top-left HUD text goes below it, for example at y = -170.

**`MessageType.Center`:**
- One slot. A new message replaces the old one at once.
- Fades out in 4 s.

**Unlock cards:**
- At most 4 are visible at once.
- When the queue empties, the game adds "N new logs" if `m_unlockMsgCount > 0`. To avoid that line,
  lower `m_unlockMsgCount` by one after each of our cards.

**Other behaviour:**
- **Hidden HUD:** every message is dropped unless `showDespiteHiddenHUD` is true.
- **Log:** messages also go into Inventory > Texts > Log, capped at 50 entries.
- **Shortcut:** `Player.m_localPlayer.Message(type, text)` does the same thing on the local player
  (`Player.cs:5388`). (verified in ValheimTomrer with `MessageType.Center`)

```csharp
var mh = MessageHud.instance;
mh.ShowMessage(MessageHud.MessageType.Center, "$mymod_saved");
mh.ShowMessage(MessageHud.MessageType.TopLeft, "$mymod_low", 0, piece.m_icon);
mh.QueueUnlockMsg(piece.m_icon, "$mymod_unlocked", "$mymod_unlocked_text");
mh.m_unlockMsgCount--;
```

### 1.2 Hover text

**How it works:**
- `Hud.UpdateCrosshair` (`Hud.cs:818`) runs every frame.
- It sets `m_hoverName.text = hoverable.GetHoverText()`, where the hoverable is
  `player.GetHoverObject()?.GetComponentInParent<Hoverable>()`.
- When the text is not empty, the crosshair turns yellow.

**Key hints:**
- Write them exactly like vanilla: `[<color=yellow><b>$KEY_Use</b></color>] text`.
- With a gamepad, the game rewrites this pattern into a button icon (`Hud.cs:835`).
- `$KEY_X` becomes the key name, or the gamepad glyph for `JoyX`.

**Hover detection:**
- Hover objects are found within 5 m (`Player.FindHoverObject`, `Player.cs:4260`).
- In build mode there is no hover object. Use `GetHoveringPiece()` there.

**Patch points** (all are `string GetHoverText()`):

| Class | File | Note |
|---|---|---|
| `Fireplace` | `Fireplace.cs:336` | Returns empty for fires with infinite fuel |
| `Bed` | `Bed.cs:22` | |
| `CraftingStation` | `CraftingStation.cs:175` | |
| `Chair` | `Chair.cs:25` | Empty for 2 s after sitting |
| `StationExtension` | `StationExtension.cs:49` | |
| `Container`, `Door`, `Sign` | | |
| Pieces with no hover text (walls, rugs, banners) | | Postfix `Hud.UpdateCrosshair` instead |

```csharp
[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetHoverText))]
static class CraftingStationGetHoverTextPatch
{
    static void Postfix(ref string __result)
    {
        // also return early when the mod's enabled setting is off
        if (string.IsNullOrEmpty(__result)) return;
        __result += Localization.instance.Localize("\n<color=#9fd3ff>$mymod_info 3</color>");
    }
}
```

**Notes:**
- These run every frame, so keep them cheap.
- In a `Hud.UpdateCrosshair` postfix the gamepad rewrite has already run. Apply it yourself if you
  add `$KEY_` text there.

### 1.3 Status row icon (display only)

**How it works:**
- `SEMan.GetHUDStatusEffects(List<StatusEffect>)` (`SEMan.cs:328`) feeds both the HUD icon row and
  Inventory > Texts > "Active effects".
- A postfix can add a cached `ScriptableObject.CreateInstance<StatusEffect>()` with `m_name`,
  `m_icon` and `m_tooltip`.
- Only when `__instance.m_character == Player.m_localPlayer`.

**What it is not:**
- It is not in the real effect list, so it has no gameplay effect.
- It is not saved and not sent. It is a runtime `StatusEffect` object only.

**Notes:**
- The text under the icon comes from `GetIconText()`. Postfix it for custom text.
- The HUD only rebuilds the row when the **number** of icons changes (`Hud.cs:1638`).
- Easier variant: postfix `SE_Cozy.GetIconText` (`SE_Cozy.cs:39`). It already shows "Comfort: N"
  under the Resting icon.

### 1.4 HUD elements

**Root:**
- `Hud.m_rootObject` (`Hud.cs:28`) holds the HUD.

**Hiding (Ctrl+F3, cutscenes):**
- The game does not disable the root. It moves it to x = 10000 (`Hud.SetVisible`, `Hud.cs:436`).
- Anything we put under it hides with the HUD at no extra cost. (verified in ValheimTomrer)
- `Hud.IsUserHidden()` (`Hud.cs:1752`) tells whether the player hid the HUD.

**Build point:**
- A postfix on `Hud.Awake` (`Hud.cs:379`).
- Or build lazily the first time it is needed, and again when `Hud.instance` is a new object.
  (verified in ValheimTomrer, which does this)
- The Hud is rebuilt on every world load. (verified in ValheimTomrer)

**Good parts to copy (all fields on `Hud`):**

| Field | What it is |
|---|---|
| `m_hoverName` | Crosshair text. It already renders gamepad glyphs, so copy this rather than making a new text. Measured: font `Valheim-AveriaSerifLibre`, material `Valheim-AveriaSerifLibre - Outline`, size 18. |
| `m_eventName`, `m_actionName`, `m_healthText` | Text styles |
| `m_actionBarRoot` + `m_actionProgress` | Bar with a label |
| `m_pieceHealthRoot` + `m_pieceHealthBar` | Bar |
| `m_eventBar` | Top banner |
| `m_statusEffectTemplate` | Status icon: "Icon", "TimeText", a name, and an Animator with trigger "flash" |

**Bars:**
- Bars are `GuiBar` (`guiutils/GuiBar.cs`).
- Methods: `SetValue(0..1)`, `SetMaxValue`, `SetColor`, `ResetColor`, `SetWidth`.

**Takeover tricks:**

| Target | Patch | How |
|---|---|---|
| Event banner | `Hud.UpdateEvent` (`Hud.cs:1695`) | Postfix. If the banner is off, turn it on with our text. Raids and bosses still win. |
| Action bar | `Hud.UpdateActionProgress` (`Hud.cs:803`) | Postfix. Fill `m_actionBarRoot` / `m_actionProgress` when vanilla left it hidden. Do not fake `Player.GetActionProgress`, it throws. |

```csharp
[HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
static class HudAwakePatch
{
    static void Postfix(Hud __instance)
    {
        var go = new GameObject("MyMod_Status", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(__instance.m_rootObject.transform, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);   // top left corner of the screen
        rt.anchoredPosition = new Vector2(28, -170);                      // under the top-left messages
        rt.sizeDelta = new Vector2(320, 60);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = __instance.m_hoverName.font;
        t.fontSharedMaterial = ownMaterial;   // corrected: not m_hoverName's own material, see 3.1
        t.fontSize = 18;
        t.raycastTarget = false;
    }
}
```

(verified in ValheimTomrer: a label anchored top left under `m_rootObject` sits in the screen's
top-left corner, so the root covers the screen.)

### 1.5 Text in the world

**a) Speech bubble** (`Chat.cs:679`):

```csharp
public void SetNpcText(GameObject talker, Vector3 offset, float cullDistance, float ttl,
                       string topic, string text, bool large)
```
- One bubble per object. A new call replaces the old one.
- `ttl <= 0` means it stays until cleared.
- Hidden past `cullDistance` or when the HUD is hidden.
- Clear with `ClearNpcText(go)`.
- The raven uses this.
- To label a bare spot, use an empty local GameObject.

**b) Rising text** (`DamageText.cs:94`, private):

```csharp
private void AddInworldText(TextType type, Vector3 pos, float distance, string text, bool mySelf)
```
- Rises and fades in 1.5 s.
- `TextType.Bonus` is orange, 1.5× size, 3 s. `Heal` adds a green "+".
- Check `Hud.IsUserHidden()` and distance yourself; vanilla does that in the public method.
- **Never call the public `ShowText`.** It sends an RPC.

```csharp
var cam = Utils.GetMainCamera();
if (cam && !Hud.IsUserHidden())
    DamageText.instance.AddInworldText(DamageText.TextType.Bonus, pos,
        Vector3.Distance(cam.transform.position, pos), "+1", false);
```

**c) Chat-style floating text** (`Chat.AddInworldText`, `Chat.cs:376`, private):
- Shows for 5 s.
- Sticks to the screen edge when the point is off screen.
- Use a made-up negative sender id and `Talker.Type.Normal`.

**d) Own label that follows a world point.** The `EnemyHud` pattern (`EnemyHud.cs:260`):
- Every `LateUpdate`, set `rt.position = Utils.GetMainCamera().WorldToScreenPointScaled(worldPos)`.
- Hide it when `z < 0`.
- Parent it under `EnemyHud.instance.m_hudRoot` (it hides with the HUD).
- Copy `EnemyHud.m_baseHud` for a name plus bar.
- Text drawn over the 3D world needs an edge (see 3.1), or it vanishes on a bright sky.

**e) Ground ring:**
- Copy `CraftingStation.m_areaMarker` (a `CircleProjector`). Set `m_radius`.
- (guess) It has no network component.
- It is a runtime copy of a game object, not a registered prefab.

### 1.6 Map pins

```csharp
// Minimap.cs:2327
public PinData AddPin(Vector3 pos, PinType type, string name, bool save, bool isChecked,
                      long ownerID = 0L, PlatformUserID author = default)
```

**Types:** `PinType`: `Icon0..Icon4` (fire, house, hammer, dot, portal (guess)), `EventArea`, `Boss`,
and others.

**Always pass `save: false`:**
- The pin is never saved and never shared.
- The player cannot delete it by right-click.

**Radius circle:**
- `AddPin(pos, PinType.EventArea, "", false, false)`, then `m_worldSize = radius * 2`.
- Vanilla event areas do the same (`Minimap.cs:1366`).

**Remove:** `RemovePin(PinData)`.

**Fields you can change on `PinData`:** `m_icon`, `m_name`, `m_pos`, `m_doubleSize`, `m_animate`
(pulses), `m_worldSize`.

**Pitfalls:**
- **Wiped on load.** The first map update after world load calls `ClearPins()`. Add pins only after
  `Minimap.instance.m_hasGenerated`.
- **Icon changes.** The icon is copied when the UI marker is created. If the marker already exists,
  also set `pin.m_iconElement.sprite`.
- **Color.** `UpdatePins` resets the color to white every frame. Tinting needs a postfix there.
- **Names** show only on the large map when zoomed in.

```csharp
var mm = Minimap.instance;
if (!mm || !mm.m_hasGenerated) return;
var pin = mm.AddPin(spot, Minimap.PinType.Icon0, "$mymod_spot", save: false, isChecked: false);
var area = mm.AddPin(spot, Minimap.PinType.EventArea, "", false, false);
area.m_worldSize = radius * 2f;
```

### 1.7 Text windows

**Parchment window** (`TextViewer.cs:94`):
`TextViewer.instance.ShowText(TextViewer.Style.Raven, topic, text, autoHide)`.
- It blocks player input.
- It closes on Use or Esc, or after walking 3 m if `autoHide` is true.
- Use `Style.Raven`. `Style.Rune` reloads all language files and leaves the rune line empty for mod
  text.

**Compendium page:**
- Postfix the private `TextsDialog.UpdateTextsList()` (`TextsDialog.cs:207`).
- In it: `__instance.m_texts.Insert(0, new TextsDialog.TextInfo("$mymod_title", report))`.
- Text is localized when shown. Nothing is saved.

**Sleep text:** postfix `DreamTexts.GetRandomDreamText` to return our own line.

**Raven hint:**
- Add a `Tutorial.TutorialText` to `Tutorial.instance.m_texts`, then call
  `Player.m_localPlayer.ShowTutorial(name)`.
- Downsides:
  - it is off if the player disabled tutorials;
  - the raven needs a flat spot to land;
  - "seen" is saved in the character file.

### 1.8 Key hints row (bottom right)

**What exists:**
- `KeyHints` (`KeyHints.cs`) holds one group per situation: `m_buildHints`, `m_combatHints`,
  `m_fishingHints`, `m_inventoryHints`, `m_inventoryWithContainerHints`, `m_barberHints`,
  `m_radialHints`.
- `KeyHints.UpdateHints` switches the groups on and off every frame.
- `m_keyHintsEnabled` is the player's "key hints" setting. Off means no row.
- The rows are TMP texts such as
  `"$hud_cyclesnap <mspace=0.6em> $KEY_PrevSnap / $KEY_NextSnap</mspace>"`.
- Inside `m_buildHints` (verified in ValheimTomrer): children `Keyboard` and `Gamepad`, two
  right-aligned rows. The keyboard row has an entry `Place` (children `Text` and `key_bkg`), a `+`
  text in its Copy entry and an image with sprite `mousew_icon` in its Rotate entry. The pad row has
  a TMP text `Text - Place`.

**Adding a row to a vanilla group:** copy a row into `m_buildHints`. (guess) The layout group places
it. Not tested. Texts with `$KEY_` must be localized again on `ZInput.OnInputLayoutChanged`, which
fires when the player switches between keyboard and pad.

**Own set in the row** (verified in ValheimTomrer, which shows its own controls there):
- Make our own group under `KeyHints`, next to `m_buildHints`: same parent, sibling index + 1, same
  anchors, pivot, position, size and scale. Put copies of its `Keyboard` and `Gamepad` rows in it,
  emptied.
- Each entry is a copy of a game entry (`Place`, `key_bkg`, `+`, `mousew_icon`, `Text - Place`), so
  font, size, colour, material and key caps match. Never build look-alikes.
- Prefix `KeyHints.UpdateHints`. While our set shows: switch the game's groups off once and return
  false. Letting the game's update run switches its groups on and off every frame (it re-runs
  `UIInputHint.OnEnable` and a layout rebuild each frame). When our set ends, let it run again: it
  sets every group as usual, nothing to undo.
- Which row: the pad row while `ZInput.IsGamepadActive()`, the keyboard row while not and
  `ZInput.IsMouseActive()` (the game's own `UIInputHint` rule). Check it every frame.
- Show our set only where the game would show its row: `m_keyHintsEnabled`, the player alive,
  `!Game.IsPaused()`, no chat window (`Chat.instance.IsChatDialogWindowVisible()`), no inventory or
  its panels (`InventoryGui.IsVisible()`, `IsSkillsPanelOpen`, `IsTrophisPanelOpen`,
  `IsAchievementsPanelOpen`, `IsTextPanelOpen`), no radial (`Hud.instance.m_radialMenu.Active`), no
  build menu (`Hud.IsPieceSelectionVisible()`), no barber (`PlayerCustomizaton.IsBarberGuiVisible()`,
  the game's spelling).
- Pad entry text, the way the game writes its own: `$"{label} <mspace=0.6em> {pad}</mspace>"`.
- `pad` comes from `Localization.instance.GetBoundKeyString("JoyUse", emptyStringOnMissing: true)`:
  the icon tag in the family of the pad in hand (xbox, ps5, switch2) and in the player's layout. Two
  buttons held together: `A + B`. Either of two: `A / B`.
- Keyboard names: `GetBoundKeyString` for the game's own buttons (`"Mouse-1"`),
  `ZInput.KeyCodeToDisplayName(key)` for our own config keys. Wrap the second in `try`: it can throw.
- The game's labels auto-size, 18 at most. A copy laid out before it has room stays small. Switch
  `enableAutoSizing` off and set `fontSize = fontSizeMax`.
- When to write the texts again: build a cheap key each frame from `ZInput.InputLayout`,
  `ZInput.CurrentGlyph` and `ZInput.ConnectedGamepadType`; read the rest once a second, so a rebound
  key shows within a second. Our copies are not in the game's list of texts it localizes again.
- Pad buttons that differ by layout: the build menu is `JoyUse`, or `JoyBuildMenu` when
  `ZInput.IsNonClassicFunctionality()`. Rotate is `JoyRotate` + `JoyRStick` in the default layout,
  `JoyRotate` / `JoyRotateRight` in the others.

### 1.9 Console command

```csharp
// Terminal.cs:152
new Terminal.ConsoleCommand(string command, string description, ConsoleEvent action,
    bool isCheat = false, bool isNetwork = false, bool onlyServer = false, bool isSecret = false,
    bool allowInDevBuild = false, bool hideBehindDevCommands = false,
    ConsoleOptionsFetcher optionsFetcher = null, bool alwaysRefreshTabOptions = false,
    bool remoteCommand = false, bool onlyAdmin = false)
```

**Registering:**
- Register it in a postfix on the private static `Terminal.InitTerminal`. A vanilla command with the
  same name would overwrite ours.
- `isCheat` needs `devcommands`.
- Never set `remoteCommand`.

**Where it runs:**
- Non-cheat commands also work in chat as `/mymod`.
- The console must be enabled first (Settings > Gameplay, or `-console`).

**Printing:**
- `args.Context.AddString(text)`.
- `Chat.instance.AddString(text)` does not open the chat window. Set `Chat.instance.m_hideTimer = 0`
  to show it for 10 s.

```csharp
[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
static class TerminalInitTerminalPatch
{
    static void Postfix() => new Terminal.ConsoleCommand("mymod", "MyMod: status",
        (Terminal.ConsoleEvent)(args => args.Context?.AddString("MyMod: ...")));
}
```

### 1.10 Radial menu

- **What exists:** `Hud.m_radialMenu` (`Valheim.UI.RadialBase`). The main ring has 8 fixed slots
  (`ValheimRadialConfig.cs:13`).
- **How to add:** replace an empty slot in a `RadialBase.ConstructRadial` prefix, or open our own
  `IRadialConfig` with `m_radialMenu.Open(cfg)`.
- **Verdict:** possible, but fragile and gamepad-focused. **Not worth it** for status info.
- `IHasHoverMenu` (Fireplace, Smelter...) only opens an "use item on this" wheel. It is not a general
  context menu.

### 1.11 The build card (verified in ValheimTomrer)

The card at the bottom of the screen in build mode shows the selected piece. A mod can show its own
content there.

- `Hud.SetupPieceInfo` runs every frame in place mode. A postfix can set:
  - `m_buildSelection.text` (the name)
  - `m_pieceDescription.text`
  - `m_buildIcon.enabled`, `m_buildIcon.sprite`
  - `m_snappingIcon.enabled`
  - hide the six requirement slots: each `GameObject` in `m_requirementItems`, `SetActive(false)`
- **Nothing to undo.** The game sets its slots again every frame, so a normal piece gets them back.
  The postfix only hides its own widgets.
- Return early while `Hud.IsPieceSelectionVisible()`: the build menu shows its hovered piece on the
  same card.
- **Finding the card:** walk up from `m_requirementItems[0]` to the object whose parent is
  `m_buildHud`. That is the card (`SelectedInfo`). Its background is its child `Bkg2` (black at
  50 %). The card is scaled 1.25.
- A child of the card hides with the HUD (Ctrl+F3).
- **Room:** do not grow up over the card. In build mode the game moves the stamina and eitr bars to
  y 320 and 285 above it.
- Keep heavy work out of the every-frame postfix: work numbers out at most twice a second, write
  only the texts that changed.

---

## 2. Own windows

### 2.1 Where the game UI lives (scene data)

**In the world:**
- Path: `_GameMain/LoadingGUI/PixelFix/IngameGui/<window>`.
- Each window is its own `Canvas` with overridden sort order, plus `CanvasScaler` and `GuiScaler`.
- `Hud.instance.transform.parent` is `IngameGui`. (verified in ValheimTomrer)

**Main menu:** `GuiRoot/GUI/StartGui/...`.

**Canvas sort orders in the world** (higher draws on top and gets clicks first):

| Order | Window |
|---|---|
| 100 | DamageText |
| 200 | EnemyHud |
| 300 | Chat (world texts) |
| 400 | HUD |
| 500 | TopLeftMessage |
| 600 | Inventory |
| 700 | Store |
| 800 | Barber |
| 900 | Chat box |
| **950** | **suggested for our window** (verified in ValheimTomrer) |
| 1000 | HudMessage (center messages) |
| 1100 | TextInput |
| 1200 | TextViewer |
| 1400 | Tutorial |
| 1700 | Menu (Esc) |
| 1900 | UnifiedPopup |
| 2000 | Radial |

Jötunn uses 2000, which puts its windows over vanilla popups.

**Other settings (scene data):**
- Vanilla canvases use `CanvasScaler` in constant pixel size mode, with reference pixels per unit
  **50**. Measured on the HUD: screen overlay, order 400, constant pixel size, 50, with `GuiScaler`.
- They are on layer 5 (UI).
- They use `additionalShaderChannels = TexCoord1 | Normal | Tangent`.

### 2.2 Create a canvas

The game does this itself in `SessionPlayerList.cs:60`. (verified in ValheimTomrer, this exact
recipe)

```csharp
static GameObject CreateRoot(string name, int order)
{
    var parent = Hud.instance.transform.parent;      // IngameGui. Main menu: FejdStartup.instance.transform
    var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
    go.SetActive(false);
    go.transform.SetParent(parent, false);
    var rt = (RectTransform)go.transform;
    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
    rt.offsetMin = rt.offsetMax = Vector2.zero;
    var c = go.AddComponent<Canvas>();
    c.renderMode = RenderMode.ScreenSpaceOverlay;
    c.overrideSorting = true;
    c.sortingOrder = order;
    c.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
        | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
    var scaler = go.AddComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
    scaler.referencePixelsPerUnit = 50f;
    go.AddComponent<GuiScaler>();          // must come after CanvasScaler (reads it in Awake)
    go.AddComponent<GraphicRaycaster>();
    go.AddComponent<CanvasGroup>();        // must come before UIGroupHandler (reads it in Awake)
    go.AddComponent<UIGroupHandler>().m_groupPriority = 5;
    return go;
}
```

**Rules:**
- **Own `Canvas` is required.** Since game version 0.220.3, UI placed under `IngameGui` without its
  own `Canvas` does not show.
- **`GuiScaler`** (`guiutils/GuiScaler.cs`) sets `scaleFactor = min(width/1920, height/1080) *
  GuiScale` every frame. Design at 1920×1080.
- **Lifetime.** The canvas dies with the scene on logout. Create it lazily, check it with the Unity
  null check, and destroy it in the plugin's `OnDestroy` for hot reload.
- **A dim backdrop** (a full-screen `Image` in black at about 65 %) as the first child dims the world
  and eats clicks that miss the window. (verified in ValheimTomrer)
- **Build a widget after its root is switched on**, or TMP measures nothing and the layout comes out
  wrong. (verified in ValheimTomrer)

### 2.3 Block player input

There is no global "block input" switch. Each system checks a list of "is a window open" methods.

**What each check blocks** (1.0.15 line numbers):

| Game code | `TextInput.IsVisible` true | `StoreGui` visible | `Chat.HasFocus` | `Minimap.IsOpen` | `Menu.IsVisible` |
|---|---|---|---|---|---|
| `PlayerController.TakeInput` (walk, look) `:222` | blocks | gamepad only | blocks | gamepad only | blocks |
| `Player.TakeInput` (attack, use, hotbar keys, build) `:2668` | blocks | blocks | blocks | blocks | blocks |
| `GameCamera.UpdateMouseCapture` (cursor) `:185` | frees | frees | no | frees | no (checks `Menu.IsActive`) |
| `GameCamera.UpdateCamera` (wheel zoom) `:255` | **no** | blocks | blocks | blocks | blocks |
| `InventoryGui.Update` (Tab) `:526` | **no** | no | blocks | blocks | blocks |
| `HotkeyBar.Update` (gamepad D-pad) `:42` | **no** | blocks | blocks | blocks | blocks |
| `Menu.Update` (Esc opens menu) `:388` | blocks | blocks | blocks (via `m_wasFocused`) | blocks | n/a |
| `Chat.Update` (Enter) `:191` | blocks | no | n/a | no | blocks |
| `Minimap.Update` (M) `:695` | blocks | no | blocks | n/a | blocks (checks `Menu.IsActive`) |

Only `Menu.Show` pauses the game. Faking `Menu.IsVisible` does not pause.

**The patch set** (verified in ValheimTomrer; the template's kit has it in `src/Ui/ModUi.cs` and
`src/Patches/InputBlockPatches.cs`):

(corrected: the first plan was the four patches `TextInput.IsVisible`, `InventoryGui.Show`,
`HotkeyBar.Update` and the wheel. ValheimTomrer ships nine. The extra five block each effect directly
instead of through the `TextInput` check alone, let the mod skip the pause menu on a frame it used
Esc for itself, and let the window lock the cursor on purpose, for mouse look.)

```csharp
internal static class ModUi
{
    static int _closedFrame = -10;
    public static bool Open;
    public static bool LockCursor;   // the window wants mouse look: cursor locked and hidden
    // true for one extra frame after closing, so one Esc press does not also open the menu
    public static bool Blocking => Open || Time.frameCount - _closedFrame <= 1;
    public static void MarkClosed() { Open = false; LockCursor = false; _closedFrame = Time.frameCount; }
}

internal static class InputBlockPatches
{
    // Attack, use, hotbar keys, building.
    [HarmonyPatch(typeof(Player), "TakeInput")]
    static class PlayerTakeInput
    { static void Postfix(ref bool __result) { if (ModUi.Blocking) __result = false; } }

    // Walking and looking. In 1.0 it takes a bool.
    [HarmonyPatch(typeof(PlayerController), "TakeInput", new[] { typeof(bool) })]
    static class PlayerControllerTakeInput
    { static void Postfix(ref bool __result) { if (ModUi.Blocking) __result = false; } }

    // The check most of the game already honours: pause menu, map key, chat, zoom, crosshair.
    [HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
    static class TextInputIsVisible
    { static void Postfix(ref bool __result) { if (ModUi.Blocking) __result = true; } }

    // Tab. InventoryGui does not look at TextInput.
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show), new[] { typeof(Container), typeof(int) })]
    static class InventoryGuiShow { static bool Prefix() => !ModUi.Blocking; }

    // Pad D-pad up uses a hotbar item (HotkeyBar.cs:66).
    [HarmonyPatch(typeof(HotkeyBar), "Update")]
    static class HotkeyBarUpdate { static bool Prefix() => !ModUi.Blocking; }

    // Esc and the pad's menu button open the pause menu. Also skip it on any frame
    // the mod uses Esc outside the window (ValheimTomrer: a capture mode, its own popup).
    [HarmonyPatch(typeof(Menu), "Update")]
    static class MenuUpdate { static bool Prefix() => !ModUi.Blocking; }

    // The map key.
    [HarmonyPatch(typeof(Minimap), "Update")]
    static class MinimapUpdate { static bool Prefix() => !ModUi.Blocking; }

    // The game locks the cursor again every LateUpdate. Runs after it decided.
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    static class GameCameraUpdateMouseCapture
    {
        static void Postfix()
        {
            if (!ModUi.Blocking) return;
            if (ModUi.LockCursor) { ZCursor.LockState = CursorLockMode.Locked; ZCursor.Hide(); return; }
            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
        }
    }

    // Camera zoom. The private method: the public static wrapper is small enough to be inlined.
    // Our ScrollRects read the wheel through the EventSystem, not ZInput, so they still scroll.
    [HarmonyPatch(typeof(ZInput), "Internal_GetMouseScrollWheel")]
    static class ZInputMouseScrollWheel
    { static void Postfix(ref float __result) { if (ModUi.Blocking) __result = 0f; } }
}
```

After a game update, check that all nine targets still exist. A missing one is a `HarmonyException`
in the log, and shows in game as "the game still reacts while the window is open".

**Other options:**

| Approach | Used by | Notes |
|---|---|---|
| Postfix `Player.TakeInput` and `PlayerController.TakeInput` to false; postfix `GameCamera.UpdateMouseCapture` to unlock the cursor | Jötunn | Says each effect explicitly |
| Transpiler that inserts the check into `InventoryGui.Update` and `GameCamera.UpdateCamera` | Jötunn | Instead of our prefix and scroll patch |
| Set `StoreGui.m_instance.m_hiddenFrames = 0` every frame | ColorfulPieces | Blocks zoom and hotbar too. Side effects: the trader greeting, the Esc menu. |

**Walk while the window is open** (like the inventory):
- Do not patch `TextInput.IsVisible`.
- Instead:
  - Postfix `PlayerController.InInventoryEtc` to true, and `Player.TakeInput` to false.
  - Add a `GameCamera.UpdateMouseCapture` postfix for the cursor.
  - Add a `Menu.Show` prefix and a map-key guard.
- With a gamepad, also block `PlayerController.TakeInput`, because the stick drives the UI.

**Useful helpers:**

| Helper | Where | What it does |
|---|---|---|
| `ZInput.ResetButtonStatus(name)` | `utils/ZInput.cs:2321` | Uses up a button until it is let go. (verified in ValheimTomrer: stops the jump after a popup closed on circle) |
| `ZInput.ResetAllButtonStates()` | `:2329` | Resets all buttons. (verified in ValheimTomrer, on close) |
| `ZInput.IgnoreMouseInputForFrames(n)` | `:2683` | Ignores the mouse for n frames |
| `PlayerController.SetTakeInputDelay(seconds)` | `PlayerController.cs:252` | Ignores player input for a short time after closing. (verified in ValheimTomrer, 0.2 s) |
| `ZInput.TryGetButtonState(name, ...)` (private) | `utils/ZInput.cs` | Every `GetButton`, `GetButtonDown` and `GetButtonUp` goes through it, in Update and FixedUpdate. A prefix that returns false hides one game button from the game, for example while our own pad combo holds it. (verified in ValheimTomrer) |

**Timing:**
- `ZInput.GetKeyDown(KeyCode)` is the same for every script in a frame.
- `ZInput.GetButtonDown(name)` is computed in `Game.Update`, so our `Update` may see it one frame
  late. The one-frame grace covers this.
- The jump is read in `FixedUpdate`. A pad press that closed our UI in
  `Update` still reaches it in the next physics step, unless it is reset. (verified in ValheimTomrer)

### 2.4 Cursor

- **Wrapper:** `ZCursor` (`utils/ZCursor.cs`) wraps `Cursor`.
- **Re-applied every frame:** `GameCamera.UpdateMouseCapture` runs every `LateUpdate` and locks and
  hides the cursor unless a known window is open. Setting `Cursor.visible` once gets overwritten, so
  free the cursor through a patch or a vanilla check (see 2.3). (verified in ValheimTomrer: its
  postfix sets `ZCursor.LockState` and calls `ZCursor.Show()` or `Hide()` every frame)
- **Gamepad:** the system cursor stays hidden even when unlocked. That is intended: gamepad UI moves
  the selection instead.
- **Clicks:** a locked cursor gets no UI hover or clicks.
- **Ctrl+F1** toggles mouse capture (`GameCamera.cs:187`).
- **A click must never lock the cursor.** Lock it only on a clear request (a key), and give it back
  on Esc. Otherwise a click to select something hides the cursor. (verified in ValheimTomrer)

### 2.5 What Esc does when several things are open

Each system handles Esc itself. The pause menu opens only when nothing else is "visible". The
one-frame grace in each check stops a double action.

1. Console closes.
2. TextInput closes.
3. Inventory closes its sub-panels first (trophies, skills, texts, split...), then itself.
4. Large map drops to the small map.
5. Store closes.
6. Build menu closes (favorites dropdown first).
7. Radial closes.
8. UnifiedPopup: its No/Ok button has Esc bound.
9. Esc menu open: closes its confirm dialog first, else closes itself.
10. Nothing open: the Esc menu opens (`Menu.cs:389`).

**Inside our window, one Esc steps back one thing** (verified in ValheimTomrer): a typing text box
first, then an open dialog, then anything smaller the window holds, and only then the window itself.

(corrected: the first research only guessed that a popup closed with Esc may also open the Esc menu
in the same frame. ValheimTomrer saw it happen with its own popup. Fix: skip `Menu.Update` on the
frame the popup closed; see 2.8.)

### 2.6 Open and close pattern

This copies `StoreGui.Update` (`StoreGui.cs:67`). (verified in ValheimTomrer; its window reads the
pad's circle through its own pad reader instead of `JoyButtonB`)

```csharp
// When may the window open? The same checks close it again in Update.
static bool CanOpen()
{
    var p = Player.m_localPlayer;
    if (!p || p.IsDead() || p.InCutscene() || p.IsTeleporting() || !Hud.instance) return false;
    if (Menu.IsVisible() || Console.IsVisible() || TextInput.IsVisible() || UnifiedPopup.IsVisible()) return false;
    return !Chat.instance || !Chat.instance.HasFocus();
}

void Open()
{
    Hud.HidePieceSelection();
    if (InventoryGui.IsVisible()) InventoryGui.instance.Hide();
    if (StoreGui.IsVisible()) StoreGui.instance.Hide();
    if (Minimap.instance && Minimap.instance.m_mode == Minimap.MapMode.Large)
        Minimap.instance.SetMapMode(Minimap.MapMode.Small);
    UITooltip.HideTooltip();
    _root.SetActive(true);
    ModUi.Open = true;
}

void Update()
{
    if (!ModUi.Open) return;
    var p = Player.m_localPlayer;
    if (!p || p.IsDead() || p.InCutscene() || p.IsTeleporting()) { Close(); return; }
    // InventoryGui.IsVisible() stays true for 1 frame after Open() hid it: don't close on it
    // The console, chat and popups handle their own Esc. Let them have it first.
    if (Console.IsVisible() || (Chat.instance && Chat.instance.HasFocus()) || UnifiedPopup.IsVisible()) return;
    // A text box that typed this frame or last frame owns Esc (see 2.7).
    if (typingNowOrLastFrame) return;
    if (ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))
    {
        ZInput.ResetButtonStatus("JoyButtonB");
        Close();
    }
}

void Close()
{
    _root.SetActive(false);
    ModUi.MarkClosed();
    UITooltip.HideTooltip();
    PlayerController.SetTakeInputDelay(0.2f);
    ZInput.ResetAllButtonStates();
}
```

**Closing may keep state.** ValheimTomrer hides the canvas on close and keeps everything in it (the
open file, selection, tabs, a dialog), so the next open is instant. Only a world load kills the
canvas. Hiding is `SetActive(false)` on the root.

### 2.7 Gamepad

**EventSystem (scene data, measured in ValheimTomrer):**
- Path: `_GameMain/EventSystem`, with `InputSystemUIInputModule`.
- The UI action map moves the selection with the D-pad, left stick and arrow keys (and a joystick's
  stick).
- Submit is `*/{Submit}` (Enter, the pad's south button). Cancel is `*/{Cancel}` (the pad's east
  button). On a Switch Pro pad the two are swapped (`buttonEast` submits, `buttonSouth` cancels).
- A DualSense shows up as `DualSenseGamepadHID`.

**Two traps with a real pad:**
- **Something is already selected** (verified in ValheimTomrer). When our window opens, the EventSystem still has a vanilla
  button selected (measured: a build menu button, `Content/ALL`). Clear it with
  `EventSystem.current.SetSelectedGameObject(null)`, or the first cross or Enter presses it.
- **One press, two actions.** The module reads the real `Gamepad.current`. If our code also reads
  the pad and one of our buttons is selected in the EventSystem, one cross press fires twice.
  (ValheimTomrer is built around this; the double press itself was not checked by hand.) Pick one:
  - let the module drive: select our buttons, set explicit navigation, bind nothing else to cross;
  - or own the focus: keep the EventSystem selection null for buttons, keep our own focused index,
    draw our own highlight, and call `onClick.Invoke()` on cross. ValheimTomrer does this. The one
    exception is a text box: typing needs the EventSystem, so a box is selected for real while it
    types.

**Text boxes on a pad** (verified in ValheimTomrer; the template's kit has the box in
`src/Ui/UiBuild.cs`):
- A typing box is the one widget the module really selects, so it gets a real pad's cross as Submit,
  circle as Cancel and the D-pad as Move. A plain `TMP_InputField` fires `onSubmit` on cross, and on
  circle stops typing and puts the old text back before our code sees the press.
- Fix: a subclass that ignores all three. Typed keys are not these events, so Enter still submits
  and Esc still puts the text back.

```csharp
internal sealed class TextBox : TMP_InputField
{
    public override void OnSubmit(BaseEventData eventData) { }
    public override void OnCancel(BaseEventData eventData) { }
    public override void OnMove(AxisEventData eventData) { }
}
```

- Leaving a box on the pad: circle stops typing and keeps the text; a D-pad or stick step stops
  typing and moves on. The pad has no keys, so a box must never hold the focus.
- Start typing on its own only when the keyboard or mouse was used last (`ZInput.IsGamepadActive()`
  false). From the pad, wait for cross.
- `ZInput.OnInputLayoutChanged` fires on every switch between keyboard/mouse and pad, and when a pad
  is plugged in. The build menu (`BuildUi`, alive behind our window) answers with
  `SetSelectedGameObject(null)`, which throws a typing box out of the keyboard. Prefix
  `BuildUi.OnLayoutChanged` to skip it while our window is open (still close
  `m_favoritesDropdown`). The template's kit: `src/Patches/BuildUiOnLayoutChangedPatch.cs`.
- A box reads its keys in the game's UI update, which may run before our `Update` in the same frame.
  Treat "typing now or last frame" as typing: check the selected object's `TMP_InputField.isFocused`
  each frame and remember the frame. Else the Esc that stopped the typing also closes the dialog, and
  the Enter that submitted a name also presses the next dialog's default button.

**`UIGroupHandler`** (`guiutils/UIGroupHandler.cs`):
- Only the active group with the highest `m_groupPriority` is interactable. The others get
  `CanvasGroup.interactable = false`.
- When a gamepad is in use and nothing is selected, it selects `m_defaultElement`.
- `m_defaultElement` may stay null: the handler then returns early. (verified in ValheimTomrer)
- Add the `CanvasGroup` **before** the `UIGroupHandler`; it reads the group in `Awake`.

**Vanilla priorities (scene data):**

| Group | Priority |
|---|---|
| Inventory | 1 |
| Inventory sub-panels | 2 |
| Esc menu | 9 |
| Menu confirm dialogs | 11 |
| UnifiedPopup | 100 |
| **Our window (suggested)** | **5** (verified in ValheimTomrer) |

**`UIGamePad`** (`UIGamePad.cs`):
- A per-button hotkey: `m_keyCode` for the keyboard, `m_zinputKey` for the gamepad (for example
  `"JoyButtonB"`), and `m_hint` (an object shown while a gamepad is active).
- It fires only if its group is the active one.

**Navigation helpers:**
- `GuiUtils.SetNavigationLeft`, `SetNavigationRight`, `SetNavigationVertical` and the other
  `GuiUtils.SetNavigation*` (`guiutils/GuiUtils.cs`). Set `navigation.mode = Explicit` first.
  (verified in ValheimTomrer for rows and columns of buttons)
- `GridNavigationUtility.ConfigureExplicitNavigation` (`GridNavigationUtility.cs:10`)
- `ScrollRectEnsureVisible.CenterOnItem`

**(guess, test in game):** a selected button that also has a `UIGamePad` bound to `JoyButtonA` may
fire twice. Do not bind A on selectable buttons.

**Reading the pad yourself** (verified in ValheimTomrer): see the
[platform file, section 1](19-09-2026-custom-ui-platform-and-mods.md#1-input) for sticks, dead zones,
look speed and the game's modifier button. Controller icons for hints: 3.1.

### 2.8 Popups (`UnifiedPopup`)

**Works in the world and in the main menu.** In the world it lives at `.../IngameGui/UnifiedPopup`
(order 1900).

**While it is open** (through `Menu.IsVisible`):
- it blocks the player;
- it frees the cursor;
- it gives gamepad focus to the popup.

It does not pause the game. No input patch is needed for it. (verified in ValheimTomrer)

| API | Where |
|---|---|
| `UnifiedPopup.Push(PopupBase)` / `Pop()` | `UnifiedPopup.cs:149, 158` |
| `IsVisible()`, `WasVisibleThisFrame()`, `SetFocus()` | `:214, :223, :184` |

| Type | Constructor |
|---|---|
| `YesNoPopup` | `(header, text, yesCallback, noCallback, localizeText = true, coverBackground = false)` |
| `WarningPopup` | `(header, text, okCallback, localizeText = true)` |
| `TextEntryPopup` | `(header, text, placeholderText, cancelCallback, validationFunc, retrieveResultFunc, localizeText = true)` |
| `TaskPopup` | `(header, text, localizeText = true)`, no buttons |
| `CancelableTaskPopup` | `(headerFunc, textFunc, shouldCloseFunc, cancelCallback)`, closes itself when `shouldClose` is true |

**Callbacks never close the popup.** Call `UnifiedPopup.Pop()` yourself.

**Keys:**

| Button | Keys |
|---|---|
| Yes | Enter / A |
| No | Esc / B |
| Ok | Esc / A / Enter |

```csharp
UnifiedPopup.Push(new YesNoPopup("$mymod_title", "$mymod_are_you_sure",
    () => { UnifiedPopup.Pop(); DoIt(); }, UnifiedPopup.Pop));
UnifiedPopup.Push(new TextEntryPopup("$mymod_name", "", "name", UnifiedPopup.Pop,
    s => !string.IsNullOrWhiteSpace(s) && s.Length <= 20,
    s => { UnifiedPopup.Pop(); Rename(s); }));
UnifiedPopup.SetFocus();
```

The built-in types above are not tested yet.

**Own popup type, own buttons** (verified in ValheimTomrer, a popup with three choices):

The game's popups have one row of two buttons. For more, push a popup of a type the game does not
know. The game then shows its panel, dark background and title, and none of its buttons.

```csharp
sealed class MyPopup : PopupBase
{
    public override PopupType Type => (PopupType)100;   // none of the game's types
}

var popup = UnifiedPopup.instance;
UnifiedPopup.Push(new MyPopup());
popup.headerText.text = "Remove this?";
popup.bodyText.text = "...";
// Buttons: copies of popup.buttonLeft (the game's No), on its parent panel.
```

- **Buttons:** copy `popup.buttonLeft` onto its parent panel, give each a new `onClick`, and widen
  the panel to fit (the game's is 400 units wide). Make the copies once per popup object; a world
  load makes a new popup.
- **Put it back:** while our popup is not the top of `popup.popupStack` (closed, or a game popup
  pushed over it), hide our copies and restore the panel's width and its `UIGroupHandler`'s
  `m_defaultElement`.
- **Pad:** explicit navigation along the row (the module moves it with the D-pad and left stick).
  Select the first button only when `ZInput.IsGamepadActive()`. With the mouse select nothing, or
  Enter presses a button the player never pointed at. Start on Cancel, as the game's "are you sure"
  dialogs start on No.
- **Esc and circle:** keep the copied `UIGamePad` only on the Cancel copy (it keeps Esc and
  `JoyButtonB`). Remove it from the others with `DestroyImmediate`, or they answer the same keys.
- **Hint icon text:** the copied `gamepad_hint` text reads `MISSING BUTTON DEF "ButtonB"`. Set it
  from `Localization.instance.Localize("$KEY_JoyButtonB")` (or `$KEY_JoyButtonA`). The game only
  localizes its own texts again.
- **The closing press must not reach the game:**
  - Circle is the jump. After the popup closed, the next physics step saw it and the player jumped
    1.4 m. Fix: `ZInput.ResetButtonStatus(name)` for every game button bound to the pad's cross or
    circle (walk `ZInput.instance.m_buttons`), as `InventoryGui` does.
  - The Esc that closed it opened the pause menu in the same frame. Fix: skip `Menu.Update` on that
    frame.
- Cancel the popup yourself when the player dies or teleports.

### 2.9 Text entry (`TextInput`, like signs)

- **Call:** `TextInput.instance.RequestText(TextReceiver receiver, string topic, int charLimit)`
  (`TextInput.cs:84`).
- **Interface:** `TextReceiver { string GetText(); void SetText(string text); }`.
- **When our code hears back:**
  - OK or Enter calls `SetText`.
  - Cancel or Esc hides the window with **no callback**. Poll `TextInput.IsVisible()` to notice a
    cancel.
- **Order:** the TextInput canvas is order 1100, so our window must sort below it to stay under it.
- Our own text box inside our window (2.7) is the tested way. This one is not tested yet.

### 2.10 Copying vanilla windows and buttons

**Best templates:**

| Template | Field | Gives you |
|---|---|---|
| Texts dialog | `InventoryGui.instance.m_textsDialog` | List on the left, text area on the right, title, `woodpanel_texts`, close button with gamepad hint. Good for a journal or a long list. |
| Store panel | `StoreGui.instance.m_rootPanel` (not the Store_Screen root) | Scroll list with list-item prefab, big action button, coin panel. Its `Update` is the behaviour model. |
| Esc menu entry | `Menu.instance.m_settingsButton` | Knot-style menu button |
| Small button | `InventoryGui.instance.m_takeAllButton` | Plain button (used by QuickStackStore) |
| Popup button | `UnifiedPopup.instance.buttonLeft` | The game's No button (verified in ValheimTomrer, see 2.8) |
| Key hints entries | children of `KeyHints.m_buildHints` | Key caps, `+`, wheel icon, pad text (verified in ValheimTomrer, see 1.8) |

**Pitfalls when copying:**
1. **Button listeners.** Listeners set in the Unity editor are copied; listeners added in code are
   not. Replace `onClick` with a new `Button.ButtonClickedEvent()`.
2. **Craft button.** It has an `EventTrigger` that still points at `InventoryGui`. Destroy it.
3. **Gamepad hotkeys.** `UIGamePad` keys and hints are copied, so the copy fires on the same gamepad
   key. Turn the source's `UIGamePad` off while copying, then clear or change the key, or remove it.
   (verified in ValheimTomrer)
4. **Colors.** `ButtonTextColor` / `ButtonImageColor` store colors in `Awake` and reset them every
   frame. Changing colors after copying does nothing.
5. **Text.** Copies hold already-translated text and do not update on language change. Set text
   with `Localization.instance.Localize("$key")`. A copied gamepad hint can read
   `MISSING BUTTON DEF "..."` until you set it. (verified in ValheimTomrer)
6. **Singletons.** Never copy an object that carries a singleton controller (`StoreGui`,
   `InventoryGui`, `TextInput`, `Menu`). Its `Awake` overwrites the static instance. Copy a child
   instead.
7. **Tooltips.** `UITooltip` draws under the nearest parent `Canvas`. Its delay uses
   `Time.deltaTime`, so no tooltips while paused.
8. **Auto-size.** Game labels often auto-size. A copy laid out before it has room stays small.
   Switch `enableAutoSizing` off and set `fontSize = fontSizeMax`. (verified in ValheimTomrer)
9. **Removing children.** `Destroy` waits until the end of the frame, and a layout group still counts
   the child until then. Switch it off and detach it (`SetParent(null, false)`) before `Destroy`.
   (verified in ValheimTomrer)

```csharp
static Button CloneButton(Button template, Transform parent, string label, UnityAction onClick)
{
    var srcPad = template.GetComponent<UIGamePad>();
    bool was = srcPad && srcPad.enabled;
    if (srcPad) srcPad.enabled = false;
    var b = Object.Instantiate(template, parent, false);
    if (srcPad) srcPad.enabled = was;
    b.onClick = new Button.ButtonClickedEvent();          // drops editor listeners too
    b.onClick.AddListener(onClick);
    foreach (var et in b.GetComponents<EventTrigger>()) Object.Destroy(et);
    var pad = b.GetComponent<UIGamePad>();
    if (pad) { pad.m_zinputKey = ""; pad.m_keyCode = KeyCode.None; pad.enabled = true; }
    b.GetComponentInChildren<TMP_Text>(true).text = Localization.instance.Localize(label);
    return b;
}
```

### 2.11 Adding to existing menus

| Target | Possible? | How |
|---|---|---|
| Esc menu | Yes | Postfix `Menu.Start` (`Menu.cs:120`). Copy `m_settingsButton` into `menuEntriesParent`. Also postfix the private `Menu.UpdateNavigation` (`:148`); it rebuilds the gamepad up/down order from a fixed list, so add our button there. On click: `Menu.instance.Hide()` (this unpauses), then open our window. |
| Main menu | Yes | Postfix `FejdStartup.Awake`. Copy an entry in `m_menuList`, then refresh `m_menuButtons = m_menuList.GetComponentsInChildren<Button>()` for keyboard and gamepad navigation. |
| Settings tab | Yes, medium effort (guess) | Prefix `Settings.Awake`. Add a `TabHandler.Tab` whose page has our component implementing `ISettingsTab` (`Initialize`, `OnTabOpen`, `OnOkAsync`...). |
| Inventory | Yes | Copy `m_takeAllButton` or an info-panel button. New crafting tabs: no, that logic is fixed in code. |
| Build menu tab | Probably (guess) | Prefix `BuildUi.Awake`: copy a tab into `m_tabContainer` and add it to `m_tabHandler.m_tabs`. Postfix: add our own `IPieceList`. The list only holds `Piece` objects. |

None of these is tested yet.

### 2.12 Pause

- **API:** `Game.Pause()`, `Unpause()`, `IsPaused()` (`Game.cs:1221`).
- **Single player only:** pause works only with no other players.
- **Who pauses:** only the Esc menu and cinematics. The inventory, map, store and popups do not.
- **Our window can pause.** Caveats:
  - `Menu.Hide` calls `Unpause()`.
  - Physics stops.
  - Use `Time.unscaledDeltaTime` in our UI.
  - Mouse tooltips never appear while paused.

---

## 3. Style kit

### 3.1 Fonts (TextMeshPro)

| Font asset | Used for (scene data) |
|---|---|
| `Valheim-AveriaSerifLibre` | Headers, buttons (popup header 32, recipe name 32) |
| `Valheim-AveriaSansLibre` | Body text (popup body 22, recipe text 17) |
| `Valheim-Norsebold` | Window titles (32) |
| `Valheim-Norse`, `Valheim-Prstartk`, `Valheim-Rune` | Other |

**Material presets:**
- `Valheim-AveriaSerifLibre - Outline`
- `- Outline (Thin)`
- `- Outline Thick`
- `- Underlay`

**How to get them:**
- **Best:** copy `.font` from a live vanilla text, for example `Hud.instance.m_hoverName` or
  `InventoryGui.instance.m_recipeName`. Measured: `m_hoverName` is `Valheim-AveriaSerifLibre` with
  material `Valheim-AveriaSerifLibre - Outline`, size 18. (verified in ValheimTomrer, which takes its
  font there; the template's kit does this in `src/Ui/UiTheme.cs`)
- **Or:** `Resources.Load<TMP_FontAsset>("Fonts & Materials/Valheim_Fonts/Valheim-AveriaSerifLibre")`.
  - This works even at plugin start.
  - It is a **second copy**, not the object the game UI uses, so `FindObjectsOfTypeAll` can return
    two with the same name.
- **Always set a font.** The default TMP font is empty, so a new `TextMeshProUGUI` with no font shows
  nothing.
- Letters outside plain ASCII render (for example `ø`). (verified in ValheimTomrer)

**The material: make our own copies** (corrected: the first research said to copy
`.fontSharedMaterial` too):
- TMP multiplies the label's colour by the material's face colour, then draws the material's outline
  and shadow over the glyph. The hover-name material is made for big white names over a dark world:
  a fat black outline and a soft shadow. At 12 to 16 point this eats the strokes, and every label
  reads grey whatever colour it is given.
- Make our own copies at runtime, with a white face, and destroy them when the `Hud` is rebuilt (the
  template's kit does this in `src/Ui/UiTheme.cs`):
  - **plain**, for text on a panel: no outline, no shadow, no glow, face dilate 0.05;
  - **outlined**, for text over the 3D world or a picture: black outline 0.2, face dilate 0.1, shadow
    (underlay) black at 65 %, offset (0.5, -0.5), dilate 0.1, softness 0.2.
- The distance-field shaders come in variants, so check `HasProperty` before each set.

```csharp
var mine = new Material(Hud.instance.m_hoverName.fontSharedMaterial) { name = "MyModText" };
var none = new Color(0, 0, 0, 0);
void Set(int id, Color c) { if (mine.HasProperty(id)) mine.SetColor(id, c); }
void SetF(int id, float f) { if (mine.HasProperty(id)) mine.SetFloat(id, f); }
Set(ShaderUtilities.ID_FaceColor, Color.white);
Set(ShaderUtilities.ID_GlowColor, none);
SetF(ShaderUtilities.ID_GlowPower, 0f);
SetF(ShaderUtilities.ID_FaceDilate, 0.05f);
Set(ShaderUtilities.ID_OutlineColor, none);
SetF(ShaderUtilities.ID_OutlineWidth, 0f);
Set(ShaderUtilities.ID_UnderlayColor, none);
mine.DisableKeyword(ShaderUtilities.Keyword_Glow);
mine.DisableKeyword(ShaderUtilities.Keyword_Outline);
mine.DisableKeyword(ShaderUtilities.Keyword_Underlay);
```

**Gamepad glyphs** (verified in ValheimTomrer; the first research only guessed here):
- The game's controller icons are in a TMP sprite asset named **`gamepad_glyphs`**. The game's
  strings hold tags like `<sprite="gamepad_glyphs" name="button_a">`. The tag the game builds for a
  given pad may name another asset per pad family, so do not write tags by hand: use the calls
  below.
- Names (found by scanning the game DLL's UTF-16 strings): `button_a`, `button_b`, `button_x`,
  `button_y`, `button_cross`, `button_circle`, `button_square`, `button_triangle`, `button_l1`,
  `button_l2`, `button_l3`, `button_r1`, `button_r2`, `button_r3`, `button_lb`, `button_rb`,
  `button_lt`, `button_rt`, `button_start`, `button_options`, `dpad`, `dpad_up`, `dpad_down`,
  `dpad_left`, `dpad_right`, `lstick`, `rstick`.
- The loaded sprite atlases also include `xbox` (36 sprites), `PS5-Atlas` (18) and `Switch2-Atlas`
  (46) (measured).
- **In a text:** the easiest way to get the right tag is `Localization.instance.GetBoundKeyString("JoyX")`
  or `Localize("$KEY_JoyX")`. Both follow the pad in hand and the player's layout. A copy of the
  game's pad hint text renders them (verified in ValheimTomrer); a copy of `m_hoverName` or a text
  made from scratch is not tested.
- **As a `Sprite`** (for an `Image` next to a key cap): find the asset in
  `Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>()` by name, read `spriteCharacterTable`, take
  `(character.glyph as TMP_SpriteGlyph).sprite`. When a glyph has no sprite, cut one out of
  `asset.spriteSheet` with `Sprite.Create(sheet, glyph.glyphRect, ...)` and destroy it later. Needs
  `UnityEngine.TextCoreFontEngineModule`. Also look in `spriteInfoList` and
  `fallbackSpriteAssets`. The template's kit does this in `src/Ui/PadGlyphs.cs`.

### 3.2 Sprites (UIAtlas)

**Where they are:**
- All UI chrome is in the SpriteAtlas **`UIAtlas`**: 246 names, pixels per unit 50, 9-slice borders
  set. (Measured in game: 247 entries, because `health_icon_valknut_0` is there twice.)
- Its asset bundle is loaded in the main menu, in the world and on the loading screen.
- Other atlases in the world: `IconAtlas` (1513 item and piece icons), and the pad glyph atlases
  (3.1).

**How to get one (best first):**
1. Copy `.sprite` from a live vanilla `Image`, for example `StoreGui.instance.m_buyButton.image.sprite`
   for `button`.
2. Look the atlas up and ask it for the sprite (verified in ValheimTomrer; the template's kit does
   this in `src/Ui/UiTheme.cs`):
   ```csharp
   var atlas = Resources.FindObjectsOfTypeAll<UnityEngine.U2D.SpriteAtlas>().FirstOrDefault(a => a.name == "UIAtlas");
   var sprite = atlas.GetSprite("woodpanel_trophys"); // returns a new copy each call: cache it
   ```
   `GetSprite` hands out a copy, not the atlas entry. It is ours: destroy it when the UI is rebuilt,
   or it leaks. (verified in ValheimTomrer) Jötunn does this first.
3. `Resources.FindObjectsOfTypeAll<Sprite>()` by name. This only finds sprites something has already
   loaded, so it can miss some.

**Use them like vanilla:**
- `Image.type = Sliced`, `pixelsPerUnitMultiplier = 1`.
- Canvas `referencePixelsPerUnit = 50`.
- With the default 100, the borders draw at the wrong size.

**Measured sizes** (pixels, border left/bottom/right/top, all 50 pixels per unit):

| Sprite | Size | Border |
|---|---|---|
| `woodpanel_trophys` | 562×329 | 10, 14, 11, 10 |
| `button` | 140×38 | 7 on each side |
| `text_field` | 140×38 | 5 on each side |

**Sprite traps** (verified in ValheimTomrer):
- `sunken` has a see-through middle. Put a flat dark `Image` under it, or the panel behind shows
  through.
- `item_background` is pale. White text on a row made of it is white on white: tint it dark (about
  `(0.17, 0.14, 0.11, 0.94)`). Icon-only tiles can keep it as it is.
- Game wood behind light text reads better tinted to about 70 % grey.

**Most useful names:**

| Group | Sprites |
|---|---|
| Wood panels | `woodpanel_512x512` (popups), `woodpanel_trophys` (Jötunn default), `woodpanel_settings`, `woodpanel_texts`, `woodpanel_password` (text input), `woodpanel_flik`, `woodpanel_info`, `woodpanel_large`, `woodpanel_container`, `woodpanel_crafting`, `woodpanel_320x320`, `woodpanel_highres`, `woodpanel_400_tileable` |
| Plain panels | `panel_bkg`, `panel_bkg_128`, `panel_bkg_256`, `panel_border_128`, `panel_interior_bkg_128`, `panel_separator`, `crafting_panel_bkg`, `sunken` |
| Buttons | `button`, `button_highlight`, `button_pressed`, `button_disabled`, `button_glow`, `button_small` (+ `_highlight`, `_pressed`, `_disabled`), `button_tab` (+ `_hover`, `_selected`, `_disabled`) |
| Inputs | `text_field` (+ `_highlight`, `_disabled`), `checkbox`, `checkbox_marker`, `selection_frame` |
| Slots | `item_background`, `item_background_sunken`, `item_bkg` |
| Decoration | `BraidLineHorisontalMedium` (+ `Fat`, `FatShort`, `HalfL`, `HalfR`), `BraidKnotMedium`, `darken_blob`, `top_darken`, `bottom_Darken`, `gradient`, `bar_gradient` |
| Icons | `hammer_icon`, `craft_icon`, `repair`, `rotate_left`, `trash_icon`, `refresh_icon`, `save_icon`, `warning_icon`, `exclamation_mark`, `check_yes`, `x_no`, `key_base`, `mouse1_icon`..`mouse5_icon`, `mousew_icon`, `mapicon_fire`, `mapicon_house`, `mapicon_hammer`, `mapicon_pin` |

For item and piece icons, use `ItemDrop` / `Piece` `m_icon(s)`, not atlas names.

<details>
<summary>All 246 UIAtlas sprite names (1.0.15)</summary>

ac_bkg, ac_bkg_large, aware, badconnection_icon, bar_food_8, bar_food_overlay, bar_gradient, bar_gradient_16, bar_gradient_40, bar_gradient_8, bar_monster_hp_20, bar_monster_hp_5, bar_stagger, blood_magic, bottom_Darken, BraidKnotMedium, BraidLineHorisontalMedium, BraidLineHorisontalMediumFat, BraidLineHorisontalMediumFatShort, BraidLineHorisontalMediumHalfL, BraidLineHorisontalMediumHalfR, button, button_disabled, button_glow, button_highlight, button_pressed, button_small, button_small_disabled, button_small_highlight, button_small_pressed, button_tab, button_tab_disabled, button_tab_hover, button_tab_selected, check_yes, checkbox, checkbox_marker, checkbox_marker_filtered, chest_bkg, chest_blue_bkg, craft_icon, craft_icon_32, craft_icon_64, crafting_panel_bkg, crosshair, crosshair_bow, crossplay, damage, damage_cold, damage_heat, damage_poison, darken_blob, direction_keys, elemental_magic, exclamation_mark, fejd_icon, fejd_logo, file_cloud, file_legacy, file_local, food_icon, food_icon_small, gamepad, gamepad_alt, gamepad_deck, gamepad_deck_alt, gamepad_dpad, gamepad_dpad_left, gold, goldore, gradient, gradient_white, hammer_icon, hammer_icon_gold, hammer_icon_small, hands, health, health_border, health_icon, health_icon_valknut_0, health_icon_walknut, health_icon_walknut_small, healthbar_border, healthbar_overlay, hear, inv_bkg, ironpiece, item_background, item_background_sunken, item_bkg, item_bkgh, item_bkgl, key_base, key_icon, keyhint_button_p, load_ship, load_water, loadscreen_gradient, mail, map_marker, mapicon_bed, mapicon_bogwitch_camp, mapicon_boss, mapicon_boss_colored, mapicon_checked, mapicon_death, mapicon_eventarea, mapicon_fire, mapicon_hammer, mapicon_hildir, mapicon_hildir1, mapicon_hildir2, mapicon_hildir3, mapicon_hildir_dress, mapicon_hildir_wagon, mapicon_house, mapicon_memorialplace, mapicon_mysteriousplace2, mapicon_pin, mapicon_ping, mapicon_player_16, mapicon_player_32, mapicon_portal, mapicon_randevent, mapicon_shout, mapicon_start, mapicon_trader, mapicon_upgradestation, mouse1_icon, mouse1double_icon, mouse2_icon, mouse3_icon, mouse4_icon, mouse5_icon, mouse_horizontal_icon, mouse_vertical_icon, mousew_icon, mousew_icon_small, nostroke, noteleport, panel_bkg, panel_bkg_128, panel_bkg_128_transparent, panel_bkg_256, panel_border_128, panel_border_bw_128, panel_interior_bkg_128, panel_separator, piece_upgrade, point 1, point3, pvp_off, pvp_off_20, pvp_on, pvp_on_20, refresh_icon, repair, repair2, repair32, rotate_left, rotate_left_small, rudder, rudder_arrow, save_icon, see, selection_frame, server_connect_0, server_connect_1, server_connect_2, server_connect_3, ship_circle, ship_circle_bw, ship_fullsail, ship_halfsail, ship_rudder, ship_rudder_icon, ship_top, ship_wind, skill_bkg, skills, snapping_building_hint_icon, snapping_hoe_leveling_off_icon, snapping_place_ship_Icon, sneak_alerted, sneak_detected, sneak_hidden, stagger, sunken, tabletop, text_field, text_field_disabled, text_field_highlight, texts_button, texts_icon, top_darken, trail, trash_icon, trophies, trophies_20, trophy_board, upgrade-arrow, upnp_icon, Valheim_ui-health-icon_suggestion-02-trnsprnt_0 .. _5, walknut_bw, walknut_bw_20, warning_icon, weight_icon, weight_icon_32, weight_icon_32_flat, winddirection, windicon, woodpanel_320x320, woodpanel_400_tileable, woodpanel_512x512, woodpanel_characterselect, woodpanel_container, woodpanel_container_mask, woodpanel_crafting, woodpanel_crafting_240, woodpanel_crafting_240_mask, woodpanel_crafting_mask, woodpanel_feedback, woodpanel_flik, woodpanel_flik_mask, woodpanel_flik_repair, woodpanel_flik_repair_mask, woodpanel_highres, woodpanel_info, woodpanel_info_180, woodpanel_info_180_mask, woodpanel_info_mask, woodpanel_large, woodpanel_password, woodpanel_password_mask, woodpanel_playerinventory, woodpanel_playerinventory_mask, woodpanel_serverlist, woodpanel_settings, woodpanel_texts, woodpanel_trophys, x_no

</details>

### 3.3 Material, sounds, tooltips

| Thing | How to get it |
|---|---|
| Panel material `litpanel` (shader `Custom/LitGui`, the lit wood look) | Copy `.material` from a vanilla panel `Image`, or find the `Material` by name |
| Button sounds (`ButtonSfx`) | Copy the fields from `Menu.instance.m_continueButton.GetComponent<ButtonSfx>()` (`m_sfxPrefab`, `m_selectSfxPrefab`). Its `Start` adds its own click listener. Sound prefabs: `sfx_gui_button`, `sfx_gui_select`. |
| Tooltip (`UITooltip`) | Take `m_tooltipPrefab` from `InventoryGui.instance.m_craftButton.GetComponent<UITooltip>()`. Set `m_topic` / `m_text`. Hide with `UITooltip.HideTooltip()`. |
| Gamepad hint for a button | `InventoryGui.instance.m_craftButton.GetComponent<UIGamePad>().m_hint` |
| Controller icons | the `gamepad_glyphs` TMP sprite asset (3.1) |
| Helpers | `Utils.FindChild(transform, name)`, `Utils.ClampUIToScreen(rectTransform)` (`utils/Utils.cs:649, 869`) |

---

## 4. Localization

- **What `Localize` does:** `Localization.instance.Localize(text)` (`guiutils/Localization.cs:388`)
  replaces every `$word`.
- **Unknown words** become `[word]`.
- **`$KEY_X`** becomes a key name or a gamepad glyph. (verified in ValheimTomrer:
  `Localize("$KEY_JoyButtonB")` gives the pad's circle icon tag)
- **Bound key text:** `Localization.instance.GetBoundKeyString(buttonName, emptyStringOnMissing: true)`
  gives the key name (`"Mouse-1"`) or the pad icon tag for one of the game's buttons. (verified in
  ValheimTomrer)
- **Placeholders:** `Localize(text, params string[] words)` fills in `$1`, `$2`... inside the
  **translation**.
- **Adding our own words:**
  - Use `private void AddWord(string key, string text)` (`:481`). The key has no `$`.
  - Reachable because the template publicizes `assembly_guiutils`.
  - After adding, clear the 100-entry cache: `m_cache.EvictAll()`.
  - Not tested yet.
- **Language change:** `SetLanguage` clears all words and raises `Localization.OnLanguageChange`. Add
  our words again there.
- **When to add them** (guess): the first use of `Localization.instance` loads files and reads user
  settings, so add words in a `FejdStartup.Awake` postfix, not in plugin `Awake`.
- **The `Localize` component** (`guiutils/Localize.cs`) localizes a UI tree at `Start`, and again on
  language change or keyboard/gamepad switch. Putting it on our root may keep `$KEY_` hints current
  (not tested).

```csharp
static void AddWords()
{
    var l = Localization.instance;
    l.AddWord("mymod_title", "My Mod");
    l.AddWord("mymod_low", "Running low ($1 left)");
    l.m_cache.EvictAll();
}
// FejdStartup.Awake postfix: AddWords(); Localization.OnLanguageChange += AddWords;
```

---

## 5. Network safety

**Local only (safe):**
- `MessageHud.ShowMessage`, `QueueUnlockMsg`, `ShowBiomeFoundMsg`
- `Player.m_localPlayer.Message`
- `Chat.SetNpcText`, `Chat.AddString`, private `Chat.AddInworldText`
- private `DamageText.AddInworldText`
- `Minimap.AddPin(save: false)`
- `TextViewer`, `UnifiedPopup`, `TextInput`, compendium page
- Hud patches, own canvases, the build card, the key hints row
- console commands without `remoteCommand`

**Sends network messages (avoid):**
- `MessageHud.MessageAll`
- `Player.Message` on another player, `Player.MessageAllInRange`
- `DamageText.ShowText` (all versions)
- `Chat.SendText`, `Chat.SendPing`, `Talker.Say`
- console commands with `remoteCommand`
- saved map pins shared at a cartography table

In single player these still "work", because the message loops back to us. They still send network
messages, which matters for a client-only mod.

**Writes to the character save:**
- raven "seen" keys
- `AddKnownText`
- `save: true` pins
