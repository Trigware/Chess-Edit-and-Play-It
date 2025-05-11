using Godot;
using System;
using System.Collections.Generic;

public partial class PauseMenu
{
	public static Sprite2D Main, Outline, CloseButton;
	public const float PauseMenuMoveDuration = 0.5f, PauseScreenAfterGameEndDuration = 0.3f, MaxTextSizeBoundary = 0.9f, PauseMenuMaxVisibilityTransparency = 0.85f, CloseButtonScaleMutliplier = 0.55f;
	public static Dictionary<Text.PauseLabel, Rect2> InteractionHitboxes = new();
	private static bool isPausedValue = false;
	private readonly static Vector2 pauseCloseButtonOffset = new(2.625f, -1.625f);
    public static bool GameEndedInThisSession = false, WaitingForPauseAfterGameEnd = false, MenuMoving = false, UndoingMovesForNewGame = false;
	public static Position.EndState EndStateOnAnimationStart = Position.EndState.Ongoing;
	public static string TitleText = "", DescriptionText = "";
	public static Vector2 MenuTextureSize;
	public static int ActiveTweensCount = 0, UndoMovesCountForResettingGame = 0;
	public static bool IsPaused
	{
		get => isPausedValue;
		set
		{
			if (WaitingForPauseAfterGameEnd || Position.ColorToMove == '\0' || UndoingMovesForNewGame) return;
			if (!isPausedValue && ActiveTweensCount == 0) EndStateOnAnimationStart = Position.GameEndState;
            UpdatePauseMenuText();
			Animations.TweenPauseMenu(Main, Chessboard.Layer.PauseMain, value);
			Animations.TweenPauseMenu(Outline, Chessboard.Layer.PauseOutline, value);
			Animations.TweenPauseMenu(CloseButton, Chessboard.Layer.PauseClose, value);
			TimeControl.PlayerTimer playerTimer = TimeControl.GetWantedTimer(Position.ColorToMove);
			if (playerTimer.HasStarted && Position.GameEndState == Position.EndState.Ongoing)
				playerTimer.ActualTimer.Paused = value;
			isPausedValue = value;
		}
	}
	public static Vector2 GetStandardPosition(Chessboard.Layer layer)
	{
		int yPauseHidePosition = Chessboard.isFlipped ? 0 : Chessboard.tileCount.Y;
		Vector2 spritePosition = (IsPaused ? Chessboard.boardCenter : new(Chessboard.boardCenter.X, yPauseHidePosition)) + GetPositionalOffsetForCloseButton(layer);
        return spritePosition;
	}
	public static Vector2 GetPointOfCloseButton(bool topLeft)
	{
		Vector2 distanceToPoint = (CloseButton.Texture.GetSize() * CloseButton.Scale) / 2;
		if (topLeft) distanceToPoint = -distanceToPoint;
		return CloseButton.Position + distanceToPoint;
	}
	public static Vector2 GetPositionalOffsetForCloseButton(Chessboard.Layer layer)
	{
		if (layer != Chessboard.Layer.PauseClose) return new();
		return pauseCloseButtonOffset * (Chessboard.isFlipped ? -1 : 1);
	}
	public static void UpdatePauseMenuText()
	{
		if (EndStateOnAnimationStart == Position.EndState.Ongoing)
		{
			TitleText = Localization.GetText(Localization.Path.GamePaused);
			DescriptionText = "";
			return;
		}
		TitleText = Localization.GetText(Localization.Path.PauseTitle, Position.WinningPlayer.ToString());
		Localization.TextVariables textVariables = EndStateOnAnimationStart switch
		{
			Position.EndState.Resignation => new(Localization.GetText(Localization.Path.Players, LegalMoves.ReverseColorReturn(Position.WinningPlayer).ToString())),
			Position.EndState.NMoveRule => new(Chessboard.NMoveRuleInPlies/2),
			_ => new()
		};
		DescriptionText = Localization.GetText(textVariables, Localization.Path.PauseDescription, EndStateOnAnimationStart.ToString());
	}
	public static void InteractWithTextElement(Text.PauseLabel pauseInteraction)
	{
		switch (pauseInteraction)
		{
			case Text.PauseLabel.NewGame: TriggerNewGame(); break;
		}
	}
	private static void TriggerNewGame()
	{
		IsPaused = false;
		if (Interaction.selectedTile != null)
			Interaction.Deselect(Interaction.selectedTile.Value.Location);
		UndoingMovesForNewGame = History.UndoMoves.Count > 0;
		UndoMovesCountForResettingGame = History.UndoMoves.Count;
		Position.GameEndState = Position.EndState.Ongoing;
		if (UndoingMovesForNewGame)
		{
			History.Undo();
			Cursor.ShowHideCursor(false);
			return;
		}
		Audio.Play(Audio.Enum.GameStart);
		Position.ColorToMove = Position.StartColorToMove;
		TimeControl.RestartTimer(Position.ColorToMove);
	}
	public static void UndoMoveForNewGame()
	{
		if (History.UndoMoves.Count > 0)
		{
			History.Undo();
			return;
		}
		UndoingMovesForNewGame = false;
		History.RedoMoves = new();
		Audio.Play(Audio.Enum.GameStart);
		foreach (char playerColor in Position.playerColors)
			TimeControl.RestartTimer(playerColor);
		Zobrist.RepeatedPositions = new();
		if (Position.ColorToMove != Position.StartColorToMove)
		{
			Position.ColorToMove = Position.StartColorToMove;
			LegalMoves.GetLegalMoves(gameReset: true);
		}
	}
}
