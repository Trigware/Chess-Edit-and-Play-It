using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

public partial class Zobrist : Node
{
    private static ulong[,] SquarePieces;
    private static ulong[] SideToMove;
    private static ulong[] CastlingRights;
    private static ulong[] EnPassant;
    public static void GenerateKeys()
    {
        SquarePieces = new ulong[64, 12];
        SideToMove = new ulong[2];
        CastlingRights = new ulong[16];
        EnPassant = new ulong[8];

        for (int i = 0; i < 64; i++)
        {
            for (int j = 0; j < 12; j++)
                SquarePieces[i, j] = GenerateRandom();
        }
        for (int i = 0; i < SideToMove.Length; i++)
            SideToMove[i] = GenerateRandom();
        for (int i = 0; i < CastlingRights.Length; i++)
            CastlingRights[i] = GenerateRandom();
        for (int i = 0; i < EnPassant.Length; i++)
            EnPassant[i] = GenerateRandom();
    }
    public static ulong PositionHash()
    {
        ulong positionHash = 0;
        /*foreach (KeyValuePair<Vector2I, char> piece in Position.pieces)
            positionHash ^= SquarePieces[(piece.Key, piece.Value)];*/
        return positionHash;
    }
    private static ulong GenerateRandom()
    {
        byte[] buffer = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
        ulong randomNumber = BitConverter.ToUInt64(buffer, 0);
        return randomNumber;
    }
    private static Vector2I IndexToBoardPosition(int index)
    {
        return new(index % Chessboard.tileCount.X, index / Chessboard.tileCount.Y);
    }
}
