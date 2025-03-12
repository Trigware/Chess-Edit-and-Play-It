using Godot;
using System;
using System.Collections.Generic;

public partial class LegalMoves : Node
{
	public static List<(Vector2I start, Vector2I end)> legalMoves = new();
	public static List<(Vector2I target, Vector2I deletion)> PawnLeapMovesInfo = new();
	public static List<int> PawnLeapMoves = new(), EnPassantMoves = new(), PromotionMoves = new(), CastlingMoves = new();
	public static List<(Vector2I start, Vector2I end)> CastleeMoves = new();
	public static List<Vector2I> OpponentMoves = new(), ProtectedPieces = new();
	public static HashSet<Vector2I> CheckedRoyals;
	public static List<List<Vector2I>> CheckResponseZones, PinnedPieceZones;

	public static readonly Dictionary<char, (Vector2I[] direction, int range)> pieceDefinitons = new()
	{
		{ 'P', ( new Vector2I[] {new(0, -1), new(-1, -1), new(1, -1)}, 1 ) },
		{ 'N', ( new Vector2I[] {new(-2, 1), new(-1, 2), new(1, 2), new(2, 1), new(-2, -1), new(-1, -2), new(1, -2), new(2, -1)}, 1 ) },
		{ 'B', ( new Vector2I[] {new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)}, int.MaxValue ) },
		{ 'R', ( new Vector2I[] {new(-1, 0), new(1, 0), new(0, -1), new(0, 1)}, int.MaxValue ) },
		{ 'Q', ( new Vector2I[] {new(-1, 0), new(1, 0), new(0, -1), new(0, 1), new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)}, int.MaxValue ) },
		{ 'K', ( new Vector2I[] {new(-1, 0), new(1, 0), new(0, -1), new(0, 1), new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)}, 1 ) }
	};
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
	protected static bool isWithinGrid(Vector2I position)
	{
		return position.X >= 0 && position.Y >= 0 && position.X < Chessboard.tileCount.X && position.Y < Chessboard.tileCount.Y;
	}
	public static List<(Vector2I, Vector2I)> GetLegalMoves(bool opponent = false)
	{
		List<(Vector2I, Vector2I)> legalMovesLocal = new();
		if (!opponent)
		{
			CheckResponseZones = new(); CheckedRoyals = new(); PinnedPieceZones = new(); ProtectedPieces = new();
		}
		OpponentMoves = opponent ? GetOnlyTargets(legalMoves) : GetOpponentMoves();
		if (!opponent)
		{
			PawnLeapMovesInfo = new(); PawnLeapMoves = new(); EnPassantMoves = new(); PromotionMoves = new(); CastlingMoves = new(); CastleeMoves = new();
		}
		foreach (KeyValuePair<Vector2I, char> piece in Position.pieces)
		{
			if (GetPieceColor(piece.Value) != Position.colorToMove)
				continue;
			legalMovesLocal.AddRange(PieceMoves.GetMoves(piece, legalMovesLocal.Count, opponent));
			int tagIndex = Tags.tagPositions.IndexOf(piece.Key);
			if (tagIndex > -1 && Tags.activeTags[tagIndex].Contains(Tags.Tag.Castler))
				legalMovesLocal.AddRange(Castling.IsLegal(piece.Key, Position.colorToMove, OpponentMoves, legalMovesLocal.Count));
		}
		if (!opponent)
		{
			legalMoves = legalMovesLocal;
			Colors.DebugColors();
		}
		return legalMovesLocal;
	}
	private static List<Vector2I> GetOpponentMoves()
	{
		Position.ReverseColor(Position.colorToMove);
		List<Vector2I> opponentMoves = GetOnlyTargets(GetLegalMoves(true));
		Position.ReverseColor(Position.colorToMove);
		return opponentMoves;
	}
	protected static List<Vector2I> GetOnlyTargets(List<(Vector2I start, Vector2I end)> moves)
	{
		List<Vector2I> onlyTargets = new();
		foreach ((Vector2I start, Vector2I end) move in moves)
			onlyTargets.Add(move.end);
		return onlyTargets;
	}
}
