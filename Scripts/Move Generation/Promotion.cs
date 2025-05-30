using Godot;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Promotion
{
	public static List<char> PromotionOptionsPieces = new();
	public static List<Vector2I> PromotionOptionsPositions = new();
	public static readonly char[] CanBePromotedTo = new char[] { 'Q', 'R', 'B', 'N' };
	private const float PromotionOptionTransparency = 0.6f;
	private static char promotionColor;
	public static Vector2I originalPromotionPosition;
	public static Vector2I? promotionPending = null;
	public static bool MoveHistoryDisable = false;
	public static void AvailablePromotions(Vector2I promotionPosition, char colorToMove)
	{
		originalPromotionPosition = promotionPosition;
		promotionColor = colorToMove;
		PromotionOptionsPieces = new(); PromotionOptionsPositions = new();
		int optionDirection = colorToMove == 'w' ? -1 : 1;
		MoveHistoryDisable = true;
		for (int i = 0; i < CanBePromotedTo.Count(); i++)
		{
			char promotable = CanBePromotedTo[i];
			if (colorToMove == 'b')
				promotable = Convert.ToChar(promotable.ToString().ToLower());
			Vector2I optionPosition = new(promotionPosition.X, promotionPosition.Y - optionDirection * i);
			Chessboard.Element pieceBelowOption = new(optionPosition, Chessboard.Layer.Piece);
			if (Chessboard.tiles.ContainsKey(pieceBelowOption))
				Animations.Tween(Chessboard.tiles[pieceBelowOption], Animations.animationSpeed * 2, optionPosition, null, null, 0, false, layer: Chessboard.Layer.Promotion);
			PromotionOptionsPieces.Add(promotable); PromotionOptionsPositions.Add(optionPosition);
		}
		Chessboard.waitingForBoardFlip = false;
        AnimateOptionPieces(promotionPosition, optionDirection);
	}
	private static void AnimateOptionPieces(Vector2I promotionPosition, int optionDirection)
	{
		for (int i = 0; i < PromotionOptionsPieces.Count; i++)
		{
			(char piece, Vector2I position) option = (PromotionOptionsPieces[i], PromotionOptionsPositions[i]);
			Vector2I optionPosition = new(promotionPosition.X, promotionPosition.Y + optionDirection * (CanBePromotedTo.Count() - i));
			Colors.Set(Colors.Enum.Promotion, option.position.X, option.position.Y);
			OptionChosen(option.piece, option.position, optionPosition, PromotionOptionTransparency, Chessboard.Layer.Promotion);
		}
	}
	public static void Promote(Vector2I promotionPosition)
	{
		promotionPending = promotionPosition;
        if ((!Animations.promotionUnsafe || Animations.animationSpeed == 0) && Animations.ActiveTweens.Count == 0)
		{
			PromoteLogic(promotionPosition);
			promotionPending = null;
		}
	}
	public static void AutomaticPromotion(Vector2I location, char piecePromotedTo)
	{
		char chosenPiece = Position.ColorToMove == 'w' ? piecePromotedTo : Convert.ToChar(piecePromotedTo.ToString().ToLower());
		Position.pieces[location] = chosenPiece;
		OptionChosen(chosenPiece, location, location, 1, Chessboard.Layer.Piece);
        LegalMoves.ReverseColor(Position.ColorToMove);
        LegalMoves.GetLegalMoves();
    }
	public static void OptionChosen(char chosenPiece, Vector2I promotionLocation, Vector2I animationStartPosition, float endTransparency, Chessboard.Layer tileLayer, float durationMultiplier = 2)
	{
		Sprite2D sprite = CreateOptionPiece(chosenPiece, animationStartPosition);
		Chessboard.Element tilePositionInDict = new(promotionLocation, tileLayer);
		if (Chessboard.tiles.ContainsKey(tilePositionInDict))
			Chessboard.tiles[tilePositionInDict] = sprite;
		else
            Chessboard.tiles.Add(tilePositionInDict, sprite);
        LoadGraphics.I.AddChild(sprite);
		Animations.Tween(sprite, Animations.animationSpeed * durationMultiplier, animationStartPosition, promotionLocation, null, endTransparency, false, false);
	}
	private static Sprite2D CreateOptionPiece(char piece, Vector2I position)
	{
		return new()
		{
			Texture = LoadGraphics.textureDict[piece.ToString()],
			Position = Chessboard.CalculateTilePosition(position.X, position.Y),
			Modulate = new Color(1, 1, 1, 0),
			Scale = new Vector2(Chessboard.gridScale, Chessboard.gridScale) / Chessboard.svgScale
		};
	}
	private static void PromoteLogic(Vector2I promotionPosition)
	{
		Audio.Play(Audio.Enum.Promotion);
		LegalMoves.ReverseColor(promotionColor);
		Sprite2D selectedPromotionSprite = null;
		int selectedIndex = 0;
		for (int i = 0; i < PromotionOptionsPositions.Count(); i++)
		{
			Vector2I location = PromotionOptionsPositions[i];
			Colors.Set(Colors.Enum.Default, location.X, location.Y);
			Sprite2D handledSprite = Chessboard.tiles[new(location, Chessboard.Layer.Promotion)];
			if (location == promotionPosition)
			{
				Animations.Tween(handledSprite, Animations.animationSpeed, location, originalPromotionPosition, null, 1, false, false, true, -1, -1, true);
				selectedPromotionSprite = handledSprite;
				selectedIndex = i;
			}
			else
				Animations.Tween(handledSprite, Animations.animationSpeed, location, null, null, 0, true, false, false);
			Chessboard.tiles.Remove(new(location, Chessboard.Layer.Promotion));
		}
		Chessboard.tiles.Add(new(originalPromotionPosition, Chessboard.Layer.Piece), selectedPromotionSprite);
		char piecePromotedTo = PromotionOptionsPieces[selectedIndex];
		Position.pieces.Add(originalPromotionPosition, piecePromotedTo);
		UpdatePosition.LatestMove.PiecePromotedTo = piecePromotedTo;
		History.Play(UpdatePosition.LatestMove);
		RestoreObscuredPieces();
		PromotionOptionsPieces = new(); PromotionOptionsPositions = new();
		LegalMoves.GetLegalMoves();
		Interaction.PreviousMoveTiles(Colors.Enum.PreviousMove);
	}
	private static void RestoreObscuredPieces()
	{
		foreach (Vector2I location in PromotionOptionsPositions)
		{
			Chessboard.Element locationKey = new(location, Chessboard.Layer.Piece);
			if (Chessboard.tiles.ContainsKey(locationKey))
				Animations.Tween(Chessboard.tiles[locationKey], Animations.animationSpeed, location, null, null, 1, false);
		}
	}
}
