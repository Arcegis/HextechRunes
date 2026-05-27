using MegaCrit.Sts2.Core.Map;

namespace HextechRunes;

internal static class HextechMapLengthReducer
{
	internal static ActMap ReduceNodeLengthByOne(ActMap map, MapCoord? currentCoord)
	{
		int rowToRemove = currentCoord.HasValue ? Math.Max(1, currentCoord.Value.row + 1) : 1;
		if (map is ShortenedActMap || rowToRemove >= map.GetRowCount())
		{
			return map;
		}

		return new ShortenedActMap(map, rowToRemove);
	}

	private sealed class ShortenedActMap : ActMap
	{
		public override MapPoint BossMapPoint { get; }

		public override MapPoint StartingMapPoint { get; }

		public override MapPoint? SecondBossMapPoint { get; }

		protected override MapPoint?[,] Grid { get; }

		private readonly int _rowToRemove;

		public ShortenedActMap(ActMap original, int rowToRemove)
		{
			_rowToRemove = rowToRemove;
			Grid = new MapPoint[original.GetColumnCount(), original.GetRowCount() - 1];
			var cloneLookup = new Dictionary<MapCoord, MapPoint>();

			foreach (MapPoint originalPoint in original.GetAllMapPoints())
			{
				if (originalPoint.coord.row == _rowToRemove)
				{
					continue;
				}

				MapPoint clone = ClonePoint(originalPoint, ShiftedRow(originalPoint.coord.row));
				Grid[clone.coord.col, clone.coord.row] = clone;
				cloneLookup[originalPoint.coord] = clone;
			}

			StartingMapPoint = ClonePoint(original.StartingMapPoint, original.StartingMapPoint.coord.row);
			BossMapPoint = ClonePoint(original.BossMapPoint, ShiftedRow(original.BossMapPoint.coord.row));
			SecondBossMapPoint = original.SecondBossMapPoint == null
				? null
				: ClonePoint(original.SecondBossMapPoint, ShiftedRow(original.SecondBossMapPoint.coord.row));

			foreach (MapPoint originalPoint in original.GetAllMapPoints())
			{
				if (!cloneLookup.TryGetValue(originalPoint.coord, out MapPoint? clonedPoint))
				{
					continue;
				}

				foreach (MapPoint child in ResolveChildTargets(original, cloneLookup, originalPoint.Children))
				{
					AddChildOnce(clonedPoint, child);
				}
			}

			foreach (MapPoint child in ResolveChildTargets(original, cloneLookup, original.StartingMapPoint.Children))
			{
				AddChildOnce(StartingMapPoint, child);
				if (!startMapPoints.Contains(child))
				{
					startMapPoints.Add(child);
				}
			}

			if (original.SecondBossMapPoint != null && SecondBossMapPoint != null)
			{
				AddChildOnce(BossMapPoint, SecondBossMapPoint);
			}
		}

		private IEnumerable<MapPoint> ResolveChildTargets(ActMap original, Dictionary<MapCoord, MapPoint> cloneLookup, IEnumerable<MapPoint> children)
		{
			foreach (MapPoint child in children)
			{
				if (child == original.BossMapPoint)
				{
					yield return BossMapPoint;
					continue;
				}

				if (child == original.SecondBossMapPoint)
				{
					if (SecondBossMapPoint != null)
					{
						yield return SecondBossMapPoint;
					}
					continue;
				}

				if (child.coord.row == _rowToRemove)
				{
					foreach (MapPoint grandChild in ResolveChildTargets(original, cloneLookup, child.Children))
					{
						yield return grandChild;
					}
					continue;
				}

				if (cloneLookup.TryGetValue(child.coord, out MapPoint? clonedChild))
				{
					yield return clonedChild;
				}
			}
		}

		private int ShiftedRow(int row)
		{
			return row > _rowToRemove ? row - 1 : row;
		}

		private static MapPoint ClonePoint(MapPoint original, int row)
		{
			var clone = new MapPoint(original.coord.col, row)
			{
				PointType = original.PointType,
				CanBeModified = original.CanBeModified
			};
			foreach (var quest in original.Quests)
			{
				clone.AddQuest(quest);
			}

			return clone;
		}

		private static void AddChildOnce(MapPoint parent, MapPoint child)
		{
			if (!parent.Children.Contains(child))
			{
				parent.AddChildPoint(child);
			}
		}
	}
}
