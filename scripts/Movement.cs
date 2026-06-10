using System;
using Godot;

public partial class Movement : CharacterBody2D
{
    [Export]
    private float ThrustForce;

    [Export]
    public float GrabRadius { get; private set; } = 100f;

    [Export]
    public float ArmRadius { get; private set; } = 100f;
    private Vector2? grabPos = null;
    private Vector2 initalPos;

    [Export]
    public Node2D ArmPivot;

    [Export]
    public bool HasThrusters = false;

    [Export]
    public float PullStiffness = 1f;

    public override void _Ready()
    {
        Debugger.instance.player = this;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector2 mousePos = GetGlobalMousePosition();
        if (grabPos == null && HasThrusters)
        {
            Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
            Velocity += inputDir * dt * ThrustForce;
        }

        // grabbing:
        // GD.Print(GameManager.instance.MouseColliding);
        Vector2 mouseDir = (mousePos - ArmPivot.GlobalPosition).Normalized();

        Vector2 grabPoint =
            ArmPivot.GlobalPosition.DistanceTo(mousePos) < GrabRadius
                ? mousePos
                : ArmPivot.GlobalPosition + mouseDir * GrabRadius;

        bool onSurface = TryClampToSurface(ArmPivot.GlobalPosition, grabPoint, out Vector2 hit);
        if (onSurface)
        {
            grabPoint = hit;
        }

        Debugger.instance.grabPoint = grabPoint;

        if (Input.IsActionJustPressed("grab") && onSurface)
        {
            grabPos = mousePos;
            initalPos =
                ArmPivot.GlobalPosition.DistanceTo(grabPoint) > ArmRadius
                    ? ArmPivot.GlobalPosition
                        + mouseDir * (ArmPivot.GlobalPosition.DistanceTo(grabPoint) - ArmRadius)
                    : ArmPivot.GlobalPosition;
        }
        if (Input.IsActionJustReleased("grab"))
        {
            grabPos = null;
        }
        if (grabPos != null)
        {
            Vector2 target = initalPos - (mousePos - grabPos.Value);
            if (target.DistanceTo(initalPos) > ArmRadius)
            {
                target = initalPos + (target - initalPos).Normalized() * ArmRadius;
            }
            Velocity = (target - ArmPivot.GlobalPosition) * PullStiffness;
        }

        MoveAndSlide();
    }

    private bool TryClampToSurface(Vector2 from, Vector2 to, out Vector2 hit)
    {
        var space = GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(from, to);
        query.CollideWithBodies = true;
        query.CollideWithAreas = false;

        var result = space.IntersectRay(query);
        if (result.Count > 0)
        {
            hit = (Vector2)result["position"];
            return true;
        }
        hit = to;
        return false;
    }
}
