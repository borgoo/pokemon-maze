using Pokémon.Maze.Core.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

/// <summary>
/// GRID SAMPLING
/// Identify:
/// - walkable tiles
/// - walls
/// - ice tiles (starting from a ice reference tile)
/// </summary>
class Program
{
    private const string FOLDER = "output";
    private const string OUTPUT_MATRIX_DATA_FILE = "maze_grid.data";
    private const string OUTPUT_MATRIX_IMAGE_FILE = "maze_grid_overlay.png";
    private const int GRID_SAMPLING_SIZE = 16;
    private const float COLOR_THRESHOLD = 167f;
    private const bool USE_CENTER_PIXEL = false;

    // Ice reference tile origin (top-left corner of image in pixels)
    private const int ICE_REF_TILE_X = 32;
    private const int ICE_REF_TILE_Y = 32;
    private const float ICE_COLOR_DISTANCE_THRESHOLD = 5f; // max Euclidean RGB distance to match ice

    // grid values
    private static readonly short WALKABLE = (short)ObjectEnum.Empty;
    private static readonly short WALL = (short)ObjectEnum.Wall;
    private static readonly short ICE = (short)ObjectEnum.Ice;

    // colors
    private static readonly Rgba32 WALL_COLOR = new(220, 50, 50, 110);
    private static readonly Rgba32 WALKABLE_COLOR = new(50, 220, 50, 60);
    private static readonly Rgba32 ICE_COLOR = new(80, 180, 255, 130);

    static void Main(string[] args)
    {
        string? imagePath = null;
        float threshold = COLOR_THRESHOLD;
        bool useCenter = USE_CENTER_PIXEL;

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--image": imagePath = args[i + 1]; break;
                case "--threshold": threshold = float.Parse(args[i + 1]); break;
                case "--center": useCenter = true; break;
                case "--help":
                case "-h":
                default:
                    Console.WriteLine("Help: --image <path> [--threshold <value>] [--center]");
                    return;
            }
        }

        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("Image path is required (see --help).\n");

        string fileId = $"{threshold}_{useCenter}_{ICE_COLOR_DISTANCE_THRESHOLD}";
        using var img = Image.Load<Rgba32>(imagePath);
        int rows = img.Height / GRID_SAMPLING_SIZE;
        int cols = img.Width / GRID_SAMPLING_SIZE;

        Console.WriteLine($"[info] Image dimension: {img.Width}px x {img.Height}px -> {cols}x{rows} resulting grid");
        Console.WriteLine($"[info] Config: {(useCenter ? "central pixel" : "tile color avg")} | threshold: {threshold} | file id: {fileId} | output directory: {FOLDER}");

        (float R, float G, float B) iceRef = ComputeTileAvgColor(img, ICE_REF_TILE_X, ICE_REF_TILE_Y);
        Console.WriteLine($"[info] Ice reference avg color: R={iceRef.R:F0} G={iceRef.G:F0} B={iceRef.B:F0}");

        // Sampling
        short[,] grid = new short[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float brightness;
                (float R, float G, float B) tileAvg;

                if (useCenter) // center pixel sampling
                {
                    int cy = r * GRID_SAMPLING_SIZE + GRID_SAMPLING_SIZE / 2;
                    int cx = c * GRID_SAMPLING_SIZE + GRID_SAMPLING_SIZE / 2;
                    var px = img[cx, cy];
                    brightness = (px.R + px.G + px.B) / 3f;
                    tileAvg = (px.R, px.G, px.B);
                }
                else // avg tile color sampling
                {
                    tileAvg = ComputeTileAvgColor(img, c * GRID_SAMPLING_SIZE, r * GRID_SAMPLING_SIZE);
                    brightness = (tileAvg.R + tileAvg.G + tileAvg.B) / 3f;
                }

                bool isWall = brightness <= threshold;

                if (!isWall) {

                    float dist = ColorDistance(tileAvg, iceRef);
                    grid[r, c] = dist <= ICE_COLOR_DISTANCE_THRESHOLD ? ICE : WALKABLE;

                    continue;

                }

                grid[r, c] = WALL;

            }
        }

        Directory.CreateDirectory(FOLDER);

        // Save grid
        string dataFileName = Path.Combine(FOLDER, OUTPUT_MATRIX_DATA_FILE.Replace(".", $"_{fileId}."));
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
        using var result = img.Clone(ctx => { });

        result.Mutate(ctx =>
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    int x0 = c * GRID_SAMPLING_SIZE;
                    int y0 = r * GRID_SAMPLING_SIZE;
                    var rect = new RectangleF(x0, y0, GRID_SAMPLING_SIZE, GRID_SAMPLING_SIZE);

                    Rgba32 fillColor;
                    if (grid[r, c] == WALL) fillColor = WALL_COLOR;
                    else if (grid[r, c] == ICE) fillColor = ICE_COLOR;
                    else fillColor = WALKABLE_COLOR;

                    ctx.Fill(fillColor, rect);
                    ctx.Draw(new Rgba32(255, 255, 255, 80), 1f, rect);
                }
        });

        result.Save(visualFileName);
        Console.WriteLine($"Visual overlay saved into: {visualFileName}");
    }

    private static (float R, float G, float B) ComputeTileAvgColor(Image<Rgba32> img, int originX, int originY)
    {
        double sumR = 0, sumG = 0, sumB = 0;
        int count = 0;
        for (int dy = 0; dy < GRID_SAMPLING_SIZE; dy++)
            for (int dx = 0; dx < GRID_SAMPLING_SIZE; dx++)
            {
                var px = img[originX + dx, originY + dy];
                sumR += px.R; sumG += px.G; sumB += px.B;
                count++;
            }
        return ((float)(sumR / count), (float)(sumG / count), (float)(sumB / count));
    }

    private static float ColorDistance((float R, float G, float B) a, (float R, float G, float B) b) // euclidean
    {
        float dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
        return MathF.Sqrt(dr * dr + dg * dg + db * db);
    }
}