using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class Chessboard : Node
{
	private const int tileSize = 45;
	private const float maxOccupiedSpace = 0.9f, boardFlipCheckTimerMultiplier = 1.35f, boardFlipDefaultTimerMultiplier = 0.75f;
	public static Vector2I tileCount = new(8, 8);
	public static Dictionary<TilesElement, Sprite2D> tiles = new();
	protected static Vector2 gameviewSize = new(), oldviewSize = new(), vectorCenter = new();
	public static Vector2 boardCenter;
	public static float gridScale = 1, svgScale = 5;
	public static float actualTileSize = 45;
	public static bool isFlipped = false, waitingForBoardFlip = false, ySizeBigger = false;
	public static Vector2 leftUpCorner = default;
	public const int PositionRepetitionCount = 3;
	public struct TilesElement
	{
		public Vector2I Location;
		public Layer Layer;
		public TilesElement(Vector2I location, Layer layer)
		{
			Location = location;
			Layer = layer;
		}
		public TilesElement(int x, int y, Layer layer)
		{
			Location = new(x, y);
			Layer = layer;
		}
	}
	public enum Layer { Background, Tile, Piece, Promotion, Cursor, ColorIndicator }
	public override void _Ready()
    {
        Position.Load(Position.FEN.CastlingTest);
		boardCenter = new(((float)tileCount.X - 1)/2, ((float)tileCount.Y - 1)/2);
		Tags.GetRoyalsPerColor();
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
		waitingForBoardFlip = false;
		Vector2 gridSize = (Vector2)tileCount * tileSize;
		bool valueSmallX = false;
		ySizeBigger = gameviewSize.Y > gameviewSize.X;
        if (ySizeBigger)
            valueSmallX = true;
		gridScale = Mathf.Min(gameviewSize.X, gameviewSize.Y) / ((valueSmallX ? gridSize.X : gridSize.Y) / maxOccupiedSpace);
		actualTileSize = tileSize * gridScale;
		vectorCenter = new Vector2(actualTileSize / 2, actualTileSize / 2) + (gameviewSize - gridSize * gridScale) / 2;
		if (tiles.Count == 0)
			Create(LoadGraphics.I);
		else
			Update();
	}
	private static void Create(Node parentNode)
	{
		gameviewSize = DisplayServer.WindowGetSize();
		isFlipped = Position.colorToMove == Position.oppositeStartColorToMove;
		DrawTilesElement("tile", 0, 0, Layer.Background, parentNode, gridScale);
		for (int x = 0; x < tileCount.X; x++)
		{
			for (int y = 0; y < tileCount.Y; y++)
			{
				DrawTilesElement("tile", x, y, Layer.Tile, parentNode, gridScale);
				DrawTilesElement(x, y, Layer.Piece, parentNode);
			}
		}
		Cursor.actualLocation = Cursor.Location[Position.colorToMove];
		Vector2I cursorLocation = Cursor.actualLocation;
		DrawTilesElement("cursor", cursorLocation.X, cursorLocation.Y, Layer.Cursor, parentNode, gridScale, 0);
		for (int colorIndicator = -1; true; colorIndicator = tileCount.Y)
		{
			DrawTilesElement("tile", colorIndicator, colorIndicator, Layer.ColorIndicator, parentNode, gridScale);
			if (colorIndicator == tileCount.Y)
				break;
		}
		Cursor.GetCursor();
		LegalMoves.GetLegalMoves();
	}
	private static void DrawTilesElement(int x, int y, Layer layer, Node parentNode, float transparency = 1)
	{
		if (Position.pieces.ContainsKey(new(x, y)))
			DrawTilesElement(Position.pieces[new(x, y)].ToString(), x, y, layer, parentNode, gridScale, transparency);
	}
	public static Sprite2D DrawTilesElement(string name, int x, int y, Layer layer, Node parentNode, float gridScale, float transparency = 1, bool update = false)
	{
		if (!LoadGraphics.textureDict.ContainsKey(name))
		{
			GD.PrintErr($"The texture '{name}' cannot be loaded because it doesn't exist or it has a different name!");
			return new();
		}
		Texture2D texture = update ? null : LoadGraphics.textureDict[name];
		PackedScene tileScene = update ? null : (PackedScene)ResourceLoader.Load("res://Scenes/Tile.tscn");
		Node tileClone = update ? null : tileScene.Instantiate();
		Sprite2D tileSprite = update ? (layer == Layer.Cursor ? tiles[new(default, Layer.Cursor)] : tiles[new(x, y, layer)]) : tileClone as Sprite2D;
		bool tileAvailable = tileSprite != null || update && tiles.ContainsKey(new(x, y, layer));
		if (!tileAvailable)
		{
			GD.PrintErr("Tile scene 'Tile.tscn' doesn't have a Sprite2D node for it's root.");
			return new();
		}
		if (!update)
		{
			tileSprite.Texture = texture;
			tileSprite.Modulate = new(tileSprite.Modulate.R, tileSprite.Modulate.G, tileSprite.Modulate.B, transparency);
		}
		Vector2 position = CalculateTilePosition(x, y, layer);
		tileSprite.Scale = new Vector2(gridScale, gridScale);
        tileSprite.ZIndex = (int)layer;
        tileSprite.Position = layer == Layer.Background ? new(actualTileSize / 2, actualTileSize / 2) : position;
		switch (layer)
		{
			case Layer.Background:
				tileSprite.Scale = new(gridScale * (gameviewSize.X / actualTileSize) * 2, gridScale * (gameviewSize.Y / actualTileSize) * 2);
				tileSprite.Modulate = Colors.Dict[Colors.Enum.Background];
				break;
			case Layer.Tile:
                if (x == 0 && y == 0) leftUpCorner = position - new Vector2(actualTileSize / 2, actualTileSize / 2);
                if (!update) Colors.Set(tileSprite, Colors.Enum.Default, x, y); break;
			case Layer.Piece:
			case Layer.Promotion:
                tileSprite.Scale /= svgScale; break;
            case Layer.Cursor:
                tileSprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest; break;
            case Layer.ColorIndicator:
                tileSprite.Modulate = Colors.Dict[Position.colorToMove == 'w' ? Colors.Enum.WhiteColorToMove : Colors.Enum.BlackColorToMove];
                tileSprite.Scale = new(tileCount.X * gridScale, gridScale);
                tileSprite.RotationDegrees = ySizeBigger ? 90 : 0; break;
        }
		if (!update)
		{
			Vector2I tileElementLocation = layer == Layer.Cursor ? default : new(x, y);
			tiles.Add(new(tileElementLocation, layer), tileSprite);
			parentNode.AddChild(tileSprite);
		}
		return tileSprite;
	}
	public static Sprite2D GetPiece(Vector2I location)
	{
		return tiles[new(location, Layer.Piece)];
	}
	public static Vector2 CalculateTilePosition(float x, float y, Layer layer = Layer.Tile)
	{
		if (layer == Layer.ColorIndicator)
		{
			if (ySizeBigger) y = boardCenter.Y;
			else x = boardCenter.X;
		}
        if (isFlipped)
		{
			x = tileCount.X - x - 1;
			y = tileCount.Y - y - 1;
		}
		return new Vector2(x, y) * actualTileSize + vectorCenter;
	}
	public static void Update()
	{
		foreach (KeyValuePair<TilesElement, Sprite2D> keyValue in tiles)
		{
			string textureName = keyValue.Key.Layer switch
			{
				Layer.Tile => "tile",
				Layer.Piece => Position.pieces[keyValue.Key.Location].ToString(),
				Layer.Promotion => Promotion.PromotionOptionsPieces[Promotion.PromotionOptionsPositions.IndexOf(keyValue.Key.Location)].ToString(),
				_ => "cursor"
			};
			Vector2I tilesElementLocation = keyValue.Key.Layer == Layer.Cursor ? Cursor.actualLocation : keyValue.Key.Location;
			DrawTilesElement(textureName, tilesElementLocation.X, tilesElementLocation.Y, keyValue.Key.Layer, LoadGraphics.I, gridScale, 1, true);
		}
	}
	public static void Delete()
	{
		foreach (Sprite2D sprite in tiles.Values)
			sprite.QueueFree();
		tiles = new();
	}
	public static void FlipBoard()
	{
		if (Position.GameEndState != Position.EndState.Ongoing) return;
		bool wasPreviouslyFlipped = isFlipped;
		isFlipped = Position.colorToMove == Position.oppositeStartColorToMove;
		if (wasPreviouslyFlipped != isFlipped)
		{
			float timerDuration = Animations.animationSpeed * (LegalMoves.CheckResponseZones.Count >= 1 && History.RedoMoves.Count == 0 ? boardFlipCheckTimerMultiplier : boardFlipDefaultTimerMultiplier);
			History.TimerCountdown(timerDuration, History.TimerType.BoardFlip);
		}
	}
}
