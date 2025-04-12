using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
public partial class Colors : Interaction
{
	private const float darkEffect = 0.6f;
	public static System.Collections.Generic.Dictionary<Enum, Color> Dict = new()
	{
		{ Enum.DefaultLight, RGB(0xB6, 0x9D, 0x96) },
		{ Enum.DefaultDark, RGB(0x6A, 0x51, 0x4A) },
		{ Enum.Legal, RGB(0xA9, 0xB6, 0x96) },
		{ Enum.Selected, RGB(0x96, 0x9C, 0xB6) },
		{ Enum.PreviousMove, RGB(0x8D, 0xBB, 0xC6) },
		{ Enum.EnPassant, RGB(0x89, 0xB5, 0x48) },
		{ Enum.Promotion, RGB(0x8D, 0x80, 0xE5) },
		{ Enum.Castling, RGB(0xE8, 0xA7, 0x4D) },
		{ Enum.Check, RGB(0x55, 0x55, 0x55) },
		{ Enum.Checkmate,  HSV(0, 80, 80) }
	};
	public enum Enum
	{
		Default,
		DefaultLight,
		DefaultDark,
		Legal,
		Selected,
		PreviousMove,
		EnPassant,
		Promotion,
		Castling,
		Check,
		Checkmate
	}
	private static Color RGB(byte r, byte g, byte b)
	{
		return new(r / 255f, g / 255f, b / 255f);
	}
	private static Color HSV(float h, float s, float v)
	{
		return Color.FromHsv(h / 360f, s / 100f, v / 100f);
	}
	public static void Set(Enum color, int x, int y)
	{
		try
		{
			Set(GetTile(new(x, y)), color, x, y);
		}
		catch { }
	}
	public static void Set(Sprite2D spr, Enum color, int x, int y)
	{
		if (spr.Texture != LoadGraphics.textureDict["tile"])
			return;
		spr.Modulate = GetColorFromEnum(color, x, y);
	}
	public static Color Get(int x, int y)
	{
		try
		{
			return GetTile(new(x, y)).Modulate;
		} catch
		{
			return GetColorFromEnum(Enum.Default, x, y);
		}
	}
	private static Color GetColorFromEnum(Enum color, int x, int y)
	{
		Color enumAsColor = color switch
		{
			Enum.Default => (x % 2 == y % 2) ? Dict[Enum.DefaultLight] : Dict[Enum.DefaultDark],
			Enum.Legal => Dict[Enum.Legal],
			Enum.Selected => Dict[Enum.Selected],
			Enum.PreviousMove => Dict[Enum.PreviousMove],
			Enum.EnPassant => Dict[Enum.EnPassant],
			Enum.Promotion => Dict[Enum.Promotion],
			Enum.Castling => Dict[Enum.Castling],
			Enum.Check => Dict[Enum.Check],
			Enum.Checkmate => Dict[Enum.Checkmate],
			_ => new()
		};
		if (color != Enum.Default && x % 2 != y % 2)
			enumAsColor *= new Color(darkEffect, darkEffect, darkEffect);
		return enumAsColor;
	}
	public static void SetTileColors(Vector2I flatMousePosition)
	{
		Cursor.LegalSelectedDirections = new();
		LegalSelectedMoves = new();
        Animations.CheckAnimationCancelEarly(flatMousePosition);
        Sprite2D currentSprite = tiles[new(flatMousePosition, Layer.Tile)];
		if (selectedTile != null)
			Deselect((selectedTile ?? default).Location);
		PreviousMoveTiles(Enum.PreviousMove);
		selectedTile = new(flatMousePosition, Layer.Tile);
		if (!Position.pieces.ContainsKey(flatMousePosition))
			return;
		char piece = Position.pieces[flatMousePosition];
		Vector2I enPassantTarget = (Position.EnPassantInfo ?? default).target;
		Set(currentSprite, Enum.Selected, flatMousePosition.X, flatMousePosition.Y);
		for (int i = 0; i < LegalMoves.legalMoves.Count; i++)
		{
			(Vector2I start, Vector2I end) startEndTiles = LegalMoves.legalMoves[i];
			if (startEndTiles.start != flatMousePosition)
				continue;
			Enum color = Enum.Legal;
			LegalSelectedMoves.Add(startEndTiles.end);
			if (Position.EnPassantInfo != null && enPassantTarget == startEndTiles.end && piece.ToString().ToUpper() == "P")
				color = Enum.EnPassant;
			if (LegalMoves.PromotionMoves.Contains(i))
				color = Enum.Promotion;
			if (LegalMoves.CastlingMoves.Contains(i))
				color = Enum.Castling;
			Set(GetTile(startEndTiles.end), color, startEndTiles.end.X, startEndTiles.end.Y);
		}
	}
	public static void ResetAllColors()
	{
		(Vector2I start, Vector2I end) lastMoveNotNull = Position.LastMoveInfo ?? default;
		for (int x = 0; x < tileCount.X; x++)
		{
			for (int y = 0; y < tileCount.Y; y++)
			{
				if (Position.LastMoveInfo != null && lastMoveNotNull.start != new Vector2I(x, y) && lastMoveNotNull.end != new Vector2I(x, y))
					Set(Enum.Default, x, y);
			}
		}
	}
	public static void ChangeTileColorBack()
	{
		(Vector2I start, Vector2I end) lastMoveNotNull = Position.LastMoveInfo ?? default;
		foreach (Vector2I previousCheckTile in Animations.PreviousCheckTiles)
		{
			Enum color = lastMoveNotNull.start == previousCheckTile || lastMoveNotNull.end == previousCheckTile ? Enum.PreviousMove : Enum.Default;
			Set(color, previousCheckTile.X, previousCheckTile.Y);
		}
		Animations.PreviousCheckTiles = new();
	}
	public static void ColorCheckedRoyalTiles(Enum color)
	{
		foreach (Vector2I checkedRoyal in LegalMoves.CheckedRoyals)
			Set(color, checkedRoyal.X, checkedRoyal.Y);
	}
}
