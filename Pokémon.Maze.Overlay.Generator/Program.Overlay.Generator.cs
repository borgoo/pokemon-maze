using Pokémon.Maze.Core;
using Pokémon.Maze.Core.Enums;
using Pokémon.Maze.Image;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Generate overlay image from a matrix.
/// Use this to verify the edits made to the matrix data file, by visualizing them on top of the original maze image.
/// </summary>
class Program
{
    private const string FOLDER = "output";
    private const string OUTPUT_MATRIX_IMAGE_FILE = "maze_grid_overlay.png";
    private const int GRID_SAMPLING_SIZE = 16;      // std pokémon tile size is 16x16 pixels


    // colors
    private static readonly Rgba32 WALL_COLOR = new(220, 50, 50, 110);
    private static readonly Rgba32 WALKABLE_COLOR = new(50, 220, 50, 60);
    private static readonly Rgba32 ICE_COLOR = new(80, 180, 255, 130);
    private static readonly Rgba32 ENTRANCE_COLOR = new(255, 210, 75, 130);
    private static readonly Rgba32 EXIT_COLOR = new(255, 130, 45, 130);
    private static readonly Rgba32 LADDER_COLOR = new(150, 80, 220, 130);
    private static readonly Rgba32 JUMP_DOWN_COLOR = new(40, 200, 110, 130);
    private static readonly Rgba32 JUMP_LEFT_COLOR = new(70, 130, 255, 130);

    // color mapping
    private static readonly Dictionary<ObjectEnum, Rgba32> OBJECT_COLOR_MAP = new() {
        {ObjectEnum.Wall, WALL_COLOR},
        {ObjectEnum.Empty, WALKABLE_COLOR},
        {ObjectEnum.Ice, ICE_COLOR},
        {ObjectEnum.Entrance, ENTRANCE_COLOR},
        {ObjectEnum.Exit, EXIT_COLOR},
        {ObjectEnum.Ladder, LADDER_COLOR},
        {ObjectEnum.JumpDown, JUMP_DOWN_COLOR},
        {ObjectEnum.JumpLeft, JUMP_LEFT_COLOR}
    };

    static void Main(string[] args)
    {
        string? imagePath = null;
        string? gridPath = null;

        for (int i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "--image":
                    if (++i >= args.Length) throw new ArgumentException("--image requires a path.");
                    imagePath = args[i];
                    break;
                case "--grid":
                    if (++i >= args.Length) throw new ArgumentException("--grid requires a path.");
                    gridPath = args[i];
                    break;
                case "--help" or "-h":
                    Console.WriteLine("Usage: --image <path> --grid <path>");
                    return;
                default:
                    Console.WriteLine($"Unknown argument: {args[i]}");
                    Console.WriteLine("Usage: --image <path> --grid <path>");
                    return;
            }

        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("Image path is required (see --help).\n");

        if (string.IsNullOrWhiteSpace(gridPath))
            throw new ArgumentException("Grid path is required (see --help).\n");



        // load grid
        ushort [,] grid = MazeLoader.GetFromFile(gridPath);

        // Visual overlay
        string fileId = $"{DateTime.Now.Ticks:x}";
        string path = Path.Combine(FOLDER, fileId);
        Directory.CreateDirectory(path);
        string visualFileName = Path.Combine(path, OUTPUT_MATRIX_IMAGE_FILE);
        ImageService.GenerateVisualOverlayImage(imagePath, GRID_SAMPLING_SIZE, OBJECT_COLOR_MAP, grid, visualFileName);

        Console.WriteLine($"Visual overlay saved into: {visualFileName}");
    }


}