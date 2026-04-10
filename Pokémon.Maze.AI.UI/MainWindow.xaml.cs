using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Pokémon.Maze.Core;
using Pokémon.Maze.Core.Enums;
using Pokémon.Maze.Core.RL;
using System.Threading.Channels;
using System.IO;

namespace Pokémon.Maze.AI.UI;

public partial class MainWindow : Window
{
    private const int SPRITE_SPEED = 30; // ms per tick (lower = faster)
    private const int WalkSubSteps = 6; // ticks to cover 16px between two tiles (higher = smoother)

    private const string MAZE_FILE = "maze.png";
    private const string MAZE_GRID_FILE = "maze_grid.data";
    private const string SPRITE_FILE = "sprite.png";
    private const string BALL_FILE = "ball.png";
    private const string QTABLE_FILE = "qTable.bin";
    private const int FoundItemMessageDurationMs = 2800;

    private static readonly Color SpriteSheetOrangeBackgroundTreatedAsTransparent = Color.FromRgb(0xFF, 0x7F, 0x27);

    // Matches TrainingField hardcoded positions (top to bottom: tm, pearl, masterball)
    private static readonly IReadOnlyDictionary<ItemType, (int X, int Y)> ItemsPositions = new Dictionary<ItemType, (int X, int Y)>
    {
        { ItemType.TM24, (7, 31) },
        { ItemType.Pearl, (9, 33) },
        { ItemType.Masterball, (23, 32) },
    };

    private BitmapSource? _spriteSheet;
    private IReadOnlyDictionary<ItemType, UIElement>? _itemMarkers;
    private ushort[,]? _mazeTiles;
    private int _rows;
    private int _cols;
    private EpisodeFrame? _lastItemSnapshot;
    private DispatcherTimer? _foundItemHideTimer;
    private DispatcherTimer _timer = null!;
    private ChannelReader<EpisodeFrame>? _frames;
    private int _animPhase;
    private EpisodeFrame? _currentFrame;
    private EpisodeFrame? _nextFrame;
    private int _walkSubStep;

