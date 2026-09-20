# 文案待裁决清单（2026-09-21）

来源：Codex 描述审计中被回退到 `6a5ec941` 原文的中文条目。游戏内文案目前全部是原文；
下面每条给出原文与 Codex 提议，批准哪条就把哪条写回 9 种语言。Codex 的代码依据见 `description-audit-2026-09-21.md`。

**裁决（2026-09-21，Natsuki）**：只采纳 B 类里与灼烧相关的 4 条，句式统一为"对……施加"：`TORMENTOR_RUNE.description`、`tormentorRune.enemyDescription`、`INFERNO_UPGRADE_RUNE.description`、`FLAME_BARRIER_UPGRADE_RUNE.description`（势不可当改名此前已单独采纳）。其余 A/B/C/D 全部不采纳，保持原文。

## A. 在原句后追加说明（原文未动，只是多了一句）（42 条）

- `cards:INFERNAL_DRAGON_SOUL_CARD.description`
  - 原文：每回合的首张攻击牌会对敌人施加[blue]{BurnPower}[/blue]层[gold]灼烧[/gold]。
  - 提议：每回合的首张攻击牌会对敌人施加[blue]{BurnPower}[/blue]层[gold]灼烧[/gold]。只计算手动打出且以敌人为目标的攻击牌。

- `powers:HEXTECH_BURN_POWER.description`
  - 原文：结算时受到等同于层数百分比的当前生命伤害，最低等同于层数。随后灼烧层数减少[blue]10%[/blue]。
  - 提议：结算时受到等同于层数百分比的当前生命伤害，最低等同于层数。随后灼烧层数减少[blue]10%[/blue]。敌人在自身回合开始时受到无法格挡的伤害；玩家在自身回合结束时受到可格挡的伤害。减少的层数向上取整，至少1层。

- `powers:HEXTECH_BURN_POWER.smartDescription`
  - 原文：结算时受到当前生命值的[blue]{Amount}%[/blue]伤害，最低[blue]{Amount}[/blue]点。随后灼烧层数减少[blue]10%[/blue]。
  - 提议：结算时受到当前生命值的[blue]{Amount}%[/blue]伤害，最低[blue]{Amount}[/blue]点。随后灼烧层数减少[blue]10%[/blue]。敌人在自身回合开始时受到无法格挡的伤害；玩家在自身回合结束时受到可格挡的伤害。减少的层数向上取整，至少1层。

- `powers:HEXTECH_NEXT_TURN_DAMAGE_POWER.description`
  - 原文：在玩家的下个回合开始时，受到等同于层数的伤害。
  - 提议：在玩家的下个回合开始时，受到等同于层数的伤害。此伤害无法被格挡。

- `powers:HEXTECH_NEXT_TURN_DAMAGE_POWER.smartDescription`
  - 原文：在玩家的下个回合开始时，受到[blue]{Amount}[/blue]点伤害。
  - 提议：在玩家的下个回合开始时，受到[blue]{Amount}[/blue]点伤害。此伤害无法被格挡。

- `powers:HEXTECH_PLAYER_SLOW_POWER.description`
  - 原文：受到的伤害改变等同于层数的百分比。层数为负时，改为降低受到的伤害。
  - 提议：受到的伤害改变等同于层数的百分比。层数为负时，改为降低受到的伤害。不影响固定伤害。

- `powers:HEXTECH_PLAYER_SLOW_POWER.smartDescription`
  - 原文：受到的伤害改变[blue]{Amount}%[/blue]。层数为负时，改为降低受到的伤害。
  - 提议：受到的伤害改变[blue]{Amount}%[/blue]。层数为负时，改为降低受到的伤害。不影响固定伤害。

- `powers:HEXTECH_INFERNAL_DRAGON_SOUL_POWER.description`
  - 原文：每回合的首张攻击牌会对敌人施加等同于层数的[gold]灼烧[/gold]。
  - 提议：每回合的首张攻击牌会对敌人施加等同于层数的[gold]灼烧[/gold]。只计算手动打出且以敌人为目标的攻击牌。

