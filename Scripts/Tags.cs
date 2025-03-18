using Godot;
using System;
using System.Collections.Generic;

public partial class Tags : Node
{
	public static List<Vector2I> tagPositions = new() { new(0, 0), new(4, 0), new(7, 0), new(0, 7), new(4, 7), new(7, 7) };
	public static List<HashSet<Tag>> activeTags = new() { new() { Tag.Castlee }, new() { Tag.Royal, Tag.Castler }, new() { Tag.Castlee }, new() { Tag.Castlee }, new() { Tag.Royal, Tag.Castler }, new() { Tag.Castlee } };
	public static List<(Vector2I location, Sprite2D sprite, int offset)> spriteTags = new();
	public enum Tag
	{
		Royal,
		Castler,
		Castlee
	}
	public static void ModifyTags(Vector2I start, Vector2I end, char handledPiece)
	{
		if (tagPositions.Contains(end))
		{
			int endIndex = tagPositions.IndexOf(end);
			tagPositions.RemoveAt(endIndex);
			activeTags.RemoveAt(endIndex);
		}
		if (tagPositions.Contains(start))
		{
			int startIndex = tagPositions.IndexOf(start);
			HashSet<Tag> tagsAssignedToPosition = activeTags[startIndex];
			if (tagsAssignedToPosition.Contains(Tag.Castlee))
				TagDeletion(startIndex, Tag.Castlee);
			else
				tagPositions[startIndex] = end;
			if (tagsAssignedToPosition.Contains(Tag.Castler))
			{
				TagDeletion(startIndex, Tag.Castler);
				CastleeDeletion(handledPiece);
			}
		}
	}
	private static void TagDeletion(int index, Tag tag)
	{
		activeTags[index].Remove(tag);
		if (activeTags[index].Count == 0)
		{
			tagPositions.RemoveAt(index);
			activeTags.RemoveAt(index);
		}
	}
	private static void CastleeDeletion(char handledPiece)
	{
		char colorCastler = LegalMoves.GetPieceColor(handledPiece);
		for (int i = 0; i < activeTags.Count; i++)
		{
			if (!Position.pieces.ContainsKey(tagPositions[i]))
				continue;
			HashSet<Tag> tags = activeTags[i];
			char colorCastlee = LegalMoves.GetPieceColor(tagPositions[i]);
			if (colorCastlee == colorCastler && tags.Contains(Tag.Castlee))
				TagDeletion(i, Tag.Castlee);
		}
	}
}
