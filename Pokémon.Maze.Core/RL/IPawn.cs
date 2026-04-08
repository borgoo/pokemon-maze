namespace Pokémon.Maze.Core.RL;

internal interface IPawn
{
    public Action ChooseAction(HashSet<Action> availableActions, EpisodeState episodeState);
}