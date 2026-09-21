# 描述与实现审计 · 2026-09-21

作者：Natsuki。阶段 A 记录在文案修复前落盘；本报告以当前实现为真值，疑似代码错误只列待裁决，不改变玩法。

## 范围与证据

枚举并对照 `src/Runes`、`src/EnemyHexes`、`src/Cards`、`src/Powers`、`src/Forges`、`src/Enchantments` 共 **568 个 C# 文件**。类声明含嵌套辅助类 722 个；其中带直接本地化或敌方映射描述的类 623 个。逐文件清单和逐模型事实在附录；辅助类不计为独立玩家内容。

官方 PCK 用指定 `scan_sts2_rich_text.py` 的 `read_pck_entries/read_entry` 导出至 `/tmp/hextech-description-audit-20260921/localization/`，九语言共414份 JSON；原包未入库。官方卡牌/能力/遗物名由这些 JSON 核对，原版 AutomationPower 用 `../tools/sts2-inspect decompile MegaCrit.Sts2.Core.Models.Powers.AutomationPower` 核实十张抽牌计数、能量和重置时机。术语表见 `terminology-glossary.md`。

审阅按完整文件分批对照条件、命令、变量、继承基类；代码行号对应阶段 A 源码，后续仅局部修复可能让行号移动。百分比取整和不足候选池的通常下限不逐项扩写到每条卡面；会显著改变理解的非致死下限、16药水槽、玩家人数阈值、自动出牌例外单列。

## 阶段 A 发现摘要

语义发现 **56** 项；其中待裁决 3 项。另发现已存在 DynamicVar 的硬编码引用候选 251 处（重复值须按语义筛选，不能把所有相等数字替换为同一个变量）；九语言格式差异 351 个语言键，涉及 88 个唯一键；角色限定和官方引用名独立归类。

|问题类型|条数|
|---|---:|
|作用对象|3|
|作用范围|8|
|作用范围/下限|1|
|回合周期|2|
|待裁决|3|
|手动出牌|6|
|持续范围|1|
|数值语义|6|
|时机/取整|1|
|格挡|1|
|概率语义|2|
|次数上限|1|
|次数语义|1|
|联机差异|1|
|联机阈值|1|
|获得效果遗漏|1|
|获得时效果遗漏|2|
|触发时机|1|
|触发时机/格挡/取整|1|
|触发条件|8|
|阈值遗漏|5|

## 启用及普通条目

|类名｜中文名|问题类型|现文案|代码事实（文件:行）|建议文案|
|---|---|---|---|---|
|AdvanceToRetreatRune｜以进为退|触发条件|攻击带有[gold]易伤[/gold]的敌人时，获得[blue]3[/blue]点格挡。|只有攻击牌伤害 TotalDamage > 0 时触发；完全零伤害不触发。 `src/Runes/AdvanceToRetreatRune.cs:25`|攻击牌对带有易伤的敌人造成伤害时，获得3点格挡。|
|BashUpgradeRune｜升级：痛击|数值语义|打出[gold]痛击[/gold]或[gold]破击[/gold]时，获得等同于给予[gold]易伤[/gold]层数的[gold]力量[/gold]。|获得卡牌 DynamicVars.Vulnerable.BaseValue 数值的力量，不读取敌人实际收到的易伤。 `src/Runes/BashUpgradeRune.cs:46`|打出痛击或破击时，获得等同于该牌基础易伤层数的力量。|
|BrokenGoldenCrownRune｜破碎金冠|作用范围/下限|卡牌奖励少出现[blue]2[/blue]张牌。回合开始时，额外获得[blue]1[/blue]点能量。|仅 CardCreationSource.Encounter；至少保留1个奖励选项。 `src/Runes/BrokenGoldenCrownRune.cs:17`|战后卡牌奖励少出现2张牌，至少保留1张。回合开始时，额外获得1点能量。|
|CarefulSelectionRune｜精挑细选|数值语义|战后卡牌奖励变为[blue]4[/blue]选[blue]1[/blue]。|只补充不足4个的奖励选项，不削减原有大于4个的选项；候选池不足时不能补足。 `src/Runes/CarefulSelectionRune.cs:20`|战后卡牌奖励的选项至少有4张（可用卡牌不足时除外）。|
|ChainInSleeveRune｜袖中连环|触发条件|每当你打出[blue]3[/blue]张[gold]小刀[/gold]，就向你的手牌中添加[blue]1[/blue]张[gold]小刀[/gold]。|只统计手动打出、系列首段的小刀；重放和自动打出不计数。 `src/Runes/ChainInSleeveRune.cs:122`|每手动打出3张小刀（不计重放），向手牌中加入1张小刀。|
|DiceManiacRune｜掷骰狂人|概率语义|战斗胜利后，有[blue]50%[/blue]概率掉落一个随机属性锻造器。所有随机属性锻造器的黄金和棱彩阶出现概率翻倍。|掉落使用CurrentDropChance，初始50%，命中后按NextOffset以10个百分点动态调整；ForgeRarityMultiplier=2改权重，归一化后的概率并非翻倍。 `src/Runes/DiceManiacRune.cs:49`|战斗胜利后，初始有50%概率掉落一个随机属性锻造器；未掉落时概率提高10个百分点，掉落时降低10个百分点。随机属性锻造器的黄金和棱彩阶出现权重翻倍。|
|DoubleVisionRune｜复视|作用范围|当你获得卡牌、金币、药水或遗物奖励时，额外获得一份相同奖励。|不复制HextechRelicBase/HextechRunes程序集遗物，以及ArchaicTooth、TouchOfOrobas、GoldenCompass；锻造器走独立专用复制路径。 `src/Runes/DoubleVisionRune.Duplication.cs:114`|补充明确不复制的遗物范围；先核对所有例外官方名。|
|EternalArmorUpgradeRune｜升级：永恒铠甲|持续范围|打出永恒铠甲后，[gold]覆甲[/gold]在你的回合开始时不再减少。|打出后本场战斗所有PlatingPower负向变化均被置0，不只回合开始。 `src/Runes/EternalArmorUpgradeRune.cs:45`|打出永恒铠甲后，本场战斗你的覆甲层数不再减少。|
|FleshAndBoneRune｜骨肉交融|阈值遗漏|战斗开始时，失去[blue]3[/blue]点生命，召唤[blue]15[/blue]。战斗结束时，奥斯提每有[blue]10[/blue]最大生命值，你回复[blue]1[/blue]点生命。|失血最低保留1点；战斗结束治疗要求奥斯提仍存活。 `src/Runes/FleshAndBoneRune.cs:28`|战斗开始时，失去3点生命（最低保留1点），召唤15。战斗结束时，若奥斯提存活，其每有10点最大生命值，你回复1点生命。|
|GlassCannonRune｜玻璃大炮|获得时效果遗漏|造成的伤害提高[blue]50%[/blue]，但你的生命值无法回复到[blue]70%[/blue]以上。|AfterObtained立刻将超过70%上限的当前生命设为上限。 `src/Runes/GlassCannonRune.cs:20`|造成的伤害提高50%。获得时将当前生命降低至最多70%，之后也无法回复到70%以上。|
|GoliathRune｜歌利亚巨人|获得时效果遗漏|体型变大，获得[blue]35%[/blue]最大生命值，并获得[blue]20%[/blue]的伤害、格挡与治疗加成。|获得时调用Heal(MaxHp-CurrentHp)，额外回复至生命上限。 `src/Runes/GoliathRune.cs:45`|补充获得时回复全部生命。|
|InfernalConduitRune｜炼狱导管|时机/取整|你的攻击牌会对敌人施加[blue]2[/blue]层[gold]灼烧[/gold]。敌人每有[blue]5[/blue]层[gold]灼烧[/gold]，你就在下回合开始时额外获得[blue]1[/blue]点能量。|在自己回合结束时快照各存活敌人灼烧层数，分别整除5后相加，下回合开始发放能量。 `src/Runes/InfernalConduitRune.cs:48`|你的回合结束时，每名敌人每有5层灼烧，你在下回合开始时额外获得1点能量。|
|InkshadowRune｜墨影|作用范围|你以任何形式获得的[gold]小刀[/gold]都会获得[gold]墨影[/gold]附魔。|已有任何附魔的牌不会再附上Inky。 `src/Runes/InkshadowRune.cs:56`|你获得的未附魔小刀会获得墨影附魔。|
|LifeFlowRune｜生命回流|作用对象|每当有[blue]1[/blue]张牌被消耗时，回复最大生命值的[blue]5%[/blue]。每回合最多触发[blue]3[/blue]次。|只统计拥有者自己的牌消耗，不是任意玩家的牌。 `src/Runes/LifeFlowRune.cs:63`|每当你有1张牌被消耗时……|
|LightEmUpRune｜点亮他们！|触发条件|每打出[blue]{Attacks}[/blue]张攻击牌后，会对其目标发射[blue]{Missiles}[/blue]个飞弹，每个飞弹造成等同于攻击牌耗能的伤害。|手动首段攻击计数，达到4后若耗能0则保持充满，后续耗能>0攻击才发射并清零。 `src/Runes/LightEmUpRune.cs:201`|每手动打出4张攻击牌（不计重放）后，耗能大于0的攻击牌会向其目标发射6个飞弹并重置计数。|
|NearDeathFeastRune｜濒死狂宴|阈值遗漏|你的生命值可降至负数。负生命值时无法获得治疗和格挡，但每[blue]1[/blue]点负生命值获得[blue]1[/blue]点[gold]力量[/gold]。负生命值达到最大生命值的[blue]50%[/blue]时死亡。|effectiveHp<1即进入濒死，显示0生命时已禁止治疗/格挡；战斗胜利恢复1实际生命。 `src/Runes/NearDeathFeastRune.cs:133`|生命值为0或负数时无法获得治疗和格挡；战斗胜利后回复至1点生命。|
|QuantumComputingRune｜量子计算|数值语义|每过[blue]2[/blue]个回合，对所有敌人造成[blue]10[/blue]+其最大生命值[blue]10%[/blue]的伤害，并回复相当于所造成伤害值[blue]10%[/blue]的生命。|治疗按UnblockedDamage合计而非TotalDamage计算。 `src/Runes/QuantumComputingRune.cs:52`|并回复相当于所造成未被格挡伤害10%的生命。|
|RedEnvelopeRune｜红包|概率语义|战斗胜利后，额外获得奖励：有[blue]75%[/blue]概率掉落[blue]20~50[/blue]金币，有[blue]25%[/blue]概率掉落一个随机属性锻造器。|锻造器概率初始25%，NextOffset每次按5个百分点动态调整；其余概率发20-50金币。 `src/Runes/RedEnvelopeRune.cs:40`|锻造器初始概率25%；未掉落时提高5个百分点，掉落时降低5个百分点，否则获得20~50金币。|
|RekindleRune｜死灰复燃|作用对象|每当有[blue]2[/blue]张牌被消耗时，获得[blue]1[/blue]点能量。|只统计拥有者自己的牌消耗。 `src/Runes/RekindleRune.cs:47`|每当你有2张牌被消耗时，获得1点能量。|
|RoyalTrialRune｜御前试剑|触发条件|每当你打出[gold]君王之剑[/gold]，随机将[blue]2[/blue]张仆从牌加入手牌。|只统计手动打出的系列首段君王之剑，自动打出/重放不会触发。 `src/Runes/RoyalTrialRune.cs:41`|每当你手动打出君王之剑（不计重放），随机将2张仆从牌加入手牌。|
|ScapegoatRune｜祸水东引|作用范围|回合开始时，将你身上的所有的负面状态转移给随机敌人。|移除所有负面效果，但仅将支持的13种类型移给敌人；不支持类型只移除。 `src/Runes/ScapegoatRune.cs:53`|回合开始时，移除你身上的所有负面效果，并将其中可转移的效果给予随机敌人。|
|SellOffRune｜变卖|作用范围|当你丢弃非[gold]奇巧[/gold]牌时，将其打出并消耗。|仅Attack/Skill/Power类型，非奇巧状态牌及诅咒牌不触发。 `src/Runes/SellOffRune.cs:58`|当你丢弃非奇巧的攻击、技能或能力牌时，将其打出并消耗。|
|SpeedDemonRune｜速度恶魔|触发条件|每回合首次对敌人造成伤害时，额外抽[blue]2[/blue]张牌。|仅 UnblockedDamage > 0 触发。 `src/Runes/SpeedDemonRune.cs:47`|改为造成未被格挡的伤害。|
|SurvivorUpgradeRune｜升级：生存者|数值语义|打出[gold]生存者[/gold]后，你可以选择并丢弃任意张牌。每丢弃[blue]1[/blue]张牌，获得[blue]1[/blue]次格挡。|丢弃全部选择后一次获得 Block.BaseValue × count 格挡，不是逐张获得一次。 `src/Runes/SurvivorUpgradeRune.cs:43`|额外获得等同于此牌格挡值乘以丢牌数量的格挡。|
|TerminalIllnessRune｜病入膏肓|作用对象|[gold]中毒[/gold]效果触发时不再降低层数。|只拦截 Enemy 一侧中毒减少。 `src/Runes/TerminalIllnessRune.cs:20`|敌人的中毒层数不再自然减少。|
|TwilightVeilRune｜薄暮法衣|作用范围|敌人在战斗内获得增益效果时，你获得相同增益效果。|第一玩家回合开始后才 armed；排除 IsMonsterMechanismBuff。 `src/Runes/TwilightVeilRune.cs:43`|从首个玩家回合开始，敌人获得可由玩家持有的普通增益效果时，你也获得相同效果；不复制怪物专属机制。|
|SomethingForNothingRune｜无本万利|触发条件|打出[blue]0[/blue]费牌时，抽[blue]{Cards}[/blue]张牌。每回合首次打出非[blue]0[/blue]费牌时，该牌在本场战斗中的耗能降低[blue]{Energy}[/blue]。|减费条件排除 CostsX。 `src/Runes/SomethingForNothingRune.cs:67`|非X费卡牌才享受首张非零费用减费。|
|BackToBasicsRune｜回归基本功|触发条件|你无法打出[blue]3[/blue]费及以上的卡牌，你获得的治疗量，格挡量和造成的伤害增加[blue]40%[/blue]。|CostsX 永远放行，不受3费限制。 `src/Runes/BackToBasicsRune.cs:8`|非X费卡牌耗能达到3及以上时无法打出。|
|FeyMagicEnemyHex｜精怪魔法|触发时机|当敌人造成未被格挡的伤害时，使玩家获得[blue]1[/blue]回合的[gold]缩小[/gold]和[gold]不可抽牌[/gold]。|命中后记录 pending，下个玩家回合开始才施加 Shrink/NoDraw。 `src/EnemyHexes/FeyMagicEnemyHex.cs:7`|命中后，在下个玩家回合开始时给予受击玩家缩小和不可抽牌。|
|BrutalityEnemyHex｜残虐|阈值遗漏|敌人在回合开始时损失[blue]5%[/blue]最大生命值，并获得[blue]1[/blue]/[blue]2[/blue]/[blue]3[/blue]点[gold]活力[/gold]。|扣除 Floor(MaxHp * 5%) 当前生命，SetCurrentHp 最低1；不是降低最大生命。 `src/EnemyHexes/BrutalityEnemyHex.cs:19`|失去等同于最大生命值5%的生命，最低保留1点。|
|GlassCannonEnemyHex｜玻璃大炮|获得效果遗漏|敌人造成的伤害提高[blue]30[/blue]/[blue]40[/blue]/[blue]50[/blue]%，但其生命值无法回复到[blue]70%[/blue]以上。|ApplyPersistentToEnemy 立即把当前生命压到最大生命70%。 `src/EnemyHexes/GlassCannonEnemyHex.cs:26`|额外注明获得该效果时当前生命降至最多70%。|
|LagavulinMatriarchEnemyHex｜升级：乐加维林族母|回合周期|每过[blue]3[/blue]/[blue]2[/blue]/[blue]1[/blue]回合，使得所有玩家失去[blue]1[/blue]点[gold]力量[/gold]和[blue]1[/blue]点[gold]敏捷[/gold]。|TryConsumeRoundInterval 参数3/2/1，HextechEnemyHexContext.cs:55 实际 round % (参数+1)，因此4/3/2回合。 `src/EnemyHexes/LagavulinMatriarchEnemyHex.cs:14`|每4/3/2回合，在玩家回合开始时触发。|
|LeafSlimeEnemyHex｜升级：树叶史莱姆|回合周期|每过[blue]3[/blue]/[blue]2[/blue]/[blue]1[/blue]回合，将[blue]1[/blue]张[gold]黏液[/gold]加入玩家的弃牌堆。|TryConsumeRoundInterval 参数3/2/1，实际4/3/2回合。 `src/EnemyHexes/LeafSlimeEnemyHex.cs:9`|每4/3/2回合，在玩家回合开始时触发。|
|LightEmUpEnemyHex｜点亮他们！|次数语义|玩家每回合打出的第四张攻击牌费用翻倍，回合结束后重置计数。|nextAttackIndex % 4 == 0，每第4张（4/8/12...），不只是第4张。 `src/EnemyHexes/LightEmUpEnemyHex.cs:12`|每回合每打出4张攻击牌，第4张费用翻倍。|
|NatureIsHealingEnemyHex｜自然即是治愈|联机差异|战斗中，所有敌人每[blue]15[/blue]/[blue]10[/blue]/[blue]5[/blue]秒回复[blue]1[/blue]点生命。|单机秒计时；联机每个敌方回合开始回复1，不使用秒数。 `src/EnemyHexes/NatureIsHealingEnemyHex.cs:34`|补充联机改为敌方回合开始时回复1生命。|
|NearDeathFeastEnemyHex｜濒死狂宴|阈值遗漏|敌人的生命值可降至负数。负生命值时无法获得治疗和格挡，但每[blue]1[/blue]点负生命值获得[blue]1[/blue]点[gold]力量[/gold]。负生命值达到最大生命值的[blue]5%[/blue]/[blue]10%[/blue]/[blue]15%[/blue]时死亡。|HextechEnemyNearDeath.cs:77 effectiveHp < 1 即濒死，显示0生命时也禁止治疗与格挡。 `src/EnemyHexes/NearDeathFeastEnemyHex.cs:11`|生命值为0或负数时无法获得治疗和格挡。|
|SwiftAndSafeEnemyHex｜快中求稳|联机阈值|玩家每抽[blue]15[/blue]/[blue]12[/blue]/[blue]10[/blue]张牌，所有敌人获得[blue]1[/blue]层[gold]人工制品[/gold]。|联机以全队抽牌总数除以（每人基础阈值×玩家人数）触发，不按单个玩家每N张触发。 `src/EnemyHexes/SwiftAndSafeEnemyHex.cs:68`|补充联机按全队累计抽牌且阈值乘玩家人数。|
|TankEngineEnemyHex｜坦克引擎|数值语义|每回合开始时，敌人获得[blue]5%[/blue]的最大生命值，并使自身体型增大[blue]5%[/blue]。|每次增加基础最大生命系数0.05，按捕获基值加算，不是按当前最大生命复利。 `src/EnemyHexes/TankEngineEnemyHex.cs:59`|每敌方回合开始增加5%基础最大生命值加成。|
|PandorasBoxEnemyHex｜潘朵拉的盒子|作用范围|玩家的卡牌奖励现在只会包含其他颜色的卡牌。|仅 Encounter 卡牌奖励，纯无色奖励与固定牌池修改标志不受影响。 `src/EnemyHexes/PandorasBoxEnemyHex.cs:10`|战后普通卡牌奖励改为其他已解锁角色的牌，无色奖励不受影响。|
|IGripEnemyHex｜我紧握|手动出牌|玩家每回合打出第1张牌时，失去[blue]0[/blue]/[blue]1[/blue]/[blue]2[/blue]点能量。|排除 IsAutoPlay 和非 IsFirstInSeries。 `src/EnemyHexes/IGripEnemyHex.cs:13`|每回合手动打出第1张牌。|
|MirrorReflectionEnemyHex｜镜中倒影|手动出牌|玩家打出[gold]打击[/gold]或[gold]防御[/gold]时，在弃牌堆放入[blue]1[/blue]张此牌的复制品。|仅手动首段且 IsBasicStrikeOrDefend。 `src/EnemyHexes/MirrorReflectionEnemyHex.cs:9`|手动打出基础打击或基础防御时。|
|MonarchsGazeEnemyHex｜王之凝视|手动出牌|玩家打出攻击牌时，失去[blue]1[/blue]点临时力量。|仅手动首段攻击牌。 `src/EnemyHexes/MonarchsGazeEnemyHex.cs:9`|手动打出攻击牌时。|
|TanksShieldEnemyHex｜坦克斯之盾|手动出牌|玩家每打出[blue]1[/blue]张攻击牌，所有敌人获得[blue]1[/blue]/[blue]2[/blue]/[blue]3[/blue]点格挡。|仅手动首段攻击牌。 `src/EnemyHexes/TanksShieldEnemyHex.cs:9`|手动打出攻击牌时。|
|UpgradeEnemyHex｜神化|手动出牌|玩家打出卡牌后，该卡牌会在本场战斗中被降级。|仅手动首段已升级卡牌。 `src/EnemyHexes/UpgradeEnemyHex.cs:9`|手动打出已升级卡牌后，该牌本战斗降级。|
|HextechInfernalDragonSoulPower｜炼狱龙魂|手动出牌|每回合的首张攻击牌会对敌人施加等同于层数的[gold]灼烧[/gold]。|仅手动首段且可解析目标的攻击牌消耗每回合一次触发。 `src/Cards/HextechDragonSoulPowers.cs:44`|每回合首张手动打出且以敌人为目标的攻击牌。|
|HextechBurnPower｜灼烧|触发时机/格挡/取整|结算时受到等同于层数百分比的当前生命伤害，最低等同于层数。随后灼烧层数减少[blue]10%[/blue]。|敌方在自身回合开始受不可格挡伤害；玩家在自身回合结束受可格挡伤害；层数扣减向上取整至少1。 `src/Powers/HextechPowers.cs:16`|补充双方不同时机和格挡差异，10%层数减少向上取整。|
|HextechNextTurnDamagePower｜下回合伤害|格挡|在玩家的下个回合开始时，受到等同于层数的伤害。|伤害具有 Unblockable。 `src/Powers/HextechNextTurnDamagePower.cs:33`|注明伤害无法被格挡。|
|HextechPlayerSlowPower｜缓慢|作用范围|受到的伤害改变等同于层数的百分比。层数为负时，改为降低受到的伤害。|Unpowered 伤害不受此系数影响。 `src/Powers/HextechPowers.cs:182`|只改变受加成的伤害，固定伤害不受影响。|
|PocketForge｜口袋锻造器|次数上限|获得时，获得[blue]2[/blue]个药水栏位。|药水栏位最多16。 `src/Forges/HextechForges.Silver.cs:240`|增加2个药水栏位，最多16个。|
|FlyingKickRune｜飞身踢|触发条件|你造成的伤害会处决生命值低于目标最大生命值[blue]{ExecutePercent}[/blue]%（[blue]10[/blue]+你最大生命值的[blue]8%[/blue]）的怪物，并回复自身[blue]10%[/blue]最大生命值。|须未被格挡伤害；普通击杀同样治疗，并非只有阈值处决才治疗。 `src/Runes/FlyingKickRune.cs:62`|造成未被格挡伤害时处决低于阈值敌人；击杀或处决敌人后治疗。|

## TXT 中以 # 标记的条目（仍审计，标记不等价于运行时配置禁用）

|类名｜中文名|问题类型|现文案|代码事实（文件:行）|建议文案|
|---|---|---|---|---|
|BrutalityRune｜残虐|阈值遗漏|在你的回合开始时，失去[blue]2[/blue]点生命，抽[blue]2[/blue]张牌。|生命扣减使用 Max(1, CurrentHp - HpLoss)，不能将生命降至1以下。 `src/Runes/BrutalityRune.cs:26`|在你的回合开始时，失去2点生命（最低保留1点），抽2张牌。|
|SwordFlightRune｜御剑飞行|数值语义|每回合首次打出[gold]君王之剑[/gold]时，抽牌直到抽满手牌。|抽牌数为 Max(0, 10 - 手牌数)，不读取手牌上限；仅手动首段。 `src/Runes/SwordFlightRune.cs:53`|每手动打出相应数量卡牌，抽牌至10张。|
|FeelTheBurnCard｜感受燃烧|作用范围|移除所有敌人的所有正面效果。<br>给予其[blue]{HextechBurnPower}[/blue]层[gold]灼烧[/gold]。|保留结构性怪物状态、玩家朝向关系及保存被偷金币/卡牌的Heist/Swipe。 `src/Cards/FeelTheBurnCard.cs:39`|移除普通正面效果，保留遭遇机制与偷窃记录。|

## 待裁决

以下项目没有改代码，也不把疑似实现错误当作文案修复依据。

|类名|代码事实|裁决问题|
|---|---|---|
|BurningInterestRune|最大生命值达到10,000,000的敌人灼烧伤害不产金币；防无限生命收益为代码显式例外。 `src/Runes/BurningInterestRune.cs:78`|待裁决是否应向玩家展示无限生命敌人的经济例外；不修改代码。|
|TauntRune|AfterPowerAmountChanged 未要求 amount > 0；自身为 applier 的零值/负向灾厄变化也可能抽牌。 `src/Runes/TauntRune.cs:41`|需判断原始Hook是否可能产生这类变化；保留代码和描述，列待裁决。|
|VoltaicUpgradeRune|重生成只 switch Lightning/Frost/Dark/Plasma/Glass，第三方充能球被忽略。 `src/Runes/VoltaicUpgradeRune.cs:52`|是否应支持第三方球需产品裁决；代码不改。|

## 官方名字与角色限定

62个卡牌升级模型与九语言官方卡名逐项配对。中文 `JuggernautUpgradeRune` 的“势不可挡”应为官方“势不可当”；俄语、日语、葡语等引用同样使用对应原包标题，不把 esp 与 spa 合并。其他外语升级标题中的直译、缩写按官方卡名更正。

角色资格以 `IsAvailableForPlayer/IsAvailableForCharacter` 为准，注册表 CharacterPool 只说明分类。根代理核对182个资格方法，其中170个角色方法与登记池一致；12个为通用、牌组或联机门槛。补齐或统一后缀不改变资格。

|类名|实际角色限制|源码|
|---|---|---|
|AdaptiveCapacitorRune|Defect|src/Runes/AdaptiveCapacitorRune.cs:10|
|AdvanceToRetreatRune|Ironclad|src/Runes/AdvanceToRetreatRune.cs:15|
|AutoPatrolRune|Necrobinder|src/Runes/AutoPatrolRune.cs:16|
|AutomationUpgradeRune|其他门槛（保留方法判定）|src/Runes/AutomationUpgradeRune.cs:35|
|BarbarianWayRune|Defect|src/Runes/BarbarianWayRune.cs:15|
|BashUpgradeRune|Ironclad|src/Runes/BashUpgradeRune.cs:20|
|BattleTranceUpgradeRune|Ironclad|src/Runes/BattleTranceUpgradeRune.cs:5|
|BeginningAndEndRune|Necrobinder|src/Runes/BeginningAndEndRune.cs:18|
|BerserkRune|Ironclad|src/Runes/BerserkRune.cs:16|
|BigHammerRune|Regent|src/Runes/BigHammerRune.cs:15|
|BigHandsRune|Necrobinder|src/Runes/BigHandsRune.cs:12|
|BigKnifeRune|Silent|src/Runes/BigKnifeRune.cs:15|
|BlankCheckRune|Regent|src/Runes/BlankCheckRune.cs:13|
|BloodArmorRune|Ironclad|src/Runes/BloodArmorRune.cs:15|
|BloodDebtRune|Ironclad|src/Runes/BloodDebtRune.cs:7|
|BloodPactRune|Ironclad|src/Runes/BloodPactRune.cs:23|
|BloodlettingUpgradeRune|Ironclad|src/Runes/BloodlettingUpgradeRune.cs:5|
|BodySlamUpgradeRune|Ironclad|src/Runes/BodySlamUpgradeRune.cs:6|
|BodyguardUpgradeRune|Necrobinder|src/Runes/BodyguardUpgradeRune.cs:5|
|BoneBreakUpgradeRune|Necrobinder|src/Runes/BoneBreakUpgradeRune.cs:12|
|BoneGuardRune|Necrobinder|src/Runes/BoneGuardRune.cs:10|
|BorrowedTimeUpgradeRune|Necrobinder|src/Runes/BorrowedTimeUpgradeRune.cs:8|
|BrandUpgradeRune|Ironclad|src/Runes/BrandUpgradeRune.cs:29|
|BrutalityRune|Ironclad|src/Runes/BrutalityRune.cs:11|
|BulletTimeUpgradeRune|Silent|src/Runes/BulletTimeUpgradeRune.cs:5|
|ByproductRune|Defect|src/Runes/ByproductRune.cs:10|
|CardUpgradeRuneBase|其他门槛（保留方法判定）|src/Runes/CardUpgradeRuneBase.cs:25|
|CatalystRune|Silent|src/Runes/CatalystRune.cs:17|
|ChainInSleeveRune|Silent|src/Runes/ChainInSleeveRune.cs:47|
|ChargeUpRune|Regent|src/Runes/ChargeUpRune.cs:11|
|ClawUpgradeRune|Defect|src/Runes/ClawUpgradeRune.cs:5|
|CompactUpgradeRune|Defect|src/Runes/CompactUpgradeRune.cs:9|
|CompensationRune|Necrobinder|src/Runes/CompensationRune.cs:14|
|CondensedRadianceRune|Regent|src/Runes/CondensedRadianceRune.cs:11|
|CoreOverloadRune|Defect|src/Runes/CoreOverloadRune.cs:16|
|CorpseExplosionRune|Silent|src/Runes/CorpseExplosionRune.cs:10|
|CorrosiveWaveUpgradeRune|Silent|src/Runes/CorrosiveWaveUpgradeRune.cs:15|
|CrashLandingUpgradeRune|Regent|src/Runes/CrashLandingUpgradeRune.cs:11|
|CreativeAiUpgradeRune|Defect|src/Runes/CreativeAiUpgradeRune.cs:10|
|CurtainCallRune|Silent|src/Runes/CurtainCallRune.cs:27|
|DeathWarrantRune|Silent|src/Runes/DeathWarrantRune.cs:46|
|DecayRune|Silent|src/Runes/DecayRune.cs:17|
|DecisionsDecisionsUpgradeRune|Regent|src/Runes/DecisionsDecisionsUpgradeRune.cs:11|
|DefendUpgradeRune|其他门槛（保留方法判定）|src/Runes/DefendUpgradeRune.cs:9|
|DemonFormUpgradeRune|Ironclad|src/Runes/DemonFormUpgradeRune.cs:6|
|DeviantCognitionRune|Ironclad|src/Runes/DeviantCognitionRune.cs:5|
|DexterityStrengthToFocusRune|Defect|src/Runes/DexterityStrengthToFocusRune.cs:10|
|DieForYouRune|Necrobinder|src/Runes/DieForYouRune.cs:35|
|DirgeUpgradeRune|Necrobinder|src/Runes/DirgeUpgradeRune.cs:7|
|DivineInterventionRune|其他门槛（保留方法判定）|src/Runes/DivineInterventionRune.cs:16|
|DoomsdayRune|Necrobinder|src/Runes/DoomsdayRune.cs:16|
|DoubleExistenceRune|Silent|src/Runes/DoubleExistenceRune.cs:5|
|DrainRune|Necrobinder|src/Runes/DrainRune.cs:10|
|DrawYourSwordRune|Defect|src/Runes/DrawYourSwordRune.cs:19|
|DualcastUpgradeRune|Defect|src/Runes/DualcastUpgradeRune.cs:13|
|EchoFormUpgradeRune|Defect|src/Runes/EchoFormUpgradeRune.cs:6|
|EchoRune|Regent|src/Runes/EchoRune.cs:10|
|ElectricSurgeRune|Defect|src/Runes/ElectricSurgeRune.cs:10|
|ElectrodynamicsRune|Defect|src/Runes/ElectrodynamicsRune.cs:16|
|EmergenceRune|Defect|src/Runes/EmergenceRune.cs:10|
|EternalArmorUpgradeRune|其他门槛（保留方法判定）|src/Runes/EternalArmorUpgradeRune.cs:17|
|ExplosionArtRune|Regent|src/Runes/ExplosionArtRune.cs:15|
|ExposeUpgradeRune|Silent|src/Runes/ExposeUpgradeRune.cs:5|
|ExtremeSpeedRune|Silent|src/Runes/ExtremeSpeedRune.cs:20|
|FallingStarUpgradeRune|Regent|src/Runes/FallingStarUpgradeRune.cs:23|
|FeedUpgradeRune|Ironclad|src/Runes/FeedUpgradeRune.cs:47|
|FlakCannonUpgradeRune|Defect|src/Runes/FlakCannonUpgradeRune.cs:7|
|FlameBarrierUpgradeRune|Ironclad|src/Runes/FlameBarrierUpgradeRune.cs:5|
|FlawlessRune|Regent|src/Runes/FlawlessRune.cs:10|
|FleshAndBoneRune|Necrobinder|src/Runes/FleshAndBoneRune.cs:13|
|FuriousGlareRune|Ironclad|src/Runes/FuriousGlareRune.cs:17|
|GalacticGiftRune|Regent|src/Runes/GalacticGiftRune.cs:41|
|GloomyCloudsRune|Defect|src/Runes/GloomyCloudsRune.cs:5|
|GrandFinaleUpgradeRune|Silent|src/Runes/GrandFinaleUpgradeRune.cs:8|
|GroundedRune|Ironclad|src/Runes/GroundedRune.cs:5|
|GrowingStrongerRune|Ironclad|src/Runes/GrowingStrongerRune.cs:10|
|HangUpgradeRune|Necrobinder|src/Runes/HangUpgradeRune.cs:5|
|HappyAccidentRune|Defect|src/Runes/HappyAccidentRune.cs:12|
|HardBonesRune|Necrobinder|src/Runes/HardBonesRune.cs:15|
|HiddenGemUpgradeRune|其他门槛（保留方法判定）|src/Runes/HiddenGemUpgradeRune.cs:12|
|HotfixUpgradeRune|Defect|src/Runes/HotfixUpgradeRune.cs:5|
|ImmortalBoneRune|Necrobinder|src/Runes/ImmortalBoneRune.cs:10|
|InfernoUpgradeRune|Ironclad|src/Runes/InfernoUpgradeRune.cs:9|
|InkshadowRune|Silent|src/Runes/InkshadowRune.cs:14|
|InstantDeathRune|Necrobinder|src/Runes/InstantDeathRune.cs:10|
|IronWaveUpgradeRune|Ironclad|src/Runes/IronWaveUpgradeRune.cs:13|
|JackpotUpgradeRune|其他门槛（保留方法判定）|src/Runes/JackpotUpgradeRune.cs:13|
|JinlianBoxRune|其他门槛（保留方法判定）|src/Runes/JinlianBoxRune.cs:11|
|JuggernautUpgradeRune|Ironclad|src/Runes/JuggernautUpgradeRune.cs:8|
|KeystoneHunterRune|Silent|src/Runes/KeystoneHunterRune.cs:17|
|KillerHunterRune|Silent|src/Runes/KillerHunterRune.cs:16|
|KingdomArmyRune|Regent|src/Runes/KingdomArmyRune.cs:14|
|KnowThyPlaceUpgradeRune|Regent|src/Runes/KnowThyPlaceUpgradeRune.cs:5|
|LethalTempoRune|Silent|src/Runes/LethalTempoRune.cs:15|
|LifeFlowRune|Ironclad|src/Runes/LifeFlowRune.cs:33|
|LoopUpgradeRune|Defect|src/Runes/LoopUpgradeRune.cs:5|
|LubricantRune|Defect|src/Runes/LubricantRune.cs:27|
|MadScientistRune|Defect|src/Runes/MadScientistRune.cs:16|
|MakeItMineRune|Necrobinder|src/Runes/MakeItMineRune.cs:27|
|MarkovBabbleRune|Defect|src/Runes/MarkovBabbleRune.cs:12|
|MirageRune|Silent|src/Runes/MirageRune.cs:15|
|MiserableFateRune|Necrobinder|src/Runes/MiserableFateRune.cs:15|
|MiseryUpgradeRune|Necrobinder|src/Runes/MiseryUpgradeRune.cs:7|
|MoltenFistUpgradeRune|Ironclad|src/Runes/MoltenFistUpgradeRune.cs:12|
|MyriadManifestationsRune|Defect|src/Runes/MyriadManifestationsRune.cs:5|
|MyriadSwordsRune|Regent|src/Runes/MyriadSwordsRune.cs:16|
|NatureIsHealingRune|其他门槛（保留方法判定）|src/Runes/NatureIsHealingRune.cs:23|
|NearDeathFeastRune|Ironclad|src/Runes/NearDeathFeastRune.cs:48|
|NetherSoulRune|Necrobinder|src/Runes/NetherSoulRune.cs:7|
|NeurosurgeUpgradeRune|Necrobinder|src/Runes/NeurosurgeUpgradeRune.cs:14|
|NeutralizeUpgradeRune|Silent|src/Runes/NeutralizeUpgradeRune.cs:25|
|NightmareRune|Defect|src/Runes/NightmareRune.cs:8|
|NightmareUpgradeRune|Silent|src/Runes/NightmareUpgradeRune.cs:5|
|NowYouSeeMeRune|Silent|src/Runes/NowYouSeeMeRune.cs:10|
|OblivionUpgradeRune|Necrobinder|src/Runes/OblivionUpgradeRune.cs:14|
|OminousPactRune|Necrobinder|src/Runes/OminousPactRune.cs:12|
|OrbSymbiosisRune|Defect|src/Runes/OrbSymbiosisRune.cs:12|
|OurHealingRune|其他门槛（保留方法判定）|src/Runes/OurHealingRune.cs:12|
|PactsEndUpgradeRune|Ironclad|src/Runes/PactsEndUpgradeRune.cs:13|
|ParticleWallUpgradeRune|Regent|src/Runes/ParticleWallUpgradeRune.cs:5|
|PlasterRune|Necrobinder|src/Runes/PlasterRune.cs:10|
|PlateletRune|Ironclad|src/Runes/PlateletRune.cs:10|
|PrecisionCognitionRune|Defect|src/Runes/PrecisionCognitionRune.cs:15|
|RageUpgradeRune|Ironclad|src/Runes/RageUpgradeRune.cs:5|
|ReanimateUpgradeRune|Necrobinder|src/Runes/ReanimateUpgradeRune.cs:14|
|ReapUpgradeRune|Necrobinder|src/Runes/ReapUpgradeRune.cs:11|
|ReaperFormUpgradeRune|Necrobinder|src/Runes/ReaperFormUpgradeRune.cs:6|
|RebootUpgradeRune|Defect|src/Runes/RebootUpgradeRune.cs:5|
|RecycleBinRune|Defect|src/Runes/RecycleBinRune.cs:5|
|ReflectUpgradeRune|Regent|src/Runes/ReflectUpgradeRune.cs:5|
|ReforgedHelmetRune|Ironclad|src/Runes/ReforgedHelmetRune.cs:10|
|RekindleRune|Ironclad|src/Runes/RekindleRune.cs:28|
|RenewalRune|Silent|src/Runes/RenewalRune.cs:10|
|ReprogramRune|Defect|src/Runes/ReprogramRune.cs:12|
|RoyalCommandRune|Regent|src/Runes/RoyalCommandRune.cs:10|
|RoyalTrialRune|Regent|src/Runes/RoyalTrialRune.cs:20|
|RoyaltiesUpgradeRune|Regent|src/Runes/RoyaltiesUpgradeRune.cs:8|
|ScaredStiffRune|Ironclad|src/Runes/ScaredStiffRune.cs:8|
|SellOffRune|Silent|src/Runes/SellOffRune.cs:15|
|SendThemInRune|Regent|src/Runes/SendThemInRune.cs:19|
|SerpentFormUpgradeRune|Silent|src/Runes/SerpentFormUpgradeRune.cs:6|
|SerpentsFangRune|Silent|src/Runes/SerpentsFangRune.cs:16|
|ServantMasterRune|Necrobinder|src/Runes/ServantMasterRune.cs:16|
|ShriekUpgradeRune|Silent|src/Runes/ShriekUpgradeRune.cs:16|
|SkyDrillUpgradeRune|Regent|src/Runes/SkyDrillUpgradeRune.cs:11|
|SmokestackUpgradeRune|Defect|src/Runes/SmokestackUpgradeRune.cs:5|
|SnakebiteRune|Silent|src/Runes/SnakebiteRune.cs:15|
|SnakebiteUpgradeRune|Silent|src/Runes/SnakebiteUpgradeRune.cs:10|
|SomethingFromNothingRune|Necrobinder|src/Runes/SomethingFromNothingRune.cs:15|
|SonataRune|其他门槛（保留方法判定）|src/Runes/SonataRune.cs:12|
|SoulUpgradeRune|Necrobinder|src/Runes/SoulUpgradeRune.cs:12|
|SowUpgradeRune|Necrobinder|src/Runes/SowUpgradeRune.cs:11|
|StardustUpgradeRune|Regent|src/Runes/StardustUpgradeRune.cs:5|
|StarlightSplendorRune|Regent|src/Runes/StarlightSplendorRune.cs:10|
|StormUpgradeRune|Defect|src/Runes/StormUpgradeRune.cs:5|
|StrikeUpgradeRune|其他门槛（保留方法判定）|src/Runes/StrikeUpgradeRune.cs:11|
|SubroutineUpgradeRune|Defect|src/Runes/SubroutineUpgradeRune.cs:20|
|SummonForthRune|Regent|src/Runes/SummonForthRune.cs:15|
|SurvivorUpgradeRune|Silent|src/Runes/SurvivorUpgradeRune.cs:10|
|SweepingBladeRune|Ironclad|src/Runes/SweepingBladeRune.cs:12|
|SwordFlightRune|Regent|src/Runes/SwordFlightRune.cs:12|
|SwordIntentRune|Regent|src/Runes/SwordIntentRune.cs:10|
|SwordsmanshipRune|Regent|src/Runes/SwordsmanshipRune.cs:15|
|TauntRune|Necrobinder|src/Runes/TauntRune.cs:15|
|TerminalIllnessRune|Silent|src/Runes/TerminalIllnessRune.cs:13|
|ThoughtOverwriteRune|Necrobinder|src/Runes/ThoughtOverwriteRune.cs:28|
|TranscendentEvilRune|Defect|src/Runes/TranscendentEvilRune.cs:29|
|TriPrismRune|Regent|src/Runes/TriPrismRune.cs:7|
|TrickLicenseRune|Silent|src/Runes/TrickLicenseRune.cs:10|
|TrinityRune|Regent|src/Runes/TrinityRune.cs:12|
|UndyingUpgradeRune|Necrobinder|src/Runes/UndyingUpgradeRune.cs:22|
|UnleashUpgradeRune|Necrobinder|src/Runes/UnleashUpgradeRune.cs:18|
|UnsealedThroneRune|Regent|src/Runes/UnsealedThroneRune.cs:10|
|VenerateUpgradeRune|Regent|src/Runes/VenerateUpgradeRune.cs:5|
|VenomousBladeRune|Silent|src/Runes/VenomousBladeRune.cs:11|
|VoidFormUpgradeRune|Regent|src/Runes/VoidFormUpgradeRune.cs:6|
|VoltaicUpgradeRune|Defect|src/Runes/VoltaicUpgradeRune.cs:8|
|WhirlwindUpgradeRune|Ironclad|src/Runes/WhirlwindUpgradeRune.cs:8|
|WizardlyThinkingRune|Defect|src/Runes/WizardlyThinkingRune.cs:10|
|WraithRune|Necrobinder|src/Runes/WraithRune.cs:18|
|WroughtInWarUpgradeRune|Regent|src/Runes/WroughtInWarUpgradeRune.cs:6|
|ZapUpgradeRune|Defect|src/Runes/ZapUpgradeRune.cs:5|

