using Pokémon.Maze.Core.RL;
using Action = Pokémon.Maze.Core.RL.Action;
namespace Pokémon.Maze.TrainingField.interfaces;

/// <summary>
/// Learn from prev state, next state (if not terminal learning), reward and action taken
/// </summary>
internal interface ITrainingPawn
{
    public Action ChooseAction(IReadOnlySet<Action> availableActions, QStatus episodeState, float epsilon);
    public void LearnFromTerminalState(QStatus s, Action a, float r);
    public void LearnAndBootstrap(QStatus s, Action a, float r, QStatus nextS, HashSet<Action> nextStepValidActions);

    public string ExportQTable();
}