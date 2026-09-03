# HextechRunesSponsorPack 架构重构方案（2026-09-03）

> 取证基线：根仓库 `dev` 分支 7aca074f，拓展包 manifest 版本 0.9.1（`ModInfo.Version` 仍写着 0.8.7），编译目标 0.107.1 / 0.110.0 / 0.111.0，本机游戏 0.111.0。
> 参考对象：本机 Workshop 已安装的 MultiEnchantmentMod v2.5.4（3747561525，下称 MEM）、PengoTarot v1.4.16（3747679239）、STS2-RitsuLib 0.5.18，全部反编译后逐项核对。
> 原版 API 以 `HextechRunes/versioned-dll-backups/0.111.0/game-refs/sts2.dll` 为准，用 `tools/sts2-inspect` 核验。
> 本文遵循工作区 `docs/模组设计哲学.md`。§2 的裁决于 2026-09-03 同日更新：不再委托 MEM，改为重做附魔大师的效果（见 §2.3）。§6 "需要本体 / ISE 配合开的口子" 本轮不动（用户裁决），§7 的阶段按此收缩。所有"原版有扩展点"的结论都标注了核验位置。

## 0. 结论

1. **"多重附魔"是这个包的病灶本身，不是其中一个问题。** 5,523 行源码里 1,537 行（28%）在实现一套多附魔引擎，21 个 Harmony 补丁里 12 个为它服务，8 个"可跳过原方法"的前缀里 6 个是它的。它的存储模型（把 `card.Enchantment` 槽位换成一个复合模型）与社区事实标准 MEM 的存储模型（第一个附魔留在原版槽位，多余的放旁车）是相反的，这就是"装了 MEM 正常、没装 MEM 就和 PengoTarot 打架"的根本原因。
2. **裁决：不再实现"多重附魔"这个机制，把附魔大师的效果重做成"战斗结束时为牌组内随机牌添加随机合法附魔"。** 附魔大师从"改游戏规则的引擎"变成 `CardCmd.Enchant` 的一个普通调用方，MEM 在不在它都不用知道：没装 MEM，合法 = 空牌或原版可叠层的同类；装了 MEM，合法 = MEM 说了算。拓展包不再持有任何附魔中枢补丁，也不再探测或桥接 MEM。删掉 1,537 行、12 个补丁、13 处原版私有成员反射；新增一个 ≤120 行的"随机合法附魔"效果和一个 ≤60 行的旧存档迁移壳。
3. **其余 9 个补丁里 5 个可以退到官方扩展点或自家 API，本轮先做其中 2 个。** `Hook.AfterCombatEnd`、`Hook.ModifyCardBeingAddedToDeck` 两个全局分发点的补丁可以用 `EnchantmentModel` 自身的 `AfterCombatEnd` 覆写与实例级递归守卫替代（本轮做）；`Creature.SetCurrentHpInternal` 全局前缀与 4 处反射写 ISE 私有属性应移进 ISE 自己的 Interop、对本体 internal 类 `HextechForgeShopPriceHelper` 打的补丁应换成本体开一个 API（§6，本轮不动）。本轮结束后剩 7 个补丁、1 个 Harmony id；§6 落地后剩 4 个。
4. **加载器已经落后本体一轮。** 拓展包 `loader/LoaderBootstrap.cs` 是本体加载器的旧拷贝，第 135 行仍无条件安装 `ReflectionHelper.ModTypes` 后缀，本体 0.9.2 已修掉的"同一批类型贡献两次"根因在拓展包里原样存在。两份加载器必须共源。
5. **"命名空间冻结"是误判。** 已核实 `ModelDb.GetEntry` 只取 `type.Name`、本地化键只取 `Id.Entry`、net-id 表按 `ModelId` 与属性名排序，命名空间不参与任何持久化身份。14 个 `namespace HextechRunes;` 文件可以安全归位到 `HextechRunesSponsorPack`，只要类名与 `[SavedProperty]` 属性名不动。
6. 版本兼容层是健康的（`#if` 只有 3 处），本地化 9 语言键集 100% 对齐，多人同步路径静态审查未发现新分叉点。这些不动。

规模上，本方案把源码从 5,523 行压到约 3,700 行，补丁 21 → 7（§6 落地后 4），可跳过原方法的前缀 8 → 1，原版私有成员反射 12 → 0，Harmony id 9 → 1，`namespace` 2 套 → 1 套；**不改任何类名、`[SavedProperty]` 属性名、随机盐与 `HextechRunesApi` 调用面**，唯一的兼容性断裂是删除 `SponsorCompositeEnchantment` 这个模型 ID（见 §2.5，随版本号 0.10.0 发布）。

## 1. 现状取证

| 指标 | 数值 | 说明 |
|---|---|---|
| 源码规模 | 36 文件 / 5,523 行 | `src/Compat` 1,487 行（27%），`src/Runes` 1,472 行，`src/Events` 601 行 |
| 多重附魔三件套 | 1,537 行（28%） | `BuiltInRepeatableEnchantments{,.Ui,.Hooks}.cs` 871 + `EnchantmentCompositionAdapter.cs` 264 + `SponsorCompositeEnchantment.cs` 402 |
| 手工 `harmony.Patch` | 21 个补丁方法 / 20 个目标 | 属性式 `[HarmonyPatch]` 0；prefix 13 / postfix 8 |
| 可跳过原方法的 bool 前缀 | 8 | `EnchantmentModel.CanEnchant`、`CardCmd.Enchant`、`Goopy.AfterCardPlayed`、`NCard.UpdateEnchantmentVisuals`、`NEnchantPreview.Init`、`CloneRestSiteOption.OnSelect`、`Hook.ModifyCardBeingAddedToDeck`、`ForgeCmd.Forge` |
| 打在全游戏中枢上的目标 | 11 | `Hook.AfterCombatEnd`、`Hook.ModifyCardBeingAddedToDeck`、`EnchantmentModel.CanEnchant`、`CardCmd.Enchant`、`RunState/CombatState.IterateHookListeners`、`Creature.SetCurrentHpInternal`、`EventModel.CreateInitialPortrait`、`RunManager.ProceedFromTerminalRewardsScreen`、`OrbCmd.Channel`、`ForgeCmd.Forge` |
| Harmony id | 9 | `…RepeatableEnchantments`、`….Ui`、`….RestSite`、`…EntropyEnchantments`、`…AbyssalContract`、`…IntegratedStrategyEventsCompat`、`…MiracleTrigger`、`…MiracleForgePrice`、`…MiracleEventPortrait` |
| 原版私有成员反射 | 12 处 | `NCard` 3 字段、`NEnchantPreview` 2 字段 + 1 方法、`NCardEnchantVfx` 4 字段、`RestSiteOption.Owner`、`ThievingHopper._stealPriorities` |
| 对其他模组的私有成员反射 | 5 处 | ISE `ProphecyProjectionRelic` 4 个私有属性、本体 internal `HextechForgeShopPriceHelper`（`TypeByName`） |
| `#if` | 3 | 健康；但 csproj 声明 10 个编译目标只发 3 个 |
| `[SavedProperty]` | 17 | 分布在 10 个模型里 |
| 测试 | 11 个用例，寄居在本体 `tests/HextechRunes.Tests/SponsorPackTests.cs` | 补丁清单快照不覆盖拓展包的 9 个 Harmony id |
| 命名空间 | `HextechRunes` 14 文件 / `HextechRunesSponsorPack` 22 文件 | 文件头注释宣称"冻结"，理由经核实不成立（§3 P11） |
| 版本号 | manifest 0.9.1 / csproj 0.9.1 / `ModInfo.Version` 0.8.7 | 三处手写，已漂移 |
| 加载器 | 663 行，本体加载器的拷贝 | 缺本体 0.9.2 的"三级回退只走一条"修复 |
| 本地化 | 9 语言 × 190 键 | 键集 100% 对齐 |