    public MainWindow()
    {
        InitializeComponent();
        _foundItemHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FoundItemMessageDurationMs) };
        _foundItemHideTimer.Tick += (_, _) =>
        {
            _foundItemHideTimer!.Stop();
            FoundItemBanner.Visibility = Visibility.Collapsed;
        };
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var baseDir = AppContext.BaseDirectory;

        var mazePath = Path.Combine(baseDir, "assets", MAZE_FILE);
        var spritePath = Path.Combine(baseDir, "assets", SPRITE_FILE);
        var gridPath = Path.Combine(baseDir, "resouces", MAZE_GRID_FILE);
        var qTablePath = Path.Combine(baseDir, "resouces", QTABLE_FILE);

        ushort[,] matrix = MazeLoader.GetFromFile(gridPath);
        _mazeTiles = matrix;
        _rows = matrix.GetLength(0);
        _cols = matrix.GetLength(1);

        (int X, int Y) entrance = FindRequiredTile(matrix, ObjectEnum.Entrance);
        (int X, int Y) exit = FindRequiredTile(matrix, ObjectEnum.Exit);

        MazeBackground.Source = LoadBitmapFrozen(mazePath);
        _spriteSheet = LoadSpriteSheetWithChromaKey(spritePath);
        var ballPath = System.IO.Path.Combine(baseDir, "assets", BALL_FILE);
        _itemMarkers = BuildItemMarkers(LoadBitmapFrozen(ballPath));

        using var stream = File.OpenRead(qTablePath);
        using var reader = new BinaryReader(stream);
        QTable qTable = QTable.Load(reader);

        var orchestrator = new EpisodeOrchestrator(entrance, exit, matrix, ItemsPositions);
        _frames = orchestrator.StartFrames(qTable);

        _animPhase = 0;
        _currentFrame = null;
        _nextFrame = null;
        _walkSubStep = 0;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SPRITE_SPEED) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    private void Tick()
    {
        if (_frames is null)
        {
            _timer.Stop();
            return;
        }

        // Fill the pipeline: current -> next. We only consume from the channel when we
        // finished animating the previous segment.
        if (_currentFrame is null)
        {
            if (!TryReadFrame(out var first)) return;
            _currentFrame = first;
            ApplyFrameInstant(first);
            return;
        }

        if (_nextFrame is null)
        {
            if (!TryReadFrame(out var nf)) return;
            _nextFrame = nf;
            _walkSubStep = 0;

            // Hidden frames (ladder blink) are rendered immediately and don't interpolate.
            if (nf.Hidden)
            {
                ApplyFrameInstant(nf);
                _currentFrame = nf;
                _nextFrame = null;
                return;
            }

            // Item pickup (same grid cell): apply immediately, no walk interpolation.
            if (_currentFrame.Value.Position == nf.Position)
            {
                ApplyFrameInstant(nf);
                _currentFrame = nf;
                _nextFrame = null;
                return;
            }
        }

        // If next is visible but current is hidden, just snap to next (reappear).
        if (_currentFrame.Value.Hidden)
        {
            ApplyFrameInstant(_nextFrame.Value);
            _currentFrame = _nextFrame.Value;
            _nextFrame = null;
            return;
        }

        // Interpolate current -> next over WalkSubSteps ticks.
        _animPhase++;
        _walkSubStep++;

        var a = _currentFrame.Value;
        var b = _nextFrame.Value;

        var t = Math.Min(1.0, _walkSubStep / (double)WalkSubSteps);
        var left = a.Position.Y * 16.0 + (b.Position.Y - a.Position.Y) * 16.0 * t;
        var top = a.Position.X * 16.0 + (b.Position.X - a.Position.X) * 16.0 * t;

        SpriteImage.Visibility = Visibility.Visible;
        Canvas.SetLeft(SpriteImage, left);
        Canvas.SetTop(SpriteImage, top);

        var dir = DirectionFromGridStep(a, b);
        var walk = !IsIce(a.Position);
        SetSpriteFrame(FrameForDirection(dir, walk, phase: _animPhase));

        if (_walkSubStep < WalkSubSteps) return;


        ApplyFrameInstant(b);
        _currentFrame = b;
        _nextFrame = null;
    }

    private bool TryReadFrame(out EpisodeFrame f)
    {
        if (_frames is null)
        {
            f = default;
            return false;
        }

        if (_frames.TryRead(out f))
            return true;

        // Channel might not have produced yet; also stop only when completed and empty.
        if (_frames.Completion.IsCompleted)
            _timer.Stop();

        return false;
    }

    private void ApplyFrameInstant(EpisodeFrame f)
    {
        MergeItemBlackouts(f);

        if (f.Hidden)
        {
            SpriteImage.Visibility = Visibility.Collapsed;
            return;
        }

        SpriteImage.Visibility = Visibility.Visible;
        Canvas.SetLeft(SpriteImage, f.Position.Y * 16.0);
        Canvas.SetTop(SpriteImage, f.Position.X * 16.0);
        var walk = !IsIce(f.Position);
        SetSpriteFrame(FrameForDirection(f.Direction, walk, phase: _animPhase));
    }

    private IReadOnlyDictionary<ItemType, UIElement> BuildItemMarkers(BitmapSource ballSource)
    {
        ItemIconsCanvas.Children.Clear();
        var dict = new Dictionary<ItemType, UIElement>();

        foreach (var (type, (x, y)) in ItemsPositions)
        {
            var img = new Image
            {
                Width = 16,
                Height = 16,
                Source = ballSource,
                IsHitTestVisible = false
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            Canvas.SetLeft(img, y * 16.0);
            Canvas.SetTop(img, x * 16.0);
            ItemIconsCanvas.Children.Add(img);
            dict[type] = img;
        }

        return dict;
    }

    private void MergeItemBlackouts(EpisodeFrame f)
    {
        if (_itemMarkers is null) return;

        if (_lastItemSnapshot is not { } prev)
        {
            _lastItemSnapshot = f;
            return;
        }

        if (prev.Tm24Available && !f.Tm24Available)
            OnItemCollected(ItemType.TM24);
        if (prev.PearlAvailable && !f.PearlAvailable)
            OnItemCollected(ItemType.Pearl);
        if (prev.MasterballAvailable && !f.MasterballAvailable)
            OnItemCollected(ItemType.Masterball);

        _lastItemSnapshot = f;
    }

    private void OnItemCollected(ItemType type)
    {
        if (_itemMarkers is not null && _itemMarkers.TryGetValue(type, out var marker))
            marker.Visibility = Visibility.Collapsed;

        ShowFoundItemBanner(type);
    }

    private void ShowFoundItemBanner(ItemType type)
    {
        FoundItemText.Inlines.Clear();
        FoundItemText.Inlines.Add(new Run("You have found "));
        FoundItemText.Inlines.Add(new Run(type.ToString()) { FontWeight = FontWeights.Bold });
        FoundItemBanner.Visibility = Visibility.Visible;
        _foundItemHideTimer?.Stop();
        _foundItemHideTimer?.Start();
    }

    private bool IsIce((int X, int Y) p)
        => _mazeTiles is not null
           && (uint)p.X < (uint)_rows
           && (uint)p.Y < (uint)_cols
           && _mazeTiles[p.X, p.Y] == (ushort)ObjectEnum.Ice;

    private static char DirectionFromGridStep(EpisodeFrame from, EpisodeFrame to)
    {
        var dx = to.Position.X - from.Position.X;
        var dy = to.Position.Y - from.Position.Y;
        if (dx == 1 && dy == 0) return 'v';
        if (dx == -1 && dy == 0) return '^';
        if (dx == 0 && dy == 1) return '>';
        if (dx == 0 && dy == -1) return '<';
        return to.Direction;
    }

    private static (int X, int Y) FindRequiredTile(ushort[,] matrix, ObjectEnum tile)
    {
        for (int i = 0; i < matrix.GetLength(0); i++)
        for (int j = 0; j < matrix.GetLength(1); j++)
            if (matrix[i, j] == (ushort)tile)
                return (i, j);

        throw new ArgumentException($"Tile {tile} not found in matrix.");
    }

    private static BitmapImage LoadBitmapFrozen(string path)
    {
        using var stream = System.IO.File.OpenRead(path);
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.StreamSource = stream;
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
}