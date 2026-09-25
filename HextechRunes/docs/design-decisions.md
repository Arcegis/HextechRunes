# 设计裁决与约束

本文只收录**代码和 CHANGELOG 里读不出来、但改这块代码的人必须知道**的裁决：为什么选这个扩展点、哪个顺序不能动、哪条边界是踩过坑换来的。过程记录写提交信息，玩法数值看 `CHANGELOG.md`，分层与补丁保留清单看 [架构说明](architecture.md)，日常契约看 [开发规范](development.md)。

新增条目沿用 [条目模板](templates/change-note.md)：一句结论 + 理由 + 涉及的类名。已经被代码自解释的不要往这里搬。

## 玩家符文

- **一呼百应同一批自动打出不会递归启动另一批。** 否则放血回手一类效果会自身无限重入；按模型 ID 匹配同名牌（升级与附魔不影响匹配），先抽牌堆后手牌、排除触发牌自身，目标死亡则按 CombatId 改选存活可命中的敌人。`RallyingCallRune`
- **轮转不息的辉星优惠必须用临时费用 + `TryModifyStarCost` 延续。** 原版辉星 ThisTurn 在打出后会被清理，不延续则"打出再回手"不再免费；能量侧用整回合减费。无每回合次数限制。`EndlessRotationRune`
- **见血封喉是加算进原版攻击伤害，不是额外伤害事件。** 因此照常吃攻击伤害修饰与格挡，预览用同一公式；小刀身份复用项目规则，所以"大号匕首"替换的君王之剑也受益。`VenomousBladeRune`、`HextechKnifeHelper`
- **森罗万象先确认持有者在本次结束回合的 participants 里再冻结球队列。** 避免队友额外回合触发；已离场的球不再触发，新生成的球不扩大本批次。`MyriadManifestationsRune`
- **祸水东引只转移逐个核对过支持敌人 Owner 的 Power 类型。** 原版没有统一的目标适用性接口；Hex、Ringing 等依赖玩家牌堆的效果只从玩家侧移除、不给敌人，否则会在敌人侧访问不存在的 Player 或带入卡牌引用。按实际层数判定类型，负数力量/敏捷也在范围内；转移时清除玩家侧 SkipNextDurationTick。`ScapegoatRune`
- **血债血偿的成长按卡牌实例记在遗物的战斗内字典里。** 牌离开手牌仍保留，同名的另一张不共享，不写回永久牌组、不给之后生成的牌补发；通过伤害加算 Hook 同时作用于预览与实际，不新增伤害事件。`BloodDebtRune`
- **冥土追魂挂在 `AfterSideTurnEndLate`（回合弃牌与虚无消耗之后），并要求持有者属于本次 participants。** 否则队友的额外回合会触发它。`NetherSoulRune`
- **玩家侧活力火花必须走 `PowerCmd.Apply<HextechVitalSparkPower>`，不能把原版敌方增益直接施加到玩家。** 牌上污染层数 = 本玩家模组层数 + 场上原版活力火花总层数，但打出时每个 Power 只施加自己那份，避免重复乘算。原版 `BeforeCombatStart` / `AfterPowerAmountChanged` / `AfterRemoved` 会覆盖或清空侵蚀，普通模型 Hook 保证不了执行在原版写入之后，因此这三处用等待原 Task 的 postfix 重算。`HextechVitalSparkPower`
- **百炼成钢的临时缓慢在官方 `BeforeSideTurnStart` 清理，并按"变化后总量减本次新增量"识别旧层。** 叠层回调会刷新整个实例的 `_appliedRound`，让旧层连续多个回合逃过清理（水银沙漏的回合开始伤害是触发链）。修正依赖 `PowerCmd.ModifyAmount` 的公开契约：先改层数 → 派发 `AfterPowerAmountChanged` → 最后才检查移除零层实例。不为沙漏或冰淇淋写特例。`HextechTemporarySlowPower`
- **我方扇巴掌 / 恶趣味 / 坚若磐石监听"持有者自己收到负面效果"（来源不限），且不限每回合次数；折磨者仍监听"给敌人施加"，同样不限次数。** 去掉上限是为了让同轴海克斯能叠加，而不是拿到第二个就零收益。判定沿用敌方侧的口径：只认层数增加、排除临时属性的包装 Power。`LimitedDebuffProcRelicBase` 的 `SavedProcsThisTurn` 对无上限子类已无用，但它在 SavedProperty 清单里，不能删。`SlapRune`、`BadTasteRune`、`AdamantRune`、`TormentorRune`
- **坚若磐石的格挡一律记到原版 `BlockNextTurnPower`，不当场发放。** 负面效果多半在敌方回合收到，当场给的格挡会在玩家回合开始时被清掉；原版这个能力在 `AfterBlockCleared` 发放（有壁垒时同样触发），自带显示、保存与联机同步，不需要新增待发状态。
- **回归基本功的"无法打出 3 费及以上"只限手动出牌，自动打出一律放行。** 与敌方同名海克斯、卡卡同口径；否则同时持有"升级：XX形态"时，3 费形态牌开局自动打出会被拦下直接进弃牌堆。`BackToBasicsRune`