每次调用都执行的隐性成本：`IsExternalMultiEnchantmentProviderActive()` 在 `CanEnchant`、`Enchant`、`HoverTips`、`GetDescriptionForPile`、`GetDescriptionForUpgradePreview`、`UpdateEnchantmentVisuals`、`NEnchantPreview.Init`、`NCardEnchantVfx._Ready`、`CloneRestSiteOption.OnSelect` 九个补丁体里各枚举一遍 `AppDomain.CurrentDomain.GetAssemblies()`；`UpdateEnchantmentVisualsPrefix` 还对每张卡每次刷新都遍历父节点子树清理"额外附魔标签"。0.9.1 更新日志里"优化多附魔洗牌性能"修的是这条路的症状，不是原因。

## 2. 多重附魔专项

### 2.1 现在的实现

三层，全部由拓展包自己维护：

- **数据模型**：`SponsorCompositeEnchantment : EnchantmentModel`，是 ModelDb 里一个真实模型（ID `SPONSOR_COMPOSITE_ENCHANTMENT`）。它把 N 个内层附魔各自 `ToSerializable()` 后序列化成一个 JSON 字符串，塞进唯一的 `[SavedProperty] SavedEnchantmentsJson`；对外把**自己**放进 `card.Enchantment` 槽位，内层附魔通过 `EnsureInnerBindings` 在每次属性访问时反复 `ClearInternal/ApplyInternal` 重绑到卡上。
- **接管点**：12 个 Harmony 补丁 + 1 处反射覆写原版私有静态数组。
  - `EnchantmentModel.CanEnchant` 跳过型前缀：整段复制原版 `CanEnchant`（0.111 第 278–299 行），只把最后一句 `card.Enchantment != null && …` 换成自己的叠加规则。
  - `CardCmd.Enchant` 跳过型前缀：重写整个附魔流程（`ApplyEnchantmentToCard`：空槽位直接附魔 / 同类叠层 / 转复合再叠）。
  - `RunState.IterateHookListeners`、`CombatState.IterateHookListeners` 后缀：把复合展开成内层附魔，让 `Hook.*` 分发看得见它们。
  - `Goopy.AfterCardPlayed` 跳过型前缀：原版 `base.Card.DeckVersion.Enchantment.Amount++` 在复合下会把"内层数量"当层数加，只能整段重写。
  - `EnchantmentModel.HoverTips`、`CardModel.GetDescriptionForPile/GetDescriptionForUpgradePreview` 后缀：把内层的提示与卡面文本拼回去。
  - `NCard.UpdateEnchantmentVisuals` 跳过型前缀（复制原版 + `Duplicate()` 附魔标签节点做第 2…N 个）、`NEnchantPreview.Init` 跳过型前缀（复制原版）、`NCardEnchantVfx._Ready` 后缀。
  - `CloneRestSiteOption.OnSelect` 跳过型前缀：原版 `c.Enchantment is Clone` 在复合下失明，整段重写。
  - `ThievingHopper._stealPriorities`：反射拿到原版私有 `Func<CardModel,bool>[4]`，就地替换 4 个 lambda（原版 `!(c.Enchantment is Imbued)` 在复合下失明）。
- **外部提供者探测**：`IsExternalMultiEnchantmentProviderActive()` 在每次调用时枚举 AppDomain 程序集找 `MultiEnchantmentMod` 或早已下线的自家独立模组 `RepeatableEnchantments`；`EnchantmentCompositionAdapter` 用反射桥接 MEM 的 `MultiEnchantmentApi.GetEnchantment`、旧版 `MultiEnchantmentSupport.GetEnchantments`、旧独立模组的 `RepeatableCompositeEnchantment.FindEnchantment/ContainsEnchantmentType` 三条路径。

### 2.2 为什么会和 PengoTarot（以及任何附魔模组）冲突

四个机制，按严重度排序。

**(a) 槽位被偷换，别人的 `is` 检查全部失明。** 原版语义里 `card.Enchantment` 就是"这张卡的附魔"，全游戏与所有模组都按 `card.Enchantment is X` 读。复合附魔把槽位换成 `SponsorCompositeEnchantment` 之后：

- PengoTarot 反编译共 **100 处**直接读 `.Enchantment`，一处也没走它自己的 `MultiEnchantmentHelper`（该 helper 0 个调用点）。典型：`PlanetCeres/Pluto/Eris` 扫 `AllCards` 找同类（`c.Enchantment is PlanetCeresEnchantment`）、`TarStarReversed` 的 4 个 X 费补丁（`__instance.Enchantment is TarStarReversedEnchantment { IsSwappedToStarX: … }`）、`TarDeathReversed` 的抽牌补丁、`TarLovers` 成对判定、`TarFool/Judgement/Devil`、手牌行星图标（`HandCardHolder_EnchantmentIconPatch.SetIndexLabel_Postfix`）。只要一张带 Pengo 附魔的牌在附魔大师下叠了第二个附魔，或者 Pengo 附魔叠到一张已附魔的牌上，这个附魔对 Pengo 自己就"不存在"了，而卡面上还画着它。
- 原版同类读取点，拓展包补了 3 处（`ThievingHopper`、`CloneRestSiteOption`、`Goopy`）。MEM 的补丁表证明原版至少还有 `SovereignBlade.AfterTransformedFrom`、`Claws.CreateMaulFromOriginal`、`Slither.AfterCardDrawn`、`SlumberingEssence.BeforeFlush`、`MysticLighter.ModifyDamageAdditive`、`Spiral/Glam.EnchantPlayCount`、`Imbued.AfterAutoPrePlayPhaseEntered`、`NDeckHistoryEntry.Reload`、`NMapPointHistoryHoverTip.PopulateRewardAndSkippedEntries` 十余处对附魔类型敏感，拓展包一处没管。每补一处就多一个补丁，这条路没有尽头。

