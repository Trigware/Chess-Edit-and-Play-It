using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Pieces : Node
{
	public static readonly Dictionary<char, (Vector2I[] direction, int range)> pieceDefinitons = new()
	{
		{ 'P', ( new Vector2I[] {}, 0 ) }, // pawn placeholder
		{ 'N', ( new Vector2I[] {new(-2, 1), new(-1, 2), new(1, 2), new(2, 1), new(-2, -1), new(-1, -2), new(1, -2), new(2, -1)}, 0 ) },
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
	public static List<(Vector2I, Vector2I)> GetMoves(KeyValuePair<Vector2I, char> piece)
	{
		List<(Vector2I start, Vector2I dest)> pieceMoves = new();
		(Vector2I[] direction, int range) defintion = pieceDefinitons[Convert.ToChar(piece.Value.ToString().ToUpper())];
		foreach (Vector2I dir in defintion.direction)
		{
			for (int i = 0; i < defintion.range; i++)
			{
				Vector2I addedFlatPosition = piece.Key + dir * (i + 1);
				if (isWithinGrid(addedFlatPosition))
				{
					char pieceColor = GetPieceColor(addedFlatPosition);
					if (pieceColor == Position.colorToMove)
						break;
					pieceMoves.Add((piece.Key, addedFlatPosition));
					if (pieceColor != '\0' && pieceColor != Position.colorToMove)
						break;
				}
				else
					break;
			}
		}
		return pieceMoves;
	}
	private static bool isWithinGrid(Vector2I position)
	{
		return position.X >= 0 && position.Y >= 0 && position.X < Chessboard.tileCount.X && position.Y < Chessboard.tileCount.Y;
	}
}
