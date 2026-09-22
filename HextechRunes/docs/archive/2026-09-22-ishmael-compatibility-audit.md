# 船玛与海克斯兼容性只读审计

审计日期：2026-09-22。目标为 [船玛工坊 3770894494](https://steamcommunity.com/sharedfiles/filedetails/?id=3770894494)。结论基于本机 DLL 反编译、当前未提交源码和隔离反射探针；没有启动游戏、操作存档、改设置或部署模组。

## 审计基线

- 本机游戏：`v0.111.0`，commit `41cef1ea`；按此检查船玛 `lib/0.111.0/PequodCaptainIshmael.dll`，没有把根目录加载器当作内容 DLL。
- 船玛目录：`/Users/iniad/Library/Application Support/Steam/steamapps/workshop/content/2868840/3770894494/`。Manifest 版本 `0.1.0`。目标 DLL SHA-256：`79e2def69b0bc476fd0fe953e7c70b9cdda98582cd649657bfd9d1fbbce51447`。
- 海克斯安装目录：`/Users/iniad/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/HextechRunes/`。Manifest 版本 `0.9.5`。`lib/0.111.0/HextechRunes.dll` SHA-256：`cfcc560a513f04566b5cf2cb91949a7fcd0b5bd0d650c6fef7d9e4e8cbc9d51b`，与其 variants manifest 一致。
- 当前工作区已删除和平主义者，并将精神过载升级改为自有 Power；安装 DLL 仍有 `PacifistRune` 和旧的 `NeurosurgeUpgradeRune.TypePatch`。因此安装包尚未包含这些源码修改。本文明确区分两者，不把旧问题当作最新源码的新发现。没有检查正在运行的游戏进程加载了哪个 DLL。
- 工坊说明是“额外幕 Boss 可能出问题，出现时禁用海克斯再测试”。当前反编译的加载器、初始化器和内容代码未找到点名 HextechRunes 后自动停用船玛的逻辑。没有找到可核实的公开源码仓库；反编译文件并非作者原始源码。
- 船玛内容程序集引用游戏、Godot、Harmony 和系统库；`LimbusShared.*` 位于该程序集内部，可选核心桥接不等于 RitsuLib 硬依赖。

证据目录：[完整反编译和探针](/Users/iniad/.codex/outputs/ishmael-audit-20260922)。其中 `ship/` 为船玛内容，`loader/` 为加载器，`hex-installed/` 为已安装海克斯；`current-source/`、`current-source-sha256.txt` 和 `workspace-diff-at-audit.patch` 保留本次相关源码状态。

## 1. P1：清除增益会拆掉亚哈转阶段和浮骸复活机制

**安装 DLL 与最新源码均存在。**

当前 [感受燃烧](/Users/iniad/sts2-mods/HextechRunes/src/Cards/FeelTheBurnCard.cs:34) 和 [升级暴露](/Users/iniad/sts2-mods/HextechRunes/src/Runes/ExposeUpgradeRune.cs:21) 枚举敌人增益，排除 `ShouldPreserveFromBuffRemoval` 后执行 `PowerCmd.Remove`。当前 [保护策略](/Users/iniad/sts2-mods/HextechRunes/src/Combat/HextechMonsterInteractionPolicy.cs:37) 只识别列出的原版机制类型和关系类 Power，没有船玛类型或第三方保护协议。

船玛的这些 Power 都标为 Buff：

- [GasHarpoonMechanismPower](/Users/iniad/.codex/outputs/ishmael-audit-20260922/ship/PequodCaptainIshmaelMod.DefinedAsEvil/GasHarpoonMechanismPower.cs:34)：`AfterDeath` 切入复活动作；同时决定尸体保留、阻止战斗结束、死亡后保留自身。被剥掉后，一、二阶段死亡失去这整套接管，可能直接移除 Boss、提前结束遭遇，跳过后续阶段。
- [FlotsamReformPower](/Users/iniad/.codex/outputs/ishmael-audit-20260922/ship/PequodCaptainIshmaelMod.DefinedAsEvil/FlotsamReformPower.cs:32)：保留浮骸尸体，死亡后计数并在两个敌方回合后复活。移除后失去复活和相应死亡反应。
- [GasHarpoonFinaleLockPower](/Users/iniad/.codex/outputs/ishmael-audit-20260922/ship/PequodCaptainIshmaelMod.DefinedAsEvil/GasHarpoonFinaleLockPower.cs:19)：终幕禁止选中亚哈，提供提示和心脏显示引导。感受燃烧枚举所有存活敌人，不受单体选中限制，因此可以剥掉这道终幕交互限制。此项证明的是交互语义被改变，不单独断言必然卡死。

新增的 `MarkBuffStripped` 动画异常护栏无法恢复已被移除的死亡/阶段 Hook，因此不能解决这个根因。

验证：隔离探针对以上三种真实程序集类型调用已安装海克斯保护函数，全部返回 `false`；当前源码的类型判定同样不覆盖它们。完整 Boss 遭遇的后果为调用链推导，尚未实机复现。

建议：先为经确认的第三方结构性 Power 增加不剥除规则；长期让未知第三方机制默认保留，通过显式能力协议或经审查的规则开放剥除。不要只在后续死亡/动画异常处吞异常。

## 2. P1：薄暮法衣复制斯达巴克援护攻击，产生确定的空怪物调用

**安装 DLL 与最新源码均存在。**

[TwilightVeilRune](/Users/iniad/sts2-mods/HextechRunes/src/Runes/TwilightVeilRune.cs:44) 在首个玩家回合开始后，镜像敌人新获得的 Buff。它仅靠 `IsMonsterMechanismBuff` 排除已知怪物机制，随后把 canonical Power 转成 mutable，施加给玩家。

实际触发链：

1. 亚哈三人组战中，斯达巴克在 `STARBUCK_ASSIST_MOVE` 的 [FollowOrders](/Users/iniad/.codex/outputs/ishmael-audit-20260922/ship/PequodCaptainIshmaelMod.DefinedAsEvil/EnemyStarbuck.cs:77) 施加 `EnemyAssistAttackPower`。这是正常战斗内施加，开场镜像门控挡不住。
2. 薄暮法衣复制该 Power 给玩家；现有策略没有排除它。
3. [EnemyAssistAttackPower.AfterApplied → Strike](/Users/iniad/.codex/outputs/ishmael-audit-20260922/ship/PequodCaptainIshmaelMod.DefinedAsEvil/EnemyAssistAttackPower.cs:32) 立刻调用 `DamageCmd.Attack(8).FromMonster(Owner.Monster)`。
4. 玩家 Creature 的 `Monster` 为空。原版 [AttackCommand.FromMonster](/Users/iniad/.codex/outputs/ishmael-audit-20260922/AttackCommand.cs:262) 直接读取 `monster.Creature`，没有空值分支。薄暮法衣的 `finally` 只解除镜像标记，不捕获并恢复该异常。

验证：真实安装策略 `skipMirror=false`；对原版实际程序集 `FromMonster(null)` 的隔离调用复现 `NullReferenceException`。这证明故障点，不等于已经运行整条游戏战斗链。实际表现预计为行动任务异常中断。

建议：把 `EnemyAssistAttackPower` 明确标为不可镜像；未知第三方 Buff 不能仅因 `Type == Buff` 就认定可施加给玩家。更换 Owner 不会自动把怪物能力转换成玩家能力。

## 3. P1：薄暮法衣复制援护防御，可能让玩家替敌方亚哈承伤

**安装 DLL 与最新源码均存在；与第 2 项同属镜像边界缺口，但表现不同。**

亚哈正常战斗动作 [ToMe](/Users/iniad/.codex/outputs/ishmael-audit-20260922/ship/PequodCaptainIshmaelMod.DefinedAsEvil/EnemyAhab.cs:117) 给魁魁格施加 `EnemyAssistBarrierPower`，会触发镜像。

[该 Power](/Users/iniad/.codex/outputs/ishmael-audit-20260922/ship/PequodCaptainIshmaelMod.DefinedAsEvil/EnemyAssistBarrierPower.cs:67) 从 `Owner.CombatState.Enemies` 中寻找活着的 `EnemyAhab`；当该敌人受到攻击时，`ModifyUnblockedDamageTarget` 返回 Power 的 Owner。镜像给玩家后，它仍然找得到敌方亚哈，却把承伤者换成了玩家。该方法没有验证 Owner 必须是敌方魁魁格。

因此，在该镜像参与伤害重定向选择时，原本打向亚哈的攻击可能变成玩家替 Boss 承伤。原 Power 和镜像同时存在时，最终采用谁受原版 Hook 遍历/重定向顺序影响，不能保证每击都转向玩家。即使没有触发转移，`ShouldClearBlock`、`AfterRemoved` 的格挡保留与清空也已迁移到玩家。

同样，终幕新施加的 `GasHarpoonFinaleLockPower` 会被镜像，其 `ShouldAllowTargeting` 将变成禁止选中玩家；它依赖亚哈语音和提示的后续逻辑也不适合玩家。实际受影响的目标选择场景需实机验证。

验证：两种 Power 的已安装海克斯 `skipMirror` 均为 `false`；行为后果来自双方当前代码调用链，未做实机伤害重定向顺序测试。

建议：援护防御、终幕锁定等涉及目标关系的 Power 与援护攻击一起排除。长期区分“数值增益”和“战斗结构/目标关系能力”，不能共用一个默认允许的镜像策略。

## 4. P2：亚哈换阶段直接重置最大生命，海克斯阶段基准没有同步接管

**当前源码存在的数值一致性问题；静态推导，未做完整离线或实机战斗复现。**

船玛 [Respawn / RestoreBody](/Users/iniad/.codex/outputs/ishmael-audit-20260922/ship/PequodCaptainIshmaelMod.DefinedAsEvil/EfflorescedEgoGasHarpoonAhab.cs:203) 在同一 Creature 上按阶段直接 `SetMaxHp(hp)`，再治疗。海克斯 [GoliathEnemyHex](/Users/iniad/sts2-mods/HextechRunes/src/EnemyHexes/GoliathEnemyHex.cs:26) 等用 CombatId 标记持久增益已经应用；[转阶段补发](/Users/iniad/sts2-mods/HextechRunes/src/Mayhem/HextechBossPhaseHexes.cs:15) 目前专门识别 `TestSubject`。海克斯全局 SetMaxHp 补丁只处理有 Player 的 Creature，不为这个敌人重建系数基准。

只带白银歌利亚、无其他生命调整的推导示例：亚哈初始基础 HP 195，经 20% 最大生命增益变为 234；二阶段代码直接改为 225，并没有经过歌利亚重新投影为 270 的路径。后续坦克引擎等再触发投影时，通用基准校准把外部变化视为“差额”，也不是识别“新阶段的完整基础生命”。

这首先表现为敌方海克斯跨阶段数值不一致，不应据此宣称 Boss 必然卡死。修复应建立可识别的阶段转换/基础 HP 更新契约，不能每次发现 MaxHp 变化都无条件再乘一次。

## 已检查但不列为已确认冲突

- **复活治疗被禁疗吞掉**：当前 Heal 代码识别敌方死亡状态下的正治疗，绕过敌方治疗修正、再生压制和治疗封顶；船玛还在仍死亡时提供直接恢复 HP 的兜底。正常阶段复活不能仅凭双方都改治疗就判为故障。
- **珠光护手重复亚哈复活动作**：船玛使用 `RESPAWN_MOVE`，海克斯已有按该 ID 排除复活动作的规则；最新源码还增加了等待后再次核对存活、战斗归属与 NextMove 的检查。没有发现足以证明重复复活的当前调用链。
- **濒死狂宴阻止终幕鱼叉结束战斗**：船玛终幕牌直接 `Kill(heart, force:true)`，随后授权亚哈脚本死亡并强杀；海克斯 Kill 路径清除濒死债务。不能把“1 HP 心脏获得额外负血”当作终幕牌打不死它的证据。
- **尸体清理无条件删掉转阶段 Boss**：当前海克斯死亡判定和滞留尸体清理会咨询 `ShouldCreatureBeRemovedFromCombatAfterDeath`；船玛机制 Power 完整时会阻止移除。真正明确的问题是第 1 项先剥掉了这些 Hook。
- **旧精神过载 canonical 异常**：工作区已改成自有 `HextechNeurosurgePower`，旧 `TypePatch` 问题不属于最新源码；安装包仍有旧实现，但本次没有找到船玛触发同一 canonical 扫描的直接路径。
- **船员代伤与濒死共用 LoseHpInternal**：对方默认优先级前缀先消耗援护者格挡，海克斯跳过型前缀是 Low。仅凭同目标补丁不足以认定格挡被跳过或船员会继承玩家濒死效果。

## 建议的最小实机回归矩阵

1. 亚哈第一阶段单独携带感受燃烧，使用后致死；检查阶段、尸体保留、胜利判定。再分别测试升级暴露和浮骸复活。
2. 亚哈三人组只携带薄暮法衣，等待斯达巴克援护攻击；检查 `FromMonster` 异常和行动能否完成。
3. 薄暮法衣等待魁魁格援护防御，再攻击亚哈；记录最终承伤目标、双方格挡及重定向 Hook 顺序。
4. 终幕持有薄暮法衣；检查玩家是否得到终幕锁定以及目标选择是否异常。
5. 只启用一个最大生命敌方海克斯，记录亚哈三个阶段的基础/实际 MaxHp；另测正常复活和终幕鱼叉，防止修补引入新回归。
6. 修复后再用双人一致版本复测以上路径；本报告未验证联机、断线重连或存档恢复。

本次仅新增此报告和外部证据文件；保留工作区原有兼容性改动，未修改任何被审计实现。