- `powers:HEXTECH_INFERNAL_DRAGON_SOUL_POWER.smartDescription`
  - 原文：每回合的首张攻击牌会对敌人施加[blue]{Amount}[/blue]层[gold]灼烧[/gold]。
  - 提议：每回合的首张攻击牌会对敌人施加[blue]{Amount}[/blue]层[gold]灼烧[/gold]。只计算手动打出且以敌人为目标的攻击牌。

- `relics:GLASS_CANNON_RUNE.description`
  - 原文：造成的伤害提高[blue]50%[/blue]，但你的生命值无法回复到[blue]70%[/blue]以上。
  - 提议：造成的伤害提高[blue]{DamageBonusPercent}%[/blue]，但你的生命值无法回复到[blue]{HealCapDisplayPercent}%[/blue]以上。获得时，当前生命值也会降至最多{HealCapDisplayPercent}%最大生命值。

- `relics:SPEED_DEMON_RUNE.description`
  - 原文：每回合首次对敌人造成伤害时，额外抽[blue]2[/blue]张牌。
  - 提议：每回合首次对敌人造成伤害时，额外抽[blue]2[/blue]张牌。伤害必须未被格挡。

- `relics:INFERNAL_CONDUIT_RUNE.description`
  - 原文：你的攻击牌会对敌人施加[blue]2[/blue]层[gold]灼烧[/gold]。敌人每有[blue]5[/blue]层[gold]灼烧[/gold]，你就在下回合开始时额外获得[blue]1[/blue]点能量。
  - 提议：你的攻击牌会对敌人施加[blue]2[/blue]层[gold]灼烧[/gold]。敌人每有[blue]5[/blue]层灼烧，你就在下回合开始时额外获得[blue]1[/blue]点能量。能量在你的回合结束时按每个存活敌人的层数分别计算。

- `relics:GOLIATH_RUNE.description`
  - 原文：体型变大，获得[blue]35%[/blue]最大生命值，并获得[blue]20%[/blue]的伤害、格挡与治疗加成。
  - 提议：体型变大，获得[blue]{MaxHpBonusPercent}%[/blue]最大生命值，并获得[blue]{SustainBonusPercent}%[/blue]的伤害、格挡与治疗加成。获得时，回复所有已损失的生命。

- `relics:BACK_TO_BASICS_RUNE.description`
  - 原文：你无法打出[blue]3[/blue]费及以上的卡牌，你获得的治疗量，格挡量和造成的伤害增加[blue]40%[/blue]。
  - 提议：你无法打出[blue]3[/blue]费及以上的卡牌，你获得的治疗量，格挡量和造成的伤害增加[blue]40%[/blue]。X费牌不受此出牌限制影响。

- `relics:LIGHT_EM_UP_RUNE.description`
  - 原文：每打出[blue]{Attacks}[/blue]张攻击牌后，会对其目标发射[blue]{Missiles}[/blue]个飞弹，每个飞弹造成等同于攻击牌耗能的伤害。
  - 提议：每打出[blue]{Attacks}[/blue]张攻击牌后，会对其目标发射[blue]{Missiles}[/blue]个飞弹，每个飞弹造成等同于攻击牌耗能的伤害。只计算手动打出；达到次数后，若该攻击牌耗能为0，则保留计数，直到打出耗能大于0的攻击牌。

- `relics:POCKET_FORGE.description`
  - 原文：获得时，获得[blue]2[/blue]个药水栏位。
  - 提议：获得时，获得[blue]{PotionSlots}[/blue]个药水栏位。药水栏位最多16个。

- `relics:LIFE_FLOW_RUNE.description`
  - 原文：每当有[blue]1[/blue]张牌被消耗时，回复最大生命值的[blue]5%[/blue]。每回合最多触发[blue]3[/blue]次。
  - 提议：每当有[blue]1[/blue]张牌被消耗时，回复最大生命值的[blue]{HealDisplayPercent}%[/blue]。每回合最多触发[blue]{MaxProcsPerTurn}[/blue]次。只计算你自己的牌。（仅战士）