**(b) 两个跳过型前缀抢同一中枢。** `EnchantmentModel.CanEnchant` 上，拓展包的 `CanEnchantPrefix` 与 PengoTarot 的 `EnchantmentModel_CanEnchant_TowerPatch.Prefix` 都是默认优先级的 bool 前缀，谁先安装谁赢：拓展包先跑且玩家持有附魔大师时，Pengo 的"塔层允许附魔升天者之祸"被静默吞掉；Pengo 先跑时对升天者之祸直接 `__result = true` 跳过拓展包的叠加规则，随后 `CardCmd.Enchant` 前缀里的 `ApplyEnchantmentToCard` 把一张带附魔的诅咒牌转成复合。PengoTarot 有 6 处 `CardCmd.Enchant` 调用（塔罗效果执行器、观星篝火选项），全部落进拓展包重写的流程。

**(c) 与 MEM 的存储模型相反，所以 MEM 的兼容层帮不上忙。** MEM 的做法是：第一个附魔留在原版 `card.Enchantment` 槽位，多余的放旁车（`ConditionalWeakTable<CardModel,…>` + `SerializableCard.Props`），再用 `MultiEnchantmentIsCheckRewriter` 在 `ModManager.Initialized` 后扫描**所有模组**的方法体，把 `card.Enchantment is X` 模式改写成走它的查询（manifest `rewriteIsChecks: true`）。这才是 PengoTarot 在多附魔下能正常工作的真正原因。拓展包在 MEM 在场时退让（正确），MEM 不在场时用一套相反的模型自己顶上，于是"装 MEM 正常、不装 MEM 打架"是结构性的。0.9.1 更新日志说的"兼容 MultiEnchantmentMod"，实质是退让，不是集成。

**(d) 复制原版逻辑没有守卫。** `CanEnchantPrefix`、`UpdateEnchantmentVisualsPrefix`、`EnchantPreviewInitPrefix` 三处整段复制了原版方法体，没有 IL 指纹守卫；游戏更新改了这三处原版逻辑，拓展包会静默走旧逻辑。本体 0.9.2 已经给同类补丁上了 `VanillaCopyGuard`，拓展包没有。

### 2.3 四条路线与裁决

| | A. 委托 MEM | B. 自建"旁车"模型 | C. 维持现状继续补 | **D. 重做效果** |
|---|---|---|---|---|
| 附魔中枢补丁 | 0 | ≥8 | 12 + 持续增加 | **0** |
| 对 MEM 的关系 | 硬门控（`min_game_version` 0.111.0，旧版本附魔大师不入池） | 互斥 | 退让 | **无关**：MEM 只改变"合法"的定义 |
| 对 PengoTarot 类模组 | 由 MEM 负责 | 首个附魔可见、其余失明 | 首个附魔也失明 | **纯调用方**：只调它们的 `CanEnchant` / `CardCmd.Enchant` |
| 符文的独立价值 | 与 MEM 重叠（MEM 本身就让所有人能多重附魔） | 有 | 有 | **有**，且不依赖任何第三方 |
| 存档 / 联机 | 删复合模型 ID，需一次迁移 | 同左 | 不变 | 同 A |
| 维护成本 | 一个桥 | 自己维护 MEM 子集 | 每个新附魔模组一轮补丁 | **一个效果类** |

**裁决：D（2026-09-03 用户定案）。** A 最初被推荐，但它有一个设计上的漏洞：MEM 本身就让所有玩家能多重附魔，附魔大师"可以给同一张牌附魔多重效果"这句在 MEM 在场时是废话，在 MEM 不在场时是谎话——符文的强度一环整个消失。D 把"多重"这个机制性承诺换成一个持续性收益，符文在任何环境都有独立价值，而且拓展包彻底退出"附魔引擎"这个角色，连桥都不需要。

新效果（棱彩，文案定稿）：

> 附魔大师：获得 1 个随机棱彩附魔锻造器，获得 2 个随机黄金附魔锻造器。战斗结束时，为牌组内的随机牌添加随机合法附魔。

### 2.4 D 的具体形态

- **符文**：`EnchantmentMasterRune.AfterObtained` 保持现状（`ObtainRandomForges` 棱彩 1 / 黄金 2）；`IsAvailableForPlayer` 保持 `true`；新增 `AfterCombatVictory(CombatRoom)` 覆写：从牌组里选一张"至少存在一个合法附魔"的牌，再从该牌的合法附魔里选一个，`CardCmd.Enchant(canonical.ToMutable(), card, 1m)` + `Flash()` + `CardCmd.Preview(card)`。每场胜利一次，不设总上限，精英 / Boss 不加倍（数值先按文案最简形态，观察遥测后再调）。
- **候选池**（`src/Features/EnchantmentMaster/RandomEnchantmentPool.cs`）：枚举 ModelDb 里全部 `EnchantmentModel` 的 canonical 实例，按 `Id.Entry` 有序（保证两端顺序一致），逐张牌过 `enchantment.CanEnchant(card)`。排除规则：
  1. `DeprecatedEnchantment`、`Corrupted`（原版负面）、`Clone`（无篝火选项时是空效果）；
  2. 基类链里有 `MultiEnchantmentMod.Api.MarkerEnchantmentModel` 的（MEM 的标记，不是附魔；按 `FullName` 字符串比对，不引用 MEM）；
  3. `IconPath == EnchantmentModel.MissingIconPath` 的（没图标的多半是模组内部用的伴随附魔）；
  4. 类型名以 `SubEnchantment` 结尾的（PengoTarot 的伴随附魔命名约定，它自己也按名字判定）；
  5. 拓展包自己的 `SponsorCompositeEnchantment` 迁移壳。
  排除表放在一个静态只读集合里，日志在首次构建池时列出"进池 N 个 / 排除 M 个"各一行。