## 硬编码与动态变量

下表为对应模型已有变量的逐处候选；同值但不同意义（如“每1张牌”与“获得1层能力”）不会机械共用。乘数1.1与10%不能直接互换占位符。尚未改用占位符的字面量保留并在阶段 B 结果列出理由；不修改 SavedProperty 或同步协议。

|类|现文本|变量引用|源码|
|---|---|---|---|
|AdamantRune|[blue]3[/blue]|[blue]{Block}[/blue]|src/Runes/AdamantRune.cs:3|
|AdaptiveCapacitorRune|[blue]1[/blue]|[blue]{OrbSlots}[/blue]|src/Runes/AdaptiveCapacitorRune.cs:3|
|AdvanceToRetreatRune|[blue]3[/blue]|[blue]{Block}[/blue]|src/Runes/AdvanceToRetreatRune.cs:3|
|AnthonyBiasRune|[blue]1[/blue]|[blue]{PercentPerCard}[/blue]|src/Runes/AnthonyBiasRune.cs:3|
|ArcanePunchRune|[blue]2[/blue]|[blue]{AttacksPerEnergy}[/blue]|src/Runes/ArcanePunchRune.cs:3|
|ArcanePunchRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/ArcanePunchRune.cs:3|
|ArchmageRune|[blue]33%[/blue]|[blue]{ChancePercent}%[/blue]|src/Runes/ArchmageRune.cs:3|
|AstralBodyRune|[blue]50%[/blue]|[blue]{MaxHpPercent}%[/blue]|src/Runes/AstralBodyRune.cs:5|
|AttackDefenseUnityRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/AttackDefenseUnityRune.cs:3|
|AutomationUpgradeRune|[blue]2[/blue]|[blue]{Cards}[/blue]|src/Runes/AutomationUpgradeRune.cs:11|
|BadTasteRune|[blue]1[/blue]|[blue]{Heal}[/blue]|src/Runes/BadTasteRune.cs:3|
|BeginningAndEndRune|[blue]100[/blue]|[blue]{LethalityPower}[/blue]|src/Runes/BeginningAndEndRune.cs:4|
|BeginningAndEndRune|[blue]6[/blue]|[blue]{CountdownPower}[/blue]|src/Runes/BeginningAndEndRune.cs:4|
|BerserkRune|[blue]3[/blue]|[blue]{VulnerablePower}[/blue]|src/Runes/BerserkRune.cs:3|
|BerserkRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/BerserkRune.cs:3|
|BladeWaltzRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/BladeWaltzRune.cs:3|
|BloodArmorRune|[blue]1[/blue]|[blue]{PlatingPower}[/blue]|src/Runes/BloodArmorRune.cs:3|
|BloodArmorRune|[blue]1[/blue]|[blue]{PlatingPower}[/blue]|src/Runes/BloodArmorRune.cs:3|
|BloodIdolRune|[blue]5[/blue]|[blue]{Heal}[/blue]|src/Runes/BloodIdolRune.cs:3|
|BloodPactRune|[blue]1[/blue]|[blue]{StrengthPower}[/blue]|src/Runes/BloodPactRune.cs:3|
|BreadSandwichRune|[blue]1[/blue]|[blue]{Replays}[/blue]|src/Runes/BreadSandwichRune.cs:3|
|BrokenGoldenCrownRune|[blue]2[/blue]|[blue]{RewardOptionsLost}[/blue]|src/Runes/BrokenGoldenCrownRune.cs:3|
|BrokenGoldenCrownRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/BrokenGoldenCrownRune.cs:3|
|BrutalForceRune|[blue]2[/blue]|[blue]{Cards}[/blue]|src/Runes/BrutalForceRune.cs:3|
|BrutalForceRune|[blue]1[/blue]|[blue]{StrengthPower}[/blue]|src/Runes/BrutalForceRune.cs:3|
|BurningInterestRune|[blue]1[/blue]|[blue]{HextechBurnPower}[/blue]|src/Runes/BurningInterestRune.cs:8|
|BurningInterestRune|[blue]1[/blue]|[blue]{HextechBurnPower}[/blue]|src/Runes/BurningInterestRune.cs:8|
|BurningInterestRune|[blue]2[/blue]|[blue]{CountPerDamage}[/blue]|src/Runes/BurningInterestRune.cs:8|
|ByproductRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/ByproductRune.cs:3|
|CantTouchThisRune|[blue]2[/blue]|[blue]{MinCost}[/blue]|src/Runes/CantTouchThisRune.cs:3|
|CantTouchThisRune|[blue]1[/blue]|[blue]{BufferPower}[/blue]|src/Runes/CantTouchThisRune.cs:3|
|CarefulSelectionRune|[blue]4[/blue]|[blue]{Cards}[/blue]|src/Runes/CarefulSelectionRune.cs:3|
|CatalystRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/CatalystRune.cs:3|
|ChainInSleeveRune|[blue]3[/blue]|[blue]{ShivsNeeded}[/blue]|src/Runes/ChainInSleeveRune.cs:3|
|ChainInSleeveRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/ChainInSleeveRune.cs:3|
|ClownCollegeRune|[blue]3[/blue]|[blue]{Cards}[/blue]|src/Runes/ClownCollegeRune.cs:3|
|CollectorRune|[blue]20[/blue]|[blue]{CountPerExecute}[/blue]|src/Runes/CollectorRune.cs:3|
|ColorDiscoveryRune|[blue]3[/blue]|[blue]{Cards}[/blue]|src/Runes/ColorDiscoveryRune.cs:6|
|ColorDiscoveryRune|[blue]1[/blue]|[blue]{Selection}[/blue]|src/Runes/ColorDiscoveryRune.cs:6|
|CoreOverloadRune|[blue]1[/blue]|[blue]{FocusPower}[/blue]|src/Runes/CoreOverloadRune.cs:3|
|CorruptedBranchRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/CorruptedBranchRune.cs:5|
|CorruptedBranchRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/CorruptedBranchRune.cs:5|
|CorruptedBranchRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/CorruptedBranchRune.cs:5|
|CourageOfColossusRune|[blue]3[/blue]|[blue]{Plating}[/blue]|src/Runes/CourageOfColossusRune.cs:3|
|CurtainCallRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/CurtainCallRune.cs:3|
|DawnbringersResolveRune|[blue]50%[/blue]|[blue]{ThresholdPercent}%[/blue]|src/Runes/DawnbringersResolveRune.cs:5|
|DevilsDanceRune|[blue]3[/blue]|[blue]{AttacksPerMaxHp}[/blue]|src/Runes/DevilsDanceRune.cs:3|
|DevilsDanceRune|[blue]1[/blue]|[blue]{MaxHp}[/blue]|src/Runes/DevilsDanceRune.cs:3|
|DexterityStrengthToFocusRune|[blue]1[/blue]|[blue]{FocusPower}[/blue]|src/Runes/DexterityStrengthToFocusRune.cs:3|
|DexterityToStrengthRune|[blue]1[/blue]|[blue]{StrengthPower}[/blue]|src/Runes/DexterityToStrengthRune.cs:3|
|DiceManiacRune|[blue]50%[/blue]|[blue]{DropChance}%[/blue]|src/Runes/DiceManiacRune.cs:3|
|DieForYouRune|[blue]5[/blue]|[blue]{Summon}[/blue]|src/Runes/DieForYouRune.cs:5|
|DieForYouRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/DieForYouRune.cs:5|
|DivineInterventionRune|[blue]3[/blue]|[blue]{TurnsNeeded}[/blue]|src/Runes/DivineInterventionRune.cs:3|
|DivineInterventionRune|[blue]1[/blue]|[blue]{IntangiblePower}[/blue]|src/Runes/DivineInterventionRune.cs:3|
|DizzySpinningRune|[blue]2[/blue]|[blue]{Cards}[/blue]|src/Runes/DizzySpinningRune.cs:3|
|DonationRune|[blue]1000[/blue]|[blue]{Gold}[/blue]|src/Runes/DonationRune.cs:3|
|DuffsVintageRune|[blue]1[/blue]|[blue]{CostReduction}[/blue]|src/Runes/DuffsVintageRune.cs:3|
|EarthAwakensRune|[blue]5[/blue]|[blue]{RollingBoulderPower}[/blue]|src/Runes/EarthAwakensRune.cs:3|
|EightPennyGateRune|[blue]1[/blue]|[blue]{Replays}[/blue]|src/Runes/EightPennyGateRune.cs:3|
|ElectricSurgeRune|[blue]1[/blue]|[blue]{OrbCount}[/blue]|src/Runes/ElectricSurgeRune.cs:3|
|ElectrodynamicsRune|[blue]1[/blue]|[blue]{OrbCount}[/blue]|src/Runes/ElectrodynamicsRune.cs:9|
|EmergenceRune|[blue]2[/blue]|[blue]{OrbCount}[/blue]|src/Runes/EmergenceRune.cs:3|
|EndlessRecoveryRune|[blue]10%[/blue]|[blue]{HealPercent}%[/blue]|src/Runes/EndlessRecoveryRune.cs:3|
|EscapePlanRune|[blue]50%[/blue]|[blue]{ThresholdPercent}%[/blue]|src/Runes/EscapePlanRune.cs:5|
|EscapePlanRune|[blue]60%[/blue]|[blue]{BlockPercent}%[/blue]|src/Runes/EscapePlanRune.cs:5|
|EurekaRune|[blue]6[/blue]|[blue]{RelicsNeeded}[/blue]|src/Runes/EurekaRune.cs:3|
|EurekaRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/EurekaRune.cs:3|
|ExplosionArtRune|[blue]1[/blue]|[blue]{TurnStartCards}[/blue]|src/Runes/ExplosionArtRune.cs:3|
|FallingStarUpgradeRune|[blue]1[/blue]|[blue]{StunTurns}[/blue]|src/Runes/FallingStarUpgradeRune.cs:3|
|FanTheHammerRune|[blue]3[/blue]|[blue]{Replays}[/blue]|src/Runes/FanTheHammerRune.cs:3|
|FeelTheBurnRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/FeelTheBurnRune.cs:3|
|FeyMagicRune|[blue]3[/blue]|[blue]{MinCost}[/blue]|src/Runes/FeyMagicRune.cs:3|
|FlawlessRune|[blue]3[/blue]|[blue]{Block}[/blue]|src/Runes/FlawlessRune.cs:3|
|FleshAndBoneRune|[blue]3[/blue]|[blue]{HpLoss}[/blue]|src/Runes/FleshAndBoneRune.cs:3|
|FleshAndBoneRune|[blue]15[/blue]|[blue]{Summon}[/blue]|src/Runes/FleshAndBoneRune.cs:3|
|FleshAndBoneRune|[blue]10[/blue]|[blue]{OstyMaxHpPerHeal}[/blue]|src/Runes/FleshAndBoneRune.cs:3|
|FleshAndBoneRune|[blue]1[/blue]|[blue]{OstyHeal}[/blue]|src/Runes/FleshAndBoneRune.cs:3|
|FlyingKickRune|[blue]8%[/blue]|[blue]{OwnerMaxHpToExecutePercent}%[/blue]|src/Runes/FlyingKickRune.cs:6|
|ForbiddenGrimoireRune|[blue]1[/blue]|[blue]{ForbiddenGrimoirePower}[/blue]|src/Runes/ForbiddenGrimoireRune.cs:3|
|FrostWraithRune|[blue]2[/blue]|[blue]{TurnsNeeded}[/blue]|src/Runes/FrostWraithRune.cs:3|
|GalacticGiftRune|[blue]1[/blue]|[blue]{Stars}[/blue]|src/Runes/GalacticGiftRune.cs:3|
|GiantSerpentsFangRune|[blue]50%[/blue]|[blue]{BlockReductionPercent}%[/blue]|src/Runes/GiantSerpentsFangRune.cs:3|
|GiantSlayerRune|[blue]2[/blue]|[blue]{Cards}[/blue]|src/Runes/GiantSlayerRune.cs:3|
|GiantSlayerRune|[blue]8[/blue]|[blue]{EnemyMaxHpPerPercent}[/blue]|src/Runes/GiantSlayerRune.cs:3|
|GoldenSpatulaRune|[blue]1[/blue]|[blue]{StackBonusPercent}[/blue]|src/Runes/GoldenSpatulaRune.cs:3|
|GoldenSpatulaRune|[blue]1%[/blue]|[blue]{StackBonusPercent}%[/blue]|src/Runes/GoldenSpatulaRune.cs:3|
|GoldenSpatulaRune|[blue]10[/blue]|[blue]{StackOverloadThreshold}[/blue]|src/Runes/GoldenSpatulaRune.cs:3|
|GoldrendRune|[blue]10[/blue]|[blue]{CountPerHit}[/blue]|src/Runes/GoldrendRune.cs:3|
|HailToTheKingRune|[blue]2[/blue]|[blue]{InitialForgeCount}[/blue]|src/Runes/HailToTheKingRune.cs:3|
|HandOfBaronRune|[blue]2[/blue]|[blue]{Shrink}[/blue]|src/Runes/HandOfBaronRune.cs:3|
|HappyAccidentRune|[blue]1[/blue]|[blue]{OrbCount}[/blue]|src/Runes/HappyAccidentRune.cs:3|
|HappyAccidentRune|[blue]1[/blue]|[blue]{OrbCount}[/blue]|src/Runes/HappyAccidentRune.cs:3|
|HardBonesRune|[blue]8[/blue]|[blue]{CalcifyPower}[/blue]|src/Runes/HardBonesRune.cs:3|
|HubrisRune|[blue]3[/blue]|[blue]{StacksPerBonus}[/blue]|src/Runes/HubrisRune.cs:3|
|IllusoryWeaponRune|[blue]2[/blue]|[blue]{Damage}[/blue]|src/Runes/IllusoryWeaponRune.cs:8|
|ImmortalBoneRune|[blue]50%[/blue]|[blue]{HealPercent}%[/blue]|src/Runes/ImmortalBoneRune.cs:3|
|InfiniteLoopRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/InfiniteLoopRune.cs:3|
|InfiniteLoopRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/InfiniteLoopRune.cs:3|
|InfiniteLoopRune|[blue]4[/blue]|[blue]{StacksPerEnergy}[/blue]|src/Runes/InfiniteLoopRune.cs:3|
|InfiniteLoopRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/InfiniteLoopRune.cs:3|
|KillerHunterRune|[blue]1[/blue]|[blue]{TemporaryStatLoss}[/blue]|src/Runes/KillerHunterRune.cs:3|
|KillerHunterRune|[blue]1[/blue]|[blue]{TemporaryStatLoss}[/blue]|src/Runes/KillerHunterRune.cs:3|
|LethalTempoRune|[blue]1[/blue]|[blue]{StrengthPower}[/blue]|src/Runes/LethalTempoRune.cs:3|
|LethalTempoRune|[blue]1[/blue]|[blue]{StrengthPower}[/blue]|src/Runes/LethalTempoRune.cs:3|
|LifeFlowRune|[blue]3[/blue]|[blue]{MaxProcsPerTurn}[/blue]|src/Runes/LifeFlowRune.cs:3|
|LoopRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/LoopRune.cs:3|
|MadScientistRune|[blue]1[/blue]|[blue]{OrbSlots}[/blue]|src/Runes/MadScientistRune.cs:9|
|MadScientistRune|[blue]1[/blue]|[blue]{OrbSlots}[/blue]|src/Runes/MadScientistRune.cs:9|
|MakeItMineRune|[blue]4[/blue]|[blue]{Summon}[/blue]|src/Runes/MakeItMineRune.cs:3|
|MentalShieldRune|[blue]2[/blue]|[blue]{Block}[/blue]|src/Runes/MentalShieldRune.cs:3|
|MindOverMatterRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/MindOverMatterRune.cs:3|
|MindToMatterRune|[blue]1[/blue]|[blue]{MaxHp}[/blue]|src/Runes/MindToMatterRune.cs:3|
|MindToMatterRune|[blue]1[/blue]|[blue]{MaxHp}[/blue]|src/Runes/MindToMatterRune.cs:3|
|MonarchsGazeRune|[blue]1[/blue]|[blue]{MonarchsGazePower}[/blue]|src/Runes/MonarchsGazeRune.cs:3|
|MoreTheMerrierRune|[blue]1.5%[/blue]|[blue]{PercentPerRelic}%[/blue]|src/Runes/MoreTheMerrierRune.cs:3|
|NatureIsHealingRune|[blue]10[/blue]|[blue]{IntervalSeconds}[/blue]|src/Runes/NatureIsHealingRune.cs:6|
|NatureIsHealingRune|[blue]1[/blue]|[blue]{Heal}[/blue]|src/Runes/NatureIsHealingRune.cs:6|
|NearDeathFeastRune|[blue]1[/blue]|[blue]{StrengthPerNegativeHp}[/blue]|src/Runes/NearDeathFeastRune.cs:6|
|NearDeathFeastRune|[blue]1[/blue]|[blue]{StrengthPerNegativeHp}[/blue]|src/Runes/NearDeathFeastRune.cs:6|
|NearDeathFeastRune|[blue]50%[/blue]|[blue]{DeathNegativeMaxHpPercent}%[/blue]|src/Runes/NearDeathFeastRune.cs:6|
|NeowsGrudgeRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/NeowsGrudgeRune.cs:3|
|NeutralizeUpgradeRune|[blue]2[/blue]|[blue]{Repeats}[/blue]|src/Runes/NeutralizeUpgradeRune.cs:3|
|NightstalkingRune|[blue]15[/blue]|[blue]{CardsNeeded}[/blue]|src/Runes/NightstalkingRune.cs:3|
|NightstalkingRune|[blue]1[/blue]|[blue]{IntangiblePower}[/blue]|src/Runes/NightstalkingRune.cs:3|
|NimbleRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/NimbleRune.cs:3|
|NineDragonPowerRune|[blue]1[/blue]|[blue]{RegenPower}[/blue]|src/Runes/NineDragonPowerRune.cs:3|
|NineDragonPowerRune|[blue]1[/blue]|[blue]{RegenPower}[/blue]|src/Runes/NineDragonPowerRune.cs:3|
|NineDragonPowerRune|[blue]3%[/blue]|[blue]{StackBonusPercent}%[/blue]|src/Runes/NineDragonPowerRune.cs:3|
|OkBoomerangRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/OkBoomerangRune.cs:3|
|OmegaRune|[blue]4[/blue]|[blue]{StartTurn}[/blue]|src/Runes/OmegaRune.cs:3|
|OmegaRune|[blue]50[/blue]|[blue]{Damage}[/blue]|src/Runes/OmegaRune.cs:3|
|OmniDragonSoulRune|[blue]3[/blue]|[blue]{Cards}[/blue]|src/Runes/OmniDragonSoulRune.cs:3|
|OrbSymbiosisRune|[blue]1[/blue]|[blue]{OrbCount}[/blue]|src/Runes/OrbSymbiosisRune.cs:3|
|OrbSymbiosisRune|[blue]1[/blue]|[blue]{OrbCount}[/blue]|src/Runes/OrbSymbiosisRune.cs:3|
|OverflowRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/OverflowRune.cs:3|
|OverflowRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/OverflowRune.cs:3|
|OverlordBloodArmorRune|[blue]40[/blue]|[blue]{MaxHpPerStrength}[/blue]|src/Runes/OverlordBloodArmorRune.cs:3|
|OverlordBloodArmorRune|[blue]1[/blue]|[blue]{StrengthPower}[/blue]|src/Runes/OverlordBloodArmorRune.cs:3|
|PiggyBankRune|[blue]200[/blue]|[blue]{Gold}[/blue]|src/Runes/PiggyBankRune.cs:3|
|PiggyBankRune|[blue]20[/blue]|[blue]{CounterGain}[/blue]|src/Runes/PiggyBankRune.cs:3|
|PlateletRune|[blue]3[/blue]|[blue]{Block}[/blue]|src/Runes/PlateletRune.cs:3|
|PorcupineRune|[blue]5[/blue]|[blue]{BlockPerThorn}[/blue]|src/Runes/PorcupineRune.cs:3|
|PrecisionCognitionRune|[blue]2[/blue]|[blue]{FocusPower}[/blue]|src/Runes/PrecisionCognitionRune.cs:3|
|ProtectiveVeilRune|[blue]1[/blue]|[blue]{ArtifactPower}[/blue]|src/Runes/ProtectiveVeilRune.cs:3|
|ProtectiveVeilRune|[blue]1[/blue]|[blue]{ArtifactPower}[/blue]|src/Runes/ProtectiveVeilRune.cs:3|
|ProteinShakeRune|[blue]2[/blue]|[blue]{MaxHpPerStep}[/blue]|src/Runes/ProteinShakeRune.cs:3|
|ProteinShakeRune|[blue]1%[/blue]|[blue]{SustainPercentPerStep}%[/blue]|src/Runes/ProteinShakeRune.cs:3|
|RekindleRune|[blue]2[/blue]|[blue]{Cards}[/blue]|src/Runes/RekindleRune.cs:3|
|RekindleRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/RekindleRune.cs:3|
|RenewalRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/RenewalRune.cs:3|
|RenewalRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/RenewalRune.cs:3|
|RepulsorRune|[blue]2[/blue]|[blue]{SlipperyPower}[/blue]|src/Runes/RepulsorRune.cs:5|
|RoyalCommandRune|[blue]3[/blue]|[blue]{ForgeAmount}[/blue]|src/Runes/RoyalCommandRune.cs:3|
|RoyalTrialRune|[blue]2[/blue]|[blue]{Cards}[/blue]|src/Runes/RoyalTrialRune.cs:3|
|SacrificeRune|[blue]5[/blue]|[blue]{CountPerEnemy}[/blue]|src/Runes/SacrificeRune.cs:3|
|SearingAttackRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/SearingAttackRune.cs:5|
|SendThemInRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/SendThemInRune.cs:3|
|SerpentsFangRune|[blue]4[/blue]|[blue]{PoisonPerHit}[/blue]|src/Runes/SerpentsFangRune.cs:3|
|ServantMasterRune|[blue]1[/blue]|[blue]{NecroMasteryPower}[/blue]|src/Runes/ServantMasterRune.cs:3|
|ServantMasterRune|[blue]3[/blue]|[blue]{Summon}[/blue]|src/Runes/ServantMasterRune.cs:3|
|ShoulderVakuRune|[blue]5%[/blue]|[blue]{HealPercent}%[/blue]|src/Runes/ShoulderVakuRune.cs:5|
|ShrinkRayRune|[blue]1[/blue]|[blue]{ShrinkPower}[/blue]|src/Runes/ShrinkRayRune.cs:3|
|SingularityAIRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/SingularityAIRune.cs:3|
|SlapRune|[blue]1[/blue]|[blue]{StrengthPower}[/blue]|src/Runes/SlapRune.cs:3|
|SlowCookRune|[blue]5%[/blue]|[blue]{BurnPercent}%[/blue]|src/Runes/SlowCookRune.cs:3|
|SnakebiteRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/SnakebiteRune.cs:3|
|SnakebiteUpgradeRune|[blue]1[/blue]|[blue]{Replays}[/blue]|src/Runes/SnakebiteUpgradeRune.cs:3|
|SomethingFromNothingRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/SomethingFromNothingRune.cs:3|
|SonataRune|[blue]1[/blue]|[blue]{Heal}[/blue]|src/Runes/SonataRune.cs:3|
|SoulCallingRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/SoulCallingRune.cs:3|
|SoulUpgradeRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/SoulUpgradeRune.cs:4|
|StarlightSplendorRune|[blue]2[/blue]|[blue]{Stars}[/blue]|src/Runes/StarlightSplendorRune.cs:3|
|StartupRoutineRune|[blue]16[/blue]|[blue]{Block}[/blue]|src/Runes/StartupRoutineRune.cs:3|
|StatsOnStatsOnStatsRune|[blue]6[/blue]|[blue]{ForgeCount}[/blue]|src/Runes/StatsOnStatsOnStatsRune.cs:3|
|StatsOnStatsRune|[blue]4[/blue]|[blue]{ForgeCount}[/blue]|src/Runes/StatsOnStatsRune.cs:3|
|StatsRune|[blue]2[/blue]|[blue]{ForgeCount}[/blue]|src/Runes/StatsRune.cs:3|
|StrengthToDexterityRune|[blue]1[/blue]|[blue]{DexterityPower}[/blue]|src/Runes/StrengthToDexterityRune.cs:3|
|SturdyRune|[blue]2%[/blue]|[blue]{HealPercent}%[/blue]|src/Runes/SturdyRune.cs:3|
|SturdyRune|[blue]50%[/blue]|[blue]{LowHpThresholdPercent}%[/blue]|src/Runes/SturdyRune.cs:3|
|SturdyRune|[blue]5%[/blue]|[blue]{LowHpHealPercent}%[/blue]|src/Runes/SturdyRune.cs:3|
|SummonForthRune|[blue]5[/blue]|[blue]{ForgeAmount}[/blue]|src/Runes/SummonForthRune.cs:3|
|SwiftAndSafeRune|[blue]10[/blue]|[blue]{CardsNeeded}[/blue]|src/Runes/SwiftAndSafeRune.cs:3|
|SwiftAndSafeRune|[blue]1[/blue]|[blue]{ArtifactPower}[/blue]|src/Runes/SwiftAndSafeRune.cs:3|
|SwordsmanshipRune|[blue]12[/blue]|[blue]{ParryPower}[/blue]|src/Runes/SwordsmanshipRune.cs:3|
|SymphonyOfWarRune|[blue]4[/blue]|[blue]{SerpentFormPower}[/blue]|src/Runes/SymphonyOfWarRune.cs:5|
|SymphonyOfWarRune|[blue]1[/blue]|[blue]{DemonFormPower}[/blue]|src/Runes/SymphonyOfWarRune.cs:5|
|TankEngineRune|[blue]6%[/blue]|[blue]{ScalePercent}%[/blue]|src/Runes/TankEngineRune.cs:3|
|TankEngineRune|[blue]6%[/blue]|[blue]{ScalePercent}%[/blue]|src/Runes/TankEngineRune.cs:3|
|TanksShieldRune|[blue]3[/blue]|[blue]{Block}[/blue]|src/Runes/TanksShieldRune.cs:3|
|TauntRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/TauntRune.cs:3|
|TezcatarasMercyRune|[blue]1[/blue]|[blue]{Relics}[/blue]|src/Runes/TezcatarasMercyRune.cs:5|
|TezcatarasMercyRune|[blue]3[/blue]|[blue]{CombatInterval}[/blue]|src/Runes/TezcatarasMercyRune.cs:5|
|ThornmailRune|[blue]20[/blue]|[blue]{MaxHpPerThorns}[/blue]|src/Runes/ThornmailRune.cs:3|
|ThornmailRune|[blue]1[/blue]|[blue]{ThornsPower}[/blue]|src/Runes/ThornmailRune.cs:3|
|ThoughtOverwriteRune|[blue]1[/blue]|[blue]{Replays}[/blue]|src/Runes/ThoughtOverwriteRune.cs:5|
|TranscendentEvilRune|[blue]4[/blue]|[blue]{StacksPerBonus}[/blue]|src/Runes/TranscendentEvilRune.cs:3|
|UnsealedThroneRune|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Runes/UnsealedThroneRune.cs:3|
|ViolenceRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/ViolenceRune.cs:3|
|VitalitySurgeRune|[blue]50[/blue]|[blue]{HpPerCard}[/blue]|src/Runes/VitalitySurgeRune.cs:4|
|VitalitySurgeRune|[blue]100[/blue]|[blue]{HpPerEnergy}[/blue]|src/Runes/VitalitySurgeRune.cs:4|
|WarmogsSpiritRune|[blue]8[/blue]|[blue]{CardsNeeded}[/blue]|src/Runes/WarmogsSpiritRune.cs:3|
|WarmogsSpiritRune|[blue]1[/blue]|[blue]{PlatingPower}[/blue]|src/Runes/WarmogsSpiritRune.cs:3|
|WhiteHoleRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/WhiteHoleRune.cs:3|
|WraithRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/WraithRune.cs:5|
|WraithRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/WraithRune.cs:5|
|WraithRune|[blue]3%[/blue]|[blue]{DamagePercentPerSoul}%[/blue]|src/Runes/WraithRune.cs:5|
|ZealotRune|[blue]5[/blue]|[blue]{RelicsNeeded}[/blue]|src/Runes/ZealotRune.cs:3|
|ZealotRune|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Runes/ZealotRune.cs:3|
|GoldLifeForge|[blue]20[/blue]|[blue]{MaxHp}[/blue]|src/Forges/HextechForges.Gold.cs:59|
|GoldHpForge|[blue]15%[/blue]|[blue]{MaxHpPercent}%[/blue]|src/Forges/HextechForges.Gold.cs:81|
|GoldFocusForge|[blue]1[/blue]|[blue]{FocusPower}[/blue]|src/Forges/HextechForges.Gold.cs:153|
|DrawForge|[blue]1[/blue]|[blue]{Cards}[/blue]|src/Forges/HextechForges.Gold.cs:182|
|RecoveryForge|[blue]1[/blue]|[blue]{Heal}[/blue]|src/Forges/HextechForges.Gold.cs:195|
|HourglassForge|[blue]5[/blue]|[blue]{Damage}[/blue]|src/Forges/HextechForges.Gold.cs:214|
|GoldUpgradeForge|[blue]4[/blue]|[blue]{Cards}[/blue]|src/Forges/HextechForges.Gold.cs:244|
|SummonForge|[blue]2[/blue]|[blue]{Summon}[/blue]|src/Forges/HextechForges.Gold.cs:285|
|FleshForge|[blue]4[/blue]|[blue]{SleightOfFleshPower}[/blue]|src/Forges/HextechForges.Gold.cs:312|
|StarsForge|[blue]1[/blue]|[blue]{Stars}[/blue]|src/Forges/HextechForges.Gold.cs:336|
|OrbSlotForge|[blue]2[/blue]|[blue]{OrbSlots}[/blue]|src/Forges/HextechForges.Gold.cs:360|
|VenomForge|[blue]1[/blue]|[blue]{EnvenomPower}[/blue]|src/Forges/HextechForges.Gold.cs:384|
|ShrinkForge|[blue]2[/blue]|[blue]{ShrinkPower}[/blue]|src/Forges/HextechForges.Gold.cs:408|
|PlatingForge|[blue]6[/blue]|[blue]{PlatingPower}[/blue]|src/Forges/HextechForges.Gold.cs:440|
|ThornsForge|[blue]4[/blue]|[blue]{ThornsPower}[/blue]|src/Forges/HextechForges.Gold.cs:464|
|ArtifactForge|[blue]1[/blue]|[blue]{ArtifactPower}[/blue]|src/Forges/HextechForges.Gold.cs:488|
|PrismaticLifeForge|[blue]30%[/blue]|[blue]{MaxHpPercent}%[/blue]|src/Forges/HextechForges.Prismatic.cs:3|
|EnergyForge|[blue]1[/blue]|[blue]{Energy}[/blue]|src/Forges/HextechForges.Prismatic.cs:75|
|RitualForge|[blue]1[/blue]|[blue]{RitualPower}[/blue]|src/Forges/HextechForges.Prismatic.cs:94|
|RegenForge|[blue]4[/blue]|[blue]{RegenPower}[/blue]|src/Forges/HextechForges.Prismatic.cs:118|
|BufferForge|[blue]1[/blue]|[blue]{BufferPower}[/blue]|src/Forges/HextechForges.Prismatic.cs:142|
|SlipperyForge|[blue]2[/blue]|[blue]{SlipperyPower}[/blue]|src/Forges/HextechForges.Prismatic.cs:166|
|PrismaticArtifactForge|[blue]2[/blue]|[blue]{ArtifactPower}[/blue]|src/Forges/HextechForges.Prismatic.cs:190|
|FortuneForge|[blue]100[/blue]|[blue]{Gold}[/blue]|src/Forges/HextechForges.Prismatic.cs:214|
|VoidForge|[blue]1[/blue]|[blue]{VoidFormPower}[/blue]|src/Forges/HextechForges.Prismatic.cs:239|
|StrengthForge|[blue]1[/blue]|[blue]{StrengthPower}[/blue]|src/Forges/HextechForges.Silver.cs:3|
|DexterityForge|[blue]1[/blue]|[blue]{DexterityPower}[/blue]|src/Forges/HextechForges.Silver.cs:22|
|SilverPlatingForge|[blue]4[/blue]|[blue]{PlatingPower}[/blue]|src/Forges/HextechForges.Silver.cs:41|
|UpgradeForge|[blue]2[/blue]|[blue]{Cards}[/blue]|src/Forges/HextechForges.Silver.cs:65|
|FocusForge|[blue]2[/blue]|[blue]{FocusPower}[/blue]|src/Forges/HextechForges.Silver.cs:110|
|LifeForge|[blue]8[/blue]|[blue]{MaxHp}[/blue]|src/Forges/HextechForges.Silver.cs:139|
|SilverHpForge|[blue]7.5%[/blue]|[blue]{MaxHpPercent}%[/blue]|src/Forges/HextechForges.Silver.cs:161|
|PocketForge|[blue]2[/blue]|[blue]{PotionSlots}[/blue]|src/Forges/HextechForges.Silver.cs:233|
|PreparedForge|[blue]2[/blue]|[blue]{Cards}[/blue]|src/Forges/HextechForges.Silver.cs:284|
|FireworksForge|[blue]6[/blue]|[blue]{Damage}[/blue]|src/Forges/HextechForges.Silver.cs:302|
|VigorForge|[blue]2[/blue]|[blue]{VigorPower}[/blue]|src/Forges/HextechForges.Silver.cs:332|
|BlockForge|[blue]3[/blue]|[blue]{Block}[/blue]|src/Forges/HextechForges.Silver.cs:356|
|NecrobinderForge|[blue]6[/blue]|[blue]{Summon}[/blue]|src/Forges/HextechForges.Silver.cs:375|
|SilverStarsForge|[blue]2[/blue]|[blue]{Stars}[/blue]|src/Forges/HextechForges.Silver.cs:403|
|SilverOrbForge|[blue]2[/blue]|[blue]{OrbCount}[/blue]|src/Forges/HextechForges.Silver.cs:427|
|ForgingForge|[blue]8[/blue]|[blue]{ForgeAmount}[/blue]|src/Forges/HextechForges.Silver.cs:456|
|UniversalSpiral|[blue]1[/blue]|[blue]{Times}[/blue]|src/Enchantments/UniversalSpiral.cs:3|

