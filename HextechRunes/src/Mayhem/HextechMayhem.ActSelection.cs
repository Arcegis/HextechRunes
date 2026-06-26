using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public override async Task AfterActEntered()
	{
		int actIndex = RunState.CurrentActIndex;
		if (!IsActResolved(actIndex) && TryRecoverResolvedActsFromPlayerRelics(nameof(AfterActEntered)))
		{
			HextechEnemyUi.Refresh(this);
		}

		if (actIndex <= 0 || actIndex > 2 || IsActResolved(actIndex))
		{
			return;
		}

		if (ShouldDeferImmediateActSelection(RunState.CurrentRoom))
		{
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] AfterActEntered: deferring act selection until room/event flow is stable actIndex={actIndex} currentRoom={RunState.CurrentRoom?.GetType().Name ?? "null"}");
			return;
		}

		HextechLog.Info($"[{ModInfo.Id}][Mayhem] AfterActEntered: resolving act selection before first room actIndex={actIndex}");
		await HextechRuneSelectionCoordinator.HandleActSelection(RunState, this);
	}

	public override async Task BeforeRoomEntered(AbstractRoom room)
	{
		int actIndex = RunState.CurrentActIndex;
		if (!IsActResolved(actIndex) && TryRecoverResolvedActsFromPlayerRelics(nameof(BeforeRoomEntered)))
		{
			HextechEnemyUi.Refresh(this);
		}

		if (actIndex < 0 || actIndex > 2 || IsActResolved(actIndex) || room is EventRoom or MapRoom)
		{
			return;
		}

		if (actIndex == 0)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] BeforeRoomEntered: skipping unsafe act0 selection before room={room.GetType().Name}; waiting for post-Neow or map path");
			return;
		}

		HextechLog.Info($"[{ModInfo.Id}][Mayhem] BeforeRoomEntered: resolving pending act selection before room={room.GetType().Name} actIndex={actIndex}");
		await HextechRuneSelectionCoordinator.HandleActSelection(RunState, this);
	}

	private static bool ShouldDeferImmediateActSelection(AbstractRoom? currentRoom)
	{
		return currentRoom == null
			|| currentRoom is EventRoom { CanonicalEvent: AncientEventModel };
	}
}
