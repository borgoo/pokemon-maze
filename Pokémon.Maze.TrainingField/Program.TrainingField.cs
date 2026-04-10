using Pokémon.Maze.Core;
using Pokémon.Maze.Core.Enums;
using Pokémon.Maze.Core.RL;
using Pokémon.Maze.TrainingField.models;
using System.Globalization;

namespace Pokémon.Maze.TrainingField;

internal static class Program
{
    private const int TIMEOUT_STEPS = 2000;         // empiric value
    private const float STARTING_ALPHA = 0.1f;
    private const float GAMMA = 0.99f;

    private const string OUTPUT_DIRECTORY_NAME = "outputs";
    private const string OUTPUT_FILE_NAME = "qTable.bin";
    private static readonly string _directory = Path.Combine(OUTPUT_DIRECTORY_NAME, Guid.CreateVersion7().ToString());

    private static readonly IReadOnlyDictionary<ItemType, (int X, int Y)> _itemsPositions = SharedItemsPositions.Get;

    private readonly static CultureInfo _cultureInfo = new("it-IT");

    public static void Main(string[] args)
    {

        (string gridPath, int numOfEpisodes, bool saveIntermediate) = HandleParams(args);
        ushort[,] matrix = MazeLoader.GetFromFile(gridPath);

        (int X, int Y)? entrancePosition = null;
        (int X, int Y)? exitPosition = null;
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                if (matrix[i, j] == (ushort)ObjectEnum.Entrance) entrancePosition = (i, j);
                else if (matrix[i, j] == (ushort)ObjectEnum.Exit) exitPosition = (i, j);
            }
        }

        if (entrancePosition is null) throw new ArgumentException("Entrance position not found.");
        if (exitPosition is null) throw new ArgumentException("Exit position not found.");

        const int progressPrintInterval = 10; // print progress every 10%
        Console.WriteLine($"({DateTime.Now:HH:mm:ss}) Training started...");
        Console.WriteLine($"Grid: {gridPath} | Number of episodes: {numOfEpisodes.ToString("N0", _cultureInfo)} | Timeout steps: {TIMEOUT_STEPS.ToString("N0", _cultureInfo)} | Progress print interval: {progressPrintInterval}%");

        TrainingPawn trainingPawn = new(STARTING_ALPHA, GAMMA, (matrix.GetLength(0), matrix.GetLength(1)));
        TrainingEpisodeOrchestrator orchestrator = new(entrancePosition.Value, exitPosition.Value, matrix, _itemsPositions, trainingPawn, TIMEOUT_STEPS);

        int numOfExitFound = 0;
        for (int i = 1; i <= numOfEpisodes; i++)
        {

            bool foundExit = orchestrator.Run(i, numOfEpisodes);

            if (foundExit) numOfExitFound++;

            if (i % (numOfEpisodes / (100 / progressPrintInterval)) == 0)
            {
                Console.WriteLine($"({DateTime.Now:HH:mm:ss}) Episodes done : {i.ToString("N0", _cultureInfo)} | Exits found: {numOfExitFound.ToString("N0", _cultureInfo)}");
                if (saveIntermediate) SaveQTableToFile(trainingPawn, i);
            }

        }

        Console.WriteLine($"({DateTime.Now:HH:mm:ss}) Training completed.");

        SaveQTableToFile(trainingPawn);

    }

    private static (string gridPath, int numOfEpisodes, bool saveIntermediate) HandleParams(string[] args)
    {

        string? gridPath = null;
        int? numOfEpisodes = null;
        bool saveIntermediate = false;
        for (int i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "--grid":
                    if (++i >= args.Length) throw new ArgumentException("--grid requires a path.");
                    gridPath = args[i];
                    break;

                case "--num":
                case "-n":
                    if (++i >= args.Length) throw new ArgumentException("--num requires a number.");
                    numOfEpisodes = int.Parse(args[i]);
                    break;

                case "--save-intermediate":
                case "-s":
                    saveIntermediate = true;
                    break;

                case "--help" or "-h":
                    Console.WriteLine("Usage: --grid <path> --num|-n <number> [--save-intermediate|-s]");
                    Environment.Exit(-1);
                    break;

                default:
                    Console.WriteLine($"Unknown argument: {args[i]}");
                    Console.WriteLine("Usage: --grid <path>");
                    Console.WriteLine("\t\t--num <number>");
                    Console.WriteLine("\t\t[--save-intermediate|-s]");
                    Environment.Exit(-1);
                    break;
            }

        if (string.IsNullOrWhiteSpace(gridPath))
            throw new ArgumentException("Grid path is required (see --help).\n");
        if (numOfEpisodes is null) throw new ArgumentException("Number of episodes is required (see --help).\n");

        return (gridPath, numOfEpisodes.Value, saveIntermediate);

    }

    private static void SaveQTableToFile(TrainingPawn trainingPawn, int? num = null)
    {
        Directory.CreateDirectory(_directory);
        string fileName = num is not null ? $"{Path.GetFileNameWithoutExtension(OUTPUT_FILE_NAME)}_{num}{Path.GetExtension(OUTPUT_FILE_NAME)}" : OUTPUT_FILE_NAME;
        string path = Path.Combine(_directory, fileName);
        using var stream = File.Open(path, FileMode.Create);
        using var writer = new BinaryWriter(stream);
        trainingPawn.SaveQTable(writer);
        Console.WriteLine("QTable saved to: " + path);
    }
}
