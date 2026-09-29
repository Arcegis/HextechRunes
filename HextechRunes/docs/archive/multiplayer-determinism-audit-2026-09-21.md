# 联机确定性审计 — 2026-09-21

作者：Natsuki。基线：`6a5ec941`。范围：`src` 的 **867 个 C# 文件**；不含 `bin/obj/dist/releases/versioned-dll-backups`。未提交、推送、部署或上传工坊。

## 证据和完成范围

完成全源码 R1–R11 候选扫描、关键调用链上下文复核、原版类型反编译以及编译后自有表现事件调用点核验。附录逐文件区分“上下文复核”和“静态筛查”；**静态筛查不等于逐行语义证明，无命中也不计为安全发现**。仍需裁决的共享异步链和配置回退见下表；本报告不宣称消除了所有可能的联机问题。

原版真值通过 `../tools/sts2-inspect decompile <完整类型名>` 获取，主要核对：`RelicModel`、`PowerModel`、`CardModel`、`NRelicInventoryHolder`、`CardFactory`、`InfernoPower`、`SkittishPower`、`StormPower`、`EntropyPower`、`SleightOfFleshPower`、`PlayerChoiceSynchronizer`。关键事实：

- `RelicModel.Flash` 直接同步调用 `Flashed` 事件；`NRelicInventoryHolder.DoFlash` 直接加载闪光场景及遗物图标，没有异常隔离。非持有端未订阅相同 UI 事件时，持有端可以先抛异常而另一端继续共享命令。
- `PowerModel.Flash/InvokeDisplayAmountChanged` 和卡牌费用刷新事件也直接分发本机订阅者。
- `CardFactory.FilterForCombat` 排除不可战斗生成以及 Basic、Ancient、Event；`GetForCombat/GetDistinctForCombat` 内部已调用过滤。变化牌走独立的 `GetFilteredTransformationOptions` 契约，不应该不加区分地套普通生成过滤。
- `StormPower` 在出牌前保存层数、出牌后移除记录，避免首次打雷暴自触发；模组 `HextechMayhem.CardEvents` 已保存这道守卫。
- `PlayerChoiceSynchronizer.ReserveChoiceId` 按玩家槽位递增独立计数。保留原版序列及现有 operation token，不另建编号协议。

计数以表中审计条目为单位；共用边界是一个条目，不把同根因数百处调用重复计数。

| 类型 | 确定 | 可疑/待裁决 | 已复核安全 |
| --- | ---: | ---: | ---: |
| R1 表现异常截断共享状态 | 20（已修 19，F04 回滚待裁决） | 0 | 2 |
| R2 异步异常/取消边界 | 0 | 1 | 2 |
| R3 保存注册/net-id | 0 | 0 | 2 |
| R4 随机生成过滤 | 0 | 0 | 5 |
| R5 随机源/UI RNG | 0 | 0 | 2 |
| R6 墙钟/帧时间 | 0 | 0 | 2 |
| R7 本地身份/本地配置 | 0 | 1 | 2 |
| R8 无序枚举 | 0 | 1 | 3 |
| R9 原版能力内部守卫 | 0 | 0 | 3 |
| R10 玩家选择计数 | 0 | 1 | 1 |
| R11 死亡后失效状态 | 0 | 0 | 2 |
| R12 共享命令脱离受等待的任务链 | 1（待裁决） | 0 | 1 |

**合计：确定 21 条，其中局部修复 19 条、待裁决 2 条（含已回滚的 F04）；另外可疑 4 条；安全裁决 27 条。** 安全裁决仅覆盖列明的机制，不替代整类/整个文件的全部运行时保证。

## 确定发现和局部修复

### 两任务整合后的最终验证补记

本报告的三版全量 `322/322` 在联机修复完成后执行。随后文案任务补充了 **24 个模型、21 个文件、28 个纯显示 DynamicVar**；逐项复核原有 CanonicalVar 数值均未改变，也没有修改玩法 Hook、SavedProperty、模型身份或同步协议。不要把此前全量结果表述为最后这批显示变量修改之后的全量结果。