- `relics:tankEngineRune.enemyDescription`
  - 原文：每回合开始时，敌人获得[blue]5%[/blue]的最大生命值，并使自身体型增大[blue]5%[/blue]。
  - 提议：每回合开始时，敌人获得[blue]5%[/blue]的最大生命值，并使自身体型增大[blue]5%[/blue]。生命加成按获得此效果时捕获的基础最大生命值加算，不按当前最大生命值复利；在敌方回合开始时触发。

- `relics:glassCannonRune.enemyDescription`
  - 原文：敌人造成的伤害提高[blue]30[/blue]/[blue]40[/blue]/[blue]50[/blue]%，但其生命值无法回复到[blue]70%[/blue]以上。
  - 提议：敌人造成的伤害提高[blue]30[/blue]/[blue]40[/blue]/[blue]50[/blue]%，但其生命值无法回复到[blue]70%[/blue]以上。获得此效果时，敌人的当前生命值也会降至最多70%最大生命值。

- `relics:feyMagicRune.enemyDescription`
  - 原文：当敌人造成未被格挡的伤害时，使玩家获得[blue]1[/blue]回合的[gold]缩小[/gold]和[gold]不可抽牌[/gold]。
  - 提议：当敌人造成未被格挡的伤害时，使玩家获得[blue]1[/blue]回合的[gold]缩小[/gold]和[gold]不可抽牌[/gold]。这些效果在下个玩家回合开始时施加。

- `relics:mirrorReflectionRune.enemyDescription`
  - 原文：玩家打出[gold]打击[/gold]或[gold]防御[/gold]时，在弃牌堆放入[blue]1[/blue]张此牌的复制品。
  - 提议：玩家打出[gold]打击[/gold]或[gold]防御[/gold]时，在弃牌堆放入[blue]1[/blue]张此牌的复制品。只计算手动打出的基础打击或基础防御，不计算自动打出或重放。

- `relics:tanksShieldRune.enemyDescription`
  - 原文：玩家每打出[blue]1[/blue]张攻击牌，所有敌人获得[blue]1[/blue]/[blue]2[/blue]/[blue]3[/blue]点格挡。
  - 提议：玩家每打出[blue]1[/blue]张攻击牌，所有敌人获得[blue]1[/blue]/[blue]2[/blue]/3点格挡。只计算手动打出，不计算自动打出或重放。

- `relics:gripHex.enemyDescription`
  - 原文：玩家每回合打出第1张牌时，失去[blue]0[/blue]/[blue]1[/blue]/[blue]2[/blue]点能量。
  - 提议：玩家每回合打出第1张牌时，失去[blue]0[/blue]/[blue]1[/blue]/[blue]2[/blue]点能量。只计算手动打出，不计算自动打出或重放。

- `relics:upgradeRune.enemyDescription`
  - 原文：玩家打出卡牌后，该卡牌会在本场战斗中被降级。
  - 提议：玩家打出卡牌后，该卡牌会在本场战斗中被降级。只计算手动打出，不计算自动打出或重放。

- `relics:nearDeathFeastRune.enemyDescription`
  - 原文：敌人的生命值可降至负数。负生命值时无法获得治疗和格挡，但每[blue]1[/blue]点负生命值获得[blue]1[/blue]点[gold]力量[/gold]。负生命值达到最大生命值的[blue]5%[/blue]/[blue]10%[/blue]/[blue]15%[/blue]时死亡。
  - 提议：敌人的生命值可降至负数。负生命值时无法获得治疗和格挡，但每[blue]1[/blue]点负生命值获得[blue]1[/blue]点[gold]力量[/gold]。负生命值达到最大生命值的[blue]5%[/blue]/[blue]10%[/blue]/[blue]15%[/blue]时死亡。生命值为0时也无法获得治疗和格挡。

