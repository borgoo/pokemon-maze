# Pokémon.Maze

Solving Pokémon Maze Puzzles with .NET BFS implementation.

## Solution layout

| Project | Role |
|--------|------|
| `Pokémon.Maze.Core` | Maze loading, BFS solver, block rules |
| `Pokémon.Maze.UI` | WPF exploration + sprite playback |
| `Pokémon.Maze.CLI` | Console playback of the solution |
| `Pokémon.Maze.Tests` | Unit tests (NUnit) |
| `Pokémon.Maze.Image` | Image/Matrix logics |
| `Pokémon.Maze.Matrix.Generator` | Utility to produce maze matrix data from image |
| `Pokémon.Maze.Overlay.Generator` | Utility to produce an overlay on the maze image using the matrix |