using Godot;
using System.Collections.Generic;

public partial class Chessboard : Node
{
	private const int tileSize = 45;
	private const float maxOccupiedSpace = 0.9f, boardFlipCheckTimerMultiplier = 1.35f, boardFlipDefaultTimerMultiplier = 0.75f;
	public static Vector2I tileCount = new(8, 8);
	public static Dictionary<Element, Sprite2D> tiles = new(), guiElements = new();
	protected static Vector2 oldviewSize = new(), vectorCenter = new();
	public static Vector2 boardCenter, gameviewSize = new();
	public static float gridScale = 1, svgScale = 5;
	public static float actualTileSize = 45;
	public static bool isFlipped = false, waitingForBoardFlip = false, ySizeBigger = false;
	public static Vector2 leftUpCorner = default;
	public const int PositionRepetitionCount = 3, NMoveRuleInPlies = 100;
	private static PackedScene spriteRenderer;
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
	public enum Layer { Background, Tile, Helper, Piece, ColorIndicator, Promotion, Cursor, Timers, PauseMain, PauseOutline, PauseClose, PauseText }
	public override void _Ready()
	{
		InitiateSceneFields();
		Position.Load(Position.FEN.Default);
		boardCenter = new(((float)tileCount.X - 1)/2, ((float)tileCount.Y - 1)/2);
		Position.StartColorToMove = Position.ColorToMove;
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
		TimeControl.CheckIfOnLowTime();
		Text.ShowTimers();
	}
	private static void InitiateSceneFields()
	{
		spriteRenderer = (PackedScene)ResourceLoader.Load("res://Scenes/Renderer.tscn");
		Text.simpleLabel = (PackedScene)ResourceLoader.Load("res://Scenes/Label.tscn");
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
		Text.RefreshPauseText();
	}
	private static void Create(Node parentNode)
	{
		TimeControl.SetupTimers();
		gameviewSize = DisplayServer.WindowGetSize();
		isFlipped = Position.ColorToMove == Position.oppositeStartColorToMove;
		CreateGUIElement("tile", Layer.Background, parentNode);
		CreateGUIElement("PauseMain", Layer.PauseMain, parentNode);
		CreateGUIElement("PauseOutline", Layer.PauseOutline, parentNode);
		CreateGUIElement("Close", Layer.PauseClose, parentNode);
		for (int x = 0; x < tileCount.X; x++)
		{
			for (int y = 0; y < tileCount.Y; y++)
			{
				DrawTilesElement("tile", x, y, Layer.Tile, parentNode, gridScale);
				DrawTilesElement(x, y, Layer.Piece, parentNode);
				Text.TileRecognitionHelper(new(x, y));
			}
		}
		Cursor.actualLocation = Cursor.Location[Position.ColorToMove];
		Vector2I cursorLocation = Cursor.actualLocation;
		DrawTilesElement("cursor", cursorLocation.X, cursorLocation.Y, Layer.Cursor, parentNode, gridScale, 0);
		for (int colorIndicator = -1; true; colorIndicator = tileCount.Y)
		{
			CreateGUIElement("tile", Layer.ColorIndicator, parentNode, 1, colorIndicator, colorIndicator);
			if (colorIndicator == tileCount.Y)
				break;
		}
		Cursor.SetCursor();
		LegalMoves.GetLegalMoves();
		TimeControl.TimeLeftAtLastPlyStart = TimeControl.GetPlayerTimersTimeLeft();
	}
	private static void DrawTilesElement(int x, int y, Layer layer, Node parentNode, float transparency = 1)
	{
		if (Position.pieces.ContainsKey(new(x, y)))
			DrawTilesElement(Position.pieces[new(x, y)].ToString(), x, y, layer, parentNode, gridScale, transparency);
	}
	public static void CreateGUIElement(string name, Layer layer, Node parentNode, float transparency = 1, int x = 0, int y = 0) => DrawTilesElement(name, x, y, layer, parentNode, gridScale, 1, false, true);
	public static void UpdateGUIElement(Layer layer, Vector2I location = default) => DrawTilesElement("", location.X, location.Y, layer, null, gridScale, 1, true, true);
	public static Sprite2D DrawTilesElement(string name, int x, int y, Layer layer, Node parentNode, float gridScale, float transparency = 1, bool update = false, bool isGUI = false)
	{
		if (!LoadGraphics.textureDict.ContainsKey(name) && !update)
		{
			GD.PrintErr($"The texture '{name}' cannot be loaded because it doesn't exist or it has a different name!");
			return new();
		}
		float xAsFloat = x, yAsFloat = y;
		if (layer == Layer.PauseMain || layer == Layer.PauseOutline || layer == Layer.PauseClose)
		{
			Vector2 pauseMenuLocation = PauseMenu.GetStandardPosition(layer);
			xAsFloat = pauseMenuLocation.X;
			yAsFloat = pauseMenuLocation.Y;
		}
		Texture2D texture = update ? null : LoadGraphics.textureDict[name];
		Node tileClone = update ? null : spriteRenderer.Instantiate();
		Sprite2D spriteElement;
		if (update)
			spriteElement = GetOldSprite(layer, isGUI, x, y);
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
			spriteElement.Modulate = new(spriteElement.Modulate.R, spriteElement.Modulate.G, spriteElement.Modulate.B, transparency);
		}
		Vector2 position = CalculateTilePosition(xAsFloat, yAsFloat, layer);
		spriteElement.Scale = new Vector2(gridScale, gridScale);
        spriteElement.ZIndex = (int)layer;
		spriteElement.Position = layer == Layer.Background ? new(actualTileSize / 2, actualTileSize / 2) : position;
		LayerConditionals(spriteElement, layer, update, position, x, y);
		if (!update)
		{
			Vector2I tileElementLocation = layer == Layer.Cursor ? default : new(x, y);
			if (isGUI) guiElements.Add(new(x, y, layer), spriteElement);
			else tiles.Add(new(tileElementLocation, layer), spriteElement);
			parentNode.AddChild(spriteElement);
		}
		return spriteElement;
	}
	private static Sprite2D GetOldSprite(Layer layer, bool isGUI, int x, int y)
	{
		return layer switch
		{
			Layer.Cursor => tiles[new(default, Layer.Cursor)],
			_ => isGUI ? guiElements[new(x, y, layer)] : tiles[new(x, y, layer)]
		};
	}
	private static void LayerConditionals(Sprite2D spriteElement, Layer layer, bool update, Vector2 position, int x, int y)
	{
		switch (layer)
		{
			case Layer.Background:
				spriteElement.Scale = new Vector2(gameviewSize.X, gameviewSize.Y) * gridScale / actualTileSize * 3;
				spriteElement.Modulate = Colors.Dict[Colors.Enum.Background];
				break;
			case Layer.Tile:
				if (x == 0 && y == 0) leftUpCorner = position - new Vector2(actualTileSize / 2, actualTileSize / 2);
				if (!update) Colors.Set(spriteElement, Colors.Enum.Default, x, y); break;
			case Layer.Piece or Layer.Promotion:
                spriteElement.Scale /= svgScale; break;
            case Layer.ColorIndicator:
				spriteElement.Modulate = Colors.GetColorAsColorToMove();
				spriteElement.Scale = new(tileCount.X * gridScale, gridScale);
				spriteElement.RotationDegrees = ySizeBigger ? 90 : 0; break;
			case Layer.Cursor:
				spriteElement.TextureFilter = CanvasItem.TextureFilterEnum.Nearest; break;
			case Layer.PauseMain or Layer.PauseOutline or Layer.PauseClose:
				if (layer == Layer.PauseClose) spriteElement.Scale *= PauseMenu.CloseButtonScaleMutliplier;
				if (update) return;
				SetPauseMenuElement(spriteElement, layer);
				goto case Layer.Cursor;
		}
	}
	private static void SetPauseMenuElement(Sprite2D spriteElement, Layer layer)
	{
		Color spriteColor = new(0xFFFFFF00);
        spriteElement.Position = CalculateTilePosition(boardCenter.X, boardCenter.Y) + new Vector2(0, gameviewSize.Y / 2);
		switch (layer)
		{
			case Layer.PauseMain:
				PauseMenu.Main = spriteElement;
				spriteColor = Colors.Dict[Colors.Enum.PauseMain]; break;
			case Layer.PauseOutline:
				PauseMenu.Outline = spriteElement;
				spriteColor = Colors.GetColorAsColorToMove(); break;
			case Layer.PauseClose:
                PauseMenu.CloseButton = spriteElement; break;
		}
        spriteElement.Modulate = new(spriteColor.R, spriteColor.G, spriteColor.B, PauseMenu.IsPaused ? PauseMenu.PauseMenuMaxVisibilityTransparency : 0);
    }
    public static Sprite2D GetPiece(Vector2I location) => tiles[new(location, Layer.Piece)];
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
		if (Text.TileRecognitionLabels.Count > 0) Text.DeleteAllLabelsInList(Text.TileRecognitionLabels);
		foreach (KeyValuePair<Element, Sprite2D> keyValue in tiles)
		{
			Layer layer = keyValue.Key.Layer;
			string textureName = layer switch
			{
				Layer.Tile => "tile",
				Layer.Piece => Position.pieces[keyValue.Key.Location].ToString(),
				Layer.Promotion => Promotion.PromotionOptionsPieces[Promotion.PromotionOptionsPositions.IndexOf(keyValue.Key.Location)].ToString(),
				_ => "cursor"
			};
			Vector2I tilesElementLocation = keyValue.Key.Layer == Layer.Cursor ? Cursor.actualLocation : keyValue.Key.Location;
			DrawTilesElement(textureName, tilesElementLocation.X, tilesElementLocation.Y, keyValue.Key.Layer, LoadGraphics.I, gridScale, 1, true);
			if (layer == Layer.Tile)
				Text.TileRecognitionHelper(keyValue.Key.Location);
		}
		UpdateAllGUI();
	}
	public static void UpdateAllGUI(bool onlyColorIndicators = false)
	{
        foreach (Element guiElement in guiElements.Keys)
		{
			if (onlyColorIndicators && guiElement.Layer != Layer.ColorIndicator) continue;
            UpdateGUIElement(guiElement.Layer, guiElement.Location);
        }
    }
	public static void Delete()
	{
		foreach (Sprite2D sprite in tiles.Values)
			sprite.QueueFree();
		tiles = new();
	}
	public static void FlipBoard(bool replay = false)
	{
		if (Position.GameEndState != Position.EndState.Ongoing || (PauseMenu.UndoingMovesForNewGame && History.UndoMoves.Count > 0)) return;
		bool wasPreviouslyFlipped = isFlipped;
		isFlipped = Position.ColorToMove == Position.oppositeStartColorToMove;
		if (wasPreviouslyFlipped != isFlipped)
		{
			float timerDuration = Animations.animationSpeed * (LegalMoves.CheckResponseZones.Count >= 1 && History.RedoMoves.Count == 0 ? boardFlipCheckTimerMultiplier : boardFlipDefaultTimerMultiplier);
			History.TimerCountdown(timerDuration, History.TimerType.BoardFlip);
			char oppositeColor = LegalMoves.ReverseColorReturn(Position.ColorToMove);
			if (!replay) TimeControl.ModifyTimeLeft(oppositeColor);
			TimeControl.TimeLeftAtLastPlyStart = TimeControl.GetPlayerTimersTimeLeft();
		}
	}
}
