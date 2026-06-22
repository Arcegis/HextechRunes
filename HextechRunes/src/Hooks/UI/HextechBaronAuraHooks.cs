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

internal static class HextechBaronAuraHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(NCombatRoom), "_Ready", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechBaronAuraHooks), nameof(CombatRoomReadyPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature), BindingFlags.Instance | BindingFlags.Public, typeof(Creature)),
			postfix: new HarmonyMethod(typeof(HextechBaronAuraHooks), nameof(AddCreaturePostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCreature), "_Ready", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechBaronAuraHooks), nameof(CreatureReadyPostfix)));
		Log.Info($"[{ModInfo.Id}][BaronAura] Hooks installed.");
	}

	private static void CombatRoomReadyPostfix(NCombatRoom __instance)
	{
		foreach (NCreature creature in __instance.CreatureNodes)
		{
			HandOfBaronAuraVisual.TryAttach(creature);
		}
	}

	private static void AddCreaturePostfix(NCombatRoom __instance, Creature creature)
	{
		HandOfBaronAuraVisual.TryAttach(__instance.GetCreatureNode(creature));
	}

	private static void CreatureReadyPostfix(NCreature __instance)
	{
		HandOfBaronAuraVisual.TryAttach(__instance);
	}
}

internal sealed class HandOfBaronAuraVisual
{
	private const string NodeName = "HextechRunes_HandOfBaronAura";
	private const float RuneRotationSpeed = -1.12f;
	private const float RingRotationSpeed = 0.34f;
	private const float SmokeRotationSpeed = -0.18f;
	private const float PulseSpeed = 2.15f;
	private const float MinWidth = 180f;
	private const float MaxWidth = 340f;
	private const float WidthMultiplier = 0.90f;
	private const float HeightRatio = 0.36f;
	private static readonly Vector2 GroundOffset = new(0f, -18f);
	private static readonly HashSet<ulong> ActiveCreatureNodes = [];
	private static readonly HashSet<string> LoggedMissingTexturePaths = [];

	private readonly NCreature _creature;
	private Node2D? _root;
	private Node? _renderParent;
	private AuraLayer? _discLayer;
	private AuraLayer? _smokeLayer;
	private AuraLayer? _ringLayer;
	private AuraLayer? _runeLayer;
	private float _time;
	private bool _lastVisible;

	private HandOfBaronAuraVisual(NCreature creature)
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

			HandOfBaronAuraVisual visual = new(creature);
			if (!visual.Start())
			{
				ActiveCreatureNodes.Remove(creatureInstanceId);
				return;
			}

			TaskHelper.RunSafely(visual.RunAsync(creatureInstanceId));
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Could not attach Hand of Baron aura visual: {ex.Message}");
		}
	}

	private static bool ShouldShow(NCreature creature)
	{
		return creature.Entity?.Player?.GetRelic<HandOfBaronRune>() != null
			&& creature.Entity.IsAlive;
	}

	private bool Start()
	{
		Node? parent = ResolveRenderParent();
		if (parent == null)
		{
			return false;
		}
		_renderParent = parent;

		Texture2D? runeTexture = LoadTextureOrWarn(HextechAssets.HandOfBaronAuraRunePath);
		if (runeTexture == null)
		{
			return false;
		}

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

		_discLayer = TryCreateLayer(_root, "GroundGlow", HextechAssets.HandOfBaronAuraDiscPath, new Color(0.52f, 0.12f, 1f, 0.18f), 0);
		_smokeLayer = TryCreateLayer(_root, "SoftVioletTrail", HextechAssets.HandOfBaronAuraSmokePath, new Color(0.72f, 0.20f, 1f, 0.18f), 2);
		_ringLayer = TryCreateLayer(_root, "SoftRing", HextechAssets.HandOfBaronAuraRingPath, new Color(0.86f, 0.42f, 1f, 0.28f), 3);
		_runeLayer = CreateLayer(_root, "BaronRune", runeTexture, new Color(1f, 0.35f, 1f, 0.78f), 4);
		UpdateTransform();
		Log.Info($"[{ModInfo.Id}][BaronAura] Attached node={_root.GetPath()} parent={parent.GetPath()} player={_creature.Entity?.Player?.Character.Id.Entry ?? "<unknown>"} hasRune={ShouldShow(_creature)}.");
		return true;
	}

	private Node? ResolveRenderParent()
	{
		Node? parent = _creature.GetParent();
		if (!GodotObject.IsInstanceValid(parent))
		{
			return null;
		}

		return parent;
	}

	private async Task RunAsync(ulong creatureInstanceId)
	{
		try
		{
			while (GodotObject.IsInstanceValid(_creature) && GodotObject.IsInstanceValid(_root))
			{
				bool visible = ShouldShow(_creature);
				_root.Visible = visible;
				if (visible != _lastVisible)
				{
					Log.Info($"[{ModInfo.Id}][BaronAura] Visibility changed: visible={visible} node={_root.GetPath()} player={_creature.Entity?.Player?.Character.Id.Entry ?? "<unknown>"}.");
					_lastVisible = visible;
				}

				if (visible)
				{
					EnsureRenderOrder();
					float dt = Mathf.Min(Mathf.Max((float)_root.GetProcessDeltaTime(), 1f / 120f), 0.05f);
					_time = Mathf.PosMod(_time + dt, 3600f);
					Animate(dt);
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
			Log.Warn($"[{ModInfo.Id}][Mayhem] Hand of Baron aura visual stopped after runtime error: {ex.Message}");
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

		ScaleLayer(_discLayer, width * 1.30f, height * 1.24f);
		ScaleLayer(_smokeLayer, width * 1.20f, height * 0.94f);
		ScaleLayer(_ringLayer, width * 1.12f, height * 1.05f);
		ScaleLayer(_runeLayer, width, height);
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

	private static AuraLayer CreateLayer(Node2D parent, string name, Texture2D texture, Color modulate, int zIndex, bool additive = false)
	{
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

	private static AuraLayer? TryCreateLayer(Node2D parent, string name, string path, Color modulate, int zIndex, bool additive = false)
	{
		Texture2D? texture = LoadTextureOrWarn(path);
		return texture == null ? null : CreateLayer(parent, name, texture, modulate, zIndex, additive);
	}

	private static Texture2D? LoadTextureOrWarn(string path)
	{
		Texture2D? texture = AssetHooks.LoadUiTexture(path);
		if (texture == null && LoggedMissingTexturePaths.Add(path))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Hand of Baron aura texture not found: {path}");
		}

		return texture;
	}

	private static void ScaleLayer(AuraLayer? layer, float width, float height)
	{
		Texture2D? texture = layer?.Sprite.Texture;
		if (layer == null || texture == null)
		{
			return;
		}

		layer.Plane.Scale = new Vector2(width / Math.Max(texture.GetWidth(), 1), height / Math.Max(texture.GetHeight(), 1));
	}

	private sealed record AuraLayer(Node2D Plane, Sprite2D Sprite);
}