- `relics:monarchsGazeRune.enemyDescription`
  - 原文：玩家打出攻击牌时，失去[blue]1[/blue]点临时力量。
  - 提议：玩家打出攻击牌时，失去[blue]1[/blue]点临时力量。只计算手动打出，不计算自动打出或重放。

- `relics:swiftAndSafeRune.enemyDescription`
  - 原文：玩家每抽[blue]15[/blue]/[blue]12[/blue]/[blue]10[/blue]张牌，所有敌人获得[blue]1[/blue]层[gold]人工制品[/gold]。
  - 提议：玩家每抽[blue]15[/blue]/[blue]12[/blue]/[blue]10[/blue]张牌，所有敌人获得[blue]1[/blue]层[gold]人工制品[/gold]。联机时按全队累计抽牌计算，触发阈值乘以玩家人数。

- `relics:natureIsHealingRune.enemyDescription`
  - 原文：战斗中，所有敌人每[blue]15[/blue]/[blue]10[/blue]/[blue]5[/blue]秒回复[blue]1[/blue]点生命。
  - 提议：战斗中，所有敌人每[blue]15[/blue]/[blue]10[/blue]/[blue]5[/blue]秒回复[blue]1[/blue]点生命。联机时改为在每个敌方回合开始时回复1点生命。

- `relics:REKINDLE_RUNE.description`
  - 原文：每当有[blue]2[/blue]张牌被消耗时，获得[blue]1[/blue]点能量。
  - 提议：每当有[blue]{Cards}[/blue]张牌被消耗时，获得[blue]{Energy}[/blue]点能量。只计算你自己的牌。（仅战士）

- `relics:ADVANCE_TO_RETREAT_RUNE.description`
  - 原文：攻击带有[gold]易伤[/gold]的敌人时，获得[blue]3[/blue]点格挡。
  - 提议：攻击带有[gold]易伤[/gold]的敌人时，获得[blue]{Block}[/blue]点格挡。仅在攻击牌实际造成伤害时触发。（仅战士）

- `relics:NEAR_DEATH_FEAST_RUNE.description`
  - 原文：你的生命值可降至负数。负生命值时无法获得治疗和格挡，但每[blue]1[/blue]点负生命值获得[blue]1[/blue]点[gold]力量[/gold]。负生命值达到最大生命值的[blue]50%[/blue]时死亡。
  - 提议：你的生命值可降至负数。负生命值时无法获得治疗和格挡，但每[blue]1[/blue]点负生命值获得[blue]{StrengthPerNegativeHp}[/blue]点[gold]力量[/gold]。负生命值达到最大生命值的[blue]{DeathNegativeMaxHpPercent}%[/blue]时死亡。生命值为0时也无法获得治疗和格挡。若以濒死状态获胜，战后恢复为1点生命。（仅战士）

- `relics:CHAIN_IN_SLEEVE_RUNE.description`
  - 原文：每当你打出[blue]3[/blue]张[gold]小刀[/gold]，就向你的手牌中添加[blue]1[/blue]张[gold]小刀[/gold]。
  - 提议：每当你打出[blue]{ShivsNeeded}[/blue]张[gold]小刀[/gold]，就向你的手牌中添加[blue]{Cards}[/blue]张[gold]小刀[/gold]。只计算手动打出，不计算自动打出或重放。（仅猎人）

- `relics:ROYAL_TRIAL_RUNE.description`
  - 原文：每当你打出[gold]君王之剑[/gold]，随机将[blue]2[/blue]张仆从牌加入手牌。
  - 提议：每当你打出[gold]君王之剑[/gold]，随机将[blue]{Cards}[/blue]张仆从牌加入手牌。只计算手动打出，不计算自动打出或重放。（仅储君）

