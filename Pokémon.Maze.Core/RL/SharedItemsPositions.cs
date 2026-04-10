
namespace Pokémon.Maze.Core.RL;

public static class SharedItemsPositions
{
    // top to bottom positions
    public static readonly IReadOnlyDictionary<ItemType, (int X, int Y)> Get = new Dictionary<ItemType, (int X, int Y)> {
        {ItemType.TM24, (7, 31)},
        {ItemType.Pearl, (9, 33)},
        {ItemType.Masterball, (23, 32)},
    };
}
