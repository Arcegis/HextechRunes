#!/usr/bin/env bash
set -euo pipefail

# 用法:
#   bash tools/run_tests.sh                          全部发布目标各构建一遍并跑全套测试,再构建两个加载器
#   bash tools/run_tests.sh --target 0.111.0 A B    只对一个目标构建,只跑名为 A、B 的测试(hextech_dev.py tests 的执行入口)
# 也可用环境变量 HEXTECH_STS2_TARGET 指定单一目标、HEXTECH_GAME_DATA_DIR 覆盖引用目录。

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
HEXTECH_TOOLS_DIR="$ROOT/tools"
# shellcheck source=lib_build.sh
source "$HEXTECH_TOOLS_DIR/lib_build.sh"

VARIANT_MANIFEST_NAME="hextech-runes-variants.manifest"
TEST_PROJECT="$ROOT/tests/HextechRunes.Tests/HextechRunes.Tests.csproj"
TEST_DLL="$ROOT/tests/HextechRunes.Tests/bin/Release/net9.0/HextechRunes.Tests.dll"
SPONSOR_ROOT="$ROOT/../HextechRunesSponsorPack"
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE="${DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE:-true}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE="${DOTNET_SKIP_FIRST_TIME_EXPERIENCE:-1}"

REQUESTED_TARGET="${HEXTECH_STS2_TARGET:-}"
TEST_NAMES=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --target)
      [[ $# -ge 2 ]] || { echo "--target 需要一个版本号" >&2; exit 2; }
      REQUESTED_TARGET="$2"
      shift 2
      ;;
    *)
      TEST_NAMES+=("$1")
      shift
      ;;
  esac
done

if [[ ${#TEST_NAMES[@]} -gt 0 && -z "$REQUESTED_TARGET" ]]; then
  echo "定向测试需要 --target" >&2
  exit 2
fi

# 加载器的结构性不变式(共享源码由两个加载器工程同时编译,身份常量与选择规则由测试直接断言)。
grep -Fq '<AssemblyName>HextechRunes.Loader</AssemblyName>' "$ROOT/loader/HextechRunes.Loader.csproj"
grep -Fq '<AssemblyName>HextechRunesSponsorPack.Loader</AssemblyName>' "$SPONSOR_ROOT/loader/HextechRunesSponsorPack.Loader.csproj"
grep -Fq '[ModInitializer(nameof(Initialize))]' "$ROOT/loader/LoaderBootstrap.cs"
grep -Fq 'LinuxNativeDependencyBootstrap.EnsureHarmonyRuntimeDependenciesVisible();' "$ROOT/loader/LoaderBootstrap.cs"
grep -Fq 'libgcc_s.so.1' "$ROOT/loader/LinuxNativeDependencyBootstrap.cs"
grep -Fq 'RtldGlobal' "$ROOT/loader/LinuxNativeDependencyBootstrap.cs"
grep -Fq 'AssociateAssemblyWithMod' "$ROOT/loader/LoaderBootstrap.cs"
grep -Fq 'ReflectionHelperModTypesPostfix' "$ROOT/loader/LoaderBootstrap.cs"
grep -Fq 'ModManager.OnModDetected += OnLegacyModDetected' "$ROOT/loader/LoaderBootstrap.cs"
grep -Fq 'LegacyModAssemblyField?.SetValue(mod, _selectedVariantAssembly)' "$ROOT/loader/LoaderBootstrap.cs"
grep -Fq '<AssemblyMetadata Include="HextechCompatibilityTarget"' "$ROOT/src/HextechRunes.csproj"
grep -Fq '<AssemblyMetadata Include="HextechSponsorCompatibilityTarget"' "$SPONSOR_ROOT/src/HextechRunesSponsorPack.csproj"

# 默认对各发布目标各跑一遍(引用目录存在才跑);指定目标则只跑该目标。
if [[ -n "$REQUESTED_TARGET" ]]; then
  TARGETS=("$REQUESTED_TARGET")
else
  TARGETS=()
  for candidate in "${HEXTECH_TARGETS[@]}"; do
    if [[ -f "$ROOT/versioned-dll-backups/$candidate/game-refs/sts2.dll" ]]; then
      TARGETS+=("$candidate")
    fi
  done
  if [[ ${#TARGETS[@]} -eq 0 ]]; then
    TARGETS=("${HEXTECH_TARGETS[0]}")
  fi
fi

for TARGET in "${TARGETS[@]}"; do
  GAME_DATA_DIR="${HEXTECH_GAME_DATA_DIR:-"$ROOT/versioned-dll-backups/$TARGET/game-refs"}"
  echo "== Building tests against STS2 $TARGET =="
  dotnet build \
    "$TEST_PROJECT" \
    --configuration Release \
    --no-incremental \
    "${HEXTECH_BUILD_STABILITY_ARGS[@]}" \
    -p:HextechSts2Target="$TARGET" \
    -p:HextechSponsorSts2Target="$TARGET" \
    -p:GameDataDir="$GAME_DATA_DIR"
  echo "== Running tests against STS2 $TARGET =="
  if [[ ${#TEST_NAMES[@]} -gt 0 ]]; then
    dotnet "$TEST_DLL" "${TEST_NAMES[@]}"
  else
    dotnet "$TEST_DLL"
  fi
done

# 定向测试只关心所选用例,跳过加载器与发行包检查。
if [[ ${#TEST_NAMES[@]} -gt 0 ]]; then
  exit 0
fi

# 两个加载器都按发布方式单独对最低目标构建一次(测试工程引用它们时用的是测试目标的引用)。
for LOADER_PROJECT in "$ROOT/loader/HextechRunes.Loader.csproj" "$SPONSOR_ROOT/loader/HextechRunesSponsorPack.Loader.csproj"; do
  dotnet build \
    "$LOADER_PROJECT" \
    --configuration Release \
    "${HEXTECH_BUILD_STABILITY_ARGS[@]}" \
    -p:GameDataDir="$ROOT/versioned-dll-backups/$HEXTECH_LOADER_REFS_TARGET/game-refs"
done

if [[ -f "$ROOT/dist/$VARIANT_MANIFEST_NAME" ]]; then
  python3 \
    "$ROOT/tools/multi_version/validate_variant_bundle.py" \
    --dist "$ROOT/dist" \
    --mod-id "HextechRunes" \
    --manifest-name "$VARIANT_MANIFEST_NAME"
fi
