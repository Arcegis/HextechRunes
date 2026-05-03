#!/bin/zsh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
FILE_STEM="HextechRunes"
DIST_DIR="$ROOT/dist"
PACKAGE_ROOT="$ROOT/.build/package"
PACKAGE_DIR="$PACKAGE_ROOT/$FILE_STEM"
ZIP_PATH="${1:-$DIST_DIR/$FILE_STEM.zip}"

clean_macos_metadata() {
  local target="$1"
  [[ -d "$target" ]] || return 0

  find "$target" -name "__MACOSX" -type d -prune -exec rm -rf {} +
  find "$target" -name ".DS_Store" -type f -delete
  find "$target" -name "._*" -type f -delete
}

for required in "$DIST_DIR/$FILE_STEM.json" "$DIST_DIR/$FILE_STEM.pck" "$DIST_DIR/$FILE_STEM.dll"; do
  if [[ ! -f "$required" ]]; then
    print -u2 "Missing release artifact: $required"
    print -u2 "Run tools/build_and_deploy.sh before packaging."
    exit 1
  fi
done

rm -rf "$PACKAGE_ROOT"
mkdir -p "$PACKAGE_DIR"
cp "$DIST_DIR/$FILE_STEM.json" "$PACKAGE_DIR/$FILE_STEM.json"
cp "$DIST_DIR/$FILE_STEM.pck" "$PACKAGE_DIR/$FILE_STEM.pck"
cp "$DIST_DIR/$FILE_STEM.dll" "$PACKAGE_DIR/$FILE_STEM.dll"
clean_macos_metadata "$PACKAGE_ROOT"

mkdir -p "$(dirname "$ZIP_PATH")"
rm -f "$ZIP_PATH"
(
  cd "$PACKAGE_ROOT"
  COPYFILE_DISABLE=1 zip -r -X "$ZIP_PATH" "$FILE_STEM" \
    -x "__MACOSX/*" "*/__MACOSX/*" ".DS_Store" "*/.DS_Store" "._*" "*/._*"
)

if unzip -l "$ZIP_PATH" | rg -q '(__MACOSX|/[.]_|[.]DS_Store)'; then
  print -u2 "Package still contains macOS metadata: $ZIP_PATH"
  exit 1
fi

echo "Packaged clean release zip: $ZIP_PATH"