- `relics:FLESH_AND_BONE_RUNE.description`
  - 原文：战斗开始时，失去[blue]3[/blue]点生命，召唤[blue]15[/blue]。战斗结束时，奥斯提每有[blue]10[/blue]最大生命值，你回复[blue]1[/blue]点生命。
  - 提议：战斗开始时，失去[blue]{HpLoss}[/blue]点生命，召唤[blue]{Summon}[/blue]。战斗结束时，奥斯提每有[blue]{OstyMaxHpPerHeal}[/blue]最大生命值，你回复[blue]{OstyHeal}[/blue]点生命。失去生命时最低保留1点；战后治疗仅在奥斯提存活时生效。（仅亡灵契约师）

- `relics:QUANTUM_COMPUTING_RUNE.description`
  - 原文：每过[blue]2[/blue]个回合，对所有敌人造成[blue]10[/blue]+其最大生命值[blue]10%[/blue]的伤害，并回复相当于所造成伤害值[blue]10%[/blue]的生命。
  - 提议：每过[blue]2[/blue]个回合，对所有敌人造成[blue]{Damage}[/blue]+其最大生命值[blue]{DamagePercent}%[/blue]的伤害，并回复相当于所造成伤害值[blue]{HealPercent}%[/blue]的生命。治疗量仅按未被格挡的伤害计算。

- `relics:BRUTALITY_RUNE.description`
  - 原文：在你的回合开始时，失去[blue]2[/blue]点生命，抽[blue]2[/blue]张牌。
  - 提议：在你的回合开始时，失去[blue]{HpLoss}[/blue]点生命，抽[blue]{Cards}[/blue]张牌。此效果最低保留1点生命。（仅战士）

- `relics:BROKEN_GOLDEN_CROWN_RUNE.description`
  - 原文：卡牌奖励少出现[blue]2[/blue]张牌。回合开始时，额外获得[blue]1[/blue]点能量。
  - 提议：卡牌奖励少出现[blue]{RewardOptionsLost}[/blue]张牌。回合开始时，额外获得[blue]{Energy}[/blue]点能量。仅影响战后卡牌奖励，至少保留1个选项。

- `relics:SELL_OFF_RUNE.description`
  - 原文：当你丢弃非[gold]奇巧[/gold]牌时，将其打出并消耗。
  - 提议：当你丢弃非[gold]奇巧[/gold]牌时，将其打出并消耗。只对攻击牌、技能牌和能力牌生效，状态牌与诅咒牌不触发。（仅猎人）

- `relics:INKSHADOW_RUNE.description`
  - 原文：你以任何形式获得的[gold]小刀[/gold]都会获得[gold]墨影[/gold]附魔。
  - 提议：你以任何形式获得的[gold]小刀[/gold]都会获得[gold]墨影[/gold]附魔。仅对尚未附魔的小刀生效。（仅猎人）

- `relics:DOUBLE_VISION_RUNE.description`
  - 原文：当你获得卡牌、金币、药水或遗物奖励时，额外获得一份相同奖励。
  - 提议：当你获得卡牌、金币、药水或遗物奖励时，额外获得一份相同奖励。不复制海克斯符文、古老牙齿、欧洛巴斯之触或黄金罗盘；属性锻造器按其奖励单独复制。

- `relics:TERMINAL_ILLNESS_RUNE.description`
  - 原文：[gold]中毒[/gold]效果触发时不再降低层数。
  - 提议：[gold]中毒[/gold]效果触发时不再降低层数。仅影响敌人身上的中毒。（仅猎人）

- `relics:SOMETHING_FOR_NOTHING_RUNE.description`
  - 原文：打出[blue]0[/blue]费牌时，抽[blue]{Cards}[/blue]张牌。每回合首次打出非[blue]0[/blue]费牌时，该牌在本场战斗中的耗能降低[blue]{Energy}[/blue]。
  - 提议：打出[blue]0[/blue]费牌时，抽[blue]{Cards}[/blue]张牌。每回合首次打出非[blue]0[/blue]费牌时，该牌在本场战斗中的耗能降低[blue]{Energy}[/blue]。X费牌不受此减费效果影响。

