using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class Chessboard : Node
{
	private const int tileSize = 45;
	private const float maxOccupiedSpace = 0.85f;
	public static Vector2I tileCount = new(8, 8);
	public static Dictionary<Vector3I, Sprite2D> tiles = new();
	protected static Vector2 gameviewSize = new(), oldviewSize = new(), vectorCenter = new();
	public static float gridScale = 1;
	public static float actualTileSize = 45;
	public static Vector2? leftUpCorner = null;
	public const int PositionRepetitionCount = 3;

	public override void _Ready()
	{
		Position.Load(Position.FEN.CastlingTest);
	}
	public override void _Process(double delta)
	{
		gameviewSize = DisplayServer.WindowGetSize();
		if (gameviewSize != oldviewSize)
		{
            Draw();
			Animations.CancelEarly();
        }
        oldviewSize = gameviewSize;
	}
	public static void Draw()
	{
		Vector2 gridSize = (Vector2)tileCount * tileSize;
		bool valueSmallX = false;
		if (gameviewSize.Y > gameviewSize.X)
			valueSmallX = true;
		gridScale = Mathf.Min(gameviewSize.X, gameviewSize.Y) / ((valueSmallX ? gridSize.X : gridSize.Y) / maxOccupiedSpace);
		actualTileSize = tileSize * gridScale;
		vectorCenter = new Vector2(actualTileSize / 2, actualTileSize / 2) + (gameviewSize - gridSize * gridScale) / 2;
		if (tiles.Count == 0)
			Create(LoadGraphics.I);
		else
			Update.Board();
	}
	private static void Create(Node parentNode)
	{
		gameviewSize = DisplayServer.WindowGetSize();
		for (int x = 0; x < tileCount.X; x++)
		{
			for (int y = 0; y < tileCount.Y; y++)
			{
				DrawTile("tile", x, y, 0, parentNode);
				DrawTile(x, y, 1, parentNode);
			}
		}
		LegalMoves.GetLegalMoves();
	}
	private static void DrawTile(int x, int y, int z, Node parentNode, int i = 0)
	{
		if (Position.pieces.ContainsKey(new(x, y)))
			DrawTile(Position.pieces[new(x, y)].ToString(), x, y, z, parentNode);
	}
	private static void DrawTile(string name, int x, int y, int z, Node parentNode, int i = 0)
	{
		if (LoadGraphics.textureDict.ContainsKey(name))
		{
			Texture2D texture = LoadGraphics.textureDict[name];
			PackedScene tileScene = (PackedScene)ResourceLoader.Load("res://Scenes/Tile.tscn");
			Node tileClone = tileScene.Instantiate();
			if (tileClone is Sprite2D tileSprite)
			{
				tileSprite.Texture = texture;
				tileSprite.Scale = new Vector2(gridScale, gridScale);
				float xFloat = x, yFloat = y;
				if (z == 3)
				{
					xFloat -= 0.3f; yFloat += 0.3f;
					xFloat += 0.3f * i;
					tileSprite.Scale /= 1.35f;
				}
				Vector2 position = CalculateTilePosition(xFloat, yFloat);
				tileSprite.Position = position;
				if (z != 0)
					tileSprite.Scale /= Update.svgScale;
				if (z == 0)
				{
					if (x == 0 && y == 0)
						leftUpCorner = position - new Vector2(actualTileSize / 2, actualTileSize / 2);
					Colors.Set(tileSprite, Colors.Enum.Default, x, y);
				}
				if (z < 3)
					tiles.Add(new(x, y, z), tileSprite);
				else
					Tags.spriteTags.Add((new(x, y), tileSprite, i));
				parentNode.AddChild(tileSprite);
			}
		}
		else
			GD.PrintErr($"The texture '{name}' cannot be loaded because it doesn't exist or it has a different name!");
	}
	public static Vector2 CalculateTilePosition(float x, float y)
	{
		return new Vector2(x, y) * actualTileSize + vectorCenter;
	}
}
