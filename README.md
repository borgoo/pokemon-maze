# Pokémon.Maze

Solving Pokémon Maze Puzzles with .NET BFS implementation or Q-Learning Agent.

## Solution layout

Solution file: `Pokémon.Maze.slnx`.

| Project | Role |
|--------|------|
| `Pokémon.Maze.Core` | Maze loading, BFS solver, block rules, RL primitives |
| `Pokémon.Maze.Image` | Image / matrix helpers |
| `Pokémon.Maze.Matrix.Generator` | CLI: build maze matrix data from an image |
| `Pokémon.Maze.Overlay.Generator` | CLI: draw an overlay on the maze image from the matrix |
| `Pokémon.Maze.UI` | WPF: manual exploration + sprite playback |
| `Pokémon.Maze.AI.UI` | WPF: autoplay using a trained Q-table (`resources/qTable.bin` or `--qtable`) |
| `Pokémon.Maze.TrainingField` | Console: Q-learning training; writes `qTable.bin` (and optional intermediate saves) |
| `Pokémon.Maze.CLI` | Console playback of the BFS solution |
| `Pokémon.Maze.Tests` | Unit tests (NUnit) |