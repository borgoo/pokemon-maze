using Pokémon.Maze.Core.RL;
using Pokémon.Maze.Play.interfaces;
using Action = Pokémon.Maze.Core.RL.Action;

namespace Pokémon.Maze.Play.models;

internal class AgentPawn : IPawn
{
    private readonly QTable _qTable;
    private readonly int _matrixRows;
    private readonly int _matrixColumns;


    private AgentPawn(QTable qTable, (int rows, int columns) matrixSize)
    {
        _qTable = qTable;
        _matrixRows = matrixSize.rows;
        _matrixColumns = matrixSize.columns;
    }
    public static AgentPawn Create(QTable qTable, (int rows, int columns) matrixSize) => new(qTable, matrixSize); 


    //  exploitation only
    public Action ChooseAction(IReadOnlySet<Action> availableActions, QStatus s)
    {
        int sIndex = s.ToIndex(_matrixRows, _matrixColumns);
        return availableActions.MaxBy(a => _qTable[sIndex, (int)a]);
    }
}