## 九语言格式问题

|语言|表.键|占位符现值→zhs|标签现值→zhs|
|---|---|---|---|
|rus|powers.HEXTECH_INFERNAL_DRAGON_SOUL_POWER.description|['BurnPower'] → []|{'blue': 1, '/blue': 1, 'gold': 1, '/gold': 1} → {'gold': 1, '/gold': 1}|
|rus|powers.HEXTECH_CLOUD_DRAGON_SOUL_POWER.smartDescription|['Amount', 'Cards'] → ['Amount']|{'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|rus|cards.HEXTECH_DRAGON_SOUL_CARD.description|['Energy'] → ['Energy']|{} → {'blue': 1, '/blue': 1}|
|rus|cards.OSTY_WISH_CARD.description|['WishBlock', 'WishDamage'] → ['WishBlock', 'WishDamage']|{'gold': 1, '/gold': 1} → {}|
|kor|cards.OSTY_WISH_CARD.hoverTip|[] → []|{'blue': 4, '/blue': 4, 'gold': 2, '/gold': 2} → {'blue': 4, '/blue': 4}|
|rus|cards.OSTY_WISH_CARD.hoverTip|[] → []|{'blue': 4, '/blue': 4, 'gold': 1, '/gold': 1} → {'blue': 4, '/blue': 4}|
|rus|modifiers.HEXTECH_MAYHEM.description|[] → []|{} → {'blue': 3, '/blue': 3}|
|kor|relics.MOLTEN_FIST_UPGRADE_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2}|
|eng|relics.JUDICATOR_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|esp|relics.JUDICATOR_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|spa|relics.JUDICATOR_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|jpn|relics.JUDICATOR_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.JUDICATOR_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|ptb|relics.JUDICATOR_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|rus|relics.JUDICATOR_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|tha|relics.JUDICATOR_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.SLAP_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2} → {'blue': 2, '/blue': 2}|
|kor|relics.DEXTERITY_TO_STRENGTH_RUNE.description|[] → []|{'gold': 3, '/gold': 3, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|rus|relics.DEXTERITY_TO_STRENGTH_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.STRENGTH_TO_DEXTERITY_RUNE.description|[] → []|{'gold': 3, '/gold': 3, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|rus|relics.STRENGTH_TO_DEXTERITY_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.DEXTERITY_STRENGTH_TO_FOCUS_RUNE.description|[] → []|{'gold': 4, '/gold': 4, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|rus|relics.DEXTERITY_STRENGTH_TO_FOCUS_RUNE.description|[] → []|{'blue': 3, '/blue': 3, 'gold': 1, '/gold': 1} → {'blue': 1, '/blue': 1}|
|eng|relics.SUPER_BRAIN_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|esp|relics.SUPER_BRAIN_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|spa|relics.SUPER_BRAIN_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|jpn|relics.SUPER_BRAIN_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.SUPER_BRAIN_RUNE.description|[] → []|{'blue': 1, '/blue': 1, 'gold': 1, '/gold': 1} → {'blue': 2, '/blue': 2}|
|ptb|relics.SUPER_BRAIN_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|rus|relics.SUPER_BRAIN_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|tha|relics.SUPER_BRAIN_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|eng|relics.MIND_TO_MATTER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|esp|relics.MIND_TO_MATTER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|spa|relics.MIND_TO_MATTER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|jpn|relics.MIND_TO_MATTER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.MIND_TO_MATTER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|ptb|relics.MIND_TO_MATTER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|rus|relics.MIND_TO_MATTER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|tha|relics.MIND_TO_MATTER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.COURAGE_OF_COLOSSUS_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2} → {'blue': 2, '/blue': 2}|
|eng|relics.INFERNAL_CONDUIT_RUNE.description|[] → []|{'blue': 3, '/blue': 3, 'gold': 1, '/gold': 1} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|esp|relics.INFERNAL_CONDUIT_RUNE.description|[] → []|{'blue': 3, '/blue': 3, 'gold': 1, '/gold': 1} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|spa|relics.INFERNAL_CONDUIT_RUNE.description|[] → []|{'blue': 3, '/blue': 3, 'gold': 1, '/gold': 1} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|jpn|relics.INFERNAL_CONDUIT_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 3, '/blue': 3} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|kor|relics.INFERNAL_CONDUIT_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 3, '/blue': 3} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|ptb|relics.INFERNAL_CONDUIT_RUNE.description|[] → []|{'blue': 3, '/blue': 3, 'gold': 1, '/gold': 1} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|rus|relics.INFERNAL_CONDUIT_RUNE.description|[] → []|{'blue': 3, '/blue': 3, 'gold': 1, '/gold': 1} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|tha|relics.INFERNAL_CONDUIT_RUNE.description|[] → []|{'blue': 3, '/blue': 3, 'gold': 1, '/gold': 1} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|kor|relics.MASTER_OF_DUALITY_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'blue': 2, '/blue': 2}|
|eng|relics.SACRIFICE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|esp|relics.SACRIFICE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|spa|relics.SACRIFICE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|jpn|relics.SACRIFICE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|kor|relics.SACRIFICE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|ptb|relics.SACRIFICE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|rus|relics.SACRIFICE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|tha|relics.SACRIFICE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|eng|relics.DRAW_YOUR_SWORD_RUNE.description|['FocusPower'] → ['FocusPower']|{'gold': 5, '/gold': 5, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1, 'gold': 4, '/gold': 4}|
|esp|relics.DRAW_YOUR_SWORD_RUNE.description|['FocusPower'] → ['FocusPower']|{'gold': 5, '/gold': 5, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1, 'gold': 4, '/gold': 4}|
|spa|relics.DRAW_YOUR_SWORD_RUNE.description|['FocusPower'] → ['FocusPower']|{'gold': 5, '/gold': 5, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1, 'gold': 4, '/gold': 4}|
|jpn|relics.DRAW_YOUR_SWORD_RUNE.description|['FocusPower'] → ['FocusPower']|{'gold': 5, '/gold': 5, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1, 'gold': 4, '/gold': 4}|
|kor|relics.DRAW_YOUR_SWORD_RUNE.description|['FocusPower'] → ['FocusPower']|{'gold': 5, '/gold': 5, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1, 'gold': 4, '/gold': 4}|
|ptb|relics.DRAW_YOUR_SWORD_RUNE.description|['FocusPower'] → ['FocusPower']|{'gold': 5, '/gold': 5, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1, 'gold': 4, '/gold': 4}|
|rus|relics.DRAW_YOUR_SWORD_RUNE.description|['FocusPower'] → ['FocusPower']|{'gold': 5, '/gold': 5, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1, 'gold': 4, '/gold': 4}|
|tha|relics.DRAW_YOUR_SWORD_RUNE.description|['FocusPower'] → ['FocusPower']|{'gold': 5, '/gold': 5, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1, 'gold': 4, '/gold': 4}|
|kor|relics.STRENGTH_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.DEXTERITY_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.SILVER_PLATING_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.FOCUS_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.SILVER_STARS_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.CONSTITUTION_FORGE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|eng|relics.DISASTER_FORGE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|esp|relics.DISASTER_FORGE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|spa|relics.DISASTER_FORGE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|jpn|relics.DISASTER_FORGE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|ptb|relics.DISASTER_FORGE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|rus|relics.DISASTER_FORGE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|tha|relics.DISASTER_FORGE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|kor|relics.GOLD_FOCUS_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.STARS_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.PLATING_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.THORNS_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.ARTIFACT_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.RITUAL_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.REGEN_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.BUFFER_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.SLIPPERY_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.PRISMATIC_ARTIFACT_FORGE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|esp|relics.RANDOM_FORGE_SHOP_RELIC.description|[] → ['Price']|{'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|kor|relics.TRANSCENDENT_EVIL_RUNE.description|[] → []|{'blue': 4, '/blue': 4, 'gold': 1, '/gold': 1} → {'blue': 4, '/blue': 4}|
|kor|relics.WIZARDLY_THINKING_RUNE.description|[] → []|{'gold': 1, '/gold': 1} → {}|
|eng|relics.TAP_DANCE_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|esp|relics.TAP_DANCE_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|spa|relics.TAP_DANCE_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|jpn|relics.TAP_DANCE_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.TAP_DANCE_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|ptb|relics.TAP_DANCE_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|rus|relics.TAP_DANCE_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|tha|relics.TAP_DANCE_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|eng|relics.BADGE_BROTHERS_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|esp|relics.BADGE_BROTHERS_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|spa|relics.BADGE_BROTHERS_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|jpn|relics.BADGE_BROTHERS_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|kor|relics.BADGE_BROTHERS_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|ptb|relics.BADGE_BROTHERS_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|rus|relics.BADGE_BROTHERS_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|tha|relics.BADGE_BROTHERS_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2}|
|kor|relics.CUTTING_EDGE_ALCHEMIST_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2}|
|kor|relics.LIFE_FLOW_RUNE.description|[] → []|{'blue': 3, '/blue': 3, 'gold': 1, '/gold': 1} → {'blue': 3, '/blue': 3}|
|kor|relics.GALACTIC_GIFT_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'blue': 2, '/blue': 2}|
|eng|relics.LUBRICANT_RUNE.description|[] → []|{} → {'blue': 1, '/blue': 1}|
|esp|relics.LUBRICANT_RUNE.description|[] → []|{} → {'blue': 1, '/blue': 1}|
|spa|relics.LUBRICANT_RUNE.description|[] → []|{} → {'blue': 1, '/blue': 1}|
|jpn|relics.LUBRICANT_RUNE.description|[] → []|{} → {'blue': 1, '/blue': 1}|
|kor|relics.LUBRICANT_RUNE.description|[] → []|{} → {'blue': 1, '/blue': 1}|
|ptb|relics.LUBRICANT_RUNE.description|[] → []|{} → {'blue': 1, '/blue': 1}|
|rus|relics.LUBRICANT_RUNE.description|[] → []|{} → {'blue': 1, '/blue': 1}|
|tha|relics.LUBRICANT_RUNE.description|[] → []|{} → {'blue': 1, '/blue': 1}|
|kor|relics.HUBRIS_RUNE.description|[] → []|{'blue': 4, '/blue': 4, 'gold': 1, '/gold': 1} → {'blue': 4, '/blue': 4}|
|eng|relics.GOLDEN_SPATULA_RUNE.description|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|esp|relics.GOLDEN_SPATULA_RUNE.description|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|spa|relics.GOLDEN_SPATULA_RUNE.description|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|jpn|relics.GOLDEN_SPATULA_RUNE.description|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|kor|relics.GOLDEN_SPATULA_RUNE.description|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|ptb|relics.GOLDEN_SPATULA_RUNE.description|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|rus|relics.GOLDEN_SPATULA_RUNE.description|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|tha|relics.GOLDEN_SPATULA_RUNE.description|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|kor|relics.getExcitedRune.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|kor|relics.masterOfDualityRune.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'blue': 2, '/blue': 2}|
|kor|relics.bloodPactRune.enemyDescription|[] → []|{'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2} → {'blue': 2, '/blue': 2}|
|kor|relics.brutalForceRune.enemyDescription|[] → []|{'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2} → {'blue': 2, '/blue': 2}|
|eng|relics.tanksShieldRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|esp|relics.tanksShieldRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|spa|relics.tanksShieldRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|jpn|relics.tanksShieldRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|kor|relics.tanksShieldRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|ptb|relics.tanksShieldRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|rus|relics.tanksShieldRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|tha|relics.tanksShieldRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|eng|relics.goldenSpatulaRune.enemyDescription|[] → []|{'blue': 7, '/blue': 7} → {'blue': 4, '/blue': 4}|
|esp|relics.goldenSpatulaRune.enemyDescription|[] → []|{'blue': 7, '/blue': 7} → {'blue': 4, '/blue': 4}|
|spa|relics.goldenSpatulaRune.enemyDescription|[] → []|{'blue': 7, '/blue': 7} → {'blue': 4, '/blue': 4}|
|jpn|relics.goldenSpatulaRune.enemyDescription|[] → []|{'blue': 7, '/blue': 7} → {'blue': 4, '/blue': 4}|
|kor|relics.goldenSpatulaRune.enemyDescription|[] → []|{'blue': 7, '/blue': 7} → {'blue': 4, '/blue': 4}|
|ptb|relics.goldenSpatulaRune.enemyDescription|[] → []|{'blue': 7, '/blue': 7} → {'blue': 4, '/blue': 4}|
|rus|relics.goldenSpatulaRune.enemyDescription|[] → []|{'blue': 7, '/blue': 7} → {'blue': 4, '/blue': 4}|
|tha|relics.goldenSpatulaRune.enemyDescription|[] → []|{'blue': 7, '/blue': 7} → {'blue': 4, '/blue': 4}|
|kor|relics.monarchsGazeRune.enemyDescription|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|eng|relics.mindOverMatterRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|esp|relics.mindOverMatterRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|spa|relics.mindOverMatterRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|jpn|relics.mindOverMatterRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|kor|relics.mindOverMatterRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|ptb|relics.mindOverMatterRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|rus|relics.mindOverMatterRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|tha|relics.mindOverMatterRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|eng|relics.archmageRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|esp|relics.archmageRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|spa|relics.archmageRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|jpn|relics.archmageRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|kor|relics.archmageRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|ptb|relics.archmageRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|rus|relics.archmageRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|tha|relics.archmageRune.enemyDescription|[] → []|{'blue': 3, '/blue': 3} → {'blue': 4, '/blue': 4}|
|eng|relics.LETHAL_TEMPO_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|esp|relics.LETHAL_TEMPO_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|spa|relics.LETHAL_TEMPO_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|jpn|relics.LETHAL_TEMPO_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|kor|relics.LETHAL_TEMPO_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|ptb|relics.LETHAL_TEMPO_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|rus|relics.LETHAL_TEMPO_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|tha|relics.LETHAL_TEMPO_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|kor|relics.PRECISION_COGNITION_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 1, '/blue': 1}|
|eng|relics.CAREFUL_SELECTION_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|esp|relics.CAREFUL_SELECTION_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|spa|relics.CAREFUL_SELECTION_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|jpn|relics.CAREFUL_SELECTION_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.CAREFUL_SELECTION_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|ptb|relics.CAREFUL_SELECTION_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|rus|relics.CAREFUL_SELECTION_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|tha|relics.CAREFUL_SELECTION_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|eng|relics.BYPRODUCT_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1}|
|esp|relics.BYPRODUCT_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1}|
|spa|relics.BYPRODUCT_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1}|
|jpn|relics.BYPRODUCT_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1}|
|kor|relics.BYPRODUCT_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1}|
|ptb|relics.BYPRODUCT_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1}|
|rus|relics.BYPRODUCT_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1}|
|tha|relics.BYPRODUCT_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 1, '/blue': 1}|
|kor|relics.CONDENSED_RADIANCE_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1} → {'blue': 2, '/blue': 2}|
|eng|relics.WRAITH_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|esp|relics.WRAITH_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|spa|relics.WRAITH_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|jpn|relics.WRAITH_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|kor|relics.WRAITH_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|ptb|relics.WRAITH_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|rus|relics.WRAITH_RUNE.description|[] → []|{'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|tha|relics.WRAITH_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3, 'gold': 2, '/gold': 2}|
|kor|relics.GROWING_STRONGER_RUNE.description|[] → []|{'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2} → {'blue': 2, '/blue': 2}|
|rus|relics.MISERABLE_FATE_RUNE.description|[] → []|{'gold': 1, '/gold': 1} → {}|
|eng|relics.HAPPY_ACCIDENT_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|esp|relics.HAPPY_ACCIDENT_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|spa|relics.HAPPY_ACCIDENT_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|ptb|relics.HAPPY_ACCIDENT_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|rus|relics.HAPPY_ACCIDENT_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|eng|relics.MORE_THE_MERRIER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|esp|relics.MORE_THE_MERRIER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|spa|relics.MORE_THE_MERRIER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|jpn|relics.MORE_THE_MERRIER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.MORE_THE_MERRIER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|ptb|relics.MORE_THE_MERRIER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|rus|relics.MORE_THE_MERRIER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|tha|relics.MORE_THE_MERRIER_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.SCARED_STIFF_RUNE.description|[] → []|{'gold': 1, '/gold': 1} → {}|
|eng|relics.TANKS_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|esp|relics.TANKS_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|spa|relics.TANKS_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|jpn|relics.TANKS_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.TANKS_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|ptb|relics.TANKS_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|rus|relics.TANKS_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|tha|relics.TANKS_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|eng|relics.MENTAL_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|esp|relics.MENTAL_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|spa|relics.MENTAL_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|jpn|relics.MENTAL_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.MENTAL_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|ptb|relics.MENTAL_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|rus|relics.MENTAL_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|tha|relics.MENTAL_SHIELD_RUNE.description|[] → []|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|kor|relics.SELL_OFF_RUNE.description|[] → []|{'gold': 2, '/gold': 2} → {'gold': 1, '/gold': 1}|
|kor|relics.KNOW_THY_PLACE_UPGRADE_RUNE.description|[] → []|{'gold': 3, '/gold': 3, 'blue': 1, '/blue': 1} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|eng|relics.SURVIVOR_UPGRADE_RUNE.description|[] → []|{'gold': 1, '/gold': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|esp|relics.SURVIVOR_UPGRADE_RUNE.description|[] → []|{'gold': 1, '/gold': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|spa|relics.SURVIVOR_UPGRADE_RUNE.description|[] → []|{'gold': 1, '/gold': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|jpn|relics.SURVIVOR_UPGRADE_RUNE.description|[] → []|{'gold': 1, '/gold': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|kor|relics.SURVIVOR_UPGRADE_RUNE.description|[] → []|{'gold': 1, '/gold': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|ptb|relics.SURVIVOR_UPGRADE_RUNE.description|[] → []|{'gold': 1, '/gold': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|rus|relics.SURVIVOR_UPGRADE_RUNE.description|[] → []|{'gold': 1, '/gold': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|tha|relics.SURVIVOR_UPGRADE_RUNE.description|[] → []|{'gold': 1, '/gold': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|eng|relics.ARCHMAGE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|esp|relics.ARCHMAGE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|spa|relics.ARCHMAGE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|jpn|relics.ARCHMAGE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|kor|relics.ARCHMAGE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|ptb|relics.ARCHMAGE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|rus|relics.ARCHMAGE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|tha|relics.ARCHMAGE_RUNE.description|[] → []|{'blue': 2, '/blue': 2} → {'blue': 3, '/blue': 3}|
|eng|relics.DECAY_RUNE.description|[] → []|{'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2} → {'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2}|
|esp|relics.DECAY_RUNE.description|[] → []|{'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2} → {'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2}|
|spa|relics.DECAY_RUNE.description|[] → []|{'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2} → {'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2}|
|jpn|relics.DECAY_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2}|
|ptb|relics.DECAY_RUNE.description|[] → []|{'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2} → {'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2}|
|rus|relics.DECAY_RUNE.description|[] → []|{'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2} → {'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2}|
|tha|relics.DECAY_RUNE.description|[] → []|{'blue': 1, '/blue': 1, 'gold': 2, '/gold': 2} → {'blue': 2, '/blue': 2, 'gold': 2, '/gold': 2}|
|eng|relics.SNAKEBITE_UPGRADE_RUNE.description|['Replays'] → ['Replays']|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|esp|relics.SNAKEBITE_UPGRADE_RUNE.description|['Replays'] → ['Replays']|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|spa|relics.SNAKEBITE_UPGRADE_RUNE.description|['Replays'] → ['Replays']|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|jpn|relics.SNAKEBITE_UPGRADE_RUNE.description|['Replays'] → ['Replays']|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|kor|relics.SNAKEBITE_UPGRADE_RUNE.description|['Replays'] → ['Replays']|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|ptb|relics.SNAKEBITE_UPGRADE_RUNE.description|['Replays'] → ['Replays']|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|rus|relics.SNAKEBITE_UPGRADE_RUNE.description|['Replays'] → ['Replays']|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|tha|relics.SNAKEBITE_UPGRADE_RUNE.description|['Replays'] → ['Replays']|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|eng|relics.INKSHADOW_RUNE.description|[] → []|{'gold': 4, '/gold': 4} → {'gold': 2, '/gold': 2}|
|esp|relics.INKSHADOW_RUNE.description|[] → []|{'gold': 4, '/gold': 4} → {'gold': 2, '/gold': 2}|
|spa|relics.INKSHADOW_RUNE.description|[] → []|{'gold': 4, '/gold': 4} → {'gold': 2, '/gold': 2}|
|jpn|relics.INKSHADOW_RUNE.description|[] → []|{'gold': 4, '/gold': 4} → {'gold': 2, '/gold': 2}|
|kor|relics.INKSHADOW_RUNE.description|[] → []|{'gold': 4, '/gold': 4} → {'gold': 2, '/gold': 2}|
|ptb|relics.INKSHADOW_RUNE.description|[] → []|{'gold': 4, '/gold': 4} → {'gold': 2, '/gold': 2}|
|rus|relics.INKSHADOW_RUNE.description|[] → []|{'gold': 4, '/gold': 4} → {'gold': 2, '/gold': 2}|
|tha|relics.INKSHADOW_RUNE.description|[] → []|{'gold': 4, '/gold': 4} → {'gold': 2, '/gold': 2}|
|eng|relics.EXTREME_SPEED_RUNE.description|['MaxHpLoss'] → ['MaxHpLoss']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|esp|relics.EXTREME_SPEED_RUNE.description|['MaxHpLoss'] → ['MaxHpLoss']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|spa|relics.EXTREME_SPEED_RUNE.description|['MaxHpLoss'] → ['MaxHpLoss']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|jpn|relics.EXTREME_SPEED_RUNE.description|['MaxHpLoss'] → ['MaxHpLoss']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|kor|relics.EXTREME_SPEED_RUNE.description|['MaxHpLoss'] → ['MaxHpLoss']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|ptb|relics.EXTREME_SPEED_RUNE.description|['MaxHpLoss'] → ['MaxHpLoss']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|rus|relics.EXTREME_SPEED_RUNE.description|['MaxHpLoss'] → ['MaxHpLoss']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|tha|relics.EXTREME_SPEED_RUNE.description|['MaxHpLoss'] → ['MaxHpLoss']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 1, '/gold': 1, 'blue': 2, '/blue': 2}|
|eng|relics.IRON_WAVE_UPGRADE_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1}|
|esp|relics.IRON_WAVE_UPGRADE_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1}|
|spa|relics.IRON_WAVE_UPGRADE_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1}|
|ptb|relics.IRON_WAVE_UPGRADE_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1}|
|rus|relics.IRON_WAVE_UPGRADE_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1}|
|tha|relics.IRON_WAVE_UPGRADE_RUNE.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1}|
|eng|relics.ANTHONY_BIAS_RUNE.description|['PercentPerCard'] → ['PercentPerCard']|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|esp|relics.ANTHONY_BIAS_RUNE.description|['PercentPerCard'] → ['PercentPerCard']|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|spa|relics.ANTHONY_BIAS_RUNE.description|['PercentPerCard'] → ['PercentPerCard']|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|jpn|relics.ANTHONY_BIAS_RUNE.description|['PercentPerCard'] → ['PercentPerCard']|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|ptb|relics.ANTHONY_BIAS_RUNE.description|['PercentPerCard'] → ['PercentPerCard']|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|rus|relics.ANTHONY_BIAS_RUNE.description|['PercentPerCard'] → ['PercentPerCard']|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|tha|relics.ANTHONY_BIAS_RUNE.description|['PercentPerCard'] → ['PercentPerCard']|{'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2}|
|eng|relics.SKULKING_COLONY_HEX.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|esp|relics.SKULKING_COLONY_HEX.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|spa|relics.SKULKING_COLONY_HEX.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|jpn|relics.SKULKING_COLONY_HEX.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|kor|relics.SKULKING_COLONY_HEX.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|ptb|relics.SKULKING_COLONY_HEX.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|rus|relics.SKULKING_COLONY_HEX.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|tha|relics.SKULKING_COLONY_HEX.description|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|eng|relics.skulkingColonyHex.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|esp|relics.skulkingColonyHex.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|spa|relics.skulkingColonyHex.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|jpn|relics.skulkingColonyHex.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|kor|relics.skulkingColonyHex.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|ptb|relics.skulkingColonyHex.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|rus|relics.skulkingColonyHex.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|tha|relics.skulkingColonyHex.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2} → {'gold': 3, '/gold': 3, 'blue': 2, '/blue': 2}|
|eng|relics.LAGAVULIN_MATRIARCH_HEX.description|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|esp|relics.LAGAVULIN_MATRIARCH_HEX.description|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|spa|relics.LAGAVULIN_MATRIARCH_HEX.description|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|jpn|relics.LAGAVULIN_MATRIARCH_HEX.description|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|kor|relics.LAGAVULIN_MATRIARCH_HEX.description|[] → []|{'blue': 4, '/blue': 4, 'gold': 2, '/gold': 2} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|ptb|relics.LAGAVULIN_MATRIARCH_HEX.description|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|rus|relics.LAGAVULIN_MATRIARCH_HEX.description|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|tha|relics.LAGAVULIN_MATRIARCH_HEX.description|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|eng|relics.lagavulinMatriarchHex.enemyDescription|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|esp|relics.lagavulinMatriarchHex.enemyDescription|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|spa|relics.lagavulinMatriarchHex.enemyDescription|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|jpn|relics.lagavulinMatriarchHex.enemyDescription|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|kor|relics.lagavulinMatriarchHex.enemyDescription|[] → []|{'blue': 4, '/blue': 4, 'gold': 2, '/gold': 2} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|ptb|relics.lagavulinMatriarchHex.enemyDescription|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|rus|relics.lagavulinMatriarchHex.enemyDescription|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|tha|relics.lagavulinMatriarchHex.enemyDescription|[] → []|{'blue': 5, '/blue': 5} → {'blue': 5, '/blue': 5, 'gold': 2, '/gold': 2}|
|esp|relics.MYRIAD_SWORDS_RUNE.description|[] → []|{'gold': 2, '/gold': 2} → {'gold': 3, '/gold': 3}|
|spa|relics.MYRIAD_SWORDS_RUNE.description|[] → []|{'gold': 2, '/gold': 2} → {'gold': 3, '/gold': 3}|
|ptb|relics.MYRIAD_SWORDS_RUNE.description|[] → []|{'gold': 2, '/gold': 2} → {'gold': 3, '/gold': 3}|
|rus|relics.MYRIAD_SWORDS_RUNE.description|[] → []|{'gold': 2, '/gold': 2} → {'gold': 3, '/gold': 3}|
|tha|relics.MYRIAD_SWORDS_RUNE.description|[] → []|{'gold': 2, '/gold': 2} → {'gold': 3, '/gold': 3}|
|eng|relics.PACTS_END_UPGRADE_RUNE.description|['DamagePerExhaust'] → ['DamagePerExhaust']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|esp|relics.PACTS_END_UPGRADE_RUNE.description|['DamagePerExhaust'] → ['DamagePerExhaust']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|spa|relics.PACTS_END_UPGRADE_RUNE.description|['DamagePerExhaust'] → ['DamagePerExhaust']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|jpn|relics.PACTS_END_UPGRADE_RUNE.description|['DamagePerExhaust'] → ['DamagePerExhaust']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|kor|relics.PACTS_END_UPGRADE_RUNE.description|['DamagePerExhaust'] → ['DamagePerExhaust']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|ptb|relics.PACTS_END_UPGRADE_RUNE.description|['DamagePerExhaust'] → ['DamagePerExhaust']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|rus|relics.PACTS_END_UPGRADE_RUNE.description|['DamagePerExhaust'] → ['DamagePerExhaust']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|tha|relics.PACTS_END_UPGRADE_RUNE.description|['DamagePerExhaust'] → ['DamagePerExhaust']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|eng|relics.BRAND_UPGRADE_RUNE.description|['DamagePercentPerBrand'] → ['DamagePercentPerBrand']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|esp|relics.BRAND_UPGRADE_RUNE.description|['DamagePercentPerBrand'] → ['DamagePercentPerBrand']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|spa|relics.BRAND_UPGRADE_RUNE.description|['DamagePercentPerBrand'] → ['DamagePercentPerBrand']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|jpn|relics.BRAND_UPGRADE_RUNE.description|['DamagePercentPerBrand'] → ['DamagePercentPerBrand']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|kor|relics.BRAND_UPGRADE_RUNE.description|['DamagePercentPerBrand'] → ['DamagePercentPerBrand']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|ptb|relics.BRAND_UPGRADE_RUNE.description|['DamagePercentPerBrand'] → ['DamagePercentPerBrand']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|rus|relics.BRAND_UPGRADE_RUNE.description|['DamagePercentPerBrand'] → ['DamagePercentPerBrand']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|tha|relics.BRAND_UPGRADE_RUNE.description|['DamagePercentPerBrand'] → ['DamagePercentPerBrand']|{'gold': 1, '/gold': 1, 'blue': 1, '/blue': 1} → {'blue': 2, '/blue': 2, 'gold': 1, '/gold': 1}|
|rus|relics.HEXTECH_RING_OF_THE_DRAKE_PLUS.description|['Cards', 'Turns'] → ['Cards', 'Turns']|{'blue': 3, '/blue': 3} → {'blue': 2, '/blue': 2}|
|eng|relics.corruptedBranchRune.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|esp|relics.corruptedBranchRune.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|spa|relics.corruptedBranchRune.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|ptb|relics.corruptedBranchRune.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|
|rus|relics.corruptedBranchRune.enemyDescription|[] → []|{'gold': 2, '/gold': 2, 'blue': 1, '/blue': 1} → {'gold': 2, '/gold': 2, 'blue': 2, '/blue': 2}|

## 无语义问题计数与边界

直接描述模型中，未在语义表记录独立问题的有 **567** 个（总623减涉及直接描述的语义模型56）。这是此次静态对照的“未发现语义差异”数量，仍可能涉及术语、角色后缀、硬编码或标签修正；不是实机/联机验证结论。

## 逐模型事实附录

|类|描述键|Hook/覆写|动态值（基础值）|资格及限制证据|
|---|---|---|---|---|
|AdamantRune|relics:ADAMANT_RUNE.description|OnEnemyDebuffApplied|{'Block': '3'}|protected override int MaxProcsPerTurn => 5;|
|AdaptiveCapacitorRune|relics:ADAPTIVE_CAPACITOR_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'OrbSlots': '1'}|public override bool IsAvailableForPlayer(Player player)|
|AdvanceToRetreatRune|relics:ADVANCE_TO_RETREAT_RUNE.description|IsAvailableForPlayer, AfterDamageGiven|{'Block': '3'}|public override bool IsAvailableForPlayer(Player player)|
|AeonglassEnemyHex|relics:aeonglassHex.enemyDescription|AfterPlayerTurnStartLate, AfterShuffle|{}|详见 `src/EnemyHexes/AeonglassEnemyHex.cs:3`|
|AeonglassHex|relics:AEONGLASS_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|AncientStatueEnemyHex|relics:ancientStatueHex.enemyDescription|AfterCardPlayed|{}|详见 `src/EnemyHexes/AncientStatueEnemyHex.cs:3`|
|AncientStatueHex|relics:ANCIENT_STATUE_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|AncientWineEnemyHex|relics:ancientWineRune.enemyDescription|AfterCardPlayed|{}|if (!cardPlay.IsFirstInSeries; \|\| cardPlay.IsAutoPlay|
|AncientWineRune|relics:ANCIENT_WINE_RUNE.description|AfterCardPlayed|{'HealPercent': '2'}|详见 `src/Runes/AncientWineRune.cs:3`|
|AnthonyBiasRune|relics:ANTHONY_BIAS_RUNE.description|ModifyBlockMultiplicative, ModifyDamageMultiplicativeCompat|{'PercentPerCard': '1'}|详见 `src/Runes/AnthonyBiasRune.cs:3`|
|ArcanePunchEnemyHex|relics:infestedPrismHex.enemyDescription|ApplyCombatStartPlayerDebuffs|{}|详见 `src/EnemyHexes/ArcanePunchEnemyHex.cs:4`|
|ArcanePunchRune|relics:ARCANE_PUNCH_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterCardPlayed, AfterCardPlayedLate|{'AttacksPerEnergy': '2', 'Energy': '1'}|详见 `src/Runes/ArcanePunchRune.cs:3`|
|ArchmageEnemyHex|relics:archmageRune.enemyDescription|AfterCardPlayed|{}|private const int ChancePercent = 33;; \|\| !cardPlay.IsFirstInSeries; \|\| cardPlay.IsAutoPlay|
|ArchmageRune|relics:ARCHMAGE_RUNE.description|AfterCardPlayed|{'ChancePercent': '33'}|详见 `src/Runes/ArchmageRune.cs:3`|
|ArtifactForge|relics:ARTIFACT_FORGE.description|BeforeCombatStart|{'ArtifactPower': '1'}|详见 `src/Forges/HextechForges.Gold.cs:488`|
|AstralBodyEnemyHex|relics:astralBodyRune.enemyDescription|ModifyDamageMultiplicative, ApplyPersistentToEnemy|{}|详见 `src/EnemyHexes/AstralBodyEnemyHex.cs:3`|
|AstralBodyRune|relics:ASTRAL_BODY_RUNE.description|AfterObtained, ModifyDamageMultiplicativeCompat|{'MaxHpPercent': '50', 'DamageMultiplier': '0.9'}|详见 `src/Runes/AstralBodyRune.cs:5`|
|AttackDefenseUnityRune|relics:ATTACK_DEFENSE_UNITY_RUNE.description|AfterObtained|{'Cards': '1'}|详见 `src/Runes/AttackDefenseUnityRune.cs:3`|
|AttackForge|relics:ATTACK_FORGE.description|ModifyDamageMultiplicativeCompat|{'DamageMultiplier': '1.2'}|详见 `src/Forges/HextechForges.Prismatic.cs:41`|
|AutoPatrolRune|relics:AUTO_PATROL_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'OstyHpPerCard': '10', 'Cards': '1'}|public override bool IsAvailableForPlayer(Player player)|
|AutomationUpgradeRune|relics:AUTOMATION_UPGRADE_RUNE.description|IsAvailableForCharacter|{'Cards': '2'}|private const int TriggerThreshold = 10;; protected override bool IsAvailableForCharacter(Player player)|
|BackToBasicsEnemyHex|relics:backToBasicsRune.enemyDescription|AfterCardPlayed, ShouldPlay|{}|internal const int TurnCardLimitTier1 = 12;; internal const int TurnCardLimitTier2 = 10;; internal const int TurnCardLimitTier3 = 8;; if (cardPlay.IsAutoPlay; \|\| !cardPlay.IsFirstInSeries|
|BackToBasicsRune|relics:BACK_TO_BASICS_RUNE.description|ShouldPlay, ModifyBlockMultiplicative, ModifyDamageMultiplicativeCompat|{}|详见 `src/Runes/BackToBasicsRune.cs:3`|
|BadTasteRune|relics:BAD_TASTE_RUNE.description|OnEnemyDebuffApplied|{'Heal': '1'}|详见 `src/Runes/BadTasteRune.cs:3`|
|BadgeBrothersRune|relics:BADGE_BROTHERS_RUNE.description|BeforeCombatStart|{'FreeAttackPower': '1', 'FreeSkillPower': '1'}|详见 `src/Runes/BadgeBrothersRune.cs:3`|
|BarbarianWayRune|relics:BARBARIAN_WAY_RUNE.description|IsAvailableForPlayer, AfterObtained|{}|public override bool IsAvailableForPlayer(Player player)|
|BashUpgradeRune|relics:BASH_UPGRADE_RUNE.description|MeetsCardAvailabilityRequirement, IsAvailableForCharacter, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player)|
|BattleTranceUpgradeRune|relics:BATTLE_TRANCE_UPGRADE_RUNE.description|IsAvailableForCharacter, ModifyPowerAmountGivenMultiplicative|{}|protected override bool IsAvailableForCharacter(Player player)|
|BeginningAndEndRune|relics:BEGINNING_AND_END_RUNE.description|IsAvailableForPlayer, BeforeCombatStart|{'LethalityPower': '100', 'CountdownPower': '6'}|public override bool IsAvailableForPlayer(Player player)|
|BerserkRune|relics:BERSERK_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterEnergyResetLate|{'VulnerablePower': '3', 'Energy': '1'}|public override bool IsAvailableForPlayer(Player player)|
|BigHammerRune|relics:BIG_HAMMER_RUNE.description|IsAvailableForPlayer|{'ForgeBonusPercent': '50'}|public override bool IsAvailableForPlayer(Player player) => IsRegentPlayer(player);|
|BigHandsRune|relics:BIG_HANDS_RUNE.description|IsAvailableForPlayer, ModifySummonAmount|{'Multiplier': '1.5'}|public override bool IsAvailableForPlayer(Player player)|
|BigKnifeRune|relics:BIG_KNIFE_RUNE.description|IsAvailableForPlayer, TryModifyEnergyCostInCombat, TryModifyStarCost|{}|public override bool IsAvailableForPlayer(Player player)|
|BigStrengthEnemyHex|relics:bigStrengthRune.enemyDescription|ModifyDamageMultiplicative|{}|详见 `src/EnemyHexes/BigStrengthEnemyHex.cs:3`|
|BigStrengthRune|relics:BIG_STRENGTH_RUNE.description|ModifyDamageMultiplicativeCompat|{'DamageMultiplier': '1.2'}|详见 `src/Runes/BigStrengthRune.cs:3`|
|BlackCandleRune|relics:BLACK_CANDLE_RUNE.description|AfterObtained, ShouldAddToDeck, AfterAddToDeckPrevented|{}|详见 `src/Runes/BlackCandleRune.cs:3`|
|BladeWaltzCard|cards:BLADE_WALTZ_CARD.description|OnPlay, OnUpgrade|{'Damage': '3', 'Hits': '9', 'IntangiblePower': '1'}|详见 `src/Cards/BladeWaltzCard.cs:3`|
|BladeWaltzRune|relics:BLADE_WALTZ_RUNE.description|AfterObtained|{'Cards': '1'}|详见 `src/Runes/BladeWaltzRune.cs:3`|
|BlankCheckRune|relics:BLANK_CHECK_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart, TryModifyEnergyCostInCombat, TryModifyStarCost, ModifyCardPlayResultPileTypeAndPositionCompat|{}|public override bool IsAvailableForPlayer(Player player)|
|BlockForge|relics:BLOCK_FORGE.description|AfterPlayerTurnStartEarly|{'Block': '3'}|详见 `src/Forges/HextechForges.Silver.cs:356`|
|BloodArmorEnemyHex|relics:bloodArmorRune.enemyDescription|AfterCurrentHpChanged|{}|详见 `src/EnemyHexes/BloodArmorEnemyHex.cs:3`|
|BloodArmorRune|relics:BLOOD_ARMOR_RUNE.description|IsAvailableForPlayer, AfterCurrentHpChanged|{'PlatingPower': '1'}|public override bool IsAvailableForPlayer(Player player)|
|BloodDebtRune|relics:BLOOD_DEBT_RUNE.description|IsAvailableForPlayer, AfterCloned, BeforeCombatStart, AfterCombatEnd, AfterCurrentHpChanged, ModifyDamageAdditiveCompat|{}|public override bool IsAvailableForPlayer(Player player) => IsIroncladPlayer(player);|
|BloodIdolEnemyHex|relics:bloodIdolRune.enemyDescription|AfterGoldGained|{}|详见 `src/EnemyHexes/BloodIdolEnemyHex.cs:3`|
|BloodIdolRune|relics:BLOOD_IDOL_RUNE.description|AfterGoldGained|{'Heal': '5'}|详见 `src/Runes/BloodIdolRune.cs:3`|
|BloodPactEnemyHex|relics:bloodPactRune.enemyDescription|AfterEnemyDamageReceived|{}|if (target.IsAlive && HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.BloodPactProcsThisTurn, target, 2))|
|BloodPactRune|relics:BLOOD_PACT_RUNE.description|IsAvailableForPlayer, AfterDamageReceived|{'StrengthPower': '1'}|public override bool IsAvailableForPlayer(Player player)|
|BloodlettingUpgradeRune|relics:BLOODLETTING_UPGRADE_RUNE.description|IsAvailableForCharacter, ModifyCardPlayResultPileTypeAndPositionCompat|{}|protected override bool IsAvailableForCharacter(Player player)|
|BlueCandleMedkitEnemyHex|relics:blueCandleMedkitRune.enemyDescription|ApplyCombatStartPlayerDebuffs|{}|详见 `src/EnemyHexes/BlueCandleMedkitEnemyHex.cs:3`|
|BlueCandleMedkitRune|relics:BLUE_CANDLE_MEDKIT_RUNE.description|TryModifyEnergyCostInCombat, TryModifyStarCost, ModifyCardPlayResultPileTypeAndPositionCompat, TryModifyKeywordsInCombat|{}|详见 `src/Runes/BlueCandleMedkitRune.cs:3`|
|BodySlamUpgradeRune|relics:BODY_SLAM_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|BodyguardUpgradeRune|relics:BODYGUARD_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player)|
|BoneBreakUpgradeRune|relics:BONE_BREAK_UPGRADE_RUNE.description|IsAvailableForCharacter, BeforeCardPlayed, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player)|
|BoneGuardRune|relics:BONE_GUARD_RUNE.description|IsAvailableForPlayer, AfterSummon|{'BlockMultiplier': '0.5'}|public override bool IsAvailableForPlayer(Player player)|
|BorrowedTimeUpgradeRune|relics:BORROWED_TIME_UPGRADE_RUNE.description|IsAvailableForCharacter, BeforeCardPlayed, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player)|
|BrandUpgradeRune|relics:BRAND_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed, ModifyDamageMultiplicativeCompat|{'DamagePercentPerBrand': '3'}|protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);|
|BreadAndButterRune|relics:BREAD_AND_BUTTER_RUNE.description|AfterObtained|{}|详见 `src/Runes/BreadAndButterRune.cs:3`|
|BreadAndCheeseRune|relics:BREAD_AND_CHEESE_RUNE.description|AfterObtained|{}|详见 `src/Runes/BreadAndCheeseRune.cs:3`|
|BreadAndJamRune|relics:BREAD_AND_JAM_RUNE.description|AfterObtained|{}|详见 `src/Runes/BreadAndJamRune.cs:3`|
|BreadSandwichRune|relics:BREAD_SANDWICH_RUNE.description|ModifyCardPlayCount, AfterModifyingCardPlayCount|{'Replays': '1'}|详见 `src/Runes/BreadSandwichRune.cs:3`|
|BrokenGoldenCrownRune|relics:BROKEN_GOLDEN_CROWN_RUNE.description|TryModifyCardRewardOptionsLate, AfterEnergyResetLate|{'RewardOptionsLost': '2', 'Energy': '1'}|详见 `src/Runes/BrokenGoldenCrownRune.cs:3`|
|BrutalForceEnemyHex|relics:brutalForceRune.enemyDescription|ApplyCombatStartToEnemy|{}|详见 `src/EnemyHexes/BrutalForceEnemyHex.cs:3`|
|BrutalForceRune|relics:BRUTAL_FORCE_RUNE.description|ModifyHandDraw, BeforeCombatStart|{'Cards': '2', 'StrengthPower': '1'}|详见 `src/Runes/BrutalForceRune.cs:3`|
|BrutalityEnemyHex|relics:brutalityRune.enemyDescription|BeforeEnemySideTurnStart|{}|详见 `src/EnemyHexes/BrutalityEnemyHex.cs:3`|
|BrutalityRune|relics:BRUTALITY_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStartEarly|{'HpLoss': '2', 'Cards': '2'}|public override bool IsAvailableForPlayer(Player player)|
|BufferForge|relics:BUFFER_FORGE.description|BeforeCombatStart|{'BufferPower': '1'}|详见 `src/Forges/HextechForges.Prismatic.cs:142`|
|BulletTimeUpgradeRune|relics:BULLET_TIME_UPGRADE_RUNE.description|IsAvailableForCharacter, ModifyPowerAmountGivenMultiplicative|{}|protected override bool IsAvailableForCharacter(Player player) => IsSilentPlayer(player);|
|BurningInterestRune|relics:BURNING_INTEREST_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterDamageGiven, AfterDamageReceived|{'HextechBurnPower': '1', 'CountPerDamage': '2'}|private const decimal InfiniteHpThreshold = 10_000_000m;|
|ByproductRune|relics:BYPRODUCT_RUNE.description|IsAvailableForPlayer, AfterCardGeneratedForCombat|{'Cards': '1'}|public override bool IsAvailableForPlayer(Player player)|
|ByrdonisEnemyHex|relics:byrdonisHex.enemyDescription|ApplyCombatStartToEnemy|{}|详见 `src/EnemyHexes/ByrdonisEnemyHex.cs:3`|
|ByrdonisHex|relics:BYRDONIS_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|CantTouchThisEnemyHex|relics:cantTouchThisRune.enemyDescription|AfterEnemyDamageGivenPlayerHit|{}|详见 `src/EnemyHexes/CantTouchThisEnemyHex.cs:3`|
|CantTouchThisRune|relics:CANT_TOUCH_THIS_RUNE.description|AfterCardPlayed|{'MinCost': '2', 'BufferPower': '1'}|详见 `src/Runes/CantTouchThisRune.cs:3`|
|CardInspectionRune|relics:CARD_INSPECTION_RUNE.description||{}|详见 `src/Runes/CardInspectionRune.cs:3`|
|CarefulSelectionRune|relics:CAREFUL_SELECTION_RUNE.description|TryModifyCardRewardOptions|{'Cards': '4'}|详见 `src/Runes/CarefulSelectionRune.cs:3`|
|CatalystCard|cards:CATALYST_CARD.description|OnPlay, OnUpgrade|{'PoisonMultiplier': '2'}|详见 `src/Cards/HextechCustomCards.cs:43`|
|CatalystRune|relics:CATALYST_RUNE.description|IsAvailableForPlayer, AfterObtained|{'Cards': '1'}|public override bool IsAvailableForPlayer(Player player)|
|CerberusEnemyHex|relics:cerberusRune.enemyDescription|BeforePlayerSideTurnStart|{}|详见 `src/EnemyHexes/CerberusEnemyHex.cs:3`|
|CerberusRune|relics:CERBERUS_RUNE.description|BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, TryModifyEnergyCostInCombat, TryModifyStarCost, AfterCardPlayed|{'FreeAttacks': '3'}|if (!cardPlay.IsFirstInSeries \|\| cardPlay.IsAutoPlay \|\| !IsOwnedAttack(cardPlay.Card)); if (!TryConsumeTurnProc(nameof(CerberusRune), ref _attacksPlayedThisTurn, int.MaxValue))|
|CeremonialBeastEnemyHex|relics:ceremonialBeastHex.enemyDescription||{}|详见 `src/EnemyHexes/CeremonialBeastEnemyHex.cs:6`|
|CeremonialBeastHex|relics:CEREMONIAL_BEAST_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|ChainInSleeveRune|relics:CHAIN_IN_SLEEVE_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, AfterCardPlayed, AfterCardPlayedLate|{'ShivsNeeded': '3', 'Cards': '1'}|public override bool IsAvailableForPlayer(Player player); return cardPlay.IsFirstInSeries; && !cardPlay.IsAutoPlay; entry.CardPlay.IsFirstInSeries; && !entry.CardPlay.IsAutoPlay|
|ChargeUpRune|relics:CHARGE_UP_RUNE.description|IsAvailableForPlayer, AfterStarsSpent|{}|public override bool IsAvailableForPlayer(Player player) => IsRegentPlayer(player);|
|ChemtechDragonSoulCard|cards:CHEMTECH_DRAGON_SOUL_CARD.description|OnPlay|{'PotionCount': '1'}|详见 `src/Cards/HextechDragonSoulCards.cs:118`|
|CircleOfDeathRune|relics:CIRCLE_OF_DEATH_RUNE.description|AfterBlockGained|{}|详见 `src/Runes/CircleOfDeathRune.cs:3`|
|ClawUpgradeRune|relics:CLAW_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);|
|CloudDragonSoulCard|cards:CLOUD_DRAGON_SOUL_CARD.description|OnPlay|{'Cards': '2'}|详见 `src/Cards/HextechDragonSoulCards.cs:143`|
|ClownCollegeEnemyHex|relics:clownCollegeRune.enemyDescription|AfterEnemyDamageReceived|{}|if (target.IsAlive && HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.ClownCollegeProcsThisTurn, target, 1))|
|ClownCollegeRune|relics:CLOWN_COLLEGE_RUNE.description|AfterObtained|{'Cards': '3'}|详见 `src/Runes/ClownCollegeRune.cs:3`|
|CollectorRune|relics:COLLECTOR_RUNE.description|AfterDamageGiven, BeforeCombatStart, AfterCombatEnd|{'ExecutePercent': '10', 'CountPerExecute': '20'}|详见 `src/Runes/CollectorRune.cs:3`|
|ColorDiscoveryRune|relics:COLOR_DISCOVERY_RUNE.description|BeforeCombatStart, BeforeHandDraw, AfterCombatVictory|{'Cards': '3', 'Selection': '1'}|详见 `src/Runes/ColorDiscoveryRune.cs:6`|
|CompactUpgradeRune|relics:COMPACT_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|CompensationEnemyHex|relics:compensationRune.enemyDescription|ResetRunScopedState, ApplyCombatStartToEnemy, BeforeSideTurnStart, AfterCombatVictory, ModifyHpLostAfterOsty, AfterEnemyDamageReceivedAny|{}|详见 `src/EnemyHexes/CompensationEnemyHex.cs:3`|
|CompensationRune|relics:COMPENSATION_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, ModifyHpLostAfterOsty, AfterDamageReceived|{}|public override bool IsAvailableForPlayer(Player player)|
|CondensedRadianceRune|relics:CONDENSED_RADIANCE_RUNE.description|IsAvailableForPlayer, AfterCardGeneratedForCombat|{'Cards': '1', 'Stars': '1'}|public override bool IsAvailableForPlayer(Player player)|
|ConstitutionForge|relics:CONSTITUTION_FORGE.description|BeforeCombatStart|{'StrengthPower': '1', 'DexterityPower': '1'}|详见 `src/Forges/HextechForges.Gold.cs:3`|
|CoreOverloadRune|relics:CORE_OVERLOAD_RUNE.description|IsAvailableForPlayer, AfterOrbEvoked|{'FocusPower': '1'}|public override bool IsAvailableForPlayer(Player player)|
|CorpseExplosionRune|relics:CORPSE_EXPLOSION_RUNE.description|IsAvailableForPlayer, AfterDeath|{}|public override bool IsAvailableForPlayer(Player player)|
|CorrosionEnemyHex|relics:corrosionRune.enemyDescription|AfterEnemyDamageGivenImmediate|{}|详见 `src/EnemyHexes/CorrosionEnemyHex.cs:3`|
|CorrosionRune|relics:CORROSION_RUNE.description|AfterDamageGiven|{}|详见 `src/Runes/CorrosionRune.cs:3`|
|CorrosiveWaveUpgradeRune|relics:CORROSIVE_WAVE_UPGRADE_RUNE.description|IsAvailableForCharacter, ModifyCardPlayResultPileTypeAndPositionCompat|{}|protected override bool IsAvailableForCharacter(Player player) => IsSilentPlayer(player);|
|CorruptedBranchEnemyHex|relics:corruptedBranchRune.enemyDescription|AfterCardExhausted|{}|详见 `src/EnemyHexes/CorruptedBranchEnemyHex.cs:3`|
|CorruptedBranchRune|relics:CORRUPTED_BRANCH_RUNE.description|AfterObtained, BeforeCombatStart, AfterCombatEnd, AfterCardEnteredCombat, AfterCardExhausted|{'Cards': '1'}|详见 `src/Runes/CorruptedBranchRune.cs:5`|
|CourageOfColossusEnemyHex|relics:courageOfColossusRune.enemyDescription|AfterEnemyDebuffReceived|{}|if (HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.CourageProcsThisTurn, target, 2))|
|CourageOfColossusRune|relics:COURAGE_OF_COLOSSUS_RUNE.description|OnEnemyDebuffApplied|{'Plating': '3'}|protected override int MaxProcsPerTurn => 2;|
|CrashLandingUpgradeRune|relics:CRASH_LANDING_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|CreativeAiUpgradeRune|relics:CREATIVE_AI_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|CrossOrbRune|relics:CROSS_ORB_RUNE.description|TryModifyCardRewardOptions, ModifyMerchantCardCreationResults, TryModifyRewards|{'CommonReductionPercent': '50'}|详见 `src/Runes/CrossOrbRune.cs:6`|
|CurtainCallRune|relics:CURTAIN_CALL_RUNE.description|IsAvailableForPlayer, AfterObtained, AfterCardEnteredCombat|{'Cards': '1'}|public override bool IsAvailableForPlayer(Player player)|
|CuttingEdgeAlchemistEnemyHex|relics:cuttingEdgeAlchemistRune.enemyDescription||{}|详见 `src/EnemyHexes/CuttingEdgeAlchemistEnemyHex.cs:3`|
|CuttingEdgeAlchemistRune|relics:CUTTING_EDGE_ALCHEMIST_RUNE.description|AfterCombatVictory|{'RarePotionCount': '1', 'UncommonPotionCount': '1'}|详见 `src/Runes/CuttingEdgeAlchemistRune.cs:3`|
|DawnbringersResolveEnemyHex|relics:dawnbringersResolveRune.enemyDescription|AfterEnemyHealthThreshold|{}|详见 `src/EnemyHexes/DawnbringersResolveEnemyHex.cs:3`|
|DawnbringersResolveRune|relics:DAWNBRINGERS_RESOLVE_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterDamageReceived|{'ThresholdPercent': '50'}|详见 `src/Runes/DawnbringersResolveRune.cs:5`|
|DeathHarvestEnemyHex|relics:deathHarvestRune.enemyDescription|AfterEnemyDamageGivenImmediate|{}|详见 `src/EnemyHexes/DeathHarvestEnemyHex.cs:3`|
|DeathHarvestRune|relics:DEATH_HARVEST_RUNE.description|AfterDamageGiven|{'HealPercent': '50'}|详见 `src/Runes/DeathHarvestRune.cs:3`|
|DeathWarrantRune|relics:DEATH_WARRANT_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, AfterCardDrawn, AfterCardPlayedLate, AfterPlayerTurnStartLate|{'CardsNeeded': '8'}|public override bool IsAvailableForPlayer(Player player) => IsSilentPlayer(player);|
|DecayRune|relics:DECAY_RUNE.description|IsAvailableForPlayer, AfterSideTurnStart|{'WeakPower': '1', 'StrengthPower': '1'}|public override bool IsAvailableForPlayer(Player player)|
|DecisionsDecisionsUpgradeRune|relics:DECISIONS_DECISIONS_UPGRADE_RUNE.description|IsAvailableForCharacter, ModifyCardPlayCount, AfterModifyingCardPlayCount|{}|protected override bool IsAvailableForCharacter(Player player) => IsRegentPlayer(player);|
|DefendUpgradeRune|relics:DEFEND_UPGRADE_RUNE.description|IsAvailableForPlayer, AfterCombatEnd|{}|public override bool IsAvailableForPlayer(Player player)|
|DemonFormUpgradeRune|relics:DEMON_FORM_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);|
|DeviantCognitionRune|relics:DEVIANT_COGNITION_RUNE.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player)|
|DevilsDanceEnemyHex|relics:devilsDanceRune.enemyDescription|AfterEnemyDamageGivenPlayerHit|{}|详见 `src/EnemyHexes/DevilsDanceEnemyHex.cs:3`|
|DevilsDanceRune|relics:DEVILS_DANCE_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterCardPlayed, AfterCardPlayedLate|{'AttacksPerMaxHp': '3', 'MaxHp': '1'}|详见 `src/Runes/DevilsDanceRune.cs:3`|
|DexterityForge|relics:DEXTERITY_FORGE.description|BeforeCombatStart|{'DexterityPower': '1'}|详见 `src/Forges/HextechForges.Silver.cs:22`|
|DexterityStrengthToFocusRune|relics:DEXTERITY_STRENGTH_TO_FOCUS_RUNE.description|IsAvailableForPlayer, AfterRoomEntered, ShouldConvert, ShouldConvertAppliedPower, ApplyConvertedPower, RevertOriginalPower|{'FocusPower': '1'}|public override bool IsAvailableForPlayer(Player player)|
|DexterityToStrengthRune|relics:DEXTERITY_TO_STRENGTH_RUNE.description|AfterRoomEntered, ShouldConvert, ShouldConvertAppliedPower, ApplyConvertedPower, RevertOriginalPower|{'StrengthPower': '1'}|详见 `src/Runes/DexterityToStrengthRune.cs:3`|
|DiceManiacRune|relics:DICE_MANIAC_RUNE.description|AfterCombatVictory|{'DropChance': '50', 'ForgeMultiplier': '2'}|internal const int BaseDropChance = 50;; internal const int DropChanceStep = 10;|
|DieForYouRune|relics:DIE_FOR_YOU_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart, BeforeCombatStart, AfterCombatEnd, AfterDeath|{'Summon': '5', 'Cards': '1'}|public override bool IsAvailableForPlayer(Player player)|
|DirgeUpgradeRune|relics:DIRGE_UPGRADE_RUNE.description|IsAvailableForCharacter, ModifyCardPlayResultPileTypeAndPositionCompat, AfterModifyingCardPlayResultPileOrPositionCompat|{}|protected override bool IsAvailableForCharacter(Player player)|
|DisasterForge|relics:DISASTER_FORGE.description|BeforeCombatStart|{'WeakPower': '1', 'VulnerablePower': '1'}|详见 `src/Forges/HextechForges.Gold.cs:24`|
|DivineInterventionEnemyHex|relics:divineInterventionRune.enemyDescription|BeforePlayerSideTurnStart|{}|if (!context.TryConsumeRoundInterval(Kind, combatState, everyNRounds: 3))|
|DivineInterventionRune|relics:DIVINE_INTERVENTION_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterPlayerTurnStart|{'TurnsNeeded': '3', 'IntangiblePower': '1'}|public override bool IsAvailableForPlayer(Player player)|
|DizzySpinningEnemyHex|relics:dizzySpinningRune.enemyDescription|AfterShuffle|{}|详见 `src/EnemyHexes/DizzySpinningEnemyHex.cs:3`|
|DizzySpinningRune|relics:DIZZY_SPINNING_RUNE.description|ModifyHandDraw, AfterShuffle|{'Cards': '2'}|详见 `src/Runes/DizzySpinningRune.cs:3`|
|DonationRune|relics:DONATION_RUNE.description|AfterObtained|{'Gold': '1000'}|详见 `src/Runes/DonationRune.cs:3`|
|DoomsdayEnemyHex|relics:doomsdayRune.enemyDescription|BeforeEnemySideTurnStart|{}|详见 `src/EnemyHexes/DoomsdayEnemyHex.cs:3`|
|DoomsdayRune|relics:DOOMSDAY_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'DoomPercent': '5', 'MinimumDoom': '5'}|public override bool IsAvailableForPlayer(Player player)|
|DoubleExistenceRune|relics:DOUBLE_EXISTENCE_RUNE.description|IsAvailableForPlayer, BeforeTurnEnd|{}|public override bool IsAvailableForPlayer(Player player)|
|DoubleVisionRune|relics:DOUBLE_VISION_RUNE.description|AfterRoomEntered, AfterCombatEnd, AfterRewardTaken|{}|详见 `src/Runes/DoubleVisionRune.Duplication.cs:9`|
|DrainRune|relics:DRAIN_RUNE.description|IsAvailableForPlayer, AfterSummon|{}|public override bool IsAvailableForPlayer(Player player)|
|DrawForge|relics:DRAW_FORGE.description|ModifyHandDraw|{'Cards': '1'}|详见 `src/Forges/HextechForges.Gold.cs:182`|
|DrawYourSwordRune|relics:DRAW_YOUR_SWORD_RUNE.description|IsAvailableForPlayer, ShouldConvert, ShouldConvertAppliedPower, ApplyConvertedPower, RevertOriginalPower|{'FocusPower': '2'}|public override bool IsAvailableForPlayer(Player player)|
|DualWieldEnemyHex|relics:dualWieldRune.enemyDescription||{}|详见 `src/EnemyHexes/DualWieldEnemyHex.cs:6`|
|DualWieldRune|relics:DUAL_WIELD_RUNE.description|ModifyCardPlayCount, AfterModifyingCardPlayCount, ModifyDamageMultiplicativeCompat|{}|详见 `src/Runes/DualWieldRune.cs:3`|
|DualcastUpgradeRune|relics:DUALCAST_UPGRADE_RUNE.description|IsAvailableForCharacter, MeetsCardAvailabilityRequirement, ModifyCardPlayResultPileTypeAndPositionCompat|{}|protected override bool IsAvailableForCharacter(Player player)|
|DuffsVintageEnemyHex|relics:duffsVintageRune.enemyDescription|ShouldFlush, BeforeTurnEnd|{}|详见 `src/EnemyHexes/DuffsVintageEnemyHex.cs:3`|
|DuffsVintageRune|relics:DUFFS_VINTAGE_RUNE.description|ShouldFlush, BeforeTurnEnd|{'CostReduction': '1'}|详见 `src/Runes/DuffsVintageRune.cs:3`|
|EarthAwakensRune|relics:EARTH_AWAKENS_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterPlayerTurnStartLate, AfterPlayerTurnStart|{'RollingBoulderPower': '5'}|详见 `src/Runes/EarthAwakensRune.cs:3`|
|EasyDoesItRune|relics:EASY_DOES_IT_RUNE.description|BeforeCombatStart|{'MayhemPower': '1'}|详见 `src/Runes/EasyDoesItRune.cs:3`|
|EchoFormUpgradeRune|relics:ECHO_FORM_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);|
|EchoRune|relics:ECHO_RUNE.description|IsAvailableForPlayer, AfterCardGeneratedForCombat|{}|public override bool IsAvailableForPlayer(Player player)|
|EightPennyGateEnemyHex|relics:hungryHex.enemyDescription|ModifyCardPlayResultPileTypeAndPosition|{}|return TryConsumeExhaustSlot(context.Tracking, card.Owner.NetId, limit); internal static bool TryConsumeExhaustSlot(|
|EightPennyGateRune|relics:EIGHT_PENNY_GATE_RUNE.description|ModifyCardPlayResultPileTypeAndPositionCompat, ModifyCardPlayCount, AfterModifyingCardPlayCount|{'Replays': '1'}|详见 `src/Runes/EightPennyGateRune.cs:3`|
|ElectricSurgeRune|relics:ELECTRIC_SURGE_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'OrbCount': '1'}|public override bool IsAvailableForPlayer(Player player)|
|ElectrodynamicsRune|relics:ELECTRODYNAMICS_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'OrbCount': '1'}|public override bool IsAvailableForPlayer(Player player)|
|EmbersForge|relics:EMBERS_FORGE.description||{}|详见 `src/Forges/HextechEnchantForges.cs:64`|
|EmergenceRune|relics:EMERGENCE_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'OrbCount': '2'}|public override bool IsAvailableForPlayer(Player player)|
|EndlessRecoveryRune|relics:ENDLESS_RECOVERY_RUNE.description|AfterRoomEntered|{'HealPercent': '10'}|详见 `src/Runes/EndlessRecoveryRune.cs:3`|
|EndlessRotationEnemyHex|relics:endlessRotationRune.enemyDescription|AfterShuffle|{}|详见 `src/EnemyHexes/EndlessRotationEnemyHex.cs:3`|
|EndlessRotationRune|relics:ENDLESS_ROTATION_RUNE.description|AfterCloned, BeforeCombatStart, AfterCombatEnd, AfterSideTurnEndLate, TryModifyStarCost, AfterShuffle|{}|详见 `src/Runes/EndlessRotationRune.cs:3`|
|EnergyForge|relics:ENERGY_FORGE.description|AfterEnergyResetLate|{'Energy': '1'}|详见 `src/Forges/HextechForges.Prismatic.cs:75`|
|EnlightenmentEnemyHex|relics:enlightenmentRune.enemyDescription|ModifyEnergyCostInCombatLate|{}|详见 `src/EnemyHexes/EnlightenmentEnemyHex.cs:3`|
|EnlightenmentRune|relics:ENLIGHTENMENT_RUNE.description|TryModifyEnergyCostInCombatLate|{}|详见 `src/Runes/EnlightenmentRune.cs:3`|
|EscapePlanEnemyHex|relics:escapePlanRune.enemyDescription|BeforePlayerSideTurnStart, AfterEnemyHealthThreshold|{}|详见 `src/EnemyHexes/EscapePlanEnemyHex.cs:3`|
|EscapePlanRune|relics:ESCAPE_PLAN_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterDamageReceived, AfterPlayerTurnStart|{'ThresholdPercent': '50', 'BlockPercent': '60'}|详见 `src/Runes/EscapePlanRune.cs:5`|
|EternalArmorUpgradeRune|relics:ETERNAL_ARMOR_UPGRADE_RUNE.description|IsAvailableForCharacter, BeforeCombatStart, AfterCombatEnd, AfterCardPlayed, TryModifyPowerAmountReceived|{}|protected override bool IsAvailableForCharacter(Player player)|
|EurekaRune|relics:EUREKA_RUNE.description|ModifyMaxEnergy|{'RelicsNeeded': '6', 'Energy': '1'}|详见 `src/Runes/EurekaRune.cs:3`|
|ExoskeletonEnemyHex|relics:exoskeletonHex.enemyDescription|ApplyCombatStartToEnemy|{}|详见 `src/EnemyHexes/ExoskeletonEnemyHex.cs:3`|
|ExoskeletonHex|relics:EXOSKELETON_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|ExplosionArtRune|relics:EXPLOSION_ART_RUNE.description|IsAvailableForPlayer, BeforeHandDraw|{'TurnStartCards': '1'}|public override bool IsAvailableForPlayer(Player player)|
|ExposeUpgradeRune|relics:EXPOSE_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player)|
|ExtremeSpeedRune|relics:EXTREME_SPEED_RUNE.description|IsAvailableForPlayer, AfterObtained|{'MaxHpLoss': '5'}|public override bool IsAvailableForPlayer(Player player)|
|FallingStarUpgradeRune|relics:FALLING_STAR_UPGRADE_RUNE.description|MeetsCardAvailabilityRequirement, IsAvailableForCharacter, AfterCardPlayed|{'StunTurns': '1'}|protected override bool IsAvailableForCharacter(Player player)|
|FanTheHammerRune|relics:FAN_THE_HAMMER_RUNE.description|BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, ModifyCardPlayCount, AfterModifyingCardPlayCount, ModifyDamageMultiplicativeCompat|{'Replays': '3', 'DamageMultiplier': '0.35'}|if (TryConsumeTurnProc(nameof(FanTheHammerRune), ref _triggeredThisTurn))|
|FeedUpgradeRune|relics:FEED_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterRoomEntered, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);|
|FeelTheBurnCard|cards:FEEL_THE_BURN_CARD.description|OnPlay, OnUpgrade|{'HextechBurnPower': '5'}|详见 `src/Cards/FeelTheBurnCard.cs:3`|
|FeelTheBurnEnemyHex|relics:feelTheBurnRune.enemyDescription|AfterEnemyHealthThreshold, BeforeEnemySideTurnStart|{}|详见 `src/EnemyHexes/FeelTheBurnEnemyHex.cs:3`|
|FeelTheBurnRune|relics:FEEL_THE_BURN_RUNE.description|AfterObtained|{'Cards': '1'}|详见 `src/Runes/FeelTheBurnRune.cs:3`|
|FeyMagicEnemyHex|relics:feyMagicRune.enemyDescription|BeforePlayerSideTurnStart, AfterEnemyDamageGivenPlayerHit|{}|详见 `src/EnemyHexes/FeyMagicEnemyHex.cs:3`|
|FeyMagicRune|relics:FEY_MAGIC_RUNE.description|AfterDamageGiven|{'MinCost': '3'}|详见 `src/Runes/FeyMagicRune.cs:3`|
|FinalFormEnemyHex|relics:finalFormRune.enemyDescription|AfterEnemyDamageGivenPlayerHit|{}|详见 `src/EnemyHexes/FinalFormEnemyHex.cs:3`|
|FinalFormRune|relics:FINAL_FORM_RUNE.description|BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, AfterCardPlayed|{'MinCost': '2', 'PlatingPercent': '0.10', 'Cards': '2'}|if (!TryConsumeTurnProc(nameof(FinalFormRune), ref _triggeredThisTurn))|
|FirebrandEnemyHex|relics:firebrandRune.enemyDescription|AfterEnemyDamageGivenImmediate|{}|详见 `src/EnemyHexes/FirebrandEnemyHex.cs:3`|
|FirebrandRune|relics:FIREBRAND_RUNE.description|AfterDamageGiven|{}|详见 `src/Runes/FirebrandRune.cs:3`|
|FireworksForge|relics:FIREWORKS_FORGE.description|BeforeCombatStart|{'Damage': '6'}|详见 `src/Forges/HextechForges.Silver.cs:302`|
|FirstAidKitEnemyHex|relics:firstAidKitRune.enemyDescription|ModifyBlockMultiplicative, ModifyEnemyHealMultiplicative|{}|详见 `src/EnemyHexes/FirstAidKitEnemyHex.cs:3`|
|FirstAidKitRune|relics:FIRST_AID_KIT_RUNE.description|ModifyBlockMultiplicative|{}|详见 `src/Runes/FirstAidKitRune.cs:3`|
|FlakCannonUpgradeRune|relics:FLAK_CANNON_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);|
|FlameBarrierUpgradeRune|relics:FLAME_BARRIER_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);|
|FlawlessRune|relics:FLAWLESS_RUNE.description|IsAvailableForPlayer, AfterCardPlayed|{'Block': '3'}|public override bool IsAvailableForPlayer(Player player)|
|FleshAndBoneRune|relics:FLESH_AND_BONE_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd|{'HpLoss': '3', 'Summon': '15', 'OstyMaxHpPerHeal': '10', 'OstyHeal': '1'}|public override bool IsAvailableForPlayer(Player player)|
|FleshForge|relics:FLESH_FORGE.description|BeforeCombatStart|{'SleightOfFleshPower': '4'}|详见 `src/Forges/HextechForges.Gold.cs:312`|
|FlyingKickRune|relics:FLYING_KICK_RUNE.description|AfterDamageGiven|{'BaseExecutePercent': '10', 'OwnerMaxHpToExecutePercent': '8', 'ExecutePercent': '10', 'Heal': '10'}|if (!FlyingKickCorpseLaunchDriver.TryConsumePending(__instance.Entity))|
|FocusForge|relics:FOCUS_FORGE.description|IsAvailableForPlayer, BeforeCombatStart|{'FocusPower': '2'}|public override bool IsAvailableForPlayer(Player player)|
|ForbiddenGrimoireEnemyHex|relics:forbiddenGrimoireRune.enemyDescription||{}|详见 `src/EnemyHexes/ForbiddenGrimoireEnemyHex.cs:3`|
|ForbiddenGrimoireRune|relics:FORBIDDEN_GRIMOIRE_RUNE.description|BeforeCombatStart|{'ForbiddenGrimoirePower': '1'}|详见 `src/Runes/ForbiddenGrimoireRune.cs:3`|
|ForgingForge|relics:FORGING_FORGE.description|IsAvailableForPlayer, AfterSideTurnStart|{'ForgeAmount': '8'}|public override bool IsAvailableForPlayer(Player player)|
|ForgottenSoulEnemyHex|relics:forgottenSoulRune.enemyDescription|BeforeTurnEnd|{}|详见 `src/EnemyHexes/ForgottenSoulEnemyHex.cs:5`|
|ForgottenSoulRune|relics:FORGOTTEN_SOUL_RUNE.description|ModifyCardPlayResultPileTypeAndPositionCompat, AfterModifyingCardPlayResultPileOrPositionCompat|{}|详见 `src/Runes/ForgottenSoulRune.cs:3`|
|FortuneForge|relics:FORTUNE_FORGE.description|AfterCombatEnd|{'Gold': '100'}|详见 `src/Forges/HextechForges.Prismatic.cs:214`|
|FossilStalkerEnemyHex|relics:fossilStalkerHex.enemyDescription|ApplyCombatStartToEnemy|{}|详见 `src/EnemyHexes/FossilStalkerEnemyHex.cs:3`|
|FossilStalkerHex|relics:FOSSIL_STALKER_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|FrostWraithEnemyHex|relics:frostWraithRune.enemyDescription|BeforeEnemySideTurnStart|{}|详见 `src/EnemyHexes/FrostWraithEnemyHex.cs:3`|
|FrostWraithRune|relics:FROST_WRAITH_RUNE.description|BeforeCombatStart, AfterPlayerTurnStart|{'TurnsNeeded': '2'}|详见 `src/Runes/FrostWraithRune.cs:3`|
|FuriousGlareRune|relics:FURIOUS_GLARE_RUNE.description|IsAvailableForPlayer, AfterPowerAmountChanged|{'VulnerablePower': '1', 'StrengthPower': '1'}|public override bool IsAvailableForPlayer(Player player)|
|GalacticGiftRune|relics:GALACTIC_GIFT_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, AfterStarsSpent|{'Stars': '1'}|public override bool IsAvailableForPlayer(Player player)|
|GetExcitedEnemyHex|relics:getExcitedRune.enemyDescription|BeforeSideTurnStart, BeforeDeath, AfterDeath|{}|详见 `src/EnemyHexes/GetExcitedEnemyHex.cs:3`|
|GetExcitedRune|relics:GET_EXCITED_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterDeath, AfterPlayerTurnStartEarly|{}|详见 `src/Runes/GetExcitedRune.cs:3`|
|GhostFormRune|relics:GHOST_FORM_RUNE.description|BeforeCombatStart|{'IntangiblePower': '3', 'NoBlockPower': '3'}|详见 `src/Runes/GhostFormRune.cs:3`|
|GiantSerpentsFangRune|relics:GIANT_SERPENTS_FANG_RUNE.description|AfterDamageGiven|{'BlockReductionPercent': '50'}|详见 `src/Runes/GiantSerpentsFangRune.cs:3`|
|GiantSlayerEnemyHex|relics:giantSlayerRune.enemyDescription|ModifyDamageMultiplicative, ApplyCombatStartToEnemy|{}|详见 `src/EnemyHexes/GiantSlayerEnemyHex.cs:3`|
|GiantSlayerRune|relics:GIANT_SLAYER_RUNE.description|AfterObtained, AfterRoomEntered, ModifyHandDraw, ModifyDamageMultiplicativeCompat|{'Cards': '2', 'EnemyMaxHpPerPercent': '8', 'DamagePerStepPercent': '0.01', 'MaxBonusPercent': '0.5', 'Scale': '0.65'}|详见 `src/Runes/GiantSlayerRune.cs:3`|
|GlamForge|relics:GLAM_FORGE.description||{}|详见 `src/Forges/HextechEnchantForges.cs:46`|
|GlassCannonEnemyHex|relics:glassCannonRune.enemyDescription|ModifyDamageMultiplicative, ModifyEnemyHealAmount, ApplyPersistentToEnemy|{}|详见 `src/EnemyHexes/GlassCannonEnemyHex.cs:3`|
|GlassCannonRune|relics:GLASS_CANNON_RUNE.description|AfterObtained, ModifyDamageMultiplicativeCompat|{'DamageMultiplier': '1.5', 'HealCapPercent': '0.7'}|详见 `src/Runes/GlassCannonRune.cs:3`|
|GlobeHeadEnemyHex|relics:globeHeadHex.enemyDescription|ApplyCombatStartPlayerDebuffs|{}|详见 `src/EnemyHexes/GlobeHeadEnemyHex.cs:3`|
|GlobeHeadHex|relics:GLOBE_HEAD_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|GloomyCloudsRune|relics:GLOOMY_CLOUDS_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{}|public override bool IsAvailableForPlayer(Player player)|
|GoldAttackForge|relics:GOLD_ATTACK_FORGE.description|ModifyDamageMultiplicativeCompat|{'DamageMultiplier': '1.1'}|详见 `src/Forges/HextechForges.Gold.cs:119`|
|GoldCardCustomerRune|relics:GOLD_CARD_CUSTOMER_RUNE.description|AfterObtained|{}|详见 `src/Runes/GoldCardCustomerRune.cs:5`|
|GoldFocusForge|relics:GOLD_FOCUS_FORGE.description|IsAvailableForPlayer, BeforeCombatStart|{'FocusPower': '1'}|public override bool IsAvailableForPlayer(Player player)|
|GoldHpForge|relics:GOLD_HP_FORGE.description|AfterObtained|{'MaxHpPercent': '15'}|详见 `src/Forges/HextechForges.Gold.cs:81`|
|GoldLifeForge|relics:GOLD_LIFE_FORGE.description|AfterObtained|{'MaxHp': '20'}|详见 `src/Forges/HextechForges.Gold.cs:59`|
|GoldProtectionForge|relics:GOLD_PROTECTION_FORGE.description|ModifyBlockMultiplicative|{'SustainMultiplier': '1.1'}|详见 `src/Forges/HextechForges.Gold.cs:136`|
|GoldUpgradeForge|relics:GOLD_UPGRADE_FORGE.description|AfterObtained|{'Cards': '4'}|详见 `src/Forges/HextechForges.Gold.cs:244`|
|GoldenSpatulaEnemyHex|relics:goldenSpatulaRune.enemyDescription|ModifyDamageMultiplicative, ModifyBlockMultiplicative, ModifyEnemyHealMultiplicative, ApplyPersistentToEnemy|{}|详见 `src/EnemyHexes/GoldenSpatulaEnemyHex.cs:3`|
|GoldenSpatulaRune|relics:GOLDEN_SPATULA_RUNE.description|AfterRoomEntered, AfterCombatVictory, ModifyDamageMultiplicativeCompat, ModifyBlockMultiplicative|{'StackBonusPercent': '1', 'StackOverloadThreshold': '10'}|详见 `src/Runes/GoldenSpatulaRune.cs:3`|
|GoldrendEnemyHex|relics:goldrendRune.enemyDescription|ModifyDamageMultiplicative, AfterEnemyDamageGivenImmediate|{}|详见 `src/EnemyHexes/GoldrendEnemyHex.cs:3`|
|GoldrendRune|relics:GOLDREND_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterDamageGiven|{'CountPerHit': '10'}|详见 `src/Runes/GoldrendRune.cs:3`|
|GoliathEnemyHex|relics:goliathRune.enemyDescription|ModifyDamageMultiplicative, ModifyBlockMultiplicative, ModifyEnemyHealMultiplicative, ApplyPersistentToEnemy|{}|详见 `src/EnemyHexes/GoliathEnemyHex.cs:3`|
|GoliathRune|relics:GOLIATH_RUNE.description|AfterObtained, AfterRoomEntered, ModifyDamageMultiplicativeCompat, ModifyBlockMultiplicative|{'HpGainPercent': '0.35', 'DamageMultiplier': '1.2', 'SustainMultiplier': '1.2', 'Scale': '1.35'}|详见 `src/Runes/GoliathRune.cs:3`|
|GoodLuckRune|relics:GOOD_LUCK_RUNE.description|TryModifyCardRewardOptions|{}|详见 `src/Runes/GoodLuckRune.cs:5`|
|GrandFinaleUpgradeRune|relics:GRAND_FINALE_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|GripHex|relics:GRIP_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|GroundedRune|relics:GROUNDED_RUNE.description|IsAvailableForPlayer, BeforeTurnEnd|{}|public override bool IsAvailableForPlayer(Player player)|
|GrowingStrongerRune|relics:GROWING_STRONGER_RUNE.description|IsAvailableForPlayer, AfterPowerAmountChanged|{}|public override bool IsAvailableForPlayer(Player player)|
|HailToTheKingEnemyHex|relics:hailToTheKingRune.enemyDescription|ApplyCombatStartToEnemy|{}|详见 `src/EnemyHexes/HailToTheKingEnemyHex.cs:3`|
|HailToTheKingRune|relics:HAIL_TO_THE_KING_RUNE.description|AfterCombatVictory|{'InitialForgeCount': '2', 'EliteForgeCount': '1', 'BossForgeCount': '1'}|详见 `src/Runes/HailToTheKingRune.cs:3`|
|HandOfBaronEnemyHex|relics:handOfBaronRune.enemyDescription|ModifyDamageMultiplicative, BeforePlayerSideTurnStart|{}|详见 `src/EnemyHexes/HandOfBaronEnemyHex.cs:3`|
|HandOfBaronRune|relics:HAND_OF_BARON_RUNE.description|ModifyDamageMultiplicativeCompat, BeforeSideTurnStart|{'DamageMultiplier': '1.2', 'Shrink': '2'}|详见 `src/Runes/HandOfBaronRune.cs:3`|
|HangUpgradeRune|relics:HANG_UPGRADE_RUNE.description|IsAvailableForCharacter, TryModifyKeywordsInCombat|{}|protected override bool IsAvailableForCharacter(Player player) => IsNecrobinderPlayer(player);|
|HappyAccidentRune|relics:HAPPY_ACCIDENT_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'OrbCount': '1'}|public override bool IsAvailableForPlayer(Player player)|
|HardBonesRune|relics:HARD_BONES_RUNE.description|IsAvailableForPlayer, BeforeCombatStart|{'CalcifyPower': '8'}|public override bool IsAvailableForPlayer(Player player)|
|HastyScribbleEnemyHex|relics:hastyScribbleRune.enemyDescription|ModifyGeneratedMapLate|{}|详见 `src/EnemyHexes/HastyScribbleEnemyHex.cs:5`|
|HastyScribbleRune|relics:HASTY_SCRIBBLE_RUNE.description|AfterPlayerTurnStartLate|{}|详见 `src/Runes/HastyScribbleRune.cs:3`|
|HattrickRune|relics:HATTRICK_RUNE.description|ShouldAllowSelectingMoreCardRewards|{}|详见 `src/Runes/HattrickRune.cs:3`|
|HauntedShipEnemyHex|relics:hauntedShipHex.enemyDescription|BeforePlayerSideTurnStart|{}|详见 `src/EnemyHexes/HauntedShipEnemyHex.cs:3`|
|HauntedShipHex|relics:HAUNTED_SHIP_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|HeavyHitterEnemyHex|relics:heavyHitterRune.enemyDescription|ModifyDamageMultiplicative|{}|详见 `src/EnemyHexes/HeavyHitterEnemyHex.cs:3`|
|HeavyHitterRune|relics:HEAVY_HITTER_RUNE.description|ModifyDamageMultiplicativeCompat|{}|详见 `src/Runes/HeavyHitterRune.cs:3`|
|HextechAttackReplayPower|powers:HEXTECH_ATTACK_REPLAY_POWER.description|ModifyCardPlayCount, AfterModifyingCardPlayCount|{}|详见 `src/Powers/HextechPowers.cs:135`|
|HextechBurnPower|powers:HEXTECH_BURN_POWER.description|AfterSideTurnStart, BeforeTurnEnd|{}|详见 `src/Powers/HextechPowers.cs:5`|
|HextechChemtechDragonSoulPower|powers:HEXTECH_CHEMTECH_DRAGON_SOUL_POWER.description|AfterSideTurnStart|{}|详见 `src/Cards/HextechDragonSoulPowers.cs:162`|
|HextechCloudDragonSoulPower|powers:HEXTECH_CLOUD_DRAGON_SOUL_POWER.description|ModifyHandDraw|{}|详见 `src/Cards/HextechDragonSoulPowers.cs:197`|
|HextechDragonSoulCard|cards:HEXTECH_DRAGON_SOUL_CARD.description|OnPlay|{'Energy': '1'}|详见 `src/Cards/HextechDragonSoulCards.cs:67`|
|HextechDragonSoulPower|powers:HEXTECH_DRAGON_SOUL_POWER.description|AfterEnergyResetLate|{}|详见 `src/Cards/HextechDragonSoulPowers.cs:126`|
|HextechDragonSoulRune|relics:HEXTECH_DRAGON_SOUL_RUNE.description||{}|详见 `src/Runes/HextechDragonSoulRune.cs:3`|
|HextechGalvanicPower|powers:HEXTECH_GALVANIC_POWER.description|AfterApplied, BeforeCombatStart, AfterCardEnteredCombat, AfterCardPlayed|{}|详见 `src/Powers/HextechGalvanicPower.cs:15`|
|HextechHangPower|powers:HEXTECH_HANG_POWER.description|ModifyDamageMultiplicativeCompat|{}|详见 `src/Powers/HextechHangPower.cs:3`|
|HextechInfernalDragonSoulPower|powers:HEXTECH_INFERNAL_DRAGON_SOUL_POWER.description|AfterSideTurnStart, AfterCardPlayed|{}|\|\| !cardPlay.IsFirstInSeries; \|\| cardPlay.IsAutoPlay; if (!TryConsumeTriggerThisTurn()); private bool TryConsumeTriggerThisTurn(); return modifier.TryConsumePlayerRuneProcThisTurn(player, nameof(HextechInfernalDragonSoulPower), 1);|
|HextechMountainDragonSoulPower|powers:HEXTECH_MOUNTAIN_DRAGON_SOUL_POWER.description|AfterSideTurnStart|{}|详见 `src/Cards/HextechDragonSoulPowers.cs:144`|
|HextechNextTurnDamagePower|powers:HEXTECH_NEXT_TURN_DAMAGE_POWER.description|AfterSideTurnStart|{}|详见 `src/Powers/HextechNextTurnDamagePower.cs:3`|
|HextechOceanDragonSoulPower|powers:HEXTECH_OCEAN_DRAGON_SOUL_POWER.description|AfterTurnEnd|{}|详见 `src/Cards/HextechDragonSoulPowers.cs:3`|
|HextechPlayerSlowPower|powers:HEXTECH_PLAYER_SLOW_POWER.description|ModifyDamageMultiplicativeCompat, AfterModifyingDamageAmount|{}|详见 `src/Powers/HextechPowers.cs:170`|
|HextechVitalSparkPower|powers:HEXTECH_VITAL_SPARK_POWER.description|AfterApplied, BeforeCombatStart, AfterCardEnteredCombat, AfterCardPlayed, AfterPowerAmountChanged, AfterRemoved|{}|详见 `src/Powers/HextechVitalSparkPower.cs:7`|
|HiddenGemUpgradeRune|relics:HIDDEN_GEM_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|HomeguardRune|relics:HOMEGUARD_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterDamageReceived, AfterPlayerTurnStart|{}|详见 `src/Runes/HomeguardRune.cs:3`|
|HotfixUpgradeRune|relics:HOTFIX_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player)|
|HourglassForge|relics:HOURGLASS_FORGE.description|AfterPlayerTurnStartEarly|{'Damage': '5'}|详见 `src/Forges/HextechForges.Gold.cs:214`|
|HubrisRune|relics:HUBRIS_RUNE.description|AfterCombatVictory, AfterRoomEntered, ModifyHandDraw|{'StacksPerBonus': '3', 'StrengthPower': '1', 'Cards': '1'}|详见 `src/Runes/HubrisRune.cs:3`|
|HundredRefinementsEnemyHex|relics:hundredRefinementsHex.enemyDescription|AfterEnemyDamageReceivedAny|{}|详见 `src/EnemyHexes/HundredRefinementsEnemyHex.cs:3`|
|HundredRefinementsHex|relics:HUNDRED_REFINEMENTS_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|HundredRefinementsRune|relics:HUNDRED_REFINEMENTS_RUNE.description|TryModifyCardRewardAlternatives|{'BodyForges': '2', 'ForgeCount': '1'}|详见 `src/Runes/HundredRefinementsRune.cs:6`|
|HungryHex|relics:HUNGRY_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|IGripEnemyHex|relics:gripHex.enemyDescription|AfterCardPlayed|{}|if (cardPlay.IsAutoPlay; \|\| !cardPlay.IsFirstInSeries; if (TryConsumeFirstCard(context.Tracking, owner.NetId, amount)); internal static bool TryConsumeFirstCard(|
|IInspectEnemyHex|relics:inspectHex.enemyDescription|ShouldDraw|{}|详见 `src/EnemyHexes/IInspectEnemyHex.cs:3`|
|IllusoryWeaponRune|relics:ILLUSORY_WEAPON_RUNE.description|AfterCardPlayed|{'Damage': '2'}|详见 `src/Runes/IllusoryWeaponRune.cs:8`|
|ImmortalBoneRune|relics:IMMORTAL_BONE_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'HealPercent': '50'}|public override bool IsAvailableForPlayer(Player player)|
|InfernalConduitRune|relics:INFERNAL_CONDUIT_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterDamageGiven, BeforeTurnEnd, AfterPlayerTurnStartEarly|{}|详见 `src/Runes/InfernalConduitRune.cs:3`|
|InfernalDragonSoulCard|cards:INFERNAL_DRAGON_SOUL_CARD.description|OnPlay|{'BurnPower': '8'}|详见 `src/Cards/HextechDragonSoulCards.cs:41`|
|InfernalDragonSoulRune|relics:INFERNAL_DRAGON_SOUL_RUNE.description||{}|详见 `src/Runes/InfernalDragonSoulRune.cs:3`|
|InfernoUpgradeRune|relics:INFERNO_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);|
|InfestedPrismHex|relics:INFESTED_PRISM_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|InfiniteLoopRune|relics:INFINITE_LOOP_RUNE.description|ModifyMaxEnergy, AfterCombatVictory|{'Energy': '1', 'StacksPerEnergy': '4'}|详见 `src/Runes/InfiniteLoopRune.cs:3`|
|InkletEnemyHex|relics:inkletHex.enemyDescription|ApplyCombatStartToEnemy|{}|详见 `src/EnemyHexes/InkletEnemyHex.cs:3`|
|InkletHex|relics:INKLET_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|InkshadowRune|relics:INKSHADOW_RUNE.description|IsAvailableForPlayer, AfterCardGeneratedForCombat, AfterCardEnteredCombat, TryModifyCardBeingAddedToDeck|{}|public override bool IsAvailableForPlayer(Player player)|
|InspectHex|relics:INSPECT_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|InstantDeathRune|relics:INSTANT_DEATH_RUNE.description|IsAvailableForPlayer, AfterPowerAmountChanged, AfterCurrentHpChanged|{}|public override bool IsAvailableForPlayer(Player player)|
|IronWaveUpgradeRune|relics:IRON_WAVE_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|JackpotUpgradeRune|relics:JACKPOT_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|JeweledGauntletEnemyHex|relics:jeweledGauntletRune.enemyDescription||{}|详见 `src/EnemyHexes/JeweledGauntletEnemyHex.cs:6`|
|JeweledGauntletRune|relics:JEWELED_GAUNTLET_RUNE.description|BeforeCombatStart, AfterCombatEnd, ModifyCardPlayCount, BeforeCardPlayed|{}|详见 `src/Runes/JeweledGauntletRune.cs:3`|
|JinlianBoxEnemyHex|relics:jinlianBoxRune.enemyDescription|TryModifyCardRewardOptions, TryModifyCardRewardOptionsLate|{}|详见 `src/EnemyHexes/JinlianBoxEnemyHex.cs:3`|
|JinlianBoxRune|relics:JINLIAN_BOX_RUNE.description|IsAvailableForPlayer, AfterObtained|{}|public override bool IsAvailableForPlayer(Player player)|
|JudicatorEnemyHex|relics:judicatorRune.enemyDescription|ModifyDamageMultiplicative|{}|详见 `src/EnemyHexes/JudicatorEnemyHex.cs:3`|
|JudicatorRune|relics:JUDICATOR_RUNE.description|ModifyDamageMultiplicativeCompat, AfterDeath|{'Energy': '1', 'DamageMultiplier': '1.25'}|详见 `src/Runes/JudicatorRune.cs:3`|
|JuggernautUpgradeRune|relics:JUGGERNAUT_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|KakaRune|relics:KAKA_RUNE.description|ShouldPlay, AfterPlayerTurnStart|{'RitualPower': '1'}|详见 `src/Runes/KakaRune.cs:3`|
|KeystoneHunterRune|relics:KEYSTONE_HUNTER_RUNE.description|IsAvailableForPlayer, BeforeCombatStart|{'ToolsOfTheTradePower': '1', 'MasterPlannerPower': '1'}|public override bool IsAvailableForPlayer(Player player)|
|KillerHunterRune|relics:KILLER_HUNTER_RUNE.description|IsAvailableForPlayer, AfterCardPlayed|{'TemporaryStatLoss': '1'}|public override bool IsAvailableForPlayer(Player player)|
|KingdomArmyRune|relics:KINGDOM_ARMY_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, AfterForge|{}|public override bool IsAvailableForPlayer(Player player) => IsRegentPlayer(player);|
|KnowThyPlaceUpgradeRune|relics:KNOW_THY_PLACE_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player)|
|LagavulinMatriarchEnemyHex|relics:lagavulinMatriarchHex.enemyDescription|BeforePlayerSideTurnStart|{}|if (!context.TryConsumeRoundInterval(|
|LagavulinMatriarchHex|relics:LAGAVULIN_MATRIARCH_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|LeafSlimeEnemyHex|relics:leafSlimeHex.enemyDescription|BeforePlayerSideTurnStart|{}|if (!context.TryConsumeRoundInterval(|
|LeafSlimeHex|relics:LEAF_SLIME_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|LethalTempoRune|relics:LETHAL_TEMPO_RUNE.description|IsAvailableForPlayer, AfterCardPlayed|{'StrengthPower': '1'}|public override bool IsAvailableForPlayer(Player player)|
|LifeFlowRune|relics:LIFE_FLOW_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, AfterCardExhausted|{'HealPercent': '0.05', 'MaxProcsPerTurn': '3'}|public override int DisplayAmount => !IsCanonical ? Math.Max(0, DynamicVars["MaxProcsPerTurn"].IntValue - GetTurnProcCount(nameof(LifeFlowRune), _procsThisTurn)) : 0;; new DynamicVar("MaxProcsPerTurn", 3m); public override bool IsAvailableForPlayer(Player player); \|\| HasTurnProcReachedLimit(nameof(LifeFlowRune), _procsThisTurn, DynamicVars["MaxProcsPerTurn"].IntValue)); if (!TryConsumeTurnProc(nameof(LifeFlowRune), ref _procsThisTurn, DynamicVars["MaxProcsPerTurn"].IntValue))|
|LifeForge|relics:LIFE_FORGE.description|AfterObtained|{'MaxHp': '8'}|详见 `src/Forges/HextechForges.Silver.cs:139`|
|LightEmUpEnemyHex|relics:lightEmUpRune.enemyDescription|ModifyPlayerAttackEnergyCostMultiplier|{}|详见 `src/EnemyHexes/LightEmUpEnemyHex.cs:3`|
|LightEmUpRune|relics:LIGHT_EM_UP_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterCardPlayed|{'Attacks': '4', 'Missiles': '6'}|return cardPlay.IsFirstInSeries; && !cardPlay.IsAutoPlay|
|LingeringMightRune|relics:LINGERING_MIGHT_RUNE.description|AfterCombatEnd, BeforeCombatStart|{}|详见 `src/Runes/LingeringMightRune.cs:9`|
|LivingFogEnemyHex|relics:livingFogHex.enemyDescription|ShouldPlay, BeforeCardPlayed|{}|internal const int SkillLimit = 3;; if (cardPlay.IsFirstInSeries && card.Type == CardType.Skill; HextechCombatProcTracker.TryConsumePlayerRuneProcThisTurn(context.Tracking, card.Owner, ProcKey, SkillLimit);|
|LivingFogHex|relics:LIVING_FOG_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|LoopEnemyHex|relics:loopRune.enemyDescription|ModifyHandDraw|{}|详见 `src/EnemyHexes/LoopEnemyHex.cs:3`|
|LoopRune|relics:LOOP_RUNE.description|ModifyMaxEnergy|{'Energy': '1'}|详见 `src/Runes/LoopRune.cs:3`|
|LoopUpgradeRune|relics:LOOP_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);|
|LubricantRune|relics:LUBRICANT_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, TryModifyEnergyCostInCombat, TryModifyStarCost, AfterCardPlayed|{}|public override bool IsAvailableForPlayer(Player player); \|\| cardPlay.IsAutoPlay; \|\| !cardPlay.IsFirstInSeries; if (!TryConsumeTurnProc(nameof(LubricantRune), ref _usedThisTurn))|
|MadScientistEnemyHex|relics:madScientistRune.enemyDescription|ApplyPersistentToEnemy, AfterEnemyDamageReceived|{}|详见 `src/EnemyHexes/MadScientistEnemyHex.cs:3`|
|MadScientistRune|relics:MAD_SCIENTIST_RUNE.description|IsAvailableForPlayer, AfterOrbChanneled|{'OrbSlots': '1'}|public override bool IsAvailableForPlayer(Player player)|
|MagicMissileRune|relics:MAGIC_MISSILE_RUNE.description|BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, AfterCardPlayed|{'Missiles': '3', 'MaxHpDamagePercent': '3'}|\|\| !cardPlay.IsFirstInSeries; \|\| !TryConsumeTurnProc(nameof(MagicMissileRune), ref _triggeredThisTurn))|
|MakeItMineRune|relics:MAKE_IT_MINE_RUNE.description|IsAvailableForPlayer, AfterCombatVictory, BeforeCombatStart, AfterPlayerTurnStart|{'Summon': '4'}|public override bool IsAvailableForPlayer(Player player)|
|ManipulateRealityEnemyHex|relics:manipulateRealityRune.enemyDescription||{}|详见 `src/EnemyHexes/ManipulateRealityEnemyHex.cs:3`|
|ManipulateRealityRune|relics:MANIPULATE_REALITY_RUNE.description|AfterCardGeneratedForCombat|{}|详见 `src/Runes/ManipulateRealityRune.cs:5`|
|MarkovBabbleRune|relics:MARKOV_BABBLE_RUNE.description|IsAvailableForPlayer, AfterSideTurnStart|{'OrbCount': '1'}|public override bool IsAvailableForPlayer(Player player)|
|MasterOfDualityEnemyHex|relics:masterOfDualityRune.enemyDescription|AfterCardPlayed|{}|详见 `src/EnemyHexes/MasterOfDualityEnemyHex.cs:3`|
|MasterOfDualityRune|relics:MASTER_OF_DUALITY_RUNE.description|AfterCardPlayed|{}|详见 `src/Runes/MasterOfDualityRune.cs:3`|
|MentalShieldRune|relics:MENTAL_SHIELD_RUNE.description|BeforeTurnEnd|{'Block': '2'}|详见 `src/Runes/MentalShieldRune.cs:3`|
|MikaelsBlessingCard|cards:MIKAELS_BLESSING_CARD.description|OnPlay, OnUpgrade|{'HealPercent': '10'}|详见 `src/Cards/MikaelsBlessingCard.cs:3`|
|MikaelsBlessingEnemyHex|relics:mikaelsBlessingRune.enemyDescription|AfterEnemyHealthThreshold|{}|详见 `src/EnemyHexes/MikaelsBlessingEnemyHex.cs:3`|
|MikaelsBlessingRune|relics:MIKAELS_BLESSING_RUNE.description|AfterObtained|{'Cards': '1'}|详见 `src/Runes/MikaelsBlessingRune.cs:3`|
|MindOverMatterEnemyHex|relics:mindOverMatterRune.enemyDescription|AfterCardDrawn|{}|\|\| !TryConsumeFirstDraw(context.Tracking, owner.NetId)); internal static bool TryConsumeFirstDraw(HextechMayhemCombatTrackingState tracking, ulong playerNetId)|
|MindOverMatterRune|relics:MIND_OVER_MATTER_RUNE.description|BeforeHandDraw|{'Cards': '1'}|详见 `src/Runes/MindOverMatterRune.cs:3`|
|MindToMatterRune|relics:MIND_TO_MATTER_RUNE.description|AfterObtained|{'MaxHp': '1'}|详见 `src/Runes/MindToMatterRune.cs:3`|
|MirageRune|relics:MIRAGE_RUNE.description|IsAvailableForPlayer, AfterSideTurnStart|{'Block': '1'}|public override bool IsAvailableForPlayer(Player player)|
|MirrorReflectionEnemyHex|relics:mirrorReflectionRune.enemyDescription|AfterCardPlayed|{}|if (!cardPlay.IsFirstInSeries; \|\| cardPlay.IsAutoPlay|
|MirrorReflectionRune|relics:MIRROR_REFLECTION_RUNE.description|AfterObtained|{}|详见 `src/Runes/MirrorReflectionRune.cs:5`|
|MiserableFateEnemyHex|relics:miserableFateRune.enemyDescription|BeforePlayerSideTurnStart|{}|详见 `src/EnemyHexes/MiserableFateEnemyHex.cs:3`|
|MiserableFateRune|relics:MISERABLE_FATE_RUNE.description|IsAvailableForPlayer, AfterSideTurnStart, BeforeTurnEnd|{'Block': '1'}|public override bool IsAvailableForPlayer(Player player)|
|MiseryRune|relics:MISERY_RUNE.description|AfterPlayerTurnStart|{'StrengthPower': '-1', 'DexterityPower': '-1'}|详见 `src/Runes/MiseryRune.cs:3`|
|MiseryUpgradeRune|relics:MISERY_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player)|
|MobileHomeRune|relics:MOBILE_HOME_RUNE.description|AfterObtained|{}|详见 `src/Runes/MobileHomeRune.cs:5`|
|MoltenFistUpgradeRune|relics:MOLTEN_FIST_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);|
|MomentumForge|relics:MOMENTUM_FORGE.description||{}|详见 `src/Forges/HextechEnchantForges.cs:59`|
|MonarchsGazeEnemyHex|relics:monarchsGazeRune.enemyDescription|AfterCardPlayed|{}|if (!cardPlay.IsFirstInSeries; \|\| cardPlay.IsAutoPlay|
|MonarchsGazeRune|relics:MONARCHS_GAZE_RUNE.description|BeforeCombatStart|{'MonarchsGazePower': '1'}|详见 `src/Runes/MonarchsGazeRune.cs:3`|
|MoreTheMerrierEnemyHex|relics:moreTheMerrierRune.enemyDescription|ModifyDamageMultiplicative, ModifyBlockMultiplicative, ModifyEnemyHealMultiplicative|{}|详见 `src/EnemyHexes/MoreTheMerrierEnemyHex.cs:3`|
|MoreTheMerrierRune|relics:MORE_THE_MERRIER_RUNE.description|ModifyBlockMultiplicative, ModifyDamageMultiplicativeCompat|{'PercentPerRelic': '1.5'}|详见 `src/Runes/MoreTheMerrierRune.cs:3`|
|MoreUniversalScopeRune|relics:MORE_UNIVERSAL_SCOPE_RUNE.description||{}|详见 `src/Runes/MoreUniversalScopeRune.cs:3`|
|MostUniversalScopeRune|relics:MOST_UNIVERSAL_SCOPE_RUNE.description||{}|详见 `src/Runes/MostUniversalScopeRune.cs:3`|
|MountainDragonSoulCard|cards:MOUNTAIN_DRAGON_SOUL_CARD.description|OnPlay|{'PlatingPower': '2'}|详见 `src/Cards/HextechDragonSoulCards.cs:92`|
|MountainSoulEnemyHex|relics:mountainSoulRune.enemyDescription|AfterEnemyDamageReceived, BeforePlayerSideTurnStart|{}|详见 `src/EnemyHexes/MountainSoulEnemyHex.cs:3`|
|MountainSoulRune|relics:MOUNTAIN_SOUL_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterDamageReceived, AfterPlayerTurnStart|{}|详见 `src/Runes/MountainSoulRune.cs:3`|
|MyriadManifestationsRune|relics:MYRIAD_MANIFESTATIONS_RUNE.description|IsAvailableForPlayer, BeforeSideTurnEndEarly|{}|public override bool IsAvailableForPlayer(Player player) => IsDefectPlayer(player);|
|MyriadSwordsRune|relics:MYRIAD_SWORDS_RUNE.description|IsAvailableForPlayer, ModifyCardPlayResultPileTypeAndPositionCompat, AfterShuffle|{}|public override bool IsAvailableForPlayer(Player player)|
|MysteryEnemyHex|relics:mysteryRune.enemyDescription|AfterPlayerTurnStartLate|{}|详见 `src/EnemyHexes/MysteryEnemyHex.cs:5`|
|MysteryRune|relics:MYSTERY_RUNE.description|BeforeCombatStart|{'MayhemPower': '2', 'EntropyPower': '2'}|详见 `src/Runes/MysteryRune.cs:3`|
|MyteEnemyHex|relics:myteHex.enemyDescription|BeforePlayerSideTurnStart|{}|详见 `src/EnemyHexes/MyteEnemyHex.cs:3`|
|MyteHex|relics:MYTE_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|NatureIsHealingEnemyHex|relics:natureIsHealingRune.enemyDescription|ResetRunScopedState, ApplyCombatStartToEnemy, BeforeEnemySideTurnStart, AfterCombatEnd|{}|详见 `src/EnemyHexes/NatureIsHealingEnemyHex.cs:6`|
|NatureIsHealingRune|relics:NATURE_IS_HEALING_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, AfterPlayerTurnStart|{'IntervalSeconds': '10', 'Heal': '1'}|// 都以 HextechCatalog.IsAvailableForPlayer 为闸门。IsNetworkMultiplayer() 两端一致,池子确定性排除、不引入分叉。; public override bool IsAvailableForPlayer(Player player)|
|NearDeathFeastEnemyHex|relics:nearDeathFeastRune.enemyDescription||{}|详见 `src/EnemyHexes/NearDeathFeastEnemyHex.cs:9`|
|NearDeathFeastRune|relics:NEAR_DEATH_FEAST_RUNE.description|IsAvailableForPlayer, AfterObtained, AfterRoomEntered, BeforeCombatStart, AfterCurrentHpChanged, AfterCombatVictory, AfterCombatEnd, ModifyBlockMultiplicative|{'DeathNegativeMaxHpPercent': '50', 'StrengthPerNegativeHp': '1'}|public override bool IsAvailableForPlayer(Player player)|
|NecrobinderForge|relics:NECROBINDER_FORGE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'Summon': '6'}|public override bool IsAvailableForPlayer(Player player)|
|NeowsGrudgeRune|relics:NEOWS_GRUDGE_RUNE.description|BeforeHandDraw|{'Cards': '1'}|详见 `src/Runes/NeowsGrudgeRune.cs:3`|
|NetherSoulRune|relics:NETHER_SOUL_RUNE.description|IsAvailableForPlayer, AfterSideTurnEndLate|{}|public override bool IsAvailableForPlayer(Player player) => IsNecrobinderPlayer(player);|
|NeurosurgeUpgradeRune|relics:NEUROSURGE_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsNecrobinderPlayer(player);|
|NeutralizeUpgradeRune|relics:NEUTRALIZE_UPGRADE_RUNE.description|MeetsCardAvailabilityRequirement, IsAvailableForCharacter, AfterCardDiscarded|{'Repeats': '2'}|protected override bool IsAvailableForCharacter(Player player)|
|NightmareRune|relics:NIGHTMARE_RUNE.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => IsDefectPlayer(player);|
|NightmareUpgradeRune|relics:NIGHTMARE_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsSilentPlayer(player);|
|NightstalkingEnemyHex|relics:nightstalkingRune.enemyDescription|AfterCardDrawn, AfterCardPlayedLate, AfterPlayerTurnStartLate, BeforeTurnEnd|{}|详见 `src/EnemyHexes/NightstalkingEnemyHex.cs:3`|
|NightstalkingRune|relics:NIGHTSTALKING_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterCardDrawn, AfterCardPlayedLate, AfterPlayerTurnStartLate|{'CardsNeeded': '15', 'IntangiblePower': '1'}|详见 `src/Runes/NightstalkingRune.cs:3`|
|NimbleRune|relics:NIMBLE_RUNE.description|ModifyHandDraw|{'Cards': '1'}|详见 `src/Runes/NimbleRune.cs:3`|
|NineDragonPowerRune|relics:NINE_DRAGON_POWER_RUNE.description|AfterRoomEntered, BeforeCombatStart, AfterPotionUsed|{'RegenPower': '1', 'StackBonusPercent': '3'}|详见 `src/Runes/NineDragonPowerRune.cs:3`|
|NonupeipeGenerosityRune|relics:NONUPEIPE_GENEROSITY_RUNE.description|AfterObtained|{}|详见 `src/Runes/NonupeipeGenerosityRune.cs:5`|
|NowYouSeeMeRune|relics:NOW_YOU_SEE_ME_RUNE.description|IsAvailableForPlayer, AfterCardDiscarded|{}|public override bool IsAvailableForPlayer(Player player)|
|OblivionUpgradeRune|relics:OBLIVION_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsNecrobinderPlayer(player);|
|OceanDragonSoulCard|cards:OCEAN_DRAGON_SOUL_CARD.description|OnPlay|{'Heal': '3'}|详见 `src/Cards/HextechDragonSoulCards.cs:16`|
|OceanDragonSoulRune|relics:OCEAN_DRAGON_SOUL_RUNE.description||{}|详见 `src/Runes/OceanDragonSoulRune.cs:3`|
|OkBoomerangCard|cards:OK_BOOMERANG_CARD.description|OnPlay, GetResultLocationForCardPlay, GetResultPileTypeAndPositionForCardPlay, GetResultPileTypeForCardPlay, OnUpgrade|{'Damage': '6', 'Hits': '2'}|详见 `src/Cards/OkBoomerangCard.cs:3`|
|OkBoomerangRune|relics:OK_BOOMERANG_RUNE.description|AfterObtained|{'Cards': '1'}|详见 `src/Runes/OkBoomerangRune.cs:3`|
|OmegaEnemyHex|relics:omegaRune.enemyDescription|BeforePlayerSideTurnStart|{}|详见 `src/EnemyHexes/OmegaEnemyHex.cs:3`|
|OmegaRune|relics:OMEGA_RUNE.description|BeforeTurnEnd|{'StartTurn': '4', 'Damage': '50'}|详见 `src/Runes/OmegaRune.cs:3`|
|OminousPactEnemyHex|relics:ominousPactRune.enemyDescription|AfterEnemyDamageGivenImmediate|{}|详见 `src/EnemyHexes/OminousPactEnemyHex.cs:3`|
|OminousPactRune|relics:OMINOUS_PACT_RUNE.description|IsAvailableForPlayer, AfterPowerAmountChanged|{}|public override bool IsAvailableForPlayer(Player player)|
|OmniDragonSoulEnemyHex|relics:omniDragonSoulRune.enemyDescription|BeforePlayerSideTurnStart|{}|详见 `src/EnemyHexes/OmniDragonSoulEnemyHex.cs:3`|
|OmniDragonSoulRune|relics:OMNI_DRAGON_SOUL_RUNE.description|BeforeCombatStart|{'Cards': '3'}|详见 `src/Runes/OmniDragonSoulRune.cs:3`|
|OrbSlotForge|relics:ORB_SLOT_FORGE.description|IsAvailableForPlayer, AfterSideTurnStart|{'OrbSlots': '2'}|public override bool IsAvailableForPlayer(Player player)|
|OrbSymbiosisRune|relics:ORB_SYMBIOSIS_RUNE.description|IsAvailableForPlayer, AfterOrbChanneled|{'OrbCount': '1'}|public override bool IsAvailableForPlayer(Player player)|
|OrobasBlessingRune|relics:OROBAS_BLESSING_RUNE.description|AfterObtained|{}|详见 `src/Runes/OrobasBlessingRune.cs:5`|
|OstyWishCard|cards:OSTY_WISH_CARD.description|AddExtraArgsToDescription, OnPlay, OnUpgrade|{'Block': '0', 'Damage': '0'}|详见 `src/Cards/OstyWishCard.cs:5`|
|OurHealingRune|relics:OUR_HEALING_RUNE.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player)|
|OverflowRune|relics:OVERFLOW_RUNE.description|ModifyMaxEnergy, TryModifyEnergyCostInCombat, ModifyBlockMultiplicative, ModifyDamageMultiplicativeCompat|{'Energy': '1'}|详见 `src/Runes/OverflowRune.cs:3`|
|OverlordBloodArmorRune|relics:OVERLORD_BLOOD_ARMOR_RUNE.description|BeforeCombatStart|{'MaxHpPerStrength': '40', 'StrengthPower': '1'}|详见 `src/Runes/OverlordBloodArmorRune.cs:3`|
|PacifistRune|relics:PACIFIST_RUNE.description|BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, ModifyBlockMultiplicative, ModifyDamageMultiplicativeCompat, AfterDamageGiven|{'SustainMultiplier': '1.5', 'DoomPower': '1'}|详见 `src/Runes/PacifistRune.cs:3`|
|PactsEndUpgradeRune|relics:PACTS_END_UPGRADE_RUNE.description|IsAvailableForCharacter, ModifyDamageAdditiveCompat|{'DamagePerExhaust': '6'}|protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);|
|PandorasBoxEnemyHex|relics:pandorasBoxRune.enemyDescription|ModifyCardRewardCreationOptions|{}|详见 `src/EnemyHexes/PandorasBoxEnemyHex.cs:3`|
|PandorasBoxRune|relics:PANDORAS_BOX_RUNE.description|AfterObtained|{}|详见 `src/Runes/PandorasBoxRune.cs:3`|
|ParticleWallUpgradeRune|relics:PARTICLE_WALL_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player) => IsRegentPlayer(player);|
|PhantasmalGardenerEnemyHex|relics:phantasmalGardenerHex.enemyDescription|ApplyCombatStartToEnemy|{}|详见 `src/EnemyHexes/PhantasmalGardenerEnemyHex.cs:3`|
|PhantasmalGardenerHex|relics:PHANTASMAL_GARDENER_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|PhrogParasiteEnemyHex|relics:phrogParasiteHex.enemyDescription|AfterEnemyDamageGivenImmediate|{}|详见 `src/EnemyHexes/PhrogParasiteEnemyHex.cs:3`|
|PhrogParasiteHex|relics:PHROG_PARASITE_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|PiercingThreadRune|relics:PIERCING_THREAD_RUNE.description|BeforeCombatStart, AfterCombatEnd, BeforeDamageReceived|{'PiercingPercent': '50'}|详见 `src/Runes/PiercingThreadRune.cs:3`|
|PiggyBankRune|relics:PIGGY_BANK_RUNE.description|AfterObtained, BeforeCombatStart, AfterDamageReceived, AfterCombatVictory|{'Gold': '200', 'CounterGain': '20'}|详见 `src/Runes/PiggyBankRune.cs:3`|
|PlasterRune|relics:PLASTER_RUNE.description|IsAvailableForPlayer, AfterSummon|{'Summon': '1'}|public override bool IsAvailableForPlayer(Player player)|
|PlateletRune|relics:PLATELET_RUNE.description|IsAvailableForPlayer, AfterCurrentHpChanged|{'Block': '3'}|public override bool IsAvailableForPlayer(Player player)|
|PlatingForge|relics:PLATING_FORGE.description|BeforeCombatStart|{'PlatingPower': '6'}|详见 `src/Forges/HextechForges.Gold.cs:440`|
|PocketForge|relics:POCKET_FORGE.description|AfterObtained, BeforeCombatStart|{'PotionSlots': '2'}|详见 `src/Forges/HextechForges.Silver.cs:233`|
|PorcupineEnemyHex|relics:porcupineRune.enemyDescription|AfterEnemyDamageReceived, BeforeTurnEnd, BeforeSideTurnStart|{}|详见 `src/EnemyHexes/PorcupineEnemyHex.cs:3`|
|PorcupineRune|relics:PORCUPINE_RUNE.description|BeforeCombatStart, AfterCombatEnd, BeforeTurnEnd, BeforeSideTurnStart|{'BlockPerThorn': '5'}|详见 `src/Runes/PorcupineRune.cs:3`|
|PortableSleepingBagRune|relics:PORTABLE_SLEEPING_BAG_RUNE.description|AfterObtained|{}|详见 `src/Runes/PortableSleepingBagRune.cs:5`|
|PowerShieldRune|relics:POWER_SHIELD_RUNE.description|BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, AfterBlockGained|{}|if (!TryConsumeTurnProc(nameof(PowerShieldRune), ref _triggeredThisTurn))|
|PrecisionCognitionRune|relics:PRECISION_COGNITION_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'FocusPower': '2'}|public override bool IsAvailableForPlayer(Player player)|
|PreparedForge|relics:PREPARED_FORGE.description|ModifyHandDraw|{'Cards': '2'}|详见 `src/Forges/HextechForges.Silver.cs:284`|
|PrimitiveMadnessRune|relics:PRIMITIVE_MADNESS_RUNE.description|AfterObtained|{}|详见 `src/Runes/PrimitiveMadnessRune.cs:6`|
|PrismaticArtifactForge|relics:PRISMATIC_ARTIFACT_FORGE.description|BeforeCombatStart|{'ArtifactPower': '2'}|详见 `src/Forges/HextechForges.Prismatic.cs:190`|
|PrismaticEggRune|relics:PRISMATIC_EGG_RUNE.description||{}|详见 `src/Runes/PrismaticEggRune.cs:11`|
|PrismaticLifeForge|relics:PRISMATIC_LIFE_FORGE.description|AfterObtained|{'MaxHpPercent': '30'}|详见 `src/Forges/HextechForges.Prismatic.cs:3`|
|ProtectionForge|relics:PROTECTION_FORGE.description|ModifyBlockMultiplicative|{'SustainMultiplier': '1.2'}|详见 `src/Forges/HextechForges.Prismatic.cs:58`|
|ProtectiveVeilEnemyHex|relics:protectiveVeilRune.enemyDescription|ApplyOpeningCombatStartToEnemy|{}|详见 `src/EnemyHexes/ProtectiveVeilEnemyHex.cs:3`|
|ProtectiveVeilRune|relics:PROTECTIVE_VEIL_RUNE.description|AfterRoomEntered, AfterCombatEnd, BeforeSideTurnStart|{'ArtifactPower': '1'}|详见 `src/Runes/ProtectiveVeilRune.cs:3`|
|ProteinShakeEnemyHex|relics:proteinShakeRune.enemyDescription|ModifyBlockMultiplicative, ModifyEnemyHealMultiplicative|{}|详见 `src/EnemyHexes/ProteinShakeEnemyHex.cs:3`|
|ProteinShakeRune|relics:PROTEIN_SHAKE_RUNE.description|ModifyBlockMultiplicative|{'MaxHpPerStep': '2', 'SustainPercentPerStep': '1'}|详见 `src/Runes/ProteinShakeRune.cs:3`|
|QuantumComputingRune|relics:QUANTUM_COMPUTING_RUNE.description|BeforeCombatStart, AfterPlayerTurnStart|{'DamagePercent': '10', 'Damage': '10', 'HealPercent': '10'}|详见 `src/Runes/QuantumComputingRune.cs:3`|
|QueenEnemyHex|relics:queenHex.enemyDescription|ApplyCombatStartPlayerDebuffs, BeforeEnemySideTurnStart|{}|if (!context.TryConsumeRoundInterval(Kind, combatState, everyNRounds: 1))|
|QueenHex|relics:QUEEN_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|QueenRune|relics:QUEEN_RUNE.description|BeforeSideTurnStart|{'FrailPower': '2', 'WeakPower': '2', 'VulnerablePower': '2'}|详见 `src/Runes/QueenRune.cs:3`|
|RageUpgradeRune|relics:RAGE_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);|
|RallyingCallRune|relics:RALLYING_CALL_RUNE.description|AfterCardPlayed|{}|详见 `src/Runes/RallyingCallRune.cs:3`|
|RandomForgeShopRelic|relics:RANDOM_FORGE_SHOP_RELIC.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player)|
|ReanimateUpgradeRune|relics:REANIMATE_UPGRADE_RUNE.description|IsAvailableForCharacter, BeforeCombatStart, AfterCombatEnd, AfterDeath, TryModifyEnergyCostInCombat|{}|protected override bool IsAvailableForCharacter(Player player)|
|ReapUpgradeRune|relics:REAP_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|ReaperFormUpgradeRune|relics:REAPER_FORM_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsNecrobinderPlayer(player);|
|RebootUpgradeRune|relics:REBOOT_UPGRADE_RUNE.description|IsAvailableForCharacter, TryModifyKeywordsInCombat|{}|protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);|
|RecoveryForge|relics:RECOVERY_FORGE.description|AfterPlayerTurnStartEarly|{'Heal': '1'}|详见 `src/Forges/HextechForges.Gold.cs:195`|
|RecycleBinRune|relics:RECYCLE_BIN_RUNE.description|IsAvailableForPlayer, ModifyShuffleOrder|{}|public override bool IsAvailableForPlayer(Player player)|
|RedEnvelopeRune|relics:RED_ENVELOPE_RUNE.description|AfterCombatVictory|{}|internal const int BaseForgeChance = 25;; internal const int ForgeChanceStep = 5;|
|ReflectUpgradeRune|relics:REFLECT_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsRegentPlayer(player);|
|ReforgedHelmetEnemyHex|relics:reforgedHelmetRune.enemyDescription|ModifyPowerAmountReceived|{}|详见 `src/EnemyHexes/ReforgedHelmetEnemyHex.cs:3`|
|ReforgedHelmetRune|relics:REFORGED_HELMET_RUNE.description|IsAvailableForPlayer, TryModifyPowerAmountReceived, AfterModifyingPowerAmountReceived|{}|public override bool IsAvailableForPlayer(Player player)|
|RegenForge|relics:REGEN_FORGE.description|BeforeCombatStart|{'RegenPower': '4'}|详见 `src/Forges/HextechForges.Prismatic.cs:118`|
|RegenerationSuppressionRune|relics:REGENERATION_SUPPRESSION_RUNE.description||{}|详见 `src/Runes/RegenerationSuppressionRune.cs:3`|
|RekindleRune|relics:REKINDLE_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, AfterCardExhausted|{'Cards': '2', 'Energy': '1'}|public override bool IsAvailableForPlayer(Player player)|
|RenewalRune|relics:RENEWAL_RUNE.description|IsAvailableForPlayer, AfterCardDiscarded|{'Cards': '1'}|public override bool IsAvailableForPlayer(Player player)|
|ReprogramCard|cards:REPROGRAM_CARD.description|OnPlay, OnUpgrade|{'FocusPower': '1', 'StrengthPower': '1', 'DexterityPower': '1'}|详见 `src/Cards/ReprogramCard.cs:3`|
|ReprogramRune|relics:REPROGRAM_RUNE.description|IsAvailableForPlayer, AfterObtained|{}|public override bool IsAvailableForPlayer(Player player)|
|RepulsorEnemyHex|relics:repulsorRune.enemyDescription|AfterEnemyHealthThreshold|{}|详见 `src/EnemyHexes/RepulsorEnemyHex.cs:3`|
|RepulsorRune|relics:REPULSOR_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterDamageReceived, AfterPlayerTurnStart|{'SlipperyPower': '2'}|详见 `src/Runes/RepulsorRune.cs:5`|
|RitualForge|relics:RITUAL_FORGE.description|BeforeCombatStart|{'RitualPower': '1'}|详见 `src/Forges/HextechForges.Prismatic.cs:94`|
|RoyalCommandRune|relics:ROYAL_COMMAND_RUNE.description|IsAvailableForPlayer, AfterStarsGained, AfterStarsSpent|{'ForgeAmount': '3'}|public override bool IsAvailableForPlayer(Player player)|
|RoyalTrialRune|relics:ROYAL_TRIAL_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, AfterCardPlayed|{'Cards': '2'}|public override bool IsAvailableForPlayer(Player player); \|\| !cardPlay.IsFirstInSeries; \|\| cardPlay.IsAutoPlay|
|RoyaltiesUpgradeRune|relics:ROYALTIES_UPGRADE_RUNE.description|IsAvailableForCharacter, BeforeCombatStart, AfterPlayerTurnStart, AfterCombatEnd|{}|protected override bool IsAvailableForCharacter(Player player) => IsRegentPlayer(player);|
|SacrificeRune|relics:SACRIFICE_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterPlayerTurnStart, ModifyBlockMultiplicative|{'CountPerEnemy': '5', 'SustainMultiplier': '1.1'}|详见 `src/Runes/SacrificeRune.cs:3`|
|ScapegoatRune|relics:SCAPEGOAT_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterPlayerTurnStart|{}|详见 `src/Runes/ScapegoatRune.cs:3`|
|ScaredStiffRune|relics:SCARED_STIFF_RUNE.description|IsAvailableForPlayer, BeforeTurnEnd|{}|public override bool IsAvailableForPlayer(Player player)|
|SearingAttackCard|cards:SEARING_ATTACK_CARD.description|OnPlay, OnUpgrade|{'Damage': '12'}|详见 `src/Cards/SearingAttackCard.cs:3`|
|SearingAttackRune|relics:SEARING_ATTACK_RUNE.description|AfterObtained|{'Cards': '1'}|详见 `src/Runes/SearingAttackRune.cs:5`|
|SellOffRune|relics:SELL_OFF_RUNE.description|IsAvailableForPlayer, AfterCardDiscarded|{}|public override bool IsAvailableForPlayer(Player player)|
|SendThemInRune|relics:SEND_THEM_IN_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, BeforeHandDraw|{'Cards': '1'}|public override bool IsAvailableForPlayer(Player player)|
|SerpentFormUpgradeRune|relics:SERPENT_FORM_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsSilentPlayer(player);|
|SerpentsFangEnemyHex|relics:serpentsFangRune.enemyDescription|AfterEnemyDamageGivenImmediate|{}|详见 `src/EnemyHexes/SerpentsFangEnemyHex.cs:3`|
|SerpentsFangRune|relics:SERPENTS_FANG_RUNE.description|IsAvailableForPlayer, AfterDamageGiven|{'PoisonPerHit': '4'}|public override bool IsAvailableForPlayer(Player player)|
|ServantMasterEnemyHex|relics:servantMasterRune.enemyDescription|ApplyPersistentToEnemy, AfterPowerAmountChanged|{}|详见 `src/EnemyHexes/ServantMasterEnemyHex.cs:3`|
|ServantMasterRune|relics:SERVANT_MASTER_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterPlayerTurnStart|{'NecroMasteryPower': '1', 'Summon': '3'}|public override bool IsAvailableForPlayer(Player player)|
|ShoulderVakuEnemyHex|relics:shoulderVakuRune.enemyDescription|AfterAutoPrePlayPhaseEnteredLate|{}|详见 `src/EnemyHexes/ShoulderVakuEnemyHex.cs:3`|
|ShoulderVakuRune|relics:SHOULDER_VAKU_RUNE.description|BeforeCombatStart, AfterCombatEnd, ModifyHandDraw, ModifyMaxEnergy, AfterPlayerTurnStart, AfterAutoPrePlayPhaseEnteredLate|{'Energy': '2', 'Cards': '2', 'HealPercent': '5'}|详见 `src/Runes/ShoulderVakuRune.cs:5`|
|ShriekUpgradeRune|relics:SHRIEK_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{'StrengthPower': '2'}|protected override bool IsAvailableForCharacter(Player player)|
|ShrinkEngineEnemyHex|relics:shrinkEngineRune.enemyDescription|BeforePlayerSideTurnStart, BeforeEnemySideTurnStart|{}|详见 `src/EnemyHexes/ShrinkEngineEnemyHex.cs:3`|
|ShrinkEngineRune|relics:SHRINK_ENGINE_RUNE.description|AfterObtained, AfterRoomEntered, AfterCombatVictory, ModifyHandDraw, ModifyMaxEnergy|{}|详见 `src/Runes/ShrinkEngineRune.cs:3`|
|ShrinkForge|relics:SHRINK_FORGE.description|BeforeCombatStart|{'ShrinkPower': '2'}|详见 `src/Forges/HextechForges.Gold.cs:408`|
|ShrinkRayEnemyHex|relics:shrinkRayRune.enemyDescription|AfterEnemyDamageGivenImmediate|{}|详见 `src/EnemyHexes/ShrinkRayEnemyHex.cs:3`|
|ShrinkRayRune|relics:SHRINK_RAY_RUNE.description|AfterDamageGiven|{'ShrinkPower': '1'}|详见 `src/Runes/ShrinkRayRune.cs:3`|
|ShrinkerBeetleEnemyHex|relics:shrinkerBeetleHex.enemyDescription|ApplyCombatStartPlayerDebuffs|{}|详见 `src/EnemyHexes/ShrinkerBeetleEnemyHex.cs:3`|
|ShrinkerBeetleHex|relics:SHRINKER_BEETLE_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|SilverAttackForge|relics:SILVER_ATTACK_FORGE.description|ModifyDamageMultiplicativeCompat|{'DamageMultiplier': '1.05'}|详见 `src/Forges/HextechForges.Silver.cs:199`|
|SilverHpForge|relics:SILVER_HP_FORGE.description|AfterObtained|{'MaxHpPercent': '7.5'}|详见 `src/Forges/HextechForges.Silver.cs:161`|
|SilverOrbForge|relics:SILVER_ORB_FORGE.description|IsAvailableForPlayer, AfterSideTurnStart|{'OrbCount': '2'}|public override bool IsAvailableForPlayer(Player player)|
|SilverPlatingForge|relics:SILVER_PLATING_FORGE.description|BeforeCombatStart|{'PlatingPower': '4'}|详见 `src/Forges/HextechForges.Silver.cs:41`|
|SilverProtectionForge|relics:SILVER_PROTECTION_FORGE.description|ModifyBlockMultiplicative|{'SustainMultiplier': '1.05'}|详见 `src/Forges/HextechForges.Silver.cs:216`|
|SilverStarsForge|relics:SILVER_STARS_FORGE.description|IsAvailableForPlayer, AfterSideTurnStart|{'Stars': '2'}|public override bool IsAvailableForPlayer(Player player)|
|SingularityAIEnemyHex|relics:singularityAiRune.enemyDescription|BeforePlayerSideTurnStart|{}|详见 `src/EnemyHexes/SingularityAIEnemyHex.cs:3`|
|SingularityAIRune|relics:SINGULARITY_AI_RUNE.description|BeforeHandDraw|{'Cards': '1'}|详见 `src/Runes/SingularityAIRune.cs:3`|
|SkulkingColonyEnemyHex|relics:skulkingColonyHex.enemyDescription|ApplyOpeningCombatStartToEnemy|{}|详见 `src/EnemyHexes/SkulkingColonyEnemyHex.cs:3`|
|SkulkingColonyHex|relics:SKULKING_COLONY_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|SkyDrillUpgradeRune|relics:SKY_DRILL_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{'XThreshold': '6', 'XMultiplier': '3'}|protected override bool IsAvailableForCharacter(Player player)|
|SlapEnemyHex|relics:slapRune.enemyDescription|AfterEnemyDebuffReceived|{}|if (HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.SlapProcsThisTurn, target, 3))|
|SlapRune|relics:SLAP_RUNE.description|OnEnemyDebuffApplied|{'StrengthPower': '1'}|详见 `src/Runes/SlapRune.cs:3`|
|SlimedBerserkerEnemyHex|relics:slimedBerserkerHex.enemyDescription|BeforePlayerSideTurnStart|{}|详见 `src/EnemyHexes/SlimedBerserkerEnemyHex.cs:3`|
|SlimedBerserkerHex|relics:SLIMED_BERSERKER_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|SlipperyForge|relics:SLIPPERY_FORGE.description|BeforeCombatStart|{'SlipperyPower': '2'}|详见 `src/Forges/HextechForges.Prismatic.cs:166`|
|SlowCookRune|relics:SLOW_COOK_RUNE.description|AfterPlayerTurnStart|{'BurnPercent': '5'}|详见 `src/Runes/SlowCookRune.cs:3`|
|SmokestackUpgradeRune|relics:SMOKESTACK_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardDrawn|{}|protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);|
|SnakebiteRune|relics:SNAKEBITE_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, BeforeHandDraw|{'Cards': '1'}|public override bool IsAvailableForPlayer(Player player)|
|SnakebiteUpgradeRune|relics:SNAKEBITE_UPGRADE_RUNE.description|IsAvailableForCharacter, BeforeTurnEnd|{'Replays': '1'}|protected override bool IsAvailableForCharacter(Player player)|
|SolidTimeEnemyHex|relics:solidTimeRune.enemyDescription|AfterCardPlayed|{}|详见 `src/EnemyHexes/SolidTimeEnemyHex.cs:3`|
|SolidTimeRune|relics:SOLID_TIME_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterPlayerTurnStartLate, AfterCardPlayed|{}|IsAutoPlay = true,|
|SomethingForNothingEnemyHex|relics:somethingForNothingRune.enemyDescription|ModifyCardPlayResultPileTypeAndPosition|{}|详见 `src/EnemyHexes/SomethingForNothingEnemyHex.cs:3`|
|SomethingForNothingRune|relics:SOMETHING_FOR_NOTHING_RUNE.description|BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, AfterCardPlayed|{'Cards': '1', 'Energy': '1'}|\|\| !TryConsumeTurnProc(nameof(SomethingForNothingRune), ref _discountTriggeredThisTurn))|
|SomethingFromNothingRune|relics:SOMETHING_FROM_NOTHING_RUNE.description|IsAvailableForPlayer, AfterCardPlayed|{'Cards': '1'}|public override bool IsAvailableForPlayer(Player player)|
|SonataEnemyHex|relics:sonataRune.enemyDescription|BeforePlayerSideTurnStart, BeforeEnemySideTurnStart|{}|详见 `src/EnemyHexes/SonataEnemyHex.cs:3`|
|SonataRune|relics:SONATA_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterPlayerTurnStartEarly|{'Cards': '2', 'Heal': '1', 'Block': '2'}|public override bool IsAvailableForPlayer(Player player)|
|SoulCallingRune|relics:SOUL_CALLING_RUNE.description|BeforeCombatStart, AfterCombatEnd, BeforeHandDraw|{'Cards': '1'}|详见 `src/Runes/SoulCallingRune.cs:3`|
|SoulEaterEnemyHex|relics:soulEaterRune.enemyDescription|AfterDeath|{}|详见 `src/EnemyHexes/SoulEaterEnemyHex.cs:3`|
|SoulEaterRune|relics:SOUL_EATER_RUNE.description|AfterDeath|{'MaxHpGainPercent': '0.05'}|详见 `src/Runes/SoulEaterRune.cs:3`|
|SoulFyshEnemyHex|relics:soulFyshHex.enemyDescription|AfterShuffle|{}|详见 `src/EnemyHexes/SoulFyshEnemyHex.cs:3`|
|SoulFyshHex|relics:SOUL_FYSH_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|SoulUpgradeRune|relics:SOUL_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{'Energy': '1'}|protected override bool IsAvailableForCharacter(Player player)|
|SoulsPowerForge|relics:SOULS_POWER_FORGE.description||{}|详见 `src/Forges/HextechEnchantForges.cs:55`|
|SowUpgradeRune|relics:SOW_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|SpeedDemonEnemyHex|relics:speedDemonRune.enemyDescription|BeforePlayerSideTurnStart, AfterEnemyDamageGivenPlayerHit|{}|详见 `src/EnemyHexes/SpeedDemonEnemyHex.cs:3`|
|SpeedDemonRune|relics:SPEED_DEMON_RUNE.description|BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, AfterDamageGiven|{}|if (!TryConsumeTurnProc(nameof(SpeedDemonRune), ref _triggeredThisTurn))|
|SpeedsterRune|relics:SPEEDSTER_RUNE.description|ModifyHandDraw|{}|详见 `src/Runes/SpeedsterRune.cs:3`|
|SpinToWinRune|relics:SPIN_TO_WIN_RUNE.description|AfterPowerAmountChanged|{}|详见 `src/Runes/SpinToWinRune.cs:3`|
|SpiralForge|relics:SPIRAL_FORGE.description||{}|详见 `src/Forges/HextechEnchantForges.cs:68`|
|StardustUpgradeRune|relics:STARDUST_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|StarlightSplendorRune|relics:STARLIGHT_SPLENDOR_RUNE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'Stars': '2'}|public override bool IsAvailableForPlayer(Player player)|
|StarsForge|relics:STARS_FORGE.description|IsAvailableForPlayer, AfterPlayerTurnStartEarly|{'Stars': '1'}|public override bool IsAvailableForPlayer(Player player)|
|StartupRoutineEnemyHex|relics:startupRoutineRune.enemyDescription|ApplyCombatStartToEnemy|{}|详见 `src/EnemyHexes/StartupRoutineEnemyHex.cs:3`|
|StartupRoutineRune|relics:STARTUP_ROUTINE_RUNE.description|BeforeCombatStart|{'Block': '16'}|详见 `src/Runes/StartupRoutineRune.cs:3`|
|StatsEnemyHex|relics:statsRune.enemyDescription|ModifyDamageMultiplicative, ModifyBlockMultiplicative, ModifyEnemyHealMultiplicative, ApplyPersistentToEnemy|{}|详见 `src/EnemyHexes/StatsEnemyHex.cs:3`|
|StatsOnStatsEnemyHex|relics:statsOnStatsRune.enemyDescription|ModifyDamageMultiplicative, ModifyBlockMultiplicative, ModifyEnemyHealMultiplicative, ApplyPersistentToEnemy|{}|详见 `src/EnemyHexes/StatsOnStatsEnemyHex.cs:3`|
|StatsOnStatsOnStatsEnemyHex|relics:statsOnStatsOnStatsRune.enemyDescription|ModifyDamageMultiplicative, ModifyBlockMultiplicative, ModifyEnemyHealMultiplicative, ApplyPersistentToEnemy|{}|详见 `src/EnemyHexes/StatsOnStatsOnStatsEnemyHex.cs:3`|
|StatsOnStatsOnStatsRune|relics:STATS_ON_STATS_ON_STATS_RUNE.description||{'ForgeCount': '6'}|详见 `src/Runes/StatsOnStatsOnStatsRune.cs:3`|
|StatsOnStatsRune|relics:STATS_ON_STATS_RUNE.description||{'ForgeCount': '4'}|详见 `src/Runes/StatsOnStatsRune.cs:3`|
|StatsRune|relics:STATS_RUNE.description||{'ForgeCount': '2'}|详见 `src/Runes/StatsRune.cs:3`|
|StokeRune|relics:STOKE_RUNE.description|TryModifyRestSiteOptions|{}|详见 `src/Runes/StokeRune.cs:8`|
|StormUpgradeRune|relics:STORM_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);|
|StrengthForge|relics:STRENGTH_FORGE.description|BeforeCombatStart|{'StrengthPower': '1'}|详见 `src/Forges/HextechForges.Silver.cs:3`|
|StrengthToDexterityRune|relics:STRENGTH_TO_DEXTERITY_RUNE.description|AfterRoomEntered, ShouldConvert, ShouldConvertAppliedPower, ApplyConvertedPower, RevertOriginalPower|{'DexterityPower': '1'}|详见 `src/Runes/StrengthToDexterityRune.cs:3`|
|StrikeUpgradeRune|relics:STRIKE_UPGRADE_RUNE.description|IsAvailableForPlayer, AfterCombatEnd|{}|public override bool IsAvailableForPlayer(Player player)|
|SturdyEnemyHex|relics:sturdyRune.enemyDescription|BeforeEnemySideTurnStart|{}|详见 `src/EnemyHexes/SturdyEnemyHex.cs:3`|
|SturdyRune|relics:STURDY_RUNE.description|AfterPlayerTurnStart|{'HealPercent': '2', 'LowHpThresholdPercent': '50', 'LowHpHealPercent': '5'}|详见 `src/Runes/SturdyRune.cs:3`|
|SubroutineUpgradeRune|relics:SUBROUTINE_UPGRADE_RUNE.description|IsAvailableForCharacter, BeforeCombatStart, AfterCombatEnd, BeforeHandDraw|{}|protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);; \|\| !TryConsumeCombatStartMove()); internal bool TryConsumeCombatStartMove()|
|SummonForge|relics:SUMMON_FORGE.description|IsAvailableForPlayer, AfterPlayerTurnStart|{'Summon': '2'}|public override bool IsAvailableForPlayer(Player player)|
|SummonForthRune|relics:SUMMON_FORTH_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, BeforeHandDraw|{'ForgeAmount': '5'}|public override bool IsAvailableForPlayer(Player player)|
|SuperBrainEnemyHex|relics:superBrainRune.enemyDescription|ApplyOpeningCombatStartToEnemy|{}|详见 `src/EnemyHexes/SuperBrainEnemyHex.cs:3`|
|SuperBrainRune|relics:SUPER_BRAIN_RUNE.description|AfterRoomEntered|{}|详见 `src/Runes/SuperBrainRune.cs:3`|
|SurvivorUpgradeRune|relics:SURVIVOR_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|SweepingBladeRune|relics:SWEEPING_BLADE_RUNE.description|IsAvailableForPlayer, BeforeAttack, AfterAttack, BeforeCardPlayed, AfterCardPlayed, BeforePowerAmountChanged, AfterPowerAmountChanged|{}|public override bool IsAvailableForPlayer(Player player)|
|SwiftAndSafeEnemyHex|relics:swiftAndSafeRune.enemyDescription|AfterCardDrawn, AfterCardPlayedLate, AfterPlayerTurnStartLate, BeforeTurnEnd|{}|详见 `src/EnemyHexes/SwiftAndSafeEnemyHex.cs:3`|
|SwiftAndSafeRune|relics:SWIFT_AND_SAFE_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterCardDrawn, AfterCardPlayedLate, AfterPlayerTurnStartLate|{'CardsNeeded': '10', 'ArtifactPower': '1'}|详见 `src/Runes/SwiftAndSafeRune.cs:3`|
|SwiftForge|relics:SWIFT_FORGE.description||{}|详见 `src/Forges/HextechEnchantForges.cs:50`|
|SwordFlightRune|relics:SWORD_FLIGHT_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, AfterCardPlayed|{}|public override bool IsAvailableForPlayer(Player player); \|\| !cardPlay.IsFirstInSeries; \|\| cardPlay.IsAutoPlay; if (!TryConsumeTurnProc(nameof(SwordFlightRune), ref _triggeredThisTurn))|
|SwordIntentRune|relics:SWORD_INTENT_RUNE.description|IsAvailableForPlayer, TryModifyEnergyCostInCombat, TryModifyStarCost|{}|public override bool IsAvailableForPlayer(Player player)|
|SwordsmanshipRune|relics:SWORDSMANSHIP_RUNE.description|IsAvailableForPlayer, BeforeCombatStart|{'ParryPower': '12'}|public override bool IsAvailableForPlayer(Player player)|
|SymphonyOfWarRune|relics:SYMPHONY_OF_WAR_RUNE.description|BeforeCombatStart|{'SerpentFormPower': '4', 'DemonFormPower': '1'}|详见 `src/Runes/SymphonyOfWarRune.cs:5`|
|TankEngineEnemyHex|relics:tankEngineRune.enemyDescription|BeforeEnemySideTurnStart|{}|详见 `src/EnemyHexes/TankEngineEnemyHex.cs:3`|
|TankEngineRune|relics:TANK_ENGINE_RUNE.description|AfterObtained, AfterRoomEntered, AfterCombatVictory|{'HpGainPercent': '0.06', 'ScalePercent': '6'}|详见 `src/Runes/TankEngineRune.cs:3`|
|TanksShieldEnemyHex|relics:tanksShieldRune.enemyDescription|AfterCardPlayed|{}|if (!cardPlay.IsFirstInSeries; \|\| cardPlay.IsAutoPlay|
|TanksShieldRune|relics:TANKS_SHIELD_RUNE.description|AfterCardPlayed|{'Block': '3'}|详见 `src/Runes/TanksShieldRune.cs:3`|
|TapDanceRune|relics:TAP_DANCE_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterCardPlayed|{}|详见 `src/Runes/TapDanceRune.cs:3`|
|TauntRune|relics:TAUNT_RUNE.description|IsAvailableForPlayer, AfterPowerAmountChanged|{'Cards': '1'}|public override bool IsAvailableForPlayer(Player player)|
|TerminalIllnessRune|relics:TERMINAL_ILLNESS_RUNE.description|IsAvailableForPlayer, TryModifyPowerAmountReceived, AfterModifyingPowerAmountReceived|{}|public override bool IsAvailableForPlayer(Player player) => IsSilentPlayer(player);|
|TestSubjectEnemyHex|relics:testSubjectHex.enemyDescription|ApplyCombatStartToEnemy, BeforeSideTurnStart, BeforeDeath|{}|详见 `src/EnemyHexes/TestSubjectEnemyHex.cs:3`|
|TestSubjectHex|relics:TEST_SUBJECT_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|TezcatarasMercyEnemyHex|relics:tezcatarasMercyRune.enemyDescription|AfterCombatVictory, TryModifyRewards|{}|详见 `src/EnemyHexes/TezcatarasMercyEnemyHex.cs:3`|
|TezcatarasMercyRune|relics:TEZCATARAS_MERCY_RUNE.description|AfterCombatVictory|{'Relics': '1', 'CombatInterval': '3'}|详见 `src/Runes/TezcatarasMercyRune.cs:5`|
|TheForgottenEnemyHex|relics:theForgottenHex.enemyDescription|ApplyCombatStartPlayerDebuffs|{}|详见 `src/EnemyHexes/TheForgottenEnemyHex.cs:3`|
|TheForgottenHex|relics:THE_FORGOTTEN_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|TheLostEnemyHex|relics:theLostHex.enemyDescription|ApplyCombatStartPlayerDebuffs|{}|详见 `src/EnemyHexes/TheLostEnemyHex.cs:3`|
|TheLostHex|relics:THE_LOST_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|ThievingHopperEnemyHex|relics:thievingHopperHex.enemyDescription||{}|详见 `src/EnemyHexes/ThievingHopperEnemyHex.cs:10`|
|ThievingHopperHex|relics:THIEVING_HOPPER_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|ThornmailEnemyHex|relics:thornmailRune.enemyDescription|ApplyOpeningCombatStartToEnemy|{}|详见 `src/EnemyHexes/ThornmailEnemyHex.cs:3`|
|ThornmailRune|relics:THORNMAIL_RUNE.description|AfterRoomEntered|{'MaxHpPerThorns': '20', 'ThornsPower': '1'}|详见 `src/Runes/ThornmailRune.cs:3`|
|ThornsForge|relics:THORNS_FORGE.description|BeforeCombatStart|{'ThornsPower': '4'}|详见 `src/Forges/HextechForges.Gold.cs:464`|
|ThoughtOverwriteRune|relics:THOUGHT_OVERWRITE_RUNE.description|IsAvailableForPlayer, AfterObtained, AfterCardEnteredCombat, ModifyCardPlayCount, AfterModifyingCardPlayCount|{'Replays': '1'}|public override bool IsAvailableForPlayer(Player player)|
|TormentorEnemyHex|relics:tormentorRune.enemyDescription|AfterEnemyDebuffReceived|{}|\|\| !HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.TormentorProcsThisTurn, target, 3))|
|TormentorRune|relics:TORMENTOR_RUNE.description|AfterPowerAmountChanged, OnEnemyDebuffApplied|{}|protected override int MaxProcsPerTurn => 1;|
|TranscendentEvilRune|relics:TRANSCENDENT_EVIL_RUNE.description|IsAvailableForPlayer, AfterCombatVictory, BeforeCombatStart, AfterSideTurnStart|{'StacksPerBonus': '4', 'FocusPower': '1', 'OrbSlots': '1'}|public override bool IsAvailableForPlayer(Player player)|
|TransmuteChaosRune|relics:TRANSMUTE_CHAOS_RUNE.description|AfterObtained|{}|详见 `src/Runes/TransmuteChaosRune.cs:3`|
|TransmuteGoldRune|relics:TRANSMUTE_GOLD_RUNE.description|AfterObtained|{}|详见 `src/Runes/TransmuteGoldRune.cs:3`|
|TransmutePrismaticRune|relics:TRANSMUTE_PRISMATIC_RUNE.description|AfterObtained|{}|详见 `src/Runes/TransmutePrismaticRune.cs:3`|
|TriPrismRune|relics:TRI_PRISM_RUNE.description|IsAvailableForPlayer, BeforeCombatStart, AfterCombatEnd, BeforeSideTurnStart, TryModifyEnergyCostInCombat, TryModifyStarCost, ModifyCardPlayCount, AfterModifyingCardPlayCount|{}|public override bool IsAvailableForPlayer(Player player); if (TryConsumeTurnProc(nameof(TriPrismRune), ref _triggeredThisTurn))|
|TrickLicenseRune|relics:TRICK_LICENSE_RUNE.description|IsAvailableForPlayer, TryModifyEnergyCostInCombat, TryModifyStarCost|{}|public override bool IsAvailableForPlayer(Player player)|
|TrickMagicCard|cards:TRICK_MAGIC_CARD.description|OnPlay, OnUpgrade|{'Cards': '2', 'BufferPower': '1', 'Replays': '1'}|详见 `src/Cards/HextechCustomCards.cs:3`|
|TrinityRune|relics:TRINITY_RUNE.description|IsAvailableForPlayer, AfterEnergySpent, AfterStarsSpent|{'Energy': '1', 'Stars': '1', 'ForgeAmount': '1'}|public override bool IsAvailableForPlayer(Player player)|
|TungstenRodEnemyHex|relics:tungstenRodHex.enemyDescription|ModifyHpLostAfterOsty|{}|详见 `src/EnemyHexes/TungstenRodEnemyHex.cs:3`|
|TungstenRodHex|relics:TUNGSTEN_ROD_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|TwiceThriceEnemyHex|relics:twiceThriceRune.enemyDescription|ModifyPlayerAttackEnergyCostMultiplier|{}|详见 `src/EnemyHexes/TwiceThriceEnemyHex.cs:3`|
|TwiceThriceRune|relics:TWICE_THRICE_RUNE.description|BeforeCombatStart, AfterCombatEnd, ModifyCardPlayCount, AfterCardPlayed|{}|return cardPlay.IsFirstInSeries; && !cardPlay.IsAutoPlay|
|TwilightVeilEnemyHex|relics:twilightVeilRune.enemyDescription|AfterBlockGained|{}|详见 `src/EnemyHexes/TwilightVeilEnemyHex.cs:3`|
|TwilightVeilRune|relics:TWILIGHT_VEIL_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterPlayerTurnStartEarly, AfterPowerAmountChanged|{}|详见 `src/Runes/TwilightVeilRune.cs:9`|
|TwinFlamesRune|relics:TWIN_FLAMES_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterCardPlayed|{'Missiles': '3'}|详见 `src/Runes/TwinFlamesRune.cs:5`|
|UltimateRefreshRune|relics:ULTIMATE_REFRESH_RUNE.description|ModifyCardPlayCount, AfterModifyingCardPlayCount|{}|详见 `src/Runes/UltimateRefreshRune.cs:3`|
|UltimateUnstoppableRune|relics:ULTIMATE_UNSTOPPABLE_RUNE.description|AfterCardPlayed|{'MinCost': '2', 'ArtifactPower': '2'}|详见 `src/Runes/UltimateUnstoppableRune.cs:3`|
|UndyingUpgradeRune|relics:UNDYING_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterObtained, AfterCardEnteredCombat, TryModifyCardBeingAddedToDeck|{}|protected override bool IsAvailableForCharacter(Player player)|
|UniversalScopeRune|relics:UNIVERSAL_SCOPE_RUNE.description||{}|详见 `src/Runes/UniversalScopeRune.cs:105`|
|UniversalSpiral|enchantments:UNIVERSAL_SPIRAL.description|EnchantPlayCount|{'Times': '1'}|详见 `src/Enchantments/UniversalSpiral.cs:3`|
|UnleashUpgradeRune|relics:UNLEASH_UPGRADE_RUNE.description|MeetsCardAvailabilityRequirement, IsAvailableForCharacter, AfterDamageGiven|{}|protected override bool IsAvailableForCharacter(Player player)|
|UnmovableMountainEnemyHex|relics:unmovableMountainRune.enemyDescription|ApplyOpeningCombatStartToEnemy, BeforeEnemySideTurnStart|{}|详见 `src/EnemyHexes/UnmovableMountainEnemyHex.cs:3`|
|UnmovableMountainRune|relics:UNMOVABLE_MOUNTAIN_RUNE.description|BeforeCombatStart|{'BarricadePower': '1', 'AfterimagePower': '1'}|详见 `src/Runes/UnmovableMountainRune.cs:3`|
|UnsealedThroneRune|relics:UNSEALED_THRONE_RUNE.description|IsAvailableForPlayer, AfterStarsGained, AfterStarsSpent|{'Energy': '1'}|public override bool IsAvailableForPlayer(Player player)|
|UpgradeEnemyHex|relics:upgradeRune.enemyDescription|AfterCardPlayed|{}|if (!cardPlay.IsFirstInSeries; \|\| cardPlay.IsAutoPlay|
|UpgradeForge|relics:UPGRADE_FORGE.description|AfterObtained|{'Cards': '2'}|详见 `src/Forges/HextechForges.Silver.cs:65`|
|UpgradeRune|relics:UPGRADE_RUNE.description|AfterObtained, TryModifyCardRewardOptionsLate, TryModifyCardBeingAddedToDeck|{}|详见 `src/Runes/UpgradeRune.cs:5`|
|VampireCrawlerRune|relics:VAMPIRE_CRAWLER_RUNE.description|AfterCardPlayed|{}|详见 `src/Runes/VampireCrawlerRune.cs:3`|
|VantomEnemyHex|relics:vantomHex.enemyDescription|ApplyCombatStartToEnemy|{}|详见 `src/EnemyHexes/VantomEnemyHex.cs:3`|
|VantomHex|relics:VANTOM_HEX.description|IsAvailableForPlayer|{}|public override bool IsAvailableForPlayer(Player player) => false;|
|VenerateUpgradeRune|relics:VENERATE_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player)|
|VenomForge|relics:VENOM_FORGE.description|BeforeCombatStart|{'EnvenomPower': '1'}|详见 `src/Forges/HextechForges.Gold.cs:384`|
|VenomousBladeRune|relics:VENOMOUS_BLADE_RUNE.description|IsAvailableForPlayer, ModifyDamageAdditiveCompat|{}|public override bool IsAvailableForPlayer(Player player) => IsSilentPlayer(player);|
|VigorForge|relics:VIGOR_FORGE.description|AfterPlayerTurnStartEarly|{'VigorPower': '2'}|详见 `src/Forges/HextechForges.Silver.cs:332`|
|ViolenceRune|relics:VIOLENCE_RUNE.description|BeforeHandDraw|{'Cards': '1'}|详见 `src/Runes/ViolenceRune.cs:3`|
|VitalitySurgeEnemyHex|relics:vitalitySurgeRune.enemyDescription|ModifyDamageMultiplicative, ModifyBlockMultiplicative, ModifyEnemyHealMultiplicative|{}|详见 `src/EnemyHexes/VitalitySurgeEnemyHex.cs:3`|
|VitalitySurgeRune|relics:VITALITY_SURGE_RUNE.description|ModifyHandDraw, ModifyMaxEnergy|{'HpPerCard': '50', 'HpPerEnergy': '100'}|详见 `src/Runes/VitalitySurgeRune.cs:4`|
|VoidForge|relics:VOID_FORGE.description|BeforeCombatStart|{'VoidFormPower': '1'}|详见 `src/Forges/HextechForges.Prismatic.cs:239`|
|VoidFormUpgradeRune|relics:VOID_FORM_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsRegentPlayer(player);|
|VoltaicUpgradeRune|relics:VOLTAIC_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|WarmogsSpiritEnemyHex|relics:warmogsSpiritRune.enemyDescription|AfterCardDrawn, AfterCardPlayedLate, AfterPlayerTurnStartLate, BeforeTurnEnd|{}|详见 `src/EnemyHexes/WarmogsSpiritEnemyHex.cs:3`|
|WarmogsSpiritRune|relics:WARMOGS_SPIRIT_RUNE.description|BeforeCombatStart, AfterCombatEnd, AfterCardDrawn, AfterCardPlayedLate, AfterPlayerTurnStartLate|{'CardsNeeded': '8', 'PlatingPower': '1'}|详见 `src/Runes/WarmogsSpiritRune.cs:3`|
|WatchOutGrapefruitRune|relics:WATCH_OUT_GRAPEFRUIT_RUNE.description|AfterCombatVictory|{}|详见 `src/Runes/WatchOutGrapefruitRune.cs:5`|
|WhirlwindUpgradeRune|relics:WHIRLWIND_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player)|
|WhiteHoleCard|cards:WHITE_HOLE_CARD.description|OnPlay, OnUpgrade|{'Energy': '2', 'Cards': '2'}|详见 `src/Cards/WhiteHoleCard.cs:3`|
|WhiteHoleRune|relics:WHITE_HOLE_RUNE.description|AfterObtained|{'Cards': '1'}|详见 `src/Runes/WhiteHoleRune.cs:3`|
|WizardlyThinkingRune|relics:WIZARDLY_THINKING_RUNE.description|IsAvailableForPlayer, AfterRoomEntered|{}|public override bool IsAvailableForPlayer(Player player)|
|WraithRune|relics:WRAITH_RUNE.description|IsAvailableForPlayer, BeforeHandDraw, ModifyDamageMultiplicativeCompat|{'Cards': '1', 'DamagePercentPerSoul': '3'}|public override bool IsAvailableForPlayer(Player player)|
|WroughtInWarUpgradeRune|relics:WROUGHT_IN_WAR_UPGRADE_RUNE.description|IsAvailableForCharacter|{}|protected override bool IsAvailableForCharacter(Player player) => IsRegentPlayer(player);|
|ZapUpgradeRune|relics:ZAP_UPGRADE_RUNE.description|IsAvailableForCharacter, AfterCardPlayed|{}|protected override bool IsAvailableForCharacter(Player player)|
|ZealotEnemyHex|relics:zealotRune.enemyDescription|ApplyCombatStartToEnemy|{}|详见 `src/EnemyHexes/ZealotEnemyHex.cs:3`|
|ZealotRune|relics:ZEALOT_RUNE.description|ModifyHandDraw|{'RelicsNeeded': '5', 'Cards': '1'}|详见 `src/Runes/ZealotRune.cs:3`|

