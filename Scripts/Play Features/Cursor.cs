using System.Collections.Generic;
using Godot;

public partial class Cursor
{
	public static Dictionary<char, Vector2I> Location = new();
	public static bool cooldownOngoing = false, wasEnterPressed = false, enterPressedNow = false, CursorMovedInRow = false, FirstMovedTimerActive = false;
	private const float cursorMoveCooldown = 0.115f, FirstMoveInRowCooldown = 0.06f;
	private static bool CursorShown = false;
	public static Vector2I actualLocation, cursorDirectionField = new();
	private static Sprite2D cursor;
	public static void KeyPressDetection()
	{
        if (cooldownOngoing || Chessboard.waitingForBoardFlip || Position.GameEndState != Position.EndState.Ongoing) return;
		if (CursorShown && Interaction.escapePressed && !Interaction.lastEscapeSelection) { ShowHideCursor(false); return; }
		bool enterPressed = Input.IsKeyPressed(Key.Enter);
		enterPressedNow = !wasEnterPressed && enterPressed && CursorShown;
		wasEnterPressed = enterPressed;
		if (enterPressed && !CursorShown) ShowHideCursor(true);
		if (Input.IsKeyPressed(Key.Left)) { cursorDirectionField.X = -1; }
		if (Input.IsKeyPressed(Key.Right)) { cursorDirectionField.X = cursorDirectionField.X == -1 ? 0 : 1; }
		if (Input.IsKeyPressed(Key.Up)) { cursorDirectionField.Y = -1; }
		if (Input.IsKeyPressed(Key.Down)) { cursorDirectionField.Y = cursorDirectionField.Y == -1 ? 0 : 1; }
		if (cursorDirectionField != Vector2I.Zero) CursorInput(cursorDirectionField); else CursorMovedInRow = false;
	}
	public static void ShowHideCursor(bool show)
	{
		CursorShown = show;
		Animations.Tween(cursor, Animations.animationSpeed, actualLocation, null, null, show ? 1 : 0, false);
	}
	private static void CursorInput(Vector2I cursorDirection)
	{
		if (!CursorMovedInRow) History.TimerCountdown(FirstMoveInRowCooldown, History.TimerType.FirstCursorMove);
		CursorMovedInRow = true;
		if (FirstMovedTimerActive) return;
		cursorDirection *= Chessboard.isFlipped ? -1 : 1;
		float moveCooldownMultiplier = Input.IsKeyPressed(Key.Shift) ? 0.5f : 1;
		History.TimerCountdown(cursorMoveCooldown * moveCooldownMultiplier, History.TimerType.Cursor);
		if (!CursorShown) ShowHideCursor(true);
		Vector2I requestedDestination = actualLocation + cursorDirection;
        if (!IsOutOfBounds(requestedDestination, out bool outOfBoundsX, out bool outOfBoundsY)) MoveCursor(requestedDestination, cursorMoveCooldown * moveCooldownMultiplier, outOfBoundsX, outOfBoundsY);
		cursorDirectionField = new();
	}
    private static bool IsOutOfBounds(Vector2I location, out bool X, out bool Y)
    {
        X = false; Y = false;
        if (location.X < 0) X = true; if (location.X >= Chessboard.tileCount.X) X = true;
        if (location.Y < 0) Y = true; if (location.Y >= Chessboard.tileCount.Y) Y = true;
		return X && Y;
    }
	public static void MoveCursor(Vector2I position, float duration, bool outOfBoundsX = false, bool outOfBoundsY = false)
	{
		if ((outOfBoundsX || outOfBoundsY) && Interaction.selectedTile != null) return; // remove when implementing tile snapping
		if (outOfBoundsX) position.X = actualLocation.X;
		if (outOfBoundsY) position.Y = actualLocation.Y;
		if (position == actualLocation) return;
        Animations.Tween(cursor, duration, actualLocation, actualLocation = position, null, null, false, false, false, -1, -1, false, Tween.TransitionType.Linear, null);
    }
	public static void GetCursor()
	{
		cursor = Chessboard.tiles[new(0, 0, Chessboard.Layer.Cursor)];
	}
}
