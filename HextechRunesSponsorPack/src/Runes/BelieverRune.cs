using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using SponsorModInfo = HextechRunesSponsorPack.ModInfo;

namespace HextechRunes;

// 信徒(棱彩,仅单人):击败 BOSS+6 / 精英+3 / 小怪+1 充能,满 6 立即触发一次「神迹」事件并扣除 6;
// 激活(获得)时也立即触发一次神迹。神迹是程序化进入的自定义事件(见 MiracleEvent)。
public sealed class BelieverRune : HextechRelicBase
{
	private const int ChargeThreshold = 6;

	private int _charge;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCharge
	{
		get => _charge;
		set
		{
			_charge = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => true;

	public override int DisplayAmount => !IsCanonical ? _charge : 0;

	// 本局锻造器售价的临时修正(可正可负),由神迹事件的选择累加;逐局持久。
	// 由 MiracleEventForgePricePatch 在主 mod 算价后叠加(纯拓展包,不动主 mod)。
	private int _forgePriceDelta;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedForgePriceDelta
	{
		get => _forgePriceDelta;
		set => _forgePriceDelta = value;
	}

	internal int ForgePriceDelta => _forgePriceDelta;

	internal void AddForgePriceDelta(int delta)
	{
		SavedForgePriceDelta = _forgePriceDelta + delta;
		Log.Info($"[{SponsorModInfo.Id}] BelieverRune forge-price delta {(delta >= 0 ? "+" : "")}{delta} -> total {_forgePriceDelta}.");
	}

	// 仅单人游戏出现。
	public override bool IsAvailableForPlayer(Player player)
	{
		return !IsNetworkMultiplayerRun();
	}

	// 激活(获得)时立即触发一次神迹。
	public override async Task AfterObtained()
	{
		await TriggerMiracle();
	}

	public override async Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead || IsNetworkMultiplayerRun())
		{
			return;
		}

		int gain = room.RoomType switch
		{
			RoomType.Boss => 6,
			RoomType.Elite => 3,
			_ => 1
		};

		SavedCharge = _charge + gain;
		Flash(Array.Empty<Creature>());

		if (_charge >= ChargeThreshold)
		{
			SavedCharge = _charge - ChargeThreshold;
			await TriggerMiracle();
		}
	}

	private async Task TriggerMiracle()
	{
		if (Owner == null || IsNetworkMultiplayerRun())
		{
			return;
		}

		try
		{
			// 直接传 canonical 事件实例;不要 ToMutable()(EventRoom 内部自行处理,
			// 否则报 MutableModelException: used in incorrect place)。
			EventModel miracle = ModelDb.Event<MiracleEvent>();
			await RunManager.Instance.EnterRoom(new EventRoom(miracle));
		}
		catch (Exception ex)
		{
			// 中途进入事件房间是已知风险点(战斗后/拾取流程)。失败不得让海克斯崩掉本局,记录后吞掉。
			Log.Warn($"[{SponsorModInfo.Id}] BelieverRune failed to trigger Miracle event: {ex.GetType().Name}: {ex.Message}", 2);
		}
	}
}
