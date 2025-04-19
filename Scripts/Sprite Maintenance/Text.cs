using Godot;
using System.Collections.Generic;

public partial class Text : Node
{
	public static PackedScene simpleLabel;
	public static List<Label> tileRecognitionLabels = new();
    public static FontFile notoSans;
    private const float tileHelperOffset = 0.35f, intendedScale = 1.62f;

	public static void DeleteRecognitionLabels()
	{
		foreach (Label label in tileRecognitionLabels)
			label.QueueFree();
		tileRecognitionLabels.Clear();
	}
	public static void TileRecognitionHelper(Vector2I location, bool rankIdentifier = true)
	{
		if (location.X != 0 && location.Y != 7) return;
		if (location.X != 0) rankIdentifier = false;
		char recognitionChar = rankIdentifier ? Notation.rankNumbers[location.Y] : Notation.fileLetters[location.X];
		tileRecognitionLabels.Add(Print(recognitionChar.ToString(), location, rankIdentifier ? -1 : 1));
		if (location.X == 0 && location.Y == 7 && rankIdentifier) TileRecognitionHelper(location, false);
	}
	public static Label Print(string text, Vector2I location, int isRank)
	{
        Node labelNode = simpleLabel.Instantiate();
		if (!(labelNode is Label)) return null;
		Label labelInstance = (Label)labelNode;
		float textRescale = Chessboard.gridScale / intendedScale / 2;

        labelInstance.Text = text;
        labelInstance.ZIndex = (int)Chessboard.Layer.Text;
		labelInstance.Scale *= textRescale;

		labelInstance.AddThemeColorOverride("font_color", Colors.GetColorFromEnum(Colors.Enum.Default, location.X+1, location.Y));
		labelInstance.AddThemeFontOverride("font", notoSans);
        labelInstance.AddThemeFontSizeOverride("font_size", 32);
        LoadGraphics.I.AddChild(labelInstance);

        Vector2 size = labelInstance.Size;
        Vector2 usedLocation = new(location.X + (tileHelperOffset + 0.05f) * isRank, location.Y + tileHelperOffset * isRank);
        Vector2 position = Chessboard.CalculateTilePosition(usedLocation.X, usedLocation.Y);
        labelInstance.Position = position - size * textRescale / 2;
		return labelInstance;
	}
}
