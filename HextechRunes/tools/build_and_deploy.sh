#!/bin/zsh
set -euo pipefail

DOTNET="${DOTNET:-/opt/homebrew/bin/dotnet}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
FILE_STEM="HextechRunes"
MANIFEST_SRC="$ROOT/assets/$FILE_STEM.json"
GAME_APP="/Users/iniad/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app"
GAME_BIN="$GAME_APP/Contents/MacOS/Slay the Spire 2"
MOD_DIR="$GAME_APP/Contents/MacOS/mods/$FILE_STEM"
BUILD_OUT="$ROOT/src/bin/Release/net9.0"
PROJECT_PATH="$ROOT/src/$FILE_STEM.csproj"
IMPORT_PROJECT="$ROOT/.build/import_project"
GODOT_EDITOR="${GODOT_EDITOR:-/opt/homebrew/bin/godot}"

major_minor_version() {
  sed -E 's/^([0-9]+[.][0-9]+).*/\1/' <<< "$1"
}

GAME_GODOT_VERSION="$("$GAME_BIN" --version 2>/dev/null | head -n 1)"
IMPORT_GODOT_VERSION="$("$GODOT_EDITOR" --version 2>/dev/null | head -n 1)"
if [[ -n "$GAME_GODOT_VERSION" && -n "$IMPORT_GODOT_VERSION" \
  && "$(major_minor_version "$GAME_GODOT_VERSION")" != "$(major_minor_version "$IMPORT_GODOT_VERSION")" ]]; then
  print -u2 "Warning: asset import Godot version ($IMPORT_GODOT_VERSION) differs from game runtime ($GAME_GODOT_VERSION)."
  print -u2 "Set GODOT_EDITOR to a matching 4.5.x editor if mobile/runtime texture compatibility regresses."
fi

rm -rf "$ROOT/src/bin" "$ROOT/src/obj" "$ROOT/dist" "$ROOT/.build"

python3 "$ROOT/tools/validate_hextech_content.py"

"$DOTNET" build "$PROJECT_PATH" -c Release

mkdir -p "$ROOT/dist"
rm -rf "$MOD_DIR"
mkdir -p "$MOD_DIR"
mkdir -p "$IMPORT_PROJECT/$FILE_STEM"

cp "$ROOT/tools/project.godot" "$IMPORT_PROJECT/project.godot"
rsync -a --exclude "$FILE_STEM.json" "$ROOT/assets/" "$IMPORT_PROJECT/$FILE_STEM/"

"$GODOT_EDITOR" --headless \
  --path "$IMPORT_PROJECT" \
  --import

cp "$MANIFEST_SRC" "$ROOT/dist/$FILE_STEM.json"
cp "$ROOT/dist/$FILE_STEM.json" "$MOD_DIR/$FILE_STEM.json"

"$GAME_BIN" --headless \
  --path "$ROOT/tools" \
  -s res://pack_mod.gd -- \
  "$MANIFEST_SRC" \
  "$ROOT/dist/$FILE_STEM.pck" \
  "$IMPORT_PROJECT"

cp "$ROOT/dist/$FILE_STEM.pck" "$MOD_DIR/$FILE_STEM.pck"

for dll in "$BUILD_OUT"/*.dll; do
  base_name="$(basename "$dll")"
  case "$base_name" in
    sts2.dll|GodotSharp.dll)
      continue
      ;;
  esac

  cp "$dll" "$ROOT/dist/$base_name"
  cp "$dll" "$MOD_DIR/$base_name"
done

echo "Deployed to $MOD_DIR"
