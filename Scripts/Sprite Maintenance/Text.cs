using Godot;
using System.Collections.Generic;

public partial class Text : Node
{
	public static PackedScene simpleLabel;
	public static List<Label> tileRecognitionLabels = new();
    public static FontFile notoSans;
    private const float tileHelperOffset = 0.35f, intendedScale = 1.62f;
	private static Dictionary<char, Label> timerLabels = new();

	public static void DeleteRecognitionLabels()
	{
		foreach (Label label in tileRecognitionLabels)
			label.QueueFree();
		tileRecognitionLabels.Clear();
	}
	public static void TileRecognitionHelper(Vector2I location, bool rankIdentifier = true)
	{
		Vector2I helperIntersectionTile = Chessboard.isFlipped ? new(7, 0) : new(0, 7);
		if (location.X != helperIntersectionTile.X && location.Y != helperIntersectionTile.Y) return;
		if ((location.X != helperIntersectionTile.X && !Chessboard.isFlipped) || (location.Y != helperIntersectionTile.Y && Chessboard.isFlipped)) rankIdentifier = false;

		bool adjustedIsRank = Chessboard.isFlipped ? !rankIdentifier : rankIdentifier;
		char recognitionChar = adjustedIsRank ? Notation.rankNumbers[location.Y] : Notation.fileLetters[location.X];
        tileRecognitionLabels.Add(Print(recognitionChar.ToString(), location, Colors.GetColorFromEnum(Colors.Enum.Default, location.X + 1, location.Y), 32, rankIdentifier ? -1 : 1));

		if (location == helperIntersectionTile && rankIdentifier) TileRecognitionHelper(location, false);
	}
	public static Label Print(string text, Vector2 location, Color textColor, int textSize, int? isRank = null, Chessboard.Layer layer = Chessboard.Layer.Helper)
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

		if (layer == Chessboard.Layer.Helper) GetPositionOfTileRecognitionHelper(isRank, location, textRescale, labelInstance);
		else labelInstance.Position = location;

		return labelInstance;
	}
	private static void GetPositionOfTileRecognitionHelper(int? isRank, Vector2 location, float textRescale, Label labelInstance)
	{
        Vector2 size = labelInstance.Size * textRescale / 2;
        Vector2 usedLocation = new(location.X + (tileHelperOffset + 0.05f) * isRank.Value, location.Y + tileHelperOffset * isRank.Value);
        Vector2 position = Chessboard.CalculateTilePosition(usedLocation.X, usedLocation.Y);
        labelInstance.Position = position - size;
    }
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
