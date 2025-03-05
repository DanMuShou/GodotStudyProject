using Godot;
using System.Collections.Generic;
using System.IO;

public partial class HexUnit : Node3D
{
    public HexGrid HexGrid { get; set; }
    public static PackedScene HexUnitPack;
    private const float TravelSpeed = 0.5f;
    private const float RotationSpeed = 180f;
    private const int VisionRange = 3;

    private Tween _moveTween;


    public HexCell Location
    {
        get => _location;
        set
        {
            if (_location is not null)
            {
                HexGrid.DecreaseVisibility(_location, VisionRange);
                _location.Unit = null;
            }

            _location = value;
            value.Unit = this;
            HexGrid.IncreaseVisibility(value, VisionRange);
            GlobalPosition = _location.GlobalPosition;
        }
    }

    private HexCell _location;

    public float Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;
            RotateY(value);
        }
    }

    private float _orientation;


    public void Travel(List<HexCell> path)
    {
        _moveTween?.Kill();
        _moveTween = CreateTween();

        _location.Unit = null;
        _location = path[^1];
        _location.Unit = this;

        Vector3 a;
        Vector3 b;
        var c = path[0].GlobalPosition;

        for (var i = 1; i < path.Count; i++)
        {
            a = c;
            b = path[i - 1].GlobalPosition;
            c = (b + path[i].GlobalPosition) * 0.5f;


            var i1 = i;
            var a1 = a;
            var b1 = b;
            var c1 = c;

            _moveTween.TweenMethod(
                Callable.From<float>(
                    (t) =>
                    {
                        GlobalPosition = Bezier.GetPoint(a1, b1, c1, t);
                        if (t >= 1f)
                        {
                            HexGrid.Call(HexGrid.MethodName.DecreaseVisibility, path[i1 - 1], VisionRange);
                            HexGrid.Call(HexGrid.MethodName.IncreaseVisibility, path[i1], VisionRange);
                        }
                    }), 0f, 1f, TravelSpeed);
        }

        a = c;
        b = _location.GlobalPosition;
        c = b;

        _moveTween.TweenMethod(
            Callable.From<float>(
                (t) =>
                {
                    GlobalPosition = Bezier.GetPoint(a, b, c, t);
                    if (t >= 1f)
                    {
                        HexGrid.DecreaseVisibility(path[^2], VisionRange);
                        HexGrid.IncreaseVisibility(_location, VisionRange);
                    }
                }), 0f, 1f, TravelSpeed);
    }

    public void ValidateLocation()
    {
        GlobalPosition = Location.GlobalPosition;
    }

    public void Die()
    {
        if (_location is not null)
        {
            HexGrid.DecreaseVisibility(_location, VisionRange);
        }

        Location.Unit = null;
        QueueFree();
    }

    public void Save(BinaryWriter writer)
    {
        _location.Coordinates.Save(writer);
        writer.Write(Orientation);
    }

    public static void Load(BinaryReader reader, HexGrid _grid)
    {
        var coordinates = HexCoordinates.Load(reader);
        var orientation = reader.ReadSingle();
        _grid.AddUnit(
            HexUnitPack.Instantiate<HexUnit>(), _grid.GetCell(coordinates), orientation);
    }

    public bool IsValidDestination(HexCell cell)
    {
        return !cell.IsUnderwater && cell.Unit == null;
    }
}