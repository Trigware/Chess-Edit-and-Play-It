using Godot;
using System.Collections.Generic;

public partial class History
{
    public static Stack<Move> UndoMoves = new(), RedoMoves = new();
    private static int movesUndoneDebug = 0;
    public class Move
    {
        public Vector2I Start, End;
        public Move(Vector2I start, Vector2I end)
        {
            Start = start; End = end;
        }
        public (Vector2I, Vector2I) GetTuple()
        {
            return (Start, End);
        }
        public override string ToString()
        {
            return $"Start: {Start.ToString()}, End: {End.ToString()}";
        }
    }
    public static void Play(Vector2I start, Vector2I end)
    {
        RedoMoves = new();
        UndoMoves.Push(new(start, end));
    }
    public static void Undo()
    {
        if (UndoMoves.Count == 0 || movesUndoneDebug > 0)
            return;
        movesUndoneDebug++;
        Move previousMove = UndoMoves.Pop();
        RedoMoves.Push(previousMove);
        ModifyLastMoveInfo();
        MoveReplayGetBack(previousMove);
    }
    private static void ModifyLastMoveInfo()
    {
        Interaction.PreviousMoveTiles(Colors.Enum.Default);
        if (UndoMoves.Count == 0)
        {
            Position.LastMoveInfo = null;
            return;
        }
        Position.LastMoveInfo = UndoMoves.Peek().GetTuple();
        Interaction.PreviousMoveTiles(Colors.Enum.PreviousMove);
        if (Interaction.selectedTile != null)
            Interaction.Deselect(Interaction.selectedTile ?? default);
    }
    private static void MoveReplayGetBack(Move previousMove)
    {
        Sprite2D spr = Chessboard.tiles[new(previousMove.End.X, previousMove.End.Y, 1)];
        spr.Position = Animations.CalculateTilePosition(previousMove.Start.X, previousMove.Start.Y);
        LegalMoves.ReverseColor(Position.colorToMove);
    }
}