- **随机源**：不碰共享 RNG。用运行种子稳定哈希（与 `MiracleEvent.StableRoll` 同款算法，抽成 `src/Features/Shared/SponsorStableRandom.cs` 供两处共用），盐 = `"enchantment-master" | 持有者 NetId | TotalFloor | 步骤名`。`AfterCombatVictory` 在所有客户端对称执行，池子按 Id 有序，两端能连上就意味着模组集合一致，因此结果对称。
- **"合法"的定义权不在拓展包**：没装 MEM，`CanEnchant` 只放行空牌与原版 `IsStackable` 的同类叠层（Sharp / Nimble 这类会持续加层）；装了 MEM，它的 `CanEnchant` 后缀放宽规则，已附魔的牌会被再次抽中，效果自然叠加。两种环境下的行为差异由 MEM 定义，拓展包不解释。
- **删除**：`BuiltInRepeatableEnchantments{,.Ui,.Hooks}.cs`、`EnchantmentCompositionAdapter.cs`、`RepeatableEnchantmentAccessPolicy.cs`、`SponsorCompositeEnchantment.cs`（换成 §2.5 的迁移壳）、`ModEntry` 里对应的 `RegisterSavedPropertyCarrier`/`RegisterEnchantmentIcon`。
- **原先走适配器的三处查询直接写 `card.Enchantment is X`**：`ArcaneForge.HasCloneEnchantment`、`Evolution.AfterCardPlayed` 与 `EntropyDecrease.AfterCardPlayed` 里对 `DeckVersion` 的查找。没装 MEM 时 `card.Enchantment` 就是唯一附魔；装了 MEM 时它的 IL 重写器会把这种写法改成走它的查询（它扫的是 `ModManager.Mods` 全部程序集）。退化情况只有"MEM 在场且重写器对某处没命中"，后果是进化在牌组版本上的计数漏一次，不致命。
- 附魔大师的 `GoldEnchantmentForgeTypes/PrismaticEnchantmentForgeTypes` 不变；本地化 9 语言的 `ENCHANTMENT_MASTER_RUNE.description` 按新文案重写，flavor 不动。

### 2.5 存档与联机迁移

- `SponsorCompositeEnchantment` 是 ModelDb 里的模型。删除它 = 模型 ID 集合变化 = `ModelIdSerializationCache.Hash` 变化 = 与旧版本联机必然 ModMismatch。这与任何一次内容增删相同，随版本号 0.10.0 走，更新日志【注意】写明。
- 旧存档：`EnchantmentModel.FromSerializable` 走 `SaveUtil.EnchantmentOrDeprecated`，未知 ID 落到 `DeprecatedEnchantment`（0.111 `SaveUtil` 第 89–92 行），**不会崩**，但内层附魔全部丢失。处理：保留 `SponsorCompositeEnchantment` 类一个版本周期作"只读迁移壳"（≤60 行，无 Harmony）：`CanEnchant` 恒 false、不注册图标、不进候选池；`OnEnchant` 时把 JSON 里的第一个内层附魔反序列化后用 `card.ClearEnchantmentInternal()` + `card.EnchantInternal` + `ModifyCard()` 放回槽位，其余丢弃并 Warn 一行"其余附魔已丢失"。不再有"MEM 在场就塞回去"的分支。下一个版本删类。
- 联机：拓展包不再有任何附魔序列化补丁；`SavedEnchantmentsJson` 这条全包最大的字符串 `[SavedProperty]` 随迁移壳保留一个版本周期，之后从本体 `tests/HextechRunes.Tests/saved_property_manifest.txt` 里去掉（测试工程引用了拓展包，快照必须同步更新）。

### 2.6 备案：保底方案 B（不采用）

若日后要在没有 MEM 的环境也做"同一张牌多重附魔"：改成 MEM 同构的旁车模型——`card.Enchantment` 永远是第一个附魔，第 2…N 个放进一个从不实例化的载体类的 `[SavedProperty]`（走 `SerializableCard.Props`，本体已有 `sts2-percard-persistent-data` 套路）+ `ConditionalWeakTable<CardModel, List<EnchantmentModel>>` 运行期缓存。补丁最少 8 个，仍与 MEM 互斥，仍对第 2…N 个附魔失明。这是把 MEM 的 1/10 重做一遍，只有在 MEM 停更且这个机制必须存在时才值得。D 路线下没有这个需求。

## 3. 其余问题清单（按冲突面排序）

### P2 打在全局分发点上的熵附魔补丁
`EntropyEnchantmentHooks` 对 `Hook.AfterCombatEnd` 打 prefix+postfix、对 `Hook.ModifyCardBeingAddedToDeck` 打跳过型前缀（用 `AsyncLocal` 计数器在 `CardCmd.Transform` 期间把**所有模组**的入牌组修饰一并压掉）。

- 熵减的"战斗结束后移出牌组"：`EnchantmentModel` 本身就是 `AbstractModel`，牌组卡的附魔在 `RunState.IterateHookListeners` 里（0.111 第 559–566 行），所以 `EntropyDecrease` 可以直接覆写 `AfterCombatEnd(CombatRoom)`（核验：`AbstractModel.AfterCombatEnd(CombatRoom)` 存在）。要"一次预览批量删除"，让**第一个**被回调的熵减实例扫 `Owner.Deck.Cards` 把所有 `PendingRemoval` 的卡一次 `CardPileCmd.RemoveFromDeck(list, showPreview: true)`，后续实例看到自己已不在牌组就 no-op。不需要任何 Harmony。
- 熵增的"变化时抑制入牌组修饰"：原版 `CardCmd.Transform` 本来就对替换卡调 `Hook.ModifyCardBeingAddedToDeck`（0.111 `CardCmd` 第 437 行附近），拓展包压掉它是在改变其他模组在"变化"这件事上的既有行为。真正要防的只有递归（新卡入牌组触发其他熵增卡再变化），这用 `AfterCardChangedPiles` 里现有的 `IsTransformingEntropyCard` 守卫即可（改成 `AsyncLocal<bool>` 而不是 Harmony），`ModifyCardBeingAddedToDeck` 前缀整个删掉。行为差异：变化出的复制品会像原版任何一次变化一样接受入牌组修饰。这是回归原版语义，接受。

### P3 借道自家父模组与兄弟模组的私有成员
- `MiracleEventForgePricePatch`：`AccessTools.TypeByName("HextechRunes.HextechForgeShopPriceHelper")` 找本体 internal 类再 postfix。对自家父模组打 Harmony 补丁，比任何第三方冲突都难看，而且 `ModEntry` 头注释因此禁止本体调整层级。应由本体开 API（§6.2）。
- `IntegratedStrategyEventsBridge.ConfigureProjectionChoraleHp`：反射写 ISE `ProphecyProjectionRelic` 的 4 个私有 `[SavedProperty]`；`IntegratedStrategyEventsCompatibilityHooks` 对 `Creature.SetCurrentHpInternal` 打全局前缀，每次任何生物 HP 变化都跑一遍 `IsFinalChorale` 判定。ISE 是同一工作区的自家模组，且已经有公开的 `IntegratedStrategyEventsInterop`（本体 `HextechIntegratedStrategyEventsCompat` 就在用）。应由 ISE 开 API（§6.3），拓展包删掉全部反射与该前缀。

