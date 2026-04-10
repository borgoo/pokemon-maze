using Pokémon.Maze.Core.RL;
using Pokémon.Maze.TrainingField.interfaces;
using Action = Pokémon.Maze.Core.RL.Action;

namespace Pokémon.Maze.TrainingField.models;

internal sealed class TrainingPawn(
    float alpha,
    float gamma,
    (int rows, int columns) matrixSize

) : ITrainingPawn
{
    // learning assets
    private readonly float _alpha = alpha;
    private readonly float _gamma = gamma;
    private readonly int _matrixRows = matrixSize.rows;
    private readonly int _matrixColumns = matrixSize.columns;


    private static readonly Random _random = new();
    private readonly QTable _qTable = new( 
                                            QStatus.GetStatusCardinality(matrixSize.rows, matrixSize.columns), 
                                            Enum.GetValues<Action>().Length
                                        );


    // epsilon-greedy policy
    public Action ChooseAction(IReadOnlySet<Action> availableActions, QStatus s, float epsilon)
    {
        // explore
        if(_random.NextDouble() < epsilon) return availableActions.ElementAt(_random.Next(availableActions.Count));
       
       // exploit
       int sIndex = s.ToIndex(_matrixRows, _matrixColumns);
       return availableActions.MaxBy(a => _qTable[sIndex, (int)a]);
    }

    public void LearnFromTerminalState(QStatus s, Action a, float r)
    {
        int sIndex = s.ToIndex(_matrixRows, _matrixColumns);
        int aIndex = (int)a;

        // Q(s, a) = Q(s, a) + α * (r - Q(s, a))
        _qTable[sIndex, aIndex] += _alpha * (r - _qTable[sIndex, aIndex]);
    }

    public void LearnAndBootstrap(QStatus s, Action a, float r, QStatus nextS, HashSet<Action> nextStepValidActions)
    {
        int sIndex = s.ToIndex(_matrixRows, _matrixColumns);
        int aIndex = (int)a;
        int nextSIndex = nextS.ToIndex(_matrixRows, _matrixColumns);
        Action maxQValueAction = nextStepValidActions.MaxBy(a => _qTable[nextSIndex, (int)a]);
        float maxQValue = _qTable[nextSIndex, (int)maxQValueAction];
        
        // Q(s, a) = Q(s, a) + α * (r + γ * max(Q(s', a')) - Q(s, a))
        _qTable[sIndex, aIndex] = _qTable[sIndex, aIndex] + _alpha * (r + _gamma * maxQValue - _qTable[sIndex, aIndex]);
    }

    public void SaveQTable(BinaryWriter writer) => _qTable.Save(writer);
}