## 已审文件清单

|文件|本地化/辅助类|
|---|---|
|src/Runes/AdamantRune.cs|AdamantRune|
|src/Runes/AdaptiveCapacitorRune.cs|AdaptiveCapacitorRune|
|src/Runes/AdvanceToRetreatRune.cs|AdvanceToRetreatRune|
|src/Runes/AncientWineRune.cs|AncientWineRune|
|src/Runes/AnthonyBiasRune.cs|AnthonyBiasRune|
|src/Runes/ArcanePunchRune.cs|ArcanePunchRune|
|src/Runes/ArchmageRune.cs|ArchmageRune|
|src/Runes/AstralBodyRune.cs|AstralBodyRune|
|src/Runes/AttackDefenseUnityRune.cs|AttackDefenseUnityRune|
|src/Runes/AutoPatrolRune.cs|AutoPatrolRune|
|src/Runes/AutoPlayFormsAtCombatStartRuneBase.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/AutomationUpgradeRune.cs|AutomationUpgradeRune|
|src/Runes/BackToBasicsRune.cs|BackToBasicsRune|
|src/Runes/BadTasteRune.cs|BadTasteRune|
|src/Runes/BadgeBrothersRune.cs|BadgeBrothersRune|
|src/Runes/BarbarianWayRune.cs|BarbarianWayRune|
|src/Runes/BashUpgradeRune.cs|BashUpgradeRune|
|src/Runes/BattleTranceUpgradeRune.cs|BattleTranceUpgradeRune|
|src/Runes/BeginningAndEndRune.cs|BeginningAndEndRune|
|src/Runes/BerserkRune.cs|BerserkRune|
|src/Runes/BigHammerRune.cs|BigHammerRune|
|src/Runes/BigHandsRune.cs|BigHandsRune|
|src/Runes/BigKnifeRune.cs|BigKnifeRune|
|src/Runes/BigStrengthRune.cs|BigStrengthRune|
|src/Runes/BlackCandleRune.cs|BlackCandleRune|
|src/Runes/BladeWaltzRune.cs|BladeWaltzRune|
|src/Runes/BlankCheckRune.cs|BlankCheckRune|
|src/Runes/BloodArmorRune.cs|BloodArmorRune|
|src/Runes/BloodDebtRune.cs|BloodDebtRune|
|src/Runes/BloodIdolRune.cs|BloodIdolRune|
|src/Runes/BloodPactRune.cs|BloodPactRune|
|src/Runes/BloodlettingUpgradeRune.cs|BloodlettingUpgradeRune|
|src/Runes/BlueCandleMedkitRune.cs|BlueCandleMedkitRune|
|src/Runes/BodySlamUpgradeRune.cs|BodySlamUpgradeRune|
|src/Runes/BodyguardUpgradeRune.cs|BodyguardUpgradeRune|
|src/Runes/BoneBreakUpgradeRune.cs|BoneBreakUpgradeRune|
|src/Runes/BoneGuardRune.cs|BoneGuardRune|
|src/Runes/BorrowedTimeUpgradeRune.cs|BorrowedTimeUpgradeRune|
|src/Runes/BrandUpgradeRune.cs|BrandUpgradeRune|
|src/Runes/BreadAndButterRune.cs|BreadAndButterRune|
|src/Runes/BreadAndCheeseRune.cs|BreadAndCheeseRune|
|src/Runes/BreadAndJamRune.cs|BreadAndJamRune|
|src/Runes/BreadSandwichAssemblyHelper.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/BreadSandwichRune.cs|BreadSandwichRune|
|src/Runes/BrokenGoldenCrownRune.cs|BrokenGoldenCrownRune|
|src/Runes/BrutalForceRune.cs|BrutalForceRune|
|src/Runes/BrutalityRune.cs|BrutalityRune|
|src/Runes/BulletTimeUpgradeRune.cs|BulletTimeUpgradeRune|
|src/Runes/BurningInterestRune.cs|BurningInterestRune|
|src/Runes/ByproductRune.cs|ByproductRune|
|src/Runes/CantTouchThisRune.cs|CantTouchThisRune|
|src/Runes/CardInspectionRune.cs|CardInspectionRune|
|src/Runes/CardUpgradeRuneBase.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/CarefulSelectionRune.cs|CarefulSelectionRune|
|src/Runes/CatalystRune.cs|CatalystRune|
|src/Runes/CerberusRune.cs|CerberusRune|
|src/Runes/ChainInSleeveRune.cs|ChainInSleeveRune|
|src/Runes/ChargeUpRune.cs|ChargeUpRune|
|src/Runes/CircleOfDeathRune.cs|CircleOfDeathRune|
|src/Runes/ClawUpgradeRune.cs|ClawUpgradeRune|
|src/Runes/ClownCollegeRune.cs|ClownCollegeRune|
|src/Runes/CollectorRune.cs|CollectorRune|
|src/Runes/ColorDiscoveryRune.cs|ColorDiscoveryRune|
|src/Runes/CompactUpgradeRune.cs|CompactUpgradeRune|
|src/Runes/CompensationRune.cs|CompensationRune|
|src/Runes/CondensedRadianceRune.cs|CondensedRadianceRune|
|src/Runes/CoreOverloadRune.cs|CoreOverloadRune|
|src/Runes/CorpseExplosionRune.cs|CorpseExplosionRune|
|src/Runes/CorrosionRune.cs|CorrosionRune|
|src/Runes/CorrosiveWaveUpgradeRune.cs|CorrosiveWaveUpgradeRune|
|src/Runes/CorruptedBranchRune.cs|CorruptedBranchRune|
|src/Runes/CourageOfColossusRune.cs|CourageOfColossusRune|
|src/Runes/CrashLandingUpgradeRune.cs|CrashLandingUpgradeRune|
|src/Runes/CreativeAiUpgradeRune.cs|CreativeAiUpgradeRune|
|src/Runes/CrossOrbRune.cs|CrossOrbRune|
|src/Runes/CurtainCallRune.cs|CurtainCallRune|
|src/Runes/CuttingEdgeAlchemistRune.cs|CuttingEdgeAlchemistRune|
|src/Runes/DawnbringersResolveRune.cs|DawnbringersResolveRune|
|src/Runes/DeathHarvestRune.cs|DeathHarvestRune|
|src/Runes/DeathWarrantRune.cs|DeathWarrantRune|
|src/Runes/DecayRune.cs|DecayRune|
|src/Runes/DecisionsDecisionsUpgradeRune.cs|DecisionsDecisionsUpgradeRune|
|src/Runes/DefendUpgradeRune.cs|DefendUpgradeRune|
|src/Runes/DemonFormUpgradeRune.cs|DemonFormUpgradeRune|
|src/Runes/DeviantCognitionRune.cs|DeviantCognitionRune|
|src/Runes/DevilsDanceRune.cs|DevilsDanceRune|
|src/Runes/DexterityStrengthToFocusRune.cs|DexterityStrengthToFocusRune|
|src/Runes/DexterityToStrengthRune.cs|DexterityToStrengthRune|
|src/Runes/DiceManiacRune.cs|DiceManiacRune|
|src/Runes/DieForYouRune.cs|DieForYouRune|
|src/Runes/DirgeUpgradeRune.cs|DirgeUpgradeRune|
|src/Runes/DivineInterventionRune.cs|DivineInterventionRune|
|src/Runes/DizzySpinningRune.cs|DizzySpinningRune|
|src/Runes/DonationRune.cs|DonationRune|
|src/Runes/DoomsdayRune.cs|DoomsdayRune|
|src/Runes/DoubleExistenceRune.cs|DoubleExistenceRune|
|src/Runes/DoubleVisionRune.Duplication.cs|DoubleVisionRune|
|src/Runes/DoubleVisionRune.Scopes.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/DoubleVisionRune.Transactions.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/DoubleVisionRune.Types.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/DoubleVisionRune.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/DragonSoulRuneBase.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/DrainRune.cs|DrainRune|
|src/Runes/DrawYourSwordRune.cs|DrawYourSwordRune|
|src/Runes/DualWieldRune.cs|DualWieldRune|
|src/Runes/DualcastUpgradeRune.cs|DualcastUpgradeRune|
|src/Runes/DuffsVintageRune.cs|DuffsVintageRune|
|src/Runes/EarthAwakensRune.cs|EarthAwakensRune|
|src/Runes/EasyDoesItRune.cs|EasyDoesItRune|
|src/Runes/EchoFormUpgradeRune.cs|EchoFormUpgradeRune|
|src/Runes/EchoRune.cs|EchoRune|
|src/Runes/EightPennyGateRune.cs|EightPennyGateRune|
|src/Runes/ElectricSurgeRune.cs|ElectricSurgeRune|
|src/Runes/ElectrodynamicsRune.cs|ElectrodynamicsRune|
|src/Runes/EmergenceRune.cs|EmergenceRune|
|src/Runes/EndlessRecoveryRune.cs|EndlessRecoveryRune|
|src/Runes/EndlessRotationRune.cs|EndlessRotationRune|
|src/Runes/EnlightenmentRune.cs|EnlightenmentRune|
|src/Runes/EscapePlanRune.cs|EscapePlanRune|
|src/Runes/EternalArmorUpgradeRune.cs|EternalArmorUpgradeRune|
|src/Runes/EurekaRune.cs|EurekaRune|
|src/Runes/ExplosionArtRune.cs|ExplosionArtRune|
|src/Runes/ExposeUpgradeRune.cs|ExposeUpgradeRune|
|src/Runes/ExtremeSpeedRune.cs|ExtremeSpeedRune|
|src/Runes/FallingStarUpgradeRune.cs|FallingStarUpgradeRune|
|src/Runes/FanTheHammerRune.cs|FanTheHammerRune|
|src/Runes/FeedUpgradeRune.cs|FeedUpgradeRune|
|src/Runes/FeelTheBurnRune.cs|FeelTheBurnRune|
|src/Runes/FeyMagicRune.cs|FeyMagicRune|
|src/Runes/FinalFormRune.cs|FinalFormRune|
|src/Runes/FirebrandRune.cs|FirebrandRune|
|src/Runes/FirstAidKitRune.cs|FirstAidKitRune|
|src/Runes/FirstTypedCardReplayRuneBase.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/FlakCannonUpgradeRune.cs|FlakCannonUpgradeRune|
|src/Runes/FlameBarrierUpgradeRune.cs|FlameBarrierUpgradeRune|
|src/Runes/FlawlessRune.cs|FlawlessRune|
|src/Runes/FleshAndBoneRune.cs|FleshAndBoneRune|
|src/Runes/FlyingKickCorpseLaunchDriver.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/FlyingKickRune.cs|FlyingKickRune|
|src/Runes/ForbiddenGrimoireRune.cs|ForbiddenGrimoireRune|
|src/Runes/ForgottenSoulRune.cs|ForgottenSoulRune|
|src/Runes/FrostWraithRune.cs|FrostWraithRune|
|src/Runes/FuriousGlareRune.cs|FuriousGlareRune|
|src/Runes/GalacticGiftRune.cs|GalacticGiftRune|
|src/Runes/GetExcitedRune.cs|GetExcitedRune|
|src/Runes/GhostFormRune.cs|GhostFormRune|
|src/Runes/GiantSerpentsFangRune.cs|GiantSerpentsFangRune|
|src/Runes/GiantSlayerRune.cs|GiantSlayerRune|
|src/Runes/GlassCannonRune.cs|GlassCannonRune|
|src/Runes/GloomyCloudsRune.cs|GloomyCloudsRune|
|src/Runes/GoldCardCustomerRune.cs|GoldCardCustomerRune|
|src/Runes/GoldenSpatulaRune.cs|GoldenSpatulaRune|
|src/Runes/GoldrendRune.cs|GoldrendRune|
|src/Runes/GoliathRune.cs|GoliathRune|
|src/Runes/GoodLuckRune.cs|GoodLuckRune|
|src/Runes/GrandFinaleUpgradeRune.cs|GrandFinaleUpgradeRune|
|src/Runes/GroundedRune.cs|GroundedRune|
|src/Runes/GrowingStrongerRune.cs|GrowingStrongerRune|
|src/Runes/HailToTheKingRune.cs|HailToTheKingRune|
|src/Runes/HandOfBaronRune.cs|HandOfBaronRune|
|src/Runes/HangUpgradeRune.cs|HangUpgradeRune|
|src/Runes/HappyAccidentRune.cs|HappyAccidentRune|
|src/Runes/HardBonesRune.cs|HardBonesRune|
|src/Runes/HastyScribbleRune.cs|HastyScribbleRune|
|src/Runes/HattrickRune.cs|HattrickRune|
|src/Runes/HeavyHitterRune.cs|HeavyHitterRune|
|src/Runes/HextechDragonSoulRune.cs|HextechDragonSoulRune|
|src/Runes/HextechGoldrendSync.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/HextechRuneTargeting.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/HextechSharedCombatVictoryRune.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/HiddenGemUpgradeRune.cs|HiddenGemUpgradeRune|
|src/Runes/HomeguardRune.cs|HomeguardRune|
|src/Runes/HotfixUpgradeRune.cs|HotfixUpgradeRune|
|src/Runes/HubrisRune.cs|HubrisRune|
|src/Runes/HundredRefinementsRune.cs|HundredRefinementsRune|
|src/Runes/IllusoryWeaponRune.cs|IllusoryWeaponRune|
|src/Runes/ImmortalBoneRune.cs|ImmortalBoneRune|
|src/Runes/InfernalConduitRune.cs|InfernalConduitRune|
|src/Runes/InfernalDragonSoulRune.cs|InfernalDragonSoulRune|
|src/Runes/InfernoUpgradeRune.cs|InfernoUpgradeRune|
|src/Runes/InfiniteLoopRune.cs|InfiniteLoopRune|
|src/Runes/InitialForgeGrantRune.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/InkshadowRune.cs|InkshadowRune|
|src/Runes/InstantDeathRune.cs|InstantDeathRune|
|src/Runes/IronWaveUpgradeRune.cs|IronWaveUpgradeRune|
|src/Runes/JackpotUpgradeRune.cs|JackpotUpgradeRune|
|src/Runes/JeweledGauntletRune.cs|JeweledGauntletRune|
|src/Runes/JinlianBoxRune.cs|JinlianBoxRune|
|src/Runes/JudicatorRune.cs|JudicatorRune|
|src/Runes/JuggernautUpgradeRune.cs|JuggernautUpgradeRune|
|src/Runes/KakaRune.cs|KakaRune|
|src/Runes/KeystoneHunterRune.cs|KeystoneHunterRune|
|src/Runes/KillerHunterRune.cs|KillerHunterRune|
|src/Runes/KingdomArmyRune.cs|KingdomArmyRune|
|src/Runes/KnowThyPlaceUpgradeRune.cs|KnowThyPlaceUpgradeRune|
|src/Runes/LethalTempoRune.cs|LethalTempoRune|
|src/Runes/LifeFlowRune.cs|LifeFlowRune|
|src/Runes/LightEmUpRune.cs|LightEmUpRune|
|src/Runes/LingeringMightRune.cs|LingeringMightRune|
|src/Runes/LoopRune.cs|LoopRune|
|src/Runes/LoopUpgradeRune.cs|LoopUpgradeRune|
|src/Runes/LubricantRune.cs|LubricantRune|
|src/Runes/MadScientistRune.cs|MadScientistRune|
|src/Runes/MagicMissileRune.cs|MagicMissileRune|
|src/Runes/MakeItMineRune.cs|MakeItMineRune|
|src/Runes/ManipulateRealityRune.cs|ManipulateRealityRune|
|src/Runes/MarkovBabbleRune.cs|MarkovBabbleRune|
|src/Runes/MasterOfDualityRune.cs|MasterOfDualityRune|
|src/Runes/MentalShieldRune.cs|MentalShieldRune|
|src/Runes/MikaelsBlessingRune.cs|MikaelsBlessingRune|
|src/Runes/MindOverMatterRune.cs|MindOverMatterRune|
|src/Runes/MindToMatterRune.cs|MindToMatterRune|
|src/Runes/MirageRune.cs|MirageRune|
|src/Runes/MirrorReflectionRune.cs|MirrorReflectionRune|
|src/Runes/MiserableFateRune.cs|MiserableFateRune|
|src/Runes/MiseryRune.cs|MiseryRune|
|src/Runes/MiseryUpgradeRune.cs|MiseryUpgradeRune|
|src/Runes/MobileHomeRune.cs|MobileHomeRune|
|src/Runes/MoltenFistUpgradeRune.cs|MoltenFistUpgradeRune|
|src/Runes/MonarchsGazeRune.cs|MonarchsGazeRune|
|src/Runes/MoreTheMerrierRune.cs|MoreTheMerrierRune|
|src/Runes/MoreUniversalScopeRune.cs|MoreUniversalScopeRune|
|src/Runes/MostUniversalScopeRune.cs|MostUniversalScopeRune|
|src/Runes/MountainSoulRune.cs|MountainSoulRune|
|src/Runes/MyriadManifestationsRune.cs|MyriadManifestationsRune|
|src/Runes/MyriadSwordsRune.cs|MyriadSwordsRune|
|src/Runes/MysteryRune.cs|MysteryRune|
|src/Runes/NatureIsHealingRune.cs|NatureIsHealingRune|
|src/Runes/NearDeathFeastRune.cs|NearDeathFeastRune|
|src/Runes/NeowsGrudgeRune.cs|NeowsGrudgeRune|
|src/Runes/NetherSoulRune.cs|NetherSoulRune|
|src/Runes/NeurosurgeUpgradeRune.cs|NeurosurgeUpgradeRune|
|src/Runes/NeutralizeUpgradeRune.cs|NeutralizeUpgradeRune|
|src/Runes/NightmareRune.cs|NightmareRune|
|src/Runes/NightmareUpgradeRune.cs|NightmareUpgradeRune|
|src/Runes/NightstalkingRune.cs|NightstalkingRune|
|src/Runes/NimbleRune.cs|NimbleRune|
|src/Runes/NineDragonPowerRune.cs|NineDragonPowerRune|
|src/Runes/NonupeipeGenerosityRune.cs|NonupeipeGenerosityRune|
|src/Runes/NowYouSeeMeRune.cs|NowYouSeeMeRune|
|src/Runes/OblivionUpgradeRune.cs|OblivionUpgradeRune|
|src/Runes/OceanDragonSoulRune.cs|OceanDragonSoulRune|
|src/Runes/OkBoomerangRune.cs|OkBoomerangRune|
|src/Runes/OmegaRune.cs|OmegaRune|
|src/Runes/OminousPactRune.cs|OminousPactRune|
|src/Runes/OmniDragonSoulRune.cs|OmniDragonSoulRune|
|src/Runes/OrbSymbiosisRune.cs|OrbSymbiosisRune|
|src/Runes/OrobasBlessingRune.cs|OrobasBlessingRune|
|src/Runes/OurHealingRune.cs|OurHealingRune|
|src/Runes/OverflowRune.cs|OverflowRune|
|src/Runes/OverlordBloodArmorRune.cs|OverlordBloodArmorRune|
|src/Runes/PacifistRune.cs|PacifistRune|
|src/Runes/PactsEndUpgradeRune.cs|PactsEndUpgradeRune|
|src/Runes/PandorasBoxRune.cs|PandorasBoxRune|
|src/Runes/ParticleWallUpgradeRune.cs|ParticleWallUpgradeRune|
|src/Runes/PiercingThreadRune.cs|PiercingThreadRune|
|src/Runes/PiggyBankRune.cs|PiggyBankRune|
|src/Runes/PlasterRune.cs|PlasterRune|
|src/Runes/PlateletRune.cs|PlateletRune|
|src/Runes/PorcupineRune.cs|PorcupineRune|
|src/Runes/PortableSleepingBagRune.cs|PortableSleepingBagRune|
|src/Runes/PowerShieldRune.cs|PowerShieldRune|
|src/Runes/PrecisionCognitionRune.cs|PrecisionCognitionRune|
|src/Runes/PrimitiveMadnessRune.cs|PrimitiveMadnessRune|
|src/Runes/PrismaticEggRune.cs|PrismaticEggRune|
|src/Runes/ProtectiveVeilRune.cs|ProtectiveVeilRune|
|src/Runes/ProteinShakeRune.cs|ProteinShakeRune|
|src/Runes/QuantumComputingRune.cs|QuantumComputingRune|
|src/Runes/QueenRune.cs|QueenRune|
|src/Runes/RageUpgradeRune.cs|RageUpgradeRune|
|src/Runes/RallyingCallRune.cs|RallyingCallRune|
|src/Runes/ReanimateUpgradeRune.cs|ReanimateUpgradeRune|
|src/Runes/ReapUpgradeRune.cs|ReapUpgradeRune|
|src/Runes/ReaperFormUpgradeRune.cs|ReaperFormUpgradeRune|
|src/Runes/RebootUpgradeRune.cs|RebootUpgradeRune|
|src/Runes/RecycleBinRune.cs|RecycleBinRune|
|src/Runes/RedEnvelopeRune.cs|RedEnvelopeRune|
|src/Runes/ReflectUpgradeRune.cs|ReflectUpgradeRune|
|src/Runes/ReforgedHelmetRune.cs|ReforgedHelmetRune|
|src/Runes/RegenerationSuppressionRune.cs|RegenerationSuppressionRune|
|src/Runes/RekindleRune.cs|RekindleRune|
|src/Runes/RenewalRune.cs|RenewalRune|
|src/Runes/ReprogramRune.cs|ReprogramRune|
|src/Runes/RepulsorRune.cs|RepulsorRune|
|src/Runes/RoyalCommandRune.cs|RoyalCommandRune|
|src/Runes/RoyalTrialRune.cs|RoyalTrialRune|
|src/Runes/RoyaltiesUpgradeRune.cs|RoyaltiesUpgradeRune|
|src/Runes/SacrificeRune.cs|SacrificeRune|
|src/Runes/ScapegoatRune.cs|ScapegoatRune|
|src/Runes/ScaredStiffRune.cs|ScaredStiffRune|
|src/Runes/SearingAttackRune.cs|SearingAttackRune|
|src/Runes/SelfUpgradeOnPlayRuneBase.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/SellOffRune.cs|SellOffRune|
|src/Runes/SendThemInRune.cs|SendThemInRune|
|src/Runes/SerpentFormUpgradeRune.cs|SerpentFormUpgradeRune|
|src/Runes/SerpentsFangRune.cs|SerpentsFangRune|
|src/Runes/ServantMasterRune.cs|ServantMasterRune|
|src/Runes/ShoulderVakuRune.cs|ShoulderVakuRune|
|src/Runes/ShriekUpgradeRune.cs|ShriekUpgradeRune|
|src/Runes/ShrinkEngineRune.cs|ShrinkEngineRune|
|src/Runes/ShrinkRayRune.cs|ShrinkRayRune|
|src/Runes/SingularityAIRune.cs|SingularityAIRune|
|src/Runes/SkyDrillUpgradeRune.cs|SkyDrillUpgradeRune|
|src/Runes/SlapRune.cs|SlapRune|
|src/Runes/SlowCookRune.cs|SlowCookRune|
|src/Runes/SmokestackUpgradeRune.cs|SmokestackUpgradeRune|
|src/Runes/SnakebiteRune.cs|SnakebiteRune|
|src/Runes/SnakebiteUpgradeRune.cs|SnakebiteUpgradeRune|
|src/Runes/SolidTimeRune.HoverTips.cs|SolidTimeRune|
|src/Runes/SolidTimeRune.PowerPlayback.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/SolidTimeRune.StoredCards.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/SolidTimeRune.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/SomethingForNothingRune.cs|SomethingForNothingRune|
|src/Runes/SomethingFromNothingRune.cs|SomethingFromNothingRune|
|src/Runes/SonataRune.cs|SonataRune|
|src/Runes/SoulCallingRune.cs|SoulCallingRune|
|src/Runes/SoulEaterRune.cs|SoulEaterRune|
|src/Runes/SoulUpgradeRune.cs|SoulUpgradeRune|
|src/Runes/SowUpgradeRune.cs|SowUpgradeRune|
|src/Runes/SpeedDemonRune.cs|SpeedDemonRune|
|src/Runes/SpeedsterRune.cs|SpeedsterRune|
|src/Runes/SpinToWinRune.cs|SpinToWinRune|
|src/Runes/StardustUpgradeRune.cs|StardustUpgradeRune|
|src/Runes/StarlightSplendorRune.cs|StarlightSplendorRune|
|src/Runes/StartupRoutineRune.cs|StartupRoutineRune|
|src/Runes/StatsOnStatsOnStatsRune.cs|StatsOnStatsOnStatsRune|
|src/Runes/StatsOnStatsRune.cs|StatsOnStatsRune|
|src/Runes/StatsRune.cs|StatsRune|
|src/Runes/StokeRune.cs|StokeRune|
|src/Runes/StormUpgradeRune.cs|StormUpgradeRune|
|src/Runes/StrengthToDexterityRune.cs|StrengthToDexterityRune|
|src/Runes/StrikeUpgradeRune.cs|StrikeUpgradeRune|
|src/Runes/SturdyRune.cs|SturdyRune|
|src/Runes/SubroutineUpgradeRune.cs|SubroutineUpgradeRune|
|src/Runes/SummonForthRune.cs|SummonForthRune|
|src/Runes/SuperBrainRune.cs|SuperBrainRune|
|src/Runes/SurvivorUpgradeRune.cs|SurvivorUpgradeRune|
|src/Runes/SweepingBladeRune.cs|SweepingBladeRune|
|src/Runes/SwiftAndSafeRune.cs|SwiftAndSafeRune|
|src/Runes/SwordFlightRune.cs|SwordFlightRune|
|src/Runes/SwordIntentRune.cs|SwordIntentRune|
|src/Runes/SwordsmanshipRune.cs|SwordsmanshipRune|
|src/Runes/SymphonyOfWarRune.cs|SymphonyOfWarRune|
|src/Runes/TankEngineRune.cs|TankEngineRune|
|src/Runes/TanksShieldRune.cs|TanksShieldRune|
|src/Runes/TapDanceRune.cs|TapDanceRune|
|src/Runes/TauntRune.cs|TauntRune|
|src/Runes/TerminalIllnessRune.cs|TerminalIllnessRune|
|src/Runes/TezcatarasMercyRune.cs|TezcatarasMercyRune|
|src/Runes/ThornmailRune.cs|ThornmailRune|
|src/Runes/ThoughtOverwriteRune.cs|ThoughtOverwriteRune|
|src/Runes/TormentorRune.cs|TormentorRune|
|src/Runes/TranscendentEvilRune.cs|TranscendentEvilRune|
|src/Runes/TransformBasicCardOnPlayRuneBase.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/TransmuteChaosRune.cs|TransmuteChaosRune|
|src/Runes/TransmuteGoldRune.cs|TransmuteGoldRune|
|src/Runes/TransmutePrismaticRune.cs|TransmutePrismaticRune|
|src/Runes/TriPrismRune.cs|TriPrismRune|
|src/Runes/TrickLicenseRune.cs|TrickLicenseRune|
|src/Runes/TrinityRune.cs|TrinityRune|
|src/Runes/TwiceThriceRune.cs|TwiceThriceRune|
|src/Runes/TwilightVeilRune.cs|TwilightVeilRune|
|src/Runes/TwinFlamesRune.cs|TwinFlamesRune|
|src/Runes/UltimateRefreshRune.cs|UltimateRefreshRune|
|src/Runes/UltimateUnstoppableRune.cs|UltimateUnstoppableRune|
|src/Runes/UndyingUpgradeRune.cs|UndyingUpgradeRune|
|src/Runes/UniversalScopeRune.cs|UniversalScopeRune|
|src/Runes/UnleashUpgradeRune.cs|UnleashUpgradeRune|
|src/Runes/UnmovableMountainRune.cs|UnmovableMountainRune|
|src/Runes/UnsealedThroneRune.cs|UnsealedThroneRune|
|src/Runes/UpgradeRune.cs|UpgradeRune|
|src/Runes/VakuuTurnController.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Runes/VampireCrawlerRune.cs|VampireCrawlerRune|
|src/Runes/VenerateUpgradeRune.cs|VenerateUpgradeRune|
|src/Runes/VenomousBladeRune.cs|VenomousBladeRune|
|src/Runes/ViolenceRune.cs|ViolenceRune|
|src/Runes/VitalitySurgeRune.cs|VitalitySurgeRune|
|src/Runes/VoidFormUpgradeRune.cs|VoidFormUpgradeRune|
|src/Runes/VoltaicUpgradeRune.cs|VoltaicUpgradeRune|
|src/Runes/WarmogsSpiritRune.cs|WarmogsSpiritRune|
|src/Runes/WatchOutGrapefruitRune.cs|WatchOutGrapefruitRune|
|src/Runes/WhirlwindUpgradeRune.cs|WhirlwindUpgradeRune|
|src/Runes/WhiteHoleRune.cs|WhiteHoleRune|
|src/Runes/WizardlyThinkingRune.cs|WizardlyThinkingRune|
|src/Runes/WraithRune.cs|WraithRune|
|src/Runes/WroughtInWarUpgradeRune.cs|WroughtInWarUpgradeRune|
|src/Runes/ZapUpgradeRune.cs|ZapUpgradeRune|
|src/Runes/ZealotRune.cs|ZealotRune|
|src/EnemyHexes/AeonglassEnemyHex.cs|AeonglassEnemyHex|
|src/EnemyHexes/AncientStatueEnemyHex.cs|AncientStatueEnemyHex|
|src/EnemyHexes/AncientWineEnemyHex.cs|AncientWineEnemyHex|
|src/EnemyHexes/ArcanePunchEnemyHex.cs|ArcanePunchEnemyHex|
|src/EnemyHexes/ArchmageEnemyHex.cs|ArchmageEnemyHex|
|src/EnemyHexes/AstralBodyEnemyHex.cs|AstralBodyEnemyHex|
|src/EnemyHexes/AttributeBoostEnemyHex.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/EnemyHexes/BackToBasicsEnemyHex.cs|BackToBasicsEnemyHex|
|src/EnemyHexes/BigStrengthEnemyHex.cs|BigStrengthEnemyHex|
|src/EnemyHexes/BloodArmorEnemyHex.cs|BloodArmorEnemyHex|
|src/EnemyHexes/BloodIdolEnemyHex.cs|BloodIdolEnemyHex|
|src/EnemyHexes/BloodPactEnemyHex.cs|BloodPactEnemyHex|
|src/EnemyHexes/BlueCandleMedkitEnemyHex.cs|BlueCandleMedkitEnemyHex|
|src/EnemyHexes/BrutalForceEnemyHex.cs|BrutalForceEnemyHex|
|src/EnemyHexes/BrutalityEnemyHex.cs|BrutalityEnemyHex|
|src/EnemyHexes/ByrdonisEnemyHex.cs|ByrdonisEnemyHex|
|src/EnemyHexes/CantTouchThisEnemyHex.cs|CantTouchThisEnemyHex|
|src/EnemyHexes/CerberusEnemyHex.cs|CerberusEnemyHex|
|src/EnemyHexes/CeremonialBeastEnemyHex.cs|CeremonialBeastEnemyHex|
|src/EnemyHexes/ClownCollegeEnemyHex.cs|ClownCollegeEnemyHex|
|src/EnemyHexes/CompensationEnemyHex.cs|CompensationEnemyHex|
|src/EnemyHexes/CorrosionEnemyHex.cs|CorrosionEnemyHex|
|src/EnemyHexes/CorruptedBranchEnemyHex.cs|CorruptedBranchEnemyHex|
|src/EnemyHexes/CourageOfColossusEnemyHex.cs|CourageOfColossusEnemyHex|
|src/EnemyHexes/CuttingEdgeAlchemistEnemyHex.cs|CuttingEdgeAlchemistEnemyHex|
|src/EnemyHexes/DawnbringersResolveEnemyHex.cs|DawnbringersResolveEnemyHex|
|src/EnemyHexes/DeathHarvestEnemyHex.cs|DeathHarvestEnemyHex|
|src/EnemyHexes/DevilsDanceEnemyHex.cs|DevilsDanceEnemyHex|
|src/EnemyHexes/DivineInterventionEnemyHex.cs|DivineInterventionEnemyHex|
|src/EnemyHexes/DizzySpinningEnemyHex.cs|DizzySpinningEnemyHex|
|src/EnemyHexes/DoomsdayEnemyHex.cs|DoomsdayEnemyHex|
|src/EnemyHexes/DualWieldEnemyHex.cs|DualWieldEnemyHex|
|src/EnemyHexes/DuffsVintageEnemyHex.cs|DuffsVintageEnemyHex|
|src/EnemyHexes/EightPennyGateEnemyHex.cs|EightPennyGateEnemyHex|
|src/EnemyHexes/EndlessRotationEnemyHex.cs|EndlessRotationEnemyHex|
|src/EnemyHexes/EnemyHexConsoleCmd.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/EnemyHexes/EnemyHexIconRelics.cs|SkulkingColonyHex, PhantasmalGardenerHex, QueenHex, LagavulinMatriarchHex, ExoskeletonHex, TestSubjectHex, LeafSlimeHex, ShrinkerBeetleHex, InkletHex, PhrogParasiteHex, VantomHex, AeonglassHex, TheLostHex, TheForgottenHex, SlimedBerserkerHex, GlobeHeadHex, MyteHex, FossilStalkerHex, TungstenRodHex, HundredRefinementsHex, AncientStatueHex, ByrdonisHex, HungryHex, InspectHex, GripHex, LivingFogHex, CeremonialBeastHex, SoulFyshHex, ThievingHopperHex, HauntedShipHex|
|src/EnemyHexes/EnlightenmentEnemyHex.cs|EnlightenmentEnemyHex|
|src/EnemyHexes/EscapePlanEnemyHex.cs|EscapePlanEnemyHex|
|src/EnemyHexes/ExoskeletonEnemyHex.cs|ExoskeletonEnemyHex|
|src/EnemyHexes/FeelTheBurnEnemyHex.cs|FeelTheBurnEnemyHex|
|src/EnemyHexes/FeyMagicEnemyHex.cs|FeyMagicEnemyHex|
|src/EnemyHexes/FinalFormEnemyHex.cs|FinalFormEnemyHex|
|src/EnemyHexes/FirebrandEnemyHex.cs|FirebrandEnemyHex|
|src/EnemyHexes/FirstAidKitEnemyHex.cs|FirstAidKitEnemyHex|
|src/EnemyHexes/ForbiddenGrimoireEnemyHex.cs|ForbiddenGrimoireEnemyHex|
|src/EnemyHexes/ForgottenSoulEnemyHex.cs|ForgottenSoulEnemyHex|
|src/EnemyHexes/FossilStalkerEnemyHex.cs|FossilStalkerEnemyHex|
|src/EnemyHexes/FrostWraithEnemyHex.cs|FrostWraithEnemyHex|
|src/EnemyHexes/GetExcitedEnemyHex.cs|GetExcitedEnemyHex|
|src/EnemyHexes/GiantSlayerEnemyHex.cs|GiantSlayerEnemyHex|
|src/EnemyHexes/GlassCannonEnemyHex.cs|GlassCannonEnemyHex|
|src/EnemyHexes/GlobeHeadEnemyHex.cs|GlobeHeadEnemyHex|
|src/EnemyHexes/GoldenSpatulaEnemyHex.cs|GoldenSpatulaEnemyHex|
|src/EnemyHexes/GoldrendEnemyHex.cs|GoldrendEnemyHex|
|src/EnemyHexes/GoliathEnemyHex.cs|GoliathEnemyHex|
|src/EnemyHexes/HailToTheKingEnemyHex.cs|HailToTheKingEnemyHex|
|src/EnemyHexes/HandOfBaronEnemyHex.cs|HandOfBaronEnemyHex|
|src/EnemyHexes/HastyScribbleEnemyHex.cs|HastyScribbleEnemyHex|
|src/EnemyHexes/HauntedShipEnemyHex.cs|HauntedShipEnemyHex|
|src/EnemyHexes/HeavyHitterEnemyHex.cs|HeavyHitterEnemyHex|
|src/EnemyHexes/HextechEnemyDrawProgress.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/EnemyHexes/HextechEnemyHexContext.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/EnemyHexes/HextechEnemyHexDispatcher.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/EnemyHexes/HextechEnemyHexEffect.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/EnemyHexes/HextechEnemyHexEffects.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/EnemyHexes/HextechEnemyHexGlobalUsings.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/EnemyHexes/HextechEnemyNearDeath.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/EnemyHexes/HextechEnemyStatusCards.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/EnemyHexes/HundredRefinementsEnemyHex.cs|HundredRefinementsEnemyHex|
|src/EnemyHexes/IGripEnemyHex.cs|IGripEnemyHex|
|src/EnemyHexes/IInspectEnemyHex.cs|IInspectEnemyHex|
|src/EnemyHexes/InfestedPrismHex.cs|InfestedPrismHex|
|src/EnemyHexes/InkletEnemyHex.cs|InkletEnemyHex|
|src/EnemyHexes/JeweledGauntletEnemyHex.cs|JeweledGauntletEnemyHex|
|src/EnemyHexes/JinlianBoxEnemyHex.cs|JinlianBoxEnemyHex|
|src/EnemyHexes/JudicatorEnemyHex.cs|JudicatorEnemyHex|
|src/EnemyHexes/LagavulinMatriarchEnemyHex.cs|LagavulinMatriarchEnemyHex|
|src/EnemyHexes/LeafSlimeEnemyHex.cs|LeafSlimeEnemyHex|
|src/EnemyHexes/LightEmUpEnemyHex.cs|LightEmUpEnemyHex|
|src/EnemyHexes/LivingFogEnemyHex.cs|LivingFogEnemyHex|
|src/EnemyHexes/LoopEnemyHex.cs|LoopEnemyHex|
|src/EnemyHexes/MadScientistEnemyHex.cs|MadScientistEnemyHex|
|src/EnemyHexes/ManipulateRealityEnemyHex.cs|ManipulateRealityEnemyHex|
|src/EnemyHexes/MasterOfDualityEnemyHex.cs|MasterOfDualityEnemyHex|
|src/EnemyHexes/MikaelsBlessingEnemyHex.cs|MikaelsBlessingEnemyHex|
|src/EnemyHexes/MindOverMatterEnemyHex.cs|MindOverMatterEnemyHex|
|src/EnemyHexes/MirrorReflectionEnemyHex.cs|MirrorReflectionEnemyHex|
|src/EnemyHexes/MiserableFateEnemyHex.cs|MiserableFateEnemyHex|
|src/EnemyHexes/MonarchsGazeEnemyHex.cs|MonarchsGazeEnemyHex|
|src/EnemyHexes/MonsterHexCatalog.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/EnemyHexes/MoreTheMerrierEnemyHex.cs|MoreTheMerrierEnemyHex|
|src/EnemyHexes/MountainSoulEnemyHex.cs|MountainSoulEnemyHex|
|src/EnemyHexes/MysteryEnemyHex.cs|MysteryEnemyHex|
|src/EnemyHexes/MyteEnemyHex.cs|MyteEnemyHex|
|src/EnemyHexes/NatureIsHealingEnemyHex.cs|NatureIsHealingEnemyHex|
|src/EnemyHexes/NearDeathFeastEnemyHex.cs|NearDeathFeastEnemyHex|
|src/EnemyHexes/NightstalkingEnemyHex.cs|NightstalkingEnemyHex|
|src/EnemyHexes/OmegaEnemyHex.cs|OmegaEnemyHex|
|src/EnemyHexes/OminousPactEnemyHex.cs|OminousPactEnemyHex|
|src/EnemyHexes/OmniDragonSoulEnemyHex.cs|OmniDragonSoulEnemyHex|
|src/EnemyHexes/PandorasBoxEnemyHex.cs|PandorasBoxEnemyHex|
|src/EnemyHexes/PhantasmalGardenerEnemyHex.cs|PhantasmalGardenerEnemyHex|
|src/EnemyHexes/PhrogParasiteEnemyHex.cs|PhrogParasiteEnemyHex|
|src/EnemyHexes/PorcupineEnemyHex.cs|PorcupineEnemyHex|
|src/EnemyHexes/ProtectiveVeilEnemyHex.cs|ProtectiveVeilEnemyHex|
|src/EnemyHexes/ProteinShakeEnemyHex.cs|ProteinShakeEnemyHex|
|src/EnemyHexes/QueenEnemyHex.cs|QueenEnemyHex|
|src/EnemyHexes/ReforgedHelmetEnemyHex.cs|ReforgedHelmetEnemyHex|
|src/EnemyHexes/RepulsorEnemyHex.cs|RepulsorEnemyHex|
|src/EnemyHexes/SerpentsFangEnemyHex.cs|SerpentsFangEnemyHex|
|src/EnemyHexes/ServantMasterEnemyHex.cs|ServantMasterEnemyHex|
|src/EnemyHexes/ShoulderVakuEnemyHex.cs|ShoulderVakuEnemyHex|
|src/EnemyHexes/ShrinkEngineEnemyHex.cs|ShrinkEngineEnemyHex|
|src/EnemyHexes/ShrinkRayEnemyHex.cs|ShrinkRayEnemyHex|
|src/EnemyHexes/ShrinkerBeetleEnemyHex.cs|ShrinkerBeetleEnemyHex|
|src/EnemyHexes/SingularityAIEnemyHex.cs|SingularityAIEnemyHex|
|src/EnemyHexes/SkulkingColonyEnemyHex.cs|SkulkingColonyEnemyHex|
|src/EnemyHexes/SlapEnemyHex.cs|SlapEnemyHex|
|src/EnemyHexes/SlimedBerserkerEnemyHex.cs|SlimedBerserkerEnemyHex|
|src/EnemyHexes/SolidTimeEnemyHex.cs|SolidTimeEnemyHex|
|src/EnemyHexes/SomethingForNothingEnemyHex.cs|SomethingForNothingEnemyHex|
|src/EnemyHexes/SonataEnemyHex.cs|SonataEnemyHex|
|src/EnemyHexes/SoulEaterEnemyHex.cs|SoulEaterEnemyHex|
|src/EnemyHexes/SoulFyshEnemyHex.cs|SoulFyshEnemyHex|
|src/EnemyHexes/SpeedDemonEnemyHex.cs|SpeedDemonEnemyHex|
|src/EnemyHexes/StartupRoutineEnemyHex.cs|StartupRoutineEnemyHex|
|src/EnemyHexes/StatsEnemyHex.cs|StatsEnemyHex|
|src/EnemyHexes/StatsOnStatsEnemyHex.cs|StatsOnStatsEnemyHex|
|src/EnemyHexes/StatsOnStatsOnStatsEnemyHex.cs|StatsOnStatsOnStatsEnemyHex|
|src/EnemyHexes/SturdyEnemyHex.cs|SturdyEnemyHex|
|src/EnemyHexes/SuperBrainEnemyHex.cs|SuperBrainEnemyHex|
|src/EnemyHexes/SwiftAndSafeEnemyHex.cs|SwiftAndSafeEnemyHex|
|src/EnemyHexes/TankEngineEnemyHex.cs|TankEngineEnemyHex|
|src/EnemyHexes/TanksShieldEnemyHex.cs|TanksShieldEnemyHex|
|src/EnemyHexes/TestSubjectEnemyHex.cs|TestSubjectEnemyHex|
|src/EnemyHexes/TezcatarasMercyEnemyHex.cs|TezcatarasMercyEnemyHex|
|src/EnemyHexes/TheForgottenEnemyHex.cs|TheForgottenEnemyHex|
|src/EnemyHexes/TheLostEnemyHex.cs|TheLostEnemyHex|
|src/EnemyHexes/ThievingHopperEnemyHex.cs|ThievingHopperEnemyHex|
|src/EnemyHexes/ThornmailEnemyHex.cs|ThornmailEnemyHex|
|src/EnemyHexes/TormentorEnemyHex.cs|TormentorEnemyHex|
|src/EnemyHexes/TungstenRodEnemyHex.cs|TungstenRodEnemyHex|
|src/EnemyHexes/TwiceThriceEnemyHex.cs|TwiceThriceEnemyHex|
|src/EnemyHexes/TwilightVeilEnemyHex.cs|TwilightVeilEnemyHex|
|src/EnemyHexes/UnmovableMountainEnemyHex.cs|UnmovableMountainEnemyHex|
|src/EnemyHexes/UpgradeEnemyHex.cs|UpgradeEnemyHex|
|src/EnemyHexes/VantomEnemyHex.cs|VantomEnemyHex|
|src/EnemyHexes/VitalitySurgeEnemyHex.cs|VitalitySurgeEnemyHex|
|src/EnemyHexes/WarmogsSpiritEnemyHex.cs|WarmogsSpiritEnemyHex|
|src/EnemyHexes/ZealotEnemyHex.cs|ZealotEnemyHex|
|src/Cards/BladeWaltzCard.cs|BladeWaltzCard|
|src/Cards/CardTransformUpgradeHelper.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Cards/FeelTheBurnCard.cs|FeelTheBurnCard|
|src/Cards/HextechCardGeneration.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Cards/HextechColorlessCardHelper.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Cards/HextechCustomCards.cs|TrickMagicCard, CatalystCard|
|src/Cards/HextechDragonSoulCards.cs|OceanDragonSoulCard, InfernalDragonSoulCard, HextechDragonSoulCard, MountainDragonSoulCard, ChemtechDragonSoulCard, CloudDragonSoulCard|
|src/Cards/HextechDragonSoulPowers.cs|HextechOceanDragonSoulPower, HextechInfernalDragonSoulPower, HextechDragonSoulPower, HextechMountainDragonSoulPower, HextechChemtechDragonSoulPower, HextechCloudDragonSoulPower|
|src/Cards/HextechOwnerPoolTokenCard.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Cards/HextechRegentGeneratedCardHelper.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Cards/MikaelsBlessingCard.cs|MikaelsBlessingCard|
|src/Cards/OkBoomerangCard.cs|OkBoomerangCard|
|src/Cards/OstyWishCard.cs|OstyWishCard|
|src/Cards/ReprogramCard.cs|ReprogramCard|
|src/Cards/SearingAttackCard.cs|SearingAttackCard|
|src/Cards/WhiteHoleCard.cs|WhiteHoleCard|
|src/Powers/HextechGalvanicPower.cs|HextechGalvanicPower|
|src/Powers/HextechHangPower.cs|HextechHangPower|
|src/Powers/HextechNextTurnDamagePower.cs|HextechNextTurnDamagePower|
|src/Powers/HextechPowers.cs|HextechBurnPower, HextechAttackReplayPower, HextechPlayerSlowPower|
|src/Powers/HextechVitalSparkPower.cs|HextechVitalSparkPower|
|src/Forges/HextechEnchantForges.cs|GlamForge, SwiftForge, SoulsPowerForge, MomentumForge, EmbersForge, SpiralForge|
|src/Forges/HextechForgeBase.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Forges/HextechForgeGrantHelper.cs|RandomForgeShopRelic|
|src/Forges/HextechForgeShopPriceHelper.cs|辅助实现／基类／分部类；跟随对应内容审阅|
|src/Forges/HextechForges.Gold.cs|ConstitutionForge, DisasterForge, GoldLifeForge, GoldHpForge, GoldAttackForge, GoldProtectionForge, GoldFocusForge, DrawForge, RecoveryForge, HourglassForge, GoldUpgradeForge, SummonForge, FleshForge, StarsForge, OrbSlotForge, VenomForge, ShrinkForge, PlatingForge, ThornsForge, ArtifactForge|
|src/Forges/HextechForges.Prismatic.cs|PrismaticLifeForge, AttackForge, ProtectionForge, EnergyForge, RitualForge, RegenForge, BufferForge, SlipperyForge, PrismaticArtifactForge, FortuneForge, VoidForge|
|src/Forges/HextechForges.Silver.cs|StrengthForge, DexterityForge, SilverPlatingForge, UpgradeForge, FocusForge, LifeForge, SilverHpForge, SilverAttackForge, SilverProtectionForge, PocketForge, PreparedForge, FireworksForge, VigorForge, BlockForge, NecrobinderForge, SilverStarsForge, SilverOrbForge, ForgingForge|
|src/Enchantments/UniversalSpiral.cs|UniversalSpiral|

