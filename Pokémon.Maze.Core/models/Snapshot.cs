namespace Pokémon.Maze.Core.models;

/// <summary>
/// Represents a snapshot of the BFS state at a given level.
/// </summary>
/// <param name="Visited">All cells visited so far.</param>
/// <param name="Frontier">Cells in the current BFS frontier (being explored this level).</param>
/// <param name="FinalPath">Non-null only on the last snapshot, when the exit is found.</param>
public record Snapshot(
    HashSet<(ushort X, ushort Y)> Visited,
    HashSet<(ushort X, ushort Y)> Frontier,
    (ushort X, ushort Y, char Direction)[]? FinalPath
);