最终 C# 冻结后，0.107.1、0.110.0、0.111.0 串行执行 Metadata、保存清单与注册、net-id、静态清单及锻造器掉率定向组，三个版本均输出：

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
11/11 tests passed
```

原始日志为 `/tmp/hextech-display-vars-final-0107-20260921.log`、`/tmp/hextech-display-vars-final-0110-20260921.log`、`/tmp/hextech-display-vars-final-0111-20260921.log`。所有构建 `HEXTECH_DEPLOY=0`，共享输出目录串行使用；这些结果仍不是双客户端联机实测证据。

### 修复条目

共同触发条件是仅一端资源缺失、缓存卸载、节点失效或 UI 订阅者抛异常。以下均是源码可证的条件性中断风险；没有伪称已用双客户端复现。严重度 P1 表示异常可截断同步链，并不代表正常资源条件下必然发生。

| ID | 类型/级别 | 文件:行（修复后入口） | 修复前具体分叉机制 | 修复及边界 |
| --- | --- | --- | --- | --- |
| F01 | R1/P1 | `src/Runes/InfernoUpgradeRune.cs:16` | 一端火焰场景创建异常则不执行伤害/灼烧，另一端执行 | 捕获原敌人列表，伤害和灼烧后才播放火焰；只捕获表现异常 |
| F02 | R1/P1 | `src/Powers/HextechGalvanicPower.cs:46` | 一端闪电 VFX 失败则不承受流电自损 | 自损先完成，VFX 独立隔离 |
| F03 | R1/P1 | `src/Runes/MadScientistRune.cs:62` | 两端虽已加球槽，持有端槽动画异常会打断后续引导球/Hook | 保留容量写入，隔离 AddSlotAnim |
| F04 | R1/P1 | `src/Combat/HextechMonsterInteractionPolicy.cs:129` | 一端 BlockEnd 音画失败则保留 Skittish，另一端移除 | **已回滚，待裁决**：曾改为先移除 Power 再补出场音画；该顺序是玩家实报"感受燃烧打四鳗卡死"的修复点，未经实机验证不调整，源码保持 `6a5ec941` 原样 |
| F05 | R1/P1 | `src/Hooks/Combat/HextechEncounterCompatibilityHooks.cs:30` | 0.107.1 无私人蜂巢的替代行动，一端 Cast 失败则不加力量 | 力量命令先完成；Cast 音画独立隔离 |
| F06 | R1/P1 | `src/Combat/HextechPlayerBodyScaleHelper.cs:9` | 玩家节点缩放异常向增益/获得符文任务链冒泡 | 只隔离最终节点缩放，数值计算不变 |
| F07 | R1/P1 | `src/Mayhem/HextechMayhem.PersistentHexes.cs:269` | 某端敌人体型节点失效中断敌方效果迭代 | 只隔离 SetDefaultScaleTo |
| F08 | R1/P1 | `src/Mayhem/HextechCombatCreatureHelper.cs:69` | 残留死敌的本地节点清理失败发生在模型移除前，只有另一端删去实体 | 先移除两份共享实体记录，再隔离节点清理 |
| F09 | R1/P1 | `src/Runes/DoubleVisionRune.Duplication.cs:150` | 复制遗物后仅持有方 Flash；抛异常会跳过 RewardSynchronizer 广播 | 先广播复制结果，再本地 Flash；Flash 由自有基类隔离 |
| F10 | R1/P1 | `src/Relics/Base/HextechRelicBase.TurnProc.cs:10` | 大量符文先 Flash/刷新计数，再发共享命令；UI 事件仅在部分端订阅 | 在自有遗物边界隐藏非虚 Flash 双重载及计数事件，仅捕获表现回调；Deferred 入口也隔离；不拦原版/第三方全局事件 |
| F11 | R1/P1 | `src/Compat/HextechModelBaseCompat.cs:8` | 自有能力 Flash/计数事件可以阻断后续 PowerCmd | 自有 Power 边界仅隔离两个表现事件 |
| F12 | R1/P1 | `src/Powers/HextechPowers.cs:151` | 直接继承 PowerModel 的重放能力在 Flash 失败端不移除，后续重放数分叉 | 先 Remove，再局部 Flash；不改变基类或保存身份 |
| F13 | R1/P1 | `src/Cards/HextechDragonSoulPowers.cs:132` | 龙魂加能量前 Flash 失败，只有另一端获得能量 | 等待 GainEnergy 后隔离 Flash |
| F14 | R1/P1 | `src/Relics/Orobas/HextechBlackBloodPlus.cs:12` | Orobas 基类不是 HextechRelicBase；黑血 Flash 失败端漏战后治疗 | 先治疗，再隔离 Flash，不改变继承关系 |
| F15 | R1/P1 | `src/Hooks/Runes/HextechPlayerRuneHooks.IllusoryWeapon.cs:76` | 反射表现失败后的原版 relic.Flash 在调用 TaskHelper 前同步抛错，漏能量/敏捷/力量/格挡 | 四条命令先写状态；表现方法等待反射 Task 并隔离 fallback Flash；catch 内没有共享命令 |
| F16 | R1/P1 | `src/Mayhem/HextechAttackCostPreviewRefresher.cs:25` | 一端费用标签更新异常截断后续敌方 Hook | 只捕获每张牌的 InvokeEnergyCostChanged，不包围玩法遍历 |
| F17 | R1/P1 | `src/Helpers/HextechKnifeHelper.cs:76` | 巨型小刀配置后费用事件异常使该端中止后续生成/入手 | 只隔离最后的费用 UI 事件 |
| F18 | R1/P1 | `src/Runes/ReanimateUpgradeRune.cs:67` | 复生费用刷新中一张 UI 牌失效使该端中止 Hook | 每张复生牌的 UI 事件单独隔离 |
| F19 | R1/P1 | `src/EnemyHexes/EndlessRotationEnemyHex.cs:7` | 手中第一张费用加成后 UI 事件抛异常，该端余下牌不加费，另一端全加 | 每张牌写入后隔离费用事件，继续同序遍历 |
| F20 | R1/P1 | `src/Runes/SomethingForNothingRune.cs:73` | 本场减费已写入，但本地费用事件抛错阻断余下结算 | 只隔离费用事件，次数及费用写入不变 |

F10/F11 **保留原成功路径的表现事件时机**，不声称把所有 Flash 都搬到了全部共享写入之后。这样避免为了统一异常边界改写数百个同步/异步 Hook 的成功顺序；只隔离原版本来就属于 UI 的事件。新发现的直接 VFX/音频/节点路径采用状态先完成的标准顺序。未新增 Harmony 补丁、模型/配置 ID、保存字段、枚举或协议。

编译后 IL 核验覆盖本程序集所有方法（包括 async 状态机）：**463 处** Flash/DisplayAmountChanged 调用，**454 处**解析到 `HextechRelicBase/HextechPowerBase`；其余 **9 处**为 5 个边界内部 base 调用及 4 个已经单点隔离的原版基类调用（AttackReplay、DragonSoul、BlackBloodPlus、IllusoryWeapon）。原始结果：`/tmp/hextech-flash-call-audit-final-20260921.txt`。这证明本次编译的调用解析，不证明原版程序集内部对非虚方法的调用也受保护；后者未修改。

## 待裁决

| ID | 类型/结论 | 文件:行 | 条件及具体分叉机制 | 方案/不直接改的原因 |
| --- | --- | --- | --- | --- |
| Q01 | R12/P1，确定存在未等待的共享命令链 | `src/Runes/NearDeathFeastRune.cs:141,180` | 负血债变化而实际 HP 仍为 1，或 PreserveNegativeHpAsDyingState 路径，直接 fire-and-forget `SyncNearDeathStrength()`；内部 PowerCmd 可 await，主客的本地任务恢复时序不受调用方结算链等待，力量应用可与下一命令交错 | 把待补力量记账并在两端同一 awaited Hook 排空。需要裁决获得力量的精确时机、跨保存恢复策略；本次不改同步状态/保存字段，也不改可感知触发时机 |
| Q02 | R8/P2，可疑 | `src/Hooks/Combat/HextechCombatHooks.SlipperyFix.cs:31` | 对 HashSet 中多个玩家 SlipperyPower 执行 Decrement。当前相同插入历史在同运行时通常同序，但集合没有稳定排序契约，换运行时/插入重放可能让两端移除 Power 的 Hook 次序不同 | 以 owner.CombatId/NetId 排序；排序会改变多人连锁效果原顺序，先定语义。未仅凭 HashSet 命中认定已经复现 |
| Q03 | R7/P2，可疑 | `src/Selection/Coordinator/HextechForgeSelectionCoordinator.cs:244` | 没有 HextechMayhemModifier 的联机局由外部 API 发锻造器时，最终回退本地 GetSnapshot().RandomForgeDirectGrant；两端配置不同会一端跳过 UI/ReserveChoiceId、另一端等待同步选择 | 有正常 MayhemModifier 时使用同步配置，安全；支持无 modifier 发放需定义主机配置来源或强制固定回退，不能擅自改变此 API 玩家行为 |
| Q04 | R10/P2，可疑 | `src/Helpers/HextechRuneGrantHelper.cs:64`、`src/Selection/Coordinator/HextechForgeSelectionCoordinator.cs:61` | 主流程各自按玩家预留 choiceId，再分本地/远端；若外部调用方并发启动独立发奖来源且两端启动序不一致，会把相同计数配给不同 operationToken | 现有幕选择 gate 不能证明任意外部调用都串行；给 API 明确对称且 awaited 的调用约定，或设计同玩家事务序列化。后者涉及交互顺序，未直接加锁/改协议 |
| Q05 | R2/P2，可疑 | `src/Selection/Coordinator/HextechRuneSelectionCoordinator.Core.cs:244` | 顶层捕获所有异常仅记录；发生在 SetStageResolved 后、ApplyToCurrentEnemies/Persist 前时，一端可能留下已解析但未完整施加状态，另一端完成。现注释关于“通常未解析、重入自愈”并不能覆盖提交后的异常 | 需逐阶段明确提交/补偿语义，不能给整段换成吞错或简单重试，否则重复发奖；保留现场异常证据，未改主流程 |

以上是全部本轮需要产品/同步时机裁决的条目。Q01 不能以 TaskHelper 的异常日志包装当作已经同步；Q02–Q05 仍需对应触发环境取证。没有用禁用内容、修改校验、全局 Hook 分发或断连兜底掩盖它们。

## 已复核安全项及各类型范本

| ID/类型 | 文件:行 | 裁决及正确写法 |
| --- | --- | --- |
| S01/R1 | `src/Hooks/UI/HextechEnemyUi.cs:34` | Refresh 只包 UI，异常不冒入共享调用；是现有表现边界范本 |
| S02/R1 | `src/Runes/StokeRune.cs:49`、`src/Hooks/Assets/HextechAssetHooks.cs:149` | 移牌经 CardSelectCmd/CardPileCmd；图标独立解析且补丁有异常边界，不依赖持有端才有的缓存别名。失败回退仍需实机资源测试 |
| S03/R2 | `src/Selection/Coordinator/HextechForgeSelectionCoordinator.cs:101` | 预留事务后的非取消异常提升为明确 protocol failure；不是只 catch OCE 后让对端永久等待 |
| S04/R2 | `src/Selection/Sync/HextechRuneSelectionCoordinator.RemoteChoices.cs:261` | 后台 observer 同时观察取消和普通异常；它只观察，不以异常路径再写共享状态 |
| S05/R3 | `src/Bootstrap/HextechSavedPropertyBootstrap.Legacy.cs:35` | 老版使用既有稳定注册列表、初始化冻结及位宽；当前保存载体检查/快照测试通过，未追加或重排 |
| S06/R3 | `src/Bootstrap/HextechSavedPropertyBootstrap.Official.cs:27` | 新版以官方缓存窗口为准，禁止初始化后注入；同名属性不能替代 per-type 注册。定向用例修复夹具后实际跑过 |
| S07/R4 | `src/Relics/Base/HextechRelicBase.CardGeneration.cs:9` | 普通战斗随机池先 FilterForCombat、再 modifier 许可与 Ordinal 排序。SingularityAI、BlankCheck、MindOverMatter、CorruptedBranch 等走此路径 |
| S08/R4 | `src/Runes/CreativeAiUpgradeRune.cs:23`、`src/Runes/JackpotUpgradeRune.cs:23` | 原版 GetDistinctForCombat/GetForCombat 内部过滤；共享 CombatCardGeneration RNG 在同一 awaited 卡/能力结算使用 |
| S09/R4 | `src/Cards/CardTransformUpgradeHelper.cs:116` | 变化采用原版专属稀有度/CanBeGeneratedInCombat/人数约束；明确保留状态/诅咒变化的特殊契约 |
| S10/R4 | `src/Compat/HextechGameApiCompat.cs:61`、`src/Relics/HextechAncientRelicHelper.cs:74` | 药水用原版 GetPotionOptions；蜡遗物仅普通奖励稀有度。Nonupeipe 固定远古清单是明确内容设计，不是普通奖励池漏过滤 |
| S11/R4 | `src/Forges/HextechForgeGrantHelper.cs:230`、`src/Helpers/HextechRuneGrantHelper.cs:135` | 符文/锻造器经可获取池、角色/开关约束及稳定ID；Minion 三种固定 token 是效果指定卡，不将指定生成误判为随机奖励池漏洞 |
| S12/R5 | `src/HextechStableRandom.cs:66` | Pick/PickDistinct 先以 Ordinal 键排序，再派生种子，不用 System.Random/GetHashCode；调用者的相同ID牌仍须来源为同序牌堆 |
| S13/R5 | `src/Selection/Reroll/HextechRuneSelectionCoordinator.Reroll.cs:86` | 联机重随走独立稳定派生分支，单机才用 Niche；确认后同步最终模型ID，不由本地UI推进共享 Niche |
| S14/R6 | `src/Runes/NatureIsHealingRune.cs:28`、`src/EnemyHexes/NatureIsHealingEnemyHex.cs:23` | 玩家/敌方 Timer 只在非联机创建；联机候选池排除，旧局退化为对称回合 Hook |
| S15/R6 | `src/Runes/MagicMissileRune.cs:63`、`src/Runes/LightEmUpRune.cs:85`、`src/Runes/TwinFlamesRune.cs:48` | 三种弹道的联机伤害走 awaited lockstep 分支，不依赖视觉抵达值；帧等待只在单机分支。参见 S27 |
| S16/R7 | `src/Runes/DoubleVisionRune.Scopes.cs:113` | 本地奖励复制后使用 RewardSynchronizer 广播；不能删 LocalContext 分支使两端重复复制。F09 修的是广播前 UI 异常 |
| S17/R7 | `src/Telemetry/HextechTelemetry.PayloadBuilder.cs:17`、`src/Services/HextechFeaturedConfigs.cs:54` | 遥测只读取种子；推荐配置 DateTime 用于 HTTP 缓存，不反向推进共享 RNG 或战斗状态 |
| S18/R8 | `src/Mayhem/HextechMayhemCombatTrackingSerializer.Values.cs:27` | 字典/集合保存复制通过 OrderedValues 稳定排序；恢复只是映射写入，不按迭代顺序发命令 |
| S19/R8 | `src/Runes/PacifistRune.cs:87` | HashSet 遍历仅删除各符文本地待处理记录，没有命令/抽选/目标顺序副作用；不能机械地给每个 HashSet 排序 |
| S20/R8 | `src/Hooks/Runes/HextechPlayerRuneHooks.DrawYourSword.cs:45` | 反射收集 Evoke 后按声明类型 FullName Ordinal 排序；目录枚举不作为玩法随机输入 |
| S21/R9 | `src/Mayhem/HextechMayhem.CardEvents.cs:44` | Storm 前置层数快照、后置 Remove，保留原版首次雷暴不自触发守卫 |
| S22/R9 | `src/Hooks/Combat/HextechCombatHooks.PowerCompat.cs:9` | Entropy 保留 owner/player 限制，经官方选牌命令获得一致选择，再确定性变化；没有本地身份分支 |
| S23/R9 | `src/Hooks/Combat/HextechCombatHooks.Outbreak.cs:99` | SleightOfFlesh 临时Power、施加者、正负状态条件与反编译一致；防递归范围通过原任务 finally 退出 |
| S24/R10 | `src/Selection/Sync/HextechRuneSelectionCoordinator.Multiplayer.cs:83`、`src/Selection/Sync/HextechChoiceCodec.cs:559` | 内建有序流程两端先 Reserve 再按local分支，payload含类型/operationToken/最终ID；不自行重置原版计数器。仅不涵盖 Q04 的外部并发 |
| S25/R11 | `src/Runes/CollectorRune.cs:67`、`src/Runes/FlyingKickRune.cs:103` | 两者在 Kill 前取得真死亡可计数结论，不再依赖 Kill 后 CombatState |
| S26/R11 | `src/EnemyHexes/ThievingHopperEnemyHex.cs:76` | await偷牌后复核活着/仍在战斗，逃跑用 CreatureCmd.Escape 而不是死亡，单独捕获本地逃跑表现 |
| S27/R12 | `src/Runes/MagicMissileRune.cs:65` | 联机 fire-and-forget 只承载VFX，伤害Task返回给原Hook等待；与 Q01 中fire-and-forget共享PowerCmd不同 |

R2 正确写法是保持共享任务被等待，取消按事务处理，表现错误只在表现边界隔离；不是对整个任务补一个 catch。R3 使用当前版本官方冻结窗口，不添加手工重排。R10 多个来源若要并发，先有明确事务语义，再谈编号；本轮不构建通用队列框架。

## 两类新增守卫与测试维护

仅新增两个测试注册：

1. `GameplayDeterminismApisRequireReviewedExceptions`：扫描源文件中的 System.Random/new Random/Random.Shared、Guid.NewGuid、DateTime.Now/UtcNow、Stopwatch、Time.GetTicks、Godot.Timer。扫描所有源码目录；6 个“文件+精确API”白名单逐项说明纯UI缓存/输入去重或仅单机Timer理由。白名单文件出现另一危险API仍失败，已消失的白名单也失败。它是已知API拼写的静态护栏，不是别名/反射/第三方调用链的完整语义分析器。
2. `RandomGenerationClassesMatchReviewedManifest`：冻结 `random_generation_audit.txt` 的 **49 个 文件+类** 条目，包含普通卡/遗物/药水工厂、稳定生成、变化和发放助手。为保守覆盖，命中入口文件中的内部补丁类也列入，不意味着内部补丁自身另有随机池。新增/移除未复核均失败；没有自动刷新开关。

已有全量首次失败揭露两处测试维护问题，均通过基线源码核对后局部修复，没有刷新补丁/SavedProperty清单：

- 三版 `static_state_manifest` 各删除已经不存在的 `HextechNearDeathFeastVisual._glowTexture/_ringTexture` 两行。`git show HEAD:HextechRunes/src/Hooks/UI/HextechNearDeathFeastVisualHooks.cs` 已没有它们；现实现用局部纹理及 `HextechTextures`。保留真正存在的 `HextechCombatVfx` 同名字段及所有其他条目。
- `Program.CaptureStaticCollectionRestore` 的 `.Cast<DictionaryEntry>()` 在新版实际枚举到 `KeyValuePair<Type,List<PropertyInfo>>`，测试在进入被测断言前就失败。改用 `IDictionary.GetEnumerator().Entry`，沿用已有 `CompatibilityRegistrationTests` 的写法；生产保存代码未动。

## 验证原文

已先用 `python3 tools/hextech_dev.py tests --list --match ...` 列出相关精确名字。0.111.0 最后一次定向命令包含两个新守卫、IllusoryWeapon、同名SavedProperty载体和静态字段清单：

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
PASS GameplayDeterminismApisRequireReviewedExceptions
PASS RandomGenerationClassesMatchReviewedManifest
PASS StaticStateManifestMatchesCheckedInList
PASS SavedPropertySameNameCarrierStillRequiresPerTypeCache
PASS IllusoryWeaponPenNibPrefixesCanReturnSkippedTask
5/5 tests passed
```

定向日志：`/tmp/hextech-determinism-targeted-final-20260921.log`。此前旧定向四项（新守卫、Storm、Entomancer）也为 `4/4 tests passed`，日志 `/tmp/hextech-determinism-targeted-20260921-rerun.log`。

最终完整命令为 `HEXTECH_DEPLOY=0 bash tools/run_tests.sh`，脚本根据当前维护范围**串行跑三个版本**，不是旧文档口语中的“双版本”。完整 stdout/stderr：`/tmp/hextech-determinism-verification-20260921.log`。关键原文：

```text
== Building tests against STS2 0.107.1 ==
Build succeeded.
    0 Warning(s)
    0 Error(s)
== Running tests against STS2 0.107.1 ==
322/322 tests passed
== Building tests against STS2 0.110.0 ==
Build succeeded.
    0 Warning(s)
    0 Error(s)
== Running tests against STS2 0.110.0 ==
322/322 tests passed
== Building tests against STS2 0.111.0 ==
Build succeeded.
    0 Warning(s)
    0 Error(s)
== Running tests against STS2 0.111.0 ==
322/322 tests passed
validated HextechRunes: loader + 3 variant(s), targets=0.107.1, 0.110.0, 0.111.0
```

该 bundle 检查读取既有 `dist`，**不表示本次源码已经重新打包或部署到 dist/游戏**。只构建测试所需本体/拓展包依赖及 loader。`git diff --check` 无输出。

保留初次失败原文，不把修复前结果抹掉：