## B. 只换了个别用词（多为 给予/使其获得 → 施加）（13 条）

- `relic_collection:HEXTECH_FORGES_SUBCATEGORY`
  - 原文：[gold]属性锻造器：[/gold] 来自属性锻造系统的自定义遗物。
  - 提议：[gold]属性锻造器：[/gold] 来自属性锻造器的自定义遗物。

- `relics:TORMENTOR_RUNE.description`
  - 原文：每当你给予一个敌人负面效果时，使其获得[blue]4[/blue]层[gold]灼烧[/gold]。每回合最多触发[blue]1[/blue]次。
  - 提议：每当你给予一个敌人负面效果时，对其施加[blue]4[/blue]层[gold]灼烧[/gold]。每回合最多触发[blue]1[/blue]次。

- `relics:CONSTITUTION_FORGE.description`
  - 原文：战斗开始时，获得[blue]1[/blue]点力量和[blue]1[/blue]点敏捷。
  - 提议：战斗开始时，获得[blue]{StrengthPower}[/blue] [gold]力量[/gold]和[blue]{DexterityPower}[/blue] [gold]敏捷[/gold]。

- `relics:DISASTER_FORGE.description`
  - 原文：战斗开始时，使所有敌人获得[blue]1[/blue]层[gold]虚弱[/gold]和[gold]易伤[/gold]。
  - 提议：战斗开始时，对所有敌人施加[blue]{WeakPower}[/blue] [gold]虚弱[/gold]和[blue]{VulnerablePower}[/blue] [gold]易伤[/gold]。

- `relics:BADGE_BROTHERS_RUNE.description`
  - 原文：战斗开始时，获得[blue]1[/blue]层[gold]免费攻击[/gold]和[gold]免费技能[/gold]。
  - 提议：战斗开始时，获得[blue]{FreeAttackPower}[/blue] [gold]免费攻击[/gold]和[blue]{FreeSkillPower}[/blue] [gold]免费技能[/gold]。

- `relics:tormentorRune.enemyDescription`
  - 原文：每当敌人受到负面效果时，给予所有玩家[blue]1[/blue]层[gold]灼烧[/gold]。每回合最多触发[blue]3[/blue]次。
  - 提议：每当敌人受到负面效果时，对所有玩家施加[blue]1[/blue]层[gold]灼烧[/gold]。每回合最多触发[blue]3[/blue]次。

- `relics:SWORD_FLIGHT_RUNE.description`
  - 原文：每回合首次打出[gold]君王之剑[/gold]时，抽牌直到抽满手牌。
  - 提议：每回合首次手动打出[gold]君王之剑[/gold]时，抽牌至手牌达到10张。（仅储君）

- `relics:JUGGERNAUT_UPGRADE_RUNE.title`
  - 原文：升级：势不可挡
  - 提议：升级：势不可当

- `relics:JUGGERNAUT_UPGRADE_RUNE.description`
  - 原文：你的[gold]势不可挡[/gold]会攻击所有敌人。
  - 提议：你的[gold]势不可当[/gold]会攻击所有敌人。（仅战士）

- `relics:ETERNAL_ARMOR_UPGRADE_RUNE.description`
  - 原文：打出永恒铠甲后，[gold]覆甲[/gold]在你的回合开始时不再减少。
  - 提议：打出永恒铠甲后，本场战斗中你的[gold]覆甲[/gold]不再减少。

- `relics:BASH_UPGRADE_RUNE.description`
  - 原文：打出[gold]痛击[/gold]或[gold]破击[/gold]时，获得等同于给予[gold]易伤[/gold]层数的[gold]力量[/gold]。
  - 提议：打出[gold]痛击[/gold]或[gold]破击[/gold]时，获得等同于该牌基础[gold]易伤[/gold]层数的[gold]力量[/gold]。（仅战士）

