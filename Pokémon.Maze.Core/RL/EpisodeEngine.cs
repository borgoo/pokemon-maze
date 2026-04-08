using System.Collections.ObjectModel;
using Pokémon.Maze.Core.Enums;

namespace Pokémon.Maze.Core.RL;

internal sealed class EpisodeEngine
{
    public EpisodeEngine(ushort[,] matrix)
    {
        _matrix = matrix;
        _matrixRows = matrix.GetLength(0);
        _matrixColumns = matrix.GetLength(1);
        _laddersPositions = GetLaddersPositions(matrix, _matrixRows, _matrixColumns);
        _movements =new(new Dictionary<Action, (int Dx, int Dy)>
        {
            { Action.MoveUp, (-1, 0) },
            { Action.MoveDown, (1, 0) },
            { Action.MoveLeft, (0, -1) },
            { Action.MoveRight, (0, 1) }
            // pickup is treated artificially
        });
    }

    private readonly ushort[,] _matrix;
    private readonly int _matrixRows;
    private readonly int _matrixColumns;
    private readonly (int X, int Y)[] _laddersPositions;
    private readonly ReadOnlyDictionary<Action, (int Dx, int Dy)> _movements;


    private static (int X, int Y)[] GetLaddersPositions(ushort[,] matrix, int matrixRows, int matrixColumns)
    {
        (int X, int Y)? first = null;
        (int X, int Y)? second = null;

        for(int i = 0; i < matrixRows; i++)
        {
            for(int j = 0; j < matrixColumns; j++)
            {
                if(matrix[i, j] == (ushort)ObjectEnum.Ladder)
                {
                    if(first is null) first = (i, j);
                    else if(second is null) second = (i, j);
                    else throw new ArgumentException("Only two or none ladders are allowed in the maze.");
                }
            }
        }

        if(first is null && second is null) return [];
        if(first is null || second is null) throw new ArgumentException("Only two or none ladders are allowed in the maze.");

        return [first.Value, second.Value];
    }




    // PICKUP IS CONSIDERED AS A MOVEMENT IN THAT TILE
    public HashSet<Action> GetAvailableActions(EpisodeState episodeState)
    {
        (int X, int Y) = episodeState.CurrentPosition;
        HashSet<Action> availableActions = [];

        // check left
        // to be valid Action.MoveLeft must be: not < 0; the _matrix[X, Y-1] must not be a wall or entrance or a JumpDown;
        if(Y > 0 && _matrix[X, Y-1] != (ushort)ObjectEnum.Wall && _matrix[X, Y-1] != (ushort)ObjectEnum.Entrance && _matrix[X, Y-1] != (ushort)ObjectEnum.JumpDown )
            availableActions.Add(Action.MoveLeft);
        
        // check right
        if(Y < _matrixColumns - 1 && _matrix[X, Y+1] != (ushort)ObjectEnum.Wall && _matrix[X, Y+1] != (ushort)ObjectEnum.Entrance && _matrix[X, Y+1] != (ushort)ObjectEnum.JumpDown && _matrix[X, Y+1] != (ushort)ObjectEnum.JumpLeft)
            availableActions.Add(Action.MoveRight);
        
        // check up
        if(X > 0 && _matrix[X-1, Y] != (ushort)ObjectEnum.Wall && _matrix[X-1, Y] != (ushort)ObjectEnum.Entrance && _matrix[X-1, Y] != (ushort)ObjectEnum.JumpDown && _matrix[X-1, Y] != (ushort)ObjectEnum.JumpLeft)
            availableActions.Add(Action.MoveUp);
        
        // check down
        if(X < _matrixRows - 1 && _matrix[X+1, Y] != (ushort)ObjectEnum.Wall && _matrix[X+1, Y] != (ushort)ObjectEnum.Entrance && _matrix[X+1, Y] != (ushort)ObjectEnum.JumpDown && _matrix[X+1, Y] != (ushort)ObjectEnum.JumpLeft)
            availableActions.Add(Action.MoveDown);

        return availableActions;        
        
    }

