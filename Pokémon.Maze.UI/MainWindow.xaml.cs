using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Pokémon.Maze.Core;
using Pokémon.Maze.Core.Enums;
using Pokémon.Maze.Core.models;

namespace Pokémon.Maze.UI;

public partial class MainWindow : Window
{
    // speed (less is faster!)
    const int EXPLORATION_SPEED = 180;
    const int SPRITE_SPEED = 200;

    // files
    const string MAZE_FILE = "maze.png";
    const string MAZE_GRID_FILE = "maze_grid.data";
    const string SPRITE_FILE = "sprite.png";

    // sprite background color to remove
    private static readonly Color SpriteSheetOrangeBackgroundTreatedAsTransparent = Color.FromRgb(0xFF, 0x7F, 0x27); // orange

    private static readonly Brush TransparentTile = Brushes.Transparent;
    private readonly Brush _visitedBrush = new SolidColorBrush(Color.FromArgb(0x60, 0x44, 0x52, 0x70)); // blue-gray
    private readonly Brush _frontierBrush = new SolidColorBrush(Color.FromArgb(0xaa, 0xff, 0xc0, 0x40)); // yellow

    private BitmapSource? _spriteSheet;
    private ushort[,]? _mazeTiles;
    private Rectangle[,]? _cells;
    private List<Snapshot> _snapshots = [];
    private int _rows;
    private int _cols;
    private int _maxTimelineStep;
    private int _currentStep;
    private int _animPhase;
    private readonly DispatcherTimer _timer;

    public MainWindow()
    {
        _visitedBrush.Freeze();
        _frontierBrush.Freeze();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(EXPLORATION_SPEED) };
        _timer.Tick += (_, _) => TimerTick();

        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var mazePath = System.IO.Path.Combine(baseDir, "assets", MAZE_FILE);
            var gridPath = System.IO.Path.Combine(baseDir, "assets", MAZE_GRID_FILE);
            var spritePath = System.IO.Path.Combine(baseDir, "assets", SPRITE_FILE);

            var matrix = MazeLoader.GetFromFile(gridPath);
            _mazeTiles = matrix;
            _rows = matrix.GetLength(0);
            _cols = matrix.GetLength(1);

            BuildOverlayGrid();

            MazeBackground.Source = LoadBitmapFrozen(mazePath);
            _spriteSheet = LoadSpriteSheetWithChromaKey(spritePath);

            await Task.Run(async () =>
            {
                var channel = Channel.CreateUnbounded<Snapshot>();
                var writeTask = MazeMatrix.SolveWithSnapshotsAsync(matrix, channel.Writer);
                var list = new List<Snapshot>();
                await foreach (var s in channel.Reader.ReadAllAsync())
                    list.Add(s);
                await writeTask;
                return list;
            }).ContinueWith(t =>
            {
                if (t.IsFaulted) throw t.Exception; // unsolvable maze

                _snapshots = t.Result;
                var pathLen = _snapshots[^1].FinalPath?.Length ?? 0;
                _maxTimelineStep = _snapshots.Count - 1 + Math.Max(0, pathLen);

                _currentStep = 0;
                _animPhase = 0;
                ApplyStep(0);
                _timer.Start();

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        catch
        {
            throw;
        }
    }

    private static BitmapImage LoadBitmapFrozen(string path)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.UriSource = new Uri(path, UriKind.Absolute);
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    private static WriteableBitmap LoadSpriteSheetWithChromaKey(string path)
        => ApplyChromaKeyTransparency(LoadBitmapFrozen(path), SpriteSheetOrangeBackgroundTreatedAsTransparent);

    private static WriteableBitmap ApplyChromaKeyTransparency(BitmapSource source, Color rgbKey)
    {
        var bgra = new FormatConvertedBitmap();
        bgra.BeginInit();
        bgra.Source = source;
        bgra.DestinationFormat = PixelFormats.Bgra32;
        bgra.EndInit();
        bgra.Freeze();

        var w = bgra.PixelWidth;
        var h = bgra.PixelHeight;
        var stride = w * 4;
        var pixels = new byte[stride * h];
        bgra.CopyPixels(pixels, stride, 0);

        var kb = rgbKey.B;
        var kg = rgbKey.G;
        var kr = rgbKey.R;
        for (var i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i] == kb && pixels[i + 1] == kg && pixels[i + 2] == kr)
                pixels[i + 3] = 0;
        }