## 阶段 B 与验证

阶段 A 先行落盘，随后才修改文案。初始 56 项语义发现中 **53 项已按实现修正文案，3 项保留待裁决**；前面的表保留审计当时的文案与事实，不用修复后的文字覆盖历史证据。623 个直接描述模型中，其余 567 个没有独立登记的语义问题；这不表示它们没有术语、格式或数字引用改动。

### 修复范围与统计口径

- 251 处既有数值引用候选中，250 处已使用语义对应的占位符；飞身踢原说明中的静态 8% 公式删除，改为显示模型实时刷新的 `ExecutePercent`。相等数字不等于相同变量，没有机械替换。
- 在 24 个模型、21 个文件中补充 28 个纯显示 DynamicVar，复用原有常量，或把原数值原样提取成常量后供实现和显示共同引用。未改变已有 CanonicalVar 值、玩法 Hook、SavedProperty、模型身份或同步协议。新增变量不用于战斗决策。
- 修正“升级：势不可挡”为原版“升级：势不可当”；九语言卡牌、能力、关键词引用按官方术语表对齐。灼烧中文动词统一为“施加”，角色后缀采用约定短称。“自然即是治愈”的判定为单机限定，使用“（仅单机出现）”，没有误写为联机限定。
- 初始 351 个语言键、88 个唯一键的格式差异以及 3 处错误占位符已处理；最终校验逐键对比九语言占位符集合、标签数量与配平。
- 阶段 B 的外语交叉复核另纠正了 8 项韩语数值错译：沙漏伤害 4→5、黄金覆甲 5→6、白银覆甲 3→4、药水栏 1→2、预备抽牌 1→2、活力 4→2、格挡 2→3、铸造 5→8；均改为对应变量。另修正谢幕的外语旧卡名/保留关键词及日语关键词修饰关系。它们计入下方实际语言键数，不回填为阶段 A 的 56 项中文语义发现。
- `docs/rune-event-notes.jsonl` 留下 369 个唯一符文的 Hook、命令/状态及上限记录，供后续维护使用。

