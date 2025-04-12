using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public partial class Zobrist
{
	public static ulong LastCastlingRightHash = 0;
	public static Dictionary<ulong, int> RepeatedPositions;
	private static Dictionary<Vector2I, ulong> Squares;
	private static Dictionary<string, ulong> CastlingRightsHashes;
	private static Dictionary<Vector2I, ulong> EnPassantHashes;
	private static ulong[] Pieces;
	private static ulong BlackToMove;
	private static Stack<ulong> LastHashes;
	private static string pieceString = "PNBRQKpnbrqk";
	public static void GenerateKeys()
	{
		Squares = new();
		Pieces = new ulong[12];
		BlackToMove = GenerateRandom();
		CastlingRightsHashes = new();
		RepeatedPositions = new();
		EnPassantHashes = new();
		LastHashes = new();

		for (int x = 0; x < Chessboard.tileCount.X; x++)
		{
			for (int y = 0; y < Chessboard.tileCount.Y; y++)
				Squares[new(x, y)] = GenerateRandom();
		}
		for (int i = 0; i < 12; i++)
			Pieces[i] = GenerateRandom();
	}
	public static ulong Hash(bool undo)
	{
		ulong hash = 0;
		foreach (KeyValuePair<Vector2I, char> piece in Position.pieces)
		{
			if (!LegalMoves.IsWithinGrid(piece.Key)) continue;
			hash ^= Squares[piece.Key] ^ Pieces[pieceString.IndexOf(piece.Value)];
		}
		if (Position.colorToMove == 'b')
			hash ^= BlackToMove;
		hash ^= LastCastlingRightHash;
		hash ^= GetEnPassantHash();
		if (!undo)
			LastHashes.Push(hash);
		return hash;
	}
	public static bool TriggersRepetitionRule(ulong hash, bool undo)
	{
		if (undo)
		{
			ulong searchedHash = LastHashes.Pop();
			if (RepeatedPositions.ContainsKey(searchedHash))
			{
				if (--RepeatedPositions[searchedHash] == 0)
					RepeatedPositions.Remove(searchedHash);
			}
			return false;
		}
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
	}
	public static ulong GetEnPassantHash()
	{
		if (Position.EnPassantInfo == null)
			return 0;
		ulong EnPassantHash = 0;
		Vector2I enPassantTarget = (Position.EnPassantInfo ?? default).target;
		if (!EnPassantHashes.TryGetValue(enPassantTarget, out EnPassantHash))
		{
			EnPassantHash = GenerateRandom();
			EnPassantHashes.Add(enPassantTarget, EnPassantHash);
		}
		return EnPassantHash;
	}
	private static ulong GenerateRandom()
	{
		byte[] buffer = new byte[8];
		System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
		ulong randomNumber = BitConverter.ToUInt64(buffer, 0);
		return randomNumber;
	}
}
