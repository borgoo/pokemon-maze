namespace Pokémon.Maze.Core.RL;

internal sealed class Episode(
    (ushort X, ushort Y) entrancePosition,
    (ushort X, ushort Y) exitPosition,
    ushort[,] matrix,
    HashSet<(int X, int Y)> availableItems,
    IPawn pawn,    
    
    int? maxSteps = null
)
{
    private readonly int _maxSteps = maxSteps ?? int.MaxValue;
    private readonly EpisodeEngine _episodeEngine = new(matrix);
    private readonly EpisodeState _episodeState = new(entrancePosition, availableItems);
    private readonly IPawn _pawn = pawn;

    public void Run() { 

        while(_episodeState.CurrentPosition != exitPosition) { // found exit

            HashSet<Action> availableActions = _episodeEngine.GetAvailableActions(_episodeState);

            Action action = _pawn.ChooseAction(availableActions, _episodeState);

            ApplyActionOnEpisodeState(action, _episodeState);

            if(_episodeState.CurrentStep >= _maxSteps ) throw new Exception("Too many steps");  
        }

        // end    

    }

    private void ApplyActionOnEpisodeState(Action action, EpisodeState episodeState)
    {
        _episodeEngine.Move(action, episodeState);
    }

}
