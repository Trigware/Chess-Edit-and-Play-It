using Godot;
using System;

public partial class Notation
{
	private const string fileLetters = "abcdefgh", rankNumbers = "87654321";
	public static Vector2I ToLocation(string position, out bool invalid)
	{
		invalid = position.Length != 2;
		if (invalid)
			return new();
		char file = position[0], rank = position[1];
		invalid = !fileLetters.Contains(file) || !rankNumbers.Contains(rank);
		if (invalid)
			return new();
		return new(fileLetters.IndexOf(file), rankNumbers.IndexOf(rank));
	}
}
