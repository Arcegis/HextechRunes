namespace HextechRunes;

internal static class HextechModelIdExtensions
{
	/// <summary>取规范模型的 ID；可变副本与规范模型同 ID，规范实例缺失时退回自身 ID。</summary>
	internal static ModelId CanonicalId(this RelicModel relic)
	{
		return relic.CanonicalInstance?.Id ?? relic.Id;
	}

	/// <inheritdoc cref="CanonicalId(RelicModel)"/>
	internal static ModelId CanonicalId(this CardModel card)
	{
		return card.CanonicalInstance?.Id ?? card.Id;
	}

	/// <inheritdoc cref="CanonicalId(RelicModel)"/>
	internal static ModelId CanonicalId(this PotionModel potion)
	{
		return potion.CanonicalInstance?.Id ?? potion.Id;
	}

	/// <inheritdoc cref="CanonicalId(RelicModel)"/>
	internal static ModelId CanonicalId(this EnchantmentModel enchantment)
	{
		return enchantment.CanonicalInstance?.Id ?? enchantment.Id;
	}
}
