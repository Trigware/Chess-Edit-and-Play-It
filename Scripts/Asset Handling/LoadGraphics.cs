using Godot;
using System.Collections.Generic;

public partial class LoadGraphics : Node
{
	public static Dictionary<string, Texture2D> textureDict = new();
	public static LoadGraphics I;
	public override void _Ready()
	{
		I = this;
		LoadAllTextures();
	}
	private static Texture2D LoadTexture(string name)
	{
		string extension = "svg";
		if (name.Length != 2)
			extension = "png";
		string fileLocation = $"res://Graphics/{name}.{extension}";
		Texture2D texture = GD.Load<Texture2D>(fileLocation);
		if (texture == null)
		{
			GD.PrintErr($"A file at location '{fileLocation}' that could be an 'Texture2D' asset was not found!");
			return null;
		}
		return texture;
	}
	private static void LoadAllTextures()
	{
		string[] textureArray = new string[] { "tile", "cursor", "wP", "wN", "wB", "wR", "wQ", "wK", "bP", "bN", "bB", "bR", "bQ", "bK" };
		foreach (string texture in textureArray)
		{
			string spriteName = texture;
			if (texture.Length == 2 && (texture[0] == 'w' || texture[0] == 'b'))
				spriteName = (texture[0] == 'w') ? texture[1].ToString().ToUpper() : texture[1].ToString().ToLower();
			textureDict.Add(spriteName, LoadTexture(texture));
		}
	}
}