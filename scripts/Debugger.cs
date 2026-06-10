using System;
using System.Diagnostics;
using Godot;

public partial class Debugger : Node2D
{
    [Export]
    private bool Debug;
    public Movement player;
    public static Debugger instance;

    public Vector2 grabPoint;

    public override void _Ready()
    {
        instance = this;
    }

    public override void _Draw()
    {
        if (player != null)
        {
            DrawArc(
                player.ArmPivot.GlobalPosition,
                player.GrabRadius,
                0,
                Mathf.Tau,
                64,
                Colors.Yellow,
                2.0f
            );
            DrawArc(
                player.ArmPivot.GlobalPosition,
                player.ArmRadius,
                0,
                Mathf.Tau,
                64,
                Colors.Blue,
                2.0f
            );
            DrawArc(grabPoint, 20, 0, Mathf.Tau, 64, Colors.Red, 2.0f);
        }
    }

    public override void _Process(double delta)
    {
        if (Debug)
        {
            QueueRedraw();
        }
    }
}
