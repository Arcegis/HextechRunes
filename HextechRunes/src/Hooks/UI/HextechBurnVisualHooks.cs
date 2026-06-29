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
/// 灼烧(<see cref="HextechBurnPower"/>)的持续燃烧特效。仿 <see cref="HandOfBaronAuraVisual"/>:挂到每个
/// <see cref="NCreature"/>(敌我通用)、逐帧轮询该角色是否带灼烧 power,带则在其身上烧起火焰,火势随层数增强。
/// 纯表现层,只读 power 状态、只建可视节点,不改任何 gameplay/同步状态。
/// </summary>
internal static class HextechBurnVisualHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(NCombatRoom), "_Ready", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechBurnVisualHooks), nameof(CombatRoomReadyPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature), BindingFlags.Instance | BindingFlags.Public, typeof(Creature)),
			postfix: new HarmonyMethod(typeof(HextechBurnVisualHooks), nameof(AddCreaturePostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCreature), "_Ready", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechBurnVisualHooks), nameof(CreatureReadyPostfix)));
		HextechLog.Info($"[{ModInfo.Id}][Burn] Visual hooks installed.");
	}

	private static void CombatRoomReadyPostfix(NCombatRoom __instance)
	{
		foreach (NCreature creature in __instance.CreatureNodes)
		{
			HextechBurnVisual.TryAttach(creature);
		}
	}

	private static void AddCreaturePostfix(NCombatRoom __instance, Creature creature)
	{
		HextechBurnVisual.TryAttach(__instance.GetCreatureNode(creature));
	}

	private static void CreatureReadyPostfix(NCreature __instance)
	{
		HextechBurnVisual.TryAttach(__instance);
	}
}

internal sealed class HextechBurnVisual
{
	private const string NodeName = "HextechRunes_BurnFlames";
	private const int EmberCount = 8;
	private const float BurnAmountForFullIntensity = 12f;

	private static readonly Color[] EmberColors =
	[
		new(1f, 0.58f, 0.16f),
		new(1f, 0.82f, 0.32f),
		new(1f, 0.34f, 0.12f)
	];
	private static readonly Color BaseGlowColor = new(1f, 0.46f, 0.12f);

	private static readonly HashSet<ulong> ActiveCreatureNodes = [];
	private static Texture2D? _glowTexture;

	private readonly NCreature _creature;
	private Node2D? _root;
	private Sprite2D? _baseGlow;
	private readonly Ember[] _embers = new Ember[EmberCount];
	private float _time;

