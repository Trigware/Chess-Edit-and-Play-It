using Godot;
using System.Collections.Generic;
public partial class LoadAudio : Node
{
	public static Dictionary<string, AudioStream> audioDict = new();
	private static AudioStream Load(string name)
	{
		string fileLocation = $"res://Audio/{name}.wav";
		AudioStream audio = GD.Load<AudioStream>(fileLocation);
		if (audio == null)
		{
			GD.PrintErr($"A file at location '{fileLocation}' that could be an 'AudioStream' asset was not found!");
			return null;
		}
		return audio;
	}
	public static void LoadAll()
	{
		string[] audioArray = new string[] { "capture", "castle", "check", "game start", "illegal", "mate", "move", "promote", "stalemate" };
		foreach (string audioName in audioArray)
			audioDict.Add(audioName, Load(audioName));
	}
}