        var writeable = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        writeable.WritePixels(new Int32Rect(0, 0, w, h), pixels, stride, 0);
        writeable.Freeze();
        return writeable;
    }

    private void BuildOverlayGrid()
    {
        OverlayCanvas.Children.Clear();
        _cells = new Rectangle[_rows, _cols];
        for (var r = 0; r < _rows; r++)
        {
            for (var c = 0; c < _cols; c++)
            {
                var rect = new Rectangle
                {
                    Width = 16,
                    Height = 16,
                    Fill = TransparentTile,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(rect, c * 16);
                Canvas.SetTop(rect, r * 16);
                OverlayCanvas.Children.Add(rect);
                _cells[r, c] = rect;
            }
        }
    }

    private void ApplyOverlay(Snapshot snapshot)
    {
        if (_cells is null) return;

        for (var r = 0; r < _rows; r++)
        {
            for (var c = 0; c < _cols; c++)
                _cells[r, c].Fill = TransparentTile;
        }

        foreach (var (x, y) in snapshot.Visited)
        {
            if (x >= _rows || y >= _cols) continue;
            _cells[x, y].Fill = snapshot.Frontier.Contains((x, y)) ? _frontierBrush : _visitedBrush;
        }
    }

    private static int FrameForDirection(char direction, bool walk, int phase)
    {
        return direction switch
        {
            'v' => walk ? (phase % 3) switch { 0 => 0, 1 => 1, _ => 2 } : 1,
            '^' => walk ? (phase % 3) switch { 0 => 3, 1 => 4, _ => 5 } : 4,
            '<' => walk ? (phase % 2 == 0 ? 7 : 6) : 6,
            '>' => walk ? (phase % 2 == 0 ? 9 : 8) : 8,
            _ => 1
        };
    }

    private void SetSpriteFrame(int frameIndex)
    {
        if (_spriteSheet is null) return;
        var cropped = new CroppedBitmap(_spriteSheet, new Int32Rect(frameIndex * 17, 0, 16, 16));
        cropped.Freeze();
        SpriteImage.Source = cropped;
    }

    private void ShowPathStep(int pathIndex)
    {
        var path = _snapshots[^1].FinalPath;
        if (path is null || path.Length == 0 || pathIndex < 0 || pathIndex >= path.Length)
        {
            SpriteImage.Visibility = Visibility.Collapsed;
            return;
        }

        var (x, y, dir) = path[pathIndex];
        SpriteImage.Visibility = Visibility.Visible;
        Canvas.SetLeft(SpriteImage, y * 16);
        Canvas.SetTop(SpriteImage, x * 16);

        var onIce = _mazeTiles is not null
            && x < _rows && y < _cols
            && _mazeTiles[x, y] == (ushort)ObjectEnum.Ice;
        var walk = pathIndex < path.Length - 1 && !onIce;
        SetSpriteFrame(FrameForDirection(dir, walk, _animPhase));
    }

    private void ApplyStep(int step)
    {
        if (_snapshots.Count == 0) return;

        var n = _snapshots.Count;
        if (step < n)
        {
            OverlayCanvas.Visibility = Visibility.Visible;
            ApplyOverlay(_snapshots[step]);
            SpriteImage.Visibility = Visibility.Collapsed;
        }
        else
        {
            OverlayCanvas.Visibility = Visibility.Collapsed;
            var pathIndex = step - n;
            ShowPathStep(pathIndex);
        }
    }

    private void SyncTimerIntervalToPhase()
    {
        if (_snapshots.Count == 0) return;
        var n = _snapshots.Count;
        _timer.Interval = TimeSpan.FromMilliseconds(
            _currentStep >= n ? SPRITE_SPEED : EXPLORATION_SPEED);
    }

    private void TimerTick()
    {
        if (_snapshots.Count == 0) return;

        if (_currentStep < _maxTimelineStep)
        {
            _currentStep++;
            _animPhase++;
            ApplyStep(_currentStep);
            SyncTimerIntervalToPhase();
        }
        else
            _timer.Stop();
    }
}
