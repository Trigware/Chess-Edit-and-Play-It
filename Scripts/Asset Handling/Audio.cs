using Godot;

public partial class Audio : AudioStreamPlayer
{
	private static AudioStreamPlayer audioPlayer = new();
	public static float sfxVolume = 1;
	public static bool playedCheck = false, silenceAudio = false;
	public enum Enum
	{
		Capture,
		Castle,
		Check,
		Checkmate,
		GameStart,
		GameEnd,
		Illegal,
		LowTime,
		Move,
		Promotion,
		Stalemate
	}
	public override void _Ready()
	{
		LoadAudio.LoadAll();
		AddChild(audioPlayer);
		PlayShiftless(Enum.GameStart);
	}
	public static void PlaySettings(Enum name, float pitchShift = 0, float volumeShift = 0, float pitch = 0, float volume = 0)
	{
		float minPitch = 1 - pitchShift, maxPitch = 1 + pitchShift;
		float minVolume = 1 - volumeShift, maxVolume = 1 + volumeShift;
		if (LoadAudio.audioDict.TryGetValue(name, out AudioStream sound))
		{
			audioPlayer.Stream = sound;
			audioPlayer.VolumeDb = (volume + GetRandom(minVolume, maxVolume)) * sfxVolume;
			audioPlayer.PitchScale = pitch + GetRandom(minPitch, maxPitch);
			audioPlayer.Play();
		}
		else
			GD.PrintErr($"Audio with name '{name}' was not loaded.");
	}

	public static void Play(Enum name)
	{
        if ((playedCheck && (name == Enum.Check || name == Enum.Checkmate)) || silenceAudio) return;
        if (name == Enum.Check || name == Enum.Checkmate) playedCheck = true;
        PlaySettings(name, 0.1f);
	}
	public static void PlayShiftless(Enum name)
	{
		Play(name);
	}
	private static float GetRandom(float lower, float upper)
	{
		return lower + (upper - lower) * GD.Randf();
	}
}
