using Pokémon.Maze.Core.RL;
using Pokémon.Maze.TrainingField.models;
using Action = Pokémon.Maze.Core.RL.Action;

namespace Pokémon.Maze.TrainingField;

internal sealed class TrainingEpisodeOrchestrator(
    (int X, int Y) entrancePosition,
    (int X, int Y) exitPosition,
    ushort[,] matrix,
    IReadOnlyDictionary<ItemType, (int X, int Y)> itemsPositions,
    int? maxSteps = null
)
{
    private const float STARTING_ALPHA = 0.1f;
    private const float STARTING_EPSILON = 1f;
    private const float ENDING_EPSILON = 0.01f;
    private const float EPSILON_CUTOFF = 0.9f;
    private const float GAMMA = 0.9f;


    private readonly int _maxSteps = maxSteps ?? int.MaxValue;
    private readonly EpisodeEngine _episodeEngine = new(matrix);
    private readonly (int X, int Y) _entrancePosition = entrancePosition;
    private readonly (int X, int Y) _exitPosition = exitPosition;
    private readonly IReadOnlyDictionary<ItemType, (int X, int Y)> _itemsPositions = itemsPositions;
    private readonly TrainingPawn _trainingPawn = new(
                                                    alpha: STARTING_ALPHA,
                                                    gamma: GAMMA,
                                                    matrixSize: (matrix.GetLength(0), matrix.GetLength(1))
                                                );

    /// <summary>
    /// Run an episode and return the QTable as a string
    /// </summary>
    /// <param name="currEpisodeNum"></param>
    /// <param name="totalEpisodes"></param>
    /// <returns>QTable as string</returns>
    public string Run(int currEpisodeNum, int totalEpisodes) 
    { 

        EpisodeState episodeState = new(_entrancePosition, _itemsPositions);

        float epsilon = GetEpsilon(currEpisodeNum, totalEpisodes);


        HashSet<Action> availableActions = _episodeEngine.GetAvailableActions(episodeState);

        while(true) { 

            QStatus s = episodeState.ToQStatus();
            int stepCount = episodeState.CurrentStep;

            Action a = _trainingPawn.ChooseAction(availableActions, s, epsilon);

            ApplyActionOnEpisodeState(a, episodeState);

            // s'
            QStatus nextS = episodeState.ToQStatus();
            int nextStepCount = episodeState.CurrentStep;
            bool timeout = episodeState.CurrentStep >= _maxSteps;
            bool exitFound = episodeState.CurrentPosition == _exitPosition;
            bool done = timeout || exitFound;

            // r
            float r = ComputeReward(s, stepCount, nextS, nextStepCount, timeout, exitFound);

            // actions valid for s' -> for the next loop as well
            HashSet<Action> nextAvailableActions = _episodeEngine.GetAvailableActions(episodeState);

            if(done){
                _trainingPawn.LearnFromTerminalState(s, a, r);
                break; 
            }

            _trainingPawn.LearnAndBootstrap(s, a, r, nextS, nextAvailableActions);
            availableActions = nextAvailableActions; 

        }

        return _trainingPawn.ExportQTable();

    }
    private void ApplyActionOnEpisodeState(Action action, EpisodeState episodeState)
    {
        _episodeEngine.Move(action, episodeState);
    }

    private static float ComputeReward(QStatus s, int stepCount, QStatus nextS, int nextStepCount, bool timeout, bool exitFound)
    {
        float reward = (nextStepCount - stepCount) * Rewards.StepCost; // - delta steps
        if(s.TM24PickedUp != nextS.TM24PickedUp) reward += Rewards.TM24PickedUp;
        if(s.MasterballPickedUp != nextS.MasterballPickedUp) reward += Rewards.MasterballPickedUp;
        if(s.PearlPickedUp != nextS.PearlPickedUp) reward += Rewards.PearlPickedUp;

        if(timeout) reward += Rewards.Timeout;
        if(exitFound) reward += Rewards.ExitFound;
        

        return reward;
    }

    private static float GetEpsilon(int currEpisodeNum, int totalEpisodes)
    {
        float decayEpisodes = totalEpisodes * EPSILON_CUTOFF;

        if (currEpisodeNum >= decayEpisodes)
            return ENDING_EPSILON;

        float t = currEpisodeNum / decayEpisodes;   
        float cosDecay = 0.5f * (1 + MathF.Cos(MathF.PI * t));
        return ENDING_EPSILON + (STARTING_EPSILON - ENDING_EPSILON) * cosDecay;
    }


}
