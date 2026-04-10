namespace Pokémon.Maze.TrainingField;

public static class Rewards
{
    public const float PearlPickedUp = 10;
    public const float TM24PickedUp = 50;
    public const float MasterballPickedUp = 300;
    public const float Timeout = -50;
    public const float ExitFound = 1000;
    public const float StepCost = -1;
    public const float CloseToExit = 0.5f;
    public const float RevisitingCell = -0.5f;
}