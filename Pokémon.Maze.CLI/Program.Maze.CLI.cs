using System.Text;
using Pokémon.Maze.Core;
using Pokémon.Maze.Core.Enums;

Console.OutputEncoding = Encoding.UTF8;
const ConsoleColor SPRITE_COLOR = ConsoleColor.Yellow;

const int SLOW = 120;
const int NORMAL = 60;
const int FAST = 30;
const int SPEED = SLOW;

static char CellDisplayChar(ushort cell) => cell switch
{
    (ushort)ObjectEnum.Wall => '#',
    (ushort)ObjectEnum.Empty => ' ',
    (ushort)ObjectEnum.Ice => '/',
    (ushort)ObjectEnum.Ladder => 'O',
    (ushort)ObjectEnum.Entrance => 'S',
    (ushort)ObjectEnum.Exit => 'E',
    (ushort)ObjectEnum.JumpDown => '_',
    (ushort)ObjectEnum.JumpLeft => '|',
    _ => '?'
};


if (args.Length != 1) throw new ArgumentException("Path to the grid file is required.\n");
if (string.IsNullOrEmpty(args[0])) throw new ArgumentException("Path to the grid file is required.\n");


ushort[,] matrix = MazeLoader.GetFromFile(args[0]);
(ushort X, ushort Y, char Direction)[] solutionPathHistory = MazeMatrix.Solve(matrix);
if (solutionPathHistory.Length == 0) throw new InvalidOperationException("Solution path solutionPathHistory is empty.");

const int frameDelayMs = SPEED;
const int matrixConsoleRow = 1; // 0 = data header

Console.Clear();
Console.CursorVisible = false;
try
{

    WriteStatusLine(0, solutionPathHistory[0], solutionPathHistory.Length);
    RenderFullMatrix(matrix, matrixConsoleRow, solutionPathHistory[0].X, solutionPathHistory[0].Y, solutionPathHistory[0].Direction);
    Thread.Sleep(frameDelayMs);

    for (int s = 1; s < solutionPathHistory.Length; s++)
    {
        WriteStatusLine(s, solutionPathHistory[s], solutionPathHistory.Length);
        var prev = solutionPathHistory[s - 1];
        var cur = solutionPathHistory[s];
        PatchCell(prev.Y, matrixConsoleRow + prev.X, CellDisplayChar(matrix[prev.X, prev.Y]));
        PatchCellArrow(cur.Y, matrixConsoleRow + cur.X, cur.Direction);
        Thread.Sleep(frameDelayMs);
    }
}
finally
{
    Console.ResetColor();
    Console.CursorVisible = true;
}

static void WriteStatusLine(int stepIndex, (ushort X, ushort Y, char Direction) step, int total)
{
    Console.SetCursorPosition(0, 0);
    string line = $"Step {stepIndex + 1} / {total}   ({step.X}, {step.Y})   direction {step.Direction}";
    int w = Console.WindowWidth > 0 ? Console.WindowWidth : 80;
    if (line.Length < w)
        line = line.PadRight(w - 1);
    Console.Write(line);
}

static void PatchCell(int column, int row, char ch)
{
    Console.SetCursorPosition(column, row);
    Console.Write(ch);
}

static void PatchCellArrow(int column, int row, char direction)
{
    Console.SetCursorPosition(column, row);
    Console.ForegroundColor = SPRITE_COLOR;
    Console.Write(direction);
    Console.ResetColor();
}

static void RenderFullMatrix(ushort[,] m, int topRow, ushort playerX, ushort playerY, char direction)
{
    int r = m.GetLength(0);
    int c = m.GetLength(1);
    for (int i = 0; i < r; i++)
    {
        Console.SetCursorPosition(0, topRow + i);
        for (int j = 0; j < c; j++)
        {
            if (i == playerX && j == playerY)
            {
                Console.ForegroundColor = SPRITE_COLOR;
                Console.Write(direction);
                Console.ResetColor();
            }
            else
                Console.Write(CellDisplayChar(m[i, j]));
        }
    }
}
