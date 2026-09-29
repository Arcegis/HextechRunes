using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace HextechRunes;

/// <summary>米凯尔的祝福(卡牌与敌方海克斯)净化时,目标脚下一次性的翠绿符文爆发。</summary>
internal static class HextechMikaelsBlessingVfx
{
	private const string LogTag = "MikaelsBlessingVfx";

	/// <summary>
	/// 纯本地表现:由战斗命令链同步调用,命令不一定跑在主线程,读碰撞框坐标、挂节点一律延后到主线程执行;
	/// 异常只记日志,不影响随后的治疗与净化结算。
	/// </summary>
	internal static void Play(Creature? creature)
	{
		try
		{
			// 先查注册表:测试进程与非战斗场景没有节点,直接返回,不触碰任何 Godot 单例。
			if (creature == null || creature.IsDead || HextechCreatureNodeRegistry.TryGet(creature) == null)
			{
				return;
			}

			Callable.From(() => PlayNow(creature)).CallDeferred();
		}
		catch (Exception ex)
		{
			LogPlayFailure(ex);
		}
	}

	private static void PlayNow(Creature creature)
	{
		try
		{
			NCreature? creatureNode = HextechCreatureNodeRegistry.TryGet(creature) ?? NCombatRoom.Instance?.GetCreatureNode(creature);
			if (!GodotObject.IsInstanceValid(creatureNode)
				|| !creatureNode.IsNodeReady()
				|| creatureNode.Hitbox == null)
			{
				return;
			}

			MikaelsBlessingBurstVisual.Play(creatureNode);
		}
		catch (Exception ex)
		{
			LogPlayFailure(ex);
		}
	}

	private static void LogPlayFailure(Exception ex)
	{
		HextechLog.Warn(LogTag, $"Could not play cleansing burst: {ex.Message}");
	}

	private sealed class MikaelsBlessingBurstVisual : HextechBehindCreatureVisual
	{
		private const float Lifetime = 1.08f;
		private const float MinWidth = 175f;
		private const float MaxWidth = 350f;
		private const float WidthMultiplier = 0.98f;
		private const float HeightRatio = 0.36f;
		private static readonly Vector2 GroundOffset = new(0f, -18f);

		private HextechAuraLayer? _runeLayer;
		private HextechAuraLayer? _glowLayer;
		private HextechAuraLayer? _ringLayer;
		private HextechAuraLayer? _flashLayer;
		private HextechAuraLayer? _waveLayer;
		private float _elapsed;

		private MikaelsBlessingBurstVisual(NCreature creature)
			: base(creature, LogTag, "Cleansing burst")
		{
		}

		internal static void Play(NCreature creature)
		{
			Launch(new MikaelsBlessingBurstVisual(creature));
		}

		protected override bool Start()
		{
			// 圆盘与柔光环是与男爵之手光环共用的通用特效贴图,只换色与时序。
			Texture2D? runeTexture = HextechTextures.LoadUiTexture(HextechAssets.MikaelsBlessingAoeRunePath);
			Texture2D? discTexture = HextechTextures.LoadUiTexture(HextechAssets.SoftDiscEffectPath);
			Texture2D? ringTexture = HextechTextures.LoadUiTexture(HextechAssets.SoftRingEffectPath);
			if (runeTexture == null
				|| discTexture == null
				|| ringTexture == null
				|| !TryCreateRoot("HextechRunes_MikaelsBlessingBurst", visible: true)
				|| Root is not { } root)
			{
				return false;
			}

			UpdatePosition();
			_runeLayer = HextechAuraLayer.Create(root, "MilioGroundRune", runeTexture, new Color(0.18f, 1f, 0.55f, 0.44f), additive: true);
			_glowLayer = HextechAuraLayer.Create(root, "EmeraldCleanseBloom", discTexture, new Color(0.02f, 0.95f, 0.70f, 0.38f), additive: true);
			_ringLayer = HextechAuraLayer.Create(root, "CleansingRing", ringTexture, new Color(0.38f, 1f, 0.72f, 0.70f), additive: true);
			_flashLayer = HextechAuraLayer.Create(root, "WhiteGreenBurst", discTexture, new Color(0.86f, 1f, 0.88f, 0.78f), additive: true);
			_waveLayer = HextechAuraLayer.Create(root, "OuterEmeraldWave", ringTexture, new Color(0.30f, 1f, 0.74f, 0.36f), additive: true);
			UpdateTransform();
			HextechLog.Info(LogTag, $"Burst attached node={root.GetPath()} creature={Creature.Entity?.ModelId.Entry ?? "<unknown>"}.");
			return true;
		}

