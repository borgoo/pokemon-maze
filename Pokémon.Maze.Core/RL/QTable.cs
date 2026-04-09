using System.Globalization;
using System.Text;

namespace Pokémon.Maze.Core.RL;

public sealed class QTable(int NumOfPossibleStates, int NumOfPossibleActions) {

    private readonly float[] _table = new float[NumOfPossibleStates * NumOfPossibleActions];
    private int FlatIndex(int statusIndex, int actionIndex) => statusIndex * NumOfPossibleActions + actionIndex;

    public float this[int statusIndex, int actionIndex]
    {
        get => _table[FlatIndex(statusIndex, actionIndex)];
        set => _table[FlatIndex(statusIndex, actionIndex)] = value;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        for (int s = 0; s < NumOfPossibleStates; s++)
        {
            for (int a = 0; a < NumOfPossibleActions; a++)
                sb.Append($"{this[s, a]:F4}\t");
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    public static QTable FromString(string data)
    {
        var rows = data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int numStates = rows.Length;
        int numActions = rows[0].Split('\t', StringSplitOptions.RemoveEmptyEntries).Length;

        QTable table = new(numStates, numActions);
        for (int s = 0; s < numStates; s++)
        {
            var cols = rows[s].Split('\t', StringSplitOptions.RemoveEmptyEntries);
            for (int a = 0; a < numActions; a++)
                table[s, a] = float.Parse(cols[a], CultureInfo.InvariantCulture);
        }
        return table;
    }
}