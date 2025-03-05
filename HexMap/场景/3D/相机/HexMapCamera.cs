using Godot;

public partial class HexMapCamera : Node3D
{
    private static HexMapCamera _instance;

    [Export] private HexGrid _hexGrid;
    [Export] private CameraPhyProcess _phyProcess;
    [Export] public Camera3D Camera;
    public bool IsLocked { get; set; }

    public override void _EnterTree() => _instance = this;

    public void Init()
    {
        _phyProcess.Init(_hexGrid);
    }

    public Vector3 ClampPosition(Vector3 position)
    {
        var xMax = (_hexGrid.CellCountX - 0.5f) * (2f * HexMetrics.InnerRadius);
        position.X = Mathf.Clamp(position.X, 0, xMax);

        var zMax = (_hexGrid.CellCountZ - 1) * (1.5f * HexMetrics.OuterRadius);
        position.Z = Mathf.Clamp(position.Z, 0, zMax);

        return position;
    }

    public static void ValidatePosition()
    {
        _instance.GlobalPosition = _instance.ClampPosition(_instance.GlobalPosition);
    }
}