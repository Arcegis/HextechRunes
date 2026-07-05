using MegaCrit.Sts2.Core.Localization;

namespace HextechRunes;

public sealed class SymphonyOfWarRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<SerpentFormPower>(4m),
		new PowerVar<DemonFormPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		PowerPreview<SerpentFormPower>("HEXTECH_RUNE_SERPENT_FORM_PREVIEW.description"),
		PowerPreview<DemonFormPower>("HEXTECH_RUNE_DEMON_FORM_PREVIEW.description")
	];

	private static IHoverTip PowerPreview<TPower>(string descriptionKey)
		where TPower : PowerModel
	{
		PowerModel power = ModelDb.Power<TPower>();
		return new HoverTip(power, new LocString("powers", descriptionKey).GetFormattedText(), true);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<SerpentFormPower>(Owner.Creature, DynamicVars["SerpentFormPower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<DemonFormPower>(Owner.Creature, DynamicVars["DemonFormPower"].BaseValue, Owner.Creature, null);
	}
}
