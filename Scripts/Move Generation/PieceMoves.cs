using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

public partial class PieceMoves : LegalMoves
{
	public static List<(Vector2I, Vector2I)> GetMoves(KeyValuePair<Vector2I, char> piece, int legalCount, bool opponent)
	{
		List<(Vector2I start, Vector2I dest)> pieceMoves = new();
		(Vector2I[] direction, int range) defintion = pieceDefinitons[Convert.ToChar(piece.Value.ToString().ToUpper())];
		bool isPawn = piece.Value.ToString().ToUpper() == "P";
		char pieceColor = GetPieceColor(piece.Value);
		int pieceRange = defintion.range;
		for (int dirIter = 0; dirIter < defintion.direction.Count(); dirIter++)
			pieceMoves.AddRange(AnalyseRange(dirIter, defintion, piece, isPawn, pieceColor, legalCount + pieceMoves.Count, opponent));
		return pieceMoves;
	}
	private static List<(Vector2I start, Vector2I dest)> AnalyseRange(int dirIter, (Vector2I[] direction, int range) defintion, KeyValuePair<Vector2I, char> piece, bool isPawn, char pieceColor, int legalCount, bool opponent)
	{
		Vector2I originalDir = defintion.direction[dirIter];
		Vector2I dir = (pieceColor == 'b' && isPawn) ? new(originalDir.X, -originalDir.Y) : originalDir;
		int pieceRange = defintion.range;
		if (isPawn && dirIter == 0 && (pieceColor == 'b' && piece.Key.Y == 1 || pieceColor == 'w' && piece.Key.Y == 6))
			pieceRange++;
		List<(Vector2I start, Vector2I dest)> rangeMoves = new();
		bool isMovedPieceRoyal = IsRoyal(piece.Key);
		bool pinnedPieceMoveAnalyse = false;
		bool beyondRoyalAnalyse = false;
		for (int range = 0; range < pieceRange; range++)
		{
			Vector2I addedFlatPosition = piece.Key + dir * (range + 1);
			if (!IsWithinGrid(addedFlatPosition))
				break;
			char targetColor = GetPieceColor(addedFlatPosition);
			if (RoyalDangerRestriction(opponent, isMovedPieceRoyal, addedFlatPosition, piece.Key))
			{
				if (targetColor == Position.colorToMove)
					break;
				else
					continue;
			}
			bool promotion = false;
			if (isPawn && AnalysePawnMove(dirIter, range, addedFlatPosition, pieceColor, legalCount + rangeMoves.Count, opponent,
										  piece, out promotion)) break;
			if (ReachedSameColor(targetColor, opponent, addedFlatPosition, pinnedPieceMoveAnalyse, dir, pieceRange - range))
			{
                RemovePromotionAttribute(promotion);
                break;
            }
            if (!opponent && BlockMovementForPinnedPiece(piece.Key, addedFlatPosition))
			{
				RemovePromotionAttribute(promotion);
				continue;
			}
			bool isTargetRoyal = IsRoyal(addedFlatPosition);
			if (DetectRoyalAttack(isTargetRoyal, opponent, pinnedPieceMoveAnalyse, beyondRoyalAnalyse, rangeMoves, piece.Key, addedFlatPosition))
			{
                rangeMoves.Add((piece.Key, addedFlatPosition));
				continue;
			}
			if (!pinnedPieceMoveAnalyse)
                rangeMoves.Add((piece.Key, addedFlatPosition));
            else if (opponent)
				PinnedPieceZones.Last().Add(addedFlatPosition);
			pinnedPieceMoveAnalyse = OnNormalCapture(pinnedPieceMoveAnalyse, targetColor, isTargetRoyal,
									 opponent, addedFlatPosition, dir, pieceRange - range,
									 rangeMoves, piece.Key, out bool broken);
			if (broken)
				break;
		}
		return rangeMoves;
	}
	public static bool SuccessfulResponseInEveryZone(Vector2I location)
	{
		for (int i = 0; i < CheckResponseZones.Count; i++)
		{
			List<Vector2I> zone = CheckResponseZones[i];
			if (!zone.Contains(location))
				return false;
		}
		return true;
	}
	private static bool ReachedSameColor(char targetColor, bool opponent, Vector2I addedFlatPosition, bool pinnedPieceMoveAnalyse, Vector2I dir, int range)
	{
		if (targetColor != Position.colorToMove) return false;
		if (opponent)
			ProtectedPieces.Add(addedFlatPosition);
		if (opponent && Position.EnPassantInfo != null && addedFlatPosition == (Position.EnPassantInfo ?? default).delete && CanMeetRoyal(addedFlatPosition, dir, range))
			EnPassantBlocked = true;
		if (pinnedPieceMoveAnalyse)
			PinnedPieceZones.RemoveAt(PinnedPieceZones.Count - 1);
		return true;
	}
	private static bool BlockMovementForPinnedPiece(Vector2I start, Vector2I end)
	{
		int pinnedPieceZone = -1;
		for (int i = 0; i < PinnedPieceZones.Count; i++)
		{
			if (PinnedPieceZones[i].Contains(start))
			{
				pinnedPieceZone = i;
				break;
			}
		}
		if (pinnedPieceZone == -1)
			return false;
		return !PinnedPieceZones[pinnedPieceZone].Contains(end);
	}
	private static bool CanMeetRoyal(Vector2I startPosition, Vector2I direction, int pieceRange)
	{
		foreach (KeyValuePair<Vector2I, char> endRoyal in Position.RoyalPiecesColor)
		{
			if (endRoyal.Value == Position.colorToMove)
				continue;
			Vector2I deltaVector = endRoyal.Key - startPosition;
			if (deltaVector.X * direction.Y != deltaVector.Y * direction.X)
				return false;
			int steps = (direction.X != 0) ? deltaVector.X / direction.X : deltaVector.Y / direction.Y;
			return steps > 0 && steps < pieceRange - 1;
		}
		return false;
	}
	private static bool OnNormalCapture(bool pinnedPieceMoveAnalyse, char targetColor, bool isTargetRoyal, bool opponent, Vector2I addedFlatPosition, Vector2I dir, int range, List<(Vector2I, Vector2I)> rangeMoves, Vector2I startPosition, out bool broken)
	{
		broken = false;
		if (targetColor == '\0')
			return pinnedPieceMoveAnalyse;
		if (pinnedPieceMoveAnalyse && isTargetRoyal)
			PinnedPieceZones.Last().Remove(addedFlatPosition);
		if (!isTargetRoyal && opponent && CanMeetRoyal(addedFlatPosition, dir, range))
		{
			if (pinnedPieceMoveAnalyse)
			{
				PinnedPieceZones.RemoveAt(PinnedPieceZones.Count - 1);
				broken = true;
			}
			else
			{
				List<Vector2I> PinnedPieceZoneInitial = GetOnlyTargets(rangeMoves);
				PinnedPieceZones.Add(PinnedPieceZoneInitial);
				PinnedPieceZones.Last().Add(addedFlatPosition);
				PinnedPieceZones.Last().Add(startPosition);
				pinnedPieceMoveAnalyse = true;
			}
		}
		else
			broken = true;
		return pinnedPieceMoveAnalyse;
	}
	private static bool AnalysePawnMove(int dirIter, int range, Vector2I addedFlatPosition, char pieceColor, int moveCount, bool opponent, KeyValuePair<Vector2I, char> piece, out bool promotion)
	{
		promotion = false;
		bool enPassant = Position.EnPassantInfo != null;
		if (enPassant)
		{
			(Vector2I target, Vector2I delete) enPassantNotNull = Position.EnPassantInfo ?? default;
			enPassant = addedFlatPosition == enPassantNotNull.target && GetPieceColor(enPassantNotNull.delete) != Position.colorToMove && !EnPassantBlocked;
		}
		bool targetCapture = Position.pieces.TryGetValue(addedFlatPosition, out _) || enPassant || opponent;
		if (dirIter == 0 && targetCapture)
			return true;
		if (dirIter > 0 && !targetCapture)
			return true;
		if (!opponent && range == 1)
		{
			PawnLeapMoves.Add(moveCount);
			PawnLeapMovesInfo.Add((new(addedFlatPosition.X, addedFlatPosition.Y + (pieceColor == 'w' ? 1 : -1)), addedFlatPosition));
		}
		if (!opponent && Promotion.CanBePromotedTo.Count() > 0 && (addedFlatPosition.Y == 0 && pieceColor == 'w' || addedFlatPosition.Y == 7 && pieceColor == 'b'))
		{
			promotion = true;
			PromotionMoves.Add(moveCount);
		}
		if (!opponent && enPassant)
			EnPassantMoves.Add(moveCount);
		return false;
	}
	public static bool IsRoyal(Vector2I location)
	{
		if (!(Position.pieces.TryGetValue(location, out char piece) && piece != Position.colorToMove))
			return false;
		int tagIndex = Tags.tagPositions.IndexOf(location);
		return tagIndex > -1 ? Tags.activeTags[tagIndex].Contains(Tags.Tag.Royal) : false;
	}
	private static bool RoyalDangerRestriction(bool opponent, bool isMovedPieceRoyal, Vector2I addedFlatPosition, Vector2I start)
	{
		if (opponent)
			return false;
		if (isMovedPieceRoyal && (OpponentMoves.Contains(addedFlatPosition) || CheckRoyalsCount > 1 || ProtectedPieces.Contains(addedFlatPosition)))
			return true;
		if (!isMovedPieceRoyal && !SuccessfulResponseInEveryZone(addedFlatPosition))
			return true;
		return CheckResponseZones.Count > 0 && isMovedPieceRoyal && !CheckedRoyals.Contains(start);
	}
	private static bool DetectRoyalAttack(bool isTargetRoyal, bool opponent, bool pinnedPieceMoveAnalyse, bool beyondRoyalAnalyse, List<(Vector2I, Vector2I)> rangeMoves, Vector2I start, Vector2I end)
	{
		if (!(isTargetRoyal && opponent && !pinnedPieceMoveAnalyse && !beyondRoyalAnalyse))
			return false;
		CheckResponseZones.Add(new() { start });
		CheckResponseZones.Last().AddRange(GetOnlyTargets(rangeMoves));
		int responseCount = CheckResponseZones.Last().Count();
		CheckResponseRange.Add(start, responseCount);
		if (responseCount > maxResponseRange)
			maxResponseRange = responseCount;
		if (!CheckedRoyals.Contains(end))
			CheckRoyalsCount++;
		CheckedRoyals.Add(end);
		RoyalAttackers.Add(start);
		beyondRoyalAnalyse = true;
		return true;
	}
	private static void RemovePromotionAttribute(bool promotion)
	{
		if (promotion)
			PromotionMoves.RemoveAt(PromotionMoves.Count - 1);
	}
}