#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# 默认对两个发布目标各跑一遍(引用目录存在才跑);显式设 HEXTECH_STS2_TARGET 则只跑该目标。
if [[ -n "${HEXTECH_STS2_TARGET:-}" ]]; then
  TARGETS=("$HEXTECH_STS2_TARGET")
else
  TARGETS=()
  for candidate in 0.107.1 0.108.0; do
    if [[ -f "$ROOT/versioned-dll-backups/$candidate/game-refs/sts2.dll" ]]; then
      TARGETS+=("$candidate")
    fi
  done
  if [[ ${#TARGETS[@]} -eq 0 ]]; then
    TARGETS=(0.107.1)
  fi
fi

for TARGET in "${TARGETS[@]}"; do
  GAME_DATA_DIR="${HEXTECH_GAME_DATA_DIR:-"$ROOT/versioned-dll-backups/$TARGET/game-refs"}"
  echo "== Running tests against STS2 $TARGET =="
  dotnet run \
    --project "$ROOT/tests/HextechRunes.Tests/HextechRunes.Tests.csproj" \
    --configuration Release \
    -p:HextechSts2Target="$TARGET" \
    -p:GameDataDir="$GAME_DATA_DIR"
done
