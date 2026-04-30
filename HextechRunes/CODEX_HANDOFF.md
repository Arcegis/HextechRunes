# HextechRunes 交接注意事项

写给下一个接手本地 `/Users/iniad/sts2-mods/HextechRunes` 的 Codex/GPT。请先读完本文件，再做代码修改。

## 硬约束

- 始终用中文回复。
- 模组作者名保持 `Natsuki`。
- 没有用户明确要求时，不要推送 GitHub。
- 不要并行跑两个本工程 build。项目偶发会撞上 `HextechRunes.deps.json` 文件锁。
- 如果要按海克斯总表同步内容，动手前必须重新读取：
  `/Users/iniad/sts2-mods/HextechRunes/hextech_relics_summary.txt`
- 如果加日志，统一沿用 `[HextechRunes][Mayhem]` 前缀。
- Neow 修复不能回退：`StartRun` 必须先等原版 `StartRun` 完成，再 `EnsureMayhemModifier(runState)`，否则 Neow 会走 modifier 分支但没有选项。

## 常用验证命令

```bash
cd /Users/iniad/sts2-mods/HextechRunes && ./tools/build_and_deploy.sh
/Users/iniad/sts2-mods/tools/verify_headless_load.sh HextechRunes
```

游戏日志：

```text
/Users/iniad/Library/Application Support/SlayTheSpire2/logs/godot.log
```

## 当前未提交变更的大方向

当前工作区有一批未提交改动，主要是：

- 使用 `sts2-monomod-to-single-dll` 思路，把 `MonoMod.RuntimeDetour` Hook 重构为游戏自带 `0Harmony`。
- `HextechRunes.csproj` 去掉 `MonoMod.RuntimeDetour` / `CopyLocalLockFileAssemblies`，引用游戏包内 `0Harmony.dll` 且 `<Private>false</Private>`。
- `build_and_deploy.sh` 新增 Godot import project 流程，让 PCK 包含 Godot 导入资源。
- 新增 `CreatureCmdCompat.cs`，兼容 `CreatureCmd.SetMaxHp` 在不同版本/移植版里返回 `Task` 或 `Task<decimal>`。

我只读审查过这些改动，没有修改源码。C# 编译曾通过：

```bash
/opt/homebrew/bin/dotnet build HextechRunes/src/HextechRunes.csproj -c Release --no-restore
```

结果：0 warning / 0 error。

## 已看过的主要风险点

1. 资源导入版本风险：
   - 当前 `/opt/homebrew/bin/godot --version` 是 `4.6.1.stable`。
   - 游戏本体 Godot 版本显示为 `4.5.1.m.9.mono.custom_build`。
   - `build_and_deploy.sh` 默认用 `/opt/homebrew/bin/godot` 做资源 import，可能生成游戏/手机移植版不兼容的 `.ctex`。
   - 后续如要正式验证 single-dll/PCK 流程，优先确认是否能用与游戏一致的 4.5.x Godot Editor 导入资源。

2. Harmony postfix 顺序风险：
   - `HextechCombatHooks.CardCanPlayPostfix` / `CardCanPlayWithReasonPostfix` 通过 postfix 改 `__result` 来实现一板一眼限制。
   - 本模组单独运行语义基本等价，但如果其他模组也 postfix `CardModel.CanPlay` 并在本模组之后改回 `true`，可能导致限制被覆盖。
   - 这是兼容性风险，不是当前代码自身必然 desync。

3. `CreatureCmdCompat` 当前看起来没有绕过命令系统：
   - 它仍然反射调用 `CreatureCmd.SetMaxHp` 并 await 返回的 Task。
   - 因此我没有看到直接造成联机不同步的风险。

4. `StartRun` / `EventRoomProceed` 的 async Harmony 包装：
   - 当前仍是 `__result = XxxAfterOriginal(__result, ...)`，在 wrapper 内 `await original` 后执行海克斯逻辑。
   - 这符合之前修 Neow 和 act0 选择时序的要求。

## 后续接手建议

- 先不要重写大段逻辑。先跑一次 `git diff -- HextechRunes` 和 `git status --short`，确认用户最新改动。
- 如果继续 single-dll 重构，优先检查部署目录最终是否只有：
  - `HextechRunes.json`
  - `HextechRunes.dll`
  - `HextechRunes.pck`
- 如果要跑完整验证，按顺序来：
  1. `dotnet build`
  2. `./tools/build_and_deploy.sh`
  3. `verify_headless_load.sh HextechRunes`
  4. 看 `godot.log` 有没有 Harmony patch、资源加载、PCK 导入相关异常
- 如果遇到联机 desync，优先排查：
  - UI 操作是否消耗共享 RNG。
  - host/client 是否各自 roll 会影响战斗或奖励的全局结果。
  - 是否有只在本地玩家执行但改变共享状态的代码。
  - `PlayerChoiceResult` 是否完整同步了选择和 reroll 链。

## 不要踩的坑

- 不要把旧版 Neow modifier 注入时序改回来。
- 不要把服务器密码、账号等敏感信息写进仓库文件或提交记录。
- 不要在用户没说“更新 GitHub”时推送。
- 不要为了验证同时开两个 build。
- 不要把从海克斯池移除的旧符文又放回图鉴或抽选池，除非用户明确要求。
