using Godot;
using System;
using System.Collections.Generic;
using static Godot.CameraFeed;

public partial class Update : Chessboard
{
	public const float svgScale = 5;
	public static Tween scaleUp;
	public static void Board()
	{
		foreach (KeyValuePair<Vector3I, Sprite2D> keyValue in tiles)
			DrawTile(keyValue.Key, keyValue.Value);
	}
	public static void DeleteBoard()
	{
		foreach (Sprite2D sprite in tiles.Values)
			sprite.QueueFree();
		tiles = new();
		leftUpCorner = null;
	}
	private static void DrawTile(Vector3I pos, Sprite2D sprite)
	{
		Vector2I gridPosition = new(pos.X, pos.Y);
		Vector2 worldPosition = new Vector2(pos.X, pos.Y) * actualTileSize + vectorCenter;
		if (pos.Z == 0 && pos.X == 0 && pos.Y == 0)
			leftUpCorner = worldPosition - new Vector2(actualTileSize / 2, actualTileSize / 2);
		sprite.Position = worldPosition;
		sprite.ZIndex = pos.Z;
		sprite.Scale = new Vector2(gridScale, gridScale);
		if (pos.Z != 0)
			sprite.Scale /= svgScale;
		if (Position.pieces.ContainsKey(gridPosition))
		{
			string textureName = Position.pieces[gridPosition].ToString();
			if (pos.Z == 0)
				textureName = "tile";
			else if (Promotion.PromotionOptionsPositions.Contains(gridPosition))
				return;
			sprite.Texture = LoadGraphics.textureDict[textureName];
		}
	}
}
