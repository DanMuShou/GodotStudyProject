using Godot;

public partial class InputProcess : ProcessComponent
{
    public bool IsZoom => CameraZoomDir != 0f;
    public bool IsRotate => CameraRotateDir != 0f;
    public bool IsMove => CameraMoveDir != Vector2.Zero;

    public bool IsClickL { get; private set; }
    public bool IsClickR { get; private set; }
    public bool IsShift { get; private set; }
    public bool IsKeyU { get; private set; }

    public float CameraZoomDir { get; private set; }
    public float CameraRotateDir { get; private set; }
    public Vector2 CameraMoveDir { get; private set; }

    public void Init()
    {
        SetProcessMode(false, true);
    }

    public override void Process(float delta)
    {
        CameraZoomDir = Input.GetAxis(
            InputInformation.MapZoomUp, InputInformation.MapZoomDown);
        CameraRotateDir = Input.GetAxis(
            InputInformation.MapRotateRight, InputInformation.MapRotateLeft);
        CameraMoveDir = Input.GetVector(
            InputInformation.MapMoveLeft, InputInformation.MapMoveRight,
            InputInformation.MapMoveUp, InputInformation.MapMoveDown);

        IsClickL = Input.IsMouseButtonPressed(MouseButton.Left);
        IsClickR = Input.IsMouseButtonPressed(MouseButton.Right);
        IsShift = Input.IsKeyPressed(Key.Shift);
        IsKeyU = Input.IsKeyPressed(Key.U);
    }
}