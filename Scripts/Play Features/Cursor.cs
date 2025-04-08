using Godot;
using Godot.Collections;

public partial class Cursor
{
	public static bool cooldownOngoing = false, wasEnterPressed = false, enterPressedNow = false, locationInitialized = false, CursorMovedInRow = false, FirstMovedTimerActive = false;
	private const float cursorMoveCooldown = 0.14f, FirstMoveInRowCooldown = 0.055f;
	private static float moveCooldownMultiplier = 1;
	private static bool CursorShown = false;
	public static Vector2I actualLocation, cursorDirectionField = new();
	private static Sprite2D cursor;
	public static void KeyPressDetection()
	{
		if (cooldownOngoing) return;
		if (CursorShown && Interaction.escapePressed && !Interaction.lastEscapeSelection) { ShowHideCursor(false); return; }
		bool enterPressed = Input.IsKeyPressed(Key.Enter);
		enterPressedNow = !wasEnterPressed && enterPressed && CursorShown;
		wasEnterPressed = enterPressed;
		if (enterPressed)
		{
			if (!CursorShown) ShowHideCursor(true);
			return;
		}
		if (Input.IsKeyPressed(Key.Left)) { cursorDirectionField.X = -1; }
		if (Input.IsKeyPressed(Key.Right)) { cursorDirectionField.X = cursorDirectionField.X == -1 ? 0 : 1; }
		if (Input.IsKeyPressed(Key.Up)) { cursorDirectionField.Y = -1; }
		if (Input.IsKeyPressed(Key.Down)) { cursorDirectionField.Y = cursorDirectionField.Y == -1 ? 0 : 1; }
		if (cursorDirectionField != Vector2I.Zero) CursorInput(cursorDirectionField); else CursorMovedInRow = false;
	}
	private static void ShowHideCursor(bool show)
	{
		CursorShown = show;
		Animations.Tween(cursor, Animations.animationSpeed, actualLocation, null, null, show ? 1 : 0, false);
	}
	private static void CursorInput(Vector2I cursorDirection)
	{
		if (!CursorMovedInRow) History.TimerCountdown(FirstMoveInRowCooldown, History.TimerType.FirstCursorMove);
		CursorMovedInRow = true;
		if (FirstMovedTimerActive) return;
		moveCooldownMultiplier = Input.IsKeyPressed(Key.Shift) ? 0.5f : 1;
		History.TimerCountdown(cursorMoveCooldown * moveCooldownMultiplier, History.TimerType.Cursor);
		if (!CursorShown)
			ShowHideCursor(true);
		IsOutOfBounds(actualLocation + cursorDirection, out bool hitXWall, out bool hitYWall);
		MoveCursor(actualLocation + cursorDirection, cursorMoveCooldown * moveCooldownMultiplier, hitXWall, hitYWall);
		cursorDirectionField = new();

	}
	private static void MoveCursor(Vector2I position, float duration, bool hitXWall = false, bool hitYWall = false)
	{
		if (hitXWall) position.X = actualLocation.X;
		if (hitYWall) position.Y = actualLocation.Y;
		if (position == actualLocation) return;
		Animations.Tween(cursor, duration, actualLocation, actualLocation = position, null, null, false, false, false, -1, -1, false, Tween.TransitionType.Linear, null);
	}
	private static void IsOutOfBounds(Vector2I location, out bool X, out bool Y)
	{
		X = false; Y = false;
		if (location.X < 0) X = true; if (location.X >= Chessboard.tileCount.X) X = true;
		if (location.Y < 0) Y = true; if (location.Y >= Chessboard.tileCount.Y) Y = true;
	}
	public static void GetCursor()
	{
		cursor = Chessboard.tiles[new(0, 0, Chessboard.Layer.Cursor)];
	}
}