## 敌方海克斯

- **`MonsterHexKind` 一律尾部追加，不重排旧编号。** 编号是保存与联机契约。
- **敌方蓝烛药箱（玩家状态/诅咒牌耗能 +1）优先级最低，视同加在基础费用上。** 原版费用 = 基础 → 卡牌临时修正 → 常规 Hook → Late Hook，敌方修饰器在监听顺序里排在遗物、能力、卡牌之后。所以 +1 在常规阶段按卡牌临时修正折算后再加（轮转不息一类的本回合 0 费会吃掉它，相对减费照常叠加）；我方蓝烛药箱的 0 费因此移到 Late 阶段，否则会被加回 1；敌方开悟的 1 费下限同在 Late 且排在遗物之后，仍最优先。原版无法打出的状态/诅咒基础费用是 -1，原版 Hook 直接跳过，这类牌不受影响。`BlueCandleMedkitEnemyHex`、`BlueCandleMedkitRune`
- **偷窃草蜢：AsleepPower / SlumberPower 不属于原版 `IsStunned`，必须单独排除。** 计划偷牌和行动结束实际偷牌两处都要排。`ThievingHopperEnemyHex`
- **偷窃草蜢：MinionPower 单位不偷牌逃跑。** 仆从退场常绑在首领的 `AfterDeath` 上（女王的 TorchHeadAmalgam），而 `CreatureCmd.Escape` 不发死亡回调，逃跑会破坏遭遇关系；不能靠改女王或伪造死亡事件绕过。
- **偷窃草蜢：逃跑意图靠 FollowUpState 自循环保留，不要设 `MustPerformOnceBeforeTransitioning`。** 那把锁会让千足虫 ReattachPower 的 `SetMoveImmediate(DeadState)` 失效，挡住复活。
- **偷窃草蜢：每个敌人每场只偷一张，复活不重置次数。** 归还走原版 `SwipePower.BeforeDeath`，逃跑走 `CreatureCmd.Escape` 因而不触发返还。
- **活雾的技能上限在 `BeforeCardPlayed` 计数。** 只有在这里计数才能拦住技能内部的自动打出越过上限；手动与自动共同计数，同一张牌重放不重复占名额。这与原版 SmoggyPower 的"只能打一张 + 迷雾附魔"不同，不施加该附魔。`LivingFogEnemyHex`
- **仪式兽追加的力量在整次行动后结算一次。** 多段伤害不多次加力量；珠光护手实际重复行动时也只结算一次追加效果。
- **扇巴掌 / 折磨者 / 巨像的勇气监听的是"仍存活的敌人实际收到减益"。** 不监听敌人对玩家施加减益或自身增益；减少层数、临时属性的包装 Power 不触发，临时力量到期的自扣也不触发；同一张牌作用于多个敌人时各敌人独立记录。扇巴掌用独立的 `HextechSlapTemporaryStrengthPower` 按原版 TemporaryStrength 生命周期收回。
- **夜狩与狂徒豪气共用抽牌进度算法但各自独立字段。** `NightstalkingPlayerCardsDrawnThisCombat` 同时接入 Tracking 与 Snapshot，避免两者互相消费进度；阈值固定 12，不按联机人数调整；加滑溜用 `ApplyExact` 绕过原版滑溜的联机倍增。`NightstalkingRune`
- **敌方"多多益善"统计本局所有玩家的完整 `Player.Relics`（含作为遗物持有的海克斯）。** 每满 N 个加 1%，N = 本局玩家数，先合计再取整、无上限，每次重算不缓存开局值。
- **敌方"开悟"在能量费用 Late Hook 把低于 1 的费用抬到 1，不改卡牌基础值。** 与"无本万利"并存时，手动打出按抬高后的费用判断，不再算零费；辉星费用与 X 费不变。
- **敌方"重铸战盔"把本场收到的负数 `StrengthPower` 变化量归零。** 因此临时力量到期的回收（施加负数力量）同样不扣减；临时效果本身仍由原版移除，不在施加后补发力量。
- **敌方默认禁用项写进可修改的默认禁用集合，不从可配置内容目录里硬删除。** 用户手动重新启用必须持续有效。
- **鲜血神像在战斗已结束/正在结束/无战斗状态时改用 `CreatureCmd.SetCurrentHp` 扣 1 点、最低保留 1 点。** 奖励界面的死亡不能交给已经停止的战斗流程处理，也避免非战斗伤害修正把扣血放大到致死；仍发送原版生命变化回调。`BloodIdolRune`
- **仅开启敌方海克斯时才显示独立确认界面。** 条件是我方数量为 0 且本幕确有新增敌方海克斯；沿用现有重掷/移除/撤销与敌方同步消息，不生成玩家候选、不发放玩家遗物；界面未确认退出不标记该幕完成。

