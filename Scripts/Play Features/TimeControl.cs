using Godot;
using System.Collections.Generic;

public partial class TimeControl : Node
{
	public static Dictionary<char, PlayerTimer> PlayerTimerInfo;
	public static Dictionary<char, double> TimeLeftAtLastPlyStart;
	private const float LowTimeDivisor = 10;
	public static void SetupTimers()
	{
		PlayerTimerInfo = new()
		{
			{ 'w', new(600, 0) },
			{ 'b', new(600, 0) }
		};
	}
	public class PlayerTimer
	{
		public double InitialTime, PlyIncrement, LowTime;
		public bool PlayerTimeout = false, LowTimeReached = false, HasStarted = false;
		public Timer ActualTimer;
		public PlayerTimer(double initialTime, double plyIncrement)
		{
			InitialTime = initialTime;
			PlyIncrement = plyIncrement;
			LowTime = initialTime / LowTimeDivisor;
			ActualTimer = new() { Autostart = true, Paused = true, OneShot = true, WaitTime = initialTime };
			LoadGraphics.I.AddChild(ActualTimer);
			ActualTimer.Timeout += () =>
			{
				PlayerTimeout = true;
				PlayerTimeout(LegalMoves.ReverseColorReturn(Position.colorToMove));
			};
		}
		public string GetTimeLeft()
		{
			double timeLeft = ActualTimer.TimeLeft;
			if (PlayerTimeout && History.RedoMoves.Count == 0) timeLeft = 0;
			return ConvertToNormalFormat(timeLeft);
		}
	}
	public static PlayerTimer GetWantedTimer(char color) => PlayerTimerInfo[color];
	public static double GetTimerTimeLeft(char color) => GetWantedTimer(color).ActualTimer.TimeLeft;
	public static void HandleTimerPauseProperty(char color, bool pause = false)
	{
		PlayerTimer playerTimer = GetWantedTimer(color);
        Timer usedTimer = playerTimer.ActualTimer;
		usedTimer.Paused = pause;
		playerTimer.HasStarted = true;
	}
	public static void ModifyTimeLeft(char color, double? modifyTo = null)
	{
		PlayerTimer playerTimer = GetWantedTimer(color);
		Timer usedTimer = playerTimer.ActualTimer;
		if (usedTimer == null) return;
		if (modifyTo == null) modifyTo = usedTimer.TimeLeft + playerTimer.PlyIncrement;
		else if (modifyTo.Value <= 0) return;

		if (modifyTo > playerTimer.LowTime) playerTimer.LowTimeReached = false;
		usedTimer.Stop();
		usedTimer.WaitTime = modifyTo.Value;
		usedTimer.Start();
	}
	private static void PlayerTimeout(char otherPlayer)
	{
		if (History.UndoMoves.Count > 0)
		{
			History.Move latestPlayedMove = History.UndoMoves.Peek();
			latestPlayedMove.TimeLeftEnd = GetPlayerTimersTimeLeft();
		}
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
	public static Dictionary<char, double> GetPlayerTimersTimeLeft()
	{
		Dictionary<char, double> timeLeftDict = new();
		timeLeftDict = new();
		foreach (char player in Position.playerColors)
			timeLeftDict.Add(player, GetTimerTimeLeft(player));
		return timeLeftDict;
	}
	private static string ConvertToNormalFormat(double seconds)
	{
		double secondsLeft = seconds % 60;
		int hoursLeft = (int)(seconds / 3600), minutesLeft = (int)((seconds % 3600) / 60), millisecondsLeft = (int)((secondsLeft - (int)secondsLeft) * 100);
		string timeFormatted = "";
		if (hoursLeft > 0) timeFormatted += hoursLeft + ":";
		if (minutesLeft > 0 || hoursLeft > 0) timeFormatted += AddZeroToMakeNumberTwoDigit(minutesLeft, hoursLeft == 0) + ":";
		timeFormatted += AddZeroToMakeNumberTwoDigit((int)secondsLeft, minutesLeft == 0 && hoursLeft < 1);
		if (seconds < 60) timeFormatted += "." + AddZeroToMakeNumberTwoDigit(millisecondsLeft, false);
		return timeFormatted;
	}
	private static string AddZeroToMakeNumberTwoDigit(double number, bool keepAsIs)
	{
		string modifiedNumber = number.ToString();
		if (modifiedNumber.Length == 1 && !keepAsIs) modifiedNumber = "0" + modifiedNumber;
		return modifiedNumber;
	}
	public static void CheckIfOnLowTime()
	{
		PlayerTimer activeTimer = GetWantedTimer(Position.colorToMove);
		if (!activeTimer.LowTimeReached && activeTimer.ActualTimer.TimeLeft <= activeTimer.LowTime)
		{
			activeTimer.LowTimeReached = true;
			Audio.Play(Audio.Enum.LowTime);
        }
	}
}
