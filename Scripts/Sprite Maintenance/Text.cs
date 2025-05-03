using Godot;
using System;
using System.Collections.Generic;

public partial class Text : Node
{
	public static PackedScene simpleLabel;
	public static List<Label> PauseMenuLabels = new(), TileRecognitionLabels = new();
	public static FontFile notoSans;
	private const float tileHelperOffset = 0.35f, intendedScale = 1.62f, PauseDescriptionYOffset = 0.2f, PauseInteractionYOffset = 1.05f, PauseInteractionXOffset = 0.5f;
	private const int TileRecognitionHelperSize = 32, PauseTitleSize = 96, PauseDescriptionSize = 48, PauseInteractionTextSize = 54;
	private static Dictionary<char, Label> timerLabels = new();

	private enum PauseLabel
	{
		Title,
		Description,
		PlayAgain,
		Settings,
		Quit
	}
	public static void DeleteAllLabelsInList(List<Label> labelList)
	{
		foreach (Label label in labelList)
			label?.QueueFree();
		labelList.Clear();
	}
	public static void RefreshPauseText(bool calledFromDraw = false)
	{
		GD.Print(PauseMenuLabels.Count);
		DeleteAllLabelsInList(PauseMenuLabels);
		foreach (PauseLabel pauseLabelEnum in Enum.GetValues(typeof(PauseLabel)))
		{
			float interactionLabelsXOffset = (pauseLabelEnum - PauseLabel.Settings) * PauseInteractionXOffset;
			PauseMenuLabels.Add(pauseLabelEnum switch
			{
				PauseLabel.Title => PrintPauseMenuElement(PauseMenu.TitleText, PauseTitleSize),
				PauseLabel.Description => PrintPauseMenuElement(PauseMenu.DescriptionText, PauseDescriptionSize, PauseDescriptionYOffset),
				_ => PrintPauseMenuElement(Localization.GetText(Localization.Path.PauseInteraction, pauseLabelEnum.ToString()), PauseInteractionTextSize, PauseInteractionYOffset, interactionLabelsXOffset)
			});
		}
	}
	public static void TileRecognitionHelper(Vector2I location, bool rankIdentifier = true)
	{
		Vector2I helperIntersectionTile = Chessboard.isFlipped ? new(7, 0) : new(0, 7);
		if (location.X != helperIntersectionTile.X && location.Y != helperIntersectionTile.Y) return;
		if ((location.X != helperIntersectionTile.X && !Chessboard.isFlipped) || (location.Y != helperIntersectionTile.Y && Chessboard.isFlipped)) rankIdentifier = false;

		bool adjustedIsRank = Chessboard.isFlipped ? !rankIdentifier : rankIdentifier;
		char recognitionChar = adjustedIsRank ? Notation.rankNumbers[location.Y] : Notation.fileLetters[location.X];
		TileRecognitionLabels.Add(Print(recognitionChar.ToString(), location, Colors.GetColorFromEnum(Colors.Enum.Default, location.X + 1, location.Y), TileRecognitionHelperSize, rankIdentifier ? -1 : 1));

		if (location == helperIntersectionTile && rankIdentifier) TileRecognitionHelper(location, false);
	}
	public static Label Print(string text, Vector2 location, Color textColor, int textSize, int? isRank = null, Chessboard.Layer layer = Chessboard.Layer.Helper, bool isPauseText = false, Vector2 pauseOffset = default)
	{
		if (text == "")
			return null;
		Node labelNode = simpleLabel.Instantiate();
		if (!(labelNode is Label)) return null;
		Label labelInstance = (Label)labelNode;
		float textRescale = GetTextRescale();

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
		Vector2 distanceToMenuTopLeft = PauseMenu.MenuTextureSize * rescale;
		pauseOffset *= -2;
		return new(textPosition.X - distanceToMenuTopLeft.X * pauseOffset.X, textPosition.Y - distanceToMenuTopLeft.Y * (1 + pauseOffset.Y));
	}
	private static void GetPositionOfTileRecognitionHelper(int? isRank, Vector2 location, Vector2 size, Label labelInstance)
	{
		Vector2 usedLocation = new(location.X + (tileHelperOffset + 0.05f) * isRank.Value, location.Y + tileHelperOffset * isRank.Value);
		Vector2 position = Chessboard.CalculateTilePosition(usedLocation.X, usedLocation.Y);
		labelInstance.Position = position - size;
	}
	private static Label PrintPauseMenuElement(string printedText, int textSize, float yOffset = 0, float xOffset = 0)
	{
		Color pauseColor = new(Colors.Dict[Colors.Enum.PauseText], PauseMenu.Main.Modulate.A);
		return Print(printedText, PauseMenu.Main.Position, pauseColor, textSize, layer: Chessboard.Layer.PauseText, isPauseText: true, pauseOffset: new(xOffset, yOffset));
	}
	private static float GetTextRescale() => Chessboard.gridScale / intendedScale / 2;

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
