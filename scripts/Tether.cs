using System;
using System.Collections.Generic;
using Godot;

public partial class Tether : Node2D
{
    record struct Pivot(Vector2 Pos, Vector2 Dir);

    List<Vector2> pivots = new();

    public override void _Ready() => pivots.Add(GlobalPosition);

    [Export]
    private float MaxLength;

    [Export]
    public float Width;

    float LengthOffset = 0;

    [Export]
    float TetherSpeed;

    Godot.Collections.Dictionary Cast(Vector2 from, Vector2 to)
    {
        var dir = (to - from).Normalized();
        var q = PhysicsRayQueryParameters2D.Create(from + dir * 0.5f, to);
        q.Exclude = [GameManager.player.GetRid()];
        return GetWorld2D().DirectSpaceState.IntersectRay(q);
    }

    public override void _PhysicsProcess(double delta)
    {
        var player = GameManager.player.GlobalPosition;
        GD.Print(pivots.Count);
        while (pivots.Count > 1 && Cast(pivots[^2], player).Count == 0)
            pivots.RemoveAt(pivots.Count - 1);

        var hit = Cast(pivots[^1], player);
        if (hit.Count > 0)
        {
            var polygon = (CollisionPolygon2D)((StaticBody2D)hit["collider"]).GetChild(0);
            var xf = polygon.GlobalTransform;
            var poly = Array.ConvertAll(polygon.Polygon, v => xf * v);
            var v = SnapVertex(poly, (Vector2)hit["position"], Winding(poly));
            if (v is Vector2 vv && vv != pivots[^1])
                pivots.Add(vv);
        }
        QueueRedraw();
        if (Input.IsActionJustPressed("tether"))
        {
            LengthOffset = (MaxLength - RopeLength());
        }
        if (Input.IsActionPressed("tether"))
        {
            LengthOffset += TetherSpeed * (float)delta;
        }
        if (Input.IsActionJustReleased("tether"))
        {
            LengthOffset = 0;
        }
    }

    public override void _Draw()
    {
        var pts = new Vector2[pivots.Count + 1];
        for (int i = 0; i < pivots.Count; i++)
            pts[i] = ToLocal(pivots[i]);
        pts[^1] = ToLocal(GameManager.player.GlobalPosition);
        DrawPolyline(
            pts,
            Colors.Black,
            RopeLength() > GetMaxLength()
                ? Width / (0.01f * (RopeLength() - GetMaxLength()) + 1)
                : Width
        );
    }

    public float GetMaxLength()
    {
        return Math.Max(0, MaxLength - LengthOffset);
    }

    float Winding(Vector2[] p)
    {
        float area = 0;
        for (int i = 0; i < p.Length; i++)
            area += (p[i].Cross(p[(i + 1) % p.Length]));
        return Mathf.Sign(area);
    }

    float DistToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float lenSq = ab.LengthSquared();
        if (lenSq == 0f)
            return point.DistanceTo(a);
        float t = Mathf.Clamp((point - a).Dot(ab) / lenSq, 0f, 1f);
        return point.DistanceTo(a + ab * t);
    }

    bool IsConvex(Vector2[] p, int i, float winding)
    {
        int n = p.Length;
        var prev = p[(i - 1 + n) % n];
        var next = p[(i + 1) % n];
        return Mathf.Sign((p[i] - prev).Cross(next - p[i])) == winding;
    }

    Vector2? SnapVertex(Vector2[] p, Vector2 hit, float winding)
    {
        int n = p.Length,
            best = 0;
        float bestD = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            float d = DistToSegment(hit, p[i], p[(i + 1) % n]);
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }
        int i0 = best,
            i1 = (best + 1) % n;
        if (hit.DistanceSquaredTo(p[i1]) < hit.DistanceSquaredTo(p[i0]))
            (i0, i1) = (i1, i0);
        if (IsConvex(p, i0, winding))
            return p[i0];
        if (IsConvex(p, i1, winding))
            return p[i1];
        return null;
    }

    public float RopeLength()
    {
        float dist = 0;
        for (int i = 0; i < pivots.Count - 1; i++)
            dist += pivots[i].DistanceTo(pivots[i + 1]);
        return dist + pivots[^1].DistanceTo(GameManager.player.GlobalPosition);
    }

    public Vector2 RopeDir()
    {
        return (GameManager.player.GlobalPosition - pivots[^1]).Normalized();
    }

    public void ResetTether(Vector2 pos)
    {
        GlobalPosition = pos;
        pivots.Clear();
        pivots.Add(GlobalPosition);
        QueueRedraw();
    }
}
