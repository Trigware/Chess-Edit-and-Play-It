using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Position : Chessboard
{
	public static readonly string playerColors = "wb";
	public static Dictionary<Vector2I, char> pieces = new();
	public static char colorToMove = 'w', WinningPlayer = '\0';
	public static (Vector2I target, Vector2I delete) EnPassantInfo = (-Vector2I.One, -Vector2I.One);
	public static (Vector2I start, Vector2I end) LastMoveInfo = (-Vector2I.One, -Vector2I.One);
	public static bool startPositionLoaded = false;
	public static Dictionary<Vector2I, char> RoyalPiecesColor;
	public static bool InCheck = false;
	public static EndState GameEndState = EndState.Ongoing;
	public static int FiftyMoveRuleClock = 0;
	public static void Load(string fen)
	{
		if (startPositionLoaded)
			return;
		GameEndState = EndState.Ongoing;
		WinningPlayer = '\0';
		string[] fenSplit = fen.Split(' ');
		LoadPosition(fenSplit);
		if (fenSplit.Length == 1)
			return;
		colorToMove = Convert.ToChar(fenSplit[1]);
		if (!playerColors.Contains(colorToMove))
			colorToMove = 'w';
		// missing castling, en passant, halfmove clock, fullmove number
		GetRoyalsPerColor();
		startPositionLoaded = true;
	}
	private static void LoadPosition(string[] fenSplit)
	{
		Vector2I piecePos = new(0, 0);
		pieces = new();
		foreach (char c in fenSplit[0])
		{
			if (c == '/')
			{
				piecePos = new(0, piecePos.Y + 1);
				continue;
			}
			if (isNumber(c, out int number))
			{
				piecePos.X += number;
				continue;
			}
			pieces.Add(piecePos, c);
			piecePos.X++;
		}
	}
	public static void Load(FEN fen)
	{
		string fenCall = fen switch
		{
			FEN.Default => "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
			FEN.NoRange => "8/1n6/6k1/8/3N4/8/5K2/8 w - - 0 1",
			FEN.Range => "8/8/3q1b2/8/3R1r2/8/3Q1B2/8 w - - 0 1",
			FEN.PawnTest => "8/pppppppp/8/8/8/8/PPPPPPPP/8 w - - 0 1",
			FEN.PromotionTest => "2q5/2qP3k/2q5/2q5/2Q5/2Q4K/2Qp4/2Q5 w - - 0 1",
			FEN.TagTest => "4k3/4K///r w - - 0 1",
			FEN.CheckTest => "4k/1R///7q///4k",
			FEN.DoubleCheck => "q3K//R//4q///4k",
			FEN.DoublePin => "qR2K//4Q//4q///4k",
			FEN.DiscoveredCheck => "4K///4n/4r///4k b",
			FEN.HiddenBlock => "q3K1R",
			FEN.ProtectedBlock => "4K/3q/3r",
			FEN.PawnRoyalBlock => "4k//3PPP b",
			FEN.EnPassantBlock => "/2p/3p/KP5r/1R3p1k//4P1P",
			FEN.CastlingTest => "r3k2r///////R3K2R",
            FEN.Checkmate => "3qKq",
            FEN.KingVsKing => "K//k",
			FEN.KingVsKingKnightKnight => "KNN//knn",
			FEN.TwoRoyalsUnderAttack => "4K///4q////4K",
			_ => ""
		};
		Load(fenCall);
	}
	public enum FEN
	{
		Default,
		NoRange,
		Range,
		PawnTest,
		PromotionTest,
		TagTest,
		CheckTest,
		DoubleCheck,
		DoublePin,
		DiscoveredCheck,
		HiddenBlock,
		ProtectedBlock,
		PawnRoyalBlock,
		EnPassantBlock,
		CastlingTest,
		Checkmate,
		KingVsKing,
		KingVsKingKnightKnight,
		TwoRoyalsUnderAttack,
		Empty
	}
	public enum EndState
	{
		Ongoing,
		Checkmate,
		Stalemate,
		InsufficientMaterial,
		ThreefoldRepetition,
		FiftyMoveRule,
		Timeout,
		InsufficientMaterialVsTimeout,
		Resignation,
		DrawAgreement
	}
	public static void ReverseColor(char originalColor)
	{
		colorToMove = ReverseColorReturn(originalColor);
	}
	public static char ReverseColorReturn(char originalColor)
	{
		int currentColorIndex = playerColors.IndexOf(originalColor);
		return playerColors[(currentColorIndex + 1) % 2];
	}
	public static void GetRoyalsPerColor()
	{
		RoyalPiecesColor = new();
		for (int i = 0; i < Tags.activeTags.Count; i++)
		{
			if (Tags.activeTags[i].Contains(Tags.Tag.Royal))
				RoyalPiecesColor.Add(Tags.tagPositions[i], LegalMoves.GetPieceColor(Tags.tagPositions[i]));
		}
	}
	public static void ModifyRoyalPieceList(Vector2I start, Vector2I end)
	{
		if (PieceMoves.isRoyal(start))
			MoveDeleteRoyal(start, end, false);
		if (PieceMoves.isRoyal(end))
			MoveDeleteRoyal(end, end, true);
	}
	private static void MoveDeleteRoyal(Vector2I start, Vector2I end, bool delete)
	{
		char royalColor = RoyalPiecesColor[start];
		RoyalPiecesColor.Remove(start);
		if (!delete)
			RoyalPiecesColor.Add(end, royalColor);
	}
	private static bool isNumber(char c, out int number)
	{
		string testedString = c.ToString();
		try
		{
			number = Convert.ToUInt16(testedString);
			return true;
		}
		catch
		{
			number = 0;
			return false;
		}
	}
}
