using Pokémon.Maze.Core.RL;
using Pokémon.Maze.Play.models;
using Action = Pokémon.Maze.Core.RL.Action;
namespace Pokémon.Maze.Play;

internal sealed class EpisodeOrchestrator(
    (int X, int Y) entrancePosition,
    (int X, int Y) exitPosition,
    ushort[,] matrix,
    IReadOnlyDictionary<ItemType, (int X, int Y)> itemsPositions
)
{
    private readonly EpisodeEngine _episodeEngine = new(matrix);
    private readonly (int X, int Y) _entrancePosition = entrancePosition;
    private readonly (int X, int Y) _exitPosition = exitPosition;
    private readonly IReadOnlyDictionary<ItemType, (int X, int Y)> _itemsPositions = itemsPositions;

    public void Run(QTable qTable)
    {
        AgentPawn agent = AgentPawn.Create(qTable, (matrix.GetLength(0), matrix.GetLength(1)));

        EpisodeState episodeState = new(_entrancePosition, _itemsPositions);
        // display snapshot

        while(true) {

            HashSet<Action> availableActions = _episodeEngine.GetAvailableActions(episodeState);
            Action a = agent.ChooseAction(availableActions, episodeState.ToQStatus());
            ApplyActionOnEpisodeState(a, episodeState);
            // display snapshot

            if(episodeState.CurrentPosition == _exitPosition) break;

        }

    }
    
    private void ApplyActionOnEpisodeState(Action action, EpisodeState episodeState)
    {
        _episodeEngine.Move(action, episodeState);
    }

  

}

