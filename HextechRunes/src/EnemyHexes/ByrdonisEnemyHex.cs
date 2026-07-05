using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace HextechRunes;

internal sealed class ByrdonisEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Byrdonis;

	internal override async Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		int amount = context.TierValue(Kind, 0, 1, 2);
		if (amount <= 0 || !enemy.IsAlive)
		{
			return;
		}

		await PowerCmd.Apply<TerritorialPower>(enemy, amount, enemy, null);
	}
}
