namespace HextechRunes;

public sealed class OrbSymbiosisRune : HextechRelicBase
{
	private bool _duplicatingOrb;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("OrbCount", 1m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb)
	{
		if (_duplicatingOrb || player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		_duplicatingOrb = true;
		try
		{
			for (int i = 0; i < DynamicVars["OrbCount"].IntValue; i++)
			{
				OrbModel duplicate = ModelDb.GetById<OrbModel>(orb.Id).ToMutable();
				await OrbCmd.Channel(choiceContext, duplicate, Owner);
			}
		}
		finally
		{
			_duplicatingOrb = false;
		}
	}
}
