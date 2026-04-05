using Pokémon.Maze.Core.Enums;
using Pokémon.Maze.Image;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Generate matrix data + image overlay from a clean maze image.
/// (The matrix will be probably need some refining to be usable for pathfinding:
/// - entrance / exit are not detected
/// - jumps are not detected
/// - ladders are detected as walls
/// - ...
/// </summary>
class Program
{
    private const string FOLDER = "output";
    private const string OUTPUT_MATRIX_DATA_FILE = "maze_grid.data";
    private const string OUTPUT_MATRIX_IMAGE_FILE = "maze_grid_overlay.png";
    private const int GRID_SAMPLING_SIZE = 16;      // std pokémon tile size is 16x16 pixels
    private const float COLOR_THRESHOLD = 167f;     // empirically found ( ok balance walkable/wall detection)
    private const bool USE_CENTER_PIXEL = false;


    // Ice reference tile origin (top-left corner of image in pixels)
    private const int ICE_REF_TILE_X = 32;
    private const int ICE_REF_TILE_Y = 32;
    private const float ICE_COLOR_DISTANCE_THRESHOLD = 5f; // max Euclidean RGB distance to match ice

    // colors
    private static readonly Rgba32 WALL_COLOR = new(220, 50, 50, 110);
    private static readonly Rgba32 WALKABLE_COLOR = new(50, 220, 50, 60);
    private static readonly Rgba32 ICE_COLOR = new(80, 180, 255, 130);

    // color mapping
    private static readonly Dictionary<ObjectEnum, Rgba32> OBJECT_COLOR_MAP = new() {
        {ObjectEnum.Wall, WALL_COLOR},
        {ObjectEnum.Empty, WALKABLE_COLOR},
        {ObjectEnum.Ice, ICE_COLOR}
    };

    static void Main(string[] args)
    {
        string? imagePath = null;
        float threshold = COLOR_THRESHOLD;
        bool useCenter = USE_CENTER_PIXEL;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--image":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("Missing value after --image.");
                    imagePath = args[++i];
                    break;
                case "--threshold":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("Missing value after --threshold.");
                    threshold = float.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--center":
                    useCenter = true;
                    break;
                case "--help":
                case "-h":
                    Console.WriteLine("Usage: --image <path> [--threshold <value>] [--center]");
                    return;
                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}. Use --help for usage.");
            }
        }

        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("Image path is required (see --help).\n");

        string fileId = $"{threshold}_{useCenter}_{ICE_COLOR_DISTANCE_THRESHOLD}";

        // Sampling
        ushort[,] grid = ImageService.GetMatrixFromImage(imagePath, GRID_SAMPLING_SIZE, useCenter, threshold, ICE_COLOR_DISTANCE_THRESHOLD, ICE_REF_TILE_X, ICE_REF_TILE_Y);

        // Save grid
        Directory.CreateDirectory(FOLDER);
        string dataFileName = Path.Combine(FOLDER, OUTPUT_MATRIX_DATA_FILE.Replace(".", $"_{fileId}."));
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        using (var sw = new StreamWriter(dataFileName))
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                    sw.Write(grid[r, c]);
                if (r < rows - 1) sw.WriteLine();
            }

        Console.WriteLine($"Grid saved into: {dataFileName}");

        // Visual overlay
        string visualFileName = Path.Combine(FOLDER, OUTPUT_MATRIX_IMAGE_FILE.Replace(".", $"_{fileId}."));
        ImageService.GenerateVisualOverlayImage(imagePath, GRID_SAMPLING_SIZE, OBJECT_COLOR_MAP, grid, visualFileName);
        
        Console.WriteLine($"Visual overlay saved into: {visualFileName}");
    }


}