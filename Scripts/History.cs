using Godot;
using System.Collections.Generic;

public partial class History
{
	public static Stack<Move> UndoMoves = new(), RedoMoves = new();
	public static bool DisableReplay = false;
	public struct Move
	{
		public Vector2I Start, End;
		public char CapturedPiece, PiecePromotedFrom;
		public Move(Vector2I start, Vector2I end, char capturedPiece, char piecePromotedFrom)
		{
			Start = start; End = end;
			CapturedPiece = capturedPiece; PiecePromotedFrom = piecePromotedFrom;
		}
		public (Vector2I, Vector2I) GetTuple()
		{
			return (Start, End);
		}
		public override string ToString()
		{
			return $"Start: {Start.ToString()}, End: {End.ToString()}\nCaptured Piece: {(CapturedPiece == '\0' ? "none" : CapturedPiece)}, Piece Promoted From: {PiecePromotedFrom}";
		}
	}
	public static void Play(Vector2I start, Vector2I end, char capturedPiece, char piecePromotedFrom)
	{
		RedoMoves = new();
		UndoMoves.Push(new(start, end, capturedPiece, piecePromotedFrom));
	}
	public static void Undo()
	{
		if (UndoMoves.Count == 0 || DisableReplay)
			return;
		DisableReplay = true;
		Move previousMove = UndoMoves.Pop();
		Interaction.Deselect(Interaction.selectedTile ?? default);
		//GD.Print(previousMove.ToString());
		RedoMoves.Push(previousMove);
		ModifyLastMoveInfo();
		MoveReplayGetBack(previousMove);
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
		bool promotion = replayedMove.PiecePromotedFrom != '\0';
        Audio.silenceAudio = promotion;
		UpdatePosition.EditPiecePositions(replayedMove.End, replayedMove.Start, Chessboard.GetPiece(replayedMove.End), false, false, false, false, replayedMove.PiecePromotedFrom, promotion);
		if (replayedMove.CapturedPiece == '\0')
			Audio.Play(Audio.Enum.Move);
		else
		{
			Animations.Tween(UpdatePosition.AddPiece(replayedMove.End, replayedMove.CapturedPiece), Animations.animationSpeed, replayedMove.End, null, 1, null, false);
			Audio.Play(Audio.Enum.Capture);
		}
        if (promotion)
        {
			Audio.silenceAudio = false;
            Audio.Play(Audio.Enum.Promotion);
            Vector2I promotionAnimationStart = new(replayedMove.Start.X, replayedMove.End.Y + (Position.colorToMove == 'w' ? 2 : -2));
            Promotion.OptionChosen(replayedMove.PiecePromotedFrom, replayedMove.Start, promotionAnimationStart, 1, 1, 1);
        }
        LegalMoves.ReverseColor(Position.colorToMove);
        LegalMoves.GetLegalMoves(false, true);
	}
}
