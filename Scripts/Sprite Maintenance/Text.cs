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
		Vector2I helperIntersectionTile = Chessboard.isFlipped ? new(7, 0) : new(0, 7);
		if (location.X != helperIntersectionTile.X && location.Y != helperIntersectionTile.Y) return;
		if ((location.X != helperIntersectionTile.X && !Chessboard.isFlipped) || (location.Y != helperIntersectionTile.Y && Chessboard.isFlipped)) rankIdentifier = false;

		bool adjustedIsRank = Chessboard.isFlipped ? !rankIdentifier : rankIdentifier;
		char recognitionChar = adjustedIsRank ? Notation.rankNumbers[location.Y] : Notation.fileLetters[location.X];
        tileRecognitionLabels.Add(Print(recognitionChar.ToString(), location, rankIdentifier ? -1 : 1));

		if (location == helperIntersectionTile && rankIdentifier) TileRecognitionHelper(location, false);
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

        Vector2 size = labelInstance.Size * textRescale / 2;
        Vector2 usedLocation = new(location.X + (tileHelperOffset + 0.05f) * isRank, location.Y + tileHelperOffset * isRank);
        Vector2 position = Chessboard.CalculateTilePosition(usedLocation.X, usedLocation.Y);
        labelInstance.Position = position - size;
		return labelInstance;
	}
}
