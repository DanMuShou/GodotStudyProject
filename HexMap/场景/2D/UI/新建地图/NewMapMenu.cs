using Godot;

public partial class NewMapMenu : Control
{
    [Export] private Button _sBut, _mBut, _lBut, _sMBut, _closeBut;

    private HexGrid _hexGrid;

    public void Init(HexGrid hexGrid)
    {
        _hexGrid = hexGrid;

        _sBut.Pressed += () => CreateMap(20, 15);
        _sMBut.Pressed += () => CreateMap(30, 25);
        _mBut.Pressed += () => CreateMap(40, 30);
        _lBut.Pressed += () => CreateMap(80, 60);

        _closeBut.Pressed += Close;

        Hide();
    }

    private void CreateMap(int x, int z)
    {
        _hexGrid.CreateMap(x, z);
        HexMapCamera.ValidatePosition();
        Close();
    }

    private void Close()
    {
        Hide();
        SystemManager.GetManager<ProcessManager>().SetAllState(
            [typeof(GameUiPhyProcess), typeof(CameraPhyProcess)], true);
    }

    public void Open()
    {
        Show();
        SystemManager.GetManager<ProcessManager>().SetAllState(
            [typeof(GameUiPhyProcess), typeof(CameraPhyProcess)], false);
    }
}