	private HextechBurnVisual(NCreature creature)
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
				|| creature.Entity == null)
			{
				return;
			}

			ulong creatureInstanceId = creature.GetInstanceId();
			if (!ActiveCreatureNodes.Add(creatureInstanceId))
			{
				return;
			}

			HextechBurnVisual visual = new(creature);
			if (!visual.Start())
			{
				ActiveCreatureNodes.Remove(creatureInstanceId);
				return;
			}

			TaskHelper.RunSafely(visual.RunAsync(creatureInstanceId));
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Burn] Could not attach burn flames visual: {ex.Message}");
		}
	}

	private static bool TryGetIntensity(NCreature creature, out float intensity)
	{
		intensity = 0f;
		if (creature.Entity is not { IsAlive: true } entity || !entity.HasPower<HextechBurnPower>())
		{
			return false;
		}

		int amount = entity.GetPowerAmount<HextechBurnPower>();
		if (amount <= 0)
		{
			return false;
		}

		intensity = Mathf.Clamp(amount / BurnAmountForFullIntensity, 0.35f, 1f);
		return true;
	}

	private bool Start()
	{
		Node? parent = _creature.GetParent();
		if (!GodotObject.IsInstanceValid(parent))
		{
			return false;
		}

		_root = new Node2D
		{
			Name = NodeName,
			Visible = false,
			ZAsRelative = true,
			ZIndex = 1
		};
		parent.AddChildSafely(_root);

		_baseGlow = MakeSprite(BaseGlowColor with { A = 0f });
		_root.AddChild(_baseGlow);

		for (int i = 0; i < EmberCount; i++)
		{
			Sprite2D sprite = MakeSprite(EmberColors[i % EmberColors.Length] with { A = 0f });
			_root.AddChild(sprite);
			_embers[i] = new Ember
			{
				Sprite = sprite,
				Phase = i / (float)EmberCount,
				Speed = 0.7f + 0.55f * ((i * 3 % 5) / 5f),
				Color = EmberColors[i % EmberColors.Length],
				Cycle = i
			};
		}

		return true;
	}

	private async Task RunAsync(ulong creatureInstanceId)
	{
		try
		{
			while (GodotObject.IsInstanceValid(_creature) && GodotObject.IsInstanceValid(_root))
			{
				bool visible = TryGetIntensity(_creature, out float intensity);
				_root!.Visible = visible;

				if (visible)
				{
					float dt = Mathf.Clamp((float)_root.GetProcessDeltaTime(), 1f / 120f, 0.05f);
					_time = Mathf.PosMod(_time + dt, 3600f);
					Animate(dt, intensity);
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
			Log.Warn($"[{ModInfo.Id}][Burn] Burn flames visual stopped after runtime error: {ex.Message}");
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

	private void Animate(float dt, float intensity)
	{
		if (_root == null || _creature.Hitbox == null)
		{
			return;
		}

		Vector2 bottom = _creature.GetBottomOfHitbox();
		float width = Mathf.Clamp(_creature.Hitbox.Size.X, 100f, 360f);
		float height = Mathf.Abs(_creature.GetTopOfHitbox().Y - bottom.Y);
		_root.GlobalPosition = bottom;

		float riseHeight = Mathf.Lerp(height * 0.42f, height * 0.82f, intensity);
		float emberSize = width * Mathf.Lerp(0.22f, 0.34f, intensity);
		float spread = width * 0.36f;

		// 火光闪烁:多正弦叠加近似火焰跳动。
		float flicker = 0.62f
			+ 0.22f * MathF.Sin(_time * 13.0f)
			+ 0.10f * MathF.Sin(_time * 23.0f + 1.3f)
			+ 0.06f * MathF.Sin(_time * 7.0f + 2.1f);
		flicker = Mathf.Clamp(flicker, 0.25f, 1f);

		if (_baseGlow != null)
		{
			float glowAlpha = Mathf.Lerp(0.18f, 0.42f, intensity) * flicker;
			_baseGlow.Modulate = BaseGlowColor with { A = glowAlpha };
			_baseGlow.Position = new Vector2(0f, -riseHeight * 0.18f);
			SetSpriteDiameter(_baseGlow, width * Mathf.Lerp(0.9f, 1.25f, intensity));
		}

		float emberSpeed = 0.6f + 0.8f * intensity;
		for (int i = 0; i < _embers.Length; i++)
		{
			Ember ember = _embers[i];
			ember.Phase += dt * ember.Speed * emberSpeed;
			if (ember.Phase >= 1f)
			{
				ember.Phase -= 1f;
				ember.Cycle++;
			}

			float phase = ember.Phase;
			float baseX = spread * MathF.Sin(ember.Cycle * 2.3f + i * 1.7f);
			float sway = width * 0.10f * MathF.Sin(phase * 9.0f + i);
			float y = -riseHeight * phase;
			// 起步淡入、升高淡出,越往上越小。
			float fade = phase < 0.14f ? phase / 0.14f : (1f - phase) / 0.86f;
			float alpha = Mathf.Clamp(fade, 0f, 1f) * intensity * (0.7f + 0.3f * flicker);
			float size = emberSize * (1f - 0.55f * phase);

			ember.Sprite.Position = new Vector2(baseX + sway, y);
			ember.Sprite.Modulate = ember.Color with { A = alpha };
			SetSpriteDiameter(ember.Sprite, size);
		}
	}

	private static Sprite2D MakeSprite(Color modulate)
	{
		return new Sprite2D
		{
			Texture = GetGlowTexture(),
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

	private static Texture2D GetGlowTexture()
	{
		if (_glowTexture != null && GodotObject.IsInstanceValid(_glowTexture))
		{
			return _glowTexture;
		}

		Gradient gradient = new()
		{
			Offsets = [0f, 0.4f, 1f],
			Colors = [new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 0.5f), new Color(1f, 1f, 1f, 0f)]
		};
		_glowTexture = new GradientTexture2D
		{
			Gradient = gradient,
			Width = 128,
			Height = 128,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(1f, 0.5f)
		};
		return _glowTexture;
	}

	private sealed class Ember
	{
		public required Sprite2D Sprite;
		public float Phase;
		public float Speed;
		public Color Color;
		public int Cycle;
	}
}
