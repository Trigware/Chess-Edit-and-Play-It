using Godot;
using System;
using System.Collections.Generic;
public partial class LoadAudio
{
	public static Dictionary<Audio.Enum, AudioStream> audioDict = new();
	private static AudioStream Load(string name)
	{
		string fileLocation = $"res://Audio/{name}.wav";
		AudioStream audio = GD.Load<AudioStream>(fileLocation);
		if (audio == null)
		{
			GD.PushError($"A file at location '{fileLocation}' that could be an 'AudioStream' asset was not found!");
			return null;
		}
		return audio;
	}
	public static void LoadAll()
	{
		Audio.Enum[] audioArray = (Audio.Enum[])Enum.GetValues(typeof(Audio.Enum));
		foreach (Audio.Enum audioName in audioArray)
		{
			string audioNameAsString = audioName.ToString();
			audioDict.Add(audioName, Load(audioNameAsString));
		}
	}
}
