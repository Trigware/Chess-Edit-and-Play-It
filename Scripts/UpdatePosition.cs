using Godot;
using System.Collections.Generic;

public partial class UpdatePosition : Node
{
	public static void MovePiece(Vector2I start, Vector2I end, int legalIndex)
	{
        int leapMoveIndex = LegalMoves.PawnLeapMoves.IndexOf(legalIndex);
		int enPassantIndex = LegalMoves.EnPassantMoves.IndexOf(legalIndex);
		int promotionIndex = LegalMoves.PromotionMoves.IndexOf(legalIndex);
		int castlingIndex = LegalMoves.CastlingMoves.IndexOf(legalIndex);

		bool enPassant = Position.EnPassantInfo.delete != -Vector2I.One && enPassantIndex > -1;
		if (enPassant)
			DeletePiece(Position.EnPassantInfo.delete, null, true);
		if (leapMoveIndex > -1)
			Position.EnPassantInfo = LegalMoves.PawnLeapMovesInfo[leapMoveIndex];
		else
			Position.EnPassantInfo = new(-Vector2I.One, -Vector2I.One);

        Position.ModifyRoyalPieceList(start, end);
		Sprite2D handledSprite = GetPiece(start);
		EditPiecePositions(start, end, handledSprite, !enPassant && castlingIndex == -1, promotionIndex > -1, castlingIndex > -1);
		if (castlingIndex > -1)
			MoveCastlee(LegalMoves.CastleeMoves[castlingIndex]);

		Interaction.Deselect(start);
		Position.LastMoveInfo = (start, end);
        if (promotionIndex == -1)
		{
			Position.ReverseColor(Position.colorToMove);
			Interaction.PreviousMoveTiles(Colors.Enum.PreviousMove);
			LegalMoves.GetLegalMoves();
		}
		else
		{
			LegalMoves.legalMoves = new();
			Promotion.AvailablePromotions(end, Position.colorToMove);
			Position.colorToMove = '\0';
		}
	}
	public static void DeletePiece(Vector2I start, Vector2I? end, bool playSound, bool animation = true, char replace = '\0', Sprite2D handledSprite = null)
	{
		Vector2I endUsed = start;
		if (end != null)
			endUsed = (Vector2I)end;
		if (end == -Vector2I.One)
			return;
		try
		{
			if (animation)
				Animations.Tween(Chessboard.tiles[new(endUsed.X, endUsed.Y, 1)], Animations.animationSpeed, start, null, new(), null, true);
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
			Audio.Play("capture");
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
				Audio.Play("move");
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
		Audio.Play("castle");
	}
	private static Sprite2D GetPiece(Vector2I location)
	{
		return Chessboard.tiles[new(location.X, location.Y, 1)];
	}
}
