using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes.Tests;

internal static partial class Program
{
	[HextechTest]
	private static void ScissorsSteadyAndSilverRecoveryForgesAreRegistered()
	{
		(Type Type, HextechRarityTier Rarity)[] expected =
		[
			(typeof(ScissorsForge), HextechRarityTier.Silver),
			(typeof(GoldScissorsForge), HextechRarityTier.Gold),
			(typeof(SteadyForge), HextechRarityTier.Silver),
			(typeof(SilverRecoveryForge), HextechRarityTier.Silver)
		];
		foreach ((Type type, HextechRarityTier rarity) in expected)
		{
			Equal(rarity, HextechForgeRegistry.Registrations.Single(row => row.Type == type).Rarity, type.Name + " rarity");
		}

		Equal(1, CreateMutableTestModel<ScissorsForge>().DynamicVars.Cards.IntValue, "silver scissors removes one card");
		Equal(2, CreateMutableTestModel<GoldScissorsForge>().DynamicVars.Cards.IntValue, "gold scissors removes two cards");
		Expect(CreateMutableTestModel<ScissorsForge>().HasUponPickupEffect, "scissors acts on pickup");
		Expect(CreateMutableTestModel<SteadyForge>().HasUponPickupEffect, "steady forge enchants on pickup");
		Equal(2m, CreateMutableTestModel<SilverRecoveryForge>().DynamicVars.Heal.BaseValue, "silver recovery heals two");
		Expect(ModelDb.GetId<SilverRecoveryForge>() != ModelDb.GetId<RecoveryForge>(), "silver and gold recovery forges stack separately");
	}
}
