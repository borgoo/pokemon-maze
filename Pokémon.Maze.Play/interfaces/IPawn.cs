using Pokémon.Maze.Core.RL;
using Action = Pokémon.Maze.Core.RL.Action;

namespace Pokémon.Maze.Play.interfaces;

/// <summary>
/// Play a game
/// </summary>
internal interface IPawn
{
    public Action ChooseAction(IReadOnlySet<Action> availableActions, QStatus s);

}
