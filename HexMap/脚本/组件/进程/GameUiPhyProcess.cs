using Godot;
using System;

public partial class GameUiPhyProcess : ProcessComponent
{
    private HexGameUi _owner;
    private HexGrid _hexGrid;
    private HexUnit _selectedUnit;
    private InputManager _input;

    public void Init(HexGrid hexGrid)
    {
        _owner = GetOwner<HexGameUi>();
        _hexGrid = hexGrid;
        _input = SystemManager.GetManager<InputManager>();

        SetProcessMode(true, false);
    }

    public override void Process(float delta)
    {
        if (_input.IsClickL && !_input.IsMouseOnUi(_owner))
        {
            DoSelection();
        }
        else if (_selectedUnit != null)
        {
            if (_input.IsClickR)
            {
                DoMove();
            }
            else
            {
                DoPathfinding();
            }
        }
    }

    void DoMove()
    {
        if (_hexGrid.HasPath)
        {
            _selectedUnit.Travel(_hexGrid.GetCellsPath());
            _hexGrid.ClearPath();
        }
    }

    private void DoSelection()
    {
        _hexGrid.ClearPath();
        var cell = GetCurrentCell();
        if (cell?.Unit == null)
        {
            _selectedUnit = null;
            return;
        }

        _selectedUnit = cell.Unit;
    }

    private void DoPathfinding()
    {
        var cell = GetCurrentCell();

        if (cell != null && _selectedUnit.IsValidDestination(cell))
            _hexGrid.FindPath(_selectedUnit.Location, cell, 24);
        else
            _hexGrid.ClearPath();
    }


    private HexCell GetCurrentCell()
    {
        var cell = _hexGrid.GetCellByCamearaRay();
        return cell;
    }
}