- `relics:INFERNO_UPGRADE_RUNE.description`
  - 原文：你的[gold]狱火[/gold]对敌人造成伤害时，额外给予其等量的[gold]灼烧[/gold]。
  - 提议：你的[gold]狱火[/gold]对敌人造成伤害时，额外对其施加等量的[gold]灼烧[/gold]。（仅战士）

- `relics:FLAME_BARRIER_UPGRADE_RUNE.description`
  - 原文：你的[gold]火焰屏障[/gold]不再对攻击者造成伤害，改为给予其等量的[gold]灼烧[/gold]。
  - 提议：你的[gold]火焰屏障[/gold]不再对攻击者造成伤害，改为对其施加等量的[gold]灼烧[/gold]。（仅战士）

## C. 整句改写（12 条）

- `cards:FEEL_THE_BURN_CARD.description`
  - 原文：移除所有敌人的所有正面效果。
给予其[blue]{HextechBurnPower}[/blue]层[gold]灼烧[/gold]。
  - 提议：移除所有敌人的所有正面效果。
对其施加[blue]{HextechBurnPower}[/blue]层[gold]灼烧[/gold]。不会移除维持遭遇机制的效果或敌人的偷窃记录。

- `relics:RED_ENVELOPE_RUNE.description`
  - 原文：战斗胜利后，额外获得奖励：有[blue]75%[/blue]概率掉落[blue]20~50[/blue]金币，有[blue]25%[/blue]概率掉落一个随机属性锻造器。
  - 提议：战斗胜利后，额外获得一个随机属性锻造器或[blue]20~50[/blue]金币。锻造器的初始概率为[blue]{BaseForgeChance}%[/blue]；每次掉落后降低{ForgeChanceStep}个百分点，未掉落时提高{ForgeChanceStep}个百分点，范围0%~100%。

- `relics:QUEEN_RUNE.description`
  - 原文：回合开始时，使当前生命值最高的敌人获得[blue]2[/blue]层[gold]脆弱[/gold]、[gold]虚弱[/gold]和[gold]易伤[/gold]。
  - 提议：回合开始时，对当前生命值最高的敌人施加[blue]{FrailPower}[/blue] [gold]脆弱[/gold]、[blue]{WeakPower}[/blue] [gold]虚弱[/gold]和[blue]{VulnerablePower}[/blue] [gold]易伤[/gold]。

- `relics:DICE_MANIAC_RUNE.description`
  - 原文：战斗胜利后，有[blue]50%[/blue]概率掉落一个随机属性锻造器。所有随机属性锻造器的黄金和棱彩阶出现概率翻倍。
  - 提议：战斗胜利后，初始有[blue]{DropChance}%[/blue]概率掉落一个随机属性锻造器；每次掉落后概率降低{DropChanceStep}个百分点，未掉落时提高{DropChanceStep}个百分点，范围0%~100%。所有随机属性锻造器的黄金和棱彩阶抽取权重翻倍。

- `relics:brutalityRune.enemyDescription`
  - 原文：敌人在回合开始时损失[blue]5%[/blue]最大生命值，并获得[blue]1[/blue]/[blue]2[/blue]/[blue]3[/blue]点[gold]活力[/gold]。
  - 提议：敌人在回合开始时失去等同于最大生命值[blue]5%[/blue]的生命（最低保留1点），并获得[blue]1[/blue]/[blue]2[/blue]/[blue]3[/blue]点[gold]活力[/gold]。

- `relics:lightEmUpRune.enemyDescription`
  - 原文：玩家每回合打出的第四张攻击牌费用翻倍，回合结束后重置计数。
  - 提议：玩家每回合每手动打出4张攻击牌，第4张费用翻倍，回合结束后重置计数。

- `relics:pandorasBoxRune.enemyDescription`
  - 原文：玩家的卡牌奖励现在只会包含其他颜色的卡牌。
  - 提议：玩家的普通战后卡牌奖励改为其他已解锁角色的卡牌；无色奖励和固定牌池不受影响。