		protected override bool Tick()
		{
			if (_elapsed >= Lifetime || Root is not { } root)
			{
				return false;
			}

			_elapsed += ClampedFrameDelta(root);
			EnsureRenderOrder();
			UpdatePosition();
			Animate();
			return true;
		}

		private void UpdatePosition()
		{
			if (Root != null)
			{
				Root.GlobalPosition = Creature.GetBottomOfHitbox() + GroundOffset;
			}
		}

		private float ResolveWidth()
		{
			return Mathf.Clamp(Creature.Hitbox.Size.X * WidthMultiplier, MinWidth, MaxWidth);
		}

		private void UpdateTransform()
		{
			float width = ResolveWidth();
			float height = width * HeightRatio;
			HextechAuraLayer.Scale(_runeLayer, width * 1.12f, height * 1.05f);
			HextechAuraLayer.Scale(_glowLayer, width * 1.28f, height * 1.16f);
			HextechAuraLayer.Scale(_ringLayer, width * 0.92f, height * 0.90f);
			HextechAuraLayer.Scale(_flashLayer, width * 0.62f, height * 0.58f);
			HextechAuraLayer.Scale(_waveLayer, width * 1.38f, height * 1.18f);
		}

		private void Animate()
		{
			float t = Mathf.Clamp(_elapsed / Lifetime, 0f, 1f);
			float intro = Mathf.SmoothStep(0f, 0.07f, t);
			float fade = 1f - Mathf.SmoothStep(0.64f, 1f, t);
			float burst = EaseOutCubic(Mathf.Clamp(t / 0.42f, 0f, 1f));
			float shockwave = EaseOutCubic(t);
			float flash = intro * (1f - Mathf.SmoothStep(0.08f, 0.30f, t));
			float alpha = intro * fade;
			float width = ResolveWidth();
			float height = width * HeightRatio;

			HextechAuraLayer.Scale(_runeLayer, width * (0.34f + burst * 0.98f), height * (0.32f + burst * 0.92f));
			HextechAuraLayer.Scale(_glowLayer, width * (0.30f + burst * 1.18f), height * (0.28f + burst * 1.02f));
			HextechAuraLayer.Scale(_ringLayer, width * (0.42f + burst * 0.78f), height * (0.40f + burst * 0.70f));
			HextechAuraLayer.Scale(_flashLayer, width * (0.14f + burst * 0.56f), height * (0.12f + burst * 0.50f));
			HextechAuraLayer.Scale(_waveLayer, width * (0.48f + shockwave * 1.22f), height * (0.44f + shockwave * 1.02f));

			if (_runeLayer != null)
			{
				_runeLayer.Sprite.Modulate = new Color(0.10f, 1f, 0.48f, 0.48f * alpha);
			}

			if (_glowLayer != null)
			{
				_glowLayer.Sprite.Modulate = new Color(0.00f, 0.96f, 0.78f, 0.40f * alpha);
			}

			if (_ringLayer != null)
			{
				_ringLayer.Sprite.Modulate = new Color(0.44f, 1f, 0.76f, 0.48f * alpha);
			}

			if (_flashLayer != null)
			{
				_flashLayer.Sprite.Modulate = new Color(0.86f, 1f, 0.86f, 0.72f * flash);
			}

			if (_waveLayer != null)
			{
				_waveLayer.Sprite.Modulate = new Color(0.28f, 1f, 0.72f, 0.34f * (1f - shockwave) * alpha);
			}
		}

		private static float EaseOutCubic(float value)
		{
			float inverse = 1f - value;
			return 1f - inverse * inverse * inverse;
		}
	}
}