    /// <summary>
    /// GetAvailableActions already filters all the invalid actions.
    /// PICK UP will NEVER be passed as action; compute it considering the movement on the item tile
    /// </summary>
    /// <param name="action"></param>
    /// <param name="episodeState"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Move(Action action, EpisodeState episodeState)
    {
        (int X, int Y) neighbor = (episodeState.CurrentPosition.X + _movements[action].Dx, episodeState.CurrentPosition.Y + _movements[action].Dy);
        ObjectEnum tile = (ObjectEnum)_matrix[neighbor.X, neighbor.Y];
        
        episodeState.CurrentPosition = neighbor; // if pickup stand still
        episodeState.CurrentStep++; // moving cause at least + 1 step
        

        switch(tile) {

            case ObjectEnum.Exit:
            case ObjectEnum.Empty:
                return;

            case ObjectEnum.JumpDown: // jump: distance is doubled
            case ObjectEnum.JumpLeft:
                episodeState.CurrentPosition = (episodeState.CurrentPosition.X + _movements[action].Dx, episodeState.CurrentPosition.Y + _movements[action].Dy);
                return;

            case ObjectEnum.Ladder:
                // teleport it to the other side
                episodeState.CurrentPosition = episodeState.CurrentPosition == _laddersPositions[0] ? _laddersPositions[1] : _laddersPositions[0];
                return;

            case ObjectEnum.Item:

                if(episodeState.AvailableItems.Remove(neighbor)) { // if present that block the movement and pick it up
                    episodeState.CurrentStep--;
                    episodeState.CurrentPosition = (episodeState.CurrentPosition.X - _movements[action].Dx, episodeState.CurrentPosition.Y - _movements[action].Dy);
                    return;
                }

                // else simply move there (handled by current position above)
                return;
            
            case ObjectEnum.Ice:
                SlideOnIce(action, episodeState);
                return;

            default:
                throw new NotImplementedException($"Tile {tile} not implemented");
        }

    }

    private bool TheNextTileIsIce(Action action, EpisodeState episodeState)
    {
        (int X, int Y) = (episodeState.CurrentPosition.X + _movements[action].Dx, episodeState.CurrentPosition.Y + _movements[action].Dy);
        return (ObjectEnum)_matrix[X, Y] == ObjectEnum.Ice;
    }

    private void SlideOnIce(Action action, EpisodeState episodeState)
    {
        
        while( TheNextTileIsIce(action, episodeState) ) {
            episodeState.CurrentPosition = (episodeState.CurrentPosition.X + _movements[action].Dx, episodeState.CurrentPosition.Y + _movements[action].Dy);
            episodeState.CurrentStep++;
        }

        (int X, int Y) finalPosition = (episodeState.CurrentPosition.X + _movements[action].Dx, episodeState.CurrentPosition.Y + _movements[action].Dy);
        ObjectEnum finalTile = (ObjectEnum)_matrix[finalPosition.X, finalPosition.Y];
        episodeState.CurrentStep++; // at least one more step



        switch(finalTile) {

            case ObjectEnum.Wall:
                episodeState.CurrentStep--; // revert previous +1
                return;
            case ObjectEnum.Ladder:
                episodeState.CurrentPosition = finalPosition == _laddersPositions[0] ? _laddersPositions[1] : _laddersPositions[0];
                return;
            case ObjectEnum.Item:

                // if still there than block the movement
                if(episodeState.AvailableItems.Contains(finalPosition)) { 
                    episodeState.CurrentStep--; 
                    return;
                }
                
                // else walk there
                episodeState.CurrentPosition = finalPosition;
                return;
            
            case ObjectEnum.JumpDown:

                if(action != Action.MoveDown)  { // if not right direction than block the movement
                    episodeState.CurrentStep--; 
                    return;
                }

                // else jump
                episodeState.CurrentPosition = (finalPosition.X + _movements[action].Dx, finalPosition.Y + _movements[action].Dy);
                episodeState.CurrentStep++;
                return;
           
            case ObjectEnum.JumpLeft:

                if(action != Action.MoveLeft)  { // if not right direction than block the movement
                    episodeState.CurrentStep--; 
                    return;
                }

                // else jump
                episodeState.CurrentPosition = (finalPosition.X + _movements[action].Dx, finalPosition.Y + _movements[action].Dy);
                episodeState.CurrentStep++;
                return;

            case ObjectEnum.Empty:
            case ObjectEnum.Exit:
            case ObjectEnum.Entrance:
                episodeState.CurrentPosition = finalPosition;
                return;

            default:
                throw new NotImplementedException($"Tile {finalTile} not implemented (after ice).");
        }        
        
    }

}
