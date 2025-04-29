using Godot;
using System;

public partial class PauseMenu
{
	public static Sprite2D Main, Outline;
	public const float pauseDuration = 0.5f, PauseScreenAfterGameEndDuration = 0.3f;
	private static bool isPausedValue = false;
	public static bool GameEndedInThisSession = false, WaitingForPauseAfterGameEnd = false;
    public static bool IsPaused
	{
		get => isPausedValue;
        set
        {
			if (WaitingForPauseAfterGameEnd) return;
			Animations.TweenPauseMenu(Main, Chessboard.Layer.PauseMain, value);
			Animations.TweenPauseMenu(Outline, Chessboard.Layer.PauseOutline, value);
			TimeControl.PlayerTimer playerTimer = TimeControl.GetWantedTimer(Position.colorToMove);
			if (playerTimer.HasStarted && Position.GameEndState == Position.EndState.Ongoing)
				playerTimer.ActualTimer.Paused = value;
            isPausedValue = value;
        }
    }
	public static Vector2 GetPosition()
	{
        int yPauseHidePosition = Chessboard.isFlipped ? 0 : Chessboard.tileCount.Y;
        return IsPaused ? Chessboard.boardCenter : new(Chessboard.boardCenter.X, yPauseHidePosition);
    }

}