- `relics:CAREFUL_SELECTION_RUNE.description`
  - 原文：战后卡牌奖励变为[blue]4[/blue]选[blue]1[/blue]。
  - 提议：战后卡牌奖励不足[blue]{Cards}[/blue]个选项时，补充至最多[blue]{Cards}[/blue]个选项。

- `relics:FLYING_KICK_RUNE.description`
  - 原文：你造成的伤害会处决生命值低于目标最大生命值[blue]{ExecutePercent}[/blue]%（[blue]10[/blue]+你最大生命值的[blue]8%[/blue]）的怪物，并回复自身[blue]10%[/blue]最大生命值。
  - 提议：你对怪物造成未被格挡的伤害时，若其生命值低于最大生命值的[blue]{ExecutePercent}%[/blue]，则将其处决。你击杀或处决怪物后，回复自身最大生命值的[blue]{Heal}%[/blue]。

- `relics:SURVIVOR_UPGRADE_RUNE.description`
  - 原文：打出[gold]生存者[/gold]后，你可以选择并丢弃任意张牌。每丢弃[blue]1[/blue]张牌，获得[blue]1[/blue]次格挡。
  - 提议：打出[gold]生存者[/gold]后，你可以选择并丢弃任意张牌。一次性获得额外格挡，数值为此牌的基础格挡值乘以丢牌数量。（仅猎人）

- `relics:TWILIGHT_VEIL_RUNE.description`
  - 原文：敌人在战斗内获得增益效果时，你获得相同增益效果。
  - 提议：从首个玩家回合开始，敌人获得普通增益效果时，你获得相同效果；不复制怪物专属机制。

- `relics:SCAPEGOAT_RUNE.description`
  - 原文：回合开始时，将你身上的所有的负面状态转移给随机敌人。
  - 提议：回合开始时，移除你身上的所有负面效果，并把可转移的负面效果给予随机敌人。

## D. 数字被改成了另一个数字（4 条）

- `relics:LAGAVULIN_MATRIARCH_HEX.description`
  - 原文：每过[blue]3[/blue]/[blue]2[/blue]/[blue]1[/blue]回合，使得所有玩家失去[blue]1[/blue]点[gold]力量[/gold]和[blue]1[/blue]点[gold]敏捷[/gold]。
  - 提议：每过[blue]4[/blue]/[blue]3[/blue]/[blue]2[/blue]回合，使得所有玩家失去[blue]1[/blue]点力量和1点敏捷。

- `relics:lagavulinMatriarchHex.enemyDescription`
  - 原文：每过[blue]3[/blue]/[blue]2[/blue]/[blue]1[/blue]回合，使得所有玩家失去[blue]1[/blue]点[gold]力量[/gold]和[blue]1[/blue]点[gold]敏捷[/gold]。
  - 提议：每过[blue]4[/blue]/[blue]3[/blue]/[blue]2[/blue]回合，使得所有玩家失去[blue]1[/blue]点力量和1点敏捷。

- `relics:LEAF_SLIME_HEX.description`
  - 原文：每过[blue]3[/blue]/[blue]2[/blue]/[blue]1[/blue]回合，将[blue]1[/blue]张[gold]黏液[/gold]加入玩家的弃牌堆。
  - 提议：每过[blue]4[/blue]/[blue]3[/blue]/[blue]2[/blue]回合，将[blue]1[/blue]张[gold]黏液[/gold]加入玩家的弃牌堆。

- `relics:leafSlimeHex.enemyDescription`
  - 原文：每过[blue]3[/blue]/[blue]2[/blue]/[blue]1[/blue]回合，将[blue]1[/blue]张[gold]黏液[/gold]加入玩家的弃牌堆。
  - 提议：每过[blue]4[/blue]/[blue]3[/blue]/[blue]2[/blue]回合，将[blue]1[/blue]张[gold]黏液[/gold]加入玩家的弃牌堆。