### P4 加载器落后本体一轮
`loader/LoaderBootstrap.cs` 第 135 行 `InstallReflectionBridge()` 无条件执行，之后第 204–208 行又调 0.108+ 的 `AssociateAssemblyWithMod`。本体 0.9.2 已把它改成"三级回退只走一条，①② 成功就不装 `ReflectionHelper.ModTypes` 后缀"，并因此删掉了压假警告的 `Log.Warn` 补丁。拓展包没跟上：同一批类型可能被贡献两次，"Two AbstractModels X and X share an ID" 会在拓展包的 40 余个模型上各报一次（本体的压警告补丁已删，这些警告现在会直接出现在玩家日志里；需 headless 复核）。根治是**共源**：拓展包 loader csproj 链接本体 `loader/*.cs`，`ModId`/变体清单名/元数据键三个常量改由 MSBuild 属性注入。但这要改本体 loader（参数化常量），归入 §6 本轮不动。本轮做两件事：把本体的单路径修复原样移植到拓展包 loader（只改 §"三级回退"那一段），并在 `tools/build_and_deploy.sh` 里加一道构建期漂移检查（两份 loader 去掉三个常量后 `diff` 必须为空，否则构建失败），让"落后一轮"不再静默发生。

### P5 前置检测的失败模式
按程序集名探测 `HextechRunes` + `AssemblyLoad` 延迟注册是既定决策（兼容二创版，见 `sts2-sponsorpack-event-and-fork-compat`），保留。但延迟路径有一个未处理的失败模式：若本体在 `ModelDb.Init` 之后才加载，`RegisterSavedPropertyCarrier` 会抛 `InvalidOperationException`（本体 API 文档写明"官方序列化缓存已初始化则抛出"），异常从 `AssemblyLoad` 事件处理器里冒出去，`_registered` 保持 false，`_contentRegistered` 可能半真半假。补两条：延迟路径先检查 `ModManager.State`/模型初始化窗口是否已关闭，关闭则 `Log.Warn` 并跳过内容注册（符文不入池比崩溃好）；`RegisterAll` 的每一步独立 try/catch 并汇报，而不是只包 optional feature。原版 `ModManager` 按 manifest `dependencies` 做拓扑排序（0.111 第 184–290 行），如果哪天放弃二创兼容，声明 `dependencies: [{"id": "HextechRunes"}]` 就能整个删掉这套探测。

### P6 深渊契约 god class
`AbyssalContractRune.cs` 573 行，5 种契约的初始化、战斗开始、胜利、出牌、回合开始、回合结束、入牌组拦截、费用修改全部在同一个类里按 `_contract` switch。拆成 `IAbyssalContract`（Warrior / Hunter / Regent / Necrobinder / Automaton 各一个无状态策略类），符文只持有 `[SavedProperty]` 与分发；`AbyssalContractHooks` 的两个补丁跟着搬进 Regent / Automaton 各自的文件。附带两处小问题：`ApplyImbuedEnchantment` 直接 `card.EnchantInternal`，绕过 `CardCmd.Enchant`，既不记附魔历史也绕过 MEM，改 `CardCmd.Enchant`；Automaton 的 `+99` 珠槽经核实 `SerializablePlayer.BaseOrbSlotCount` 是 16 位序列化，联机安全，但 `OrbQueue.maxCapacity = 10` 这个常量的 UI 用途需要跑一次实机确认布局不溢出。

### P7 过时的"错发分支"防御
`GoldStarRelic.RollPotionReward` 用反射按参数个数自适应 `PotionRewardOdds.Roll` 的 2/3 参签名，注释说是防 Workshop 错发分支构建。现在是单物品多变体包，加载器按游戏版本选 DLL，这个前提已经不存在。改成 `#if STS2_107_1` 分部文件，与本体 `HextechEnemyCuttingEdgeAlchemistHooks` 同款。

### P8 编译目标与版本号
csproj 声明 0.104.0 → 0.111.0 共 10 个目标、8 个 `STS2_*` 符号，只发 3 个；`ModInfo.Version = "0.8.7"` 与 manifest/csproj 的 0.9.1 漂移。目标砍到 3 个（同本体阶段 0），`ModInfo.Version` 改由 `AssemblyInformationalVersion` 读取或直接删除（全包只有日志在用）。

### P9 补丁基础设施缺席
9 个 Harmony id、每组自己 `RequireMethod/RequireField`、失败时各自 `UnpatchAll` 自己那个 id、`InstallOptionalFeature` 只包了两组。本体已经有 `HextechPatcher`（属性式 + 逐条汇报 + 共享目标告警 + `VanillaCopyGuard` + 补丁清单快照），拓展包应直接复用（§6.1），单 Harmony id `Natsuki.HextechRunesSponsorPack`。

### P10 死代码与噪声
`BuiltInRepeatableEnchantments.VerboseLog` 是 `static readonly bool = false`，约 20 处 `DebugLog` 永远不跑（随 §2 一起消失）；日志前缀三种写法（`[HextechRunesSponsorPack][RepeatableEnchantments]`、`[{ModInfo.Id}]`、`[HextechRunesSponsorPack][GoldStar]`）。

### P11 "命名空间冻结"经核实不成立
`ModEntry.cs` 头注释称类名与命名空间参与本地化键与 ModelId。核验：`ModelDb.GetEntry(Type) => StringHelper.Slugify(type.Name)`（0.111 第 540–543 行），`EnchantmentModel.Title => new LocString("enchantments", Id.Entry + ".title")`，`ModelIdSerializationCache` 用 `ContentSorter<ModelId>` 按 ModelId 排模型、`CompareOrdinal(p1.Name, p2.Name)` 排属性（第 77、196 行）。命名空间不参与任何持久化身份。14 个 `namespace HextechRunes;` 文件可归位到 `HextechRunesSponsorPack`，条件只有两条：类名不动、`[SavedProperty]` 属性名不动。`DollysMirrorForge.IsHextechType` 已按程序集名判断，不受影响；本体 `TypeByName("HextechRunes.HextechForgeShopPriceHelper")` 那条是拓展包找本体，随 §6.2 一起消失。归位后 `ModEntry` 头注释整段删除。

### P12 交付元数据
`workshop/workshop.json` 仍带 `minBranch: public / maxBranch: public-beta`；本体 0.9.2 起按现行政策不关联游戏分支，拓展包下次上传前清掉这两项。

## 4. 目标架构

