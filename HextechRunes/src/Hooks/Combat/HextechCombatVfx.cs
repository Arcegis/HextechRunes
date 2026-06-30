using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

/// <summary>
/// 事件驱动的战斗特效派发器。符文在其触发点(战斗逻辑、各端一致执行)调用这里的方法,
/// 由本类把可视节点延迟挂到对应 <see cref="NCreature"/> 上。纯表现层:只新建可视节点、不读写任何
/// gameplay/同步状态;取不到节点时安全跳过。<see cref="HextechCreatureNodeRegistry"/> 提供 entity→node 桥。
/// </summary>
internal static class HextechCombatVfxHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(NCombatRoom), "_Ready", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechCombatVfxHooks), nameof(CombatRoomReadyPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature), BindingFlags.Instance | BindingFlags.Public, typeof(Creature)),
			postfix: new HarmonyMethod(typeof(HextechCombatVfxHooks), nameof(AddCreaturePostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCreature), "_Ready", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechCombatVfxHooks), nameof(CreatureReadyPostfix)));
		HextechLog.Info($"[{ModInfo.Id}][CombatVfx] Hooks installed.");
	}

	private static void CombatRoomReadyPostfix(NCombatRoom __instance)
	{
		foreach (NCreature creature in __instance.CreatureNodes)
		{
			HextechCreatureNodeRegistry.Register(creature);
		}
	}

	private static void AddCreaturePostfix(NCombatRoom __instance, Creature creature)
	{
		HextechCreatureNodeRegistry.Register(__instance.GetCreatureNode(creature));
	}

	private static void CreatureReadyPostfix(NCreature __instance)
	{
		HextechCreatureNodeRegistry.Register(__instance);
	}
}

/// <summary>entity → 屏幕节点 的弱映射,由战斗节点生命周期 hook 填充;取用时校验有效性、惰性清理失效项。</summary>
internal static class HextechCreatureNodeRegistry
{
	private static readonly Dictionary<Creature, NCreature> Nodes = new();

	internal static void Register(NCreature? node)
	{
		if (!GodotObject.IsInstanceValid(node) || node!.Entity == null)
		{
			return;
		}

		Nodes[node.Entity] = node;
		if (Nodes.Count > 24)
		{
			Prune();
		}
	}

	internal static NCreature? TryGet(Creature? creature)
	{
		if (creature != null && Nodes.TryGetValue(creature, out NCreature? node) && GodotObject.IsInstanceValid(node))
		{
			return node;
		}

		return null;
	}

	private static void Prune()
	{
		List<Creature> stale = [];
		foreach (KeyValuePair<Creature, NCreature> pair in Nodes)
		{
			if (!GodotObject.IsInstanceValid(pair.Value))
			{
				stale.Add(pair.Key);
			}
		}

		foreach (Creature key in stale)
		{
			Nodes.Remove(key);
		}
	}
}

internal static class HextechCombatVfx
{
	// 死亡之环改用 LoL 卡尔萨斯式幽绿光环色调(原血色已弃用)。
	private static readonly Color DeathRingColor = new(0.24f, 0.96f, 0.45f);
	private static readonly Color DeathFlashColor = new(0.62f, 1f, 0.64f);
	private static readonly Color DivineRingColor = new(1f, 0.9f, 0.55f);
	private static readonly Color DivineFlashColor = new(1f, 0.97f, 0.82f);
	// 吞噬灵魂:幽青色亡魂。
	private static readonly Color SoulColor = new(0.42f, 0.95f, 0.82f);
	private static readonly Color SoulCoreColor = new(0.78f, 1f, 0.95f);

	private static Texture2D? _glowTexture;
	private static Texture2D? _ringTexture;

	/// <summary>死亡之环:从施法者甩向目标的血色光束 + 目标身上炸开的死亡环与闪光。</summary>
	internal static void DeathRingLash(Creature source, Creature target)
	{
		Callable.From(() => RunDeathRingLash(source, target)).CallDeferred();
	}

	/// <summary>神圣干预:为每个受益玩家罩上一圈金色圣光脉冲与柔光。</summary>
	internal static void DivinePulse(IReadOnlyList<Creature> allies)
	{
		Creature[] snapshot = [.. allies];
		Callable.From(() => RunDivinePulse(snapshot)).CallDeferred();
	}

	/// <summary>吞噬灵魂:幽青色亡魂从死亡的敌人身上被抽离、飘向并汇入施法者。</summary>
	internal static void SoulDrain(Creature source, Creature destination)
	{
		Callable.From(() => RunSoulDrain(source, destination)).CallDeferred();
	}

