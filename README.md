# ValheimModClaudeTemplate

A starting point for a Valheim 1.0 mod (BepInEx 5 + HarmonyX) that you build with Claude Code. It
carries everything learned while building [ValheimTomrer](https://github.com/mikamarik/ValheimTomrerMod),
an in-game blueprint editor: the build and deploy setup, an in-game test harness, a small UI and
controller kit, the rules for the agent, and the reasons behind them.

## What you get

- **A plugin that builds and loads.** `dotnet build` compiles against your own Valheim install and
  copies the DLL into `BepInEx/plugins`. A main-menu line proves Harmony and the publicizer work.
- **An in-game test harness** (`scripts/autotest.sh`). It starts the game with its own character and
  world, presses real keys, mouse clicks and a made-up controller, prints PASS/FAIL lines, takes
  screenshots and quits. `./scripts/autotest.sh all` is the check before anything is called done.
- **A UI and controller kit**: a window made from the game's own font and sprites, the input
  takeover that stops the game while it is open, a text box that behaves with a controller, the
  game's controller icons, and a helper that reads the controller through the game's own button
  names (so the player's layout decides) and keeps a combo's buttons away from the game.
- **An example feature** that uses all of it: F9 or L2 + R1 opens a small window. Its test covers
  the keyboard, the mouse and the controller. Delete it when your first feature lands.
- **The agent's guide** (`CLAUDE.md`): how the owner wants to work, the environment, every file,
  the rules for UI, controller, tests and publishing, and the failure signatures seen so far.
- **The reasons** (`.claude/design-decisions.md`): the measured facts about the game and the traps
  behind each rule, and what ValheimTomrer's plans got wrong.
- **UI research** (`.claude/research/`): every way to show UI in Valheim 1.0, with the exact game
  methods, checked against what ValheimTomrer built.
- **The `phased-plan` skill** (`.claude/skills/phased-plan/`): big features are planned so an
  orchestrator runs one sub-agent per phase and no session runs out of context.
- **Publishing**: `npm run zip` makes the Thunderstore package and checks the manifest, the
  versions, the changelog and the icon.

## Start a new mod

You need macOS on Apple Silicon (the scripts are macOS only), Valheim with
BepInExPack_Valheim 5.4.2350, the .NET 9 SDK and Claude Code.

1. Make a repo from this template (GitHub: "Use this template"), clone it.
2. Patch BepInEx for Apple Silicon once, if you have not: see "Apple Silicon" in `CLAUDE.md`.
3. `./scripts/new-mod.sh <ModName> <Author>`, for example `./scripts/new-mod.sh HearthAndHome mikamarik`.
4. `dotnet build`, then `./scripts/autotest.sh all`. Expect `DONE pass=N fail=0` and `art guard:`.
   The first run makes the test world, so it takes a few minutes.
5. Commit. Open Claude Code in the repo and describe the mod. It will fill in "What this mod is"
   and "Scope" in `CLAUDE.md` with you, then plan the first feature with the `phased-plan` skill.

Windows and Linux: `Directory.Build.props` has the usual Steam paths, so the build may work, but
nothing here was tested there and the scripts need a port.

## Layout

```
CLAUDE.md                   the agent's guide (rules)
.claude/design-decisions.md the reasons and numbers behind the rules
.claude/research/           UI research
.claude/skills/phased-plan/ the planning skill
src/Plugin.cs               the BepInEx entry
src/Ui/, src/Input/         the UI and controller kit
src/Patches/                one Harmony patch class per game method
src/Example/                the example feature (delete it)
src/Dev/                    the in-game test harness (Debug builds only)
scripts/                    new-mod, dev, autotest, zip, make-gifs
docs/templates/             the player-facing README skeleton (new-mod.sh makes it README.md)
thunderstore/               the package manifest (add icon.png, 256x256)
```

This template was made with the help of Generative AI (Claude, Anthropic).