```
loader/                 本体 loader 的同步副本（构建期漂移检查守着；共源归 §6）
src/
  ModEntry.cs           ≤40 行：Prerequisite.Resolve() → SponsorCatalog.Register() → SponsorPatcher.ApplyAll()
  Content/              SponsorCatalog：符文 / 锻造器 / 事件遗物 / 附魔图标一张表，ModEntry 只遍历
  Patching/             SponsorPatcher + [SponsorPatch(id, feature, Rune=, Optional=)]：本体 HextechPatcher 的最小同构副本
                        （属性式 + 逐条汇报 + 单 Harmony id；§6.1 落地后换成直接复用本体）
  Bridges/              IntegratedStrategyEventsBridge（现状保留，§6.3 后改走 ISE Interop）
  Compat/               唯一允许 #if 的目录：HextechSts2ApiCompat.cs、GoldStar 的 Roll 签名分部文件
  Features/
    Shared/             SponsorStableRandom（运行种子稳定哈希，神迹与附魔大师共用）
    EnchantmentMaster/  Rune（AfterCombatVictory 随机合法附魔）、RandomEnchantmentPool、CompositeMigrationShell（一个版本周期后删）
    Entropy/            EntropyIncrease、EntropyDecrease（自带 AfterCombatEnd）、EntropyForge、ChoiceRelics
    Evolution/          Evolution、EvolutionForge
    AbyssalContract/    Rune、Contracts/{Warrior,Hunter,Regent,Necrobinder,Automaton}.cs、ChoiceRelics、Patches（Forge / Channel）
    Believer/           Rune、MiracleEvent、TriggerPatch、PortraitPatch、ForgePricePatch（§6.2 后删）
    DollysMirror/  Arcane/  Basic/  Mystic/  Regret/  Cosplay/  StarlightSparkle/  Gastritis/  Otter/  DesperateFinale/
```

命名空间统一 `HextechRunesSponsorPack`。依赖方向：`Features → (Bridges | Patching | Compat) → (本体 HextechRunesApi | ISE)`，`Features` 之间不互相引用，任何地方不引用 MEM。

## 5. Harmony 补丁处置清单

处置代码：**删**=官方扩展点 / 自家 API 已覆盖；**改**=保留但改属性式 + 守卫；**退**=随 §2 附魔大师重做退役。

### 5.1 退（随附魔大师重做，12 个补丁 + 1 处反射）

"核验"列写的是 MEM 在同一目标上的做法，用来说明这些目标本来就是多附魔引擎的职责范围，拓展包退出后不留空洞。

| 目标 | 现状 | 核验 |
|---|---|---|
| `EnchantmentModel.CanEnchant` 跳过型前缀 | 复制原版 + 改叠加规则 | MEM 自己在此目标有 `[HarmonyPriority(200)]` **后缀**收紧，不复制原版 |
| `CardCmd.Enchant` 跳过型前缀 | 重写附魔流程 | MEM 在此目标有优先级 200 的前缀，两者互斥 |
| `RunState.IterateHookListeners`、`CombatState.IterateHookListeners` 后缀 | 展开复合 | MEM 同目标后缀追加旁车附魔 |
| `Goopy.AfterCardPlayed` 跳过型前缀 | 复合下层数错乱 | MEM 同目标优先级 200 前缀 |
| `EnchantmentModel.get_HoverTips`、`CardModel.GetDescriptionForPile`、`GetDescriptionForUpgradePreview` 后缀 | 拼内层文本 | 旁车模型下原版自己就能读到首个附魔 |
| `NCard.UpdateEnchantmentVisuals` 跳过型前缀、`NEnchantPreview.Init` 跳过型前缀、`NCardEnchantVfx._Ready` 后缀 | 复制原版 UI | MEM 同三个目标（`UpdateEnchantmentVisuals` 用后缀） |
| `CloneRestSiteOption.OnSelect` 跳过型前缀 | `is Clone` 失明 | MEM 同目标 |
| `ThievingHopper._stealPriorities` 反射覆写 | `is Imbued` 失明 | 旁车模型下首个附魔可见，无需处理 |

### 5.2 删（官方扩展点 / 自家 API 已覆盖）

| 目标 | 现状 | 替代 | 核验 |
|---|---|---|---|
| `Hook.AfterCombatEnd` prefix+postfix | 收集熵减待删卡、战后批量删除 | `EntropyDecrease.AfterCombatEnd(CombatRoom)` 覆写 + 首个实例批量删除 | `AbstractModel.AfterCombatEnd(CombatRoom)` 存在；牌组附魔在 `RunState.IterateHookListeners`（0.111 第 562–565 行）✔ |
| `Hook.ModifyCardBeingAddedToDeck` 跳过型前缀 | 变化期间压掉所有入牌组修饰 | 删除；递归守卫留在 `EntropyIncrease.AfterCardChangedPiles` 的 `AsyncLocal<bool>` | 原版 `CardCmd.Transform` 本就对替换卡调该 Hook（0.111 `CardCmd` 第 437 行附近）✔ |
| `Creature.SetCurrentHpInternal` 前缀 | 合唱团 HP 可超上限 | ISE Interop（§6.3）；HP 超上限的规则属于 ISE 的 FinalChorale 自己 | **本轮不动**，先改属性式 |
| `HextechForgeShopPriceHelper.GetRandomForgeShopPriceFor` 后缀（本体 internal） | 叠加信徒的售价修正 | 本体 API（§6.2） | **本轮不动**，先改属性式（`Optional = true`，目标在本体运行时枚举） |

### 5.3 改（保留，属性式 + 守卫，本轮 7 个 / §6 后 4 个，单 Harmony id）

| 目标 | 形态 | 处置 |
|---|---|---|
| `ForgeCmd.Forge` bool 前缀 | 门控型（摄政契约：非击剑手册来源的锻造直接返回空），不复制原版 | 原版只有 `Hook.AfterForge`（0.111 `ForgeCmd` 第 56 行），无事前拦截口子，保留；`[HextechPatch("abyssal.regent-forge", …, Rune = typeof(AbyssalContractRune))]`；不进 VanillaCopyGuard（不含原版逻辑） |
| `OrbCmd.Channel` 参数改写前缀 | 非跳过（自动机契约：所有珠替换为闪电） | 原版无 `ModifyOrbBeingChanneled`，保留；`Rune = typeof(AbyssalContractRune)` |
| `RunManager.ProceedFromTerminalRewardsScreen` 后缀 | 神迹注入点 | 保留；有充分踩坑记录（见文件头注释），`Rune = typeof(BelieverRune)` |
| `EventModel.CreateInitialPortrait` 后缀 | 神迹立绘 | `InitialPortraitPath` 是 private 非虚（0.111 第 203 行），无覆写口子；保留，`Optional = true`。不要把 PNG 塞到 `res://images/events/` 去撞原版命名空间 |

## 6. 需要本体 / ISE 配合开的口子（本轮不动，用户裁决 2026-09-03）

