# MyValheimMod, agent guide

Client-side BepInEx plugin for **Valheim 1.0** on **macOS / Apple Silicon**, started from
ValheimModClaudeTemplate. Everything under Environment was verified on the owner's machine
(19-09-2026, re-checked by the template's `probe` on 23-09-2026).

<!-- template:start -->
## This repo is still the template

`scripts/new-mod.sh` deletes this block. Until then:

1. `./scripts/new-mod.sh <ModName> <Author>`: renames everything (namespace, assembly, GUID,
   manifest, package.json), swaps README.md for the player-facing skeleton, removes this block.
2. `dotnet build`, then `./scripts/autotest.sh all`: expect `DONE pass=N fail=0` and `art guard:`.
3. Fill in [What this mod is](#what-this-mod-is) and [Scope](#scope-do-not-exceed-without-asking)
   with the user. Ask about each default in Scope, do not assume it.
4. Plan the first feature with the `phased-plan` skill (`.claude/skills/phased-plan/`). Its Phase 0
   copies the `probe` scenario.
5. When the first real feature lands, delete `src/Example/`, `src/Dev/AutoTest.Example.cs`, the
   `Example` lines in `Plugin.cs`, `AutoTest.Reset` and `AutoTest.Scenarios`, the `example` clip,
   and the example rows in README.md.
<!-- template:end -->

**Do not map the repo and do not hunt for a file.** Every file is listed under
[Project structure](#project-structure-every-file), plus a [Where to change what](#where-to-change-what)
table. Go straight to the file. Search only for a symbol inside a file this guide pointed you at,
or when the guide is wrong, and then fix the guide.

**This file has the rules:** what to do, what never to break. The reasons, the measured numbers and
the full behaviour behind each rule are in `.claude/design-decisions.md` (the § numbers below).
Read the section for an area before you change how it behaves or undo a rule. Skip it for ordinary
work.

---

## What this mod is

<Fill in with the user: one paragraph a player would understand, then a table of features with
their keyboard and pad controls. Keep it short; README.md is the long version.>

---

## How to work with the user

The repo owner's own rules, copied from their global `~/.claude/CLAUDE.md`. They apply to every
session here. Someone else working on the mod edits this section to fit them.

### About me

- I'm a developer. I understand architecture, git, code concepts and trade-offs. Talk to me as a
  peer engineer.
- I often don't read source files or review diffs closely. I judge by the running result. Verify
  things yourself by running them (build, launch, read the log) instead of saying "this should work".
- English is not my native language. My prompts have typos and informal phrasing, but the intent is
  usually clear. Never correct my English.
- I work by vibecoding: I try things to see what works. Open-ended asks ("change it", "try another",
  "let's see") and quick follow-ups are my method, not vague prompting.
- I play Valheim on a controller. Every feature works on a pad (see Scope).

### Writing

Applies to every reply, and to docs, plans, comments and commit messages. Dense writing costs me
real effort and hides the one fact I needed.

- **Short.** Lead with the answer or result. One or two sentences is often the whole reply.
- **Simple words.** Everyday English. No literary style, metaphors or dramatic clause stacking.
- **Lists over paragraphs.** Facts, findings, options, steps or files go in a list or a table.
- **Cut** preambles, restating my request, stacked caveats, self-analysis and explaining your
  reasoning. After drafting, delete every sentence that isn't the answer, a caveat that changes
  what I do, or the next step.
- **Status first.** When reporting on a task, the first sentence says done or not done, and
  verified or not. Example: "I changed the code for all 9 tests, but I don't know yet whether they
  pass." Leave out numbers from earlier runs and side details unless I ask.
- **No em dashes.** They read as AI-written. Use a comma, a period, parentheses or a reworded
  sentence. Don't offer em-dash variants as an option.
- **Plain words, not jargon.** Don't borrow terms the code or the domain invented just because the
  code uses them. Keep a real identifier only when I need it to find a file, symbol or setting.
- **No figurative "buy".** Use buy / buys / bought only for real purchases. Never "what that buys
  you" or "buys us time". Say "gives you", "the benefit is", "lets you", "saves". Same for other
  figurative money words like "pays off".
- The README is written to read human: no bold lead-ins, no fragment lists.

### Working

- **Find the target yourself.** When a request is ambiguous (which file, screen, method), work it
  out from the code. Don't ask me to name it. For open-ended or design asks, offer 2-3 concrete
  options in one reply instead of an open question. Ask only when truly torn, in plain terms, with
  the options listed.
- **Corrections are for you.** When I correct you, fix your own understanding and output. Don't
  write the correction into the project (content, comments, docs) unless that text's real reader
  needs it.
- **Leave other worktrees alone.** I run several agent sessions in parallel, each in its own git
  worktree. An unfamiliar worktree or branch is probably another live session. Only remove
  worktrees or branches you created or that I name. Never suggest cleaning up stray ones.
- **Verify by running.** Build, launch, run the scenario, read the log, open the screenshot. Never
  report "should work". If something was not run, say so in the first sentence.
- **Anything I would have an opinion on goes in the reply**, never into a quiet decision.

### Plans and handoffs

- Save plans in `.claude/plans/` and handoffs in `.claude/handoff/`. Never in the OS temp or
  scratchpad folder, even when a skill defaults there. Scratchpad is fine for real throwaways like
  probe scripts.
- Start the file name with the date as `DD-MM-YYYY-` (day first, not ISO). Example:
  `19-09-2026-camp-mechanics-plan.md`.
- When the task of a plan or handoff is finished, mark it done in two places:
  - Add a status line at the top: `> **Status: DONE (DD-MM-YYYY).** Result: <path>.`
  - Rename the file to end with `-done`, e.g. `19-09-2026-camp-mechanics-plan-done.md`. Keep it in
    the same folder.
- Don't read `-done` files unless you really need them, for example to find out why an earlier
  decision was made.
- A build worth more than a few hours gets a phased plan: see [Plans and sub-agents](#plans-and-sub-agents).

---

## Maintaining this file

It is loaded on every turn, so every line in it is paid for on every turn. ValheimTomrer's reached
13,000 words because each finished phase added its own section. Before adding anything, apply three
tests:

1. **What, not why.** A rule goes here in one line: what to do, or what never to break. The reason,
   the history, what was tried, the measured numbers and the full behaviour of a feature go in
   `.claude/design-decisions.md`, with at most a one-line rule here pointing to its section.
2. **Not already checked.** If the autotest, the compiler or a guard already fails on the mistake,
   say it in a clause, not a section.
3. **Not already said.** Search this file first.

A new feature is not a new section here. Its plan goes to `.claude/plans/`, its log to
`.claude/handoff/`, its reasons to `.claude/design-decisions.md`, and this file gets only the lines a
later session would break something without. Removing a stale line is as welcome as adding a true
one. **Add or remove a file: update [Project structure](#project-structure-every-file) in the same
commit.**

---

## Scope, do not exceed without asking

**Every feature works on a controller. No exceptions.** The owner plays on a pad. Anything a key,
the mouse or the wheel can do, a pad button (or combo) must do too, in the same phase, with its own
autotest check through the test pad and its line in the README's controller table. A plan must
never mark pad support "out of scope" or "later". A feature without it is not done. (ValheimTomrer
shipped a feature keyboard-only once; the owner made this a hard rule.)

The rest is decided per mod, with the user, when the mod starts. The template's defaults are
ValheimTomrer's. Keep each one, or change it and its guard:

- **Single-player / client-side only.** No RPCs, no ZDO sync, no server-side logic.
- **No custom prefabs or pieces.** Patch existing game behaviour instead.
- **No Jötunn dependency.** It is only needed for registering custom content. Plain BepInEx +
  HarmonyX covers a patch-only mod.
- **Old worlds and characters** are out of scope: no save-migration handling.

If a task seems to require crossing one of these lines, stop and ask.

### Never break (defaults, with their guards)

**The world-save rule.** The mod writes nothing of its own into the world save:

- no ZDO keys, no RPCs, no network messages, no ServerSync;
- pieces go up only through the game's `Player.PlacePiece` and come down only through the calls
  `Player.RemovePiece` makes; items move only through the game's `Inventory` calls;
- the mod's own data is a text file under `BepInEx/config/MyValheimMod/`, never in the world.

If a change seems to need a ZDO key, an RPC or a new prefab, the design went wrong: stop and ask.
The check, expected to print nothing. Name every folder under `src/` except `src/Dev` (plain `src/`
lets the test code's lines through, §1):

```bash
command grep -rn "ZDO().Set\|\.Set(ZDOVars\|InvokeRPC\|RoutedRPC\|Register<" src/Example src/Input src/Patches src/Ui src/Plugin.cs
```

**The art rule.** The mod never writes a model, mesh, texture, icon, sprite, atlas or material to
disk, in any format, under any folder. Everything it draws is made at runtime from what the game
already loaded (§4). `all` ends with an in-game guard (`AutoTest.CheckNoArtWritten`, it walks the
mod's folders) and `autotest.sh` walks the repo, skipping only `thunderstore/icon.png`. A mod that
ships art on purpose sets `ART_GUARD=false` in `scripts/autotest.sh` and drops this rule.

---

## Environment (verified, not assumed)

| | |
|---|---|
| Game version | 1.0.15 (`Version.CurrentVersion`) |
| Unity | `6000.0.75f1` |
| Runtime | **Mono** (`MonoBleedingEdge`), *not* IL2CPP |
| Game code | `assembly_valheim.dll` (1311 types), `assembly_utils.dll` (ZInput), `assembly_guiutils.dll` (UIGroupHandler, GuiUtils) |
| BepInEx | 5.4.23.5 (denikson BepInExPack_Valheim 5.4.2350) |
| doorstop | 4.5.0, universal (x86_64 + arm64) |
| Plugin TFM | `netstandard2.1` |
| .NET SDK | 9.0.303 (no Mono, no msbuild, not needed) |
| Network version | `Version.c_networkVersion = 40` |

`./scripts/autotest.sh probe` writes the live numbers to `.devtest/probe.txt`. After a game update,
run it and fix this table.

**Paths** (macOS layout differs from Windows/Linux):

```
GAME     ~/Library/Application Support/Steam/steamapps/common/Valheim
MANAGED  $GAME/valheim.app/Contents/Resources/Data/Managed     # NOT valheim_Data/Managed
PLUGINS  $GAME/BepInEx/plugins/MyValheimMod/
CONFIG   $GAME/BepInEx/config/com.yourname.myvalheimmod.cfg
LOG      $GAME/BepInEx/LogOutput.log
```

`Directory.Build.props` also has the usual Windows and Linux Steam paths. Those, and the scripts on
Windows or Linux, were never tested; the scripts are macOS only (`run_bepinex.sh`, the `.app`).

### Apple Silicon: BepInEx fails silently

On native arm64 no plugin loads and no `LogOutput.log` is written; the game looks vanilla (§1).
`run_bepinex.sh` must prefer x86_64 (keep a pristine copy as `run_bepinex.sh.orig`):

```sh
export ARCHPREFERENCE="x86_64,arm64"
```

**Any BepInEx reinstall or update reverts this patch.** After one, re-apply it (an outer
`arch -x86_64` does not help), then clear Gatekeeper quarantine on the new injector, even one
copied over the old file:

```bash
xattr -d com.apple.quarantine libdoorstop.dylib
```

### Machine traps (this Mac)

- **`grep` here is gitignore-aware.** A plain `grep -r` silently skips `.devtest/`, `bin/`, `obj/`
  (and anything else ignored). Use `command grep` on named files, or `find ... -print0 | xargs -0
  command grep`. Say so in every sub-agent prompt.
- **`timeout` is an Intel binary.** Anything under it (the game, node, Chrome) runs under Rosetta,
  10 to 20 times slower. Never wrap `autotest.sh` in it; use the Bash tool's own timeout.
- macOS `grep -rn ... src/` prints `src//Dev/...`, so `grep -v "src/Dev/"` filters nothing. Name
  the folders.
- The game's Remove key on this Mac is Left Command, not the middle mouse. Tests press whatever
  ZInput binds (`AutoTest.PressBound`).
- The screen is 3456x2160 with the game's GUI scale 1.8. Pixel numbers in tests depend on it.
- A real DualSense is often plugged in during runs, so `Gamepad.current` is not null.

---

## Project structure, every file

Nothing here is a guess. Use this instead of searching. Every file in the repo outside `bin/`,
`obj/` and `.devtest/`.

**Build, scripts, data**

```
MyValheimMod.csproj               netstandard2.1, local refs, publicizer, the AI metadata Thunderstore
                                  asks for, auto-deploys the DLL into BepInEx/plugins after every build
                                  (-p:DeployToGame=false skips it)
Directory.Build.props             finds the install: ValheimInstall, ValheimManaged, BepInExDir
README.md                         the template's own readme until new-mod.sh swaps in the player page
docs/templates/README.mod.md      the player-facing readme skeleton, also the Thunderstore page
                                  (new-mod.sh moves it to README.md)
docs/media/                       the README's GIFs (scripts/make-gifs.py writes them)
CHANGELOG.md                      the player-facing changes, one block per released version
LICENSE                           MIT
package.json                      npm run build (Debug), build:release, test (autotest all), zip
.gitignore                        bin, obj, .devtest, thunderstore/build, .claude/settings.local.json
scripts/new-mod.sh                turns the template into a named mod, run once
scripts/dev.sh                    build, deploy, launch, tail our log lines (--debug adds the debugger)
scripts/autotest.sh               run one AutoTest scenario (or "all"), output lands in .devtest/
scripts/zip.sh                    npm run zip: Release build (not deployed), checks the manifest, one
                                  version everywhere, the changelog and the icon, then
                                  thunderstore/build/<Name>.zip, made from scratch
scripts/make-gifs.py              the readme_gifs frames in .devtest/gifs/ to docs/media/*.gif
thunderstore/manifest.json        the package's manifest (description 250 characters at most)
thunderstore/icon.png             NOT in the template: add a 256x256 PNG before the first zip. The one
                                  image the art guard allows
.vscode/tasks.json                tasks "build", "run: game (debug)", "test: all"
.vscode/launch.json               "Attach to Valheim", vstuc, 127.0.0.1:10000
```

**`.claude/`** (tracked, so a second developer sees it; only `settings.local.json` is ignored)

```
design-decisions.md               the reasons and numbers behind every rule here, one § per area.
                                  §1 to §9 are general Valheim modding knowledge, §10 on this mod's
research/19-09-2026-custom-ui.md             UI: start here, which approach fits which need
research/19-09-2026-custom-ui-game-api.md    UI: exact game methods, patch targets, fonts, sprites
research/19-09-2026-custom-ui-platform-and-mods.md  UI: input, asset bundles on macOS, other mods
skills/phased-plan/SKILL.md       the skill: plans an orchestrator runs one sub-agent per phase
skills/phased-plan/references/plan-template.md  its fill-in plan skeleton and sub-agent prompt
plans/                            DD-MM-YYYY-<name>.md, -done when finished
handoff/                          DD-MM-YYYY-<name>.md, one per plan, one block per phase
```

**`src/`**

```
Plugin.cs                         BepInEx entry. Binds ModEnabled, starts Harmony, its Update ticks
                                  GamePad first, then each feature. OnDestroy unpatches
Input/Keys.cs                     the mod's keys through ZInput, safe against a KeyCode it cannot map
Input/GamePad.cs                  the pad in the world through the game's button names (ZInput):
                                  Pressed, Held, ModifierHeld, ButtonOf, PlayStation, Live, and
                                  HoldWhile, which holds game buttons back while a combo uses them
Ui/ModUi.cs                       the Open / Blocking flag every input patch reads, TakeEscape,
                                  Typing / JustTyping, WorldFree (no game menu, chat or console up)
Ui/UiTheme.cs                     the font, two own copies of the HUD text material (plain and
                                  outlined), colours, the UIAtlas chrome sprites. Keyed on the live
                                  Hud object, which dies on every world load
Ui/UiBuild.cs                     widget builders: Canvas (the game's recipe), Panel, Label,
                                  OverPicture, Button, InputField (a TextBox), Scroll, Row, Column,
                                  LinkRow / LinkColumn (pad navigation), and the TextBox class
Ui/PadGlyphs.cs                   the game's controller icons out of its gamepad_glyphs TMP asset
Example/ExampleWindow.cs          EXAMPLE, delete with its test: F9 or L2 + R1 opens a small window
```

**`src/Patches/`**, one class per target, file named `<Type><Method>Patch.cs`

```
FejdStartupAwakePatch.cs          main-menu smoke test, prints harmony=OK | publicizer=OK
InputBlockPatches.cs              the input takeover for a window, nine patches in one file on
                                  purpose, all gated on ModUi.Blocking
BuildUiOnLayoutChangedPatch.cs    while a window is up, switching keyboard and pad no longer clears
                                  the UI selection (it threw a typing box out of the keyboard)
ZInputTryGetButtonStatePatch.cs   holds back the pad buttons GamePad.HoldWhile names. Every
                                  GetButton* of the game comes here
```

**`src/Dev/`**, Debug builds only (`#if DEBUG`), stripped from a Release build

```
AutoTest.cs                       the runner: own character and world, the Scenarios list, the "all"
                                  chain, Guard, Reset, ResetSettings, the in-game art guard
AutoTest.Input.cs                 real input: PressKey, HoldKey, KeyOf, WheelNotch, ClickButton,
                                  ClickScreen, the test pad (PadOn, PadPress, PadDown, PadSticks,
                                  PadOff), PressBound (a game button by name)
AutoTest.World.cs                 MoveToBuildSpot (flat, levelled), LevelGround, ClearVegetation,
                                  EquipHammer, ClearInventoryExcept, RemovePlayerPieces, Screenshot,
                                  ScreenBox, OnScreen, V
AutoTest.Probe.cs                 "probe": versions, plugins, layers, every button binding, UI assets
AutoTest.Gifs.cs                  "readme_gifs": records frames for the README's GIFs
AutoTest.Example.cs               EXAMPLE: "example_window" and the "example" GIF clip
AutoTestPeace.cs                  stops the AI, the spawns and the raids in the test world
```

### Where to change what

| Task | File |
|---|---|
| Add a feature's per-frame work | a static `Tick()` in the feature, called from `Plugin.Update` after `GamePad.Tick` |
| Add a setting | `Config.Bind` in the feature's `Bind(ConfigFile)`, called from `Plugin.Awake`. `AutoTest.ResetSettings` resets every entry already |
| Add a key and its pad twin | the feature's Tick: `Keys.Down(key)` and `GamePad.Pressed(...)`; hold the pad buttons back with `GamePad.HoldWhile` in its `Bind` |
| Open a window of our own | `UiBuild.Canvas` under `Hud.instance.transform.parent`, `ModUi.Open` / `MarkClosed` (copy `ExampleWindow`) |
| Colours, font, sprites | `src/Ui/UiTheme.cs` |
| Add a game hook | a new `src/Patches/<Type><Method>Patch.cs` |
| Add a test scenario | a new `src/Dev/AutoTest.<Feature>.cs`, a line in `AutoTest.Scenarios`, a line in `scripts/autotest.sh`'s header, its reset in `AutoTest.Reset` |
| Measure something before building on it | `src/Dev/AutoTest.Probe.cs`, or a probe scenario of the plan's own |
| A README GIF | a clip in `AutoTest.GifClip` and `CLIPS` in `scripts/make-gifs.py` |
| Write down why something works the way it does | `.claude/design-decisions.md`, the section for that area |

---

## Build

```bash
dotnet build                 # or: npm run build
dotnet build -c Release      # or: npm run build:release (no src/Dev, no autotest)
npm run zip                  # the Thunderstore zip, thunderstore/build/<Name>.zip
```

About 1.6 s. A successful build **auto-deploys** the DLL + `.pdb` into
`BepInEx/plugins/MyValheimMod/`. There is no separate install step, never copy by hand. Never build
while a test runs: the deploy writes over the DLL the game has loaded.

References resolve out of the **local install**, not NuGet. `VALHEIM_INSTALL` overrides the path.
`assembly_valheim`, `assembly_utils` and `assembly_guiutils` are publicized by Krafs.Publicizer, so
private and internal members are directly accessible (`fireplace.m_fuel`, not `AccessTools`).
Publicized copies land in `obj/`; the game's own DLLs are never modified. Keep builds at 0 warnings.

---

## Run and verify

```bash
./scripts/dev.sh          # build, launch, stream our lines + errors
./scripts/dev.sh --debug  # same, plus Mono soft debugger on 127.0.0.1:10000
cd "$GAME" && ./run_bepinex.sh    # or manually
```

**Verification signal.** `src/Patches/FejdStartupAwakePatch.cs` hooks `FejdStartup.Awake`, which
runs on the **main menu**, so the whole pipeline is provable in about 13 s without loading a world:

```
[Info   :MyValheimMod] MyValheimMod 0.1.0 loaded.
[Info   :MyValheimMod] main menu reached | harmony=OK | publicizer=OK
```

That line confirms plugin load, Harmony patching, *and* publicization (it reads
`FejdStartup.m_instance`, a private static). Prefer main-menu hooks for smoke tests; load a world
only for features that need one. For a feature, the check is its autotest scenario.

- Steam does not need to be running: `steam_appid.txt` lets the game launch standalone.
- Stop a test instance: `pkill -f "valheim.app/Contents/MacOS/Valheim"`.
- In game, **F5** opens the console, then `devcommands` unlocks `god`, `fly`, `spawn`, `tod`. The
  console is **off by default** (since 0.221.4): enable it once in Settings > Gameplay > Enable
  console (saved), or launch with `-console`. While it is off, F5 does nothing.
- Only one game at a time: `autotest.sh` refuses to start while Valheim runs (another session may
  own it).

---

## Debug

`./scripts/dev.sh --debug`, then in VS Code **Run and Debug -> "Attach to Valheim"** (the `vstuc`
type from the Visual Studio Tools for Unity extension, §1). When it won't attach, the game was
usually not started with `--debug`. Confirm the endpoint is live before blaming the editor:
`lsof -nP -iTCP:10000 -sTCP:LISTEN` (expect `Valheim ... (LISTEN)`).

**A `--doorstop-*` flag takes a value.** Passing one bare aborts `run_bepinex.sh` silently: empty
output, no game, no log.

```bash
./run_bepinex.sh --doorstop-mono-debug-enabled true    # correct
./run_bepinex.sh --doorstop-mono-debug-enabled         # WRONG, script dies
```

- `--doorstop-mono-debug-suspend true` freezes the game at startup until a debugger attaches (for
  breakpoints in `Awake` or plugin load).
- Logs: `BepInEx/LogOutput.log` (includes the Unity log). Vanilla Unity log:
  `~/Library/Logs/IronGate/Valheim/Player.log`.

---

## Reading game code

Valheim has no modding API or docs. Finding a patch target means reading the game.

- **Names, signatures, enum values, private or not:** read the metadata with
  `System.Reflection.Metadata` from a throwaway `dotnet run` project in the scratchpad.
- **Method bodies and call flow:** `ilspycmd` (`dotnet tool install -g ilspycmd`) wants .NET 8, so
  roll forward. One type at a time is quick:
  `DOTNET_ROLL_FORWARD=Major ~/.dotnet/tools/ilspycmd -t ZInput "$MANAGED/assembly_utils.dll"`.
  The whole assembly (about 1 minute): `-p -o <scratchpad>/valheim-src "$MANAGED/assembly_valheim.dll"`,
  then grep the tree. `/tmp` can be gone after a reboot.
- **Names that only exist as text** (sprite and glyph names): scan the DLL's strings. macOS
  `strings` has no `-el`, so UTF-16 needs python: `re.finditer(rb'(?:[\x20-\x7e]\x00){3,}', data)`.
- **Which keys and pad buttons the game uses**: `.devtest/probe.txt`, "Buttons". A combo the game
  already binds with its modifier ("with the modifier") is taken.

Game 1.0 facts, patch target rules and API traps: §2 and §3. The ones that bite most:

- Patch the **private** method when the public one is a small wrapper that may be inlined
  (`ZInput.TryGetButtonState`, `ZInput.Internal_GetMouseScrollWheel`).
- `ZInput.GetKeyDown(KeyCode)` throws for some KeyCodes (`KeyCode.Question`) and the throw kills the
  rest of that frame's tick. Read keys through `Input/Keys.cs`, never `ZInput.GetKeyDown` directly.
- `Managed/` ships `Newtonsoft.Json` 13.0: do not bundle it.

**After a game update**, run `probe` and `all`, and check every patched method still exists (a
missing one is a `HarmonyException` in the log): the nine input targets in `InputBlockPatches.cs`
(`Player.TakeInput`, `PlayerController.TakeInput`, `TextInput.IsVisible`, `InventoryGui.Show`,
`HotkeyBar.Update`, `Menu.Update`, `Minimap.Update`, `GameCamera.UpdateMouseCapture`,
`ZInput.Internal_GetMouseScrollWheel`), `ZInput.TryGetButtonState`, `BuildUi.OnLayoutChanged`,
`FejdStartup.Awake`, and every patch the mod added. Keep this list current.

---

## Custom UI

For any UI task (HUD text, messages, hover text, map pins, popups, own windows, menu buttons,
hints), **read the research first**: `.claude/research/19-09-2026-custom-ui.md`, then the game-API
file for the exact calls. Reasons and numbers: §4.

- Build with uGUI + TextMeshPro in code. No `UnityEngine.UI.Text`, IMGUI only for debug.
- Prefer filling a vanilla surface (hover text, `MessageHud`, the game's popups, the key hint row)
  over an own window. Copy the game's own widgets (its popup, its hint entries) instead of drawing
  look-alikes.
- A new TMP text shows nothing until `.font` is set (`UiBuild.Label` does it). **Never share the
  vanilla text material**: at 12 to 16 point every label on it reads grey. Use `UiTheme.FontMaterial`
  on a panel, `UiTheme.FontOutlined` over the world.
- An own window: `UiBuild.Canvas` (sort order 950, reference pixels per unit 50, `GuiScaler`,
  `UIGroupHandler` priority 5) under `Hud.instance.transform.parent`, `ModUi.Open` while it shows,
  `ModUi.MarkClosed()` and `PlayerController.SetTakeInputDelay(0.2f)` when it hides. It dies with
  the world: check it with Unity's null and build it again.
- Never cache theme colours or sprites across a world load: read them in Build.
- **Text boxes** are `UiBuild.InputField` (a `TextBox`), never a plain `TMP_InputField`: the game's UI
  module sends the real pad's cross, circle and D-pad to a typing box. While a box types, the mod's
  keys and Esc are the box's (`ModUi.JustTyping`).
- **Esc** that closes something of ours must not also pause: a window gets it from
  `ModUi.Blocking`'s extra frame; anything else calls `ModUi.TakeEscape()` that frame.
- A row that carries white text on `item_background` is tinted `UiTheme.Slot`, and a `Button` on it
  uses `Selectable.Transition.None` (the colour tint would darken it again).
- Panels over the bright world need at least 70 % black behind red or small text.
- Never use the UI calls that send network messages: `Chat.SendText`, `Chat.SendPing`,
  `DamageText.ShowText`, `MessageHud.MessageAll`, `Player.MessageAllInRange`, map pins with
  `save: true`.
- The owner's taste (from ValheimTomrer): white text, no dim grey; no redundant lines; controls go in
  the game's hint row, never on a card or status line; no message when a mode starts; settings live
  in the config file only, no settings dialog; a click never locks or hides the cursor.

---

## The pad

Every key the mod reads has a pad twin, read through the game's own button names so the game's
layout decides (`Input/GamePad.cs`). "L2" means the game's modifier `JoyAltKeys` (L1 in the
alternative layout). The player's tables are in README.md. Reasons and numbers: §5.

- **In the world**, read the game's buttons: `GamePad.Pressed(GamePad.Circle)`,
  `GamePad.ModifierHeld`. Never read `Gamepad.current` for a world combo, and never go through
  `ZInput.GetButtonDown` for the mod's own reads (it passes the hold-back).
- **A combo's buttons are held back from the game** with `GamePad.HoldWhile(when, buttons)`, by
  binding path, until let go. Without it L2 + R1 would also swing a secondary attack.
- **A press that closes something must not reach the game.** Circle is the jump, read in the next
  FixedUpdate: hold circle back while the thing is open (`ExampleWindow.Bind`), or reset the game's
  buttons with `ZInput.ResetButtonStatus(name)` (ValheimTomrer measured a 1.38 m jump without it).
- **Inside a window, one owner for the pad.** Either the game's UI module drives it (Selectables with
  explicit navigation, `UiBuild.LinkColumn`, the EventSystem selection set, cross presses through
  the module) and the mod never binds cross, or the mod drives it and keeps the EventSystem selection
  null (ValheimTomrer's editor). Both at once fires every press twice.
- When a window opens, set the EventSystem selection yourself: it may still hold a vanilla button
  (`Content/ALL`), which the first cross would press.
- Combos work only where the player is free (`GamePad.Live`: mod on, no game menu, chat, console or
  window of ours).
- **The pad layout is the player's.** Once a pad connects, the game switches to the layout saved in
  its settings; this machine uses Alternative2, where cross is the jump and circle the build menu.
  Never assume a button's game meaning: tests press game buttons by name (`AutoTest.PressBound`) and
  check a hold-back on every name bound to the button (`GamePad.NamesOf`). `probe` lists both layouts.
- Sticks: read raw (`ReadUnprocessedValue`) and apply the game's radial dead zone 0.2, never
  `ReadValue()` (the game sets Unity's stick filter to 0.4 to 0.75, which made a look clunky).
  Camera look is the game's: 110 degrees a second times `PlayerController.m_gamepadSens`, its invert.
- Hints: `Localization.GetBoundKeyString("JoyButtonB")` gives the game's own icon tag, in the pad
  family and layout the player has. `PadGlyphs` gives the icons as Sprites.
- Do not use Ctrl as a world modifier (it is crouch); many Mac mice have no Mouse4 or Mouse5.

---

## Autotest

`./scripts/autotest.sh <scenario>`, Debug builds only. What the harness does and why: §7.

| Scenario | What it proves |
|---|---|
| `probe` | measures the game into `.devtest/probe.txt`: versions, plugins, layers, every button binding (again with a pad connected, in the player's saved layout), UI assets. No feature |
| `build_spot` | the shared flat build spot is found and levelled, the hammer is in hand |
| `example_window` | EXAMPLE: F9, Esc, clicks, L2 + R1, D-pad, cross, circle; the game never sees the combo's R1 or the closing circle, no pause, the pad's jump still jumps after; not over the inventory |
| `all` | every scenario above in one game, then the in-game art guard, then the repo walk. **This is the one to run** before calling anything done |
| `readme_gifs` | no test: records README GIF frames. Not in `all`. Then `python3 scripts/make-gifs.py` |

- **Done means** `DONE pass=N fail=0` from `all`, plus `art guard:` in the output. A single scenario
  green is not enough: a failure only in the chain means one scenario leaks state into the next;
  fix the reset, not the check.
- **Real input only.** Tests press keys, the wheel, the mouse and the test pad through Unity's input
  system (`AutoTest.Input.cs`), so the game reads them like a person's. Never call the feature's
  method to fake a press.
- **Every input has a pad check** through the test pad ("AutoTestPad DualSense"). Presses go to that
  device, never through `InputSystem.FindControl`, which can pick the real DualSense.
- **Check what the game must not do** with the same press: pause, jump, open the inventory, radial.
- **Screenshots are read, not counted.** A visual check is done only when the agent opened the PNG
  and said what it shows. Every run deletes `.devtest/*.png` first: run the scenario whose pictures
  matter alone or last. Clear "New item" popups and close our dialogs before the picture.
- **Reset between scenarios** (`AutoTest.Reset`): every feature adds the line that closes or forgets
  it. `ResetSettings` puts every config entry back, so no run changes the player's config file.
- **The test world is saved on quit.** Pieces, items and the hammer's wear carry over between runs.
  Remove what you built (`RemovePlayerPieces`), repair before use (`EquipHammer`), compare counts
  before and after, never absolute counts.
- World-building scenarios build at `MoveToBuildSpot` (flat, levelled, always the same spot and
  facing). `AutoTestPeace` quiets the world at the start of every run.
- Wait for a condition, not a fixed number of frames. Re-run once before believing a single odd
  failure; if it repeats, it is real.
- `AUTOTEST_CHAIN="a,b" ./scripts/autotest.sh all` runs only those, in that order.
- A world load takes about 40 s; ValheimTomrer's 24-scenario chain took 12 minutes. Run long chains
  in the background and wait for them.
- **Not covered by any test:** a real pad in a person's hands. The test pad is a real input device
  the game and its UI module read, but check a new pad feature once by hand. Say so in the report.

---

## Plans and sub-agents

A build worth more than a few hours gets a plan written with the **`phased-plan` skill**
(`.claude/skills/phased-plan/SKILL.md`): an orchestrator fires one sub-agent per phase, so no session
runs out of context. The lessons from ValheimTomrer's five plans (§9):

- **Phase 0 is a probe.** It turns every guess into a number in `.devtest/*.txt` (copy
  `AutoTest.Probe.cs`). Every plan there had wrong facts; the sub-agent fixes the fact, proves it with
  a test, and logs it under "Plan was wrong". Never make a test assert what the plan said when the
  game says otherwise.
- **Each phase has a named scenario** in the phase map, and its Done-when has numbers in it.
- **Repeat the hard rules in every sub-agent prompt:** the pad rule, the world-save rule, the art
  rule, "grep skips ignored folders here", "never wrap autotest.sh in timeout".
- **The orchestrator never reads source files**, runs `dotnet build` itself, commits each green
  phase (sub-agents don't commit), and runs `all` before the plan is done.
- Claims about earlier phases in a prompt can be wrong ("the region scrolls" when it never did).
  Sub-agents check before building on them.
- A bug outside the phase goes in the report with a one-line fix, not into the phase.
- For visual work, show the owner the screenshot and wait for an OK before the next phase. Green
  tests do not prove the feel: ValheimTomrer's editor shipped at 519 passes and the owner found four
  problems on first use.
- The last phase updates this file (only the rules), `design-decisions.md` (the reasons), README and
  CHANGELOG, runs `all`, and marks the plan and its handoff done.

---

## Publishing (Thunderstore)

`npm run zip` builds Release (no autotest in it), checks and packs `thunderstore/build/<Name>.zip`.
Rules and what the check covers: §8.

- **One version in three places:** the csproj `<Version>`, `Plugin.PluginVersion`, the manifest's
  `version_number`, plus a `## <version>` block in CHANGELOG.md. `zip.sh` fails on any mismatch.
  Never bump the version in a fix commit; add the change under the unreleased block.
- The manifest `description` is 250 characters at most and says the same as the README's first
  lines. The `name` is `[A-Za-z0-9_]`.
- `thunderstore/icon.png` is 256x256. It is the one image the art guard allows.
- README.md is the Thunderstore page: images by GitHub raw URL, the controller table, and the AI
  line Thunderstore asks for (also in the csproj's `AssemblyMetadata`).
- Dependency: `denikson-BepInExPack_Valheim-5.4.2350`. Add nothing the game already ships.

---

## Conventions

- One patch class per target, file named `<Type><Method>Patch.cs` in `src/Patches/`. The one
  exception is `InputBlockPatches.cs` (nine targets that break together).
- Gate feature behaviour on `MyValheimModPlugin.ModEnabled.Value`.
- Log through `MyValheimModPlugin.Log` (BepInEx `ManualLogSource`), never `Debug.Log`.
- Keep `OnDestroy` calling `_harmony.UnpatchSelf()`, required for ScriptEngine hot reload not to
  stack duplicate patches.
- **Do not** add `[BepInProcess("valheim.exe")]`. Common Valheim templates include it; on macOS the
  executable is `Valheim` and the attribute silently prevents loading.
- **Do not** switch BepInEx/game references to NuGet. Local refs guarantee version match.
- Everything in `src/Dev` is inside `#if DEBUG`. `zip.sh` fails if the Release DLL has the autotest.
- Write files to a temp file, then rename.
- Comments and docs follow the Writing rules above: short, plain, no em dashes.
- **Add or remove a file, update Project structure in the same commit.**

---

## Failure modes and their signatures

| Symptom | Cause |
|---|---|
| No `LogOutput.log` at all, game runs fine | arm64, `ARCHPREFERENCE` reverted (see Apple Silicon) |
| "cannot be opened because the developer cannot be verified" | quarantine on a new `libdoorstop.dylib`: `xattr -d` it |
| `0 plugins to load` | DLL not deployed; re-run `dotnet build` |
| Plugin loads, patch never fires | wrong method name or overload, or the public wrapper was inlined: grep the log for `HarmonyException`, patch the private method |
| Compile error reaching a private member | assembly missing from `<Publicize>` in the csproj |
| `DllNotFoundException: AppleCoreNativeMac` | **benign**, vanilla Apple GameKit probe failing under Rosetta |
| `run_bepinex.sh` prints nothing, no game | a `--doorstop-*` flag passed with no value |
| Small text in our window reads grey | it shares the HUD's TMP material; use `UiTheme.FontMaterial` |
| A TMP text shows nothing | no `.font` set, or measured before its Awake (build widgets switched on, hide after) |
| Esc closes our window and pauses the game too | the window did not go through `ModUi.MarkClosed` (no extra blocking frame), or a mode forgot `ModUi.TakeEscape` |
| The circle that closed something makes the player jump | circle not held back (`GamePad.HoldWhile`) or reset (`ZInput.ResetButtonStatus`) |
| One pad cross presses a button twice | both the game's UI module and the mod press it: one owner for the pad in a window |
| The first cross in a new window presses a build-menu button | the EventSystem still held a vanilla selection; set it when the window opens |
| A typing box drops out on the first pad press | `BuildUiOnLayoutChangedPatch` did not apply (`BuildUi.OnLayoutChanged` renamed) |
| A key read throws and the rest of the tick stops | `ZInput.GetKeyDown` called directly on a KeyCode it cannot map (`KeyCode.Question`); use `Keys` |
| Test: a mouse click does nothing, the button stays pressed | a `MouseState.WithButton` state reused for the release; build each state on its own |
| Test: a pad press does nothing | it went to a real pad (`FindControl`), or `PadOn` was not called |
| Test: build mode never starts, the hammer key does nothing | the test character's hammer broke (saved run after run); `EquipHammer` repairs it |
| Test: the hammer is gone right after a teleport | the player landed in water: swimming puts it away |
| Test: a support or build check fails at random | a creature wandered in (`AutoTestPeace` off?) or the spot is not levelled |
| Test: a scenario passes alone and fails in `all` | the one before it left state behind; add its line to `AutoTest.Reset` |
| Test: the player's config file changed after a run | a setting not reset; `ResetSettings` covers `Config`, a file the feature writes needs its own reset |
| `autotest.sh` died with a syntax error after the game quit | the script was edited while it ran |
| The game runs 10 to 20 times slower under a script | it was started under `timeout` (Rosetta) |
