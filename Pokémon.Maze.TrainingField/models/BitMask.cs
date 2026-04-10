namespace Pokémon.Maze.TrainingField.models;

public sealed class BitMask(int Rows, int Cols)
{
    private readonly ulong[] _visitedCells = new ulong[(Rows * Cols + 63) / 64];
    public void Clear() => Array.Fill(_visitedCells, 0UL);
    public bool IsVisited((int X, int Y) pos)
    {
        int idx = pos.X * Cols + pos.Y;
        return (_visitedCells[idx >> 6] & (1UL << (idx & 63))) != 0;
    }

    public void MarkAsVisited((int X, int Y) pos)
    {
        int idx = pos.X * Cols + pos.Y;
        _visitedCells[idx >> 6] |= (1UL << (idx & 63));
    }
}