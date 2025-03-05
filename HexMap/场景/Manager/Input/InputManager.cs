using Godot;

public partial class InputManager : Node, IManager
{
    [Export] private InputProcess _inputProcess;

    public bool IsZoom => CameraZoomDir != 0f;
    public bool IsRotate => CameraRotateDir != 0f;
    public bool IsMove => CameraMoveDir != Vector2.Zero;

    public bool IsClickL => _inputProcess.IsClickL;
    public bool IsClickR => _inputProcess.IsClickR;
    public bool IsShift => _inputProcess.IsShift;
    public bool IsKeyU => _inputProcess.IsKeyU;

    public float CameraZoomDir => _inputProcess.CameraZoomDir;
    public float CameraRotateDir => _inputProcess.CameraRotateDir;
    public Vector2 CameraMoveDir => _inputProcess.CameraMoveDir;

    public bool IsLocked { get; set; }

    public void Init()
    {
        _inputProcess.Init();
    }

    public bool IsMouseOnUi(Control ui)
        => ui.GetGlobalRect().HasPoint(GetViewport().GetMousePosition());

    private float GetMouseScroll()
    {
        if (Input.IsActionJustReleased(InputInformation.MouseScrollWheelUp))
            return 1f;
        if (Input.IsActionJustReleased(InputInformation.MouseScrollWheelDown))
            return -1f;
        return 0f;
    }
}