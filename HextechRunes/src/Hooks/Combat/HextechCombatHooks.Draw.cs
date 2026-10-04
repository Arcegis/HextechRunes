namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Draw), typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool))]
	[HextechPatch("combat.draw", "卡牌检视")]
	private static class DrawPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw, ref Task<IEnumerable<CardModel>> __result)
		{
			CardInspectionRune? cardInspectionRune = player.GetRelic<CardInspectionRune>();
			if (cardInspectionRune != null && fromHandDraw && count > 0m && player.Creature.CombatState != null)
			{
				cardInspectionRune.Flash();
				__result = HextechSelectedDrawHelper.DrawSelectedFromDrawPile(
					choiceContext,
					player,
					(int)Math.Ceiling(count),
					fromHandDraw: true);
				return false;
			}

			return true;
		}
	}
}
