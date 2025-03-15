using Godot;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Promotion : Node
{
	public static List<char> PromotionOptionsPieces = new();
	public static List<Vector2I> PromotionOptionsPositions = new();
	public static readonly char[] CanBePromotedTo = new char[] { 'Q', 'R', 'B', 'N' };
	private const float PromotionOptionTransparency = 0.6f;
	private static char promotionColor;
	private static Vector2I originalPromotionPosition;
	public static Vector2I? promotionPending = null;
	public static void AvailablePromotions(Vector2I promotionPosition, char colorToMove)
	{
		originalPromotionPosition = promotionPosition;
		promotionColor = colorToMove;
		PromotionOptionsPieces = new(); PromotionOptionsPositions = new();
		int optionDirection = colorToMove == 'w' ? -1 : 1;
		for (int i = 0; i < CanBePromotedTo.Count(); i++)
		{
			char promotable = CanBePromotedTo[i];
			if (colorToMove == 'b')
				promotable = Convert.ToChar(promotable.ToString().ToLower());
			Vector2I optionPosition = new(promotionPosition.X, promotionPosition.Y - optionDirection * i);
			Vector3I pieceBelowOption = new(optionPosition.X, optionPosition.Y, 1);
			if (Update.tiles.ContainsKey(pieceBelowOption))
				Animations.Tween(Update.tiles[pieceBelowOption], Animations.animationSpeed * 2, optionPosition, null, null, 0, false);
			PromotionOptionsPieces.Add(promotable); PromotionOptionsPositions.Add(optionPosition);
		}
		AnimateOptionPieces(promotionPosition, optionDirection);
	}
	private static void AnimateOptionPieces(Vector2I promotionPosition, int optionDirection)
	{
		for (int i = 0; i < PromotionOptionsPieces.Count; i++)
		{
			(char piece, Vector2I position) option = (PromotionOptionsPieces[i], PromotionOptionsPositions[i]);
			Vector2I optionPosition = new(promotionPosition.X, promotionPosition.Y + optionDirection * (CanBePromotedTo.Count() - i));
			Colors.Set(Colors.Enum.Promotion, option.position.X, option.position.Y);
			Sprite2D sprite = CreateOptionPiece(option.piece, optionPosition);
			Update.tiles.Add(new(option.position.X, option.position.Y, 2), sprite);
			LoadGraphics.I.AddChild(sprite);
			Animations.Tween(sprite, Animations.animationSpeed * 2, optionPosition, option.position, null, PromotionOptionTransparency, false, true);
		}
	}
	public static void Promote(Vector2I promotionPosition)
	{
		promotionPending = promotionPosition;
		if (!Animations.promotionUnsafe)
		{
			PromoteLogic(promotionPosition);
			promotionPending = null;
		}
	}
	private static Sprite2D CreateOptionPiece(char piece, Vector2I position)
	{
		return new()
		{
			Texture = LoadGraphics.textureDict[piece.ToString()],
			Position = Chessboard.CalculateTilePosition(position.X, position.Y),
			Modulate = new Color(1, 1, 1, 0),
			Scale = new Vector2(Chessboard.gridScale, Chessboard.gridScale) / Update.svgScale
		};
	}
	private static void PromoteLogic(Vector2I promotionPosition)
	{
		Audio.Play(Audio.Enum.Promotion);
		Position.ReverseColor(promotionColor);
		Sprite2D selectedPromotionSprite = null;
		int selectedIndex = 0;
		for (int i = 0; i < PromotionOptionsPositions.Count(); i++)
		{
			Vector2I location = PromotionOptionsPositions[i];
			Colors.Set(Colors.Enum.Default, location.X, location.Y);
			Sprite2D handledSprite = Chessboard.tiles[new(location.X, location.Y, 2)];
			if (location == promotionPosition)
			{
				Animations.Tween(handledSprite, Animations.animationSpeed, location, originalPromotionPosition, null, 1, false);
				selectedPromotionSprite = handledSprite;
				selectedIndex = i;
			}
			else
				Animations.Tween(handledSprite, Animations.animationSpeed, location, null, null, 0, true, false, false);
			Chessboard.tiles.Remove(new(location.X, location.Y, 2));
		}
		Chessboard.tiles.Add(new(originalPromotionPosition.X, originalPromotionPosition.Y, 1), selectedPromotionSprite);
		Position.pieces.Add(originalPromotionPosition, PromotionOptionsPieces[selectedIndex]);
		RestoreObscuredPieces();
		PromotionOptionsPieces = new(); PromotionOptionsPositions = new();
		LegalMoves.GetLegalMoves();
		Interaction.PreviousMoveTiles(Colors.Enum.PreviousMove);
	}
	private static void RestoreObscuredPieces()
	{
		foreach (Vector2I location in PromotionOptionsPositions)
		{
			Vector3I locationKey = new(location.X, location.Y, 1);
			if (Chessboard.tiles.ContainsKey(locationKey))
				Animations.Tween(Chessboard.tiles[locationKey], Animations.animationSpeed, location, null, null, 1, false);
		}
	}
}