	private static void RunDeathRingLash(Creature source, Creature target)
	{
		try
		{
			NCreature? targetNode = HextechCreatureNodeRegistry.TryGet(target);
			if (targetNode == null)
			{
				return;
			}

			Node? parent = targetNode.GetParent();
			if (!GodotObject.IsInstanceValid(parent))
			{
				return;
			}

			Vector2 targetPos = CreatureCenter(targetNode);
			float width = CreatureWidth(targetNode);
			// 敌人受击爆发刻意做得比我方(施法侧)的环更小一点 —— 它只是被动触发的命中,不该盖过你自身的攻击表现。
			SpawnFlash(parent!, targetPos, width * 0.75f, DeathFlashColor, 0.40f, 0.40f);
			SpawnRing(parent!, targetPos, width * 0.3f, width * 0.8f, 0.45f, 0.95f, DeathRingColor);

			NCreature? sourceNode = HextechCreatureNodeRegistry.TryGet(source);
			if (sourceNode != null)
			{
				Vector2 sourcePos = CreatureCenter(sourceNode);
				SpawnBeam(parent!, sourcePos, targetPos, DeathRingColor, 0.30f);
				SpawnRing(parent!, sourcePos, width * 0.25f, width * 0.95f, 0.30f, 0.55f, DeathRingColor);
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][CombatVfx] Death ring lash failed: {ex.Message}");
		}
	}

	private static void RunDivinePulse(Creature[] allies)
	{
		try
		{
			foreach (Creature ally in allies)
			{
				NCreature? node = HextechCreatureNodeRegistry.TryGet(ally);
				if (node == null)
				{
					continue;
				}

				Node? parent = node.GetParent();
				if (!GodotObject.IsInstanceValid(parent))
				{
					continue;
				}

				Vector2 pos = CreatureCenter(node);
				float width = CreatureWidth(node);
				SpawnFlash(parent!, pos, width * 1.8f, DivineFlashColor, 0.34f, 0.55f);
				SpawnRing(parent!, pos, width * 0.4f, width * 1.7f, 0.55f, 0.85f, DivineRingColor);
				SpawnRing(parent!, pos, width * 0.4f, width * 1.25f, 0.70f, 0.55f, DivineRingColor);
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][CombatVfx] Divine pulse failed: {ex.Message}");
		}
	}

