using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class UpdatePosition
{
	private static (Vector2I target, Vector2I delete)? previousEnPassantInfo;
	private static char enPassantDelete;
	private static int previousHalfmoveClock;
	public static History.Move LatestMove;
	public static void MovePiece(Vector2I start, Vector2I end, int leapMoveIndex, int enPassantIndex, int promotionIndex, int castlingIndex)
	{
		bool enPassant = Position.EnPassantInfo != null && enPassantIndex > -1;
		char capturedPiece = Position.pieces.TryGetValue(end, out char val) ? val : '\0', pieceMoved = Position.pieces[start];
		Tags.lastDeletedTags = new();
		Chessboard.waitingForBoardFlip = true;
		Cursor.Location[Position.ColorToMove] = end;
		HandleEnPassantAndClocks(enPassant, leapMoveIndex, start, end, pieceMoved);
		MovePiecesInternally(enPassant, castlingIndex, promotionIndex, start, end);
		UpdateForUserFeatures(start, end, promotionIndex != -1, capturedPiece, pieceMoved, castlingIndex);
		NextMovePreparations(promotionIndex, end);
		DiscoveredCheckAnimation(end, false);
	}
	public static void MovePiece(Vector2I start, Vector2I end)
	{
        int legalIndex = LegalMoves.legalMoves.IndexOf((start, end));
		if (legalIndex == -1) return;
        int leapMoveIndex = LegalMoves.PawnLeapMoves.IndexOf(legalIndex);
		int enPassantIndex = LegalMoves.EnPassantMoves.IndexOf(legalIndex);
		int promotionIndex = LegalMoves.PromotionMoves.IndexOf(legalIndex);
		int castlingIndex = LegalMoves.CastlingMoves.IndexOf(legalIndex);
		MovePiece(start, end, leapMoveIndex, enPassantIndex, promotionIndex, castlingIndex);
	}
	private static void HandleEnPassantAndClocks(bool enPassant, int leapMoveIndex, Vector2I start, Vector2I end, char pieceMoved)
	{
		Audio.playedCheck = false;
		previousEnPassantInfo = Position.EnPassantInfo;
		Animations.CheckAnimationsStarted = new();
		enPassantDelete = enPassant ? Position.pieces[(previousEnPassantInfo ?? default).delete] : '\0';
		previousHalfmoveClock = Position.HalfmoveClock;
		if (enPassant) DeletePiece((Position.EnPassantInfo ?? default).delete, null, true, true, '\0', null, true);
		if (leapMoveIndex > -1) Position.EnPassantInfo = LegalMoves.PawnLeapMovesInfo[leapMoveIndex]; else Position.EnPassantInfo = null;

		if (Chessboard.tiles.ContainsKey(new(end, Chessboard.Layer.Piece)) || pieceMoved.ToString().ToLower() == "p" || enPassant) Position.HalfmoveClock = 0; else Position.HalfmoveClock++;
		if (Position.ColorToMove == Position.oppositeStartColorToMove) Position.FullmoveNumber++;
	}
	private static void MovePiecesInternally(bool enPassant, int castlingIndex, int promotionIndex, Vector2I start, Vector2I end)
	{
		Tags.ModifyRoyalPieceList(start, end);
		Sprite2D handledSprite = GetPiece(start);
		if (castlingIndex > -1)
			MoveCastlee(LegalMoves.CastleeMoves[castlingIndex]);
		EditPiecePositions(start, end, handledSprite, !enPassant && castlingIndex == -1, promotionIndex > -1, castlingIndex > -1, true);
	}
	private static void UpdateForUserFeatures(Vector2I start, Vector2I end, bool isPromoting, char capturedPiece, char pieceMoved, int castlingIndex)
	{
		Interaction.Deselect(start);
		if (Position.InCheck)
			Colors.ColorCheckedRoyalTiles(Colors.Enum.Default);
		Position.LastMoveInfo = (start, end);
		if (!isPromoting || Promotion.PromotionOptionsPieces.Count == 0)
			Interaction.PreviousMoveTiles(Colors.Enum.PreviousMove);
		LatestMove = new(start, end, capturedPiece, isPromoting ? pieceMoved : '\0', previousEnPassantInfo, Position.EnPassantInfo, enPassantDelete,
			previousHalfmoveClock, castlingIndex == -1 ? null : LegalMoves.CastleeMoves[castlingIndex],
			Tags.lastDeletedTags, TimeControl.TimeLeftAtLastPlyStart, TimeControl.GetPlayerTimersTimeLeft());
		if (!isPromoting || Promotion.CanBePromotedTo.Count() <= 1)
			History.Play(LatestMove);
	}
	private static void NextMovePreparations(int promotionIndex, Vector2I end)
	{
		if (promotionIndex == -1)
		{
			LegalMoves.ReverseColor(Position.ColorToMove);
			LegalMoves.GetLegalMoves();
		}
		else
		{
			LegalMoves.legalMoves = new();
			if (Promotion.CanBePromotedTo.Length > 1)
			{
				Promotion.AvailablePromotions(end, Position.ColorToMove);
				Position.ColorToMove = '\0';
			}
			else
				Promotion.AutomaticPromotion(end, Promotion.CanBePromotedTo[0]);
		}
	}
	public static void DeletePiece(Vector2I start, Vector2I? end, bool playSound, bool animation = true, char replace = '\0', Sprite2D handledSprite = null, bool enPassant = false, float animationSpeedMultiplier = 1)
	{
		Vector2I endUsed = start;
		if (end != null)
			endUsed = (Vector2I)end;
		try
		{
			if (animation)
			{
				if (enPassant)
					Animations.Tween(Chessboard.GetPiece(endUsed), Animations.animationSpeed * animationSpeedMultiplier, start, null, null, 0, true);
				else
					Animations.Tween(Chessboard.GetPiece(endUsed), Animations.animationSpeed * animationSpeedMultiplier, start, null, new(), null, true);
			}
			if (replace == '\0')
			{
				Position.pieces.Remove(endUsed);
				Chessboard.tiles.Remove(new(endUsed, Chessboard.Layer.Piece));
			}
			else
			{
				Position.pieces[endUsed] = replace;
				Chessboard.tiles[new(endUsed, Chessboard.Layer.Piece)] = handledSprite;
			}
		}
		catch { }
		if (playSound)
			Audio.Play(Audio.Enum.Capture);
	}
	public static void EditPiecePositions(Vector2I start, Vector2I end, Sprite2D handledSprite, bool playSound, bool promotion, bool castling, bool updateCastlingRightsHash, char handledPiece = '\0', bool promotionReplay = false, float durationMultiplier = 1, bool isRedo = false)
	{
		if (handledPiece == '\0')
			handledPiece = Position.pieces[start];
		Tags.ModifyTags(start, end, handledPiece, updateCastlingRightsHash, castling);
		Position.pieces.Remove(start);
		Chessboard.tiles.Remove(new(start, Chessboard.Layer.Piece));
		Chessboard.Element endPiece = new(end, Chessboard.Layer.Piece);
		bool capture = Chessboard.tiles.ContainsKey(endPiece);
		if (!capture)
		{
			Position.pieces.Add(end, handledPiece);
			Chessboard.tiles.Add(endPiece, handledSprite);
			if (playSound)
				Audio.Play(Audio.Enum.Move);
		}
		else
			DeletePiece(start, end, playSound, true, handledPiece, handledSprite, false, durationMultiplier);
		if (castling)
			Castling.TweenCastle(handledSprite, Animations.animationSpeed * durationMultiplier, start, end.X, isRedo);
		else
		{
			Vector2I animationEnd = promotionReplay ? new(start.X, end.Y + (Position.ColorToMove != 'w' ? 3 : -3)) : end;
			Animations.Tween(handledSprite, Animations.animationSpeed * durationMultiplier, start, isRedo && promotionReplay ? end : animationEnd, null, promotion || promotionReplay ? 0 : null, promotion || promotionReplay, promotion, !(isRedo && promotionReplay));
		}
	}
	public static Sprite2D AddPiece(Vector2I location, char piece, float gridScale, float transparency)
	{
		Sprite2D handledPiece = Chessboard.DrawTilesElement(Convert.ToString(piece), location.X, location.Y, Chessboard.Layer.Piece, LoadGraphics.I, gridScale, transparency);
		Position.pieces.Add(location, piece);
		return handledPiece;
	}
	private static void MoveCastlee((Vector2I start, Vector2I end) castleePosition)
	{
		EditPiecePositions(castleePosition.start, castleePosition.end, GetPiece(castleePosition.start), false, false, false, false);
		Audio.Play(Audio.Enum.Castle);
	}
	private static Sprite2D GetPiece(Vector2I location) => Chessboard.tiles[new(location, Chessboard.Layer.Piece)];
	public static void DiscoveredCheckAnimation(Vector2I? end, bool moveReplay, float durationMultiplier = 1)
	{
		Animations.firstCheckZone = 0;
		for (int i = 0; i < LegalMoves.RoyalAttackers.Count; i++)
		{
			Vector2I attackerPosition = LegalMoves.RoyalAttackers[i];
			if (attackerPosition != end || Animations.animationSpeed == 0 || moveReplay)
			{
				Animations.CheckAnimation(1, ((SceneTree)Engine.GetMainLoop()).CurrentScene, i, attackerPosition, false, durationMultiplier);
				if (Animations.firstCheckZone == 0)
					Animations.firstCheckZone = i;
			}	
		}
	}
}
