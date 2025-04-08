using Godot;
using System;

public partial class Interaction : Chessboard
{
	public static TilesElement? selectedTile = null;
	public static bool escapePressed, lastEscapeSelection = false;
	private bool interactionButtonPressed = false, leftMouseOld = false;
	public override void _Process(double delta)
	{
		History.KeyPressDetection();
		bool leftActuallyPressed = Input.IsMouseButtonPressed(MouseButton.Left);
		escapePressed = Input.IsKeyPressed(Key.Escape);
		if (escapePressed) { if (selectedTile != null) lastEscapeSelection = true; } else lastEscapeSelection = false;
		Cursor.KeyPressDetection();
		interactionButtonPressed = (leftActuallyPressed && !leftMouseOld) || Cursor.enterPressedNow;
		if ((interactionButtonPressed || escapePressed) && Position.GameEndState == Position.EndState.Ongoing)
			Select();
		leftMouseOld = leftActuallyPressed;
	}
	private Vector2I GetPositionOnBoard()
	{
		if (leftUpCorner == null)
		{
			GD.PrintErr("The chessboard is not loaded!");
			return new();
		}
		Vector2 leftUpNotNull = (Vector2)leftUpCorner;
		Vector2 mousePosition = GetViewport().GetMousePosition();
		return (Vector2I)((mousePosition - leftUpNotNull) / actualTileSize).Floor();
	}
	private void Select()
	{
		Vector2I flatMousePosition = Cursor.enterPressedNow ? Cursor.actualLocation : GetPositionOnBoard();
		if (interactionButtonPressed && Promotion.PromotionOptionsPositions.Contains(flatMousePosition))
		{
			Promotion.Promote(flatMousePosition);
			return;
		}
		if (Position.colorToMove == '\0')
			return;
		TilesElement mousePositionBoard = new(flatMousePosition, Layer.Tile);
		if (selectedTile != null && (Input.IsKeyPressed(Key.Escape) || (interactionButtonPressed && (selectedTile ?? default).Location == mousePositionBoard.Location)))
		{
			Deselect((selectedTile ?? default).Location);
			PreviousMoveTiles(Colors.Enum.PreviousMove);
			return;
		}
		if (interactionButtonPressed)
			InteractWithPiece(flatMousePosition);
	}
	private static void InteractWithPiece(Vector2I targetedPiece)
	{
		bool canSwitchSelectedTile = PieceMoves.GetPieceColor(targetedPiece) == Position.colorToMove;
		if (canSwitchSelectedTile)
		{
			Colors.SetTileColors(targetedPiece);
			return;
		}
		if (selectedTile == null)
			return;
		Vector2I selectedTileFlat = (selectedTile ?? default).Location;
		UpdatePosition.MovePiece(selectedTileFlat, targetedPiece);
	}
	public static void Deselect(Vector2I start)
	{
		PreviousMoveTiles(Colors.Enum.Default);
		Colors.Set(GetTile(start), Colors.Enum.Default, start.X, start.Y);
		foreach ((Vector2I start, Vector2I end) startEndTiles in LegalMoves.legalMoves)
		{
			if (startEndTiles.start == start)
				Colors.Set(GetTile(startEndTiles.end), Colors.Enum.Default, startEndTiles.end.X, startEndTiles.end.Y);
		}
		Colors.ColorCheckedRoyalTiles(Colors.Enum.Check);
		selectedTile = null;
	}
	public static void PreviousMoveTiles(Colors.Enum color)
	{
		if (Position.LastMoveInfo == null)
			return;
		(Vector2I start, Vector2I end) lastMoveNotNull = Position.LastMoveInfo ?? default;
		Colors.Set(color, lastMoveNotNull.start.X, lastMoveNotNull.start.Y);
		Colors.Set(color, lastMoveNotNull.end.X, lastMoveNotNull.end.Y);
	}
	public static Sprite2D GetTile(Vector2I location)
	{
		return tiles[new(location.X, location.Y, 0)];
	}
}
