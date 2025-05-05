using Godot;
using System.Collections.Generic;

public partial class PauseMenu
{
	public static Sprite2D Main, Outline;
	public const float PauseMenuMoveDuration = 0.5f, PauseScreenAfterGameEndDuration = 0.3f, MaxTextSizeBoundary = 0.9f, PauseMenuMaxVisibilityTransparency = 0.85f;
	public static Dictionary<Text.PauseLabel, Rect2> InteractionHitboxes = new();
	private static bool isPausedValue = false;
	public static bool GameEndedInThisSession = false, WaitingForPauseAfterGameEnd = false, MenuMoving = false;
	public static string TitleText = "", DescriptionText = "";
	public static Vector2 MenuTextureSize;
	public static int ActiveTweens = 0;
	public static bool IsPaused
	{
		get => isPausedValue;
		set
		{
			if (WaitingForPauseAfterGameEnd || Position.ColorToMove == '\0') return;
			UpdatePauseMenuText();
			Animations.TweenPauseMenu(Main, Chessboard.Layer.PauseMain, value);
			Animations.TweenPauseMenu(Outline, Chessboard.Layer.PauseOutline, value);
			TimeControl.PlayerTimer playerTimer = TimeControl.GetWantedTimer(Position.ColorToMove);
			if (playerTimer.HasStarted && Position.GameEndState == Position.EndState.Ongoing)
				playerTimer.ActualTimer.Paused = value;
			isPausedValue = value;
		}
	}
	public static Vector2 GetStandardPosition(Chessboard.Layer layer)
	{
		int yPauseHidePosition = Chessboard.isFlipped ? 0 : Chessboard.tileCount.Y;
		return IsPaused ? Chessboard.boardCenter : new(Chessboard.boardCenter.X, yPauseHidePosition);
	}
	public static void UpdatePauseMenuText()
	{
		if (Position.GameEndState == Position.EndState.Ongoing)
		{
			TitleText = Localization.GetText(Localization.Path.GamePaused);
			DescriptionText = "";
			return;
		}
		TitleText = Localization.GetText(Localization.Path.PauseTitle, Position.WinningPlayer.ToString());
		Localization.TextVariables textVariables = Position.GameEndState switch
		{
			Position.EndState.Resignation => new(Localization.GetText(Localization.Path.Players, LegalMoves.ReverseColorReturn(Position.WinningPlayer).ToString())),
			Position.EndState.NMoveRule => new(Chessboard.NMoveRuleInPlies/2),
			_ => new()
		};
		DescriptionText = Localization.GetText(textVariables, Localization.Path.PauseDescription, Position.GameEndState.ToString());
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
		History.Undo(true);
    }
}
