using Pokémon.Maze.Core.Enums;
using Pokémon.Maze.Core.Exceptions;

namespace Pokémon.Maze.Core;

public static class MazeMatrix
{

    private const char _downDirection = 'v';
    private const char _leftDirection = '<';
    private const char _rightDirection = '>';
    private const char _upDirection = '^';

    private static readonly Dictionary<char, (short X, short Y)> _directions = new() {
        {_upDirection, (-1, 0)},
        {_rightDirection, (0,1)},
        {_downDirection, (1,0)},
        {_leftDirection, (0,-1)}
    };

    private static readonly Dictionary<char, char> _revertDirections = new() {
        {_upDirection, _downDirection},
        {_rightDirection, _leftDirection},
        {_downDirection, _upDirection},
        {_leftDirection, _rightDirection}
    };

    public record Step(short X, short Y, char Direction, Step? Prev);

    private static (short X, short Y, char Direction)[] GetHistory(Step? lastStep) {

        List<(short X, short Y, char Direction)> history = [];

        if (lastStep is null) return [];

        do { 
            history.Add((lastStep.X, lastStep.Y, lastStep.Direction));
            lastStep = lastStep.Prev;
        }while(lastStep is not null);

        history.Reverse();
        return [.. history];


    }

    public static (short X, short Y, char Direction)[] Solve(short[,] matrix)
    {
        // get maze initial data
        int n = matrix.GetLength(0);
        int m = matrix.GetLength(1);
        (short X, short Y)? entrance = null;
        (short X, short Y)? exit = null;

        for (short i = 0; i < n; i++)
            for (short j = 0; j < m; j++) 
                if(matrix[i, j] == (ushort)ObjectEnum.Entrance)
                    entrance = (i, j);
                else if(matrix[i, j] == (ushort)ObjectEnum.Exit)
                    exit = (i, j);
                else if(i == 0 || j == 0 || i == n - 1 || j == m - 1)
                    if(matrix[i, j] != (ushort)ObjectEnum.Wall) throw new ArgumentException("Maze must be closed by walls.");
            
        
                
        if(entrance is null) throw new ArgumentException("Entrance not present in the maze.");
        if(exit is null) throw new ArgumentException("Exit not present in the maze.");

        // setup BFS
        Step currStep = new(entrance.Value.X, entrance.Value.Y, '^', null);
        HashSet<(short X, short Y)> seen = [(currStep.X, currStep.Y)];
        HashSet<(short X, short Y, char Direction)> iceSlipsAlreadySeen = [];
        Queue<Step> nodes = [];
        nodes.Enqueue(currStep);

        while (nodes.Count > 0) {

            int num = nodes.Count;
            for(int i = 0; i < num; i++) {

                currStep = nodes.Dequeue();
                foreach(char direction in _directions.Keys) {

                    (short X, short Y) neighbor = (
                        (short)(currStep.X + _directions[direction].X),
                        (short)(currStep.Y + _directions[direction].Y)
                    );

                    if(neighbor.X < 0 || neighbor.X >= n || neighbor.Y < 0 || neighbor.Y >= m) continue; // out of bounds

                    // jumps
                    if(matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.JumpDown) {

                        if(direction != _downDirection) continue; // can be approached only from above

                        // jump down
                        neighbor = (
                            (short)(neighbor.X + _directions[direction].X),
                            (short)(neighbor.Y + _directions[direction].Y)
                        );
                    }
                    if(matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.JumpLeft) {

                        if(direction != _leftDirection) continue; // can be approached only from the right

                        // jump left
                        neighbor = (
                            (short)(neighbor.X + _directions[direction].X),
                            (short)(neighbor.Y + _directions[direction].Y)
                        );
                    }

                    if(matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.Wall) continue; // wall
                    if(matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.Ladder) continue; // ladder

                    if(matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.Ice) // is ice
                    {
                        (short X, short Y, char Direction) currIcePoint = (neighbor.X, neighbor.Y, direction);
                        if(iceSlipsAlreadySeen.Contains( currIcePoint )) continue; // already slipped on this way
                        iceSlipsAlreadySeen.Add( currIcePoint ); // register from -> to as seen

                        Step iceStep = currStep;
                        (short X, short Y, char Direction) lastIcePoint = currIcePoint;

                        while(matrix[currIcePoint.X, currIcePoint.Y] == (ushort)ObjectEnum.Ice) {

                            iceStep = new(currIcePoint.X, currIcePoint.Y, direction, iceStep);
                            lastIcePoint = currIcePoint;

                            currIcePoint = (
                                (short)(currIcePoint.X + _directions[direction].X),
                                (short)(currIcePoint.Y + _directions[direction].Y),
                                direction
                            );

                        }

                        // register to <- from also
                        iceSlipsAlreadySeen.Add((lastIcePoint.X, lastIcePoint.Y, _revertDirections[direction]));

                        bool landedOnAWalkableCell = 
                            matrix[currIcePoint.X, currIcePoint.Y] == (ushort)ObjectEnum.Empty ||
                            matrix[currIcePoint.X, currIcePoint.Y] == (ushort)ObjectEnum.Exit ||
                            matrix[currIcePoint.X, currIcePoint.Y] == (ushort)ObjectEnum.Entrance;

                        if(landedOnAWalkableCell) iceStep = new(currIcePoint.X, currIcePoint.Y, direction, iceStep);

                        if( seen.Contains((iceStep.X, iceStep.Y)) ) continue;

                        if(matrix[currIcePoint.X, currIcePoint.Y] == (ushort)ObjectEnum.Exit) return GetHistory(iceStep); // landed on exit
                        
                        seen.Add((iceStep.X, iceStep.Y));
                        nodes.Enqueue(iceStep);

                        continue;
                    }


                    if(seen.Contains(neighbor)) continue; // already visited

                    Step newStep = new(neighbor.X, neighbor.Y, direction, currStep);
                    if (matrix[neighbor.X, neighbor.Y] == (ushort)ObjectEnum.Exit) return GetHistory(newStep); // exit

                    seen.Add(neighbor);
                    nodes.Enqueue(newStep);
                 

                }              
            }

        }
        
        throw new UnsolvableMazeException("Exit not found in the maze.");


        
    }

}

