using Godot;
using System;
using System.Collections.Generic;

public partial class Zobrist : Node
{
    public static ulong LastCastlingRightHash = 0;
    private static Dictionary<Vector2I, ulong> Squares;
    private static ulong[] Pieces;
    private static ulong BlackToMove;
    private static string pieceString = "PNBRQKpnbrqk";
    public static void GenerateKeys()
    {
        Squares = new();
        Pieces = new ulong[12];
        BlackToMove = GenerateRandom();

        for (int x = 0; x < Chessboard.tileCount.X; x++)
        {
            for (int y = 0; y < Chessboard.tileCount.Y; y++)
                Squares[new(x, y)] = GenerateRandom();
        }
        for (int i = 0; i < 12; i++)
            Pieces[i] = GenerateRandom();
    }
    public static ulong PositionHash()
    {
        ulong positionHash = 0;
        foreach (KeyValuePair<Vector2I, char> piece in Position.pieces)
            positionHash ^= Squares[piece.Key] ^ Pieces[pieceString.IndexOf(piece.Value)];
        if (Position.colorToMove == 'b')
            positionHash ^= BlackToMove;
        positionHash ^= LastCastlingRightHash;
        return positionHash;
    }
    private static ulong GenerateRandom()
    {
        byte[] buffer = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
        ulong randomNumber = BitConverter.ToUInt64(buffer, 0);
        return randomNumber;
    }
    public static ulong GetCastlingHash()
    {
        if (Tags.CastlingRights.Count == 0)
            return 0;
        ulong castlingHash = 0;
        Tags.CastlingRights.Sort((a, b) => ComparePositions(a, b));
        foreach (Vector2I position in Tags.CastlingRights)
            castlingHash ^= Squares[position];
        return castlingHash;

        int ComparePositions(Vector2I a, Vector2I b)
        {
            if (a.X != b.X)
                return a.X.CompareTo(b.X);
            return a.Y.CompareTo(b.Y);
        }
    }
    private static Vector2I IndexToBoardPosition(int index)
    {
        return new(index % Chessboard.tileCount.X, index / Chessboard.tileCount.Y);
    }
}
