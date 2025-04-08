using System.Collections.Generic;
using Godot;

public partial class History
{
	public static Stack<Move> UndoMoves = new(), RedoMoves = new();
	public static bool cooldownOngoing = false;
	private static float MoveReplayAnimationSpeedMultiplier = 0.75f;
	private static int movesReplayedInThisSession = 0;
	private static ReplayType lastReplay = ReplayType.None;

	public class Move
	{
		public Vector2I Start, End;
		public char CapturedPiece, PiecePromotedFrom, PiecePromotedTo = '\0', EnPassantCapture;
		public (Vector2I target, Vector2I delete)? EnPassantInfo, LeapMoveInfo;
		public Dictionary<Vector2I, HashSet<Tags.Tag>> RemovedTags;
		public (Vector2I start, Vector2I end)? CastleeInfo;
		public int HalfmoveClock;
		public Move(Vector2I start, Vector2I end, char capturedPiece, char piecePromotedFrom, (Vector2I target, Vector2I delete)? enPassantInfo, (Vector2I target, Vector2I delete)? leapMoveInfo, char enPassantCapture, int halfmoveClock,
				   (Vector2I start, Vector2I end)? castleeInfo, Dictionary<Vector2I, HashSet<Tags.Tag>> removedTags)
		{
			Start = start; End = end;
			CapturedPiece = capturedPiece; PiecePromotedFrom = piecePromotedFrom;
			EnPassantInfo = enPassantInfo; LeapMoveInfo = leapMoveInfo; EnPassantCapture = enPassantCapture;
			RemovedTags = removedTags;
			CastleeInfo = castleeInfo;
			HalfmoveClock = halfmoveClock;
		}
		public (Vector2I, Vector2I) GetTuple()
		{
			return (Start, End);
		}
		public void SwapLocations()
		{
			Vector2I savedStart = Start;
			Start = End;
			End = savedStart;
			if (CastleeInfo != null)
			{
				(Vector2I start, Vector2I end) castleeInfo = CastleeInfo ?? default;
				CastleeInfo = (castleeInfo.end, castleeInfo.start);
			}
		}
		public override string ToString()
		{
			string removedTagsString = "";
			int i = 0;
			foreach (KeyValuePair<Vector2I, HashSet<Tags.Tag>> tagRemovalPair in RemovedTags)
			{
				i++;
				removedTagsString += tagRemovalPair.Key + " [";
				int j = 0;
				foreach (Tags.Tag tag in tagRemovalPair.Value)
				{
					j++;
					removedTagsString += tag.ToString() + (j == tagRemovalPair.Value.Count ? "]" : ", ");
				}
				if (i != RemovedTags.Count)
					removedTagsString += ", ";
			}
			return $"Start: {Start.ToString()}; End: {End.ToString()}\n" +
				   $"Captured Piece: {(CapturedPiece == '\0' ? "none" : CapturedPiece)}; Piece Promoted From: {(PiecePromotedFrom == '\0' ? "none" : PiecePromotedFrom)}; Piece Promoted To: {(PiecePromotedTo == '\0' ? "none" : PiecePromotedTo)}\n" +
				   $"EnPassantInfo: {(EnPassantInfo == null ? "none" : EnPassantInfo)}; LeapMoveInfo: {(LeapMoveInfo == null ? "none" : LeapMoveInfo)}, EnPassantCapture: {(EnPassantCapture == '\0' ? "none" : EnPassantCapture)}\n" +
				   $"Removed Tags: {(removedTagsString == "" ? "none" : removedTagsString)}\n" +
				   $"CastleeInfo: {(CastleeInfo == null ? "none" : CastleeInfo)}; Halfmove Clock: {HalfmoveClock}\n";
		}
	}
	public enum ReplayType { None, Undo, Redo }
	public static void Play(Move latestMove)
	{
		RedoMoves = new();
		UndoMoves.Push(latestMove);
	}
	private static void Undo()
	{
		lastReplay = ReplayType.Undo;
		UpdateTileColorsAndUndoTimer();
		MoveReplay(UndoMoves.Pop(), true);
	}
	private static void Redo()
	{
		lastReplay = ReplayType.Redo;
		UpdateTileColorsAndUndoTimer();
		MoveReplay(RedoMoves.Pop(), false);
	}
	public static void KeyPressDetection()
	{
		bool replayDisabled = Promotion.MoveHistoryDisable || Animations.ActiveTweens.Count > 0 || cooldownOngoing;
		bool pressedZ = Input.IsKeyPressed(Key.Z), pressedY = Input.IsKeyPressed(Key.Y);
		if (pressedZ && pressedY)
		{
			movesReplayedInThisSession = 0;
            return;
        }
        if (pressedZ && !replayDisabled)
		{
			if (UndoMoves.Count == 0 || lastReplay == ReplayType.Redo) movesReplayedInThisSession = 0;
			if (UndoMoves.Count > 0) { Undo(); return; }
		}
		if (pressedY && !replayDisabled && RedoMoves.Count > 0)
		{
			if (RedoMoves.Count == 0 || lastReplay == ReplayType.Undo) movesReplayedInThisSession = 0;
			if (RedoMoves.Count > 0) { Redo(); return; }
		}
		if (!pressedZ && !pressedY)
		{
			lastReplay = ReplayType.None;
			movesReplayedInThisSession = 0;
		}
	}
	private static void UpdateTileColorsAndUndoTimer()
	{
		Animations.CheckAnimationsStarted = new();
		Colors.ChangeTileColorBack();
		MoveReplayAnimationSpeedMultiplier = Mathf.Lerp(1, 0.3f, Mathf.Min(10, movesReplayedInThisSession) / 10f);
		Colors.ColorCheckedRoyalTiles(Colors.Enum.Default);
		LegalMoves.CheckedRoyals = new();
		movesReplayedInThisSession++;
		MoveReplayCooldown(Mathf.Max(Animations.lowAnimationDurationBoundary, Animations.animationSpeed) * MoveReplayAnimationSpeedMultiplier, true);
	}
	private static void MoveReplay(Move replayedMove, bool isUndo)
	{
		Stack<Move> movePushedTo = isUndo ? RedoMoves : UndoMoves;
		Animations.CheckAnimationCancelEarly(replayedMove.End);
		Position.EnPassantInfo = isUndo ? replayedMove.EnPassantInfo : replayedMove.LeapMoveInfo;
		Interaction.Deselect((Interaction.selectedTile ?? default).Location);
		if (Position.pieces.ContainsKey(replayedMove.Start))
			replayedMove.SwapLocations();
		Tags.ModifyRoyalPieceList(replayedMove.End, replayedMove.Start);
		movePushedTo.Push(replayedMove);
		ModifyLastMoveInfo();
		MoveReplayGetBack(replayedMove, isUndo);
		UpdatePosition.DiscoveredCheckAnimation(null, true, MoveReplayAnimationSpeedMultiplier);
	}
	public static void MoveReplayCooldown(float waitTime, bool isReplay)
	{
		Timer cooldown = new() { WaitTime = waitTime, OneShot = true };
		if (isReplay) cooldownOngoing = true; else Cursor.cooldownOngoing = true;
		LoadGraphics.I.AddChild(cooldown);
		cooldown.Timeout += () =>
		{
			if (isReplay) cooldownOngoing = false; else Cursor.cooldownOngoing = false;
			cooldown.QueueFree();
		};
		cooldown.Start();
	}
	private static void ModifyLastMoveInfo()
	{
		Interaction.PreviousMoveTiles(Colors.Enum.Default);
		if (UndoMoves.Count == 0)
		{
			Position.LastMoveInfo = null;
			return;
		}
		Position.LastMoveInfo = UndoMoves.Peek().GetTuple();
		Interaction.PreviousMoveTiles(Colors.Enum.PreviousMove);
		if (Interaction.selectedTile != null)
			Interaction.Deselect((Interaction.selectedTile ?? default).Location);
	}
	private static void MoveReplayGetBack(Move replayedMove, bool isUndo)
	{
		bool promotion = replayedMove.PiecePromotedFrom != '\0', enPassant = replayedMove.EnPassantCapture != '\0', capture = replayedMove.CapturedPiece != '\0', castling = replayedMove.CastleeInfo != null;
		Audio.playedCheck = false;
		Audio.silenceAudio = promotion || enPassant || castling;
		LegalMoves.ReverseColor(Position.colorToMove);
		ReplayRegularMove(replayedMove, capture, promotion, castling, isUndo);
		if (promotion) ReplayPromotion(replayedMove, isUndo);
		if (enPassant) ReplayEnPassant(replayedMove, isUndo);
		if (castling) ReplayCastling(replayedMove);
		ReplayTags(replayedMove, isUndo);
		LegalMoves.GetLegalMoves(false, isUndo);
	}
	private static void ReplayRegularMove(Move replayedMove, bool capture, bool promotion, bool castling, bool isUndo)
	{
		UpdatePosition.EditPiecePositions(replayedMove.End, replayedMove.Start, Chessboard.GetPiece(replayedMove.End), false, false, castling, false, isUndo ? replayedMove.PiecePromotedFrom : replayedMove.PiecePromotedTo, promotion, MoveReplayAnimationSpeedMultiplier, !isUndo);
		Audio.Play(capture ? Audio.Enum.Capture : Audio.Enum.Move);
		if (capture && isUndo)
			Animations.Tween(UpdatePosition.AddPiece(replayedMove.End, replayedMove.CapturedPiece, 0, 1), Animations.animationSpeed * MoveReplayAnimationSpeedMultiplier, replayedMove.End, null, 1, null, false);
		Position.HalfmoveClock = replayedMove.HalfmoveClock;
		if (Position.colorToMove != Position.oppositeStartColorToMove)
			Position.FullmoveNumber--;
	}
	private static void ReplayPromotion(Move replayedMove, bool isUndo)
	{
		Vector2I promotionAnimationStart = new(replayedMove.Start.X, replayedMove.End.Y + (Position.colorToMove == 'w' ? 2 : -2));
		Promotion.OptionChosen(isUndo ? replayedMove.PiecePromotedFrom : replayedMove.PiecePromotedTo, replayedMove.Start, promotionAnimationStart, 1, Chessboard.Layer.Piece, MoveReplayAnimationSpeedMultiplier);
		ReplayMoveAudio(Audio.Enum.Promotion);
	}
	private static void ReplayEnPassant(Move replayedMove, bool isUndo)
	{
		Vector2I enPassantDelete = (replayedMove.EnPassantInfo ?? default).delete;
		if (isUndo)
			Animations.Tween(UpdatePosition.AddPiece(enPassantDelete, replayedMove.EnPassantCapture, Chessboard.gridScale, 0), Animations.animationSpeed * MoveReplayAnimationSpeedMultiplier, replayedMove.End, null, 1, 1, false);
		else
			UpdatePosition.DeletePiece(enPassantDelete, null, true, true, '\0', null, true, MoveReplayAnimationSpeedMultiplier);
		ReplayMoveAudio(Audio.Enum.Capture);
	}
	private static void ReplayCastling(Move replayedMove)
	{
		(Vector2I end, Vector2I start) castleeReplay = replayedMove.CastleeInfo ?? default;
		UpdatePosition.EditPiecePositions(castleeReplay.start, castleeReplay.end, Chessboard.tiles[new(castleeReplay.start, Chessboard.Layer.Piece)], false, false, false, false, '\0', false, MoveReplayAnimationSpeedMultiplier);
		ReplayMoveAudio(Audio.Enum.Castle);
	}
	private static void ReplayTags(Move replayedMove, bool isUndo)
	{
		if (replayedMove.RemovedTags.Count == 0)
			return;
		foreach (KeyValuePair<Vector2I, HashSet<Tags.Tag>> tagRemovalPair in replayedMove.RemovedTags)
		{
			foreach (Tags.Tag tag in tagRemovalPair.Value)
			{
				if (isUndo)
					Tags.Add(tagRemovalPair.Key, tag);
				else
					Tags.Delete(tagRemovalPair.Key, tag, false);
			}
		}
		Tags.GetCastlingRightsHash();
	}
	private static void ReplayMoveAudio(Audio.Enum audio)
	{
		Audio.silenceAudio = false;
		Audio.Play(audio);
	}
}
