using Godot;
using System.Collections.Generic;

public partial class UpdatePosition
{
	public static void MovePiece(Vector2I start, Vector2I end, int legalIndex)
	{
		int leapMoveIndex = LegalMoves.PawnLeapMoves.IndexOf(legalIndex);
		int enPassantIndex = LegalMoves.EnPassantMoves.IndexOf(legalIndex);
		int promotionIndex = LegalMoves.PromotionMoves.IndexOf(legalIndex);
		int castlingIndex = LegalMoves.CastlingMoves.IndexOf(legalIndex);

		bool enPassant = Position.EnPassantInfo.delete != -Vector2I.One && enPassantIndex > -1;

		if (enPassant)
			DeletePiece(Position.EnPassantInfo.delete, null, true, true, '\0', null, true);
		if (leapMoveIndex > -1)
			Position.EnPassantInfo = LegalMoves.PawnLeapMovesInfo[leapMoveIndex];
		else
			Position.EnPassantInfo = new(-Vector2I.One, -Vector2I.One);

		if (Chessboard.tiles.ContainsKey(new(end.X, end.Y, 1)) || Position.pieces[start].ToString().ToLower() == "p")
			Position.HalfmoveClock = 0;
		else
			Position.HalfmoveClock++;
		if (Position.colorToMove == Position.oppositeStartColorToMove)
			Position.FullmoveNumber++;

		Tags.ModifyRoyalPieceList(start, end);
		Sprite2D handledSprite = GetPiece(start);
		EditPiecePositions(start, end, handledSprite, !enPassant && castlingIndex == -1, promotionIndex > -1, castlingIndex > -1);
		if (castlingIndex > -1)
			MoveCastlee(LegalMoves.CastleeMoves[castlingIndex]);

		Interaction.Deselect(start);
		if (Position.InCheck && PieceMoves.SuccessfulResponseInEveryZone(end))
			Colors.ColorCheckedRoyalTiles(Colors.Enum.Default);
		Position.LastMoveInfo = (start, end);
		if (promotionIndex == -1 || Promotion.PromotionOptionsPieces.Count == 0)
			Interaction.PreviousMoveTiles(Colors.Enum.PreviousMove);
		if (promotionIndex == -1)
		{
			LegalMoves.ReverseColor(Position.colorToMove);
			LegalMoves.GetLegalMoves();
		}
		else
		{
			LegalMoves.legalMoves = new();
			if (Promotion.CanBePromotedTo.Length > 1)
			{
				Promotion.AvailablePromotions(end, Position.colorToMove);
				Position.colorToMove = '\0';
			}
			else
				Promotion.AutomaticPromotion(end);
		}
		DiscoveredCheckAnimation(start);
	}
	public static void DeletePiece(Vector2I start, Vector2I? end, bool playSound, bool animation = true, char replace = '\0', Sprite2D handledSprite = null, bool enPassant = false)
	{
		Vector2I endUsed = start;
		if (end != null)
			endUsed = (Vector2I)end;
		if (end == -Vector2I.One)
			return;
		try
		{
			if (animation)
			{
				if (enPassant)
                    Animations.Tween(Chessboard.tiles[new(endUsed.X, endUsed.Y, 1)], Animations.animationSpeed, start, null, null, 0, true);
                else
                    Animations.Tween(Chessboard.tiles[new(endUsed.X, endUsed.Y, 1)], Animations.animationSpeed, start, null, new(), null, true);
            }
            if (replace == '\0')
			{
				Position.pieces.Remove(endUsed);
				Chessboard.tiles.Remove(new(endUsed.X, endUsed.Y, 1));
			}
			else
			{
				Position.pieces[endUsed] = replace;
				Chessboard.tiles[new(endUsed.X, endUsed.Y, 1)] = handledSprite;
			}
		}
		catch { }
		if (playSound)
			Audio.Play(Audio.Enum.Capture);
	}
	private static void EditPiecePositions(Vector2I start, Vector2I end, Sprite2D handledSprite, bool playSound, bool promotion, bool castling)
	{
		char handledPiece = Position.pieces[start];
		Tags.ModifyTags(start, end, handledPiece);
		Position.pieces.Remove(start);
		Chessboard.tiles.Remove(new(start.X, start.Y, 1));
		Vector3I endPiece = new(end.X, end.Y, 1);
		bool capture = Chessboard.tiles.ContainsKey(endPiece);
		if (!capture)
		{
			Position.pieces.Add(end, handledPiece);
			Chessboard.tiles.Add(endPiece, handledSprite);
			if (playSound)
				Audio.Play(Audio.Enum.Move);
		}
		else
			DeletePiece(start, end, playSound, true, handledPiece, handledSprite);
		if (castling)
			Castling.TweenCastle(handledSprite, Animations.animationSpeed, start, end.X);
		else
			Animations.Tween(handledSprite, Animations.animationSpeed, start, end, null, promotion ? 0 : null, promotion);
	}
	private static void MoveCastlee((Vector2I start, Vector2I end) castleePosition)
	{
		EditPiecePositions(castleePosition.start, castleePosition.end, Chessboard.tiles[new(castleePosition.start.X, castleePosition.start.Y, 1)], false, false, false);
		Audio.Play(Audio.Enum.Castle);
	}
	private static Sprite2D GetPiece(Vector2I location)
	{
		return Chessboard.tiles[new(location.X, location.Y, 1)];
	}
	private static void DiscoveredCheckAnimation(Vector2I start)
	{
		for (int i = 0; i < LegalMoves.CheckResponseZones.Count; i++)
		{
			List<Vector2I> zone = LegalMoves.CheckResponseZones[i];
			foreach (Vector2I tile in zone)
			{
				if (start == tile)
                    Animations.CheckAnimation(1, ((SceneTree)Engine.GetMainLoop()).CurrentScene, i);
            }
        }
	}
}
