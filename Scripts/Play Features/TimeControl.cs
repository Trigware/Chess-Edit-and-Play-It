using Godot;
using System.Collections.Generic;

public partial class TimeControl : Node
{
	public static Dictionary<char, PlayerTimer> PlayerTimerInfo;
	public static Dictionary<char, double> TimeLeftAtLastPlyStart;
	public static void SetupTimers()
	{
		PlayerTimerInfo = new()
		{
			{ 'w', new(3600, 0) },
			{ 'b', new(1800, 0) }
		};
	}
	public class PlayerTimer
	{
		public float InitialTime, PlyIncrement;
		public bool HasStarted;
		public Timer actualTimer;
		public PlayerTimer(float initialTime, float plyIncrement)
		{
			InitialTime = initialTime;
			PlyIncrement = plyIncrement;
			actualTimer = new() { Autostart = true, Paused = true, OneShot = true, WaitTime = initialTime };
			LoadGraphics.I.AddChild(actualTimer);
			actualTimer.Timeout += () =>
			{
				actualTimer.QueueFree();
				actualTimer = null;
				PlayerTimeout(LegalMoves.ReverseColorReturn(Position.colorToMove));
			};
		}
		public double GetTimeLeft()
		{
			double timeLeft = 0;
			if (actualTimer != null)
				timeLeft = actualTimer.TimeLeft;
			return timeLeft;
		}
	}
	public static bool HasPlayerTimerStarted(char color) => GetWantedTimer(color).HasStarted;
	private static bool MarkPlayerTimerAsStarted(char color) => GetWantedTimer(color).HasStarted = true;
	private static PlayerTimer GetWantedTimer(char color) => PlayerTimerInfo[color];
	private static double GetTimerTimeLeft(char color) => GetWantedTimer(color).actualTimer.TimeLeft;
	public static void HandleTimerPauseProperty(char color, bool pause = false)
	{
		Timer usedTimer = GetWantedTimer(color).actualTimer;
		if (usedTimer == null) return;
		if (!pause) MarkPlayerTimerAsStarted(color);
		usedTimer.Paused = pause;
	}
	public static void ModifyTimeLeft(char color, double? modifyTo = null)
	{
		PlayerTimer playerTimer = GetWantedTimer(color);
		Timer usedTimer = playerTimer.actualTimer;
		if (usedTimer == null) return;
		if (modifyTo == null) modifyTo = usedTimer.TimeLeft + playerTimer.PlyIncrement;
		usedTimer.Stop();
		usedTimer.WaitTime = modifyTo.Value;
		usedTimer.Start();
	}
	private static void PlayerTimeout(char otherPlayer)
	{
		if (InsufficientMaterial.PlayerMaterialInsufficiency[otherPlayer])
		{
			Position.GameEndState = Position.EndState.InsufficientMaterialVsTimeout;
			Position.WinningPlayer = 'd';
		}
		else
		{
			Position.GameEndState = Position.EndState.Timeout;
			Position.WinningPlayer = LegalMoves.ReverseColorReturn(Position.colorToMove);
		}
		Audio.Play(Audio.Enum.GameEnd);
		if (Interaction.selectedTile != null)
			Interaction.Deselect(Interaction.selectedTile.Value.Location);
		LegalMoves.EndGame();
	}
	public static void RefreshCurrentPlayerTimerTracker()
	{
		TimeLeftAtLastPlyStart = new();
		foreach (char player in Position.playerColors)
			TimeLeftAtLastPlyStart.Add(player, GetTimerTimeLeft(player));
	}
}
