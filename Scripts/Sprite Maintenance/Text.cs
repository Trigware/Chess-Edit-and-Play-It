using Godot;
using System;
using System.Collections.Generic;

public partial class Text : Node
{
	public static PackedScene simpleLabel;
	public static List<Label> activeLabels = new();
	public static FontFile notoSans;
	private const float tileHelperOffset = 0.35f, intendedScale = 1.62f, PauseDescriptionYOffset = 0.2f;
	private const int TileRecognitionHelperSize = 32, PauseTitleSize = 96, PauseDescriptionSize = 48;
	private static Dictionary<char, Label> timerLabels = new();

	public static void DeleteRecognitionLabels()
	{
		foreach (Label label in activeLabels)
			label.QueueFree();
		activeLabels.Clear();
	}
	public static void RefreshPauseText()
	{
		if (PauseMenu.TitleLabel != null)
			PauseMenu.TitleLabel.QueueFree();
		if (PauseMenu.DescriptionLabel != null)
			PauseMenu.DescriptionLabel.QueueFree();
		PauseMenu.TitleLabel = PrintPauseMenuElement(PauseMenu.TitleText, PauseTitleSize, 0);
		PauseMenu.DescriptionLabel = PrintPauseMenuElement(PauseMenu.DescriptionText, PauseDescriptionSize, PauseDescriptionYOffset);
	}
	public static void TileRecognitionHelper(Vector2I location, bool rankIdentifier = true)
	{
		Vector2I helperIntersectionTile = Chessboard.isFlipped ? new(7, 0) : new(0, 7);
		if (location.X != helperIntersectionTile.X && location.Y != helperIntersectionTile.Y) return;
		if ((location.X != helperIntersectionTile.X && !Chessboard.isFlipped) || (location.Y != helperIntersectionTile.Y && Chessboard.isFlipped)) rankIdentifier = false;

		bool adjustedIsRank = Chessboard.isFlipped ? !rankIdentifier : rankIdentifier;
		char recognitionChar = adjustedIsRank ? Notation.rankNumbers[location.Y] : Notation.fileLetters[location.X];
		activeLabels.Add(Print(recognitionChar.ToString(), location, Colors.GetColorFromEnum(Colors.Enum.Default, location.X + 1, location.Y), TileRecognitionHelperSize, rankIdentifier ? -1 : 1));

		if (location == helperIntersectionTile && rankIdentifier) TileRecognitionHelper(location, false);
	}
	public static Label Print(string text, Vector2 location, Color textColor, int textSize, int? isRank = null, Chessboard.Layer layer = Chessboard.Layer.Helper, bool isPauseText = false, Vector2 pauseOffset = default)
	{
		Node labelNode = simpleLabel.Instantiate();
		if (!(labelNode is Label)) return null;
		Label labelInstance = (Label)labelNode;
		float textRescale = Chessboard.gridScale / intendedScale / 2;

		labelInstance.Text = text;
		labelInstance.ZIndex = (int)layer;
		labelInstance.Scale *= textRescale;

		labelInstance.AddThemeColorOverride("font_color", textColor);
		labelInstance.AddThemeFontOverride("font", notoSans);
		labelInstance.AddThemeFontSizeOverride("font_size", textSize);
		LoadGraphics.I.AddChild(labelInstance);

		Vector2 controlSizeScaled = labelInstance.Size * textRescale / 2;
		if (layer == Chessboard.Layer.Helper) GetPositionOfTileRecognitionHelper(isRank, location, controlSizeScaled, labelInstance);
		else labelInstance.Position = isPauseText ? RepositionPauseText(location, controlSizeScaled, textRescale, labelInstance, pauseOffset) : location;

		return labelInstance;
	}
	private static Vector2 RepositionPauseText(Vector2 location, Vector2 textSize, float rescale, Label labelInstance, Vector2 pauseOffset)
	{
        Vector2 rescalingToFit = PauseMenu.Main.Scale * PauseMenu.MenuTextureSize * PauseMenu.PauseMenuTextboxSize / (labelInstance.Size * rescale);
        float actualTextRescale = Math.Min(rescalingToFit.X, rescalingToFit.Y);
		if (actualTextRescale < 1)
		{
			labelInstance.Scale *= actualTextRescale;
            textSize *= actualTextRescale;
        }
        Vector2 textPosition = location - textSize;
		float distanceToMenuTop = PauseMenu.MenuTextureSize.Y * rescale;
		pauseOffset *= -2;
		return new(textPosition.X, textPosition.Y - distanceToMenuTop * (1 + pauseOffset.Y));
	}
	private static void GetPositionOfTileRecognitionHelper(int? isRank, Vector2 location, Vector2 size, Label labelInstance)
	{
		Vector2 usedLocation = new(location.X + (tileHelperOffset + 0.05f) * isRank.Value, location.Y + tileHelperOffset * isRank.Value);
		Vector2 position = Chessboard.CalculateTilePosition(usedLocation.X, usedLocation.Y);
		labelInstance.Position = position - size;
	}
	private static Label PrintPauseMenuElement(string printedText, int textSize, float yOffset)
	{
		Color pauseColor = new(Colors.Dict[Colors.Enum.PauseText], PauseMenu.Main.Modulate.A);
		return Print(printedText, PauseMenu.Main.Position, pauseColor, textSize, layer: Chessboard.Layer.PauseText, isPauseText: true, pauseOffset: new(0, yOffset));
	}

	[Obsolete("This timer is used for debug purposes only, replace with a scalable version that contains GUI elements when needed")]
	public static void ShowTimers()
	{
		int timersPrinted = 0;
		foreach (KeyValuePair<char, TimeControl.PlayerTimer> timeControlPair in TimeControl.PlayerTimerInfo)
		{
			string timeLeft = timeControlPair.Value.GetTimeLeft();
			char playerChar = timeControlPair.Key;
			if (timerLabels.ContainsKey(playerChar)) timerLabels[playerChar].Text = timeLeft;
			else timerLabels.Add(playerChar, Print(timeLeft, new Vector2I(0, timersPrinted * 50), Colors.Dict[Colors.Enum.WhiteColorToMove], 100, layer: Chessboard.Layer.Timers));
			timersPrinted++;
		}
	}
}
