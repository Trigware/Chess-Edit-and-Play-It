using Godot;
using System;
using System.Collections.Generic;

public partial class Tags
{
	public static List<Vector2I> tagPositions = new() { new(4, 0), new(4, 7) };
	public static List<HashSet<Tag>> activeTags = new() { new() { Tag.Royal, Tag.Castler }, new() { Tag.Royal, Tag.Castler } };
	public static List<(Vector2I location, Sprite2D sprite, int offset)> spriteTags = new();
	public static List<Vector2I> CastlingRights = new();
	public enum Tag
	{
		Royal,
		Castler,
		Castlee
	}
	public static void Add(Vector2I location, Tag tag)
	{
		int tagIndex = tagPositions.IndexOf(location);
		if (tagIndex == -1)
		{
			tagPositions.Add(location);
			activeTags.Add(new() {tag});
		}
		else
			activeTags[tagIndex].Add(tag);
	}
	public static void ModifyTags(Vector2I start, Vector2I end, char handledPiece, bool updateCastlingRightsHash)
	{
		bool updatedCastlingRights = false;
		if (tagPositions.Contains(end))
		{
			int endIndex = tagPositions.IndexOf(end);
			HashSet<Tag> tagsAtPosition = activeTags[endIndex];
			if (tagsAtPosition.Contains(Tag.Castlee) || tagsAtPosition.Contains(Tag.Castler))
				updatedCastlingRights = true;
			tagPositions.RemoveAt(endIndex);
			activeTags.RemoveAt(endIndex);
			CastlerAction(end, out _, out _);
		}
		if (tagPositions.Contains(start))
		{
			if (CastlerAction(start, out int index, out bool entireTagDeleted))
				updatedCastlingRights = true;
			if (!entireTagDeleted)
				tagPositions[index] = end;
			if (CastleeMoved(end))
				updatedCastlingRights = true;
		}
		if (updateCastlingRightsHash && updatedCastlingRights)
			GetCastlingRightsHash();
	}
	private static bool TagDeletion(Vector2I location, Tag tag)
	{
		int index = tagPositions.IndexOf(location);
		activeTags[index].Remove(tag);
		if (activeTags[index].Count == 0)
		{
			tagPositions.RemoveAt(index);
			activeTags.RemoveAt(index);
			return true;
		}
		return false;
	}
	private static bool CastlerAction(Vector2I location, out int index, out bool entireTagDeleted)
	{
		entireTagDeleted = false;
		index = tagPositions.IndexOf(location);
		if (index == -1 || !activeTags[index].Contains(Tag.Castler))
			return false;
		char colorCastler = LegalMoves.GetPieceColor(location);
		List<Vector2I> deletedTags = new();
		for (int i = 0; i < activeTags.Count; i++)
		{
			if (!Position.pieces.ContainsKey(tagPositions[i]))
				continue;
			HashSet<Tag> tags = activeTags[i];
			char colorCastlee = LegalMoves.GetPieceColor(tagPositions[i]);
			if (colorCastlee == colorCastler && tags.Contains(Tag.Castlee))
				deletedTags.Add(tagPositions[i]);
		}
		if (deletedTags.Count == 0)
			return false;
		foreach (Vector2I deletedLoc in deletedTags)
			TagDeletion(deletedLoc, Tag.Castlee);
		entireTagDeleted = TagDeletion(location, Tag.Castler);
		return true;
	}
	private static bool CastleeMoved(Vector2I location)
	{
		int index = tagPositions.IndexOf(location);
		if (index > -1 && activeTags[index].Contains(Tag.Castlee))
		{
			TagDeletion(location, Tag.Castlee);
			return true;
		}
		return false;
	}
	public static void GetRoyalsPerColor()
	{
		Position.RoyalPiecesColor = new();
		for (int i = 0; i < activeTags.Count; i++)
		{
			if (activeTags[i].Contains(Tag.Royal))
			{
				char pieceColor = LegalMoves.GetPieceColor(tagPositions[i]);
				if (pieceColor == '\0')
					continue;
				Position.RoyalPiecesColor.Add(tagPositions[i], pieceColor);
				Position.RoyalsPerColor[pieceColor]++;
			}
		}
	}
	public static void GetCastlingRightsHash()
	{
		CastlingRights = new();
		for (int i = 0; i < activeTags.Count; i++)
		{
			if (activeTags[i].Contains(Tag.Castlee))
				CastlingRights.Add(tagPositions[i]);
		}
		CastlingRights.Sort((a, b) => a.Y == b.Y ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
		Zobrist.GetCastlingHash();
	}
	public static void ModifyRoyalPieceList(Vector2I start, Vector2I end)
	{
		if (PieceMoves.IsRoyal(start))
			MoveDeleteRoyal(start, end, false);
		if (PieceMoves.IsRoyal(end))
			MoveDeleteRoyal(end, end, true);
	}
	private static void MoveDeleteRoyal(Vector2I start, Vector2I end, bool delete)
	{
		char royalColor = Position.RoyalPiecesColor[start];
		Position.RoyalPiecesColor.Remove(start);
		if (!delete)
			Position.RoyalPiecesColor.Add(end, royalColor);
		else
			Position.RoyalsPerColor[royalColor]--;
	}
}
