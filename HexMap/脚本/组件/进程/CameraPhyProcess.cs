using Godot;

public partial class CameraPhyProcess : ProcessComponent
{
    [Export] private CameraMoveRes _cameraMoveRes;
    [Export] private Node3D _swivel;
    [Export] private Node3D _stick;

    private HexGrid _hexGrid;
    private HexMapCamera _owner;
    private InputManager _input;
    private float _zoom = 1.0f;

    public void Init(HexGrid hexGrid)
    {
        _owner = GetOwner<HexMapCamera>();
        _hexGrid = hexGrid;
        _input = SystemManager.GetManager<InputManager>();

        SetProcessMode(true, true);
    }

    public override void Process(float delta)
    {
        AdjustZoom(delta);
        AdjustRotation(delta);
        AdjustPosition(delta);
    }

    private void AdjustZoom(float delta)
    {
        if (!_input.IsZoom)
            return;

        var vec = _input.CameraZoomDir;
        _zoom = Mathf.Clamp(_zoom + vec * delta, 0f, 1f);

        var distance = Mathf.Lerp(_cameraMoveRes.StickMinZoom, _cameraMoveRes.StickMaxZoom, _zoom);
        _stick.Position = new Vector3(0, 0, distance);

        var angle = Mathf.Lerp(_cameraMoveRes.SwivelMinZoom, _cameraMoveRes.SwivelMaxZoom, _zoom);
        _swivel.RotationDegrees = new Vector3(angle, 0f, 0f);
    }

    private void AdjustPosition(float delta = 1)
    {
        if (!_input.IsMove)
            return;

        var vec = _input.CameraMoveDir;
        var direction = new Vector3(vec.X, 0, vec.Y).Normalized().Rotated(Vector3.Up, _owner.Rotation.Y);
        var distance = Mathf.Lerp(_cameraMoveRes.MoveSpeedMinZoom, _cameraMoveRes.MoveSpeedMaxZoom, _zoom) * delta;
        var targetPosition = _owner.ClampPosition(_owner.GlobalPosition + direction * distance);
        _owner.GlobalPosition = targetPosition;
    }

    private void AdjustRotation(float delta)
    {
        if (!_input.IsRotate)
            return;

        var rotaVec = _input.CameraRotateDir;
        var rotationAngleY = rotaVec * _cameraMoveRes.RotationSpeed * delta;
        _owner.RotateY(-rotationAngleY / Mathf.Pi);
    }
}