本轮不改本体与 ISE 的任何源码，下面是留给后续的小 PR 清单。本轮受此影响的处理：拓展包自带一个 `SponsorPatcher`（本体 `HextechPatcher` 的最小同构副本，≤120 行，§6.1 落地后删掉换成复用）；`MiracleEventForgePricePatch`、`IntegratedStrategyEventsBridge` 的反射与 `SetCurrentHpInternal` 前缀原样保留，只改成属性式声明。唯一会碰本体目录的是 `tests/HextechRunes.Tests/`：它引用了拓展包工程，`SponsorPackTests.cs` 与 `saved_property_manifest.txt` 必须随删除的类型同步，否则测试工程编不过——这是测试同步，不是开口子。

1. **本体：补丁基础设施对拓展包可见。** `HextechPatcher` / `HextechPatchAttribute` / `HextechVanillaCopyGuard` 是 internal。最省事的一行是 `[assembly: InternalsVisibleTo("HextechRunesSponsorPack")]`（测试工程已有先例）；更规矩的是公开 `HextechRunesApi.ApplyPatches(Harmony, Assembly)` 并把属性类公开。补丁清单快照测试要能按第二个 Harmony id 生成 `patch_manifest.sponsor.<target>.txt`。
2. **本体：锻造器售价修正 API。** `HextechRunesApi.RegisterForgeShopPriceModifier(Func<RunState, int> delta)` 或让遗物实现 `IHextechForgePriceModifier`，`HextechForgeShopPriceHelper` 内部汇总。落地后拓展包删 `MiracleEventForgePricePatch`，本体解除"不能改层级"的约束。
3. **ISE：`IntegratedStrategyEventsInterop` 增加** `IsFinalChorale(Creature)`、`GrantProphecyProjection(Player, int? scaledChoraleHp)`（内部完成移除旧投影 + 配置 HP + `RelicCmd.Obtain`），并把"合唱团当前 HP 允许超过 MaxHp 时抬 MaxHp"的规则放进 ISE 的 FinalChorale 或投影遗物自己。落地后拓展包删 `IntegratedStrategyEventsBridge` 的 4 处私有属性反射、`SetNonPublicProperty`、`IntegratedStrategyEventsCompatibilityHooks` 整个文件。
4. **本体（随 §2）：** `saved_property_manifest.txt` 去掉 `SavedEnchantmentsJson`；`SponsorPackTests.cs` 里 `EnchantmentCompositionAdapterFindsSponsorCompositeEnchantments`、`SponsorCompositeExpandsInnerHookListeners` 两个用例改为桥测试（有 MEM / 无 MEM 两种退化路径）。

## 7. 分阶段执行

每阶段一个提交，合并门槛沿用本体：`bash HextechRunes/tools/run_tests.sh` 三目标全绿、三个变体 Release 0 warning、headless 加载确认行（`[HextechRunesSponsorPack] Loaded and registered …`）、补丁目标集合 diff 与预期一致。

### 阶段 0：零风险清理
- csproj 目标 10 → 3，`ModInfo.Version` 去手写（改读程序集版本或删除）。
- `GoldStar.Roll` 反射 → `#if STS2_107_1` 分部文件。
- 命名空间归位（14 文件），类名不动。验证：反编译 diff 只差命名空间，`saved_property_manifest.txt` 无 diff。
- 加载器移植本体单路径修复 + 构建期漂移检查（P4 本轮版）。验证：headless 日志无 "share an ID"，`hextech-runes-sponsor-pack-variants.manifest` 与 `dist/lib/*` 结构不变。
- `workshop.json` 清 `minBranch/maxBranch`。

### 阶段 1：补丁基础设施（行为不变）
- 新建 `src/Patching/SponsorPatcher.cs` + `SponsorPatchAttribute.cs`（本体同构：属性式、逐条汇报、`Optional`、`Rune`、动态目标用 `static void Apply(Harmony)`）。
- 21 个补丁机械转属性类，单 Harmony id `Natsuki.HextechRunesSponsorPack`，`ModEntry` 缩到编排；`InstallOptionalFeature` 退役。
- 验证：补丁目标集合逐条相同（用 Harmony `GetAllPatchedMethods` 按 owner 过滤，前后各 dump 一次 diff）。

### 阶段 2：附魔大师重做（核心）
- 新效果类、候选池、稳定随机、迁移壳（§2.4 / §2.5），删 1,537 行；9 语言文案。
- 本体测试工程同步（§6 末段）。
- 验证矩阵：
  - 无 MEM：附魔大师入池；打赢一场后日志出现 `[EnchantmentMaster] Enchanted <card> with <enchantment>`，卡面附魔标签正确；牌组全部附魔后只剩可叠层附魔继续加层，无异常。
  - 有 MEM（0.111）：同一流程，已附魔的牌被再次抽中时由 MEM 叠加，无 `[MultiEnchantment]` Error。
  - 有 MEM + PengoTarot：牌组里有带 Pengo 行星附魔的牌时，附魔大师叠第二个附魔后行星效果与手牌图标仍触发。这是本轮的验收标准。
  - 旧存档（含复合附魔）读一次，看迁移壳日志与卡面。
- 版本 0.10.0，更新日志【注意】：与 0.9.x 不能联机；带复合附魔的旧存档只保留第一个附魔。

#### 更新日志草稿（0.10.0）

```
海斗拓展包0.10.0更新日志：
0.10.0同时适配游戏0.111.0、0.110.0和0.107.1；

【优化】
重做了附魔大师：不再改写附魔规则，改为每场战斗结束时为牌组内的随机牌添加一个随机的合法附魔，锻造器奖励不变；
附魔大师现在把「哪些附魔合法」交给游戏和其他附魔模组判断，装不装多重附魔类模组都能正常工作；
移除了拓展包自带的多重附魔实现，与卡牌附魔相关的改动位置从十余处降到零，与其他附魔模组的冲突面大幅收窄；
优化了附魔相关的判定与刷新流程，减少了每次卡牌显示时的额外开销；

【修复】
修复了在未安装多重附魔类模组时，已附魔的牌再被附魔后，其他模组看不到该附魔、卡面效果不触发的问题；
修复了复制、篝火、偷牌等原版流程在多重附魔下识别错误附魔的问题；

【注意】
本版本与0.9.x无法联机，请与好友同时更新到0.10.0；
0.9.x存档中带有多个附魔的卡牌，读档后只会保留其中的第一个附魔，其余附魔会丢失；
附魔大师的效果已变更为「战斗结束时，为牌组内的随机牌添加随机合法附魔」，不再提供「同一张牌附魔多重效果」；
```

### 阶段 3：去中枢与拆分
- 熵附魔按 §5.2 改覆写 + 递归守卫。
- 深渊契约拆策略类（P6），`ApplyImbuedEnchantment` 改 `CardCmd.Enchant`。
- 验证：补丁清单剩 7 项；熵减"一次预览批量删除"headless 或实机看一次。

