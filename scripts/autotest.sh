#!/usr/bin/env bash
# Runs a scripted test session in Valheim (src/Dev/AutoTest*.cs, Debug builds only) and prints
# the results. It uses its own character, world and save folder, never the player's saves.
#   ./scripts/autotest.sh                 same as "all"
#   ./scripts/autotest.sh all             every scenario marked InAll, in one game, then the art guards.
#                                         The release check: "DONE pass=N fail=0" and "art guard:"
#   ./scripts/autotest.sh probe           measure the game into .devtest/probe.txt: versions, plugins,
#                                         layers, every button binding (keys and pad), UI assets
#   ./scripts/autotest.sh build_spot      find and level the flat build spot, equip the hammer
#   ./scripts/autotest.sh example_window  the example window: keyboard, mouse and pad (delete with src/Example)
#   ./scripts/autotest.sh readme_gifs     no test: records README GIF frames into .devtest/gifs/<name>/,
#                                         then: python3 scripts/make-gifs.py
# Add a line here when you add a scenario to AutoTest.Scenarios.
#
# AUTOTEST_CHAIN="a,b" ./scripts/autotest.sh all    runs only those, in that order.
# AUTOTEST_GIFS="example" ./scripts/autotest.sh readme_gifs   records only those clips.
# AUTOTEST_TIMEOUT=900                               seconds before giving up (600, all: 1800).
# Output (log, screenshots, result.txt, test saves) goes to .devtest/ in the repo.
# Never wrap this script in `timeout` (it runs under Rosetta here, the game gets 10-20x slower),
# never build or edit this script while it runs (the build deploys over the loaded DLL, and bash
# reads the script as it goes).
set -euo pipefail

SCENARIO="${1:-all}"
if [[ "$SCENARIO" == "all" ]]; then
  DEFAULT_TIMEOUT=1800
else
  DEFAULT_TIMEOUT=600
fi
TIMEOUT="${AUTOTEST_TIMEOUT:-$DEFAULT_TIMEOUT}"

# The repo walk at the end of "all": no image, mesh, material or bundle file in the repo
# (CLAUDE.md, the art rule). Set to false if this mod ships art on purpose, and drop the rule.
ART_GUARD=true

VALHEIM="${VALHEIM_INSTALL:-$HOME/Library/Application Support/Steam/steamapps/common/Valheim}"
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CSPROJ="$(ls "$REPO"/*.csproj | head -1)"
NAME="$(sed -n 's:.*<AssemblyName>\(.*\)</AssemblyName>.*:\1:p' "$CSPROJ")"
OUT="$REPO/.devtest"
LOG="$VALHEIM/BepInEx/LogOutput.log"
GAME_PROCESS="valheim.app/Contents/MacOS/Valheim"

if pgrep -f "$GAME_PROCESS" >/dev/null; then
  echo "!! Valheim is already running. Close it first."
  exit 1
fi

echo "==> building $NAME (Debug)"
dotnet build "$CSPROJ" -c Debug -v minimal -nologo

# Keep the test saves: generating the test world is slow the first time.
mkdir -p "$OUT"
rm -f "$OUT"/*.png "$OUT/result.txt" "$OUT/LogOutput.log"

echo "==> launching Valheim (scenario: $SCENARIO, timeout ${TIMEOUT}s)"
cd "$VALHEIM"
rm -f "$LOG"
AUTOTEST="$SCENARIO" AUTOTEST_MOD="$NAME" AUTOTEST_OUT="$OUT" ./run_bepinex.sh >"$OUT/run.log" 2>&1 &
trap 'pkill -f "$GAME_PROCESS" 2>/dev/null || true' EXIT

for ((i = 0; i < TIMEOUT; i++)); do
  [[ -f "$OUT/result.txt" ]] && break
  if ((i > 30)) && ! pgrep -f "$GAME_PROCESS" >/dev/null; then
    echo "!! the game closed before the test finished"
    break
  fi
  sleep 1
done

# Give the game a moment to quit and flush the log.
sleep 3
cp "$LOG" "$OUT/LogOutput.log" 2>/dev/null || true
grep -E "AUTOTEST|Exception" "$OUT/LogOutput.log" 2>/dev/null || echo "!! no BepInEx log (see CLAUDE.md, Apple Silicon)"

if [[ ! -f "$OUT/result.txt" ]]; then
  echo "!! no result: timeout or crash. Full log: $OUT/LogOutput.log"
  exit 1
fi

cat "$OUT/result.txt"

# The in-game guard walks the folders the mod writes to; this walks the repo, where only the
# autotest's own output may be images, plus the Thunderstore icon (that one path only).
if [[ "$SCENARIO" == "all" && "$ART_GUARD" == "true" ]]; then
  ART=$(find "$REPO" -path "$REPO/.git" -prune -o -path "$REPO/.devtest" -prune -o \
    -path "$REPO/bin" -prune -o -path "$REPO/obj" -prune -o \
    -path "$REPO/thunderstore/icon.png" -prune -o \
    \( -name '*.png' -o -name '*.jpg' -o -name '*.jpeg' -o -name '*.tga' -o -name '*.glb' \
    -o -name '*.fbx' -o -name '*.obj' -o -name '*.mat' -o -name '*.mesh' -o -name '*.bundle' \
    -o -name '*.asset' -o -name '*.prefab' \) -print)
  if [[ -n "$ART" ]]; then
    echo "!! game art in the repo:"
    echo "$ART"
    exit 1
  fi
  echo "art guard: no image, mesh, material or bundle file in the repo"
fi

grep -q "fail=0" "$OUT/result.txt"
