using Godot;
using System;
using System.Collections.Generic;

public partial class UpdatePosition
{
	public static void MovePiece(Vector2I start, Vector2I end, int leapMoveIndex, int enPassantIndex, int promotionIndex, int castlingIndex)
	{
		bool enPassant = Position.EnPassantInfo != null && enPassantIndex > -1;
		char capturedPiece = Position.pieces.TryGetValue(end, out char val) ? val : '\0', pieceMoved = Position.pieces[start];
		HandleEnPassantAndClocks(enPassant, leapMoveIndex, start, end, pieceMoved);
		MovePiecesInternally(enPassant, castlingIndex, promotionIndex, start, end);
		UpdateForUserFeatures(start, end, promotionIndex != -1, capturedPiece, pieceMoved);
        NextMovePreparations(promotionIndex, end);
		DiscoveredCheckAnimation(end);
	}
	public static void MovePiece(Vector2I start, Vector2I end, int legalIndex)
	{
        int leapMoveIndex = LegalMoves.PawnLeapMoves.IndexOf(legalIndex);
        int enPassantIndex = LegalMoves.EnPassantMoves.IndexOf(legalIndex);
        int promotionIndex = LegalMoves.PromotionMoves.IndexOf(legalIndex);
        int castlingIndex = LegalMoves.CastlingMoves.IndexOf(legalIndex);
        MovePiece(start, end, leapMoveIndex, enPassantIndex, promotionIndex, castlingIndex);
	}
	private static void HandleEnPassantAndClocks(bool enPassant, int leapMoveIndex, Vector2I start, Vector2I end, char pieceMoved)
	{
        Audio.playedCheck = false;
        if (enPassant)
            DeletePiece((Position.EnPassantInfo ?? default).delete, null, true, true, '\0', null, true);
        if (leapMoveIndex > -1)
            Position.EnPassantInfo = LegalMoves.PawnLeapMovesInfo[leapMoveIndex];
        else
            Position.EnPassantInfo = null;

        if (Chessboard.tiles.ContainsKey(new(end.X, end.Y, 1)) || pieceMoved.ToString().ToLower() == "p")
            Position.HalfmoveClock = 0;
        else
            Position.HalfmoveClock++;
        if (Position.colorToMove == Position.oppositeStartColorToMove)
            Position.FullmoveNumber++;
    }
	private static void MovePiecesInternally(bool enPassant, int castlingIndex, int promotionIndex, Vector2I start, Vector2I end)
	{
        Tags.ModifyRoyalPieceList(start, end);
        Sprite2D handledSprite = GetPiece(start);
        if (castlingIndex > -1)
            MoveCastlee(LegalMoves.CastleeMoves[castlingIndex]);
        EditPiecePositions(start, end, handledSprite, !enPassant && castlingIndex == -1, promotionIndex > -1, castlingIndex > -1, true);
    }
	private static void UpdateForUserFeatures(Vector2I start, Vector2I end, bool isPromoting, char capturedPiece, char pieceMoved)
	{
        Interaction.Deselect(start);
        if (Position.InCheck && PieceMoves.SuccessfulResponseInEveryZone(end))
            Colors.ColorCheckedRoyalTiles(Colors.Enum.Default);
        Position.LastMoveInfo = (start, end);
        if (!isPromoting || Promotion.PromotionOptionsPieces.Count == 0)
            Interaction.PreviousMoveTiles(Colors.Enum.PreviousMove);
        History.Play(start, end, capturedPiece, isPromoting ? pieceMoved : '\0');
    }
	private static void NextMovePreparations(int promotionIndex, Vector2I end)
	{
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
    }
	public static void DeletePiece(Vector2I start, Vector2I? end, bool playSound, bool animation = true, char replace = '\0', Sprite2D handledSprite = null, bool enPassant = false)
	{
		Vector2I endUsed = start;
		if (end != null)
			endUsed = (Vector2I)end;
		try
		{
			if (animation)
			{
				if (enPassant)
					Animations.Tween(Chessboard.GetPiece(endUsed), Animations.animationSpeed, start, null, null, 0, true);
				else
					Animations.Tween(Chessboard.GetPiece(endUsed), Animations.animationSpeed, start, null, new(), null, true);
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
	public static void EditPiecePositions(Vector2I start, Vector2I end, Sprite2D handledSprite, bool playSound, bool promotion, bool castling, bool updateCastlingRightsHash, char handledPiece = '\0', bool promotionUndo = false)
	{
		if (handledPiece == '\0')
			handledPiece = Position.pieces[start];
		Tags.ModifyTags(start, end, handledPiece, updateCastlingRightsHash);
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
		{
			Vector2I animationEnd = promotionUndo ? new(start.X, end.Y + (Position.colorToMove == 'w' ? 2 : -2)) : end;
			Animations.Tween(handledSprite, Animations.animationSpeed, start, animationEnd, null, promotion || promotionUndo ? 0 : null, promotion || promotionUndo, promotion);
        }
    }
	public static Sprite2D AddPiece(Vector2I location, char piece)
	{
		Sprite2D handledPiece = Chessboard.DrawTile(Convert.ToString(piece), location.X, location.Y, 1, LoadGraphics.I, 0);
		Position.pieces.Add(location, piece);
        return handledPiece;
	}
	private static void MoveCastlee((Vector2I start, Vector2I end) castleePosition)
	{
		EditPiecePositions(castleePosition.start, castleePosition.end, GetPiece(castleePosition.start), false, false, false, false);
		Audio.Play(Audio.Enum.Castle);
	}
	private static Sprite2D GetPiece(Vector2I location)
	{
		return Chessboard.tiles[new(location.X, location.Y, 1)];
	}
	private static void DiscoveredCheckAnimation(Vector2I end)
	{
		Animations.firstCheckZone = 0;
		for (int i = 0; i < LegalMoves.RoyalAttackers.Count; i++)
		{
			Vector2I attackerPosition = LegalMoves.RoyalAttackers[i];
            if (attackerPosition != end)
			{
                Animations.CheckAnimation(1, ((SceneTree)Engine.GetMainLoop()).CurrentScene, i);
				if (Animations.firstCheckZone == 0)
					Animations.firstCheckZone = i;
            }	
        }
	}
}
