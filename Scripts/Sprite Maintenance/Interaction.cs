using Godot;
using System;
using System.Collections.Generic;

public partial class Interaction : Chessboard
{
	public static Element? selectedTile = null;
	public static List<Vector2I> LegalSelectedMoves = new();
	public static bool escapePressed, lastEscapeSelection = false;
	private bool interactionButtonPressed = false, leftMouseOld = false;
	public override void _Process(double delta)
	{
		History.KeyPressDetection();
		bool leftActuallyPressed = Input.IsMouseButtonPressed(MouseButton.Left);
		escapePressed = Input.IsKeyPressed(Key.Escape);
		if (escapePressed) { if (selectedTile != null) lastEscapeSelection = true; } else lastEscapeSelection = false;
		Cursor.KeyPressDetection();
		bool leftMousePressStartNow = leftActuallyPressed && !leftMouseOld;
		interactionButtonPressed = leftMousePressStartNow || Cursor.enterPressedNow;
		if ((interactionButtonPressed || escapePressed) && Position.GameEndState == Position.EndState.Ongoing)
			Select(leftMousePressStartNow);
		leftMouseOld = leftActuallyPressed;
	}
	private Vector2I GetPositionOnBoard()
	{
		Vector2 leftUpNotNull = leftUpCorner;
		Vector2 mousePosition = GetViewport().GetMousePosition();
		Vector2 tileSelectionPosition = ((mousePosition - leftUpNotNull) / actualTileSize).Floor().Abs();
		GD.Print(tileSelectionPosition);
		return (Vector2I)tileSelectionPosition;
	}
	private void Select(bool leftMousePressStartNow)
	{
		if (waitingForBoardFlip) return;
		Vector2I interactionPosition = leftMousePressStartNow ? GetPositionOnBoard() : Cursor.actualLocation;
		if (interactionButtonPressed && Promotion.PromotionOptionsPositions.Contains(interactionPosition))
		{
			waitingForBoardFlip = true;
			Cursor.MoveCursor(Promotion.originalPromotionPosition, 0);
			Promotion.Promote(interactionPosition);
			return;
		}
		if (Position.colorToMove == '\0')
			return;
		Element mousePositionBoard = new(interactionPosition, Layer.Tile);
		if (selectedTile != null && (Input.IsKeyPressed(Key.Escape) || (interactionButtonPressed && (selectedTile ?? default).Location == mousePositionBoard.Location)))
		{
			Deselect((selectedTile ?? default).Location);
			PreviousMoveTiles(Colors.Enum.PreviousMove);
			return;
		}
		if (interactionButtonPressed)
			InteractWithPiece(interactionPosition);
	}
	private static void InteractWithPiece(Vector2I targetedLocation)
	{
		bool canSwitchSelectedTile = PieceMoves.GetPieceColor(targetedLocation) == Position.colorToMove;
		if (canSwitchSelectedTile) Colors.SetTileColors(targetedLocation);
		if (canSwitchSelectedTile || LegalSelectedMoves.Contains(targetedLocation)) Cursor.MoveCursor(targetedLocation, 0);
		Vector2I selectedTileFlat = (selectedTile ?? default).Location;
		if (LegalMoves.legalMoves.Contains((selectedTileFlat, targetedLocation)))
			UpdatePosition.MovePiece(selectedTileFlat, targetedLocation);
		else if (LegalSelectedMoves.Count == 1)
			Cursor.MoveCursor(LegalSelectedMoves[0], 0);
	}
	public static void Deselect(Vector2I start)
	{
		LegalSelectedMoves = new();
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
		return tiles[new(location.X, location.Y, Layer.Tile)];
	}
}
