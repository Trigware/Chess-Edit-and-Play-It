using Godot;
using System;
using System.Collections.Generic;

public partial class Interaction : Chessboard
{
	public static Element? selectedTile = null;
	public static Text.PauseLabel InteractionTextHoveredOver = Text.PauseLabel.Unknown;
	public static List<Vector2I> LegalSelectedMoves = new();
	public static bool escapePressed, lastEscapeSelection = false, escapePressedOld = false;
	private static Vector2 MousePosition;
	private static bool InteractionButtonPressed = false, leftMouseOld = false, pausedPressedOld = false, leftMousePressStartNow = false;
	public override void _Process(double delta)
	{
		MousePosition = GetViewport().GetMousePosition();
		History.KeyPressDetection();
		BoardInteractionInputs();
	}
	private void BoardInteractionInputs()
	{
		if (PauseMenu.MenuMoving)
			Text.RefreshPauseText();
		InteractWithPauseMenuText();
		bool leftActuallyPressed = Input.IsMouseButtonPressed(MouseButton.Left), pauseKeyPressedNow = Input.IsKeyPressed(Key.P);
		if (pauseKeyPressedNow && !pausedPressedOld)
			PauseMenu.IsPaused = !PauseMenu.IsPaused;
		escapePressed = Input.IsKeyPressed(Key.Escape);
		if (escapePressed) { if (selectedTile != null) lastEscapeSelection = true; } else lastEscapeSelection = false;
		Cursor.KeyPressDetection();
		leftMousePressStartNow = leftActuallyPressed && !leftMouseOld;
		InteractionButtonPressed = leftMousePressStartNow || Cursor.enterPressedNow;
		if ((InteractionButtonPressed || escapePressed) && Position.GameEndState == Position.EndState.Ongoing && !PauseMenu.IsPaused)
			Select(leftMousePressStartNow);
		leftMouseOld = leftActuallyPressed;
		escapePressedOld = escapePressed;
		pausedPressedOld = pauseKeyPressedNow;
	}
	private Vector2I GetPositionOnBoard()
	{
		Vector2 leftUpNotNull = leftUpCorner;
		Vector2 tileSelectionPosition = ((MousePosition - leftUpNotNull) / actualTileSize).Floor();
		if (isFlipped)
			tileSelectionPosition *= -1;
		return (Vector2I)tileSelectionPosition;
	}
	private void InteractWithPauseMenuText()
	{
		if ((!PauseMenu.IsPaused && !PauseMenu.MenuMoving) || !leftMousePressStartNow) return;
		Text.PauseLabel lastPauseLabel = InteractionTextHoveredOver;
		InteractionTextHoveredOver = Text.PauseLabel.Unknown;
		foreach (KeyValuePair<Text.PauseLabel, Rect2> pauseInteractionHitbox in PauseMenu.InteractionHitboxes)
		{
			if (pauseInteractionHitbox.Value.HasPoint(MousePosition))
			{
                PauseMenu.InteractWithTextElement(pauseInteractionHitbox.Key);
				return;
			}
		}
		HandleCloseButton();
	}
	private void HandleCloseButton()
	{
		Vector2 topLeft = PauseMenu.GetPointOfCloseButton(true), bottomRight = PauseMenu.GetPointOfCloseButton(false);
        Rect2 closeButtonHitbox = new(topLeft, bottomRight - topLeft);
		if (closeButtonHitbox.HasPoint(MousePosition))
			PauseMenu.IsPaused = !PauseMenu.IsPaused;
	}
	private void Select(bool leftMousePressStartNow)
	{
		if (waitingForBoardFlip || PauseMenu.UndoingMovesForNewGame || History.cooldownOngoing) return;
		Vector2I interactionPosition = leftMousePressStartNow ? GetPositionOnBoard() : Cursor.actualLocation;
		if (InteractionButtonPressed && Promotion.PromotionOptionsPositions.Contains(interactionPosition))
		{
			waitingForBoardFlip = true;
			Cursor.MoveCursor(Promotion.originalPromotionPosition, 0);
			Promotion.Promote(interactionPosition);
			return;
		}
		if (Position.ColorToMove == '\0')
			return;
		Element mousePositionBoard = new(interactionPosition, Layer.Tile);
		if (selectedTile != null && (Input.IsKeyPressed(Key.Escape) || (InteractionButtonPressed && (selectedTile ?? default).Location == mousePositionBoard.Location)))
		{
			Deselect((selectedTile ?? default).Location);
			PreviousMoveTiles(Colors.Enum.PreviousMove);
			return;
		}
		if (InteractionButtonPressed)
			InteractWithPiece(interactionPosition);
	}
	private static void InteractWithPiece(Vector2I targetedLocation)
	{
		bool canSwitchSelectedTile = LegalMoves.GetPieceColor(targetedLocation) == Position.ColorToMove;
		if (canSwitchSelectedTile)
		{
			TimeControl.HandleTimerPauseProperty(Position.ColorToMove);
			Colors.SetTileColors(targetedLocation);
		}
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
