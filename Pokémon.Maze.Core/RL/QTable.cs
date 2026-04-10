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
            {
                sb.Append(this[s, a].ToString("G9", CultureInfo.InvariantCulture));
                if (a < NumOfPossibleActions - 1) sb.Append('\t');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public void Save(BinaryWriter writer)
    {
        // header
        writer.Write(NumOfPossibleStates);
        writer.Write(NumOfPossibleActions);

        for (int i = 0; i < NumOfPossibleStates; i++)
        {
            for (int j = 0; j < NumOfPossibleActions; j++)
            {
                writer.Write(this[i, j]); // 4 bytes for the float
            }
        }
    }

    public static QTable Load(BinaryReader reader)
    {
        int numOfPossibleStates = reader.ReadInt32();
        int numOfPossibleActions = reader.ReadInt32();

        QTable qTable = new(numOfPossibleStates, numOfPossibleActions);

        for (int i = 0; i < numOfPossibleStates; i++)
        {
            for (int j = 0; j < numOfPossibleActions; j++)
            {
                qTable[i, j] = reader.ReadSingle(); // Read 4 bytes
            }
        }

        return qTable;
    }
}