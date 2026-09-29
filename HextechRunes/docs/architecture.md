# HextechRunes 架构重构路线

本文件记录已采用的分层与后续重构方向，不是要求立即执行的迁移清单。日常维护先读 [开发规范](development.md) 和 [工具手册](developer-tools.md)；结构重构不改变游戏行为、模型 ID、随机算法或联机协议语义。

## 依赖方向

长期目标是让业务逻辑沿单向依赖流动：

```text
Platform/Hooks/Config/Localization/Telemetry
  -> Mayhem
  -> Selection
  -> Core/Catalog/Runes/EnemyHexes
```

多人同步和随机数属于横切能力，所有会影响联机一致性的选择结果都必须通过明确 payload 或稳定随机输入表达，不允许依赖“各端本地池子刚好一样”。

## 补丁层（2026-09 重构后）

所有 Harmony 补丁都是自描述的嵌套静态类，入口 `ModEntry.Initialize` 只做编排，不再有安装顺序契约：

- 声明：`[HarmonyPatch(...)]` + `[HextechPatch(id, feature, Rune=/Runes=/Optional=)]`，由 `src/Patching/HextechPatcher.ApplyAll` 统一应用，逐条成败可见；`Optional=true` 的目标缺失只记 Info。目标需要运行时枚举时，类只带 `[HextechPatch]` 并声明 `static void Apply(Harmony)`。
- 同一目标的执行序只由 `[HarmonyPriority]` / `[HarmonyAfter]` 决定，禁止依赖安装顺序。
- 符文专属补丁内嵌在符文自己的文件里（如 `SurvivorUpgradeRune` 里的 `SurvivorPatch`）；`src/Hooks/**` 只放跨符文的横切补丁（战斗、UI、资源、商店、运行生命周期）与共享辅助。
- 版本差异写成整文件 `#if` 的分部文件（`HextechSavedPropertyBootstrap.Legacy.cs` / `.Official.cs`），共享代码里不写 `#if`；剩余的 `#if` 只允许出现在原版虚方法签名随版本变化的覆写处。
- 私有成员访问集中经 `HextechHookReflection`，缺失成员会在启动摘要里列出。

三道护栏（`tests/HextechRunes.Tests/`，三编译目标各一份，用 `HEXTECH_WRITE_PATCH_MANIFEST=1` 重生成）：`patch_manifest.<target>.txt` 冻结补丁目标与优先级；`static_state_manifest.<target>.txt` 冻结可变静态字段清单；`tests/vanilla_copy_guard.0.111.0.txt` 冻结所有可跳过原方法的 bool 前缀目标的 IL 哈希，游戏更新后 headless 日志出现 `[VanillaCopyGuard] DRIFT` 即需复核。

## 当前 Selection 分层

`src/Selection` 已按职责拆分为以下目录：

- `Coordinator/`：选择流程编排。负责何时弹界面、何时等待远端、何时落地奖励。这里可以调用其它 selection 服务，但不应继续堆具体池生成和同步编解码细节。
- `Pool/`：玩家海克斯池生成、过滤、标签权重、幕数限制、配置过滤边界。`HextechRunePoolBuilder` 是当前池构建入口，Coordinator 只保留兼容门面和流程侧调用点。
- `Reroll/`：玩家海克斯重随机制。所有重随逻辑必须保持本地 UI 随机不推进共享 run RNG，联机路径要保持可重放或同步最终选项。
- `Sync/`：多人选择 payload、远端等待、选择确认、ack 等同步边界。只负责“传什么、如何还原”，不负责具体 UI。
- `EnemyAdjust/`：选择界面里的敌方海克斯重随/移除同步。
- `AI/`：AI 队友或主机代选逻辑。
- `UI/`：`HextechRuneSelectionScreen` 的渲染、交互、hover、音效、布局。

## 已裁决保留的补丁

以下补丁点在 2026-09 重构中逐条评估过，结论是**保留**，理由已核实，不要再翻案；要改先拿出新的原版证据。

- `CardPileCmd.Draw` 前缀：卡牌检视是"用选牌界面替换抽牌返回值"，`ShouldDraw` / `ModifyHandDraw` / `BeforeHandDraw` 都表达不了，且改走 Hook 会把 PlayerChoice 挪到不同的同步点。
- `RunManager.OnEnded` 前缀 + 后缀：前缀必须在 `ToSave` 之前补战斗历史（原版败北存档缺房间记录），`OnMetricsUpload` 只在 `ShouldSave` 且首次上报时触发，替不了。
- `NGame.StartRun` / `LoadRun`：需要包住原版 UI 任务链再做延续，`RunManager.RunStarted` 只在模型层触发。
- 剩余的资源图标补丁组：理论上可整组删除，但纹理加载多轮返工过，headless 验证不了视觉，必须真机看过遗物栏/图鉴/检视/悬浮四处再删。
- 复视奖励事务 8 个补丁：改用 `AfterRewardTaken` 需要重新设计"同一事务只复制一次"的幂等键，属于重做而非迁移。
- 濒死狂宴的 `GainBlock` 跳过 **不**改成 `ModifyBlock=0`：原版对 0 格挡仍播音效/特效并触发 Before/AfterBlockGained。同理治疗管线的"禁止回血"也必须继续跳过原方法（`CreatureCmd.Heal` 的 amount 为 0 也会播演出），只给封顶前缀补 `[HarmonyAfter]` RitsuLib / BaseLib。
- 星尘的 `SpendResources` 跳过 **不**改成 `ModifyStarCost=0`：那会让牌在没有星星时也能打出，是语义变化。
- 遗忘 / 腐蚀波 / 主宰等升级符文的 Power 前缀：只对"持有符文的玩家自己的 Power 实例"生效，官方 Hook 无法表达"按实例替换回调"。
- 敌方海克斯的联机缩放前缀：已用 AsyncLocal 限定在本模组自己的 `PowerCmd.Apply` 窗口内，不必子类化。
- 形态自动打出的代表牌必须继续走原版出牌管线（`CardCmd.AutoPlay`）：那里才有附魔、流电、克隆语义；改成直接施加 Power 会全部丢失。

## Source of truth 方向

后续重构应收敛到单一内容元数据源：

- 符文 ID、稀有度、角色池、标签、默认禁用、是否进入图鉴、是否进入抽选池都应从同一份 catalog metadata 派生。
- 本地化、配置界面、统计中文名、图鉴可见性不应各自维护重复名单。
- 在正式切换前，应保留旧 registry 与新 metadata 的双读对比，确认输出一致后再删除旧路径。

## 高风险规则

- 不在结构重构中顺手改平衡、文案或触发时机。
- 不改变模型注册顺序，除非明确重打版本并接受联机 hash 改变。
- 不改变 `PlayerChoiceResult` 的既有语义；如需扩展 payload，必须兼容旧 payload 或提供明确 fallback。
- 多人池过滤不能只同步 index；只要各客户端候选池可能不同，就必须同步最终选项 ID。
- 按工作区设计哲学第 8 节选择与改动相关的验证；目录搬移核对引用，补丁重构核对目标与优先级。构建、部署、加载和实机是不同交付层级，不要求每次重构都部署或启动游戏。
