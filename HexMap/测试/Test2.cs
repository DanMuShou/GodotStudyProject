using Godot;
using System;

[Tool]
public partial class Test2 : Node3D
{
    [Export] private Path3D a;

    public override void _Ready()
    {
        Vector3[] point = [new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0), new(0, 0, 0)];

        var curve = new Curve3D();
        for (var i = 0; i < point.Length; i++)
        {
            curve.AddPoint(point[i], null, null, i);
        }

        a.Curve = curve;
    }
}