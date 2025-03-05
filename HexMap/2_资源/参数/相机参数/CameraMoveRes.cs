using Godot;

public partial class CameraMoveRes : Resource
{
    [Export(PropertyHint.Range, "0f,500f")]
    public float MoveSpeedMinZoom { get; private set; } = 40;

    [Export(PropertyHint.Range, "0f,500f")]
    public float MoveSpeedMaxZoom { get; private set; } = 10;

    [Export(PropertyHint.Range, "0f,30f")] public float RotationSpeed { get; private set; } = 10f;

    [Export] public float StickMinZoom { get; private set; } = 25f;
    [Export] public float StickMaxZoom { get; private set; } = 4.5f;

    [Export] public float SwivelMinZoom { get; private set; } = -90;
    [Export] public float SwivelMaxZoom { get; private set; } = -45;
}