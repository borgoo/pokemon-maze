using System;
using System.Collections.Generic;
using System.Text;

namespace Pokémon.Maze.Core;

public static class MazeLoader
{
    public static ushort[,] GetFromFile(string filePath) { 
        
        if(string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.");
        if(!File.Exists(filePath)) throw new FileNotFoundException("File not found.");
        
        string[] lines = File.ReadAllLines(filePath);
        if(lines.Length == 0) throw new InvalidOperationException("File is empty.");

        int n = lines.Length;
        int m = lines[0].Length;

        if (n <= 0 || m <= 0) throw new InvalidOperationException("Invalid maze dimensions.");

        ushort[,] matrix = new ushort[n, m];

        for (int i = 0; i < n; i++)
            for (int j = 0; j < m; j++)
                matrix[i, j] = (ushort)(lines[i][j] - '0');

        

        return matrix;
        

    }
}
