using Godot;
using System;
using System.Collections.Generic;

public partial class Zobrist
{
    public static ulong LastCastlingRightHash = 0;
    public static Dictionary<ulong, int> RepeatedPositions;
    private static Dictionary<Vector2I, ulong> Squares;
    private static ulong[] Pieces;
    private static ulong BlackToMove;
    private static string pieceString = "PNBRQKpnbrqk";
    public static void GenerateKeys()
    {
        Squares = new();
        Pieces = new ulong[12];
        BlackToMove = GenerateRandom();
        RepeatedPositions = new();

        for (int x = 0; x < Chessboard.tileCount.X; x++)
        {
            for (int y = 0; y < Chessboard.tileCount.Y; y++)
                Squares[new(x, y)] = GenerateRandom();
        }
        for (int i = 0; i < 12; i++)
            Pieces[i] = GenerateRandom();
    }
    public static ulong Hash()
    {
        ulong hash = 0;
        foreach (KeyValuePair<Vector2I, char> piece in Position.pieces)
            hash ^= Squares[piece.Key] ^ Pieces[pieceString.IndexOf(piece.Value)];
        if (Position.colorToMove == 'b')
            hash ^= BlackToMove;
        hash ^= LastCastlingRightHash;
        if (Position.EnPassantInfo != null)
            hash ^= Squares[(Position.EnPassantInfo ?? default).target];
        return hash;
    }
    public static bool TriggersRepetitionRule(ulong hash)
    {
        int positionRepeated = 1;
        if (RepeatedPositions.ContainsKey(hash))
            positionRepeated = ++RepeatedPositions[hash];
        else
            RepeatedPositions.Add(hash, 1);
        if (positionRepeated >= 3)
            return true;
        return false;
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
    private static ulong GenerateRandom()
    {
        byte[] buffer = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
        ulong randomNumber = BitConverter.ToUInt64(buffer, 0);
        return randomNumber;
    }
}
