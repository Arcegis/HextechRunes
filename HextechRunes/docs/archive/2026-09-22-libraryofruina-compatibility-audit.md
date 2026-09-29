# LibraryOfRuina 与海克斯兼容性只读审计（2026-09-22）

## 结论

对方当前版确实按模组 ID 把海克斯列入黑名单，并在初始化时跳过自身主要游戏内容。已确认一个海克斯自身的规范模型访问错误；另外存在独立伤害通道与海克斯命令记账不衔接、特殊数值规则覆盖符文效果的兼容面。不能把黑名单删除等同于完成兼容。

本次未修改模组源码、DLL、启用配置或存档，未部署、提交或发布。新增本审计报告；反编译输出和独立探针位于 [审计证据目录](/Users/iniad/.codex/outputs/lor-audit-20260922)。

## 样本与方法

- [工坊项目 3747541547](https://steamcommunity.com/sharedfiles/filedetails/?id=3747541547)：LibraryOfRuina；页面明确要求避免与 ARAM: Mayhem 同用。
- 本地工坊主模组 `v0.20.2`，前置 LibraryOfRuinaLib `1.2.14`，均要求游戏至少 `0.111.0`。主模组 manifest 还依赖 RitsuLib ≥ 0.6.2 与 ActLikeIt2 ≥ 0.2.0。
- 原版依据：本机 STS2 `v0.111.0`，游戏 commit `41cef1ea`。
- 海克斯：当前源码及本地安装的 `0.111.0` 实现变体，manifest 均为 `0.9.5`；工作区 HEAD `3cdfa97b0d1ebd8ea0f21078fb2534ca7300cd5d`。审计开始时海克斯目录没有 Git 改动。
- 使用 ILSpy 9.1 反编译主模组及前置库，原版类型使用 `tools/sts2-inspect` 定向核对。反编译文本是 DLL 还原结果，不是作者原始工程；部分类型含反编译器生成的语法，不保证可直接重编译。
- 查阅工坊和按模组名、作者名检索，未找到可确认对应当前项目的公开 GitHub 源码；这不证明仓库不存在。

DLL SHA-256：

| 样本 | SHA-256 |
| --- | --- |
| LibraryOfRuina.dll | `97820b141911cf2ccda9712fe30c1efc5704363458d281d2508d069054db9557` |
| LibraryOfRuinaLib.dll | `bff2436a845d0697c9bb9d843bf2597437aea5a6116352173f50899e676dbfb7` |
| 已安装 HextechRunes/lib/0.111.0/HextechRunes.dll | `cfcc560a513f04566b5cf2cb91949a7fcd0b5bd0d650c6fef7d9e4e8cbc9d51b` |

## 1. 自动停用：已确认

[IncompatibleModBlacklist.cs](/Users/iniad/.codex/outputs/lor-audit-20260922/main/LibraryOfRuina.compat/IncompatibleModBlacklist.cs:10) 只有一个条目：`HextechRunes`，使用大小写不敏感的精确 ID 比较。没有按海克斯版本区分，也不检查这一局是否启用了海克斯大乱斗。

[IncompatibleModGuard.cs](/Users/iniad/.codex/outputs/lor-audit-20260922/main/LibraryOfRuina.compat/IncompatibleModGuard.cs:29) 遍历 `ModManager.Mods`，排除 `Failed`、`Disabled`、`DisabledDuplicate`，其余状态若 ID 命中则阻止初始化。因此不是看磁盘上有没有文件，也不必等海克斯初始化完成才可检测。

[LibraryOfRuinaInitializer.cs](/Users/iniad/.codex/outputs/lor-audit-20260922/main/LibraryOfRuina/LibraryOfRuinaInitializer.cs:45) 命中后只安装设置相关补丁并返回，跳过内容池、主要运行控制器、BGM、遭遇战和游戏 Harmony 补丁，主菜单显示不兼容弹窗。

这不是卸载 DLL，也没有写玩家设置把海克斯关闭：前面的网络/UI 框架初始化、设置注册等已经发生。独立前置库 [Entry.cs](/Users/iniad/.codex/outputs/lor-audit-20260922/lib/LibraryLib/Entry.cs:14) 仍会以 `LibraryOfRuinaLib` 为 owner 执行自己的 `PatchAll()`，不受主模组黑名单直接控制。故“主模组内容关闭”不能当作“所有前置库补丁也消失”。

## 2. 精神过载规范模型异常：我方缺陷，独立探针已复现

调用链：

```text
图书馆 PowerIconLocalizationPatch.LoadVanillaEntries
  → 遍历 ModelDb.AllPowers 的规范模型，读取 power.Type
  → 海克斯 NeurosurgeUpgradeRune.TypePatch.Postfix
  → __instance.Owner
  → PowerModel.get_Owner → AssertMutable
  → CanonicalModelException
```

我方 [NeurosurgeUpgradeRune.cs:42](/Users/iniad/sts2-mods/HextechRunes/src/Runes/NeurosurgeUpgradeRune.cs:42) 读取 Owner 前缺少 `IsMutable` 判断。原版 NeurosurgePower.Type 只是返回 Debuff；[PowerModel.Owner](/Users/iniad/.codex/outputs/lor-audit-20260922/PowerModel.cs:269) 明确要求可变实例。规范模型可以读取类型，但不能读取战斗实例的拥有者。即便玩家没有拿到这个符文，参数求值也会先读取 Owner；不能靠 OwnsUpgradeRune 的判空化解。

已用 [独立探针](/Users/iniad/.codex/outputs/lor-audit-20260922/probe/Program.cs) 对已安装海克斯 DLL 直接反射调用该 Postfix。使用无构造初始化、`IsMutable=False` 的 NeurosurgePower 对象，先读取原版 Type，再调用补丁；没有调用模组初始化器或启动 Godot 游戏。

[输出](/Users/iniad/.codex/outputs/lor-audit-20260922/probe-result.txt)：原版结果为 Debuff；补丁抛出 CanonicalModelException，栈经过 `AbstractModel.AssertMutable → PowerModel.get_Owner → NeurosurgeUpgradeRune.TypePatch.Postfix`。这证明局部访问缺陷，不是完整 UI/实机复现。

当前图书馆 `v0.20.2` 的 [TryGetPowerType](/Users/iniad/.codex/outputs/lor-audit-20260922/main/LibraryOfRuina.localization/PowerIconLocalizationPatch.cs:91) 已捕获异常、记 Warn 并跳过该图标，`DecorateText` 还有外层保护。加上当前黑名单，这个版本通常不会通过原来的主模组路径继续触发整屏失败。因此，评论中“选书页卡死”的线索与局部异常吻合，但没有当时 DLL/完整日志，不能确认旧版连锁卡死细节，也不能说当前版仍必然卡死。

修复方向：我方类型 Postfix 对规范实例保留原版结果，仅对可变 Power 查询 Owner；检查同类补丁的规范实例访问。此次未实施修复。

## 3. 独立伤害路径：已有具体入口，战斗症状待实机验证

具体入口：[XiaoPulaoBellEgoCard.OnPlay](/Users/iniad/.codex/outputs/lor-audit-20260922/main/LibraryOfRuina.cards.Xiao/XiaoPulaoBellEgoCard.cs:77) 调用 `LibraryDamageCmd.Attack`，随后 [LibraryAttackCommand](/Users/iniad/.codex/outputs/lor-audit-20260922/lib/LibraryLib.Commands/LibraryAttackCommand.cs:532) 调用 `LibraryCreatureCmd.Damage`。

[LibraryCreatureCmd.Damage](/Users/iniad/.codex/outputs/lor-audit-20260922/lib/LibraryLib.Commands/LibraryCreatureCmd.cs:118) 对玩家目标转回原版 `CreatureCmd.Damage`，对非玩家目标则自己执行 LibraryHooks、格挡、掉血和后续回调。它仍会分发普通模型 Hook，不能说所有符文都不触发。

我方 [DamageCommandPatch](/Users/iniad/sts2-mods/HextechRunes/src/Hooks/Combat/HextechCombatHooks.Pacifist.cs:58) 只在原版 `CreatureCmd.Damage` 周围建立 `CurrentActualDamageCommandId`。当直接调用图书馆独立通道且没有外层原版伤害命令时，ID 为 0：

- [和平主义者](/Users/iniad/sts2-mods/HextechRunes/src/Runes/PacifistRune.cs:53) 的乘法 Hook 仍可把伤害乘为 0，但只有非零命令 ID 才登记待施加灾厄，AfterDamageGiven 又拒绝 ID 0。由代码可推导“伤害归零、灾厄也未补发”的路径。
- [敌方代偿](/Users/iniad/sts2-mods/HextechRunes/src/EnemyHexes/CompensationEnemyHex.cs:45) 在 ID 0 时直接返回原伤害，延期伤害效果可能失效。
- [穿针引线](/Users/iniad/sts2-mods/HextechRunes/src/Runes/PiercingThreadRune.cs:45) 对 ID 0 不登记穿透，属于同类适配风险。但上面的 Xiao 卡本身已设置 Unblockable，不能用它证明穿针引线另有丢失效果；仍需另一个可格挡的独立通道攻击作为实机案例。

相反，经原版 CreatureCmd.Damage 进入后再被图书馆前缀转发的攻击，有机会保留海克斯外层记账，不能一概判定所有图书馆伤害都不兼容。玩家侧“代偿”也不能据此判定普遍失效，因为玩家受伤分支会回到原版命令。

修复方向：明确独立伤害通道的记账边界、嵌套和清理语义，再做局部适配；不能直接对所有伤害重复建立作用域。

## 4. 无效化与数值分发：已确认机制覆盖，需要设计裁决

前置库的 [LibraryVanillaDamageResolutionPatch](/Users/iniad/.codex/outputs/lor-audit-20260922/lib/LibraryLib.Patches/LibraryVanillaDamageResolutionPatch.cs:15) 和 [LibraryVanillaBlockResolutionPatch](/Users/iniad/.codex/outputs/lor-audit-20260922/lib/LibraryLib.Patches/LibraryVanillaBlockResolutionPatch.cs:14) 在策略非 Default 时返回 false，跳过原版 Hook 分发，清空 modifiers，并返回基础值或 0。独立的 LibraryHooks.ModifyDamage 也有同类提前返回。

具体策略来源：[NullifyPower](/Users/iniad/.codex/outputs/lor-audit-20260922/main/LibraryOfRuina.powers/NullifyPower.cs:37) 对持有者适用的攻击/格挡使用 `BaseValueAndResistanceOnly`。因此相关海克斯乘区、格挡加成及和平主义者的伤害转灾厄逻辑会被一起绕过。这是有条件的机制覆盖，不是安装基础库后所有战斗都跳过 Hook。

需决定图书馆“无效化”是否应该连遗物/符文也一并压制。若遵循对方机制，可以把这项当明确规则；若只应压制部分 Power，则需要对方收窄分发。不能未经设计裁决全局强制恢复海克斯倍率。前置库还拦截 ModifyHpLost，但当前 NullifyPower 不作用于 HpLoss，不能把它写成本状态下必然触发的额外问题。

## 其余审阅面与边界

- 对方 CombatSafetyHookTaskPatch 包装所有返回 Task 的 Hook，网络前置库也有消息与动作序列化补丁。这些扩大兼容面，但尚无证据证明与海克斯形成特定联机错误；消息接收适配对非自身 envelope 有回退，不能仅凭补丁目标相同判定冲突。
- 敌方海克斯改 HP、回血、重复行动，与图书馆多阶段/假死机制需要组合测试。我方珠光护手已有 NextMove 身份检查，避免状态已变时重放旧行动，不能直接宣称所有多阶段 Boss 必坏。
- 当前自动停用阻止了主模组与海克斯正常同时启用。上述第 3、4 项针对兼容后并存或旧版本同时运行时的风险；本次没有移除黑名单去强行实测。

建议顺序：先修我方规范模型缺陷；再验证独立伤害路径的和平主义者/敌方代偿；裁决无效化的符文范围；最后在隔离环境验证奖励选择、Boss 阶段和双端联机，取得证据后再讨论取消整模组黑名单。

## 验证完成情况

已完成 DLL 反编译、原版契约核对、源码与已安装海克斯类型补丁一致性核对，以及局部异常探针复现。未进行游戏启动、奖励界面、真实战斗、存档读写或多人验证；未构建或部署模组。
