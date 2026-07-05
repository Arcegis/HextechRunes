namespace HextechRunes;

public sealed class GloomyCloudsRune : HextechRelicBase
{
	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner?.PlayerCombatState == null || Owner.Creature.IsDead)
		{
			return;
		}

		List<DarkOrb> darkOrbs = Owner.PlayerCombatState.OrbQueue.Orbs
			.OfType<DarkOrb>()
			.ToList();
		if (darkOrbs.Count == 0)
		{
			return;
		}

		Flash();
		foreach (DarkOrb orb in darkOrbs)
		{
			await orb.Passive(choiceContext, null);
		}
	}
}
