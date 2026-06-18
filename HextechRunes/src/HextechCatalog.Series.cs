using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Exceptions;

namespace HextechRunes;

internal static partial class HextechCatalog
{
	private readonly record struct IndexedRuneType(Type Type, int Index);
	private static readonly HashSet<Type> MissingVisibleCustomRelicLogs = [];

	public static IReadOnlyList<RelicModel> GetCanonicalRunes()
	{
		return AllRuneTypes
			.Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
			.ToArray();
	}

	public static IReadOnlyList<RelicModel> GetCanonicalSelectableRunes()
	{
		return GetAllSelectableRuneTypes()
			.Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
			.ToArray();
	}

	public static IReadOnlyList<RelicModel> GetCanonicalGenericSelectableRunes()
	{
		return GetGenericSelectableRuneTypes()
			.Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
			.ToArray();
	}

	public static IReadOnlyList<RelicModel> GetCanonicalGenericVisibleRunes()
	{
		return GetGenericVisibleRuneTypes()
			.Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
			.ToArray();
	}

	public static IReadOnlyList<RuneSeriesGroup> GetCharacterRuneGroups()
	{
		return CharacterRunePools
			.Select(static pool => new RuneSeriesGroup(
				$"CHARACTER.{pool.LocalizationKey}",
				pool.RuneTypes
					.Select(static (type, index) => new IndexedRuneType(type, index))
					.Where(static rune => IsPlayerRuneTypeVisibleInCollection(rune.Type))
					.OrderBy(static rune => GetPlayerRuneRaritySortOrder(rune.Type))
					.ThenBy(static rune => rune.Index)
					.Select(static rune => ModelDb.GetById<RelicModel>(ModelDb.GetId(rune.Type)))
					.ToArray()))
			.Where(static group => group.Relics.Count > 0)
			.ToArray();
	}

	public static IReadOnlyList<RelicModel> GetCanonicalForges()
	{
		return AllForgeTypes
			.Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
			.ToArray();
	}

	public static IReadOnlyList<RelicModel> GetCanonicalCustomRelics()
	{
		return AllCustomRelicTypes
			.Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
			.ToArray();
	}

	public static IReadOnlyList<RelicModel> GetCanonicalVisibleCustomRelics()
	{
		return AllCustomRelicTypes
			.Where(static type => !AllRuneTypes.Contains(type) || IsPlayerRuneTypeVisibleInCollection(type))
			.Select(static type => TryGetCanonicalVisibleCustomRelic(type, out RelicModel? relic) ? relic : null)
			.OfType<RelicModel>()
			.ToArray();
	}

	private static bool TryGetCanonicalVisibleCustomRelic(Type type, out RelicModel? relic)
	{
		ModelId id = ModelDb.GetId(type);
		try
		{
			relic = ModelDb.GetById<RelicModel>(id);
			return true;
		}
		catch (ModelNotFoundException ex)
		{
			relic = null;
			lock (MissingVisibleCustomRelicLogs)
			{
				if (MissingVisibleCustomRelicLogs.Add(type))
				{
					Log.Warn($"[{ModInfo.Id}] Skipping missing visible custom relic during inspect list build: type={type.FullName} id={id.Entry}: {ex.Message}");
				}
			}

			return false;
		}
	}

	public static IReadOnlyList<RuneSeriesGroup> GetRuneSeriesGroups(IReadOnlyList<RelicModel> relics)
	{
		Dictionary<ModelId, RelicModel> byId = relics.ToDictionary(static relic => relic.CanonicalInstance?.Id ?? relic.Id);

		IReadOnlyList<RelicModel> BuildGroup(IEnumerable<Type> runeTypes)
		{
			List<RelicModel> group = new();
			foreach (Type runeType in runeTypes)
			{
				if (!IsPlayerRuneTypeVisibleInCollection(runeType))
				{
					continue;
				}

				ModelId id = ModelDb.GetId(runeType);
				if (byId.TryGetValue(id, out RelicModel? relic))
				{
					group.Add(relic);
				}
			}

			return group;
		}

		return
		[
			new RuneSeriesGroup("SILVER", BuildGroup(SilverRuneTypes)),
			new RuneSeriesGroup("GOLD", BuildGroup(GoldRuneTypes)),
			new RuneSeriesGroup("PRISMATIC", BuildGroup(PrismaticRuneTypes))
		];
	}

	public static IReadOnlyList<RuneSeriesGroup> GetForgeSeriesGroups()
	{
		IReadOnlyList<RelicModel> relics = GetCanonicalForges();
		Dictionary<ModelId, RelicModel> byId = relics.ToDictionary(static relic => relic.CanonicalInstance?.Id ?? relic.Id);

		IReadOnlyList<RelicModel> BuildGroup(IEnumerable<Type> forgeTypes)
		{
			List<RelicModel> group = new();
			foreach (Type forgeType in forgeTypes)
			{
				ModelId id = ModelDb.GetId(forgeType);
				if (byId.TryGetValue(id, out RelicModel? relic))
				{
					group.Add(relic);
				}
			}

			return group;
		}

		return
		[
			new RuneSeriesGroup("SILVER", BuildGroup(SilverForgeTypes)),
			new RuneSeriesGroup("GOLD", BuildGroup(GoldForgeTypes)),
			new RuneSeriesGroup("PRISMATIC", BuildGroup(PrismaticForgeTypes))
		];
	}

	private static int GetPlayerRuneRaritySortOrder(Type type)
	{
		return PlayerRuneMetadata.GetRaritySortOrder(type);
	}
}