	private static void RunSoulDrain(Creature source, Creature destination)
	{
		try
		{
			NCreature? destNode = HextechCreatureNodeRegistry.TryGet(destination);
			if (destNode == null)
			{
				return;
			}

			Node? parent = destNode.GetParent();
			if (!GodotObject.IsInstanceValid(parent))
			{
				return;
			}

			Vector2 destPos = CreatureCenter(destNode);
			float width = CreatureWidth(destNode);
			NCreature? sourceNode = HextechCreatureNodeRegistry.TryGet(source);
			Vector2 sourcePos = sourceNode != null ? CreatureCenter(sourceNode) : destPos;

			if (sourceNode != null)
			{
				// 亡魂自敌人身上被抽离的一瞬。
				SpawnFlash(parent!, sourcePos, width * 0.85f, SoulCoreColor, 0.35f, 0.5f);
			}

			// 主魂飘入你身上,到达时一记吸收闪光与圈;再添两缕错峰拖尾。
			SpawnSoulWisp(parent!, sourcePos, destPos, width * 0.52f, 0.55f, 0f, SoulColor, () =>
			{
				if (GodotObject.IsInstanceValid(parent))
				{
					SpawnFlash(parent!, destPos, width * 1.0f, SoulCoreColor, 0.35f, 0.5f);
					SpawnRing(parent!, destPos, width * 0.2f, width * 1.05f, 0.4f, 0.8f, SoulColor);
				}
			});
			SpawnSoulWisp(parent!, sourcePos, destPos, width * 0.32f, 0.6f, 0.07f, SoulCoreColor, null);
			SpawnSoulWisp(parent!, sourcePos, destPos, width * 0.28f, 0.62f, 0.13f, SoulColor, null);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][CombatVfx] Soul drain failed: {ex.Message}");
		}
	}

	private static void SpawnSoulWisp(Node parent, Vector2 from, Vector2 to, float diameter, float duration, float delay, Color color, Action? onArrival)
	{
		Sprite2D wisp = MakeSprite(GetGlowTexture(), color with { A = 0.95f });
		wisp.TopLevel = true;
		parent.AddChildSafely(wisp);
		wisp.GlobalPosition = from;
		SetSpriteDiameter(wisp, diameter);

		Tween tween = wisp.CreateTween();
		if (delay > 0f)
		{
			tween.TweenInterval(delay);
		}

		tween.SetParallel(true);
		tween.TweenProperty(wisp, "global_position", to, duration)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(wisp, "modulate:a", 0f, duration).SetEase(Tween.EaseType.In);
		tween.Chain().TweenCallback(Callable.From(() =>
		{
			onArrival?.Invoke();
			FreeNode(wisp);
		}));
	}

	private static Vector2 CreatureCenter(NCreature node)
	{
		return node.GetTopOfHitbox().Lerp(node.GetBottomOfHitbox(), 0.5f);
	}

	private static float CreatureWidth(NCreature node)
	{
		return Mathf.Clamp(node.Hitbox?.Size.X ?? 180f, 120f, 360f);
	}

	private static void SpawnRing(Node parent, Vector2 globalPos, float startDiameter, float endDiameter, float duration, float startAlpha, Color color)
	{
		Sprite2D ring = MakeSprite(GetRingTexture(), color with { A = startAlpha });
		parent.AddChildSafely(ring);
		ring.GlobalPosition = globalPos;
		SetSpriteDiameter(ring, startDiameter);
		float endScale = endDiameter / Math.Max(GetRingTexture().GetWidth(), 1);

		Tween tween = ring.CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(ring, "scale", new Vector2(endScale, endScale), duration)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(ring, "modulate:a", 0f, duration).SetEase(Tween.EaseType.In);
		tween.Chain().TweenCallback(Callable.From(() => FreeNode(ring)));
	}

	private static void SpawnFlash(Node parent, Vector2 globalPos, float diameter, Color color, float duration, float peakAlpha)
	{
		Sprite2D flash = MakeSprite(GetGlowTexture(), color with { A = 0f });
		parent.AddChildSafely(flash);
		flash.GlobalPosition = globalPos;
		SetSpriteDiameter(flash, diameter);

		Tween tween = flash.CreateTween();
		tween.TweenProperty(flash, "modulate:a", peakAlpha, duration * 0.3f).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(flash, "modulate:a", 0f, duration * 0.7f).SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(() => FreeNode(flash)));
	}

	private static void SpawnBeam(Node parent, Vector2 fromGlobal, Vector2 toGlobal, Color color, float duration)
	{
		Line2D beam = new()
		{
			Name = "HextechRunes_VfxBeam",
			TopLevel = true,
			Width = 6f,
			DefaultColor = color with { A = 0.9f },
			BeginCapMode = Line2D.LineCapMode.Round,
			EndCapMode = Line2D.LineCapMode.Round,
			Points = [fromGlobal, toGlobal],
			Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
		};
		parent.AddChildSafely(beam);

		Tween tween = beam.CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(beam, "modulate:a", 0f, duration).SetEase(Tween.EaseType.In);
		tween.TweenProperty(beam, "width", 1.5f, duration).SetEase(Tween.EaseType.In);
		tween.Chain().TweenCallback(Callable.From(() => FreeNode(beam)));
	}

	private static Sprite2D MakeSprite(Texture2D texture, Color modulate)
	{
		return new Sprite2D
		{
			Texture = texture,
			Centered = true,
			Modulate = modulate,
			Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
		};
	}

	private static void SetSpriteDiameter(Sprite2D sprite, float diameter)
	{
		if (sprite.Texture is { } texture)
		{
			sprite.Scale = Vector2.One * (diameter / Math.Max(texture.GetWidth(), 1));
		}
	}

	private static void FreeNode(Node node)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			node.QueueFree();
		}
	}

	private static Texture2D GetGlowTexture()
	{
		if (_glowTexture != null && GodotObject.IsInstanceValid(_glowTexture))
		{
			return _glowTexture;
		}

		Gradient gradient = new()
		{
			Offsets = [0f, 0.45f, 1f],
			Colors = [new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 0.55f), new Color(1f, 1f, 1f, 0f)]
		};
		_glowTexture = new GradientTexture2D
		{
			Gradient = gradient,
			Width = 256,
			Height = 256,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(1f, 0.5f)
		};
		return _glowTexture;
	}

	private static Texture2D GetRingTexture()
	{
		if (_ringTexture != null && GodotObject.IsInstanceValid(_ringTexture))
		{
			return _ringTexture;
		}

		Gradient gradient = new()
		{
			Offsets = [0f, 0.60f, 0.80f, 0.93f, 1f],
			Colors =
			[
				new Color(1f, 1f, 1f, 0f),
				new Color(1f, 1f, 1f, 0f),
				new Color(1f, 1f, 1f, 1f),
				new Color(1f, 1f, 1f, 0f),
				new Color(1f, 1f, 1f, 0f)
			]
		};
		_ringTexture = new GradientTexture2D
		{
			Gradient = gradient,
			Width = 256,
			Height = 256,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(1f, 0.5f)
		};
		return _ringTexture;
	}
}
