using Godot;
using System.Collections.Generic;
using System.IO;

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
			GD.PushError($"A file at location '{fileLocation}' that could be an 'Texture2D' asset was not found!");
			return null;
		}
		return texture;
	}
	private static void LoadAllTextures()
	{
		string[] pieceTextures = new string[] { "wP", "wN", "wB", "wR", "wQ", "wK", "bP", "bN", "bB", "bR", "bQ", "bK" };
		List<string> otherTextures = GetNonPieceTexturesInDirectory();
		foreach (string texture in pieceTextures)
		{
			string spriteName = texture;
			if (texture.Length == 2 && (texture[0] == 'w' || texture[0] == 'b'))
				spriteName = (texture[0] == 'w') ? texture[1].ToString().ToUpper() : texture[1].ToString().ToLower();
			textureDict.Add(spriteName, LoadTexture(texture + ".svg"));
		}
		foreach (string texture in otherTextures)
		{
			string spriteName = texture.Substring(0, texture.IndexOf('.'));
			Texture2D loadedTexture = LoadTexture(texture);
			if (spriteName == "PauseMain")
				PauseMenu.MenuTextureSize = loadedTexture.GetSize();
            textureDict.Add(spriteName, loadedTexture);
		}
		Text.notoSans = GD.Load<FontFile>("res://Fonts/NotoSans.ttf");
    }
	private static List<string> GetNonPieceTexturesInDirectory()
	{
		List<string> fileList = new();
		DirAccess directory = DirAccess.Open("res://Graphics");
		directory.ListDirBegin();
		while (true)
		{
			string fileName = directory.GetNext();
			if (fileName == "") break;
			if (fileName.Length == 6 || fileName.EndsWith(".import")) continue; // piece files
			fileList.Add(fileName);
		}
		directory.ListDirEnd();
		directory.Dispose();
		return fileList;
	}
}