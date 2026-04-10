using System.Threading.Channels;
using Pokémon.Maze.Core.Enums;
using Pokémon.Maze.Core.RL;
using Pokémon.Maze.AI.UI.models;
using Action = Pokémon.Maze.Core.RL.Action;
namespace Pokémon.Maze.AI.UI;

internal readonly record struct EpisodeFrame(
    int StepIndex,
    (int X, int Y) Position,
    char Direction,
    bool Hidden,
    bool Tm24Available,
    bool PearlAvailable,
    bool MasterballAvailable
);

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
    private readonly ((int X, int Y) A, (int X, int Y) B)? _ladderPair = FindLadderEndpoints(matrix);

    /// <summary>
    /// Pattern A: the orchestrator produces frames into a channel (producer),
    /// while the UI consumes frames at its own pace (consumer).
    /// </summary>
    public ChannelReader<EpisodeFrame> StartFrames(QTable qTable, int maxSteps = 10_000, CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<EpisodeFrame>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        _ = Task.Run(async () =>
        {
            var writer = channel.Writer;
            try
            {
                AgentPawn agent = AgentPawn.Create(qTable, (matrix.GetLength(0), matrix.GetLength(1)));
                EpisodeState episodeState = new(_entrancePosition, _itemsPositions);

                int frameIndex = 0;
                await writer.WriteAsync(MakeFrame(frameIndex++, episodeState, 'v', hidden: false), cancellationToken);

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (episodeState.CurrentStep >= maxSteps) break;
                    if (episodeState.CurrentPosition == _exitPosition) break;

                    HashSet<Action> availableActions = _episodeEngine.GetAvailableActions(episodeState);
                    Action a = agent.ChooseAction(availableActions, episodeState.ToQStatus());

                    var prev = episodeState.CurrentPosition;
                    ApplyActionOnEpisodeState(a, episodeState);
                    var next = episodeState.CurrentPosition;
                    var dir = DirectionFromAction(a);

                    // Item pickup (engine reverts position): no linear expansion, but UI still needs a frame for overlays.
                    if (prev == next)
                    {
                        await writer.WriteAsync(MakeFrame(frameIndex++, episodeState, next, dir, hidden: false), cancellationToken);
                        continue;
                    }

                    // Ladder teleport: walk onto the first ladder tile, then blink there, then appear on the paired ladder.
                    if (IsLadder(next) && Manhattan(prev, next) != 1 && _ladderPair is { } pair)
                    {
                        var ladderEntered = PartnerLadder(pair, next);
                        foreach (var p in ExpandLinear(prev, ladderEntered))
                            await writer.WriteAsync(MakeFrame(frameIndex++, episodeState, p, dir, hidden: false), cancellationToken);
                        await writer.WriteAsync(MakeFrame(frameIndex++, episodeState, ladderEntered, dir, hidden: true), cancellationToken);
                        await writer.WriteAsync(MakeFrame(frameIndex++, episodeState, next, dir, hidden: false), cancellationToken);
                        continue;
                    }

                    // If the engine moved multiple tiles in one action (ice slide / jump),
                    // emit intermediate tiles for smoother autoplay.
                    foreach (var p in ExpandLinear(prev, next))
                        await writer.WriteAsync(MakeFrame(frameIndex++, episodeState, p, dir, hidden: false), cancellationToken);
                }
            }
            catch (OperationCanceledException oce)
            {
                writer.TryComplete(oce);
                return;
            }
            catch (Exception ex)
            {
                writer.TryComplete(ex);
                return;
            }

            writer.TryComplete();
        }, cancellationToken);

        return channel.Reader;
    }
    
    private void ApplyActionOnEpisodeState(Action action, EpisodeState episodeState)
    {
        _episodeEngine.Move(action, episodeState);
    }

    private static ((int X, int Y) A, (int X, int Y) B)? FindLadderEndpoints(ushort[,] m)
    {
        (int X, int Y)? a = null, b = null;
        int n = 0;
        for (int i = 0; i < m.GetLength(0); i++)
        for (int j = 0; j < m.GetLength(1); j++)
            if (m[i, j] == (ushort)ObjectEnum.Ladder)
            {
                n++;
                if (a is null) a = (i, j);
                else b = (i, j);
            }

        if (n != 2 || a is null || b is null)
            return null;

        return (a.Value, b.Value);
    }

    private static (int X, int Y) PartnerLadder(((int X, int Y) A, (int X, int Y) B) pair, (int X, int Y) oneEnd)
        => oneEnd == pair.A ? pair.B : pair.A;

    private bool IsLadder((int X, int Y) p)
        => p.X >= 0 && p.Y >= 0
           && p.X < matrix.GetLength(0) && p.Y < matrix.GetLength(1)
           && matrix[p.X, p.Y] == (ushort)ObjectEnum.Ladder;

    private EpisodeFrame MakeFrame(int stepIndex, EpisodeState episodeState, char direction, bool hidden)
    {
        var (t, p, m) = ItemFlags(episodeState);
        return new EpisodeFrame(stepIndex, episodeState.CurrentPosition, direction, hidden, t, p, m);
    }

    private EpisodeFrame MakeFrame(int stepIndex, EpisodeState episodeState, (int X, int Y) position, char direction, bool hidden)
    {
        var (t, p, m) = ItemFlags(episodeState);
        return new EpisodeFrame(stepIndex, position, direction, hidden, t, p, m);
    }

    private (bool Tm24, bool Pearl, bool Masterball) ItemFlags(EpisodeState episodeState)
    {
        var items = episodeState.AvailableItems;
        return (
            items.Contains(_itemsPositions[ItemType.TM24]),
            items.Contains(_itemsPositions[ItemType.Pearl]),
            items.Contains(_itemsPositions[ItemType.Masterball])
        );
    }

    private static int Manhattan((int X, int Y) a, (int X, int Y) b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    private static char DirectionFromAction(Action action) => action switch
    {
        Action.MoveUp => '^',
        Action.MoveDown => 'v',
        Action.MoveLeft => '<',
        Action.MoveRight => '>',
        _ => 'v'
    };

    // Returns next positions from prev->next, including next and excluding prev, when movement is axis-aligned.
    // If not aligned (unexpected), just returns next.
    private static IEnumerable<(int X, int Y)> ExpandLinear((int X, int Y) prev, (int X, int Y) next)
    {
        int dx = Math.Sign(next.X - prev.X);
        int dy = Math.Sign(next.Y - prev.Y);

        if (dx != 0 && dy != 0)
        {
            yield return next;
            yield break;
        }

        var cur = prev;
        while (cur != next)
        {
            cur = (cur.X + dx, cur.Y + dy);
            yield return cur;
        }
    }
  

}

