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
	public static void ModifyTags(Vector2I start, Vector2I end, char handledPiece)
	{
		bool updateCastlingRights = false;
		if (tagPositions.Contains(end))
		{
			int endIndex = tagPositions.IndexOf(end);
			if (activeTags[endIndex].Contains(Tag.Castlee))
				updateCastlingRights = true;
            tagPositions.RemoveAt(endIndex);
			activeTags.RemoveAt(endIndex);
		}
		if (tagPositions.Contains(start))
		{
			if (CastleeMoved(start, out int startIndex, out HashSet<Tag> tagsAssignedToPosition))
				updateCastlingRights = true;
			else
				tagPositions[startIndex] = end;
            if (tagsAssignedToPosition.Contains(Tag.Castler))
			{
				TagDeletion(startIndex, Tag.Castler);
				CastleeDeletion(handledPiece);
                updateCastlingRights = true;
            }
        }
		if (updateCastlingRights)
			GetCastlingRights();
	}
	private static bool CastleeMoved(Vector2I start, out int startIndex, out HashSet<Tag> tagsAssignedToPosition)
	{
        startIndex = tagPositions.IndexOf(start);
        tagsAssignedToPosition = activeTags[startIndex];
        if (tagsAssignedToPosition.Contains(Tag.Castlee))
        {
            TagDeletion(startIndex, Tag.Castlee);
			return true;
        }
		return false;
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
	private static void GetCastlingRights()
	{
		CastlingRights = new();
		for (int i = 0; i < activeTags.Count; i++)
		{
			if (activeTags[i].Contains(Tag.Castlee))
                CastlingRights.Add(tagPositions[i]);
        }
		Zobrist.LastCastlingRightHash = Zobrist.GetCastlingHash();
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
