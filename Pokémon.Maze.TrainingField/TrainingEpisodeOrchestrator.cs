using Pokémon.Maze.Core.RL;
using Pokémon.Maze.TrainingField.models;
using Action = Pokémon.Maze.Core.RL.Action;

namespace Pokémon.Maze.TrainingField;

internal sealed class TrainingEpisodeOrchestrator(
    (int X, int Y) entrancePosition,
    (int X, int Y) exitPosition,
    ushort[,] matrix,
    IReadOnlyDictionary<ItemType, (int X, int Y)> itemsPositions,
    TrainingPawn trainingPawn,
    int? maxSteps = null
)
{
    private const float STARTING_EPSILON = 1f;
    private const float ENDING_EPSILON = 0.01f;
    private const float EPSILON_CUTOFF = 0.9f;


    private readonly int _maxSteps = maxSteps ?? int.MaxValue;
    private readonly EpisodeEngine _episodeEngine = new(matrix);
    private readonly (int X, int Y) _entrancePosition = entrancePosition;
    private readonly (int X, int Y) _exitPosition = exitPosition;
    private readonly IReadOnlyDictionary<ItemType, (int X, int Y)> _itemsPositions = itemsPositions;
    private readonly TrainingPawn _trainingPawn = trainingPawn;


    public bool Run(int currEpisodeNum, int totalEpisodes) 
    { 

        EpisodeState episodeState = new(_entrancePosition, _itemsPositions);

        float epsilon = GetEpsilon(currEpisodeNum, totalEpisodes);


        HashSet<Action> availableActions = _episodeEngine.GetAvailableActions(episodeState);
        bool timeout = false;
        bool exitFound = false;

        while(true) { 

            QStatus s = episodeState.ToQStatus();
            int stepCount = episodeState.CurrentStep;


            Action a = _trainingPawn.ChooseAction(availableActions, s, epsilon);

            ApplyActionOnEpisodeState(a, episodeState);

            // s'
            QStatus nextS = episodeState.ToQStatus();
            int nextStepCount = episodeState.CurrentStep;
            timeout = episodeState.CurrentStep >= _maxSteps;
            exitFound = episodeState.CurrentPosition == _exitPosition;
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

        return exitFound;
    }
    private void ApplyActionOnEpisodeState(Action action, EpisodeState episodeState)
    {
        _episodeEngine.Move(action, episodeState);
    }

    private float ComputeReward(QStatus s, int stepCount, QStatus nextS, int nextStepCount, bool timeout, bool exitFound)
    {
        // - delta steps
        float reward = (nextStepCount - stepCount) * Rewards.StepCost; // - delta steps

        // manhattan distance to the exit
        int sDistanceToExit = GetManhattanDistance((s.X, s.Y), _exitPosition);
        int nextSDistanceToExit = GetManhattanDistance((nextS.X, nextS.Y), _exitPosition);
        int distDelta = sDistanceToExit - nextSDistanceToExit;
        float shaping = nextSDistanceToExit < sDistanceToExit 
                        ? distDelta * Rewards.CloseToExit               // + reward if closer to the exit
                        : distDelta * (Rewards.CloseToExit * 0.5f);     // if I'm farther - penalization ( smaller that reward to encourage exploration)
        reward += shaping;

        // + items rewards
        if (s.TM24PickedUp != nextS.TM24PickedUp) reward += Rewards.TM24PickedUp;
        if(s.MasterballPickedUp != nextS.MasterballPickedUp) reward += Rewards.MasterballPickedUp;
        if(s.PearlPickedUp != nextS.PearlPickedUp) reward += Rewards.PearlPickedUp;

        // termination rewards
        if (timeout) reward += Rewards.Timeout;
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

    private static int GetManhattanDistance((int X, int Y) pos1, (int X, int Y) pos2) => Math.Abs(pos1.X - pos2.X) + Math.Abs(pos1.Y - pos2.Y);

}
