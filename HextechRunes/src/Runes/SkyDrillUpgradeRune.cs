using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class SkyDrillUpgradeRune : CardUpgradeRuneBase<HeavenlyDrill>
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("XThreshold", 6m),
		new DynamicVar("XMultiplier", 3m)
	];

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not HeavenlyDrill card
			|| cardPlay.Target is not Creature target)
		{
			return;
		}

		int energyX = (int)HextechCombatHooks.GetEnergyCostForCurrentCardPlay(card);
		if (energyX < DynamicVars["XThreshold"].IntValue || target.IsDead)
		{
			return;
		}

		int baseMultiplier = energyX >= card.DynamicVars.Energy.IntValue ? 2 : 1;
		int extraHits = energyX * (DynamicVars["XMultiplier"].IntValue - baseMultiplier);
		if (extraHits <= 0)
		{
			return;
		}

		Flash();
		await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
			.WithHitCount(extraHits)
			.FromCardCompat(card)
			.Targeting(target)
			.WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3")
			.Execute(context);
	}
}
