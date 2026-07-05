using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechSlowCookAuraHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(NCombatRoom), "_Ready", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechSlowCookAuraHooks), nameof(CombatRoomReadyPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature), BindingFlags.Instance | BindingFlags.Public, typeof(Creature)),
			postfix: new HarmonyMethod(typeof(HextechSlowCookAuraHooks), nameof(AddCreaturePostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCreature), "_Ready", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechSlowCookAuraHooks), nameof(CreatureReadyPostfix)));
	}

	private static void CombatRoomReadyPostfix(NCombatRoom __instance)
	{
		foreach (NCreature creature in __instance.CreatureNodes)
		{
			SlowCookAuraVisual.TryAttach(creature);
		}
	}

	private static void AddCreaturePostfix(NCombatRoom __instance, Creature creature)
	{
		SlowCookAuraVisual.TryAttach(HextechCreatureNodeRegistry.SafeGetCreatureNode(__instance, creature));
	}

	private static void CreatureReadyPostfix(NCreature __instance)
	{
		SlowCookAuraVisual.TryAttach(__instance);
	}
}

/// <summary>
/// 慢炖的持续脚底光环:参考 LOL 日炎斗篷的灼烧光环——贴地橙红火盘、
/// 流动的炼狱烟、反向旋转的火环,外加周期性向外扩散的灼烧脉冲波。
/// 骨架照抄 <see cref="HandOfBaronAuraVisual"/>(attach/可见性轮询/清理)。
/// </summary>
internal sealed class SlowCookAuraVisual
{
	private const string NodeName = "HextechRunes_SlowCookAura";
	private const float RuneRotationSpeed = 0.55f;
	private const float RingRotationSpeed = -0.4f;
	private const float SmokeRotationSpeed = 0.22f;
	private const float PulseSpeed = 2.4f;
	// 灼烧脉冲波:周期性从中心扩散的橙红衝击环,模拟日炎的灼烧 tick。
	private const float BurnWaveInterval = 2.2f;
	private const float BurnWaveDuration = 0.9f;
	private const float MinWidth = 180f;
	private const float MaxWidth = 340f;
	private const float WidthMultiplier = 0.90f;
	private const float HeightRatio = 0.36f;
	private static readonly Vector2 GroundOffset = new(0f, -18f);
	private static readonly Color EmberDiscColor = new(1f, 0.32f, 0.08f, 0.16f);
	private static readonly Color InfernoSmokeColor = new(1f, 0.45f, 0.12f, 0.16f);
	private static readonly Color FlameRingColor = new(1f, 0.6f, 0.18f, 0.3f);
	private static readonly Color SunfireRuneColor = new(1f, 0.5f, 0.14f, 0.6f);
	private static readonly Color BurnWaveColor = new(1f, 0.55f, 0.15f);
	private static readonly HashSet<ulong> ActiveCreatureNodes = [];
	private static readonly HashSet<string> LoggedMissingTexturePaths = [];

	private readonly NCreature _creature;
	private Node2D? _root;
	private Node? _renderParent;
	private AuraLayer? _discLayer;
	private AuraLayer? _smokeLayer;
	private AuraLayer? _ringLayer;
	private AuraLayer? _runeLayer;
	private AuraLayer? _waveLayer;
	private float _time;
	private float _waveTime;

	private SlowCookAuraVisual(NCreature creature)
	{
		_creature = creature;
	}

