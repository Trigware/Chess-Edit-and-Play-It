using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class InsufficientMaterial : Node
{
    public static bool[] PlayerMaterialInsufficiency = new bool[2];
    private static List<List<char>> InsufficientMaterialSide = new() { new() { 'k' }, new() { 'k', 'b' }, new() { 'k', 'n' } };
    private static List<(List<char> player1, List<char> player2)> InsufficientMaterialCombinations = new() { (new() { 'k' }, new() { 'k', 'n', 'n' })
    
    };
    public static bool Check()
    {
        (List<char> white, List<char> black) piecePositionsByColor = GetPiecePositionByColor();
        int playersInsufficient = 0;
        for (int i = 0; i < 2; i++)
        {
            List<char> wantedPlayerPiecesList = i == 0 ? piecePositionsByColor.white : piecePositionsByColor.black;
            PlayerMaterialInsufficiency[i] = InsufficientMaterialSide.Any(insufficient => insufficient.SequenceEqual(wantedPlayerPiecesList));
            playersInsufficient += PlayerMaterialInsufficiency[i] ? 1 : 0;
        }
        if (playersInsufficient == 2)
            return true;
        if (playersInsufficient == 1)
        {
            foreach ((List<char> player1, List<char> player2) insufficientMaterialCombination in InsufficientMaterialCombinations)
            {
                if ((insufficientMaterialCombination.player1.SequenceEqual(piecePositionsByColor.white) && insufficientMaterialCombination.player2.SequenceEqual(piecePositionsByColor.black)) ||
                    (insufficientMaterialCombination.player1.SequenceEqual(piecePositionsByColor.black) && insufficientMaterialCombination.player2.SequenceEqual(piecePositionsByColor.white)))
                    return true;
            }
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
