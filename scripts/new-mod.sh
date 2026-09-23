#!/usr/bin/env bash
# Turns this template into your mod. Run it once, right after you copied the template.
#   ./scripts/new-mod.sh <ModName> <Author>
#   ./scripts/new-mod.sh HearthAndHome mikamarik
#
# What it does:
#   - MyValheimMod -> <ModName> everywhere (namespace, assembly, plugin class, config, manifest),
#     and renames MyValheimMod.csproj
#   - the plugin GUID becomes com.<author>.<modname> (lowercase), package.json's name <mod-name>
#   - YourName -> <Author> (LICENSE, manifest website_url)
#   - README.md becomes the player-facing skeleton in docs/templates/README.mod.md (the
#     Thunderstore page), and the template's own README goes
#   - the "template" block at the top of CLAUDE.md goes, and this script deletes itself
# Then: dotnet build, ./scripts/autotest.sh all, and commit.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO"

NAME="${1:-}"
AUTHOR="${2:-}"
if [[ -z "$NAME" || -z "$AUTHOR" ]]; then
  echo "usage: ./scripts/new-mod.sh <ModName> <Author>"
  exit 1
fi

# Thunderstore allows [A-Za-z0-9_] in a package name; a C# namespace wants a letter first.
[[ "$NAME" =~ ^[A-Za-z][A-Za-z0-9_]*$ ]] || { echo "!! ModName: letters, digits and _ only, a letter first"; exit 1; }
[[ "$AUTHOR" =~ ^[A-Za-z0-9_-]+$ ]] || { echo "!! Author: letters, digits, _ and - only"; exit 1; }
[[ -f MyValheimMod.csproj ]] || { echo "!! MyValheimMod.csproj is gone: the template was already renamed"; exit 1; }

LOWER="$(echo "$NAME" | tr '[:upper:]' '[:lower:]')"
AUTHOR_LOWER="$(echo "$AUTHOR" | tr '[:upper:]' '[:lower:]')"
# HearthAndHome -> hearth-and-home
KEBAB="$(echo "$NAME" | sed -E 's/([a-z0-9])([A-Z])/\1-\2/g; s/_/-/g' | tr '[:upper:]' '[:lower:]')"

# Every text file in the repo, tracked or not, but no build output, test output or git data.
FILES=$(find . \( -path ./.git -o -path ./bin -o -path ./obj -o -path ./.devtest -o -path ./thunderstore/build \) -prune -o \
  -type f \( -name '*.cs' -o -name '*.csproj' -o -name '*.props' -o -name '*.json' -o -name '*.md' \
  -o -name '*.sh' -o -name '*.py' -o -name 'LICENSE' -o -name '.gitignore' \) -print)

echo "==> $NAME by $AUTHOR (guid com.$AUTHOR_LOWER.$LOWER, npm $KEBAB)"
for f in $FILES; do
  [[ "$f" == "./scripts/new-mod.sh" ]] && continue
  perl -pi -e "s/MyValheimMod/$NAME/g; s/myvalheimmod/$LOWER/g; s/my-valheim-mod/$KEBAB/g; s/com\.yourname\./com.$AUTHOR_LOWER./g; s/YourName/$AUTHOR/g" "$f"
done

mv MyValheimMod.csproj "$NAME.csproj"

# README.md is the Thunderstore page from now on.
mv docs/templates/README.mod.md README.md
rmdir docs/templates 2>/dev/null || true

# The template's own "start here" block in CLAUDE.md, and the file list's template-only lines.
perl -0pi -e 's/<!-- template:start -->.*?<!-- template:end -->\n*//s' CLAUDE.md
perl -ni -e 'print unless m{^docs/templates/|^\s+\(new-mod\.sh moves it|^scripts/new-mod\.sh}' CLAUDE.md
perl -pi -e "s/the template's own readme until new-mod.sh swaps in the player page/the player-facing readme, also the Thunderstore page/" CLAUDE.md

# This script has done its job; a second run would refuse anyway.
rm -- "$0"

echo "==> done. Next:"
echo "   1. dotnet build                       (0 errors, deploys into BepInEx/plugins/$NAME)"
echo "   2. ./scripts/autotest.sh all          (DONE pass=N fail=0, art guard)"
echo "   3. fill in CLAUDE.md: What this mod is, Scope"
echo "   4. git add -A && git commit -m \"chore: start $NAME from the template\""
echo "   5. when the first feature lands: delete src/Example and src/Dev/AutoTest.Example.cs"