### 阶段 4：垂直切片与收尾
- `Features/` 目录化，纯移动，反编译 diff 验证。
- 前置检测补失败模式（P5）。
- 死代码、日志前缀统一为 `[HextechRunesSponsorPack]`。

## 8. 不要动的东西

- **前置检测按程序集名 + `AssemblyLoad` 延迟注册**：二创版兼容是既定决策，只补失败模式。
- **类名、`[SavedProperty]` 属性名、`RegretRune.SavedDamageBonusPercent` 等遗留哑属性、`MiracleEvent.StableRoll` 的盐与算法**：全部是持久化身份或联机确定性输入。
- **神迹事件的触发点设计**（`ProceedFromTerminalRewardsScreen` 后缀 + `CallDeferred` + `EnterRoomDebug`）：文件头有完整的踩坑记录（战斗胜利 hook 直接 `EnterRoom` 会弹掉领奖房），不要为了"少一个补丁"翻案。
- **`HextechRunesApi` 13 个符号的调用面**：`RegisterPlayerRune / RegisterEventRelic / RegisterForge / SelectRelicOption / RegisterSavedPropertyCarrier / RegisterEnchantmentIcon / ObtainRandomForges / TrackPersistentInnate / IsPersistentInnateTracked / RestorePersistentInnate` 等，本体承诺签名不改。
- **`HextechSts2ApiCompat.cs` 的 global using 别名**与本体 `ModifyDamageMultiplicativeCompat` 适配层：0.108 签名变更的正解。
- **本地化 9 语言**：键集已对齐，只随内容增删同步。

## 9. 与本体方案的差异

本体那份方案（`HextechRunes/docs/refactor-plan-20260902.md`）的主题是"把 199 个补丁收进基础设施、把 4 成补丁迁到官方扩展点"。拓展包的主题不同：**它最大的一块不该存在**。所以本方案的核心动作是"删并重做效果"而不是"迁移"，阶段 2 的验收标准是三方共装（拓展包 + MEM + PengoTarot）冒烟通过，而不是补丁数字。

## 10. 执行进度（2026-09-03）

五次提交，全部在 `dev` 分支：

| 提交 | 阶段 | 内容 |
|---|---|---|
| `205ac76f` | 阶段 0 | 编译目标 10 → 3、版本号改读程序集、命名空间归位 14 文件、加载器移植本体单路径修复并加构建期漂移检查、`workshop.json` 清分支字段 |
| `aebb5a5a` | 阶段 2 | 附魔大师重做为"战斗结束时为牌组内随机牌添加随机合法附魔"，删除自建多重附魔引擎与 12 个附魔中枢补丁，`SponsorCompositeEnchantment` 降为只读迁移壳，9 语言文案 |
| `7f8d5cea` | 阶段 3 | 熵附魔退出 `Hook.AfterCombatEnd` / `Hook.ModifyCardBeingAddedToDeck` 两个全局分发点，改覆写 + `AsyncLocal` 递归守卫；深渊契约拆 5 个策略类 |
| `e8c9f8e9` | 阶段 1 | `SponsorPatcher` + `[SponsorPatch]`（本体 `HextechPatcher` 的最小同构副本），补丁全部改属性式、单 Harmony id，`ModEntry` 只做编排；补丁表 dump 工具 |
| 本提交 | 阶段 4 | `Features/` 垂直切片（纯移动，反编译类型/成员清单 diff 为空）、`Content/SponsorCatalog` 表驱动注册、延迟注册补窗口检查（P5）、日志前缀与死 using 清理 |

### 指标（改前 = 根仓库 `7aca074f`，改后 = 阶段 4 完成时，均为实测）

| 指标 | 改前 | 改后 | 说明 |
|---|---|---|---|
| `src/**/*.cs` 文件数 | 36 | 47 | 垂直切片：一个功能一个目录，拆分不共享文件 |
| `src/**/*.cs` 行数 | 5,523 | 4,606 | −917 行（多重附魔引擎 −1,537，新增效果 / 补丁基础设施 / 目录骨架 +620） |
| 补丁方法数 | 21 | 6 | 改后数值来自 headless 导出的补丁表（`HEXTECH_SPONSOR_DUMP_PATCHES`） |
| 补丁目标方法数 | 20 | 6 | 同上 |
| 可跳过原方法的 bool 前缀 | 8 | 1 | 只剩 `ForgeCmd.Forge`（摄政契约门控，不复制任何原版逻辑） |
| 原版私有成员反射 | 12 | 0 | 剩余反射只有 ISE 的 4 个私有 `[SavedProperty]`（§6.3 未落地）与补丁应用器自身的类型自省 |
| Harmony id | 9 | 1 | `Natsuki.HextechRunesSponsorPack` |
| `namespace` 套数 | 2 | 1 | 全部 `HextechRunesSponsorPack`；目录分层不引入子命名空间，便于反编译比对 |
| `#if` 处数 | 3 | 1 | 唯一一处是 `Compat/GoldStarRelic.Roll.cs`（0.107.1 的 `PotionRewardOdds.Roll` 签名） |
| 拓展包测试用例 | 10 | 10 | 寄居本体 `tests/HextechRunes.Tests/SponsorPackTests.cs`；本体全量 262/262，三编译目标各一遍 |

### 遗留

1. **§6 的三个口子按用户裁决本轮未动**：①本体把补丁基础设施对拓展包可见（落地后删掉拓展包自带的 `SponsorPatcher`）；②本体开锻造器售价 API（落地后删 `MiracleEventForgePricePatch`，本体解除"internal 类不能挪"的约束）；③ISE 开 `IsFinalChorale` / `GrantProphecyProjection` Interop（落地后删 `IntegratedStrategyEventsBridge` 的 4 处私有属性反射与 `Creature.SetCurrentHpInternal` 前缀）。
2. **`SponsorCompositeEnchantment` 迁移壳**：只读，保留一个版本周期供 0.9.x 存档读一次；下个版本删类，并同步本体 `tests/HextechRunes.Tests/saved_property_manifest.txt` 里的 `SavedEnchantmentsJson`。
3. **实机验收未做**：本轮只做到"编译 + 单元测试 + headless 加载 + 补丁表等价"。附魔大师的三张验收清单在 §7 阶段 2（无 MEM / 有 MEM / MEM + PengoTarot / 旧存档读档），深渊契约自动机的 `+99` 珠槽 UI 布局（§3 P6）与熵减"一次预览批量删除"（§7 阶段 3）也仍待实机确认。
4. **加载器仍是本体的副本**：构建期漂移检查守着，共源（`loader/*.cs` 用 MSBuild 属性注入三个常量）归 §6，未做。
