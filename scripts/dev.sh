#!/usr/bin/env bash
# Build the mod, deploy it, launch Valheim, and stream our log lines.
#   ./scripts/dev.sh           normal run
#   ./scripts/dev.sh --debug   also open the Mono soft debugger on 127.0.0.1:10000
# macOS only (run_bepinex.sh, the .app layout). See CLAUDE.md, Apple Silicon.
set -euo pipefail

VALHEIM="${VALHEIM_INSTALL:-$HOME/Library/Application Support/Steam/steamapps/common/Valheim}"
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CSPROJ="$(ls "$REPO"/*.csproj | head -1)"
NAME="$(sed -n 's:.*<AssemblyName>\(.*\)</AssemblyName>.*:\1:p' "$CSPROJ")"

EXTRA=()
if [[ "${1:-}" == "--debug" ]]; then
  # NOTE: this doorstop flag takes a value; passing it bare aborts run_bepinex.sh
  EXTRA+=(--doorstop-mono-debug-enabled true)
  echo "==> Mono debugger will listen on 127.0.0.1:10000"
fi

echo "==> building $NAME"
dotnet build "$CSPROJ" -v minimal

echo "==> launching Valheim"
cd "$VALHEIM"
rm -f BepInEx/LogOutput.log
./run_bepinex.sh "${EXTRA[@]}" >"/tmp/$NAME-run.log" 2>&1 &

# run_bepinex.sh execs the game, so trap cleanup on Ctrl-C
trap 'pkill -f "valheim.app/Contents/MacOS/Valheim" 2>/dev/null || true' EXIT

echo "==> waiting for BepInEx"
for _ in $(seq 1 90); do
  [[ -f BepInEx/LogOutput.log ]] && break
  sleep 1
done

if [[ ! -f BepInEx/LogOutput.log ]]; then
  echo "!! BepInEx never wrote a log."
  echo "!! Check ARCHPREFERENCE in run_bepinex.sh is x86_64 (arm64 fails silently)."
  exit 1
fi

echo "==> streaming $NAME + errors (Ctrl-C to quit)"
tail -f BepInEx/LogOutput.log | grep --line-buffered -E "$NAME|Error|Exception|Harmony"