	internal static void TryAttach(NCreature? creature)
	{
		try
		{
			if (!GodotObject.IsInstanceValid(creature)
				|| !creature.IsNodeReady()
				|| creature.Hitbox == null
				|| creature.Entity?.Player == null)
			{
				return;
			}

			ulong creatureInstanceId = creature.GetInstanceId();
			if (!ActiveCreatureNodes.Add(creatureInstanceId))
			{
				return;
			}

			SlowCookAuraVisual visual = new(creature);
			if (!visual.Start())
			{
				ActiveCreatureNodes.Remove(creatureInstanceId);
				return;
			}

			TaskHelper.RunSafely(visual.RunAsync(creatureInstanceId));
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][SlowCookAura] Could not attach aura visual: {ex.Message}");
		}
	}

	private static bool ShouldShow(NCreature creature)
	{
		return creature.Entity?.Player?.GetRelic<SlowCookRune>() != null
			&& creature.Entity.IsAlive;
	}

	private bool Start()
	{
		Node? parent = _creature.GetParent();
		if (!GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		_renderParent = parent;

		_root = new Node2D
		{
			Name = NodeName,
			Visible = false,
			ShowBehindParent = false,
			TopLevel = false,
			ZAsRelative = true,
			ZIndex = 0
		};
		parent.AddChildSafely(_root);
		EnsureRenderOrder();

		_discLayer = TryCreateLayer(_root, "EmberDisc", HextechAssets.HandOfBaronAuraDiscPath, EmberDiscColor, additive: true);
		_smokeLayer = TryCreateClippedLayer(_root, "InfernoSmoke", HextechAssets.HandOfBaronAuraSmokePath, HextechAssets.HandOfBaronAuraDiscPath, InfernoSmokeColor, additive: true);
		_ringLayer = TryCreateLayer(_root, "FlameRing", HextechAssets.HandOfBaronAuraRingPath, FlameRingColor, additive: true);
		_runeLayer = TryCreateLayer(_root, "SunfireRune", HextechAssets.SlowCookAuraRunePath, SunfireRuneColor, additive: true);
		_waveLayer = TryCreateLayer(_root, "BurnWave", HextechAssets.HandOfBaronAuraRingPath, BurnWaveColor with { A = 0f }, additive: true);
		UpdateTransform();
		return _discLayer != null || _ringLayer != null;
	}

	private async Task RunAsync(ulong creatureInstanceId)
	{
		try
		{
			while (GodotObject.IsInstanceValid(_creature) && GodotObject.IsInstanceValid(_root))
			{
				bool visible = ShouldShow(_creature);
				_root.Visible = visible;
				if (visible)
				{
					EnsureRenderOrder();
					float dt = Mathf.Min(Mathf.Max((float)_root.GetProcessDeltaTime(), 1f / 120f), 0.05f);
					_time = Mathf.PosMod(_time + dt, 3600f);
					_waveTime += dt;
					Animate();
					UpdateTransform();
				}

				SceneTree tree = _root.GetTree();
				if (!GodotObject.IsInstanceValid(tree))
				{
					return;
				}

				await _root.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][SlowCookAura] Aura visual stopped after runtime error: {ex.Message}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(_root))
			{
				_root.QueueFree();
			}

			ActiveCreatureNodes.Remove(creatureInstanceId);
		}
	}

	private void EnsureRenderOrder()
	{
		if (GodotObject.IsInstanceValid(_renderParent) && GodotObject.IsInstanceValid(_root) && _root.GetIndex() != 0)
		{
			_renderParent.MoveChildSafely(_root, 0);
		}
	}

	private void UpdateTransform()
	{
		if (_root == null)
		{
			return;
		}

		float width = Mathf.Clamp(_creature.Hitbox.Size.X * WidthMultiplier, MinWidth, MaxWidth);
		float height = width * HeightRatio;
		_root.GlobalPosition = _creature.GetBottomOfHitbox() + GroundOffset;

		ScaleLayer(_discLayer, width * 1.34f, height * 1.26f);
		ScaleLayer(_smokeLayer, width * 1.18f, height * 0.92f);
		ScaleLayer(_ringLayer, width * 1.10f, height * 1.04f);
		ScaleLayer(_runeLayer, width * 0.96f, height * 0.96f);

		// 灼烧脉冲波:按周期从中心扩散并淡出。
		float wavePhase = Mathf.PosMod(_waveTime, BurnWaveInterval);
		if (_waveLayer != null)
		{
			if (wavePhase <= BurnWaveDuration)
			{
				float progress = wavePhase / BurnWaveDuration;
				float waveScale = 0.25f + progress * 1.25f;
				ScaleLayer(_waveLayer, width * waveScale, height * waveScale);
				_waveLayer.Sprite.Modulate = BurnWaveColor with { A = (1f - progress) * 0.45f };
			}
			else
			{
				_waveLayer.Sprite.Modulate = BurnWaveColor with { A = 0f };
			}
		}
	}

	private void Animate()
	{
		float dt = Mathf.Min(Mathf.Max((float)(_root?.GetProcessDeltaTime() ?? 0.016), 1f / 120f), 0.05f);
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

		// 火焰呼吸:透明度随相位起伏,层间相位错开避免同频闪烁。
		float pulse = 0.5f + 0.5f * MathF.Sin(_time * PulseSpeed);
		float pulseLate = 0.5f + 0.5f * MathF.Sin(_time * PulseSpeed + 1.3f);
		if (_discLayer != null)
		{
			_discLayer.Sprite.Modulate = EmberDiscColor with { A = 0.12f + pulse * 0.1f };
		}

		if (_ringLayer != null)
		{
			_ringLayer.Sprite.Modulate = FlameRingColor with { A = 0.24f + pulseLate * 0.14f };
		}

		if (_smokeLayer != null)
		{
			_smokeLayer.Sprite.Modulate = InfernoSmokeColor with { A = 0.12f + pulse * 0.1f };
		}

		if (_runeLayer != null)
		{
			_runeLayer.Sprite.Modulate = SunfireRuneColor with { A = 0.48f + pulseLate * 0.18f };
		}
	}

	private static AuraLayer? TryCreateLayer(Node2D parent, string name, string path, Color modulate, bool additive = false)
	{
		Texture2D? texture = LoadTextureOrWarn(path);
		if (texture == null)
		{
			return null;
		}

		Node2D plane = new()
		{
			Name = name,
			ZIndex = 0,
			ZAsRelative = true
		};
		parent.AddChildSafely(plane);

		Sprite2D sprite = new()
		{
			Name = "Texture",
			Texture = texture,
			Centered = true,
			Modulate = modulate
		};
		if (additive)
		{
			sprite.Material = new CanvasItemMaterial
			{
				BlendMode = CanvasItemMaterial.BlendModeEnum.Add
			};
		}

		plane.AddChildSafely(sprite);
		return new AuraLayer(plane, sprite);
	}

	private static Texture2D? LoadTextureOrWarn(string path)
	{
		Texture2D? texture = AssetHooks.LoadUiTexture(path);
		if (texture == null && LoggedMissingTexturePaths.Add(path))
		{
			Log.Warn($"[{ModInfo.Id}][SlowCookAura] Aura texture not found: {path}");
		}

		return texture;
	}

	// 长方形纹理(烟雾拖尾)旋转会露方角:套圆形裁剪父(按其 alpha 裁子内容),
	// 纹理放大覆盖裁剪圆的外接旋转范围,旋转全程不露边;缩放基准取裁剪圆纹理。
	private static AuraLayer? TryCreateClippedLayer(Node2D parent, string name, string texturePath, string clipPath, Color modulate, bool additive = false)
	{
		Texture2D? texture = LoadTextureOrWarn(texturePath);
		Texture2D? clipTexture = LoadTextureOrWarn(clipPath);
		if (texture == null || clipTexture == null)
		{
			return null;
		}

		Node2D plane = new()
		{
			Name = name,
			ZIndex = 0,
			ZAsRelative = true
		};
		parent.AddChildSafely(plane);

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
			Scale = new Vector2(
				clipSize * 1.5f / Math.Max(texture.GetWidth(), 1),
				clipSize * 1.5f / Math.Max(texture.GetHeight(), 1))
		};
		if (additive)
		{
			sprite.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
		}

		clip.AddChildSafely(sprite);
		return new AuraLayer(plane, sprite, clipTexture);
	}

	private static void ScaleLayer(AuraLayer? layer, float width, float height)
	{
		Texture2D? texture = layer?.ScaleBasis ?? layer?.Sprite.Texture;
		if (layer == null || texture == null)
		{
			return;
		}

		layer.Plane.Scale = new Vector2(width / Math.Max(texture.GetWidth(), 1), height / Math.Max(texture.GetHeight(), 1));
	}

	private sealed record AuraLayer(Node2D Plane, Sprite2D Sprite, Texture2D? ScaleBasis = null);
}
