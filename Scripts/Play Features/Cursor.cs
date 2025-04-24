using System.Collections.Generic;
using Godot;
using static Godot.TextServer;

public partial class Cursor
{
	public static Dictionary<char, Vector2I> Location = new();
	public static List<Vector2I> LegalSelectedDirections = new();
	public static bool cooldownOngoing = false, wasEnterPressed = false, enterPressedNow = false, CursorMovedInRow = false, FirstMovedTimerActive = false;
	private const float cursorMoveCooldown = 0.115f, FirstMoveInRowCooldown = 0.06f;
	private static bool CursorShown = false;
	public static Vector2I actualLocation, cursorDirectionField = new();
	private static Vector2I? previousDirection = null, previousInputDirection = null;
	private static Sprite2D cursor;
	private static readonly List<(int eval, Vector2I dir)> directionPriority = new() { (4, new(0, -1)), (2, new(-1, 0)), (2, new(1, 0)), (1, new(0, 1)) };
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
		if (cursorDirectionField != Vector2I.Zero) CursorInput(cursorDirectionField); else { CursorMovedInRow = false; previousDirection = null; previousInputDirection = null; }
	}
	public static void ShowHideCursor(bool show)
	{
		CursorShown = show;
		Animations.Tween(cursor, Animations.animationSpeed, actualLocation, null, null, show ? 1 : 0, false, layer: Chessboard.Layer.Cursor);
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
		if (!IsOutOfBounds(requestedDestination, out bool outOfBoundsX, out bool outOfBoundsY)) MoveCursor(actualLocation, cursorMoveCooldown * moveCooldownMultiplier, outOfBoundsX, outOfBoundsY, cursorDirection, Interaction.selectedTile != null && cursorDirection != Vector2I.Zero);
		cursorDirectionField = new();
	}
	private static bool IsOutOfBounds(Vector2I location, out bool X, out bool Y)
	{
		X = false; Y = false;
		bool promotionOptions = Promotion.PromotionOptionsPositions.Count > 0 && !Promotion.PromotionOptionsPositions.Contains(location);
        if (Interaction.selectedTile != null) return false;
		if (location.X < 0 || location.X >= Chessboard.tileCount.X || promotionOptions) X = true;
		if (location.Y < 0 || location.Y >= Chessboard.tileCount.Y || promotionOptions) Y = true;
		return X && Y;
	}
	public static void MoveCursor(Vector2I position, float duration, bool outOfBoundsX = false, bool outOfBoundsY = false, Vector2I direction = default, bool moveSnapping = false)
	{
		Vector2I actualDirection = moveSnapping ? SnapCursorToAvailableMove(position, direction) : direction;
		if (moveSnapping && actualDirection != Vector2I.Zero) previousDirection = actualDirection;
		position += actualDirection;
		if (outOfBoundsX) position.X = actualLocation.X;
		if (outOfBoundsY) position.Y = actualLocation.Y;
		if (position == actualLocation) return;
		previousInputDirection = direction;
		Animations.Tween(cursor, duration, actualLocation, actualLocation = position, null, null, false, transition: Tween.TransitionType.Linear, easeType: null, layer: Chessboard.Layer.Cursor);
	}
	private static Vector2I SnapCursorToAvailableMove(Vector2I position, Vector2I direction)
	{
		UpdateLegalSelectedMoves(position);
		if (previousDirection != null && direction == previousInputDirection)
		{
			if (LegalSelectedDirections.Contains(previousDirection ?? default)) return previousDirection ?? default;
			return Vector2I.Zero;
        }
		if (LegalSelectedDirections.Count == 1)
		{
			Interaction.Deselect((Interaction.selectedTile ?? default).Location);
			Interaction.PreviousMoveTiles(Colors.Enum.PreviousMove);
            return direction;
        }
        List<Vector2I> availableMovesInDirection = new();
		foreach (Vector2I selectedDirection in LegalSelectedDirections)
		{
			bool sameXDir = IsSameSign(direction.X, selectedDirection.X) || direction.X == 0, sameYDir = IsSameSign(direction.Y, selectedDirection.Y) || direction.Y == 0;
            if (sameXDir && sameYDir) availableMovesInDirection.Add(selectedDirection);
		}
		if (availableMovesInDirection.Count == 0) return Vector2I.Zero;
		availableMovesInDirection = CalculateClosestMove(availableMovesInDirection, direction);
		if (availableMovesInDirection.Count > 1) return BreakOptionTie(position, availableMovesInDirection);
		return availableMovesInDirection[0];
	}
	private static void UpdateLegalSelectedMoves(Vector2I location)
	{
		LegalSelectedDirections = new();
		foreach (Vector2I selectedMove in Interaction.LegalSelectedMoves)
			LegalSelectedDirections.Add(selectedMove - location);
		LegalSelectedDirections.Add((Interaction.selectedTile ?? default).Location - location);
	}
	private static List<Vector2I> CalculateClosestMove(List<Vector2I> availableMovesInDirection, Vector2I direction)
	{
		List<Vector2I> closestMoves = new();
		float shortestDirection = float.MaxValue;
		foreach (Vector2I moveDir in availableMovesInDirection)
		{
			float distance = EuclideanDistance(direction, moveDir);
			if (distance < shortestDirection)
			{
				closestMoves = new();
				shortestDirection = distance;
				closestMoves.Add(moveDir);
			}
			else if (distance == shortestDirection) closestMoves.Add(moveDir);
		}
		return closestMoves;
	}
	private static Vector2I BreakOptionTie(Vector2I position, List<Vector2I> remainingOptions)
	{
		int bestEvaluatedOption = int.MinValue;
		Vector2I bestEvaluatedDirection = default;
		foreach (Vector2I option in remainingOptions)
		{
			int optionEval = OptionEvaluation(position, option);
			if (optionEval > bestEvaluatedOption)
			{
				bestEvaluatedOption = optionEval;
				bestEvaluatedDirection = option;
			}
		}
		return bestEvaluatedDirection;
	}
	private static float EuclideanDistance(Vector2I start, Vector2I end) => Mathf.Sqrt(Mathf.Pow(end.X - start.X, 2) + Mathf.Pow(end.Y - start.Y, 2));
	private static int OptionEvaluation(Vector2I position, Vector2I direction)
	{
		int optionEval = 0;
		bool isCloserToCenter = IsCloserToCenter(position.X, direction.X);
		foreach ((int eval, Vector2I dir) dirEval in directionPriority)
		{
			Vector2I prioritizedDirection = dirEval.dir;
			if (Chessboard.isFlipped) prioritizedDirection.Y *= -1;
			if (IsSameSign(direction.X, prioritizedDirection.X) && prioritizedDirection.X != 0) optionEval += direction.X / prioritizedDirection.X * (dirEval.eval + (isCloserToCenter ? 1 : 0));
			if (IsSameSign(direction.Y, prioritizedDirection.Y) && prioritizedDirection.Y != 0) optionEval += direction.Y / prioritizedDirection.Y * dirEval.eval;
		}
		return optionEval;
	}
	private static bool IsSameSign(int a, int b) => a * b > 0;
	private static bool IsCloserToCenter(int axisPosition, int axisDirection) => Mathf.Abs(Chessboard.boardCenter.X - axisPosition - axisDirection) < Mathf.Abs(Chessboard.boardCenter.X - axisPosition);
	public static void SetCursor() => cursor = Chessboard.tiles[new(0, 0, Chessboard.Layer.Cursor)];
}
