using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class SearingAttackCard : CardModel
{
	public override CardPoolModel Pool => IsMutable && Owner != null
		? Owner.Character.CardPool
		: ModelDb.CardPool<TokenCardPool>();

	public override CardPoolModel VisualCardPool => Pool;

	public override string PortraitPath => HextechAssets.SearingAttackCardPortraitPath;

	public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

	public override int MaxUpgradeLevel => 999;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(12m, ValueProp.Move)
	];

	public SearingAttackCard()
		: base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, shouldShowInCardLibrary: true)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return cardPlay.Target == null
			? Task.CompletedTask
			: CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		decimal previousDamage = DamageForUpgradeLevel(CurrentUpgradeLevel - 1);
		decimal targetDamage = DamageForUpgradeLevel(CurrentUpgradeLevel);
		DynamicVars.Damage.UpgradeValueBy(targetDamage - previousDamage);
	}

	private static decimal DamageForUpgradeLevel(int upgradeLevel)
	{
		return upgradeLevel <= 0
			? 12m
			: upgradeLevel * (upgradeLevel + 7m) / 2m + 12m;
	}
}
