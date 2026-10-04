using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HextechRunes;

/// <summary>男爵之手持有者脚下的紫色常驻光环:地面辉光、烟雾拖尾、柔光环与旋转的男爵符文四层。</summary>
internal sealed class HandOfBaronAuraVisual : HextechBehindCreatureVisual
{
	private const string NodeName = "HextechRunes_HandOfBaronAura";
	private const string LogTag = "BaronAura";
	private const float RuneRotationSpeed = -1.12f;
	private const float RingRotationSpeed = 0.34f;
	private const float SmokeRotationSpeed = -0.18f;
	private const float PulseSpeed = 2.15f;
	private const float MinWidth = 180f;
	private const float MaxWidth = 340f;
	private const float WidthMultiplier = 0.90f;
	private const float HeightRatio = 0.36f;
	private static readonly Vector2 GroundOffset = new(0f, -18f);

	private HextechAuraLayer? _discLayer;
	private HextechAuraLayer? _smokeLayer;
	private HextechAuraLayer? _ringLayer;
	private HextechAuraLayer? _runeLayer;
	private float _time;

	private HandOfBaronAuraVisual(NCreature creature)
		: base(creature, LogTag, "Hand of Baron aura visual")
	{
	}

	internal static void TryAttach(NCreature? creature)
	{
		TryAttach(
			creature,
			LogTag,
			"Hand of Baron aura visual",
			static node => node.Hitbox != null && node.Entity?.Player != null,
			static node => new HandOfBaronAuraVisual(node));
	}

	private static bool ShouldShow(NCreature creature)
	{
		return creature.Entity?.Player?.GetRelic<HandOfBaronRune>() != null
			&& creature.Entity.IsAlive;
	}

	protected override bool Start()
	{
		Texture2D? runeTexture = HextechTextures.LoadUiTexture(HextechAssets.HandOfBaronAuraRunePath);
		if (runeTexture == null || !TryCreateRoot(NodeName, visible: false) || Root == null)
		{
			return false;
		}

		_discLayer = HextechAuraLayer.TryCreate(Root, "GroundGlow", HextechAssets.SoftDiscEffectPath, new Color(0.52f, 0.12f, 1f, 0.18f));
		_smokeLayer = TryCreateClippedLayer(Root, "SoftVioletTrail", HextechAssets.HandOfBaronAuraSmokePath, HextechAssets.SoftDiscEffectPath, new Color(0.72f, 0.20f, 1f, 0.18f));
		_ringLayer = HextechAuraLayer.TryCreate(Root, "SoftRing", HextechAssets.SoftRingEffectPath, new Color(0.86f, 0.42f, 1f, 0.28f));
		_runeLayer = HextechAuraLayer.Create(Root, "BaronRune", runeTexture, new Color(1f, 0.35f, 1f, 0.78f));
		UpdateTransform();
		return true;
	}

	protected override bool Tick()
	{
		if (Root is not { } root)
		{
			return false;
		}

		bool visible = ShouldShow(Creature);
		root.Visible = visible;
		if (visible)
		{
			EnsureRenderOrder();
			float dt = ClampedFrameDelta(root);
			_time = Mathf.PosMod(_time + dt, 3600f);
			Animate(dt);
			UpdateTransform();
		}

		return true;
	}

	private void UpdateTransform()
	{
		if (Root == null)
		{
			return;
		}

		float width = Mathf.Clamp(Creature.Hitbox.Size.X * WidthMultiplier, MinWidth, MaxWidth);
		float height = width * HeightRatio;
		Root.GlobalPosition = Creature.GetBottomOfHitbox() + GroundOffset;

		HextechAuraLayer.Scale(_discLayer, width * 1.30f, height * 1.24f);
		HextechAuraLayer.Scale(_smokeLayer, width * 1.20f, height * 0.94f);
		HextechAuraLayer.Scale(_ringLayer, width * 1.12f, height * 1.05f);
		HextechAuraLayer.Scale(_runeLayer, width, height);
	}

	private void Animate(float dt)
	{
		if (_runeLayer != null)
		{
			_runeLayer.Sprite.Rotation = Mathf.PosMod(_runeLayer.Sprite.Rotation + RuneRotationSpeed * dt, Mathf.Tau);
		}

		if (_ringLayer != null)
		{
			_ringLayer.Sprite.Rotation = Mathf.PosMod(_ringLayer.Sprite.Rotation + RingRotationSpeed * dt, Mathf.Tau);
		}

		if (_smokeLayer != null)
		{
			_smokeLayer.Sprite.Rotation = Mathf.PosMod(_smokeLayer.Sprite.Rotation + SmokeRotationSpeed * dt, Mathf.Tau);
		}

		float pulse = 0.5f + 0.5f * MathF.Sin(_time * PulseSpeed);
		if (_discLayer != null)
		{
			_discLayer.Sprite.Modulate = new Color(0.52f, 0.12f, 1f, 0.14f + pulse * 0.10f);
		}

		if (_ringLayer != null)
		{
			_ringLayer.Sprite.Modulate = new Color(0.86f, 0.42f, 1f, 0.24f + pulse * 0.10f);
		}

		if (_smokeLayer != null)
		{
			_smokeLayer.Sprite.Modulate = new Color(0.72f, 0.20f, 1f, 0.12f + pulse * 0.10f);
		}
	}

	// 长方形纹理(如烟雾拖尾)旋转时会露出方角:套一层圆形裁剪父(按其 alpha 裁子内容),
	// 纹理放大到覆盖裁剪圆的外接尺寸,旋转全程不露边。缩放基准取裁剪圆纹理。
	private static HextechAuraLayer? TryCreateClippedLayer(Node2D parent, string name, string texturePath, string clipPath, Color modulate)
	{
		Texture2D? texture = HextechTextures.LoadUiTexture(texturePath);
		Texture2D? clipTexture = HextechTextures.LoadUiTexture(clipPath);
		if (texture == null || clipTexture == null)
		{
			return null;
		}

		Node2D plane = HextechAuraLayer.CreatePlane(parent, name);
		Sprite2D clip = new()
		{
			Name = "ClipCircle",
			Texture = clipTexture,
			Centered = true,
			Modulate = Colors.White,
			ClipChildren = CanvasItem.ClipChildrenMode.Only
		};
		plane.AddChildSafely(clip);

		float clipSize = Math.Max(clipTexture.GetWidth(), clipTexture.GetHeight());
		Sprite2D sprite = new()
		{
			Name = "Texture",
			Texture = texture,
			Centered = true,
			Modulate = modulate,
			// 覆盖裁剪圆的外接旋转范围(√2 倍直径),长方形被拉成方形无碍烟雾观感。
			Scale = new Vector2(
				clipSize * 1.5f / Math.Max(texture.GetWidth(), 1),
				clipSize * 1.5f / Math.Max(texture.GetHeight(), 1))
		};
		clip.AddChildSafely(sprite);
		return new HextechAuraLayer(plane, sprite, clipTexture);
	}
}