```text
FAIL GameplayDeterminismApisRequireReviewedExceptions: unreviewed nondeterministic gameplay APIs: src/Hooks/UI/HextechRelicVisibilityHooks.ToggleUi.cs: Godot.Timer; src/Hooks/UI/HextechRelicVisibilityHooks.ToggleUi.cs: Godot.Timer
3/4 tests passed
FAIL StaticStateManifestMatchesCheckedInList: static state manifest drift; removed: [HextechRunes.HextechNearDeathFeastVisual._glowTexture : Texture2D; HextechRunes.HextechNearDeathFeastVisual._ringTexture : Texture2D] new mutable statics: []
321/322 tests passed
FAIL SavedPropertySameNameCarrierStillRequiresPerTypeCache: Unable to cast object of type 'System.Collections.Generic.KeyValuePair`2[System.Type,System.Collections.Generic.List`1[System.Reflection.PropertyInfo]]' to type 'System.Collections.DictionaryEntry'.
320/322 tests passed
```

分别见 `/tmp/hextech-determinism-targeted-20260921.log`、`/tmp/hextech-determinism-full-20260921.log`、`/tmp/hextech-determinism-full-0110-20260921.log`、`/tmp/hextech-determinism-full-0111-20260921.log`。初次新增守卫发现的是仅UI定位Timer，读上下文后加精确白名单；不是删除检查。

## mplab 与最小双客户端验证

**未做联机实测，需用户双客户端验证。** `tools/mplab/run_mplab.sh:40` 构建驱动，`:45` 起把 DLL/JSON 部署到真实游戏 mods，`:72` 主机使用真实 HOME；只有客户端在`:81`另设 HOME。它还会运行已安装的模组而不是自动验证本次源码。直接执行违反本轮禁止部署/用户数据边界，因此本次没有运行，**没有两端状态一致的实测证据**，也没有虚构 mplab 输出。

最小复现/观测点（两端先使用用户自行安装的同版构建；不在此任务部署）：

| 范围 | 符文/操作 | 两端应一致的观测点 |
| --- | --- | --- |
| F01–F03 | 持有“升级：狱火”受自损；带流电打能力牌；科学狂人引导球 | 敌人HP/灼烧、玩家HP、球槽数和后续出牌一致；在测试构建中让一端对应VFX失败仍不漏命令 |
| F04/F08 | 对带 Skittish 的敌人清增益；对存在旧 PainfulStabs 尸体残留的测试遭遇清理 | Power列表、战斗敌人数、战斗结束状态一致，失败端仅缺动画 |
| F05 | 仅0.107.1，昆虫法师无私人蜂巢时行动 | 两端均获得2力量；一端Cast表现失败不改变数值 |
| F06/F07/F10/F11 | 玩家/敌方坦克引擎体型变化，触发带计数符文及Power；测试端让闪光订阅者抛错 | 层数/计数、随后伤害和回合推进一致 |
| F09/F15 | 复制普通遗物；虚幻武器触发双截棍/苦无/手里剑/饰物扇 | 两端新增遗物一致；能量、敏捷、力量、格挡一致，即使本地表现资源故障 |
| F12–F14 | 使用重放能力；龙魂回合开始；二次强化黑血战斗胜利 | 重放能力移除、能量增加、治疗量一致 |
| F16–F20 | 敌方轮转不息洗牌后查看多张手牌；巨型小刀/复生/无本万利等费用变化 | 每张手牌费用、已用次数与下一张出牌结果一致；一端费用UI节点失效不停止余下牌更新 |
| Q01 | 濒死狂宴进入负血但实际HP仍为1，连续伤害/立即打攻击牌 | 对比每次伤害后的债务和力量，记录第一个状态分叉及命令序；尚未修复或实测 |

异常注入应仅在隔离测试环境进行，不修改玩家正常资源或存档。检查游戏原生 checksum/state dump 的首处分歧，不以握手成功、无日志或单机 headless 退出0替代结论。

## 附录：逐文件覆盖清单

下表由全量源文件清单与扫描定位生成，包含所有867个文件；“上下文”表示本轮读过对应风险上下文，“事件IL”表示该文件所属类有编译后表现事件绑定核验，“筛查”仅表示全文件候选扫描。组合标签不代表已逐行证明所有行为安全。R列是候选定位，不是问题裁决，数量不加入前述发现计数。

清单统计：867 文件全部静态筛查；其中 102 文件有风险上下文复核。事件IL标签按所属类绑定核验，partial 类涉及多个源文件时仅表示该类编译产物受核验，不等于每个分部都存在事件调用。

| 目录 | .cs 数 |
| --- | ---: |
| (src根) | 6 |
| Api | 5 |
| Assets | 3 |
| Bootstrap | 7 |
| Cards | 16 |
| Combat | 10 |
| Compat | 13 |
| Config | 4 |
| Content | 15 |
| Enchantments | 1 |
| EnemyHexes | 151 |
| Forges | 7 |
| Helpers | 11 |
| Hooks | 91 |
| Mayhem | 52 |
| Patching | 3 |
| Powers | 5 |
| Properties | 1 |
| Relics | 16 |
| Rewards | 5 |
| RunModifiers | 2 |
| Runes | 388 |
| Selection | 41 |
| Services | 5 |
| Telemetry | 6 |
| UI | 3 |

| 文件 | 证据层 | 候选类型及首行 |
| --- | --- | --- |
| `src/Api/HextechRuneGeneration.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Api/HextechRunesApi.cs` | 筛查 | R3:5；R4:117；R5:128 |
| `src/Api/IHextechGeneratedRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Api/IHextechHealingMultiplierProvider.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Api/RelicBundleGrantHelper.cs` | 筛查 | R3:18 |
| `src/Assets/AssetResourceResolver.cs` | 筛查 | R8:14 |
| `src/Assets/HextechAssets.cs` | 筛查 | R1:41 |
| `src/Assets/HextechTextures.cs` | 筛查 | R1:24；R8:12；R9:108 |
| `src/Bootstrap/HextechModelBootstrap.cs` | 上下文 | R3:18 |
| `src/Bootstrap/HextechModelPoolRegistrar.cs` | 筛查 | R9:90 |
| `src/Bootstrap/HextechModelTypeIdentity.cs` | 筛查 | R3:7；R8:7 |
| `src/Bootstrap/HextechSavedPropertyBootstrap.Legacy.cs` | 上下文 | R3:7 |
| `src/Bootstrap/HextechSavedPropertyBootstrap.Official.cs` | 上下文 | R3:6 |
| `src/Bootstrap/HextechSavedPropertyBootstrap.cs` | 上下文 | R3:6；R8:74 |
| `src/Bootstrap/HextechSavedPropertyNetIdCanonicalizer.cs` | 上下文 | R3:10 |
| `src/Cards/BladeWaltzCard.cs` | 筛查 | R11:31 |
| `src/Cards/CardTransformUpgradeHelper.cs` | 上下文 | R4:17；R5:2；R9:12；R11:17 |
| `src/Cards/FeelTheBurnCard.cs` | 筛查 | R11:21 |
| `src/Cards/HextechCardGeneration.cs` | 上下文 | 未命中所列模式（不是安全证明） |
| `src/Cards/HextechColorlessCardHelper.cs` | 上下文 | 未命中所列模式（不是安全证明） |
| `src/Cards/HextechCustomCards.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Cards/HextechDragonSoulCards.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Cards/HextechDragonSoulPowers.cs` | 上下文 / 事件IL | R1:16；R4:182；R5:191；R9:84；R11:114 |
| `src/Cards/HextechOwnerPoolTokenCard.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Cards/HextechRegentGeneratedCardHelper.cs` | 上下文 | 未命中所列模式（不是安全证明） |
| `src/Cards/MikaelsBlessingCard.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Cards/OkBoomerangCard.cs` | 筛查 | R1:33；R11:20 |
| `src/Cards/OstyWishCard.cs` | 筛查 | R11:69 |
| `src/Cards/ReprogramCard.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Cards/SearingAttackCard.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Cards/WhiteHoleCard.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Combat/HextechAutoPlayHelper.cs` | 上下文 | R8:9 |
| `src/Combat/HextechEnemyCoefficientHelper.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Combat/HextechForgeCoefficientHelper.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Combat/HextechLegacyEnemyMaxHpMigration.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Combat/HextechMaxHpScaling.cs` | 筛查 | R6:91 |
| `src/Combat/HextechMonsterInteractionPolicy.cs` | 上下文 | R1:137；R11:16 |
| `src/Combat/HextechPlayerBodyScaleHelper.cs` | 上下文 | R1:25；R7:25 |
| `src/Combat/HextechPlayerCoefficientHelper.cs` | 筛查 | R8:14 |
| `src/Combat/HextechSelectedDrawHelper.cs` | 筛查 | R11:23 |
| `src/Combat/HextechStarterUpgradeHelper.cs` | 筛查 / 事件IL | R1:49；R8:27；R11:21 |
| `src/Compat/CreatureCmdCompat.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Compat/HextechGameApiCompat.cs` | 上下文 | R4:11；R8:77 |
| `src/Compat/HextechIntegratedStrategyEventsCompat.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Compat/HextechModelBaseCompat.cs` | 上下文 / 事件IL | R1:8 |
| `src/Compat/HextechMultiplayerDiagnostics.cs` | 筛查 | R3:103；R9:65 |
| `src/Compat/HextechMultiplayerScalingCompat.cs` | 筛查 | R11:25 |
| `src/Compat/HextechOrbPassiveCompat.Legacy.cs` | 筛查 | R11:9 |
| `src/Compat/HextechOrbPassiveCompat.Official.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Compat/HextechPowerCmdCompat.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Compat/HextechRunesInterop.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Compat/HextechRuntimeRuneCompatibility.cs` | 筛查 | R8:5 |
| `src/Compat/HextechSts2ApiCompat.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Compat/HextechSts2Compat.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Config/HextechConfigShareCodec.cs` | 筛查 | R4:43；R8:137 |
| `src/Config/HextechPlayerRuneConfigIds.cs` | 筛查 | R8:5 |
| `src/Config/HextechRunConfigurationSnapshot.cs` | 筛查 | R4:15；R8:8 |
| `src/Config/HextechRuneConfiguration.cs` | 筛查 | R4:20；R6:622；R8:240；R9:414 |
| `src/Content/ForgeMetadataCatalog.cs` | 筛查 | R8:6；R9:40 |
| `src/Content/ForgeRegistration.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Content/HextechCatalog.Lookups.cs` | 筛查 | R8:92；R9:13 |
| `src/Content/HextechCatalog.Series.cs` | 筛查 | R8:8；R9:96 |
| `src/Content/HextechCatalog.cs` | 上下文 | R8:210；R9:85 |
| `src/Content/HextechContentRegistry.cs` | 筛查 | R8:57 |
| `src/Content/HextechCustomModelRegistry.cs` | 上下文 | R4:39 |
| `src/Content/HextechExternalContentRegistry.cs` | 筛查 | R1:10；R8:9；R9:179 |
| `src/Content/HextechForgeRegistry.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Content/HextechMonsterHexRegistry.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Content/HextechPlayerRuneRegistry.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Content/MonsterHexMetadataCatalog.cs` | 筛查 | R8:6 |
| `src/Content/MonsterHexRegistration.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Content/PlayerRuneMetadataCatalog.cs` | 筛查 | R8:6；R9:85 |
| `src/Content/PlayerRuneRegistration.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Enchantments/UniversalSpiral.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/AeonglassEnemyHex.cs` | 筛查 | R11:10 |
| `src/EnemyHexes/AncientStatueEnemyHex.cs` | 筛查 | R11:15 |
| `src/EnemyHexes/AncientWineEnemyHex.cs` | 筛查 | R11:13 |
| `src/EnemyHexes/ArcanePunchEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ArchmageEnemyHex.cs` | 筛查 | R5:32；R11:13 |
| `src/EnemyHexes/AstralBodyEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/AttributeBoostEnemyHex.cs` | 筛查 | R8:34 |
| `src/EnemyHexes/BackToBasicsEnemyHex.cs` | 筛查 | R8:24；R11:18 |
| `src/EnemyHexes/BigStrengthEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/BloodArmorEnemyHex.cs` | 筛查 | R6:7；R11:13 |
| `src/EnemyHexes/BloodIdolEnemyHex.cs` | 筛查 | R11:14 |
| `src/EnemyHexes/BloodPactEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/BlueCandleMedkitEnemyHex.cs` | 筛查 | R5:21；R11:11 |
| `src/EnemyHexes/BrutalForceEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/BrutalityEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ByrdonisEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/CantTouchThisEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/CerberusEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/CeremonialBeastEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ClownCollegeEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/CompensationEnemyHex.cs` | 筛查 | R9:145；R11:37 |
| `src/EnemyHexes/CorrosionEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/CorruptedBranchEnemyHex.cs` | 筛查 | R5:18；R11:11 |
| `src/EnemyHexes/CourageOfColossusEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/CuttingEdgeAlchemistEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/DawnbringersResolveEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/DeathHarvestEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/DevilsDanceEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/DivineInterventionEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/DizzySpinningEnemyHex.cs` | 筛查 | R5:25；R11:11 |
| `src/EnemyHexes/DoomsdayEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/DualWieldEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/DuffsVintageEnemyHex.cs` | 筛查 | R11:9 |
| `src/EnemyHexes/EightPennyGateEnemyHex.cs` | 筛查 | R9:36；R11:18 |
| `src/EnemyHexes/EndlessRotationEnemyHex.cs` | 上下文 | R11:10 |
| `src/EnemyHexes/EnemyHexConsoleCmd.cs` | 筛查 | R9:129 |
| `src/EnemyHexes/EnemyHexIconRelics.cs` | 筛查 | R1:6 |
| `src/EnemyHexes/EnlightenmentEnemyHex.cs` | 筛查 | R11:10 |
| `src/EnemyHexes/EscapePlanEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ExoskeletonEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/FeelTheBurnEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/FeyMagicEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/FinalFormEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/FirebrandEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/FirstAidKitEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ForbiddenGrimoireEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ForgottenSoulEnemyHex.cs` | 筛查 | R11:16 |
| `src/EnemyHexes/FossilStalkerEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/FrostWraithEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/GetExcitedEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/GiantSlayerEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/GlassCannonEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/GlobeHeadEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/GoldenSpatulaEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/GoldrendEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/GoliathEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/HailToTheKingEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/HandOfBaronEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/HastyScribbleEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/HauntedShipEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/HeavyHitterEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/HextechEnemyDrawProgress.cs` | 筛查 | R8:5 |
| `src/EnemyHexes/HextechEnemyHexContext.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/HextechEnemyHexDispatcher.cs` | 筛查 | R9:51 |
| `src/EnemyHexes/HextechEnemyHexEffect.cs` | 筛查 | R6:205；R9:252 |
| `src/EnemyHexes/HextechEnemyHexEffects.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/HextechEnemyHexGlobalUsings.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/HextechEnemyNearDeath.cs` | 筛查 | R6:223；R9:30；R11:96 |
| `src/EnemyHexes/HextechEnemyStatusCards.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/HundredRefinementsEnemyHex.cs` | 筛查 | R11:16 |
| `src/EnemyHexes/IGripEnemyHex.cs` | 筛查 | R11:17 |
| `src/EnemyHexes/IInspectEnemyHex.cs` | 筛查 | R9:28；R11:11 |
| `src/EnemyHexes/InfestedPrismHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/InkletEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/JeweledGauntletEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/JinlianBoxEnemyHex.cs` | 上下文 | R4:77；R8:26；R9:23 |
| `src/EnemyHexes/JudicatorEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/LagavulinMatriarchEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/LeafSlimeEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/LightEmUpEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/LivingFogEnemyHex.cs` | 筛查 | R11:13 |
| `src/EnemyHexes/LoopEnemyHex.cs` | 筛查 | R11:10 |
| `src/EnemyHexes/MadScientistEnemyHex.cs` | 筛查 | R11:27 |
| `src/EnemyHexes/ManipulateRealityEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/MasterOfDualityEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/MikaelsBlessingEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/MindOverMatterEnemyHex.cs` | 筛查 | R11:12 |
| `src/EnemyHexes/MirrorReflectionEnemyHex.cs` | 筛查 | R11:13 |
| `src/EnemyHexes/MiserableFateEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/MonarchsGazeEnemyHex.cs` | 筛查 | R11:13 |
| `src/EnemyHexes/MonsterHexCatalog.cs` | 筛查 | R1:240；R8:14 |
| `src/EnemyHexes/MoreTheMerrierEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/MountainSoulEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/MysteryEnemyHex.cs` | 上下文 | R5:20；R11:12 |
| `src/EnemyHexes/MyteEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/NatureIsHealingEnemyHex.cs` | 上下文 | R2:109；R6:10；R9:155；R11:153；R12:109 |
| `src/EnemyHexes/NearDeathFeastEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/NightstalkingEnemyHex.cs` | 筛查 | R11:11 |
| `src/EnemyHexes/OmegaEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/OminousPactEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/OmniDragonSoulEnemyHex.cs` | 筛查 | R5:19 |
| `src/EnemyHexes/PandorasBoxEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/PhantasmalGardenerEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/PhrogParasiteEnemyHex.cs` | 筛查 | R11:13 |
| `src/EnemyHexes/PorcupineEnemyHex.cs` | 筛查 | R11:36 |
| `src/EnemyHexes/ProtectiveVeilEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ProteinShakeEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/QueenEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ReforgedHelmetEnemyHex.cs` | 筛查 | R11:10 |
| `src/EnemyHexes/RepulsorEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/SerpentsFangEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ServantMasterEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ShoulderVakuEnemyHex.cs` | 筛查 | R11:15 |
| `src/EnemyHexes/ShrinkEngineEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ShrinkRayEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ShrinkerBeetleEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/SingularityAIEnemyHex.cs` | 筛查 | R5:22 |
| `src/EnemyHexes/SkulkingColonyEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/SlapEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/SlimedBerserkerEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/SolidTimeEnemyHex.cs` | 筛查 | R11:11 |
| `src/EnemyHexes/SomethingForNothingEnemyHex.cs` | 筛查 | R11:12 |
| `src/EnemyHexes/SonataEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/SoulEaterEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/SoulFyshEnemyHex.cs` | 筛查 | R5:20；R11:10 |
| `src/EnemyHexes/SpeedDemonEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/StartupRoutineEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/StatsEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/StatsOnStatsEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/StatsOnStatsOnStatsEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/SturdyEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/SuperBrainEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/SwiftAndSafeEnemyHex.cs` | 筛查 | R11:10 |
| `src/EnemyHexes/TankEngineEnemyHex.cs` | 筛查 | R8:63 |
| `src/EnemyHexes/TanksShieldEnemyHex.cs` | 筛查 | R11:13 |
| `src/EnemyHexes/TestSubjectEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/TezcatarasMercyEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/TheForgottenEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/TheLostEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/ThievingHopperEnemyHex.cs` | 上下文 | R1:98；R5:37；R7:98；R11:21 |
| `src/EnemyHexes/ThornmailEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/TormentorEnemyHex.cs` | 筛查 | R11:18 |
| `src/EnemyHexes/TungstenRodEnemyHex.cs` | 筛查 | R11:16 |
| `src/EnemyHexes/TwiceThriceEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/TwilightVeilEnemyHex.cs` | 筛查 | R11:10 |
| `src/EnemyHexes/UnmovableMountainEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/UpgradeEnemyHex.cs` | 筛查 | R11:13 |
| `src/EnemyHexes/VantomEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/VitalitySurgeEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/EnemyHexes/WarmogsSpiritEnemyHex.cs` | 筛查 | R11:10 |
| `src/EnemyHexes/ZealotEnemyHex.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Forges/HextechEnchantForges.cs` | 筛查 | R1:39 |
| `src/Forges/HextechForgeBase.cs` | 筛查 / 事件IL | R1:18；R3:6 |
| `src/Forges/HextechForgeGrantHelper.cs` | 上下文 | R3:15；R4:11；R5:1；R9:31 |
| `src/Forges/HextechForgeShopPriceHelper.cs` | 筛查 | R4:5；R9:46 |
| `src/Forges/HextechForges.Gold.cs` | 筛查 / 事件IL | R1:18；R3:85；R5:260；R11:40 |
| `src/Forges/HextechForges.Prismatic.cs` | 筛查 / 事件IL | R1:36；R3:7 |
| `src/Forges/HextechForges.Silver.cs` | 筛查 / 事件IL | R1:17；R3:165；R5:87；R11:293 |
| `src/Helpers/EventRewardTransaction.cs` | 筛查 | R9:25 |
| `src/Helpers/HextechCardPlayTiming.cs` | 上下文 | R6:29；R9:21；R11:19 |
| `src/Helpers/HextechCombatHistoryHelper.cs` | 筛查 | R9:47 |
| `src/Helpers/HextechDataPaths.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Helpers/HextechGodotAsync.cs` | 上下文 | R6:7；R9:11；R11:7 |
| `src/Helpers/HextechHookReflection.cs` | 筛查 | R8:6 |
| `src/Helpers/HextechKnifeHelper.cs` | 上下文 | R9:12；R11:62 |
| `src/Helpers/HextechMapLengthReducer.cs` | 筛查 | R8:156；R9:76 |
| `src/Helpers/HextechPlayerContextHelper.cs` | 上下文 | R9:15 |
| `src/Helpers/HextechRuneGrantHelper.cs` | 上下文 | R4:24；R5:151；R7:73；R8:11；R9:61；R10:66 |
| `src/Helpers/HextechScopedDepthGuard.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/HextechGlobalUsings.cs` | 筛查 | R3:14 |
| `src/HextechLog.cs` | 筛查 | R9:39 |
| `src/HextechStableRandom.cs` | 上下文 | R4:105；R5:5；R9:24 |
| `src/HextechTypes.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Hooks/Assets/HextechAssetHooks.cs` | 上下文 | R1:15；R9:153 |
| `src/Hooks/Combat/HextechArtifactCompatibilityHooks.cs` | 筛查 | R9:19 |
| `src/Hooks/Combat/HextechCombatHooks.AttackCommand.cs` | 筛查 / 事件IL | 未命中所列模式（不是安全证明） |
| `src/Hooks/Combat/HextechCombatHooks.Draw.cs` | 筛查 / 事件IL | R1:29；R9:10；R11:27 |
| `src/Hooks/Combat/HextechCombatHooks.DualWield.cs` | 筛查 / 事件IL | R9:51；R11:56 |
| `src/Hooks/Combat/HextechCombatHooks.DualWieldIntent.cs` | 筛查 / 事件IL | R9:50；R11:54 |
| `src/Hooks/Combat/HextechCombatHooks.Healing.cs` | 筛查 / 事件IL | R9:72；R11:40 |
| `src/Hooks/Combat/HextechCombatHooks.JeweledGauntlet.cs` | 筛查 / 事件IL | R5:156；R9:98；R11:60 |
| `src/Hooks/Combat/HextechCombatHooks.MaxHp.cs` | 筛查 / 事件IL | R6:88；R9:72 |
| `src/Hooks/Combat/HextechCombatHooks.MonsterUpgrades.cs` | 筛查 / 事件IL | R9:66；R11:12 |
| `src/Hooks/Combat/HextechCombatHooks.NearDeathFeast.cs` | 筛查 / 事件IL | R9:31；R11:146 |
| `src/Hooks/Combat/HextechCombatHooks.Outbreak.cs` | 上下文 / 事件IL | R9:50 |
| `src/Hooks/Combat/HextechCombatHooks.Pacifist.cs` | 筛查 / 事件IL | R9:65 |
| `src/Hooks/Combat/HextechCombatHooks.PiercingThread.cs` | 筛查 / 事件IL | R9:10 |
| `src/Hooks/Combat/HextechCombatHooks.PlayCost.cs` | 筛查 / 事件IL | R1:113；R8:12；R9:36；R11:71 |
| `src/Hooks/Combat/HextechCombatHooks.PowerCompat.cs` | 上下文 / 事件IL | R5:36；R9:63；R11:37 |
| `src/Hooks/Combat/HextechCombatHooks.ShrinkPower.cs` | 筛查 / 事件IL | R9:43 |
| `src/Hooks/Combat/HextechCombatHooks.SlipperyFix.cs` | 上下文 / 事件IL | R8:9 |
| `src/Hooks/Combat/HextechCombatHooks.SlowPower.cs` | 筛查 / 事件IL | R9:16 |
| `src/Hooks/Combat/HextechCombatHooks.State.cs` | 筛查 / 事件IL | 未命中所列模式（不是安全证明） |
| `src/Hooks/Combat/HextechCombatVfx.Primitives.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Hooks/Combat/HextechCombatVfx.Sequences.cs` | 筛查 | R6:138；R11:138 |
| `src/Hooks/Combat/HextechCombatVfx.cs` | 筛查 | R1:84；R5:137；R7:84；R8:66；R11:39 |
| `src/Hooks/Combat/HextechEncounterCompatibilityHooks.cs` | 上下文 | R1:35；R9:55 |
| `src/Hooks/Combat/HextechEndlessModeCompatibilityHooks.cs` | 筛查 | R9:85；R11:58 |
| `src/Hooks/Combat/HextechEnemyPowerScalingHooks.Rules.cs` | 筛查 | R11:26 |
| `src/Hooks/Combat/HextechEnemyPowerScalingHooks.Targets.cs` | 筛查 | R9:78 |
| `src/Hooks/Combat/HextechEnemyPowerScalingHooks.cs` | 筛查 | R9:75 |
| `src/Hooks/Combat/HextechFormAutoPlayHooks.cs` | 筛查 | R8:121；R9:98；R11:101 |
| `src/Hooks/Combat/HextechFormVfxSafetyHooks.cs` | 筛查 | R1:68；R7:68；R9:90 |
| `src/Hooks/Combat/HextechMyriadSwordsVfx.cs` | 筛查 | R1:40；R7:40 |
| `src/Hooks/Combat/HextechPersonalHiveSafetyHooks.cs` | 筛查 | R9:39 |
| `src/Hooks/Combat/HextechSovereignBladeVfxSync.cs` | 筛查 | R1:45；R7:45；R8:55；R11:40 |
| `src/Hooks/Combat/HextechVitalSparkCompatibilityHooks.cs` | 筛查 | R11:12 |
| `src/Hooks/Compat/HextechGameOverCompatibilityHooks.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Hooks/Compat/HextechMobileModelRegistrationHooks.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Hooks/Compat/HextechRewardSafetyHooks.cs` | 筛查 | R4:214；R9:219 |
| `src/Hooks/Compat/HextechSavedPropertyNetIdHooks.cs` | 筛查 | R3:13；R8:50 |
| `src/Hooks/EnemyHexes/HextechEnemyCuttingEdgeAlchemistHooks.cs` | 筛查 | R5:3；R9:32 |
| `src/Hooks/EnemyHexes/HextechEnemyTezcatarasMercyHooks.cs` | 筛查 | R9:14 |
| `src/Hooks/RunLifecycle/HextechNaturalRelicPoolHooks.cs` | 筛查 | R5:10 |
| `src/Hooks/RunLifecycle/HextechRunLifecycleHooks.Core.cs` | 筛查 | R6:102；R8:16；R11:102 |
| `src/Hooks/RunLifecycle/HextechRunLifecycleHooks.Endless.cs` | 筛查 | R2:16；R12:16 |
| `src/Hooks/RunLifecycle/HextechRunLifecycleHooks.EventSelection.cs` | 筛查 | R9:86 |
| `src/Hooks/RunLifecycle/HextechRunLifecycleHooks.LoadedUi.cs` | 筛查 | R2:22；R3:239；R9:56；R12:22 |
| `src/Hooks/RunLifecycle/HextechRunLifecycleHooks.RoomEvents.cs` | 筛查 | R2:98；R9:155；R12:98 |
| `src/Hooks/RunLifecycle/HextechRunLifecycleHooks.RunEnd.cs` | 筛查 | R9:68 |
| `src/Hooks/RunLifecycle/HextechRunLifecycleHooks.StartRun.cs` | 筛查 | R3:114；R5:106；R9:94 |
| `src/Hooks/Runes/HextechInkshadowHooks.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Hooks/Runes/HextechNeurosurgeHooks.cs` | 筛查 | R11:18 |
| `src/Hooks/Runes/HextechNightmareHooks.cs` | 筛查 | R11:31 |
| `src/Hooks/Runes/HextechPlayerRuneHooks.DrawYourSword.cs` | 上下文 / 事件IL | R8:15；R9:64 |
| `src/Hooks/Runes/HextechPlayerRuneHooks.IllusoryWeapon.cs` | 上下文 / 事件IL | R1:185；R2:86；R11:70；R12:86 |
| `src/Hooks/Runes/HextechPlayerRuneHooks.Orbs.cs` | 筛查 / 事件IL | R1:187；R6:107；R9:41；R11:107 |
| `src/Hooks/Runes/HextechPlayerRuneHooks.cs` | 筛查 / 事件IL | R1:23；R9:94；R11:17 |
| `src/Hooks/Runes/HextechSelfUpgradeCardStore.cs` | 筛查 | R3:16 |
| `src/Hooks/Runes/HextechStarterUpgradeHooks.cs` | 筛查 | R9:87 |
| `src/Hooks/Runes/HextechThoughtOverwriteKeywordPersistenceHooks.cs` | 筛查 | R3:272；R9:397；R11:307 |
| `src/Hooks/Runes/HextechTreasureRuneHooks.cs` | 筛查 | R5:45；R8:55 |
| `src/Hooks/Shop/HextechForgeStackingHooks.cs` | 筛查 | R9:33 |
| `src/Hooks/Shop/HextechShopForgeHooks.cs` | 筛查 | R4:13；R5:39；R8:46；R9:273 |
| `src/Hooks/UI/HextechAnimTriggerSafetyHooks.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Hooks/UI/HextechBaronAuraHooks.cs` | 筛查 | R2:64；R6:154；R8:21；R9:83；R11:154；R12:64 |
| `src/Hooks/UI/HextechBehindCreaturesLayer.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Hooks/UI/HextechBurnHealthBarHooks.cs` | 筛查 | R9:79 |
| `src/Hooks/UI/HextechBurnVisualHooks.cs` | 筛查 | R1:512；R2:68；R5:18；R6:146；R8:17；R9:81；R11:146；R12:68 |
| `src/Hooks/UI/HextechCardGridPreviewHooks.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Hooks/UI/HextechCollectionHooks.FlatFallback.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Hooks/UI/HextechCollectionHooks.Reflection.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Hooks/UI/HextechCollectionHooks.Subcategories.cs` | 筛查 | R8:12 |
| `src/Hooks/UI/HextechCollectionHooks.cs` | 筛查 | R8:42；R9:124 |
| `src/Hooks/UI/HextechCreatureVisualHost.cs` | 筛查 | R1:33；R7:33 |
| `src/Hooks/UI/HextechEnemyUi.cs` | 上下文 | R9:198 |
| `src/Hooks/UI/HextechGlassCannonHealthBarHooks.cs` | 筛查 | R2:43；R6:89；R8:14；R9:113；R11:89；R12:43 |
| `src/Hooks/UI/HextechInspectHooks.cs` | 筛查 | R8:94；R9:52 |
| `src/Hooks/UI/HextechMikaelsBlessingVfx.cs` | 筛查 | R1:27；R2:44；R6:124；R7:27；R8:16；R9:78；R11:124；R12:44 |
| `src/Hooks/UI/HextechNearDeathFeastVisualHooks.cs` | 上下文 | R2:57；R6:148；R8:14；R9:79；R11:148；R12:57 |
| `src/Hooks/UI/HextechPlayerStatsHoverHooks.cs` | 筛查 | R9:100 |
| `src/Hooks/UI/HextechRelicVisibilityHooks.Config.cs` | 筛查 | R9:144 |
| `src/Hooks/UI/HextechRelicVisibilityHooks.ToggleUi.cs` | 上下文 | R1:111；R6:157 |
| `src/Hooks/UI/HextechRelicVisibilityHooks.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Hooks/UI/HextechRuneConfigMenuHooks.BottomBar.cs` | 筛查 | R4:36；R8:21 |
| `src/Hooks/UI/HextechRuneConfigMenuHooks.Community.cs` | 筛查 | R2:153；R8:21；R12:153 |
| `src/Hooks/UI/HextechRuneConfigMenuHooks.Entries.cs` | 筛查 | R8:180 |
| `src/Hooks/UI/HextechRuneConfigMenuHooks.Overlay.cs` | 筛查 | R2:35；R4:180；R6:41；R8:171；R11:41；R12:35 |
| `src/Hooks/UI/HextechRuneConfigMenuHooks.Pages.cs` | 筛查 | R4:347；R6:771；R8:197 |
| `src/Hooks/UI/HextechRuneConfigMenuHooks.RuneGrid.cs` | 筛查 | R2:82；R6:239；R8:180；R11:239；R12:82 |
| `src/Hooks/UI/HextechRuneConfigMenuHooks.Types.cs` | 筛查 | R8:32 |
| `src/Hooks/UI/HextechRuneConfigMenuHooks.cs` | 筛查 | R9:118 |
| `src/Hooks/UI/HextechSlowCookAuraHooks.cs` | 筛查 | R2:123；R6:214；R8:29；R9:142；R11:214；R12:123 |
| `src/Hooks/UI/HextechUiSafetyHooks.cs` | 筛查 | R9:71 |
| `src/Mayhem/CombatTrackingClearPhase.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/CombatTrackingTransientAttribute.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechActiveMonsterHexCache.cs` | 筛查 | R8:6 |
| `src/Mayhem/HextechAttackCostPreviewRefresher.cs` | 上下文 | R11:19 |
| `src/Mayhem/HextechCombatCreatureHelper.cs` | 上下文 | R1:83；R7:83；R11:39 |
| `src/Mayhem/HextechCombatProcTracker.cs` | 筛查 | R8:5；R9:9 |
| `src/Mayhem/HextechEnemyHealModifier.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechEnemyHexCountState.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechEnemyPowerTriggerHelper.cs` | 筛查 | R9:13 |
| `src/Mayhem/HextechEnemyTriggerGuard.cs` | 筛查 | R5:49；R9:46 |
| `src/Mayhem/HextechMayhem.ActSelection.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhem.ActStateFacade.cs` | 筛查 | R9:214 |
| `src/Mayhem/HextechMayhem.BlockEvents.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhem.BossPhases.cs` | 筛查 | R9:62；R11:14 |
| `src/Mayhem/HextechMayhem.CardEvents.cs` | 上下文 | R8:47；R11:15 |
| `src/Mayhem/HextechMayhem.ChoiceHistoryFacade.cs` | 筛查 | R8:15；R9:113 |
| `src/Mayhem/HextechMayhem.CombatLifecycle.cs` | 筛查 | R11:96 |
| `src/Mayhem/HextechMayhem.CombatModifiers.cs` | 筛查 | R9:94；R11:7 |
| `src/Mayhem/HextechMayhem.CombatStart.cs` | 筛查 | R11:13 |
| `src/Mayhem/HextechMayhem.CombatTracking.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhem.Constants.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhem.DamageEvents.cs` | 筛查 | R6:53；R9:65；R11:7 |
| `src/Mayhem/HextechMayhem.DeathEvents.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhem.Effects.cs` | 上下文 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhem.EnemyHealingBlock.cs` | 筛查 | R9:9 |
| `src/Mayhem/HextechMayhem.Map.cs` | 筛查 | R5:33 |
| `src/Mayhem/HextechMayhem.PersistentHexes.cs` | 上下文 | R1:279；R6:149；R7:279；R9:250；R11:248 |
| `src/Mayhem/HextechMayhem.PlayerRuneConfig.cs` | 筛查 | R8:34 |
| `src/Mayhem/HextechMayhem.PowerEvents.cs` | 筛查 | R11:19 |
| `src/Mayhem/HextechMayhem.Rewards.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhem.RunConfiguration.cs` | 筛查 | R3:113；R4:24 |
| `src/Mayhem/HextechMayhem.RuneSelectionJournal.cs` | 筛查 | R9:50 |
| `src/Mayhem/HextechMayhem.SavedState.cs` | 筛查 | R1:192；R3:38 |
| `src/Mayhem/HextechMayhem.TurnEnd.cs` | 筛查 | R11:18 |
| `src/Mayhem/HextechMayhem.TurnStart.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhem.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhemActRecovery.cs` | 筛查 | R8:112；R9:217 |
| `src/Mayhem/HextechMayhemActState.cs` | 筛查 | R6:313；R8:11；R9:123 |
| `src/Mayhem/HextechMayhemChoiceHistoryState.cs` | 筛查 | R8:58 |
| `src/Mayhem/HextechMayhemCombatTrackingSerializer.Bindings.cs` | 筛查 | R8:9 |
| `src/Mayhem/HextechMayhemCombatTrackingSerializer.Clearing.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhemCombatTrackingSerializer.Values.cs` | 上下文 | R8:31；R9:140 |
| `src/Mayhem/HextechMayhemCombatTrackingSerializer.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhemCombatTrackingSnapshot.cs` | 筛查 | R8:5 |
| `src/Mayhem/HextechMayhemCombatTrackingState.Serialization.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechMayhemCombatTrackingState.cs` | 筛查 | R8:6 |
| `src/Mayhem/HextechMayhemRunContext.cs` | 筛查 | R3:22 |
| `src/Mayhem/HextechMonsterSustainHelper.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechPlayerHexCountState.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Mayhem/HextechPlayerRuneConfigSnapshotState.cs` | 筛查 | R8:7；R9:67 |
| `src/Mayhem/HextechRuneSelectionJournalState.cs` | 筛查 | R8:16；R9:55 |
| `src/Mayhem/HextechServantMasterIllusionService.cs` | 筛查 | R11:15 |
| `src/ModEntry.cs` | 筛查 | R3:29 |
| `src/ModInfo.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Patching/HextechPatchAttribute.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Patching/HextechPatcher.cs` | 上下文 | 未命中所列模式（不是安全证明） |
| `src/Patching/HextechVanillaCopyGuard.cs` | 筛查 | R8:45 |
| `src/Powers/HextechGalvanicPower.cs` | 上下文 | R1:53 |
| `src/Powers/HextechHangPower.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Powers/HextechNextTurnDamagePower.cs` | 筛查 / 事件IL | R1:26；R11:19 |
| `src/Powers/HextechPowers.cs` | 上下文 / 事件IL | R1:47；R11:261 |
| `src/Powers/HextechVitalSparkPower.cs` | 筛查 | R11:52 |
| `src/Properties/AssemblyInfo.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Relics/Base/AttributeConversionRelicBase.cs` | 筛查 / 事件IL | R1:59；R9:32 |
| `src/Relics/Base/HextechRelicBase.CardGeneration.cs` | 上下文 / 事件IL | R4:13；R5:21；R11:62 |
| `src/Relics/Base/HextechRelicBase.CombatHelpers.cs` | 筛查 / 事件IL | R9:85 |
| `src/Relics/Base/HextechRelicBase.PlayerContext.cs` | 筛查 / 事件IL | 未命中所列模式（不是安全证明） |
| `src/Relics/Base/HextechRelicBase.TurnProc.cs` | 上下文 / 事件IL | R1:10；R9:105；R11:51 |
| `src/Relics/Base/HextechRelicBase.cs` | 上下文 / 事件IL | R1:9；R8:136 |
| `src/Relics/Base/LimitedDebuffProcRelicBase.cs` | 筛查 / 事件IL | R1:69；R3:9 |
| `src/Relics/HextechAncientRelicHelper.cs` | 上下文 | R4:45；R5:33 |
| `src/Relics/Orobas/ArchaicToothEternalHooks.cs` | 筛查 | R9:33 |
| `src/Relics/Orobas/HextechBlackBloodPlus.cs` | 上下文 / 事件IL | R1:19 |
| `src/Relics/Orobas/HextechDivineDestinyPlus.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Relics/Orobas/HextechInfusedCorePlus.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Relics/Orobas/HextechPhylacteryUnboundPlus.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Relics/Orobas/HextechRingOfTheDrakePlus.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Relics/Orobas/OrobasPlusRelicBase.cs` | 上下文 | 未命中所列模式（不是安全证明） |
| `src/Relics/Orobas/OrobasPlusUpgrades.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Rewards/ColorDiscoveryCardReward.cs` | 筛查 | R9:67 |
| `src/Rewards/HextechDynamicDropChance.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Rewards/HextechForgeChoiceReward.cs` | 筛查 | R1:25；R4:60；R9:42 |
| `src/Rewards/HextechGoldRewardHelper.cs` | 筛查 | R5:34 |
| `src/Rewards/HextechWaxRelicReward.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/RunModifiers/HextechCustomRunModifiers.cs` | 筛查 | R1:11；R9:62 |
| `src/RunModifiers/HextechPresetChallenges.cs` | 筛查 | R1:15；R9:111 |
| `src/Runes/AdamantRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/AdaptiveCapacitorRune.cs` | 筛查 / 事件IL | R1:22 |
| `src/Runes/AdvanceToRetreatRune.cs` | 筛查 / 事件IL | R1:32 |
| `src/Runes/AncientWineRune.cs` | 筛查 / 事件IL | R1:18 |
| `src/Runes/AnthonyBiasRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/ArcanePunchRune.cs` | 筛查 / 事件IL | R1:14；R3:7 |
| `src/Runes/ArchmageRune.cs` | 筛查 / 事件IL | R1:24；R5:37；R9:33；R11:42 |
| `src/Runes/AstralBodyRune.cs` | 筛查 | R3:9 |
| `src/Runes/AttackDefenseUnityRune.cs` | 筛查 / 事件IL | R1:36 |
| `src/Runes/AutoPatrolRune.cs` | 筛查 / 事件IL | R1:41；R11:28 |
| `src/Runes/AutoPlayFormsAtCombatStartRuneBase.cs` | 筛查 | R1:58 |
| `src/Runes/AutomationUpgradeRune.cs` | 筛查 / 事件IL | R1:21；R9:86；R11:72 |
| `src/Runes/BackToBasicsRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/BadTasteRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/BadgeBrothersRune.cs` | 筛查 / 事件IL | R1:24 |
| `src/Runes/BarbarianWayRune.cs` | 筛查 / 事件IL | R1:54 |
| `src/Runes/BashUpgradeRune.cs` | 筛查 / 事件IL | R1:38 |
| `src/Runes/BattleTranceUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/BeginningAndEndRune.cs` | 筛查 / 事件IL | R1:30 |
| `src/Runes/BerserkRune.cs` | 筛查 / 事件IL | R1:28 |
| `src/Runes/BigHammerRune.cs` | 筛查 / 事件IL | R1:51；R9:33 |
| `src/Runes/BigHandsRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/BigKnifeRune.cs` | 筛查 | R9:25；R11:119 |
| `src/Runes/BigStrengthRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/BlackCandleRune.cs` | 筛查 / 事件IL | R1:22 |
| `src/Runes/BladeWaltzRune.cs` | 筛查 / 事件IL | R1:19 |
| `src/Runes/BlankCheckRune.cs` | 上下文 / 事件IL | R1:49；R4:36；R5:40；R9:58；R11:23 |
| `src/Runes/BloodArmorRune.cs` | 筛查 / 事件IL | R1:37；R6:20 |
| `src/Runes/BloodDebtRune.cs` | 筛查 / 事件IL | R1:32；R6:23；R8:5 |
| `src/Runes/BloodIdolRune.cs` | 筛查 / 事件IL | R1:17 |
| `src/Runes/BloodPactRune.cs` | 筛查 / 事件IL | R1:38；R3:6 |
| `src/Runes/BloodlettingUpgradeRune.cs` | 筛查 / 事件IL | R1:20 |
| `src/Runes/BlueCandleMedkitRune.cs` | 筛查 | R9:10 |
| `src/Runes/BodySlamUpgradeRune.cs` | 筛查 / 事件IL | R1:24；R9:37 |
| `src/Runes/BodyguardUpgradeRune.cs` | 筛查 / 事件IL | R1:28 |
| `src/Runes/BoneBreakUpgradeRune.cs` | 筛查 / 事件IL | R1:50；R11:40 |
| `src/Runes/BoneGuardRune.cs` | 筛查 / 事件IL | R1:28 |
| `src/Runes/BorrowedTimeUpgradeRune.cs` | 筛查 / 事件IL | R1:37 |
| `src/Runes/BrandUpgradeRune.cs` | 筛查 / 事件IL | R1:16；R3:9 |
| `src/Runes/BreadAndButterRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/BreadAndCheeseRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/BreadAndJamRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/BreadSandwichAssemblyHelper.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/BreadSandwichRune.cs` | 筛查 / 事件IL | R1:19 |
| `src/Runes/BrokenGoldenCrownRune.cs` | 筛查 / 事件IL | R1:29；R9:20 |
| `src/Runes/BrutalForceRune.cs` | 筛查 / 事件IL | R1:30；R11:18 |
| `src/Runes/BrutalityRune.cs` | 筛查 / 事件IL | R1:23 |
| `src/Runes/BulletTimeUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/BurningInterestRune.cs` | 筛查 / 事件IL | R1:21；R3:14 |
| `src/Runes/ByproductRune.cs` | 筛查 / 事件IL | R1:23 |
| `src/Runes/CantTouchThisRune.cs` | 筛查 / 事件IL | R1:23 |
| `src/Runes/CardInspectionRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/CardUpgradeRuneBase.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/CarefulSelectionRune.cs` | 上下文 / 事件IL | R1:52；R4:43；R8:61；R9:16 |
| `src/Runes/CatalystRune.cs` | 筛查 / 事件IL | R1:24 |
| `src/Runes/CerberusRune.cs` | 筛查 / 事件IL | R1:18；R3:7；R9:59 |
| `src/Runes/ChainInSleeveRune.cs` | 筛查 / 事件IL | R1:16；R3:9 |
| `src/Runes/ChargeUpRune.cs` | 筛查 / 事件IL | R1:20 |
| `src/Runes/CircleOfDeathRune.cs` | 筛查 / 事件IL | R1:41；R5:33；R11:11 |
| `src/Runes/ClawUpgradeRune.cs` | 筛查 / 事件IL | R1:11 |
| `src/Runes/ClownCollegeRune.cs` | 筛查 / 事件IL | R1:19 |
| `src/Runes/CollectorRune.cs` | 上下文 / 事件IL | R1:23；R3:16；R8:11；R11:73 |
| `src/Runes/ColorDiscoveryRune.cs` | 上下文 / 事件IL | R1:65；R3:11；R4:93；R5:98 |
| `src/Runes/CompactUpgradeRune.cs` | 筛查 / 事件IL | R1:56；R9:64；R11:22 |
| `src/Runes/CompensationRune.cs` | 筛查 / 事件IL | R1:79；R8:5；R9:149；R11:108 |
| `src/Runes/CondensedRadianceRune.cs` | 筛查 / 事件IL | R1:24 |
| `src/Runes/CoreOverloadRune.cs` | 筛查 / 事件IL | R1:28 |
| `src/Runes/CorpseExplosionRune.cs` | 筛查 / 事件IL | R1:35；R11:22 |
| `src/Runes/CorrosionRune.cs` | 筛查 / 事件IL | R1:28 |
| `src/Runes/CorrosiveWaveUpgradeRune.cs` | 筛查 / 事件IL | R1:29；R9:42 |
| `src/Runes/CorruptedBranchRune.cs` | 上下文 / 事件IL | R1:45；R3:7；R4:157；R5:127；R11:85 |
| `src/Runes/CourageOfColossusRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/CrashLandingUpgradeRune.cs` | 筛查 / 事件IL | R1:29；R9:57；R11:23 |
| `src/Runes/CreativeAiUpgradeRune.cs` | 上下文 / 事件IL | R1:46；R4:29；R5:35；R9:59 |
| `src/Runes/CrossOrbRune.cs` | 上下文 / 事件IL | R1:42；R4:167；R5:125；R8:145；R9:21 |
| `src/Runes/CurtainCallRune.cs` | 筛查 / 事件IL | R1:34；R3:5 |
| `src/Runes/CuttingEdgeAlchemistRune.cs` | 上下文 / 事件IL | R1:46；R4:28；R5:75；R9:62 |
| `src/Runes/DawnbringersResolveRune.cs` | 筛查 / 事件IL | R1:53；R3:9 |
| `src/Runes/DeathHarvestRune.cs` | 筛查 / 事件IL | R1:28；R11:14 |
| `src/Runes/DeathWarrantRune.cs` | 筛查 / 事件IL | R1:16；R3:9；R11:128 |
| `src/Runes/DecayRune.cs` | 筛查 / 事件IL | R1:45 |
| `src/Runes/DecisionsDecisionsUpgradeRune.cs` | 筛查 / 事件IL | R1:15；R9:85 |
| `src/Runes/DefendUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/DemonFormUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/DeviantCognitionRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/DevilsDanceRune.cs` | 筛查 / 事件IL | R1:91；R3:7 |
| `src/Runes/DexterityStrengthToFocusRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/DexterityToStrengthRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/DiceManiacRune.cs` | 上下文 / 事件IL | R1:59；R3:15；R4:60；R5:47 |
| `src/Runes/DieForYouRune.cs` | 筛查 / 事件IL | R1:47；R3:10；R11:66 |
| `src/Runes/DirgeUpgradeRune.cs` | 筛查 / 事件IL | R1:33 |
| `src/Runes/DivineInterventionRune.cs` | 筛查 / 事件IL | R1:53；R11:34 |
| `src/Runes/DizzySpinningRune.cs` | 筛查 / 事件IL | R1:28；R5:29；R11:22 |
| `src/Runes/DonationRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/DoomsdayRune.cs` | 筛查 / 事件IL | R1:34；R11:23 |
| `src/Runes/DoubleExistenceRune.cs` | 筛查 / 事件IL | R1:19 |
| `src/Runes/DoubleVisionRune.Duplication.cs` | 上下文 / 事件IL | R1:41；R7:157；R10:301 |
| `src/Runes/DoubleVisionRune.Scopes.cs` | 上下文 / 事件IL | R7:125；R9:117 |
| `src/Runes/DoubleVisionRune.Transactions.cs` | 筛查 / 事件IL | R2:112；R8:326 |
| `src/Runes/DoubleVisionRune.Types.cs` | 筛查 / 事件IL | R9:135 |
| `src/Runes/DoubleVisionRune.cs` | 筛查 / 事件IL | R3:23；R11:53 |
| `src/Runes/DragonSoulRuneBase.cs` | 筛查 | R1:20 |
| `src/Runes/DrainRune.cs` | 筛查 / 事件IL | R1:28；R11:17 |
| `src/Runes/DrawYourSwordRune.cs` | 筛查 / 事件IL | R1:35 |
| `src/Runes/DualWieldRune.cs` | 筛查 / 事件IL | R1:14 |
| `src/Runes/DualcastUpgradeRune.cs` | 筛查 / 事件IL | R1:36 |
| `src/Runes/DuffsVintageRune.cs` | 筛查 / 事件IL | R1:30 |
| `src/Runes/EarthAwakensRune.cs` | 筛查 / 事件IL | R1:65；R3:7 |
| `src/Runes/EasyDoesItRune.cs` | 筛查 / 事件IL | R1:22 |
| `src/Runes/EchoFormUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/EchoRune.cs` | 筛查 / 事件IL | R1:33 |
| `src/Runes/EightPennyGateRune.cs` | 筛查 / 事件IL | R1:30 |
| `src/Runes/ElectricSurgeRune.cs` | 筛查 / 事件IL | R1:22；R11:17 |
| `src/Runes/ElectrodynamicsRune.cs` | 筛查 / 事件IL | R1:28；R9:45；R11:23 |
| `src/Runes/EmergenceRune.cs` | 筛查 / 事件IL | R1:22；R5:25；R11:30 |
| `src/Runes/EndlessRecoveryRune.cs` | 筛查 / 事件IL | R1:17 |
| `src/Runes/EndlessRotationRune.cs` | 筛查 / 事件IL | R1:49；R8:5 |
| `src/Runes/EnlightenmentRune.cs` | 筛查 | R9:16 |
| `src/Runes/EscapePlanRune.cs` | 筛查 / 事件IL | R1:61；R3:10 |
| `src/Runes/EternalArmorUpgradeRune.cs` | 筛查 / 事件IL | R1:39；R9:54 |
| `src/Runes/EurekaRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/ExplosionArtRune.cs` | 筛查 / 事件IL | R1:33 |
| `src/Runes/ExposeUpgradeRune.cs` | 筛查 / 事件IL | R1:30 |
| `src/Runes/ExtremeSpeedRune.cs` | 筛查 / 事件IL | R1:59 |
| `src/Runes/FallingStarUpgradeRune.cs` | 筛查 / 事件IL | R1:41；R11:52 |
| `src/Runes/FanTheHammerRune.cs` | 筛查 / 事件IL | R1:83；R3:10；R11:137 |
| `src/Runes/FeedUpgradeRune.cs` | 筛查 / 事件IL | R1:76；R3:13 |
| `src/Runes/FeelTheBurnRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/FeyMagicRune.cs` | 筛查 / 事件IL | R1:29；R3:7 |
| `src/Runes/FinalFormRune.cs` | 筛查 / 事件IL | R1:67；R3:7 |
| `src/Runes/FirebrandRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/FirstAidKitRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/FirstTypedCardReplayRuneBase.cs` | 筛查 / 事件IL | R1:55 |
| `src/Runes/FlakCannonUpgradeRune.cs` | 筛查 | R9:37；R11:24 |
| `src/Runes/FlameBarrierUpgradeRune.cs` | 筛查 | R9:16 |
| `src/Runes/FlawlessRune.cs` | 筛查 / 事件IL | R1:22 |
| `src/Runes/FleshAndBoneRune.cs` | 筛查 / 事件IL | R1:25 |
| `src/Runes/FlyingKickCorpseLaunchDriver.cs` | 筛查 | R2:42；R6:72；R8:13；R9:49；R11:154；R12:42 |
| `src/Runes/FlyingKickRune.cs` | 上下文 / 事件IL | R1:99；R9:141；R11:108 |
| `src/Runes/ForbiddenGrimoireRune.cs` | 筛查 / 事件IL | R1:22 |
| `src/Runes/ForgottenSoulRune.cs` | 筛查 / 事件IL | R1:26 |
| `src/Runes/FrostWraithRune.cs` | 筛查 / 事件IL | R1:58；R11:32 |
| `src/Runes/FuriousGlareRune.cs` | 筛查 / 事件IL | R1:40 |
| `src/Runes/GalacticGiftRune.cs` | 筛查 / 事件IL | R1:14；R3:7 |
| `src/Runes/GetExcitedRune.cs` | 筛查 / 事件IL | R1:48；R3:8 |
| `src/Runes/GhostFormRune.cs` | 筛查 / 事件IL | R1:24 |
| `src/Runes/GiantSerpentsFangRune.cs` | 筛查 / 事件IL | R1:23 |
| `src/Runes/GiantSlayerRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/GlassCannonRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/GloomyCloudsRune.cs` | 筛查 / 事件IL | R1:25 |
| `src/Runes/GoldCardCustomerRune.cs` | 筛查 / 事件IL | R1:28 |
| `src/Runes/GoldenSpatulaRune.cs` | 筛查 / 事件IL | R1:28；R3:8 |
| `src/Runes/GoldrendRune.cs` | 筛查 / 事件IL | R1:21；R3:14 |
| `src/Runes/GoliathRune.cs` | 筛查 | R3:7 |
| `src/Runes/GoodLuckRune.cs` | 上下文 / 事件IL | R1:42；R4:34；R8:16；R9:13 |
| `src/Runes/GrandFinaleUpgradeRune.cs` | 筛查 | R9:52；R11:20 |
| `src/Runes/GroundedRune.cs` | 筛查 / 事件IL | R1:17 |
| `src/Runes/GrowingStrongerRune.cs` | 筛查 / 事件IL | R1:47；R5:80；R11:85 |
| `src/Runes/HailToTheKingRune.cs` | 上下文 / 事件IL | R1:33；R4:36 |
| `src/Runes/HandOfBaronRune.cs` | 筛查 / 事件IL | R1:23 |
| `src/Runes/HangUpgradeRune.cs` | 筛查 | R9:32 |
| `src/Runes/HappyAccidentRune.cs` | 筛查 / 事件IL | R1:34；R5:38；R11:22 |
| `src/Runes/HardBonesRune.cs` | 筛查 / 事件IL | R1:27 |
| `src/Runes/HastyScribbleRune.cs` | 筛查 / 事件IL | R1:19 |
| `src/Runes/HattrickRune.cs` | 筛查 / 事件IL | R1:15；R9:9 |
| `src/Runes/HeavyHitterRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/HextechDragonSoulRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/HextechGoldrendSync.cs` | 上下文 | R7:65；R8:9；R9:201 |
| `src/Runes/HextechRuneTargeting.cs` | 筛查 | R5:26 |
| `src/Runes/HextechSharedCombatVictoryRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/HiddenGemUpgradeRune.cs` | 筛查 / 事件IL | R1:24；R5:49；R9:75；R11:55 |
| `src/Runes/HomeguardRune.cs` | 筛查 / 事件IL | R1:60；R3:13 |
| `src/Runes/HotfixUpgradeRune.cs` | 筛查 / 事件IL | R1:20 |
| `src/Runes/HubrisRune.cs` | 筛查 / 事件IL | R1:14；R3:7；R11:69 |
| `src/Runes/HundredRefinementsRune.cs` | 上下文 / 事件IL | R1:18；R3:11；R4:81；R9:36 |
| `src/Runes/IllusoryWeaponRune.cs` | 筛查 / 事件IL | R1:42；R9:69；R11:22 |
| `src/Runes/ImmortalBoneRune.cs` | 筛查 / 事件IL | R1:22 |
| `src/Runes/InfernalConduitRune.cs` | 筛查 / 事件IL | R1:63；R3:12；R11:43 |
| `src/Runes/InfernalDragonSoulRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/InfernoUpgradeRune.cs` | 上下文 | R1:32；R7:32；R9:44；R11:18 |
| `src/Runes/InfiniteLoopRune.cs` | 筛查 / 事件IL | R1:14；R3:7 |
| `src/Runes/InitialForgeGrantRune.cs` | 上下文 / 事件IL | R1:22；R3:5；R4:40；R9:35 |
| `src/Runes/InkshadowRune.cs` | 筛查 / 事件IL | R1:70；R9:42；R11:85 |
| `src/Runes/InstantDeathRune.cs` | 筛查 / 事件IL | R1:42；R6:23 |
| `src/Runes/IronWaveUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/JackpotUpgradeRune.cs` | 上下文 / 事件IL | R1:38；R4:40；R5:44；R9:61；R11:33 |
| `src/Runes/JeweledGauntletRune.cs` | 筛查 / 事件IL | R1:64；R5:35；R8:10；R11:40 |
| `src/Runes/JinlianBoxRune.cs` | 上下文 / 事件IL | R1:50；R5:42 |
| `src/Runes/JudicatorRune.cs` | 筛查 / 事件IL | R1:32 |
| `src/Runes/JuggernautUpgradeRune.cs` | 筛查 | R9:17 |
| `src/Runes/KakaRune.cs` | 筛查 / 事件IL | R1:38；R11:21 |
| `src/Runes/KeystoneHunterRune.cs` | 筛查 / 事件IL | R1:29 |
| `src/Runes/KillerHunterRune.cs` | 筛查 / 事件IL | R1:34；R11:23 |
| `src/Runes/KingdomArmyRune.cs` | 上下文 / 事件IL | R1:39；R4:38；R5:38；R11:31 |
| `src/Runes/KnowThyPlaceUpgradeRune.cs` | 筛查 / 事件IL | R1:21 |
| `src/Runes/LethalTempoRune.cs` | 筛查 / 事件IL | R1:27 |
| `src/Runes/LifeFlowRune.cs` | 筛查 / 事件IL | R1:18；R3:7 |
| `src/Runes/LightEmUpRune.cs` | 上下文 / 事件IL | R1:25；R2:87；R3:18；R11:72；R12:87 |
| `src/Runes/LingeringMightRune.cs` | 筛查 / 事件IL | R1:62；R3:14；R5:50；R11:33 |
| `src/Runes/LoopRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/LoopUpgradeRune.cs` | 筛查 | R9:28 |
| `src/Runes/LubricantRune.cs` | 筛查 / 事件IL | R1:18；R3:7；R9:59 |
| `src/Runes/MadScientistRune.cs` | 上下文 / 事件IL | R1:34；R7:65；R9:42 |
| `src/Runes/MagicMissileRune.cs` | 上下文 / 事件IL | R1:60；R2:65；R11:52；R12:65 |
| `src/Runes/MakeItMineRune.cs` | 筛查 / 事件IL | R1:14；R3:7；R11:69 |
| `src/Runes/ManipulateRealityRune.cs` | 筛查 / 事件IL | R1:15 |
| `src/Runes/MarkovBabbleRune.cs` | 筛查 / 事件IL | R1:31；R5:34 |
| `src/Runes/MasterOfDualityRune.cs` | 筛查 / 事件IL | R1:14 |
| `src/Runes/MentalShieldRune.cs` | 筛查 / 事件IL | R1:23 |
| `src/Runes/MikaelsBlessingRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/MindOverMatterRune.cs` | 上下文 / 事件IL | R1:44；R4:30；R5:34 |
| `src/Runes/MindToMatterRune.cs` | 筛查 / 事件IL | R1:25 |
| `src/Runes/MirageRune.cs` | 筛查 / 事件IL | R1:34 |
| `src/Runes/MirrorReflectionRune.cs` | 筛查 / 事件IL | R1:25 |
| `src/Runes/MiserableFateRune.cs` | 筛查 / 事件IL | R1:34 |
| `src/Runes/MiseryRune.cs` | 筛查 | R11:19 |
| `src/Runes/MiseryUpgradeRune.cs` | 筛查 / 事件IL | R1:43；R11:20 |
| `src/Runes/MobileHomeRune.cs` | 筛查 / 事件IL | R1:32 |
| `src/Runes/MoltenFistUpgradeRune.cs` | 筛查 / 事件IL | R1:25；R11:20 |
| `src/Runes/MonarchsGazeRune.cs` | 筛查 / 事件IL | R1:22 |
| `src/Runes/MoreTheMerrierRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/MoreUniversalScopeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/MostUniversalScopeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/MountainSoulRune.cs` | 筛查 / 事件IL | R1:60；R3:13 |
| `src/Runes/MyriadManifestationsRune.cs` | 筛查 / 事件IL | R1:23 |
| `src/Runes/MyriadSwordsRune.cs` | 筛查 / 事件IL | R1:54；R11:36 |
| `src/Runes/MysteryRune.cs` | 筛查 / 事件IL | R1:24 |
| `src/Runes/NatureIsHealingRune.cs` | 上下文 / 事件IL | R1:52；R2:99；R6:10；R11:132；R12:99 |
| `src/Runes/NearDeathFeastRune.cs` | 上下文 / 事件IL | R1:203；R2:141；R3:16；R6:225；R8:10；R9:81；R12:141 |
| `src/Runes/NeowsGrudgeRune.cs` | 筛查 / 事件IL | R1:25 |
| `src/Runes/NetherSoulRune.cs` | 筛查 / 事件IL | R1:28；R11:20 |
| `src/Runes/NeurosurgeUpgradeRune.cs` | 筛查 | R9:20 |
| `src/Runes/NeutralizeUpgradeRune.cs` | 筛查 / 事件IL | R1:53；R11:37 |
| `src/Runes/NightmareRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/NightmareUpgradeRune.cs` | 筛查 | R9:23；R11:15 |
| `src/Runes/NightstalkingRune.cs` | 筛查 / 事件IL | R1:14；R3:7 |
| `src/Runes/NimbleRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/NineDragonPowerRune.cs` | 筛查 / 事件IL | R1:28；R3:8 |
| `src/Runes/NonupeipeGenerosityRune.cs` | 上下文 / 事件IL | R1:18 |
| `src/Runes/NowYouSeeMeRune.cs` | 筛查 / 事件IL | R1:29 |
| `src/Runes/OblivionUpgradeRune.cs` | 筛查 | R9:20 |
| `src/Runes/OceanDragonSoulRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/OkBoomerangRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/OmegaRune.cs` | 筛查 / 事件IL | R1:28；R11:16 |
| `src/Runes/OminousPactRune.cs` | 筛查 / 事件IL | R1:35 |
| `src/Runes/OmniDragonSoulRune.cs` | 筛查 / 事件IL | R1:33；R5:57；R11:26 |
| `src/Runes/OrbSymbiosisRune.cs` | 筛查 / 事件IL | R1:36 |
| `src/Runes/OrobasBlessingRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/OurHealingRune.cs` | 筛查 / 事件IL | R1:58 |
| `src/Runes/OverflowRune.cs` | 筛查 | R9:20 |
| `src/Runes/OverlordBloodArmorRune.cs` | 筛查 / 事件IL | R1:29 |
| `src/Runes/PacifistRune.cs` | 上下文 / 事件IL | R1:83；R8:5；R9:139 |
| `src/Runes/PactsEndUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/PandorasBoxRune.cs` | 上下文 / 事件IL | R1:15；R4:16；R8:20 |
| `src/Runes/ParticleWallUpgradeRune.cs` | 筛查 / 事件IL | R1:13 |
| `src/Runes/PiercingThreadRune.cs` | 筛查 / 事件IL | R1:92；R8:7；R9:79 |
| `src/Runes/PiggyBankRune.cs` | 筛查 / 事件IL | R1:16；R3:9 |
| `src/Runes/PlasterRune.cs` | 筛查 / 事件IL | R1:27 |
| `src/Runes/PlateletRune.cs` | 筛查 / 事件IL | R1:32；R6:15 |
| `src/Runes/PorcupineRune.cs` | 筛查 / 事件IL | R1:44 |
| `src/Runes/PortableSleepingBagRune.cs` | 筛查 / 事件IL | R1:32 |
| `src/Runes/PowerShieldRune.cs` | 筛查 / 事件IL | R1:47 |
| `src/Runes/PrecisionCognitionRune.cs` | 筛查 / 事件IL | R1:27 |
| `src/Runes/PrimitiveMadnessRune.cs` | 筛查 / 事件IL | R1:46 |
| `src/Runes/PrismaticEggRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/ProtectiveVeilRune.cs` | 筛查 / 事件IL | R1:32；R3:7 |
| `src/Runes/ProteinShakeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/QuantumComputingRune.cs` | 筛查 / 事件IL | R1:42；R11:23 |
| `src/Runes/QueenRune.cs` | 筛查 / 事件IL | R1:35 |
| `src/Runes/RageUpgradeRune.cs` | 筛查 | R9:11 |
| `src/Runes/RallyingCallRune.cs` | 筛查 / 事件IL | R1:27；R11:11 |
| `src/Runes/ReanimateUpgradeRune.cs` | 上下文 / 事件IL | R1:36；R3:7；R9:54 |
| `src/Runes/ReapUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/ReaperFormUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/RebootUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/RecycleBinRune.cs` | 筛查 / 事件IL | R1:20 |
| `src/Runes/RedEnvelopeRune.cs` | 上下文 / 事件IL | R1:37；R3:11；R4:47；R5:38 |
| `src/Runes/ReflectUpgradeRune.cs` | 筛查 | R9:11 |
| `src/Runes/ReforgedHelmetRune.cs` | 筛查 / 事件IL | R1:29；R9:20 |
| `src/Runes/RegenerationSuppressionRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/RekindleRune.cs` | 筛查 / 事件IL | R1:14；R3:7 |
| `src/Runes/RenewalRune.cs` | 筛查 / 事件IL | R1:22 |
| `src/Runes/ReprogramRune.cs` | 筛查 / 事件IL | R1:24 |
| `src/Runes/RepulsorRune.cs` | 筛查 / 事件IL | R1:64；R3:10 |
| `src/Runes/RoyalCommandRune.cs` | 筛查 / 事件IL | R1:32 |
| `src/Runes/RoyalTrialRune.cs` | 上下文 / 事件IL | R1:56；R4:63；R5:63；R11:45 |
| `src/Runes/RoyaltiesUpgradeRune.cs` | 筛查 / 事件IL | R1:17；R3:10；R11:32 |
| `src/Runes/SacrificeRune.cs` | 筛查 / 事件IL | R1:23；R3:16；R11:52 |
| `src/Runes/ScapegoatRune.cs` | 筛查 / 事件IL | R1:30；R11:18 |
| `src/Runes/ScaredStiffRune.cs` | 筛查 / 事件IL | R1:31；R11:41 |
| `src/Runes/SearingAttackRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/SelfUpgradeOnPlayRuneBase.cs` | 筛查 | R1:40；R3:10 |
| `src/Runes/SellOffRune.cs` | 筛查 / 事件IL | R1:64；R11:71 |
| `src/Runes/SendThemInRune.cs` | 上下文 / 事件IL | R1:46；R4:44；R5:44 |
| `src/Runes/SerpentFormUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/SerpentsFangRune.cs` | 筛查 / 事件IL | R1:34 |
| `src/Runes/ServantMasterRune.cs` | 筛查 / 事件IL | R1:28；R11:34 |
| `src/Runes/ShoulderVakuRune.cs` | 筛查 / 事件IL | R1:57；R9:97；R11:95 |
| `src/Runes/ShriekUpgradeRune.cs` | 筛查 / 事件IL | R1:38；R11:27 |
| `src/Runes/ShrinkEngineRune.cs` | 筛查 / 事件IL | R1:14；R3:7 |
| `src/Runes/ShrinkRayRune.cs` | 筛查 / 事件IL | R1:28 |
| `src/Runes/SingularityAIRune.cs` | 上下文 / 事件IL | R1:40；R4:27；R5:31 |
| `src/Runes/SkyDrillUpgradeRune.cs` | 筛查 / 事件IL | R1:39 |
| `src/Runes/SlapRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/SlowCookRune.cs` | 筛查 / 事件IL | R1:40；R11:28 |
| `src/Runes/SmokestackUpgradeRune.cs` | 筛查 / 事件IL | R1:13；R11:14 |
| `src/Runes/SnakebiteRune.cs` | 筛查 / 事件IL | R1:43 |
| `src/Runes/SnakebiteUpgradeRune.cs` | 筛查 / 事件IL | R1:30 |
| `src/Runes/SolidTimeRune.HoverTips.cs` | 筛查 / 事件IL | 未命中所列模式（不是安全证明） |
| `src/Runes/SolidTimeRune.PowerPlayback.cs` | 筛查 / 事件IL | R9:95 |
| `src/Runes/SolidTimeRune.StoredCards.cs` | 筛查 / 事件IL | 未命中所列模式（不是安全证明） |
| `src/Runes/SolidTimeRune.cs` | 筛查 / 事件IL | R1:43；R3:8；R11:53 |
| `src/Runes/SomethingForNothingRune.cs` | 上下文 / 事件IL | R1:62；R3:13 |
| `src/Runes/SomethingFromNothingRune.cs` | 筛查 / 事件IL | R1:27 |
| `src/Runes/SonataRune.cs` | 筛查 | R11:30 |
| `src/Runes/SoulCallingRune.cs` | 筛查 / 事件IL | R1:44；R3:17 |
| `src/Runes/SoulEaterRune.cs` | 筛查 / 事件IL | R1:46；R3:12；R11:77 |
| `src/Runes/SoulUpgradeRune.cs` | 筛查 / 事件IL | R1:27 |
| `src/Runes/SowUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/SpeedDemonRune.cs` | 筛查 / 事件IL | R1:58；R3:7 |
| `src/Runes/SpeedsterRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/SpinToWinRune.cs` | 筛查 / 事件IL | R1:56 |
| `src/Runes/StardustUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/StarlightSplendorRune.cs` | 筛查 / 事件IL | R1:28；R11:17 |
| `src/Runes/StartupRoutineRune.cs` | 筛查 / 事件IL | R1:17 |
| `src/Runes/StatsOnStatsOnStatsRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/StatsOnStatsRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/StatsRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/StokeRune.cs` | 上下文 | R9:14 |
| `src/Runes/StormUpgradeRune.cs` | 上下文 | 未命中所列模式（不是安全证明） |
| `src/Runes/StrengthToDexterityRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/StrikeUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/SturdyRune.cs` | 筛查 / 事件IL | R1:25 |
| `src/Runes/SubroutineUpgradeRune.cs` | 筛查 / 事件IL | R1:53；R3:8；R9:64 |
| `src/Runes/SummonForthRune.cs` | 筛查 / 事件IL | R1:27 |
| `src/Runes/SuperBrainRune.cs` | 筛查 / 事件IL | R1:18 |
| `src/Runes/SurvivorUpgradeRune.cs` | 筛查 / 事件IL | R1:45；R9:52 |
| `src/Runes/SweepingBladeRune.cs` | 筛查 / 事件IL | R1:37；R8:198；R11:26 |
| `src/Runes/SwiftAndSafeRune.cs` | 筛查 / 事件IL | R1:14；R3:7 |
| `src/Runes/SwordFlightRune.cs` | 筛查 / 事件IL | R1:64 |
| `src/Runes/SwordIntentRune.cs` | 筛查 | R9:20 |
| `src/Runes/SwordsmanshipRune.cs` | 筛查 / 事件IL | R1:27 |
| `src/Runes/SymphonyOfWarRune.cs` | 筛查 / 事件IL | R1:33 |
| `src/Runes/TankEngineRune.cs` | 筛查 / 事件IL | R1:28；R3:8 |
| `src/Runes/TanksShieldRune.cs` | 筛查 / 事件IL | R1:17 |
| `src/Runes/TapDanceRune.cs` | 筛查 / 事件IL | R1:14；R3:7 |
| `src/Runes/TauntRune.cs` | 筛查 / 事件IL | R1:30 |
| `src/Runes/TerminalIllnessRune.cs` | 筛查 / 事件IL | R1:34；R9:25；R11:45 |
| `src/Runes/TezcatarasMercyRune.cs` | 上下文 / 事件IL | R1:47；R3:9 |
| `src/Runes/ThornmailRune.cs` | 筛查 / 事件IL | R1:31 |
| `src/Runes/ThoughtOverwriteRune.cs` | 筛查 / 事件IL | R1:60；R3:7 |
| `src/Runes/TormentorRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/TranscendentEvilRune.cs` | 筛查 / 事件IL | R1:14；R3:7 |
| `src/Runes/TransformBasicCardOnPlayRuneBase.cs` | 筛查 | R1:34；R11:29 |
| `src/Runes/TransmuteChaosRune.cs` | 上下文 / 事件IL | R1:15；R4:16 |
| `src/Runes/TransmuteGoldRune.cs` | 上下文 / 事件IL | R1:15；R4:16 |
| `src/Runes/TransmutePrismaticRune.cs` | 上下文 / 事件IL | R1:15；R4:16 |
| `src/Runes/TriPrismRune.cs` | 筛查 / 事件IL | R1:69；R9:39 |
| `src/Runes/TrickLicenseRune.cs` | 筛查 | R9:20 |
| `src/Runes/TrinityRune.cs` | 筛查 / 事件IL | R1:24 |
| `src/Runes/TwiceThriceRune.cs` | 筛查 / 事件IL | R1:16；R3:9 |
| `src/Runes/TwilightVeilRune.cs` | 筛查 / 事件IL | R1:78；R8:12；R11:54 |
| `src/Runes/TwinFlamesRune.cs` | 上下文 / 事件IL | R1:61；R2:62；R5:46；R11:33；R12:62 |
| `src/Runes/UltimateRefreshRune.cs` | 筛查 / 事件IL | R1:34；R3:5 |
| `src/Runes/UltimateUnstoppableRune.cs` | 筛查 / 事件IL | R1:23 |
| `src/Runes/UndyingUpgradeRune.cs` | 筛查 / 事件IL | R1:44；R3:5；R9:71 |
| `src/Runes/UniversalScopeRune.cs` | 筛查 / 事件IL | R1:57；R5:94；R9:91；R11:99 |
| `src/Runes/UnleashUpgradeRune.cs` | 筛查 / 事件IL | R1:36 |
| `src/Runes/UnmovableMountainRune.cs` | 筛查 / 事件IL | R1:24 |
| `src/Runes/UnsealedThroneRune.cs` | 筛查 / 事件IL | R1:32 |
| `src/Runes/UpgradeRune.cs` | 筛查 / 事件IL | R1:21；R9:35 |
| `src/Runes/VakuuTurnController.cs` | 筛查 | R5:78；R11:13 |
| `src/Runes/VampireCrawlerRune.cs` | 筛查 / 事件IL | R1:15；R11:8 |
| `src/Runes/VenerateUpgradeRune.cs` | 筛查 / 事件IL | R1:26 |
| `src/Runes/VenomousBladeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/ViolenceRune.cs` | 筛查 / 事件IL | R1:35；R5:25 |
| `src/Runes/VitalitySurgeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/VoidFormUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/VoltaicUpgradeRune.cs` | 筛查 / 事件IL | R1:20；R9:59 |
| `src/Runes/WarmogsSpiritRune.cs` | 筛查 / 事件IL | R1:16；R3:9 |
| `src/Runes/WatchOutGrapefruitRune.cs` | 筛查 / 事件IL | R1:60；R5:52 |
| `src/Runes/WhirlwindUpgradeRune.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Runes/WhiteHoleRune.cs` | 筛查 / 事件IL | R1:19 |
| `src/Runes/WizardlyThinkingRune.cs` | 筛查 / 事件IL | R1:23 |
| `src/Runes/WraithRune.cs` | 筛查 / 事件IL | R1:30 |
| `src/Runes/WroughtInWarUpgradeRune.cs` | 筛查 / 事件IL | R1:21；R9:35 |
| `src/Runes/ZapUpgradeRune.cs` | 筛查 / 事件IL | R1:29 |
| `src/Runes/ZealotRune.cs` | 筛查 | R11:13 |
| `src/Selection/Coordinator/HextechActSelectionGate.cs` | 筛查 | R9:15 |
| `src/Selection/Coordinator/HextechForgeSelectionCoordinator.cs` | 上下文 | R2:93；R4:34；R5:31；R7:50；R9:241；R10:61 |
| `src/Selection/Coordinator/HextechRarityRollResolver.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/Coordinator/HextechRelicOptionSelectionCoordinator.cs` | 筛查 | R2:75；R7:32；R10:43 |
| `src/Selection/Coordinator/HextechRuneSelectionCoordinator.ActRoll.cs` | 筛查 | R2:163；R5:278；R7:252；R8:218；R10:167 |
| `src/Selection/Coordinator/HextechRuneSelectionCoordinator.Core.cs` | 上下文 | R2:240；R6:77；R8:123；R9:290；R11:77 |
| `src/Selection/Coordinator/HextechRuneSelectionCoordinator.Selection.cs` | 筛查 | R2:85；R6:395；R7:53；R8:25；R9:432；R10:52 |
| `src/Selection/Coordinator/HextechRuneSelectionCoordinator.Types.cs` | 筛查 | R2:8；R10:35 |
| `src/Selection/EnemyAdjust/HextechMonsterHexRoller.cs` | 筛查 | R8:9 |
| `src/Selection/EnemyAdjust/HextechRuneSelectionCoordinator.EnemyHexSync.cs` | 筛查 | R2:166；R7:45；R8:46；R10:22 |
| `src/Selection/EnemyAdjust/HextechRuneSelectionCoordinator.EnemyOnly.cs` | 筛查 | R2:22；R7:25；R8:26 |
| `src/Selection/HextechEnemyHexAdjustmentOptions.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/HextechSelectionHelpers.cs` | 筛查 | R6:92；R9:11；R11:92 |
| `src/Selection/Pool/HextechRunePoolBuilder.cs` | 筛查 | R5:60；R8:13；R9:273 |
| `src/Selection/Pool/HextechRuneSelectionCoordinator.Pools.cs` | 筛查 | R8:31 |
| `src/Selection/Pool/HextechWeightedRuneOptions.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/Reroll/GoldenRerollConsoleCmd.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/Reroll/HextechGoldenRerollSession.cs` | 筛查 | R5:50；R9:19 |
| `src/Selection/Reroll/HextechRuneSelectionCoordinator.GoldenReroll.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/Reroll/HextechRuneSelectionCoordinator.Reroll.cs` | 上下文 | R5:79；R8:12 |
| `src/Selection/Sync/HextechChoiceCodec.cs` | 筛查 | R4:21；R8:91；R9:126；R10:37 |
| `src/Selection/Sync/HextechGeneratedRuneDataCodec.cs` | 筛查 | R9:30 |
| `src/Selection/Sync/HextechRuneSelectionCoordinator.Multiplayer.cs` | 筛查 | R2:178；R7:84；R8:23；R10:83 |
| `src/Selection/Sync/HextechRuneSelectionCoordinator.MultiplayerAck.cs` | 上下文 | R2:115；R6:93；R7:24；R10:23；R11:101 |
| `src/Selection/Sync/HextechRuneSelectionCoordinator.RemoteChoices.cs` | 上下文 | R2:84；R6:252；R9:315；R11:252；R12:258 |
| `src/Selection/Sync/HextechRuneWeightCodec.cs` | 筛查 | R9:21 |
| `src/Selection/Sync/HextechStableModelIdListCodec.cs` | 筛查 | R9:52 |
| `src/Selection/UI/HextechGoldenRerollVisual.cs` | 筛查 | R2:155；R6:127；R11:171；R12:155 |
| `src/Selection/UI/HextechRuneSelectionScreen.Audio.cs` | 筛查 | R1:21；R8:12 |
| `src/Selection/UI/HextechRuneSelectionScreen.Controller.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/UI/HextechRuneSelectionScreen.Core.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/UI/HextechRuneSelectionScreen.EnemyPreview.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/UI/HextechRuneSelectionScreen.Hover.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/UI/HextechRuneSelectionScreen.Interaction.cs` | 筛查 | R6:68；R9:127；R11:330 |
| `src/Selection/UI/HextechRuneSelectionScreen.Layout.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/UI/HextechRuneSelectionScreen.LayoutHelpers.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/UI/HextechRuneSelectionScreen.MapPreview.cs` | 筛查 | R2:128；R6:231；R11:231；R12:128 |
| `src/Selection/UI/HextechRuneSelectionScreen.Metadata.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/UI/HextechRuneSelectionScreen.Metrics.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/UI/HextechRuneSelectionScreen.PlayerCards.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Selection/UI/HextechRuneSelectionScreen.Style.cs` | 筛查 | R8:105 |
| `src/Services/HextechFeaturedConfigs.cs` | 筛查 | R6:21；R8:144 |
| `src/Services/HextechRunLogBudget.cs` | 筛查 | R8:6；R9:12 |
| `src/Services/HextechSteamIdentity.cs` | 筛查 | R9:16 |
| `src/Services/HextechUpdateChecker.Network.cs` | 筛查 | R6:23 |
| `src/Services/HextechUpdateChecker.cs` | 筛查 | R6:98；R9:116；R11:98；R12:49 |
| `src/Telemetry/HextechServerEndpoints.cs` | 筛查 | R9:20 |
| `src/Telemetry/HextechTelemetry.Config.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Telemetry/HextechTelemetry.PayloadBuilder.cs` | 筛查 | R5:17；R6:37 |
| `src/Telemetry/HextechTelemetry.PayloadTypes.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/Telemetry/HextechTelemetry.UploadQueue.cs` | 筛查 | R9:68 |
| `src/Telemetry/HextechTelemetry.cs` | 筛查 | R8:16；R12:100 |
| `src/UI/HextechControllerOverlay.cs` | 筛查 | 未命中所列模式（不是安全证明） |
| `src/UI/HextechEnemyHexCollapseView.cs` | 筛查 | R1:285 |
| `src/UI/HextechUiTheme.cs` | 筛查 | 未命中所列模式（不是安全证明） |