## 卡牌升级

- **`CardUpgradeRuneBase<TCard>` 的局部替换一律是 `Priority.Low` 的条件 prefix。** 原版没有改变单张卡/Power 内部操作的细粒度 Hook；只有模型所属玩家持有对应符文时才跳过回调，其他玩家与未启用效果保持原版。
- **这些局部替换的入口与异步 `MoveNext` 的 IL 冻结在 `vanilla_copy_guard.<target>.txt`。** 游戏更新后必须人工核对原版行为，核对完才用 `HEXTECH_WRITE_UPGRADE_GUARD=1` 增补冻结表；刷新快照不能代替行为审查。
- **爪击的永久成长按 `DeckVersion` 去重写进 `HextechSelfUpgradeCardStore`。** 原版爪击的本场成长不写入永久计数；之后新获得的爪击不追溯之前的触发。`ClawUpgradeRune`
- **吊杀施加独立的 `HextechHangPower`（倍率 2、4、8……），适用于该目标受到的所有伤害。** 但不把直接失去生命改成伤害，也不替换其他玩家原版吊杀的 Power。`HangUpgradeRune`
- **狱火用自身伤害命令返回的 `DamageResult.TotalDamage` 施加灼烧。** 包含被格挡的伤害，但不把伤害链中其他效果的伤害算成狱火的。`InfernoUpgradeRune`
- **范围限定的三条：** 子弹时间只阻止该牌自身施加的无法抽牌；狂怒/倒映只阻止持有者对应 Power 的定时清理，不禁止外部移除；粒子墙只改战斗卡实例的格挡，不回写牌库本体。
- **烟囱在持有者抽到状态牌时补伤害，不是"加入手牌"就触发。**
- **散射炮把可变化的状态牌走原版变化命令变为燃料再移到弃牌堆，不触发消耗。** 不可变化的牌保留原状，原命中次数不变。

## 金币与奖励

- **战斗内发金币一律在模型 Hook 里等待原版 `PlayerCmd.GainGold`，不按 `LocalContext` 筛选执行端，也不追加金币同步消息。** 确定性来自各端执行同一条触发链，不能从 UI 或本地回调补发。涉及献祭、收集者、炽燃利息、小猪存钱罐、夺金、升级：王国资产。
- **小猪存钱罐、夺金、炽燃利息在自身发钱期间禁止自身重入（`finally` 解除）。** 防"获得金币 → 鲜血神像伤害 → 受击/反击 → 再次发钱"的循环；后续独立伤害仍正常触发。
- **`SavedCountThisCombat` / `SavedCounter` 保留原序列化身份，但新触发不再增加计数。** 旧存档的遗留计数仍按原战后奖励路径发放一次后清零；改名或删除会破坏旧档。
- **欧洛巴斯二次强化只在 `TouchOfOrobas.GetUpgradedStarterRelic` 的 postfix 里、且原结果是头环时才补映射。** 不覆盖其他模组已有的非头环升级结果；已持有"+"版时再次获得仍映射到同一"+"版，不叠加也不降级。`OrobasPlusUpgrades`
- **"+"版继承 `RelicModel` 而非 `HextechRelicBase`。** 因此不参加海克斯计数、重铸与候选生成，只注册到 EventRelicPool；保留 Starter 稀有度并默认在图鉴隐藏。类名是联机契约，不要改。
- **启用判定复用 `HextechMayhemModifier.IsEnabledForRun`：缺少 modifier ≠ 禁用。** 单机旧局或控制台缺 modifier 时读菜单开关，否则单机会回退成头环；联机缺快照时不使用各端本地配置。
- **古老牙齿遇到永恒牌会抛异常并卡住整条组合奖励链。** 原版 `CardTransformation` 构造器与 `CardCmd.Transform` 都检查 `IsTransformable`。修法是 AsyncLocal 作用域 + `CardModel.IsTransformable` postfix 只放行本次记录的那一张永恒牌，不改 `IsRemovable`、不删原牌关键词、不复制原版转换命令、不吞其他异常。`ArchaicToothEternalHooks`