以下按相对基线 Git JSON 值实际发生变化的键去重统计，不按修改轮数累加；共 3,875 个语言键，九语言新增/删除键均为 0，flavor 改动为 0。

|语言目录|改动键数|
|---|---:|
|zhs|383|
|eng|378|
|esp|481|
|spa|470|
|jpn|454|
|kor|400|
|ptb|447|
|rus|475|
|tha|387|

### 保留硬编码的原因

没有宣称消除所有字面数字。以下保留项已按实际语义核对：

|类别/例子|依据与处理|
|---|---|
|敌方三阶 0/1/2 等总览|`ShrinkEngineEnemyHex`、`OmniDragonSoulEnemyHex`、`TestSubjectEnemyHex` 用 `TierValue`；`MonsterHexCatalog` 只注入已定义的人数缩放变量，不存在表示三阶总览的单一玩家 DynamicVar。保留三阶数字，不能借用同名我方符文变量。|
|最终形态每回合 1 次|由 `HasTurnProcTriggered/TryConsumeTurnProc` 的布尔门闩实现，不是层数变量。保留次数 1。|
|生命回流每 1 张牌|一次消耗 Hook 处理一张牌，是计数单位，不能用恰好同值的治疗或上限变量代替。|
|裁决使 50% 阈值|来自 `CurrentHp * 2 >= MaxHp` 比较式，没有现成百分比变量。保留文字 50%，未为文案改写判断式。|
|红包 20～50 金币|`AddStableRangedExtraGoldReward` 的字面实参；本次复用已有概率常量，不为这组奖励上下限扩展模型变量。|
|最低保留 1 点生命、最少 1 个奖励选项|来自非致死或 `Math.Max` 下限；保留含义明确的 1，不借同值 Power 层数。|

