using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

public sealed class ReanimateUpgradeRune : CardUpgradeRuneBase<Reanimate>
{
	private int _deathsThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedDeathsThisCombat
	{
		get => _deathsThisCombat;
		set => _deathsThisCombat = Math.Max(0, value);
	}

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		_deathsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_deathsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (!wasRemovalPrevented && HextechMonsterInteractionPolicy.IsTrueCombatDeath(target))
		{
			_deathsThisCombat++;
		}

		return Task.CompletedTask;
	}

	public override decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source)
	{
		if (summoner != Owner || source is not Reanimate || _deathsThisCombat <= 0)
		{
			return amount;
		}

		Flash();
		return amount + _deathsThisCombat * 5m;
	}
}