## 视觉

- **带文字的按钮不用 Godot `Button.Text`，文字交给居中的 `MegaLabel`。** `Button` 用主题默认字体，中日韩等语言会落到系统回退字体而发虚；`MegaLabel` 在 `_Ready` 时按当前语言替换字体。选择界面的确认按钮统一走 `CreateConfirmButton`（与重随/移除按钮同一套铜金色面板）。
- **濒死狂宴红光用的是原版 0.111.0 SOUL_NEXUS 图集的 `glowie` 区域，未改色未重绘。** 源 `res://animations/monsters/soul_nexus/soulnexus.png`，图集 1063×656，裁切 `(2, 77, 580, 577)`，无旋转；模组自己打包该纹理，运行时不依赖原版图集位置、Spine 或怪物场景脚本。重新提取用 `tools/extract_near_death_feast_glow.gd`，该脚本只校验整张图集尺寸，识别不了同尺寸重排 —— 游戏更新后先人工核对 `.atlas` 里 `glowie` 的坐标。`HextechNearDeathFeastVisual`
- **红光几何是调出来的固定值：** 中心在碰撞框自底向上 64% 处，基础宽度 = 碰撞框宽度夹取到 120–360 像素后的 2.45 倍，两层同步缩放。
- **表现节点不进战斗状态、不调用共享 RNG。** 死亡或脱离濒死时隐藏，角色节点销毁时释放。
- **灼烧常驻火焰保持程序化渐变粒子(沿骨骼发射的火焰、烟与火星);每次灼烧结算额外升起原版地面火 `NGroundFireVfx`(状态牌"灼伤"同款)。** 实机试过三种替代都被否决:原版火把 4 帧翻页图放大后像多边形碎片,整团着色器火焰摆在脚下像站在一排篝火上,着色器火苗无论撒在身上还是从脚底窜起都像火焰贴纸。`HextechBurnVisual`
- **夺金命中爆金币复用原版小鬼佣兵的 `vfx_coin_explosion_regular` 场景，挂到被命中生物的父节点、定位到碰撞框中心，并在主线程读取坐标。** 挂 `CombatVfxContainer` + 读 `VfxSpawnPosition` 的原版组合在实机上把金币放到了屏幕左上角；Godot 在非主线程读全局坐标会得到原点。不用 `VfxCmd.PlayOnCreatureCenter`，它会跳过已死目标。`HextechCombatVfx.CoinBurst`

## 生成与权重

