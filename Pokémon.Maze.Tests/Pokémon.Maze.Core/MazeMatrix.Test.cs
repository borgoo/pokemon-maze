using Pokémon.Maze.Core;
using Pokémon.Maze.Core.Enums;
using Pokémon.Maze.Core.Exceptions;

namespace Pokémon.Maze.Tests.Pokémon.Maze.Core;

internal class GameEngine_EvaluateHands_Tests
{

    const ushort ENTRANCE_VAL = (ushort)ObjectEnum.Entrance;
    const ushort EXIT_VAL = (ushort)ObjectEnum.Exit;
    const ushort EMPTY_VAL = (ushort)ObjectEnum.Empty;
    const ushort WALL_VAL = (ushort)ObjectEnum.Wall;
    const ushort LADDER_VAL = (ushort)ObjectEnum.Ladder;
    const ushort JUMP_DOWN_VAL = (ushort)ObjectEnum.JumpDown;
    const ushort JUMP_LEFT_VAL = (ushort)ObjectEnum.JumpLeft;
    const ushort ICE_VAL = (ushort)ObjectEnum.Ice;

    [Test]
    public void When_Solve_Simply_Maze_Return_The_Path()
    {
       
        (ushort X, ushort Y, char Direction)[] expected = [
            (4, 1, '^'),
            (3, 1, '^'),
            (2, 1, '^'),
            (1, 1, '^'),
            (1, 2, '>'),
            (1, 3, '>'),
            (2, 3, 'v'),
            (3, 3, 'v'),
            (4, 3, 'v')
        ];


        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

       (ushort X, ushort Y, char Direction)[] history =  BFS.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));


    }

    [Test]
    public void When_Solve_Extremely_Simply_Maze_Return_The_Path()
    {
        const ushort ENTRANCE_VAL = (ushort)ObjectEnum.Entrance;
        const ushort EXIT_VAL = (ushort)ObjectEnum.Exit;
        const ushort EMPTY_VAL = (ushort)ObjectEnum.Empty;
        const ushort WALL_VAL = (ushort)ObjectEnum.Wall;
        (ushort X, ushort Y, char Direction)[] expected = [
            (5, 1, '^'),
            (4, 1, '^'),
            (4, 2, '>'),
            (4, 3, '>'),
            (5, 3, 'v')
        ];


        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

        (ushort X, ushort Y, char Direction)[] history = BFS.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));


    }

    [Test]
    public void When_Solve_JumpDown_Skips_Fast_Down()
    {

        (ushort X, ushort Y, char Direction)[] expected = [
            (4, 1, '^'),
            (3, 1, '^'),
            (2, 1, '^'),
            (1, 1, '^'),
            (1, 2, '>'),
            (1, 3, '>'),
            (3, 3, 'v'),
            (4, 3, 'v')
        ];


        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_DOWN_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

        (ushort X, ushort Y, char Direction)[] history = BFS.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }
    
    [Test]
    public void When_Solve_JumpDown_Can_Not_Be_Approached_From_Left()
    {

        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, JUMP_DOWN_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => BFS.Solve(maze));
    }

    [Test]
    public void When_Solve_JumpDown_Can_Not_Be_Approached_From_Right()
    {
        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, JUMP_DOWN_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EXIT_VAL, WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => BFS.Solve(maze));
    }

    [Test]
    public void When_Solve_JumpDown_Can_Not_Be_Approached_From_Bottom()
    {
        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_DOWN_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EXIT_VAL, WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => BFS.Solve(maze));
    }

    [Test]
    public void When_Solve_JumpLeft_Skips_Fast_Left()
    {

        (ushort X, ushort Y, char Direction)[] expected = [
            (4, 3, '^'),
            (3, 3, '^'),
            (2, 3, '^'),
            (1, 3, '^'),
            (1, 1, '<'),
            (2, 1, 'v'),
            (3, 1, 'v'),
            (4, 1, 'v')
        ];


        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EXIT_VAL, WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL}
        };

        (ushort X, ushort Y, char Direction)[] history = BFS.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }

    [Test]
    public void When_Solve_JumpLeft_Can_Not_Be_Approached_From_Bottom()
    {
        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_LEFT_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EXIT_VAL, WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => BFS.Solve(maze));
    }

    [Test]
    public void When_Solve_JumpLeft_Can_Not_Be_Approached_From_Left()
    {
        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_LEFT_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => BFS.Solve(maze));
    }
    
    [Test]
    public void When_Solve_JumpLeft_Can_Not_Be_Approached_From_Above()
    {
        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, JUMP_LEFT_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EXIT_VAL, WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => BFS.Solve(maze));
    }


    [Test]
    public void When_Solve_Ice_Makes_The_Player_Slide()
    {

        (ushort X, ushort Y, char Direction)[] expected = [
          (4, 1, '^'),
          (3, 1, '^'),
          (2, 1, '^'),
          (1, 1, '^'),
          (1, 2, '>'),
          (1, 3, '>'),
          (1, 4, '>'),
          (2, 4, 'v'),
          (3, 4, 'v'),
          (3, 3, '<'),
          (4, 3, 'v')
        ];


        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_DOWN_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

        (ushort X, ushort Y, char Direction)[] history = BFS.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }
    
    [Test]
    public void When_Solve_Include_Valid_Jumps_After_Ice()
    {

        (ushort X, ushort Y, char Direction)[] expected = [
          (4, 1, '^'),
          (3, 1, '^'),
          (2, 1, '^'),
          (1, 1, '^'),
          (1, 2, '>'),
          (1, 3, '>'),
          (1, 4, '>'),
          (2, 4, 'v'),
          (4, 4, 'v')
        ];


        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_DOWN_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_DOWN_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL, EXIT_VAL, WALL_VAL}
        };

        (ushort X, ushort Y, char Direction)[] history = BFS.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }

    [Test]
    public void When_Solve_Include_Not_Valid_Jumps_After_Ice()
    {
        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_DOWN_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL, EXIT_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => BFS.Solve(maze));
    }
   
    [Test]
    public void When_Solve_Include_Mixed_Jumps_After_Ice()
    {
         (ushort X, ushort Y, char Direction)[] expected = [
            (4, 1, '^'),
            (3, 1, '^'),
            (2, 1, '^'),
            (1, 1, '^'),
            (1, 2, '>'),
            (1, 3, '>'),
            (1, 4, '>'),
            (1, 5, '>'),
            (2, 5, 'v'),
            (2, 3, '<'),
            (3, 3, 'v'),
            (4, 3, 'v') 
        ];

        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };

        (ushort X, ushort Y, char Direction)[] history = BFS.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }

    [Test]
    public void Ladders_Can_Be_Used_As_Teleportation_Points()
    {
         (ushort X, ushort Y, char Direction)[] expected = [
            (4, 1, '^'),
            (3, 1, '^'),
            (3, 3, '^'),
            (4, 3, 'v')
        ];

        ushort[,] maze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, LADDER_VAL, WALL_VAL, LADDER_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };

        (ushort X, ushort Y, char Direction)[] history = BFS.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }

    [Test]
    public void Ladders_Can_Be_Zero() {


        ushort[,] noLadderMaze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };


        Assert.DoesNotThrow(() => BFS.Solve(noLadderMaze));

    }

    [Test]
    public void Ladders_Can_Not_Be_One() {


        ushort[,] oneLadderMaze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, WALL_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, WALL_VAL, WALL_VAL, LADDER_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };


        Assert.Throws<ArgumentException>(() => BFS.Solve(oneLadderMaze));

    }

    [Test]
    public void Ladders_Can_Be_Two()
    {       

        ushort[,] twoLaddersMaze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, WALL_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, LADDER_VAL, WALL_VAL, LADDER_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };


        Assert.DoesNotThrow(() => BFS.Solve(twoLaddersMaze));
    }

    [Test]
    public void Ladders_Can_Not_Be_More_Than_Two()
    {

        ushort[,] twoLaddersMaze = new ushort[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, WALL_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, LADDER_VAL, WALL_VAL},
            {WALL_VAL, LADDER_VAL, WALL_VAL, LADDER_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };


        Assert.Throws<ArgumentException>(() => BFS.Solve(twoLaddersMaze));
    }
}