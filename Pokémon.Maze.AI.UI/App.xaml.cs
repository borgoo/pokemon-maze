using System.IO;
using System.Windows;

namespace Pokémon.Maze.AI.UI;

public partial class App : Application
{
    private static readonly string DefaultQTableRelative = Path.Combine("resources", "qTable.bin");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!TryResolveQTablePath(e.Args, out var qTablePath, out var error))
        {
            MessageBox.Show(error, "Pokémon Maze - AI", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        MainWindow = new MainWindow(qTablePath);
        MainWindow.Show();
    }

    internal static bool TryResolveQTablePath(string[] args, out string qTablePath, out string errorMessage)
    {
        var custom = TryParseQTableSwitch(args);
        if (custom is null)
        {
            qTablePath = Path.Combine(AppContext.BaseDirectory, DefaultQTableRelative);
            if (!File.Exists(qTablePath))
            {
                errorMessage = $"Q-table not found (default): {qTablePath}";
                return false;
            }

            errorMessage = "";
            return true;
        }

        try
        {
            qTablePath = Path.IsPathRooted(custom) ? Path.GetFullPath(custom) : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, custom));
        }
        catch (Exception ex)
        {
            errorMessage = $"Invalid Q-table path: {custom}\n{ex.Message}";
            qTablePath = "";
            return false;
        }

        if (!File.Exists(qTablePath))
        {
            errorMessage = $"Q-table not found: {qTablePath}";
            return false;
        }

        errorMessage = "";
        return true;
    }

    private static string? TryParseQTableSwitch(string[] args)
    {
        const string prefix = "--qtable=";

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a.StartsWith(prefix, StringComparison.Ordinal))
            {
                var v = a[prefix.Length..];
                return string.IsNullOrWhiteSpace(v) ? null : v.Trim('"');
            }

            if (string.Equals(a, "--qtable", StringComparison.Ordinal) && i + 1 < args.Length)
                return args[i + 1].Trim('"');
        }

        return null;
    }
}
