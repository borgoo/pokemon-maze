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
    const int EXPLORATION_SPEED = 120;
    const int SPRITE_SPEED = 30;

    // how many ticks to cover 16px between two cells in the path (higher = smoother movement)
    const int PathWalkSubSteps = 6;

    // files
    const string MAZE_FILE = "maze.png";
    const string MAZE_GRID_FILE = "maze_grid.data";
    const string SPRITE_FILE = "sprite.png";

    // sprite background color to remove
    private static readonly Color SpriteSheetOrangeBackgroundTreatedAsTransparent = Color.FromRgb(0xFF, 0x7F, 0x27);

    private static readonly Brush TransparentTile = Brushes.Transparent;
    private static readonly Brush _visitedBrush = new SolidColorBrush(Color.FromArgb(0x60, 0x44, 0x52, 0x70));
    private static readonly Brush _frontierBrush = new SolidColorBrush(Color.FromArgb(0xaa, 0xff, 0xc0, 0x40));
    
    // Dark overlay on each grid cell after the sprite has left it
    private static readonly Brush _pathTrailBrush = _visitedBrush;

    private BitmapSource? _spriteSheet;
    private ushort[,]? _mazeTiles;
    private Rectangle[,]? _cells;
    private List<Snapshot> _snapshots = [];
    private int _rows;
    private int _cols;
    private int _currentStep;
    private int _animPhase;
    private bool _inSpritePhase;
    private bool _spriteEntrancePending;
    private int _pathSegmentFrom;
    private int _pathSubStep;
    private bool _ladderTeleportAwaitReappear;
    private readonly DispatcherTimer _timer;

    public MainWindow()
    {
        _visitedBrush.Freeze();
        _frontierBrush.Freeze();
        _pathTrailBrush.Freeze();
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

                _currentStep = 0;
                _animPhase = 0;
                _inSpritePhase = false;
                _spriteEntrancePending = false;
                _pathSegmentFrom = 0;
                _pathSubStep = 0;
                _ladderTeleportAwaitReappear = false;
                ApplyStep(0);
                SyncTimerIntervalToPhase();
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

        ClearOverlayCellsTransparent();

        foreach (var (x, y) in snapshot.Visited)
        {
            if (x >= _rows || y >= _cols) continue;
            _cells[x, y].Fill = snapshot.Frontier.Contains((x, y)) ? _frontierBrush : _visitedBrush;
        }
    }

    private void ClearOverlayCellsTransparent()
    {
        if (_cells is null) return;
        for (var r = 0; r < _rows; r++)
        {
            for (var c = 0; c < _cols; c++)
                _cells[r, c].Fill = TransparentTile;
        }
    }
    private void BeginSpriteOverlayTrail()
    {
        OverlayCanvas.Visibility = Visibility.Visible;
        ClearOverlayCellsTransparent();
    }

    private void DarkenTrailForPathCell(ushort x, ushort y)
    {
        if (_cells is null || x >= _rows || y >= _cols) return;
        _cells[x, y].Fill = _pathTrailBrush;
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

    private (ushort X, ushort Y, char Direction)[]? GetFinalPath()
        => _snapshots.Count == 0 ? null : _snapshots[^1].FinalPath;

    private void ShowPathStep(int pathIndex)
    {
        var path = GetFinalPath();
        if (path is null || path.Length == 0 || pathIndex < 0 || pathIndex >= path.Length)
        {
            SpriteImage.Visibility = Visibility.Collapsed;
            return;
        }

        var (x, y, dir) = path[pathIndex];
        SpriteImage.Visibility = Visibility.Visible;
        Canvas.SetLeft(SpriteImage, y * 16.0);
        Canvas.SetTop(SpriteImage, x * 16.0);

        var onIce = _mazeTiles is not null
            && x < _rows && y < _cols
            && _mazeTiles[x, y] == (ushort)ObjectEnum.Ice;
        var walk = pathIndex < path.Length - 1 && !onIce;
        SetSpriteFrame(FrameForDirection(dir, walk, _animPhase));
    }

    private bool IsLadderTeleportSegment((ushort X, ushort Y, char Direction) a, (ushort X, ushort Y, char Direction) b)
    {
        if (_mazeTiles is null) return false;
        if (a.X >= _rows || a.Y >= _cols || b.X >= _rows || b.Y >= _cols) return false;
        if (_mazeTiles[a.X, a.Y] != (ushort)ObjectEnum.Ladder) return false;
        if (_mazeTiles[b.X, b.Y] != (ushort)ObjectEnum.Ladder) return false;
        var manhattan = Math.Abs((int)b.X - a.X) + Math.Abs((int)b.Y - a.Y);
        return manhattan != 1;
    }

    private static char DirectionFromGridStep((ushort X, ushort Y, char Direction) from, (ushort X, ushort Y, char Direction) to)
    {
        var sdr = (int)to.X - from.X;
        var sdc = (int)to.Y - from.Y;
        if (sdr == 1) return 'v';
        if (sdr == -1) return '^';
        if (sdc == 1) return '>';
        if (sdc == -1) return '<';
        return to.Direction;
    }

    private void ShowPathSegmentInterpolated(int segmentFrom, int subStep)
    {
        var path = GetFinalPath();
        if (path is null || path.Length < 2) return;

        var a = path[segmentFrom];
        var b = path[segmentFrom + 1];
        var t = subStep / (double)PathWalkSubSteps;
        var left = a.Y * 16.0 + ((int)b.Y - a.Y) * 16.0 * t;
        var top = a.X * 16.0 + ((int)b.X - a.X) * 16.0 * t;

        SpriteImage.Visibility = Visibility.Visible;
        Canvas.SetLeft(SpriteImage, left);
        Canvas.SetTop(SpriteImage, top);

        var dir = DirectionFromGridStep(a, b);
        var onIce = _mazeTiles is not null
            && a.X < _rows && a.Y < _cols
            && _mazeTiles[a.X, a.Y] == (ushort)ObjectEnum.Ice;
        var walk = segmentFrom < path.Length - 1 && !onIce;
        SetSpriteFrame(FrameForDirection(dir, walk, _animPhase));
    }

    private void ApplyStep(int step)
    {
        if (_snapshots.Count == 0) return;

        var n = _snapshots.Count;
        if (step < 0 || step >= n) return;

        OverlayCanvas.Visibility = Visibility.Visible;
        ApplyOverlay(_snapshots[step]);
        SpriteImage.Visibility = Visibility.Collapsed;
    }

    private void SyncTimerIntervalToPhase()
    {
        if (_snapshots.Count == 0) return;
        _timer.Interval = TimeSpan.FromMilliseconds(_inSpritePhase ? SPRITE_SPEED : EXPLORATION_SPEED);
    }

    private void TimerTick()
    {
        if (_snapshots.Count == 0) return;

        var n = _snapshots.Count;
        var path = GetFinalPath();

        if (!_inSpritePhase)
        {
            if (_currentStep < n - 1)
            {
                _currentStep++;
                _animPhase++;
                ApplyStep(_currentStep);
            }
            else
            {
                _inSpritePhase = true;
                BeginSpriteOverlayTrail();

                if (path is null || path.Length == 0)
                {
                    _timer.Stop();
                    return;
                }

                if (path.Length == 1)
                {
                    ShowPathStep(0);
                    DarkenTrailForPathCell(path[0].X, path[0].Y);
                    _animPhase++;
                    _timer.Stop();
                    return;
                }

                _spriteEntrancePending = true;
                _pathSegmentFrom = 0;
                _pathSubStep = 0;
                _ladderTeleportAwaitReappear = false;
            }

            SyncTimerIntervalToPhase();
            return;
        }

        _animPhase++;

        if (_spriteEntrancePending)
        {
            ShowPathStep(0);
            _spriteEntrancePending = false;
            SyncTimerIntervalToPhase();
            return;
        }

        if (path is null || path.Length < 2)
        {
            _timer.Stop();
            return;
        }

        var segA = path[_pathSegmentFrom];
        var segB = path[_pathSegmentFrom + 1];
        if (IsLadderTeleportSegment(segA, segB))
        {
            if (!_ladderTeleportAwaitReappear)
            {
                SpriteImage.Visibility = Visibility.Collapsed;
                _ladderTeleportAwaitReappear = true;
            }
            else
            {
                DarkenTrailForPathCell(segA.X, segA.Y);
                _pathSegmentFrom++;
                _pathSubStep = 0;
                _ladderTeleportAwaitReappear = false;
                if (_pathSegmentFrom >= path.Length - 1)
                {
                    var goal = path[path.Length - 1];
                    DarkenTrailForPathCell(goal.X, goal.Y);
                    ShowPathStep(path.Length - 1);
                    _timer.Stop();
                    return;
                }

                ShowPathStep(_pathSegmentFrom);
            }

            SyncTimerIntervalToPhase();
            return;
        }

        _pathSubStep++;
        ShowPathSegmentInterpolated(_pathSegmentFrom, _pathSubStep);

        if (_pathSubStep < PathWalkSubSteps)
        {
            SyncTimerIntervalToPhase();
            return;
        }

        DarkenTrailForPathCell(path[_pathSegmentFrom].X, path[_pathSegmentFrom].Y);
        _pathSubStep = 0;
        _pathSegmentFrom++;
        if (_pathSegmentFrom >= path.Length - 1)
        {
            var goal = path[path.Length - 1];
            DarkenTrailForPathCell(goal.X, goal.Y);
            ShowPathStep(path.Length - 1);
            _timer.Stop();
            return;
        }

        SyncTimerIntervalToPhase();
    }
}
