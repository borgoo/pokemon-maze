namespace Pokémon.Maze.Core.RL;

public readonly record struct QStatus(
    int X, 
    int Y, 
    bool TM24PickedUp,
    bool MasterballPickedUp,
    bool PearlPickedUp
){
    private const int NUM_OF_BOOLEANS = 3; // TM24, Masterball, Pearl

    public int ToIndex(int gridRows, int gridCols)
    {
        int index = (X * gridCols) + Y;
        int layerSize = gridRows * gridCols;

        if (TM24PickedUp)       index += layerSize;    
        if (MasterballPickedUp) index += layerSize * 2; 
        if (PearlPickedUp)      index += layerSize * 4;

        return index;
    }

    public static int GetStatusCardinality(int matrixRows, int matrixColumns)
    {
        return matrixRows * matrixColumns * (int)Math.Pow(2, NUM_OF_BOOLEANS);
    }
}