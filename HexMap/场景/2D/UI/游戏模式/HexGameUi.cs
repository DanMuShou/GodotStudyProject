using Godot;
using System;

public partial class HexGameUi : Control
{
    [Export] private GameUiPhyProcess _gamePhyProcess;

    private HexGrid _hexGrid;

    public void Init(HexGrid hexGrid)
    {
        _hexGrid = hexGrid;
        _gamePhyProcess.Init(hexGrid);

        Hide();
    }

    public void SetEditMode(bool toggle)
    {
        Visible = !toggle;
        _hexGrid.ShowUi(!toggle);
        _hexGrid.ClearPath();
        _gamePhyProcess.ProcessEnable = !toggle;
    }
}