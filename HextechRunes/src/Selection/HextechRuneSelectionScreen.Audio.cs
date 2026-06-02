using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal sealed partial class HextechRuneSelectionScreen
{
	private static AudioStreamPlayer? RerollSfxPlayer;
	private static AudioStream? RerollSfxStream;

	private void PlayRerollSfx()
	{
		AudioStream? stream = GetRerollSfxStream();
		if (stream == null)
		{
			return;
		}

		AudioStreamPlayer? player = GetRerollSfxPlayer();
		if (player == null)
		{
			return;
		}

		player.Stop();
		player.Stream = stream;
		player.VolumeLinear = Math.Clamp(GetSfxVolume() * RerollButtonSfxVolumeScale, 0f, 1f);
		player.Play();
	}

	private AudioStream? GetRerollSfxStream()
	{
		if (RerollSfxStream != null)
		{
			return RerollSfxStream;
		}

		RerollSfxStream = GD.Load<AudioStream>(RerollButtonSfxPath) ?? ResourceLoader.Load<AudioStream>(RerollButtonSfxPath);
		if (RerollSfxStream == null)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] SelectionScreen.PlayRerollSfx: failed to load sfx path={RerollButtonSfxPath}");
		}

		return RerollSfxStream;
	}

	private AudioStreamPlayer? GetRerollSfxPlayer()
	{
		if (GodotObject.IsInstanceValid(RerollSfxPlayer))
		{
			return RerollSfxPlayer;
		}

		RerollSfxPlayer = new AudioStreamPlayer
		{
			Name = "HextechRerollSfx",
			Bus = "Master",
			ProcessMode = ProcessModeEnum.Always
		};

		Node? host = NGame.Instance;
		host ??= GetTree()?.Root;
		if (host == null || !GodotObject.IsInstanceValid(host))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] SelectionScreen.PlayRerollSfx: failed to attach audio player.");
			RerollSfxPlayer.QueueFree();
			RerollSfxPlayer = null;
			return null;
		}

		host.AddChild(RerollSfxPlayer);
		return RerollSfxPlayer;
	}

	private static float GetSfxVolume()
	{
		return Math.Clamp(SaveManager.Instance?.SettingsSave?.VolumeSfx ?? 1f, 0f, 1f);
	}
}
