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
		string fileLocation = $"res://Graphics/{name}";
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
		string[] pieceTextures = new string[] { "wP", "wN", "wB", "wR", "wQ", "wK", "bP", "bN", "bB", "bR", "bQ", "bK" },
                 miscellaneousTextures = new string[] { "tile.png", "cursor.png", "tag.svg", "royal.png", "castlee.png" };
		foreach (string texture in pieceTextures)
		{
			string textureNameUsedElsewhere = texture;
			if (texture[0] == 'w' || texture[0] == 'b')
				textureNameUsedElsewhere = (texture[0] == 'w') ? texture[1].ToString().ToUpper() : texture[1].ToString().ToLower();
			textureDict.Add(textureNameUsedElsewhere, LoadTexture(texture + ".svg"));
		}
		foreach (string texture in miscellaneousTextures)
		{
			string textureNameUsedElsewhere = texture.Split('.')[0];
            textureDict.Add(textureNameUsedElsewhere, LoadTexture(texture));
        }
    }
}