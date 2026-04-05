using Pokémon.Maze.Core.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Pokémon.Maze.Image;

public static class ImageService
{

    // grid values
    private static readonly ushort WALKABLE = (ushort)ObjectEnum.Empty;
    private static readonly ushort WALL = (ushort)ObjectEnum.Wall;
    private static readonly ushort ICE = (ushort)ObjectEnum.Ice;



   
    public static void GenerateVisualOverlayImage(
        string inputImageFilePath,
        int gridSamplingSize,  
        Dictionary<ObjectEnum, Rgba32> objectColorMap,
        ushort[,] grid,
        string resultFilePath
    )
    {
        using Image<Rgba32> img = SixLabors.ImageSharp.Image.Load<Rgba32>(inputImageFilePath);
        using var result = img.Clone(ctx => { });

        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        result.Mutate(ctx =>
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    int x0 = c * gridSamplingSize;
                    int y0 = r * gridSamplingSize;
                    var rect = new RectangleF(x0, y0, gridSamplingSize, gridSamplingSize);

                    ObjectEnum val = Enum.IsDefined(typeof(ObjectEnum), grid[r, c])
                        ? (ObjectEnum)grid[r, c]
                        : throw new ArgumentException($"Invalid object value: {grid[r, c]}");                    

                    Rgba32 fillColor = objectColorMap.TryGetValue(val, out Rgba32 value) ? value : throw new ArgumentException($"No color mapping for object value: {val}");

                    ctx.Fill(fillColor, rect);
                    ctx.Draw(new Rgba32(255, 255, 255, 80), 1f, rect);
                }
        });

        result.Save(resultFilePath);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputImageFilePath"></param>
    /// <param name="gridSamplingSize"></param>
    /// <param name="userCenterAlgorithm"></param>
    /// <param name="threshold"></param>
    /// <param name="iceColorDistanceThreshold">Max Euclidean RGB distance to match ice</param>
    /// <param name="iceRefTileX">Ice reference tile origin (top-left corner of image in pixels)</param>
    /// <param name="iceRefTileY">Ice reference tile origin (top-left corner of image in pixels)</param>
    /// <returns></returns>
    public static ushort[,] GetMatrixFromImage(
        string inputImageFilePath,
        int gridSamplingSize,
        bool userCenterAlgorithm,
        float threshold,
        float iceColorDistanceThreshold,
        int iceRefTileX, 
        int iceRefTileY
    ) {

        using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(inputImageFilePath);
        int rows = img.Height / gridSamplingSize;
        int cols = img.Width / gridSamplingSize;
        (float R, float G, float B) iceRef = ComputeTileAvgColor(img, iceRefTileX, iceRefTileY, gridSamplingSize);

        ushort[,] grid = new ushort[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float brightness;
                (float R, float G, float B) tileAvg;

                if (userCenterAlgorithm) // center pixel sampling
                {
                    int cy = r * gridSamplingSize + gridSamplingSize / 2;
                    int cx = c * gridSamplingSize + gridSamplingSize / 2;
                    var px = img[cx, cy];
                    brightness = (px.R + px.G + px.B) / 3f;
                    tileAvg = (px.R, px.G, px.B);
                }
                else // avg tile color sampling
                {
                    tileAvg = ComputeTileAvgColor(img, c * gridSamplingSize, r * gridSamplingSize, gridSamplingSize);
                    brightness = (tileAvg.R + tileAvg.G + tileAvg.B) / 3f;
                }

                bool isWall = brightness <= threshold;

                if (!isWall)
                {

                    float dist = ColorDistance(tileAvg, iceRef);
                    grid[r, c] = dist <= iceColorDistanceThreshold ? ICE : WALKABLE;

                    continue;

                }

                grid[r, c] = WALL;

            }
        }

        return grid;


    }

    private static (float R, float G, float B) ComputeTileAvgColor(Image<Rgba32> img, int originX, int originY, int gridSamplingSize)
    {
        double sumR = 0, sumG = 0, sumB = 0;
        int count = 0;
        for (int dy = 0; dy < gridSamplingSize; dy++)
            for (int dx = 0; dx < gridSamplingSize; dx++)
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
