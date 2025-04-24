using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class InsufficientMaterial
{
	public static Dictionary<char, bool> PlayerMaterialInsufficiency;
	private static List<List<char>> InsufficientMaterialSide = new() { new() { 'k' }, new() { 'k', 'b' }, new() { 'k', 'n' } };
	private static List<(List<char> player1, List<char> player2)> InsufficientMaterialCombinations = new() { (new() { 'k' }, new() { 'k', 'n', 'n' })
	
	};
	public static void ResetMaterialInsufficiency()
	{
		PlayerMaterialInsufficiency = new();
		foreach (char player in Position.playerColors)
			PlayerMaterialInsufficiency.Add(player, false);
	}
	public static bool Check()
	{
		(List<char> white, List<char> black) piecePositionsByColor = GetPiecePositionByColor();
		int playersInsufficient = 0;
		foreach (char player in Position.playerColors)
		{
			List<char> wantedPlayerPiecesList = player == 'w' ? piecePositionsByColor.white : piecePositionsByColor.black;
			PlayerMaterialInsufficiency[player] = InsufficientMaterialSide.Any(side => side.SequenceEqual(wantedPlayerPiecesList));
			playersInsufficient += PlayerMaterialInsufficiency[player] ? 1 : 0;
		}
		if (playersInsufficient == 2)
			return true;
		if (playersInsufficient == 1)
		{
			return InsufficientMaterialCombinations.Any(comb => (comb.player1.SequenceEqual(piecePositionsByColor.white) && comb.player2.SequenceEqual(piecePositionsByColor.black)) || 
																(comb.player1.SequenceEqual(piecePositionsByColor.black) && comb.player2.SequenceEqual(piecePositionsByColor.white)));
		}
		return false;
	}

	private static (List<char>, List<char>) GetPiecePositionByColor()
	{
		(List<char> white, List<char> black) PiecePositionsByColor = (new List<char>(), new List<char>());
		foreach (char piece in Position.pieces.Values)
		{
			char addedPiece = Convert.ToChar(piece.ToString().ToLower());
			if (LegalMoves.GetPieceColor(piece) == 'w')
				PiecePositionsByColor.white.Add(addedPiece);
			else
				PiecePositionsByColor.black.Add(addedPiece);
		}
		return PiecePositionsByColor;
	}
}
