using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class Zobrist
{
    private static ulong LastCastlingRightHash = 0;
    public static Dictionary<ulong, int> RepeatedPositions;
    private static Dictionary<Vector2I, ulong> Squares;
    private static Dictionary<string, ulong> CastlingRightsHashes;
    private static ulong[] Pieces;
    private static ulong BlackToMove;
    private static string pieceString = "PNBRQKpnbrqk";
    public static void GenerateKeys()
    {
        Squares = new();
        Pieces = new ulong[12];
        BlackToMove = GenerateRandom();
        CastlingRightsHashes = new();
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
    public static void GetCastlingHash()
    {
        if (Tags.CastlingRights.Count == 0)
        {
            LastCastlingRightHash = 0;
            return;
        }
        StringBuilder castlingRightsEncoded = new();
        foreach (Vector2I location in Tags.CastlingRights)
            castlingRightsEncoded.Append($"{location.X},{location.Y}|");
        string castlingRightsAsString = castlingRightsEncoded.ToString();
        ulong castlingHash = 0;
        if (!CastlingRightsHashes.TryGetValue(castlingRightsAsString, out castlingHash))
        {
            castlingHash = GenerateRandom();
            CastlingRightsHashes.Add(castlingRightsAsString, castlingHash);
        }
        LastCastlingRightHash = castlingHash;
        GD.Print(LastCastlingRightHash);
    }
    private static ulong GenerateRandom()
    {
        byte[] buffer = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
        ulong randomNumber = BitConverter.ToUInt64(buffer, 0);
        return randomNumber;
    }
}
