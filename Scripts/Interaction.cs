using Godot;
using System;

public partial class Interaction : Chessboard
{
	public static Vector3I? selectedTile = null;
	private bool leftMouseButtonPressed = false, leftMouseOld = false;
	public static int movesUndoInASession = 0;
	public override void _Process(double delta)
	{
		if ((Input.IsKeyPressed(Key.Z) && !Promotion.MoveHistoryDisable) || History.undoPending)
            History.Undo();
        else
			movesUndoInASession = 0;
		bool leftActuallyPressed = Input.IsMouseButtonPressed(MouseButton.Left);
		leftMouseButtonPressed = leftActuallyPressed && !leftMouseOld;
		if ((leftMouseButtonPressed || Input.IsKeyPressed(Key.Escape)) && Position.GameEndState == Position.EndState.Ongoing)
			Select();
		leftMouseOld = leftActuallyPressed;
	}
	private Vector2I GetPositionOnBoard()
	{
		if (leftUpCorner == null)
		{
			GD.PrintErr("The chessboard is not loaded!");
			return new();
		}
		Vector2 leftUpNotNull = (Vector2)leftUpCorner;
		Vector2 mousePosition = GetViewport().GetMousePosition();
		return (Vector2I)((mousePosition - leftUpNotNull) / actualTileSize).Floor();
	}
	private void Select()
	{
		Vector2I flatMousePosition = GetPositionOnBoard();
		if (leftMouseButtonPressed && Promotion.PromotionOptionsPositions.Contains(flatMousePosition))
		{
			Promotion.Promote(flatMousePosition);
			return;
		}
		if (Position.colorToMove == '\0')
			return;
		Vector3I mousePositionBoard = new(flatMousePosition.X, flatMousePosition.Y, 0), mousePositionPieces = new(flatMousePosition.X, flatMousePosition.Y, 1);
		bool canSwitchSelectedTile = PieceMoves.GetPieceColor(flatMousePosition) == Position.colorToMove;
		if ((Input.IsKeyPressed(Key.Escape) && selectedTile != null) || (leftMouseButtonPressed && selectedTile == mousePositionBoard))
		{
			Deselect((Vector3I)selectedTile);
			PreviousMoveTiles(Colors.Enum.PreviousMove);
			return;
		}
		if (leftMouseButtonPressed)
		{
			if (canSwitchSelectedTile)
			{
				Colors.SetTileColors(flatMousePosition);
				return;
			}
			if (selectedTile == null)
				return;
			Vector3I selectedNotNull = (Vector3I)selectedTile;
			Vector2I selectedTileFlat = new(selectedNotNull.X, selectedNotNull.Y);
			int legalIndex = LegalMoves.legalMoves.IndexOf((selectedTileFlat, flatMousePosition));
			if (legalIndex > -1)
			{
                UpdatePosition.MovePiece(selectedTileFlat, flatMousePosition, legalIndex);
            }
        }
	}
	public static void Deselect(Vector2I start)
	{
		PreviousMoveTiles(Colors.Enum.Default);
		Colors.Set(GetTile(start), Colors.Enum.Default, start.X, start.Y);
		foreach ((Vector2I start, Vector2I end) startEndTiles in LegalMoves.legalMoves)
		{
			if (startEndTiles.start == start)
				Colors.Set(GetTile(startEndTiles.end), Colors.Enum.Default, startEndTiles.end.X, startEndTiles.end.Y);
		}
		Colors.ColorCheckedRoyalTiles(Colors.Enum.Check);
		selectedTile = null;
	}
	public static void PreviousMoveTiles(Colors.Enum color)
	{
		if (Position.LastMoveInfo == null)
			return;
		(Vector2I start, Vector2I end) lastMoveNotNull = Position.LastMoveInfo ?? default;
		Colors.Set(color, lastMoveNotNull.start.X, lastMoveNotNull.start.Y);
		Colors.Set(color, lastMoveNotNull.end.X, lastMoveNotNull.end.Y);
	}
	public static void Deselect(Vector3I start)
	{
		Deselect(new Vector2I(start.X, start.Y));
	}
	public static Sprite2D GetTile(Vector2I location)
	{
		return tiles[new(location.X, location.Y, 0)];
	}
}
