#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TARGET="${HEXTECH_STS2_TARGET:-0.107.1}"
GAME_DATA_DIR="${HEXTECH_GAME_DATA_DIR:-"$ROOT/versioned-dll-backups/$TARGET/game-refs"}"

dotnet run \
  --project "$ROOT/tests/HextechRunes.Tests/HextechRunes.Tests.csproj" \
  --configuration Release \
  -p:HextechSts2Target="$TARGET" \
  -p:GameDataDir="$GAME_DATA_DIR"
