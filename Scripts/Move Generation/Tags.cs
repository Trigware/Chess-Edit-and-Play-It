using Godot;
using System.Collections.Generic;
using System.Drawing;

public partial class Tags
{
	public static List<Vector2I> tagPositions = new() { new(4, 0), new(4, 7) };
	public static List<HashSet<Tag>> activeTags = new() { new() { Tag.Royal, Tag.Castler }, new() { Tag.Royal, Tag.Castler } };
	public static Dictionary<Vector2I, HashSet<Tag>> lastDeletedTags;
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
			activeTags.Add(new() { tag });
		}
		else
			activeTags[tagIndex].Add(tag);
	}
	public static void ModifyTags(Vector2I start, Vector2I end, char handledPiece, bool updateCastlingRightsHash, bool castling)
	{
		bool updatedCastlingRights = false;
		if (!updateCastlingRightsHash || !castling)
			UpdateCastlingRightsForLastDeletedTags(start, start, !updateCastlingRightsHash, out _);
		if (tagPositions.Contains(end))
		{
			Vector2I usedPosition = ContainsCastlingTag(start, out _, out _) && ContainsCastlingTag(end, out _, out _) ? start : end;
			updatedCastlingRights = UpdateCastlingRightsForLastDeletedTags(usedPosition, end, true, out int endIndex);
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
			if (CastleeMoved(end, updatedCastlingRights ? null : start))
				updatedCastlingRights = true;
		}
		if (updateCastlingRightsHash && updatedCastlingRights)
			GetCastlingRightsHash();
	}
	public static bool Delete(Vector2I location, Tag tag, bool updateLastDeletedTags, Vector2I? updateLocation = null)
	{
		int index = tagPositions.IndexOf(location);
		if (index == -1)
			return false;
		activeTags[index].Remove(tag);
		if (updateLastDeletedTags)
			UpdateLastDeletedTags(updateLocation == null ? location : (updateLocation ?? default), tag);
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
			Delete(deletedLoc, Tag.Castlee, true);
		entireTagDeleted = Delete(location, Tag.Castler, true);
		return true;
	}
	private static bool CastleeMoved(Vector2I location, Vector2I? start)
	{
		int index = tagPositions.IndexOf(location);
		if (index > -1 && activeTags[index].Contains(Tag.Castlee))
		{
			Delete(location, Tag.Castlee, true, start);
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
				Vector2I tagPosition = tagPositions[i];
				char pieceColor = LegalMoves.GetPieceColor(tagPosition);
				if (pieceColor == '\0')
					continue;
				Cursor.Location.TryAdd(pieceColor, tagPosition);
				History.initialCursorLocation.TryAdd(pieceColor, tagPosition);
				Position.RoyalPiecesColor.Add(tagPosition, pieceColor);
				Position.RoyalsPerColor[pieceColor]++;
			}
		}
		foreach (char color in Position.playerColors)
		{
			if (History.initialCursorLocation.ContainsKey(color)) continue;
			Vector2I randomChosenPiece = PickRandomPieceOfColor(color);
			History.initialCursorLocation.TryAdd(color, randomChosenPiece);
			Cursor.Location.TryAdd(color, randomChosenPiece);
		}
		History.LatestReverseCursorLocation = History.initialCursorLocation[LegalMoves.ReverseColorReturn(Position.colorToMove)];
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
	private static void UpdateLastDeletedTags(Vector2I location, Tag tag)
	{
		if (lastDeletedTags.ContainsKey(location))
			lastDeletedTags[location].Add(tag);
		else
			lastDeletedTags.Add(location, new() { tag });
	}
	private static bool UpdateCastlingRightsForLastDeletedTags(Vector2I start, Vector2I end, bool updateLastDeletedTags, out int endIndex)
	{
		bool containsCastlingTag = ContainsCastlingTag(end, out endIndex, out HashSet<Tag> tagsAtPosition);
		if (!updateLastDeletedTags)
			return containsCastlingTag;
		if (containsCastlingTag)
		{
			foreach (Tag tag in tagsAtPosition)
			{
				if (tag != Tag.Castlee && tag != Tag.Castler) continue;
				UpdateLastDeletedTags(start, tag);
			}
			return true;
		}
		return false;
	}
	private static bool ContainsCastlingTag(Vector2I position, out int tagIndex, out HashSet<Tag> tagsAtPosition)
	{
		tagIndex = tagPositions.IndexOf(position);
		tagsAtPosition = new();
		if (tagIndex == -1)
			return false;
		tagsAtPosition = activeTags[tagIndex];
		return tagsAtPosition.Contains(Tag.Castlee) || tagsAtPosition.Contains(Tag.Castler);
	}
	private static Vector2I PickRandomPieceOfColor(char color)
	{
		List<Vector2I> pieceLocationsOfStartColor = new();
		foreach (KeyValuePair<Vector2I, char> piece in Position.pieces)
		{
			if (LegalMoves.GetPieceColor(piece.Value) != color) continue;
			pieceLocationsOfStartColor.Add(piece.Key);
		}
		if (pieceLocationsOfStartColor.Count == 0) return default;
		return pieceLocationsOfStartColor[new RandomNumberGenerator().RandiRange(0, pieceLocationsOfStartColor.Count - 1)];
	}
}
