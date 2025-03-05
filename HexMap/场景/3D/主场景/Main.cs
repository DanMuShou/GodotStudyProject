using Godot;

public partial class Main : Node3D
{
    [Export] private SystemManager _systemManager;
    [Export] private HexGrid _hexGrid;
    [Export] private HexMapCamera _camera;
    [Export] private Ui _ui;

    public override void _Ready()
    {
        _systemManager.Init();
        _hexGrid.Init();
        _ui.Init();
        _camera.Init();
    }
}