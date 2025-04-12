using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Position
{
	public static Dictionary<Vector2I, char> pieces = new();
	public static char colorToMove, WinningPlayer = '\0', oppositeStartColorToMove = playerColors.Last();
	public static (Vector2I target, Vector2I delete)? EnPassantInfo = null;
	public static (Vector2I start, Vector2I end)? LastMoveInfo = null;
	public static bool startPositionLoaded = false;
	public static Dictionary<Vector2I, char> RoyalPiecesColor;
	public static bool InCheck = false;
	public static EndState GameEndState = EndState.Ongoing;
	public static int HalfmoveClock, FullmoveNumber;
	public const string playerColors = "wb", castlingSideOptions = "KQ";
    public static Dictionary<char, int> RoyalsPerColor = new() { { 'w', 0 }, { 'b', 0 } };
    public static void Load(FEN fen)
    {
        string fenCall = fen switch
        {
            FEN.Default => "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            FEN.NoRange => "8/1n6/6k1/8/3N4/8/5K2/8 w - - 0 1",
            FEN.Range => "8/8/3q1b2/8/3R1r2/8/3Q1B2/8 w - - 0 1",
            FEN.PawnTest => "8/pppppppp/8/8/8/8/PPPPPPPP/8 w - - 0 1",
            FEN.PromotionTest => "8/PPPPPPPP/Q/8/8/q/pppppppp/8 w - - 0 1",
            FEN.TagTest => "4k3/4K///r w - - 0 1",
            FEN.CheckTest => "4k//1R/////4k",
            FEN.DoubleCheck => "q3K//R//4q///4k",
            FEN.DoublePin => "qR2r//4Q//4q///4k",
            FEN.DiscoveredCheck => "4K///4n/4r///4k b",
            FEN.HiddenBlock => "q3K1R",
            FEN.ProtectedBlock => "4K/3q/3r",
            FEN.PawnRoyalBlock => "4k//3PPP b",
            FEN.EnPassantBlock => "/2p/3p/KP5r/1R3p1k//4P1P",
            FEN.CastlingTest => "r3k2r/8/8/8/8/8/PPPP4/R3K2R w KQkq - 0 1",
            FEN.Checkmate => "3qKq",
            FEN.KingVsKing => "K//k",
            FEN.KingVsKingKnightKnight => "KNN//nn",
            FEN.TwoRoyalsUnderAttack => "4K///4q////4K",
            FEN.MoveFlagFilterBug => "/q1P1K//////4k",
			FEN.PerpetualCheck => "4k////Q///4K b",
            FEN.EnPassantFen => "4k3/8/8/8/2pPp3/8/8/4K3 b - d3 99 1",
			FEN.WrongPin => "4k/3r/4Q/5K/",
			FEN.BrokenCheckAnimation => "2R1k//4Q",
			FEN.DoubleCheckDiscovered => "4k//4N/4R",
			FEN.TagRemovalTest => "r3k2r/8/R/8/8/r/8/R3K2R w KQkq - 0 1",
			FEN.UndoCheckAnimationByCapturedAttacker => "4k3/8/8/8/8/2R5/2n5/4K3 w - - 0 1",
			FEN.KingVsQueenQueen => "4K3/1qq5/8/8/8/8/8/8 w - - 0 1",
			FEN.KnightTest => "8/8/5b2/8/8/2B5/8/8 w - - 0 1",
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
        MoveFlagFilterBug,
        EnPassantFen,
		PerpetualCheck,
		WrongPin,
		BrokenCheckAnimation,
		DoubleCheckDiscovered,
		TagRemovalTest,
		UndoCheckAnimationByCapturedAttacker,
		KingVsQueenQueen,
		KnightTest,
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
    public static void Load(string fen)
	{
		if (startPositionLoaded)
			return;
		colorToMove = 'w';
        Zobrist.GenerateKeys();
        LegalMoves.IsGettingLegalMovesOnLoad = true;
		Animations.firstCheckZone = 0;
		GameEndState = EndState.Ongoing;
		HalfmoveClock = 0; FullmoveNumber = 1;
		WinningPlayer = '\0';
		string[] fenSplit = fen.Split(' ');
		LoadPosition(fenSplit[0]);
        startPositionLoaded = true;
        if (LoadColorToMove(fenSplit))
			return;
		if (LoadCastling(fenSplit))
			return;
		if (LoadEnPassant(fenSplit))
			return;
		LoadCounters(fenSplit);
	}
	private static void LoadPosition(string position)
	{
		Vector2I piecePos = new(0, 0);
		pieces = new();
		foreach (char symbol in position)
		{
			if (symbol == '/')
			{
				piecePos = new(0, piecePos.Y + 1);
				continue;
			}
			if (IsNumber(symbol.ToString(), out int number))
			{
				piecePos.X += number;
				continue;
			}
			pieces.Add(piecePos, symbol);
			piecePos.X++;
		}
	}
	private static bool LoadColorToMove(string[] fenSplit)
	{
        if (fenSplit.Length == 1)
            return true;
        colorToMove = fenSplit[1] switch
        {
            "b" => 'b',
            _ => 'w'
        };
        return false;
    }
	private static bool LoadCastling(string[] fenSplit)
	{
		if (fenSplit.Length == 2)
			return true;
		string fenCastling = fenSplit[2];
		if (fenCastling == "-")
			return false;
		foreach (char castlingSide in fenCastling)
		{
			Vector2I castleeLocation = GetCastleeLocation(castlingSide, out bool invalid);
			if (invalid)
				continue;
			Tags.Add(castleeLocation, Tags.Tag.Castlee);
		}
		return false;
	}
	private static bool LoadEnPassant(string[] fenSplit)
	{
		if (fenSplit.Length == 3)
			return true;
		string fenEnPassant = fenSplit[3];
		if (fenEnPassant == "-")
		{
			EnPassantInfo = null;
            return false;
        }
        Vector2I location = Notation.ToLocation(fenEnPassant, out bool invalid);
		if (invalid)
			return false;
        switch (location.Y)
		{
			case 5: EnPassantInfo = (location, new(location.X, 4)); break;
			case 2: EnPassantInfo = (location, new(location.X, 3)); break;
		}
        return false;
	}
	private static void LoadCounters(string[] fenSplit)
	{
		if (fenSplit.Length == 4)
			return;
		if (IsNumber(fenSplit[4], out int halfmoveClock))
			HalfmoveClock = halfmoveClock;
		if (fenSplit.Length == 5)
			return;
		if (IsNumber(fenSplit[5], out int fullmoveNumber))
			FullmoveNumber = fullmoveNumber;
	}
	private static bool IsNumber(string numAsString, out int number)
	{
		string numList = "0123456789";
		number = 0;
		foreach (char c in numAsString)
		{
			if (!numList.Contains(c))
				return false;
		}
		number = Convert.ToInt32(numAsString);
		return true;
	}
	private static Vector2I GetCastleeLocation(char castlingSide, out bool invalid)
	{
		invalid = !castlingSideOptions.Contains(castlingSide.ToString().ToUpper());
		if (invalid)
			return new();
		Vector2I location = new();
		location.X = castlingSide.ToString().ToUpper() switch
		{
			"Q" => 0,
			_ => 7
		};
		location.Y = LegalMoves.GetPieceColor(castlingSide) switch
		{
			'w' => 7,
			_ => 0
		};
		return location;
	}
}
