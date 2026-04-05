using Pokémon.Maze.Core;
using Pokémon.Maze.Core.Enums;
using Pokémon.Maze.Core.Exceptions;

namespace Pokémon.Maze.Tests.Pokémon.Maze.Core;

internal class GameEngine_EvaluateHands_Tests
{

    const short ENTRANCE_VAL = (short)ObjectEnum.Entrance;
    const short EXIT_VAL = (short)ObjectEnum.Exit;
    const short EMPTY_VAL = (short)ObjectEnum.Empty;
    const short WALL_VAL = (short)ObjectEnum.Wall;
    const short LADDER_VAL = (short)ObjectEnum.Ladder;
    const short JUMP_DOWN_VAL = (short)ObjectEnum.JumpDown;
    const short JUMP_LEFT_VAL = (short)ObjectEnum.JumpLeft;
    const short ICE_VAL = (short)ObjectEnum.Ice;

    [Test]
    public void When_Solve_Simply_Maze_Return_The_Path()
    {
       
        (short X, short Y, char Direction)[] expected = [
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


        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

       (short X, short Y, char Direction)[] history =  MazeMatrix.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));


    }

    [Test]
    public void When_Solve_Extremely_Simply_Maze_Return_The_Path()
    {
        const short ENTRANCE_VAL = (short)ObjectEnum.Entrance;
        const short EXIT_VAL = (short)ObjectEnum.Exit;
        const short EMPTY_VAL = (short)ObjectEnum.Empty;
        const short WALL_VAL = (short)ObjectEnum.Wall;
        (short X, short Y, char Direction)[] expected = [
            (5, 1, '^'),
            (4, 1, '^'),
            (4, 2, '>'),
            (4, 3, '>'),
            (5, 3, 'v')
        ];


        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

        (short X, short Y, char Direction)[] history = MazeMatrix.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));


    }

    [Test]
    public void When_Solve_JumpDown_Skips_Fast_Down()
    {

        (short X, short Y, char Direction)[] expected = [
            (4, 1, '^'),
            (3, 1, '^'),
            (2, 1, '^'),
            (1, 1, '^'),
            (1, 2, '>'),
            (1, 3, '>'),
            (3, 3, 'v'),
            (4, 3, 'v')
        ];


        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_DOWN_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

        (short X, short Y, char Direction)[] history = MazeMatrix.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }
    
    [Test]
    public void When_Solve_JumpDown_Can_Not_Be_Approached_From_Left()
    {

        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, JUMP_DOWN_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => MazeMatrix.Solve(maze));
    }

    [Test]
    public void When_Solve_JumpDown_Can_Not_Be_Approached_From_Right()
    {
        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, JUMP_DOWN_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EXIT_VAL, WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => MazeMatrix.Solve(maze));
    }

    [Test]
    public void When_Solve_JumpDown_Can_Not_Be_Approached_From_Bottom()
    {
        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_DOWN_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EXIT_VAL, WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => MazeMatrix.Solve(maze));
    }

    [Test]
    public void When_Solve_JumpLeft_Skips_Fast_Left()
    {

        (short X, short Y, char Direction)[] expected = [
            (4, 3, '^'),
            (3, 3, '^'),
            (2, 3, '^'),
            (1, 3, '^'),
            (1, 1, '<'),
            (2, 1, 'v'),
            (3, 1, 'v'),
            (4, 1, 'v')
        ];


        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EXIT_VAL, WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL}
        };

        (short X, short Y, char Direction)[] history = MazeMatrix.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }

    [Test]
    public void When_Solve_JumpLeft_Can_Not_Be_Approached_From_Bottom()
    {
        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_LEFT_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EXIT_VAL, WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => MazeMatrix.Solve(maze));
    }

    [Test]
    public void When_Solve_JumpLeft_Can_Not_Be_Approached_From_Left()
    {
        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_LEFT_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => MazeMatrix.Solve(maze));
    }
    
    [Test]
    public void When_Solve_JumpLeft_Can_Not_Be_Approached_From_Above()
    {
        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, JUMP_LEFT_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, WALL_VAL},
            {WALL_VAL, EXIT_VAL, WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => MazeMatrix.Solve(maze));
    }


    [Test]
    public void When_Solve_Ice_Makes_The_Player_Slide()
    {

        (short X, short Y, char Direction)[] expected = [
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


        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_DOWN_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL}
        };

        (short X, short Y, char Direction)[] history = MazeMatrix.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }
    
    [Test]
    public void When_Solve_Include_Valid_Jumps_After_Ice()
    {

        (short X, short Y, char Direction)[] expected = [
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


        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_DOWN_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_DOWN_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL, EXIT_VAL, WALL_VAL}
        };

        (short X, short Y, char Direction)[] history = MazeMatrix.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }

    [Test]
    public void When_Solve_Include_Not_Valid_Jumps_After_Ice()
    {
        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, JUMP_DOWN_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, WALL_VAL, EXIT_VAL, WALL_VAL}
        };

        Assert.Throws<UnsolvableMazeException>(() => MazeMatrix.Solve(maze));
    }
   
    [Test]
    public void When_Solve_Include_Mixed_Jumps_After_Ice()
    {
         (short X, short Y, char Direction)[] expected = [
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

        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };

        (short X, short Y, char Direction)[] history = MazeMatrix.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }

    [Test]
    public void Ladders_Can_Be_Used_As_Teleportation_Points()
    {
         (short X, short Y, char Direction)[] expected = [
            (4, 1, '^'),
            (3, 1, '^'),
            (3, 3, '^'),
            (4, 3, 'v')
        ];

        short[,] maze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, LADDER_VAL, WALL_VAL, LADDER_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };

        (short X, short Y, char Direction)[] history = MazeMatrix.Solve(maze);

        Assert.That(history, Is.EquivalentTo(expected));
    }

    [Test]
    public void Ladders_Can_Be_Zero() {


        short[,] noLadderMaze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, EMPTY_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };


        Assert.DoesNotThrow(() => MazeMatrix.Solve(noLadderMaze));

    }

    [Test]
    public void Ladders_Can_Not_Be_One() {


        short[,] oneLadderMaze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, WALL_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, WALL_VAL, WALL_VAL, LADDER_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };


        Assert.Throws<ArgumentException>(() => MazeMatrix.Solve(oneLadderMaze));

    }

    [Test]
    public void Ladders_Can_Be_Two()
    {       

        short[,] twoLaddersMaze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, WALL_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, LADDER_VAL, WALL_VAL, LADDER_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };


        Assert.DoesNotThrow(() => MazeMatrix.Solve(twoLaddersMaze));
    }

    [Test]
    public void Ladders_Can_Not_Be_More_Than_Two()
    {

        short[,] twoLaddersMaze = new short[,] {
            {WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL, WALL_VAL},
            {WALL_VAL, WALL_VAL, ICE_VAL, ICE_VAL, ICE_VAL, ICE_VAL, WALL_VAL},
            {WALL_VAL, EMPTY_VAL, WALL_VAL, EMPTY_VAL, JUMP_LEFT_VAL, LADDER_VAL, WALL_VAL},
            {WALL_VAL, LADDER_VAL, WALL_VAL, LADDER_VAL, EMPTY_VAL, JUMP_LEFT_VAL, WALL_VAL},
            {WALL_VAL, ENTRANCE_VAL, WALL_VAL, EXIT_VAL, WALL_VAL, WALL_VAL, WALL_VAL}
        };


        Assert.Throws<ArgumentException>(() => MazeMatrix.Solve(twoLaddersMaze));
    }
}