using System.Reflection;
using System.Runtime.CompilerServices;
using HextechRunesSponsorPack;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace HextechRunes.Tests;

internal static partial class Program
{
	private static void RepeatableEnchantmentsRequireCurrentlyOwnedEnchantmentMasterRune()
	{
		Player player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
		List<RelicModel> relics = [];
		typeof(Player)
			.GetField("_relics", BindingFlags.Instance | BindingFlags.NonPublic)!
			.SetValue(player, relics);

		Expect(!RepeatableEnchantmentAccessPolicy.IsEnabledFor(player), "repeatable enchantments must be disabled without EnchantmentMasterRune");
		relics.Add((EnchantmentMasterRune)RuntimeHelpers.GetUninitializedObject(typeof(EnchantmentMasterRune)));
		Expect(RepeatableEnchantmentAccessPolicy.IsEnabledFor(player), "repeatable enchantments must be enabled while EnchantmentMasterRune is owned");
		relics.Clear();
		Expect(!RepeatableEnchantmentAccessPolicy.IsEnabledFor(player), "repeatable enchantments must be disabled immediately after EnchantmentMasterRune is removed");
	}

	private static void EnchantmentCompositionAdapterFindsDirectEnchantments()
	{
		EnchantmentModel enchantment = (Sharp)RuntimeHelpers.GetUninitializedObject(typeof(Sharp));
		Equal(enchantment, EnchantmentCompositionAdapter.Find(enchantment, typeof(Sharp)), "direct enchantment lookup");
		Expect(EnchantmentCompositionAdapter.Contains(enchantment, typeof(Sharp)), "direct enchantment should be contained");
		Expect(!EnchantmentCompositionAdapter.Contains(enchantment, typeof(Sown)), "different enchantment type should not be contained");
	}

	private static void EnchantmentCompositionAdapterFindsSponsorCompositeEnchantments()
	{
		EnchantmentModel sharp = (Sharp)RuntimeHelpers.GetUninitializedObject(typeof(Sharp));
		SponsorCompositeEnchantment composite = (SponsorCompositeEnchantment)RuntimeHelpers.GetUninitializedObject(typeof(SponsorCompositeEnchantment));
		typeof(SponsorCompositeEnchantment)
			.GetField("_innerEnchantments", BindingFlags.Instance | BindingFlags.NonPublic)!
			.SetValue(composite, new List<EnchantmentModel> { sharp });
		typeof(SponsorCompositeEnchantment)
			.GetField("_subscribedInnerEnchantments", BindingFlags.Instance | BindingFlags.NonPublic)!
			.SetValue(composite, new List<EnchantmentModel>());

		Equal(sharp, EnchantmentCompositionAdapter.Find(composite, typeof(Sharp)), "sponsor composite lookup");
		Expect(EnchantmentCompositionAdapter.Contains(composite, typeof(Sharp)), "sponsor composite should contain its inner enchantment");
		Expect(!EnchantmentCompositionAdapter.Contains(composite, typeof(Sown)), "sponsor composite should reject missing enchantment types");
	}
}
