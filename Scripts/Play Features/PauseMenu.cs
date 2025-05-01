using Godot;
using System.Collections.Generic;

public partial class PauseMenu
{
	public static Sprite2D Main, Outline;
	public static Label TitleLabel = null, DescriptionLabel = null;
	public const float pauseDuration = 0.5f, PauseScreenAfterGameEndDuration = 0.3f;
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
			if (WaitingForPauseAfterGameEnd) return;
			UpdatePauseMenuText();
			Animations.TweenPauseMenu(Main, Chessboard.Layer.PauseMain, value);
			Animations.TweenPauseMenu(Outline, Chessboard.Layer.PauseOutline, value);
			TimeControl.PlayerTimer playerTimer = TimeControl.GetWantedTimer(Position.ColorToMove);
			if (playerTimer.HasStarted && Position.GameEndState == Position.EndState.Ongoing)
				playerTimer.ActualTimer.Paused = value;
			isPausedValue = value;
		}
	}
	public static Vector2 GetStandardPosition()
	{
		int yPauseHidePosition = Chessboard.isFlipped ? 0 : Chessboard.tileCount.Y;
		return IsPaused ? Chessboard.boardCenter : new(Chessboard.boardCenter.X, yPauseHidePosition);
	}
	public static void UpdatePauseMenuText()
	{
		if (Position.GameEndState == Position.EndState.Ongoing)
		{
			TitleText = "Game Paused";
			DescriptionText = "";
			return;
		}
		TitleText = Position.WinningPlayer switch
		{
			'd' => "Game Drawn",
			'w' => "White Won",
			_ => "Black Won"
		} + "!";
		DescriptionText = "Due to " + Position.GameEndState switch
		{
			Position.EndState.Checkmate => "checkmate",
			Position.EndState.Timeout => "timeout",
			Position.EndState.Resignation => "resignation", // unused, add later

			Position.EndState.Stalemate => "stalemate",
			Position.EndState.ThreefoldRepetition => "repeated position",
			Position.EndState.FiftyMoveRule => $"the {Chessboard.NMoveRuleInPlies/2}-move rule",
			Position.EndState.InsufficientMaterial => "insufficient material",
			Position.EndState.TimeoutVsInsufficientMaterial => "timeout vs insufficient material",
			Position.EndState.DrawAgreement => "an agreement" // unused, add later
		} + "!";
	}
}
