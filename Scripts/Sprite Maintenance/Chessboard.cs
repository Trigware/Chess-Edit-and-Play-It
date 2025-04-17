using Godot;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Chessboard;

public partial class Chessboard : Node
{
	private const int tileSize = 45;
	private const float maxOccupiedSpace = 0.9f, boardFlipCheckTimerMultiplier = 1.35f, boardFlipDefaultTimerMultiplier = 0.75f, tagRescaler = 2.5f, tagOffset = 0.285f;
	public static Vector2I tileCount = new(8, 8);
	public static Dictionary<Element, Sprite2D> tiles = new(), guiElements = new();
	protected static Vector2 gameviewSize = new(), oldviewSize = new(), vectorCenter = new();
	public static Vector2 boardCenter;
	public static float gridScale = 1, svgScale = 5;
	public static float actualTileSize = 45;
	public static bool isFlipped = false, waitingForBoardFlip = false, ySizeBigger = false;
	public static Vector2 leftUpCorner = default;
	public const int PositionRepetitionCount = 3;
    public override void _Ready()
    {
        Position.Load(Position.FEN.PromotionTest);
        boardCenter = new(((float)tileCount.X - 1) / 2, ((float)tileCount.Y - 1) / 2);
        Tags.GetRoyalsPerColor();
    }
	public struct Element
	{
		public Vector2I Location;
		public Layer Layer;
		public Element(Vector2I location, Layer layer)
		{
			Location = location;
			Layer = layer;
		}
		public Element(int x, int y, Layer layer)
		{
			Location = new(x, y);
			Layer = layer;
		}
	}
	public enum Layer { Background, Tile, Piece, ColorIndicator, Promotion, TagVisualizer, TagEmblem, Cursor }
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
		ySizeBigger = gameviewSize.Y > gameviewSize.X;
		if (ySizeBigger)
			valueSmallX = true;
		gridScale = Mathf.Min(gameviewSize.X, gameviewSize.Y) / ((valueSmallX ? gridSize.X : gridSize.Y) / maxOccupiedSpace);
		actualTileSize = tileSize * gridScale;
		vectorCenter = new Vector2(actualTileSize / 2, actualTileSize / 2) + (gameviewSize - gridSize * gridScale) / 2;
		if (tiles.Count == 0) Create(LoadGraphics.I); else Update();
	}
	private static void Create(Node parentNode)
	{
		gameviewSize = DisplayServer.WindowGetSize();
		isFlipped = Position.colorToMove == Position.oppositeStartColorToMove;
		CreateGUIElement("tile", Layer.Background, parentNode);
		for (int x = 0; x < tileCount.X; x++)
		{
			for (int y = 0; y < tileCount.Y; y++)
			{
				DrawTilesElement("tile", x, y, Layer.Tile, parentNode, gridScale);
				DrawPiece(x, y, parentNode);
			}
		}
		for (int colorIndicator = -1; true; colorIndicator = tileCount.Y)
		{
			CreateGUIElement("tile", Layer.ColorIndicator, parentNode, 1, colorIndicator, colorIndicator);
			if (colorIndicator == tileCount.Y)
				break;
		}
		for (int i = 0; i < Tags.tagPositions.Count; i++)
		{
			Vector2I position = Tags.tagPositions[i];
			int tagIndex = -1, j = 0;
			foreach (Tags.Tag tag in Tags.activeTags[i])
			{
				if (j == 3) break;
				if (!Tags.visualizerColors.ContainsKey(tag)) continue;
				DrawTagElement(position, tag, parentNode, tagIndex);
				tagIndex = tagIndex == -1 ? 1 : 0;
				j++;
			}
		}
		Cursor.actualLocation = Cursor.Location[Position.colorToMove];
		Vector2I cursorLocation = Cursor.actualLocation;
		DrawTilesElement("cursor", cursorLocation.X, cursorLocation.Y, Layer.Cursor, parentNode, gridScale, 0);
		Cursor.GetCursor();
		LegalMoves.GetLegalMoves();
	}
	private static void DrawPiece(int x, int y, Node parentNode, float transparency = 1)
	{
		Layer layer = Layer.Piece;
		if (Position.pieces.ContainsKey(new(x, y)))
			DrawTilesElement(Position.pieces[new(x, y)].ToString(), x, y, layer, parentNode, gridScale, transparency);
	}
	private static void DrawTagElement(Vector2I position, Tags.Tag tag, Node parentNode, int tagIndex, Tags.VisibleTag visibleTag = default, bool update = false)
	{
		Sprite2D tagVisualizer = DrawTilesElement("tag", position.X, position.Y, Layer.TagVisualizer, parentNode, gridScale, 1, update, false, tag, visibleTag.TagVisualizer, tagIndex);
		Sprite2D tagEmblem = null;
		if (Tags.TagEmblemName.ContainsKey(tag))
			tagEmblem = DrawTilesElement(Tags.TagEmblemName[tag], position.X, position.Y, Layer.TagEmblem, parentNode, gridScale, 1, update, false, tag, visibleTag.TagEmblem, tagIndex);
		if (update) return;
		if (Tags.visibleTags.ContainsKey(position)) Tags.visibleTags[position].Add(new(tag, tagVisualizer, tagEmblem));
		else Tags.visibleTags.Add(position, new() { new(tag, tagVisualizer, tagEmblem) });
	} 
	private static void CreateGUIElement(string name, Layer layer, Node parentNode, float transparency = 1, int x = 0, int y = 0) => DrawTilesElement(name, x, y, layer, parentNode, gridScale, 1, false, true);
	public static void UpdateGUIElement(Layer layer, Vector2I location = default) => DrawTilesElement("", location.X, location.Y, layer, null, gridScale, 1, true, true);
	public static Sprite2D DrawTilesElement(string name, int x, int y, Layer layer, Node parentNode, float gridScale, float transparency = 1, bool update = false, bool isGUI = false, Tags.Tag? tag = null, Sprite2D sprite = null, int tagIndex = 0)
	{
		if (!LoadGraphics.textureDict.ContainsKey(name) && !update)
		{
			GD.PrintErr($"The texture '{name}' cannot be loaded because it doesn't exist or it has a different name!");
			return new();
		}
		Texture2D texture = update ? null : LoadGraphics.textureDict[name];
		PackedScene tileScene = update ? null : (PackedScene)ResourceLoader.Load("res://Scenes/Tile.tscn");
		Node tileClone = update ? null : tileScene.Instantiate();
		Sprite2D spriteElement;
		if (update)
		{
			if (sprite != null) spriteElement = sprite;
			else
			{
				if (layer == Layer.Cursor) spriteElement = tiles[new(default, Layer.Cursor)];
				else spriteElement = isGUI ? guiElements[new(x, y, layer)] : tiles[new(x, y, layer)];
			}
		}
		else spriteElement = tileClone as Sprite2D;
		bool tileAvailable = spriteElement != null || update && tiles.ContainsKey(new(x, y, layer));
		if (!tileAvailable)
		{
			GD.PrintErr("Tile scene 'Tile.tscn' doesn't have a Sprite2D node for it's root.");
			return new();
		}
		if (!update)
		{
			spriteElement.Texture = texture;
			if (tag != null) spriteElement.Modulate = Tags.visualizerColors[tag ?? default];
			else spriteElement.Modulate = new(spriteElement.Modulate.R, spriteElement.Modulate.G, spriteElement.Modulate.B, transparency);
		}
		Vector2 position = CalculateTilePosition(x, y, layer, tagIndex);
		spriteElement.Scale = new Vector2(gridScale, gridScale);
		spriteElement.ZIndex = (int)layer;
		spriteElement.Position = layer == Layer.Background ? new(actualTileSize / 2, actualTileSize / 2) : position;
		LayerConditions(spriteElement, layer, position, x, y, update);
		if (!update)
		{
			Vector2I tileElementLocation = layer == Layer.Cursor ? default : new(x, y);
			if (isGUI) guiElements.Add(new(x, y, layer), spriteElement);
			else if (tag == null) tiles.Add(new(tileElementLocation, layer), spriteElement);
			parentNode.AddChild(spriteElement);
		}
		return spriteElement;
	}
	private static void LayerConditions(Sprite2D spriteElement, Layer layer, Vector2 position, int x, int y, bool update)
	{
        switch (layer)
        {
            case Layer.Background:
                spriteElement.Scale = new Vector2(gridScale, gridScale) * (gameviewSize.X / actualTileSize) * 2;
                spriteElement.Modulate = Colors.Dict[Colors.Enum.Background];
                break;
            case Layer.Tile:
                if (x == 0 && y == 0) leftUpCorner = position - new Vector2(actualTileSize / 2, actualTileSize / 2);
                if (!update) Colors.Set(spriteElement, Colors.Enum.Default, x, y); break;
            case Layer.Piece: goto case Layer.Promotion;
            case Layer.ColorIndicator:
                spriteElement.Modulate = Colors.Dict[isFlipped ? Colors.Enum.BlackColorToMove : Colors.Enum.WhiteColorToMove];
                spriteElement.Scale = new(tileCount.X * gridScale, gridScale);
                spriteElement.RotationDegrees = ySizeBigger ? 90 : 0; break;
            case Layer.Promotion:
                spriteElement.Scale /= svgScale; break;
            case Layer.TagVisualizer:
            case Layer.TagEmblem:
                Vector2 idealTextureSize = layer == Layer.TagVisualizer ? new(221, 221) : new(180, 180);
                spriteElement.Scale /= svgScale * tagRescaler * (spriteElement.Texture.GetSize() / idealTextureSize); break;
            case Layer.Cursor:
                spriteElement.TextureFilter = CanvasItem.TextureFilterEnum.Nearest; break;
        }
    }
	public static Sprite2D GetPiece(Vector2I location)
	{
		return tiles[new(location, Layer.Piece)];
	}
	public static Vector2 CalculateTilePosition(Vector2 position, Layer layer = Layer.Tile, int tagIndex = 0) => CalculateTilePosition(position.X, position.Y, layer, tagIndex);
	public static Vector2 CalculateTilePosition(float x, float y, Layer layer = Layer.Tile, int tagIndex = 0)
	{
		switch (layer)
		{
			case Layer.ColorIndicator:
				if (ySizeBigger) y = boardCenter.Y;
				else x = boardCenter.X; break;
			case Layer.TagEmblem:
			case Layer.TagVisualizer:
				float usedTagOffset = tagOffset * (isFlipped ? -1 : 1);
				Vector2 tagPosition = new Vector2(x + usedTagOffset * tagIndex, y + usedTagOffset);
				x = tagPosition.X; y = tagPosition.Y; break;
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
		foreach (KeyValuePair<Element, Sprite2D> keyValue in tiles)
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
		foreach (Element guiElement in guiElements.Keys)
			UpdateGUIElement(guiElement.Layer, guiElement.Location);
		foreach (KeyValuePair<Vector2I, List<Tags.VisibleTag>> tagsAtPos in Tags.visibleTags)
		{
			int tagIndex = -1;
			foreach (Tags.VisibleTag tagAtPosition in tagsAtPos.Value)
			{
				DrawTagElement(tagsAtPos.Key, tagAtPosition.Tag, LoadGraphics.I, tagIndex, tagAtPosition, true);
				tagIndex = tagIndex == -1 ? 1 : 0;
			}
		}
	}
	public static void Delete()
	{
		foreach (Sprite2D sprite in tiles.Values)
			sprite.QueueFree();
		foreach (Sprite2D sprite in guiElements.Values)
			sprite.QueueFree();
		tiles = new();
		guiElements = new();
		Tags.visibleTags = new();
	}
	public static void FlipBoard()
	{
		if (Position.GameEndState != Position.EndState.Ongoing) return;
		bool wasPreviouslyFlipped = isFlipped;
		if (isFlipped != (Position.colorToMove == Position.oppositeStartColorToMove))
		{
			float timerDuration = Animations.animationSpeed * (LegalMoves.CheckResponseZones.Count >= 1 && History.RedoMoves.Count == 0 ? boardFlipCheckTimerMultiplier : boardFlipDefaultTimerMultiplier);
			History.TimerCountdown(timerDuration, History.TimerType.BoardFlip);
		}
	}
}
