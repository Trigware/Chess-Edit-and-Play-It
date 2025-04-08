using Godot;
using Godot.Collections;

public partial class Cursor
{
	public static Dictionary<char, Vector2I> Location = new(), RoyalPieceInitialPosition = new();
	public static bool cooldownOngoing = false, wasEnterPressed = false, enterPressedNow = false;
	private const float cursorMoveCooldown = 0.125f;
	private static float moveCooldownMultiplier = 1;
	private static bool CursorShown = false;
	public static Vector2I actualLocation;
	private static Sprite2D cursor;
	public static Vector2I InitializeLocation(char colorToMove)
	{
		Vector2I initialCursorPosition = RoyalPieceInitialPosition[colorToMove];
		return initialCursorPosition;
	}
	public static void KeyPressDetection()
	{
		if (cooldownOngoing) return;
		if (CursorShown && Interaction.escapePressed && !Interaction.lastEscapeSelection) { ShowHideCursor(false); return; }
		bool enterPressed = Input.IsKeyPressed(Key.Enter);
		enterPressedNow = !wasEnterPressed && enterPressed && CursorShown;
        if (enterPressedNow)
            Location[Position.colorToMove] = actualLocation;
		wasEnterPressed = enterPressed;
		if (enterPressed) return;
		Vector2I cursorDirection = new();
		if (Input.IsKeyPressed(Key.Left)) cursorDirection.X--;
		if (Input.IsKeyPressed(Key.Right)) cursorDirection.X++;
		if (Input.IsKeyPressed(Key.Up)) cursorDirection.Y--;
		if (Input.IsKeyPressed(Key.Down)) cursorDirection.Y++;
		if (cursorDirection != Vector2I.Zero)
		{
			moveCooldownMultiplier = Input.IsKeyPressed(Key.Shift) ? 0.5f : 1;
			History.MoveReplayCooldown(cursorMoveCooldown * moveCooldownMultiplier, false);
			if (!CursorShown)
				ShowHideCursor(true);
			IsOutOfBounds(actualLocation + cursorDirection, out bool hitXWall, out bool hitYWall);
			MoveCursor(actualLocation + cursorDirection, cursorMoveCooldown * moveCooldownMultiplier, hitXWall, hitYWall);
		}
    }
	public static void GetCursor()
	{
		actualLocation = Location[Position.colorToMove];
		cursor = Chessboard.tiles[new(actualLocation, Chessboard.Layer.Cursor)];
	}
	private static void ShowHideCursor(bool show)
	{
		CursorShown = show;
        Animations.Tween(cursor, Animations.animationSpeed, actualLocation, null, null, show ? 1 : 0, false);
    }
	private static void MoveCursor(Vector2I position, float duration, bool hitXWall = false, bool hitYWall = false)
	{
		if (hitXWall) position.X = actualLocation.X;
		if (hitYWall) position.Y = actualLocation.Y;
		if (position == actualLocation) return;
		Animations.Tween(cursor, duration, actualLocation, actualLocation = position, null, null, false, false, false, -1, -1, false, Tween.TransitionType.Linear, null);
        SetCursorLocation(actualLocation);
    }
    private static void SetCursorLocation(Vector2I location)
	{
		Chessboard.tiles.Remove(new(actualLocation, Chessboard.Layer.Cursor));
		Chessboard.tiles.Add(new(location, Chessboard.Layer.Cursor), cursor);
	}
	private static void IsOutOfBounds(Vector2I location, out bool X, out bool Y)
	{
		X = false; Y = false;
		if (location.X < 0) X = true; if (location.X >= Chessboard.tileCount.X) X = true;
        if (location.Y < 0) Y = true; if (location.Y >= Chessboard.tileCount.Y) Y = true;
	}
}