- **模组关闭时模型仍无条件注册，只在 `SharedRelicPool.GetUnlockedRelics` 的窄范围 postfix 里排除 `HextechRelicBase`。** 原版 `RelicGrabBag.Populate` 会给 Starter 稀有度条目洗牌，即使它们最终不掉落也会消耗与遭遇/Boss 共用的 UpFront 随机数，生成后再移除条目无法回退已推进的 RNG。过滤保留原版与其他模组条目及顺序。
- **该过滤只读本局冻结配置，不读本地菜单值。** 联机房主配置尚未同步时读本地值会产生不同候选池。已接受的代价：开启模组的新局种子结果也可能与旧版本不同；旧存档已生成的房间和遭遇不重置。
- **角色专属海克斯用动态权重，没有固定位置保底。** 每名玩家新局 150%，刷出非专属 +10 个百分点、刷出本角色专属 −10 个百分点，最低 0%、无上限；三个位置依次抽取并立即使用更新后的倍率。
- **计数依据是刷出的候选，不是最终拿取。** 重掷成功生成的候选也计一次，未发生替换不计；混沌实验室在普通候选生成之后替换，不产生第二次计数。
- **倍率为零且合法池只剩专属时回退到原有标签权重，避免空选项。** 没有映射到原版角色池的模组角色不推进专属权重。
- **权重确认后提交绝对值，不按最后三个候选反推被重掷覆盖的历史。** 远端缺倍率或格式错误时中止该选择，禁止默默回退到 150%。`HextechWeightedRuneOptions`
- **权重存进既有 `SavedRuneSelectionJournalJson` 的 `characterWeights`（按玩家 ID 排序），没有新增或改名 SavedProperty。** 旧存档缺这部分数据时从 150% 开始；无尽循环清理选择流水时保留倍率。
- **玩家重随次数设为无限时，每幕选择界面改成直接自选：列出本稀有度的全部合法海克斯，与配置界面的启用开关同一套过滤（配置、幕、角色、已拥有、互斥）。** 判定读本局冻结配置（联机是房主的）；三候选照常先按权重生成，所以候选 RNG 与角色倍率照常推进，稀有度也由候选决定。自选池只在本机用 `BuildSelectableRunePool` 构造，不消耗 RNG；提交时把最终候选换成所选的一个（序号 0、无重随历史、倍率原样），远端按 ID 还原，同步格式不变。锻造器选择与只选敌方海克斯的界面不受影响。`HextechRuneSelectionScreen.SelfPick.cs`、`BuildSelfPickPool`
- **海克斯选择二次确认（来自公开仓库 PR #33）是本机界面偏好，默认关，放在配置-杂项。** 开启后普通每幕三选一点卡片只标记待定，按确认才提交；待定时仍可重随，重随待定的那张会清掉待定。它只改变本机何时提交，提交内容与同步协议不变，所以存在 UI 偏好文件而不是本局冻结配置，联机各端可以不同；界面打开时读一次。锻造器、只选敌方海克斯、自选模式都不启用：自选本身就是点选加确认，而且它的确认走同一个选定入口、不带卡槽，若启用会被当成无效待定而吞掉。`ShouldUsePlayerRuneConfirmation`

## 手柄

- **是否给默认焦点只看游戏自己的输入模式（0.110 起 `IsUsingDirectionalNavigation`，0.107.1 为 `IsUsingController`），鼠标玩家打开界面不出焦点框。** 不能从事件类型自己判断：Steam Input 下按键到达时已是合成的动作事件，没有原始 `InputEventJoypadButton`；不走 Steam 时第一下按键又会被原版切换手柄模式时吞掉。原版切进手柄模式和切换界面时都会聚焦 `IScreenContext.DefaultFocusedControl`，选择界面靠这个拿到焦点。`HextechControllerInput`
- **原版确认键 A/× 映射为 `ui_select`，只有原版 `NClickableControl` 认它；本模组的 Godot `Button` 和自绘控件只认 `ui_accept`，而游戏里没有任何手柄键映射到 `ui_accept`。** 选择界面和配置菜单在自己子树有焦点时，把 `ui_select` 的按下和松开延后转成 `ui_accept`；原版可点击控件不转换，以免触发两次。`HextechControllerInput.TryTranslateSelectToAccept`
- **配置菜单挂在场景根上，不是原版认得的当前界面。** 打开期间关掉主菜单的可聚焦性，否则方向导航会跨过遮罩落到背后的主菜单按钮；社区面板和上传对话框登记为子弹窗，按 B 先逐层关闭它们；LB/RB 切页签。`HextechControllerOverlay`
- **顶栏的敌方海克斯折叠按钮和隐藏 UI 开关不在原版顶栏焦点链上，手柄够不着，暂未处理。**

## 待实机验证

- **F04 幽灵鳗 Skittish 的动画顺序改动已回滚，源码保持 `6a5ec941` 原样。** 曾把"BlockEnd 音画失败仍保留 Skittish"改成先移除 Power 再补出场音画，但这个顺序正是玩家实报"感受燃烧打四鳗卡死"的修复点，未经实机验证不要再调整。`src/Combat/HextechMonsterInteractionPolicy.cs`
- **F10/F11 在自有基类边界用 `new` 隐藏非虚的 `Flash` 重载与计数事件，只捕获表现回调，尚未实机验证。** 目的是防 UI 订阅者抛错截断共享写入；只隔离原版本来就属于 UI 的事件，不拦原版/第三方全局事件，也没有把所有 Flash 搬到共享写入之后。`HextechRelicBase.TurnProc.cs`、`src/Compat/HextechModelBaseCompat.cs`