### 工具与 TXT

`tools/sync_content_txt.py` 现在按模型解析 CanonicalVar/PowerVar/命名 ForgeVar 和常量，处理 `diff()`、`energyIcons()`，剥离富文本；未知变量或不能可靠求值的表达式明确失败，不把裸占位符写入 TXT。C# 整数除法缺少类型上下文时不猜测结果。运行时百分比使用已核实的说明公式；敌方人数缩放来自敌方注册表，不复制我方数值。

三个内容 TXT 已重生成。通过显式采纳本轮 365 个已审 JSON 锚点更新说明，保留手改保护、原有卡牌费用/升级信息和未登记的六行先古遗物说明；没有使用 `--prune`。这六行不属于工具的注册表生成范围，不表示从游戏中删除了这些遗物。

`tools/validate_hextech_content.py` 只增加用户指定的两类检查：

1. 原版中文名称引用：读取 `tools/official_zhs_titles.json` 小型快照，包含 624 个卡名、289 个能力名及 68 个关联官方术语，并登记 323 个文案键的 396 个原版模型引用。检查升级卡绑定、标记名称和已登记的普通文本引用；不在校验时读取本机 PCK。新增普通文本引用仍须维护登记，未声称自动理解任意自然语言句子。
2. 九语言逐键占位符集合、BBCode 配平和标签数量一致；语言目录或文件缺失也失败。

