using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LegalMoves
{
	public static List<(Vector2I start, Vector2I end)> legalMoves = new();
	public static List<(Vector2I target, Vector2I deletion)> PawnLeapMovesInfo = new();
	public static List<int> PawnLeapMoves = new(), EnPassantMoves = new(), PromotionMoves = new(), CastlingMoves = new();
	public static List<(Vector2I start, Vector2I end)> CastleeMoves = new();
	public static List<Vector2I> OpponentMoves = new(), ProtectedPieces, CheckedRoyals = new(), RoyalAttackers, LegalMovesRanges;
	public static List<List<Vector2I>> CheckResponseZones, PinnedPieceZones;
	public static Dictionary<Vector2I, int> CheckResponseRange;
	public static bool EnPassantBlocked, IsGettingLegalMovesOnLoad;
	public static int maxResponseRange, CheckRoyalsCount;

	public static readonly Dictionary<char, (Vector2I[] direction, int range)> pieceDefinitons = new()
	{
		{ 'P', ( new Vector2I[] {new(0, -1), new(-1, -1), new(1, -1)}, 1 ) },
		{ 'N', ( new Vector2I[] {new(-2, 1), new(-1, 2), new(1, 2), new(2, 1), new(-2, -1), new(-1, -2), new(1, -2), new(2, -1)}, 1 ) },
		{ 'B', ( new Vector2I[] {new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)}, int.MaxValue ) },
		{ 'R', ( new Vector2I[] {new(-1, 0), new(1, 0), new(0, -1), new(0, 1)}, int.MaxValue ) },
		{ 'Q', ( new Vector2I[] {new(-1, 0), new(1, 0), new(0, -1), new(0, 1), new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)}, int.MaxValue ) },
		{ 'K', ( new Vector2I[] {new(-1, 0), new(1, 0), new(0, -1), new(0, 1), new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)}, 1 ) }
	};
	public static List<(Vector2I, Vector2I)> GetLegalMoves(bool opponent = false, bool undo = false, bool gameReset = false)
	{
		List<(Vector2I, Vector2I)> legalMovesLocal = new();
		if (!opponent)
		{
			CheckResponseZones = new(); CheckedRoyals = new(); CheckResponseRange = new();
			PinnedPieceZones = new(); ProtectedPieces = new(); RoyalAttackers = new();
			EnPassantBlocked = false; maxResponseRange = 0; CheckRoyalsCount = 0; LegalMovesRanges = new();
		}
		OpponentMoves = opponent ? GetOnlyTargets(legalMoves) : GetOpponentMoves();
		if (!opponent)
		{
			PawnLeapMovesInfo = new(); PawnLeapMoves = new(); EnPassantMoves = new(); PromotionMoves = new(); CastlingMoves = new(); CastleeMoves = new();
		}
		if (!opponent && History.RedoMoves.Count > 0 && Position.GameEndState != Position.EndState.Ongoing)
			return new();
		foreach (KeyValuePair<Vector2I, char> piece in Position.pieces)
		{
			if (GetPieceColor(piece.Value) != Position.ColorToMove)
				continue;
			legalMovesLocal.AddRange(PieceMoves.GetMoves(piece, legalMovesLocal.Count, opponent));
			int tagIndex = Tags.tagPositions.IndexOf(piece.Key);
			if (tagIndex > -1 && Tags.activeTags[tagIndex].Contains(Tags.Tag.Castler))
				legalMovesLocal.AddRange(Castling.IsLegal(piece.Key, Position.ColorToMove, OpponentMoves, legalMovesLocal.Count));
		}
		if (!opponent)
		{
			legalMoves = legalMovesLocal;
			if (!gameReset) PostMoveGeneration(undo);
		}
		return legalMovesLocal;
	}
	private static List<Vector2I> GetOpponentMoves()
	{
		ReverseColor(Position.ColorToMove);
		List<Vector2I> opponentMoves = GetOnlyTargets(GetLegalMoves(true));
		ReverseColor(Position.ColorToMove);
		return opponentMoves;
	}
	private static void PostMoveGeneration(bool undo)
    {
        if (Position.GameEndState != Position.EndState.Ongoing)
		{
			if (Position.GameEndState == Position.EndState.Checkmate) return;
			Audio.Play(Position.GameEndState == Position.EndState.Stalemate ? Audio.Enum.Stalemate : Audio.Enum.GameEnd);
			return;
		}

		Position.InCheck = CheckResponseZones.Count >= 1;
		if (legalMoves.Count == 0)
			Position.GameEndState = Position.InCheck ? Position.EndState.Checkmate : Position.EndState.Stalemate;
		if (Position.HalfmoveClock >= Chessboard.NMoveRuleInPlies)
			Position.GameEndState = Position.EndState.NMoveRule;
		if (Position.GameEndState == Position.EndState.Ongoing && InsufficientMaterial.Check())
			Position.GameEndState = Position.EndState.InsufficientMaterial;
		if (IsGettingLegalMovesOnLoad)
			Tags.GetCastlingRightsHash();
		if (Zobrist.TriggersRepetitionRule(Zobrist.Hash(undo), undo))
			Position.GameEndState = Position.EndState.RepeatedPosition;

		bool isGameStateOngoing = Position.GameEndState == Position.EndState.Ongoing;
		Animations.CancelCheckEarly = false;
		Animations.PreviousCheckTiles = new();
		if (Position.InCheck)
			Colors.ResetAllColors();

		if (IsGettingLegalMovesOnLoad) Animations.ActiveAllCheckAnimationZones();
        if (Position.GameEndState == Position.EndState.Stalemate)
			Audio.Play(Audio.Enum.Stalemate);

		if (!isGameStateOngoing)
		{
			if (!Position.UniqueGameEndAudio.Contains(Position.GameEndState))
				Audio.Play(Audio.Enum.GameEnd);
			Position.WinningPlayer = Position.WinLossEndStates.Contains(Position.GameEndState) ? ReverseColorReturn(Position.ColorToMove) : 'd';
			Chessboard.waitingForBoardFlip = false;
            if (IsGettingLegalMovesOnLoad)
            {
                Position.ColorToMove = Position.WinningPlayer;
                Chessboard.Update();
            }
            EndGame();
		}
        IsGettingLegalMovesOnLoad = false;
	}
	public static void EndGame()
	{
        Interaction.PreviousMoveTiles(Colors.Enum.Default);
        Cursor.ShowHideCursor(false);
        if (Position.GameEndState != Position.EndState.Ongoing && !Position.InCheck) 
			History.TimerCountdown(PauseMenu.PauseScreenAfterGameEndDuration, History.TimerType.GameEndScreen);
    }
    protected static List<Vector2I> GetOnlyTargets(List<(Vector2I start, Vector2I end)> moves)
	{
		List<Vector2I> onlyTargets = new();
		foreach ((Vector2I start, Vector2I end) move in moves)
			onlyTargets.Add(move.end);
		return onlyTargets;
	}
	public static char GetPieceColor(Vector2I location)
	{
		if (Position.pieces.TryGetValue(location, out char piece))
			return GetPieceColor(piece);
		return '\0';
	}
	public static char GetPieceColor(char piece)
	{
		if (!pieceDefinitons.ContainsKey(Convert.ToChar(piece.ToString().ToUpper())))
			return '\0';
		return (piece.ToString() == piece.ToString().ToUpper()) ? 'w' : 'b';
	}
	public static bool IsWithinGrid(Vector2I position) => position.X >= 0 && position.Y >= 0 && position.X < Chessboard.tileCount.X && position.Y < Chessboard.tileCount.Y;
	public static void ReverseColor() => Position.ColorToMove = ReverseColorReturn(Position.ColorToMove);
	public static void ReverseColor(char originalColor) => Position.ColorToMove = ReverseColorReturn(originalColor);
	public static char ReverseColorReturn(char originalColor)
	{
		int currentColorIndex = Position.playerColors.IndexOf(originalColor);
		return Position.playerColors[(currentColorIndex + 1) % 2];
	}
}
