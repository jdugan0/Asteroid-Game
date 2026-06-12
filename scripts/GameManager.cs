using System;
using Godot;

public partial class GameManager : Node
{
    public static GameManager instance;
    public static Movement player;

    public override void _Ready()
    {
        instance = this;
    }

    public bool MouseColliding { get; private set; }
    public bool MouseEnter;
    public bool MouseExit;

    public override void _Process(double delta)
    {
        if (MouseEnter)
        {
            MouseColliding = true;
        }
        if (MouseExit && !MouseEnter)
        {
            MouseColliding = false;
        }
    }
}