工具回归覆盖变量解析、错误输入拒绝、手改保护及上述两类校验的正反例，没有增加其他内容校验类别。

### 最终验证原文与边界

最终文案冻结后执行：

```text
$ python3 tools/validate_hextech_content.py
Hextech content validation passed.

$ python3 -m unittest discover -s tools/tests -p test_developer_tools.py
.........
----------------------------------------------------------------------
Ran 9 tests in 0.112s

OK
```

工具测试后另有临时夹具打包成功输出；这是临时目录测试，未制作、部署或发布本次发行包。

```text
$ python3 tools/sync_content_txt.py
summary: 已从注册表移除的条目 6 条,保留原位(--prune 才删除): 事件遗物:先古遗物+（二次获得欧洛巴斯之触时替换对应先古遗物，默认不在图鉴及随机池出现）; 事件遗物:黑暗之血+; 事件遗物:长蛇戒指+; 事件遗物:天命所归+; 事件遗物:无界命匣+; 事件遗物:注能核心+
三个 txt 与真值一致(待裁决分歧除外)。

$ git diff --check
（无输出，退出码 0）
```

通过 `python3 tools/hextech_dev.py tests` 选择文案/元数据相关组后执行，原始日志 `/tmp/hextech-description-audit-20260921/targeted-tests.log`：

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
19/19 tests passed
```

两任务整合后的验证顺序：联机修复完成时，三个维护版本各全量 `322/322 tests passed`；之后增加纯显示变量，最终 C# 冻结后在 0.107.1、0.110.0、0.111.0 各跑元数据/保存/注册/静态清单/掉率定向组，均为：

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
11/11 tests passed
```

最终三版日志：`/tmp/hextech-display-vars-final-0107-20260921.log`、`/tmp/hextech-display-vars-final-0110-20260921.log`、`/tmp/hextech-display-vars-final-0111-20260921.log`。显示变量中间较广定向组还曾各通过 29/29；不把此前全量执行冒充为最后一次显示变量修改之后的全量执行。

全部构建使用 `HEXTECH_DEPLOY=0`。未提交、推送、部署、上传工坊；未做实机 UI 或双客户端联机验证。联机审计的失败记录、修复范围与实机限制另见 `multiplayer-determinism-audit-2026-09-21.md`。
