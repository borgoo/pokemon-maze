using Pokémon.Maze.Core.Enums;
using Pokémon.Maze.Core.Exceptions;
using Pokémon.Maze.Core.models;
using System.Threading.Channels;

namespace Pokémon.Maze.Core;

public static class BFS
{
    private const int MAX_LADDERS = 2;
    private const char _downDirection = 'v';
    private const char _leftDirection = '<';
    private const char _rightDirection = '>';
    private const char _upDirection = '^';

    private static readonly Dictionary<char, (short X, short Y)> _directions = new()
    {
        { _upDirection,    (-1,  0) },
        { _rightDirection, ( 0,  1) },
        { _downDirection,  ( 1,  0) },
        { _leftDirection,  ( 0, -1) }
    };

    private static readonly Dictionary<char, char> _revertDirections = new()
    {
        { _upDirection,    _downDirection  },
        { _rightDirection, _leftDirection  },
        { _downDirection,  _upDirection    },
        { _leftDirection,  _rightDirection }
    };

    private record Step(ushort X, ushort Y, char Direction, Step? Prev);

    private static (ushort X, ushort Y, char Direction)[] GetHistory(Step? lastStep)
    {
        List<(ushort X, ushort Y, char Direction)> history = [];

        if (lastStep is null) return [];

        do
        {
            history.Add((lastStep.X, lastStep.Y, lastStep.Direction));
            lastStep = lastStep.Prev;
        } while (lastStep is not null);

        history.Reverse();
        return [.. history];
    }


