namespace Pokémon.Maze.Core.RL;

internal sealed class EpisodeState((int X, int Y) currentPosition, HashSet<(int X, int Y)> availableItems)
{
    public int CurrentStep { get; set; } = 0;
    public (int X, int Y) CurrentPosition { get; set; } = currentPosition;
    public HashSet<(int X, int Y)> AvailableItems { get; set; } = availableItems;
}