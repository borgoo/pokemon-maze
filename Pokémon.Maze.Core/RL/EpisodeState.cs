namespace Pokémon.Maze.Core.RL;

public sealed class EpisodeState((int X, int Y) currentPosition, IReadOnlyDictionary<ItemType, (int X, int Y)> itemsPositions)
{
    private readonly IReadOnlyDictionary<ItemType, (int X, int Y)> _itemsPositions = itemsPositions;
    public int CurrentStep { get; set; } = 0;
    public (int X, int Y) CurrentPosition { get; set; } = currentPosition;
    public HashSet<(int X, int Y)> AvailableItems { get; set; } = [.. itemsPositions.Values];

    public QStatus ToQStatus() => new(
        CurrentPosition.X,
        CurrentPosition.Y,
        !AvailableItems.Contains(_itemsPositions[ItemType.TM24]),
        !AvailableItems.Contains(_itemsPositions[ItemType.Masterball]),
        !AvailableItems.Contains(_itemsPositions[ItemType.Pearl])
    );
}