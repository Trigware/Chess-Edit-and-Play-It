using Godot;
using System.Collections.Generic;

public partial class History
{
	public static Stack<Move> UndoMoves = new(), RedoMoves = new();
	public static bool DisableReplay = false;
	private static float MoveReplayAnimationSpeedMultiplier = 0.75f;
	public struct Move
	{
		public Vector2I Start, End;
		public char CapturedPiece, PiecePromotedFrom, EnPassantCapture;
		public (Vector2I target, Vector2I delete)? EnPassantInfo;
		public int HalfmoveClock;
        public Move(Vector2I start, Vector2I end, char capturedPiece, char piecePromotedFrom, (Vector2I target, Vector2I delete)? enPassantInfo, char enPassantCapture, int halfmoveClock)
		{
			Start = start; End = end;
			CapturedPiece = capturedPiece; PiecePromotedFrom = piecePromotedFrom;
			EnPassantInfo = enPassantInfo; EnPassantCapture = enPassantCapture;
			HalfmoveClock = halfmoveClock;
		}
		public (Vector2I, Vector2I) GetTuple()
		{
			return (Start, End);
		}
		public override string ToString()
		{
			return $"Start: {Start.ToString()}, End: {End.ToString()}\n" +
				   $"Captured Piece: {(CapturedPiece == '\0' ? "none" : CapturedPiece)}, Piece Promoted From: {(PiecePromotedFrom == '\0' ? "none" : PiecePromotedFrom)}\n" +
				   $"EnPassantInfo: {(EnPassantInfo == null ? "none" : EnPassantInfo)}, EnPassantCapture: {(EnPassantCapture == '\0' ? "none" : EnPassantCapture)}\n" +
				   $"Halfmove Clock: {HalfmoveClock}";
		}
	}
	public static void Play(Vector2I start, Vector2I end, char capturedPiece, char piecePromotedFrom, (Vector2I target, Vector2I delete)? enPassantInfo, char enPassantCapture, int halfmoveClock)
	{
		RedoMoves = new();
		UndoMoves.Push(new(start, end, capturedPiece, piecePromotedFrom, enPassantInfo, enPassantCapture, halfmoveClock));
	}
	public static void Undo()
	{
        if (UndoMoves.Count == 0 || DisableReplay)
			return;
        if (Animations.ActiveTweens.Count > 0)
        {
            Interaction.undoPending = true;
            return;
        }
        Interaction.undoPending = false;
        MoveReplayAnimationSpeedMultiplier = Mathf.Lerp(1, 0.3f, Mathf.Min(10f, Interaction.movesUndoInASession) / 10);
		DisableReplay = true;
		Colors.ColorCheckedRoyalTiles(Colors.Enum.Default);
		LegalMoves.CheckedRoyals = new();
        Move previousMove = UndoMoves.Pop();
		GD.Print(previousMove.ToString());
		Position.EnPassantInfo = previousMove.EnPassantInfo;
        Interaction.Deselect(Interaction.selectedTile ?? default);
		RedoMoves.Push(previousMove);
		ModifyLastMoveInfo();
		MoveReplayGetBack(previousMove);
        Interaction.movesUndoInASession++;
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
			Interaction.Deselect(Interaction.selectedTile ?? default);
	}
	private static void MoveReplayGetBack(Move replayedMove)
	{
		bool promotion = replayedMove.PiecePromotedFrom != '\0', enPassant = replayedMove.EnPassantCapture != '\0', capture = replayedMove.CapturedPiece != '\0';
        Audio.silenceAudio = promotion || enPassant;
		UpdatePosition.EditPiecePositions(replayedMove.End, replayedMove.Start, Chessboard.GetPiece(replayedMove.End), false, false, false, false, replayedMove.PiecePromotedFrom, promotion, MoveReplayAnimationSpeedMultiplier);
		if (!capture)
			Audio.Play(Audio.Enum.Move);
		else
		{
			Animations.Tween(UpdatePosition.AddPiece(replayedMove.End, replayedMove.CapturedPiece, 0, 1), Animations.animationSpeed * MoveReplayAnimationSpeedMultiplier, replayedMove.End, null, 1, null, false);
			Audio.Play(Audio.Enum.Capture);
		}
        if (promotion)
        {
			Audio.silenceAudio = false;
            Audio.Play(Audio.Enum.Promotion);
            Vector2I promotionAnimationStart = new(replayedMove.Start.X, replayedMove.End.Y + (Position.colorToMove == 'w' ? 2 : -2));
            Promotion.OptionChosen(replayedMove.PiecePromotedFrom, replayedMove.Start, promotionAnimationStart, 1, 1, MoveReplayAnimationSpeedMultiplier);
        }
		if (enPassant)
		{
			Audio.silenceAudio = false;
			Vector2I enPassantDelete = (replayedMove.EnPassantInfo ?? default).delete;
            Animations.Tween(UpdatePosition.AddPiece(enPassantDelete, replayedMove.EnPassantCapture, Chessboard.gridScale, 0), Animations.animationSpeed * MoveReplayAnimationSpeedMultiplier, replayedMove.End, null, 1, 1, false);
            Audio.Play(Audio.Enum.Capture);
		}
		Position.HalfmoveClock = replayedMove.HalfmoveClock;
		if (Position.colorToMove != Position.oppositeStartColorToMove)
			Position.FullmoveNumber--;
        if (Position.GameEndState != Position.EndState.Ongoing)
			return;
        LegalMoves.ReverseColor(Position.colorToMove);
        LegalMoves.GetLegalMoves(false, true);
	}
}
