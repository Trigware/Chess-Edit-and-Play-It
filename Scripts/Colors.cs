using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
public partial class Colors : Interaction
{
	private const float darkEffect = 0.6f; // 0 dark, 1 light
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
		spr.Modulate = color switch
		{
			Enum.Default => (x % 2 == y % 2) ? Dict[Enum.DefaultLight] : Dict[Enum.DefaultDark],
			Enum.Legal => Dict[Enum.Legal],
			Enum.Selected => Dict[Enum.Selected],
			Enum.PreviousMove => Dict[Enum.PreviousMove],
			Enum.EnPassant => Dict[Enum.EnPassant],
			Enum.Promotion => Dict[Enum.Promotion],
			Enum.Castling => Dict[Enum.Castling],
			_ => new()
		};
		if (color != Enum.Default && x % 2 != y % 2)
			spr.Modulate *= new Color(darkEffect, darkEffect, darkEffect);
	}
	public static void SetTileColors(Vector2I flatMousePosition)
	{
		Sprite2D currentSprite = tiles[new(flatMousePosition.X, flatMousePosition.Y, 0)];
		if (selectedTile != -Vector3I.One)
			Deselect(selectedTile);
		PreviousMoveTiles(Enum.PreviousMove);
		selectedTile = new(flatMousePosition.X, flatMousePosition.Y, 0);
		if (!Position.pieces.ContainsKey(flatMousePosition))
			return;
		char piece = Position.pieces[flatMousePosition];
		Set(currentSprite, Enum.Selected, flatMousePosition.X, flatMousePosition.Y);
		for (int i = 0; i < LegalMoves.legalMoves.Count; i++)
		{
			(Vector2I start, Vector2I end) startEndTiles = LegalMoves.legalMoves[i];
			if (startEndTiles.start != flatMousePosition)
				continue;
			Enum color = Enum.Legal;
			if (Position.EnPassantInfo.target == startEndTiles.end && piece.ToString().ToUpper() == "P")
				color = Enum.EnPassant;
			if (LegalMoves.PromotionMoves.Contains(i))
				color = Enum.Promotion;
			if (LegalMoves.CastlingMoves.Contains(i))
				color = Enum.Castling;
			Set(tiles[new(startEndTiles.end.X, startEndTiles.end.Y, 0)], color, startEndTiles.end.X, startEndTiles.end.Y);
		}
	}
}
