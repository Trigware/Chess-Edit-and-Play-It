using Godot;
using System;
using System.Collections.Generic;

public partial class Castling : Node
{
	private const int elipseQualityConst = 65;
	public static int elipseQuality;
	public static List<int> endXpositions = new();
	public static List<bool> elipsePathUp = new();
	public static List<(Vector2I, Vector2I)> IsLegal(Vector2I castlerPosition, char color, List<Vector2I> opponentMoves, int moveCount)
	{
		if (opponentMoves.Contains(castlerPosition))
			return new();
		List<(Vector2I, Vector2I)> castlingMoves = new();
		for (int castlingDirection = -1; castlingDirection <= 1; castlingDirection += 2)
		{
			Vector2I castleePosition = new(castlingDirection == -1 ? 0 : Chessboard.tileCount.X - 1, castlerPosition.Y);
			if (!Position.pieces.ContainsKey(castleePosition))
				continue;
			int tagIndex = Tags.tagPositions.IndexOf(castleePosition);
			if (tagIndex == -1)
				continue;
			bool castleeExists = Tags.activeTags[tagIndex].Contains(Tags.Tag.Castlee);
			if (!castleeExists)
				continue;
			bool loopBroken = false;
			for (Vector2I handledPosition = new(castleePosition.X + castlingDirection * -1, castleePosition.Y); handledPosition != castlerPosition; handledPosition.X += castlingDirection * -1)
			{
				if (Position.pieces.ContainsKey(handledPosition) || opponentMoves.Contains(handledPosition))
				{
					loopBroken = true;
					break;
				}
			}
			if (!loopBroken)
			{
				castlingMoves.Add((castlerPosition, new(castlerPosition.X + 2 * castlingDirection, castlerPosition.Y)));
				LegalMoves.CastlingMoves.Add(moveCount + castlingMoves.Count - 1);
				LegalMoves.CastleeMoves.Add((castleePosition, new(castleePosition.X + (castlingDirection == -1 ? 3 : -2), castleePosition.Y)));
			}
		}
		return castlingMoves;
	}
	public static void TweenCastle(Sprite2D spr, float duration, Vector2I startPosition, int endXLocal, bool isRedo)
	{
		if (duration < Animations.lowAnimationDurationBoundary || !CanDoElipseAnimation(startPosition, endXLocal, duration, isRedo))
		{
			Animations.Tween(spr, duration, startPosition, new(endXLocal, startPosition.Y), null, null, false);
			return;
		}
		bool elipseUp = Position.colorToMove == (isRedo ? 'b' : 'w');
        Animations.CancelCastlingEarly = false;
		endXpositions.Add(endXLocal);
		elipsePathUp.Add(elipseUp);
		elipseQuality = Convert.ToInt32(elipseQualityConst * Animations.animationSpeed);
		Animations.Tween(spr, duration / elipseQuality, startPosition, CalculatePointOnElipse(1, startPosition, endXLocal, elipseUp), null, null, false, false, true, 1, endXpositions.Count-1);
	}
	private static bool CanDoElipseAnimation(Vector2I startPosition, int endXLocal, float duration, bool isRedo)
	{
		if (duration < Animations.lowAnimationDurationBoundary)
			return false;
		bool elipsePath = true;
		bool shortCastle = endXLocal > startPosition.X;
		for (int x = startPosition.X; shortCastle ? x <= endXLocal : x >= endXLocal; x += shortCastle ? 1 : -1)
		{
			Vector2I scannedLocation = new(x, startPosition.Y + (Position.colorToMove == (isRedo ? 'w' : 'b') ? 1 : -1));
            if (Position.pieces.ContainsKey(scannedLocation))
			{
				elipsePath = false;
				break;
			}
		}
		return elipsePath;
	}
	public static Vector2 CalculatePointOnElipse(int elipsePointUnit, Vector2I start, int endX, bool elipseUp)
	{
		int direction = 1;
		if (endX > start.X)
		{
			endX = start.X - endX;
			direction = -1;
		}
		else
			endX -= start.X;

		float angle = (float)elipsePointUnit / elipseQuality * Mathf.Pi;
		float xRadius = Mathf.Abs(endX) / 2f;
		float yRadius = elipseUp ? 1 : -1;

		float xPoint = xRadius * Mathf.Cos(angle) - xRadius;
		float yPoint = yRadius * Mathf.Sin(angle);
        return start + new Vector2(xPoint * direction, -yPoint);
	}
}