    private static (List<Snapshot> Snapshots, Step FinalStep) SolveCore(ushort[,] matrix)
    {
        // get maze inital data
        int n = matrix.GetLength(0);
        int m = matrix.GetLength(1);
        (ushort X, ushort Y)? entrance = null;
        (ushort X, ushort Y)? exit = null;
        (ushort X, ushort Y)[] ladders = new (ushort X, ushort Y)[MAX_LADDERS];
        int ladderIndex = 0;

        for (ushort i = 0; i < n; i++)
        {
            for (ushort j = 0; j < m; j++)
            {
                if (matrix[i, j] == (ushort)ObjectEnum.Entrance) entrance = (i, j);
                else if (matrix[i, j] == (ushort)ObjectEnum.Exit) exit = (i, j);
                else if (i == 0 || j == 0 || i == n - 1 || j == m - 1)
                {
                    if (matrix[i, j] != (ushort)ObjectEnum.Wall)
                        throw new ArgumentException("Maze must be closed by walls.");
                }
                else if (matrix[i, j] == (ushort)ObjectEnum.Ladder)
                {
                    if (ladderIndex >= MAX_LADDERS)
                        throw new ArgumentException($"Too many ladders in the maze (max {MAX_LADDERS}).");
                    ladders[ladderIndex++] = (i, j);
                }
            }
        }

        if (entrance is null) throw new ArgumentException("Entrance not present in the maze.");
        if (exit is null) throw new ArgumentException("Exit not present in the maze.");
        if (ladderIndex != 0 && ladderIndex != MAX_LADDERS)
            throw new ArgumentException($"Too few ladders in the maze (expected 0 or {MAX_LADDERS}).");

        // setup bfs
        Step currStep = new(entrance.Value.X, entrance.Value.Y, '^', null);
        HashSet<(ushort X, ushort Y)> seen = [(currStep.X, currStep.Y)];
        HashSet<(ushort X, ushort Y, char Direction)> iceSlipsAlreadySeen = [];
        Queue<Step> nodes = [];
        nodes.Enqueue(currStep);

        // snapshots for visualization
        List<Snapshot> snapshots = [];


        // bfs loop
        while (nodes.Count > 0)
        {
            int levelSize = nodes.Count;

            // Frontier: nodes explored in this level
            HashSet<(ushort X, ushort Y)> frontier = [.. nodes
                .Take(levelSize)
                .Select(s => (s.X, s.Y))];

            // Snapshot: nodes seen so far
            snapshots.Add(new Snapshot(
                [.. seen],
                frontier,
                FinalPath: null
            ));

            for (int i = 0; i < levelSize; i++)
            {
                currStep = nodes.Dequeue();

                foreach (char direction in _directions.Keys)
                {
                    (ushort X, ushort Y) neighbor = (
                        (ushort)(currStep.X + _directions[direction].X),
                        (ushort)(currStep.Y + _directions[direction].Y)
                    );

                    if (neighbor.X < 0 || neighbor.X >= n || neighbor.Y < 0 || neighbor.Y >= m) continue;

                    // jump down
                    if (matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.JumpDown)
                    {
                        if (direction != _downDirection) continue;
                        neighbor = (
                            (ushort)(neighbor.X + _directions[direction].X),
                            (ushort)(neighbor.Y + _directions[direction].Y)
                        );
                    }

                    // jump left
                    if (matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.JumpLeft)
                    {
                        if (direction != _leftDirection) continue;
                        neighbor = (
                            (ushort)(neighbor.X + _directions[direction].X),
                            (ushort)(neighbor.Y + _directions[direction].Y)
                        );
                    }

                    if (matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.Wall) continue;

                    // ice zone
                    if (matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.Ice)
                    {
                        (ushort X, ushort Y, char Direction) currIcePoint = (neighbor.X, neighbor.Y, direction);
                        if (iceSlipsAlreadySeen.Contains(currIcePoint)) continue;
                        iceSlipsAlreadySeen.Add(currIcePoint);

                        Step iceStep = currStep;
                        (ushort X, ushort Y, char Direction) lastIcePoint = currIcePoint;

                        while (matrix[currIcePoint.X, currIcePoint.Y] == (ushort)ObjectEnum.Ice)
                        {
                            iceStep = new(currIcePoint.X, currIcePoint.Y, direction, iceStep);
                            lastIcePoint = currIcePoint;
                            currIcePoint = (
                                (ushort)(currIcePoint.X + _directions[direction].X),
                                (ushort)(currIcePoint.Y + _directions[direction].Y),
                                direction
                            );
                        }

                        iceSlipsAlreadySeen.Add((lastIcePoint.X, lastIcePoint.Y, _revertDirections[direction]));

                        bool landedOnAWalkableCell =
                            matrix[currIcePoint.X, currIcePoint.Y] == (ushort)ObjectEnum.Empty ||
                            matrix[currIcePoint.X, currIcePoint.Y] == (ushort)ObjectEnum.Exit ||
                            matrix[currIcePoint.X, currIcePoint.Y] == (ushort)ObjectEnum.Entrance;

                        if (landedOnAWalkableCell) iceStep = new(currIcePoint.X, currIcePoint.Y, direction, iceStep);

                        if (seen.Contains((iceStep.X, iceStep.Y))) continue;

                        if (matrix[currIcePoint.X, currIcePoint.Y] == (ushort)ObjectEnum.Exit)
                        {
                            // Final snapshot with path
                            var finalPath = GetHistory(iceStep);
                            snapshots.Add(new Snapshot(
                                new HashSet<(ushort X, ushort Y)>(seen),
                                [],
                                finalPath
                            ));
                            return (snapshots, iceStep);
                        }

                        seen.Add((iceStep.X, iceStep.Y));
                        nodes.Enqueue(iceStep);
                        continue;

                    } // end of ice zone

                    if (seen.Contains(neighbor)) continue;

                    Step newStep = new(neighbor.X, neighbor.Y, direction, currStep);

                    // exit found
                    if (matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.Exit)
                    {
                        var finalPath = GetHistory(newStep);
                        snapshots.Add(new Snapshot(
                            [.. seen],
                            [],
                            finalPath // populate final path
                        ));
                        return (snapshots, newStep);
                    }

                    // teleportation ladder
                    if (matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.Ladder)
                    {
                        (ushort X, ushort Y) to = ladders[0] == (neighbor.X, neighbor.Y) ? ladders[1] : ladders[0];
                        newStep = new(to.X, to.Y, direction, newStep);
                        seen.Add((to.X, to.Y));
                    }

                    seen.Add(neighbor);
                    nodes.Enqueue(newStep);
                }
            }
        }

        throw new UnsolvableMazeException("Exit not found in the maze.");
    }

    /// <summary>
    /// Simply return the resulting path steps.
    /// </summary>
    public static (ushort X, ushort Y, char Direction)[] Solve(ushort[,] matrix)
        => GetHistory(SolveCore(matrix).FinalStep);

    /// <summary>
    /// Simply return the resulting path steps.
    /// </summary>
    public static (ushort X, ushort Y, char Direction)[] Solve(string mazeMatrixFilePath)
        => GetHistory(SolveCore(MazeLoader.GetFromFile(mazeMatrixFilePath)).FinalStep);

    /// <summary>
    /// Populate (and close in the end) the channel with snapshots of the maze being solved.
    /// Only the last snapshot contains the final (solution) path.
    /// </summary>
    public static async Task SolveWithSnapshotsAsync(ushort[,] matrix, ChannelWriter<Snapshot> writer)
    {
        try
        {
            var (snapshots, _) = SolveCore(matrix);
            foreach (var snapshot in snapshots)
                await writer.WriteAsync(snapshot);

            writer.Complete();
        }
        catch (Exception ex)
        {
            writer.Complete(ex);
            throw;
        }
    }
}