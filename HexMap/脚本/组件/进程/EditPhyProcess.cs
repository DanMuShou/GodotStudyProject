using System;
using Godot;

public partial class EditPhyProcess : ProcessComponent
{
    private EnableLevelComponent Elevation => _owner.Elevation;
    private EnableLevelComponent Water => _owner.Water;
    private EnableLevelComponent Urban => _owner.Urban;
    private EnableLevelComponent Farm => _owner.Farm;
    private EnableLevelComponent Plant => _owner.Plant;
    private EnableLevelComponent Special => _owner.Special;
    private EnableLevelComponent Brush => _owner.Brush;

    private OptionSelectComponent Texture => _owner.Texture;
    private OptionSelectComponent River => _owner.River;
    private OptionSelectComponent Road => _owner.Road;
    private OptionSelectComponent Wall => _owner.Wall;

    private HexMapEditor _owner;
    private HexGrid _hexGrid;
    private InputManager _input;

    private HexCell _previousCell;
    private HexDirection _dragDirection;
    private bool _isDrag;

    public void Init(HexGrid hexGrid)
    {
        _owner = GetOwner<HexMapEditor>();
        _input = SystemManager.GetManager<InputManager>();
        _hexGrid = hexGrid;

        SetProcessMode(true, true);
    }

    public override void Process(float delta)
    {
        if (!_input.IsMouseOnUi(_owner))
        {
            if (_input.IsClickL)
            {
                SelectCell();
                return;
            }

            if (_input.IsKeyU)
            {
                if (_input.IsShift)
                    DestroyUnit();
                else
                    CreatUnit();
                return;
            }
        }

        _previousCell = null;
    }


    private void SelectCell()
    {
        var currentCell = GetCellUnderCursor();

        if (currentCell != null)
        {
            if (_previousCell != null && _previousCell != currentCell)
                ValidateDrag(currentCell);
            else
                _isDrag = false;

            EditCells(currentCell);
            _previousCell = currentCell;
        }
        else
        {
            _previousCell = null;
        }
    }

    private void CreatUnit()
    {
        var cell = GetCellUnderCursor();
        if (cell is { Unit: null })
        {
            _hexGrid.AddUnit(
                HexUnit.HexUnitPack.Instantiate<HexUnit>(), cell, HexMetrics.Rng.Randf() * Mathf.Pi * 2f);
        }
    }


    private void DestroyUnit()
    {
        var cell = GetCellUnderCursor();
        if (cell is { Unit: not null })
        {
            _hexGrid.RemoveUnit(cell.Unit);
        }
    }

    private void EditCells(HexCell center)
    {
        var centerX = center.Coordinates.X;
        var centerZ = center.Coordinates.Z;

        if (!Brush.Enable)
        {
            EditCell(_hexGrid.GetCell(new HexCoordinates(centerX, centerZ)));
            return;
        }

        for (int r = 0, z = centerZ - Brush.Level; z <= centerZ; z++, r++)
        for (var x = centerX - r; x <= centerX + Brush.Level; x++)
        {
            EditCell(_hexGrid.GetCell(new HexCoordinates(x, z)));
        }

        for (int r = 0, z = centerZ + Brush.Level; z > centerZ; z--, r++)
        for (var x = centerX - Brush.Level; x <= centerX + r; x++)
        {
            EditCell(_hexGrid.GetCell(new HexCoordinates(x, z)));
        }
    }

    private void EditCell(HexCell cell)
    {
        if (cell == null)
            return;


        if (Elevation.Enable)
            cell.Elevation = Elevation.Level;
        if (Water.Enable)
            cell.WaterLevel = Water.Level;

        if (Urban.Enable)
            cell.UrbanLevel = Urban.Level;
        if (Farm.Enable)
            cell.FarmLevel = Farm.Level;
        if (Plant.Enable)
            cell.PlantLevel = Plant.Level;

        if (Special.Enable)
            cell.SpecialIndex = Special.Level;

        if (Texture.ModeIndex != (int)TextureToggle.None)
            cell.TerrainTypeIndex = Texture.ModeIndex - 1;
        if (River.ModeIndex == (int)OptionalToggle.No)
            cell.RemoveRiver();
        if (Road.ModeIndex == (int)OptionalToggle.No)
            cell.RemoveRoads();
        if (Wall.ModeIndex != (int)OptionalToggle.Ignore)
            cell.Walled = Wall.ModeIndex == (int)OptionalToggle.Yes;


        if (_isDrag)
        {
            var otherCell = cell.GetNeighbor(_dragDirection.Opposite());
            if (otherCell == null) return;

            if (River.ModeIndex == (int)OptionalToggle.Yes)
                otherCell.SetOutgoingRiver(_dragDirection);

            if (Road.ModeIndex == (int)OptionalToggle.Yes)
                otherCell.AddRoads(_dragDirection);
        }
    }

    private HexCell GetCellUnderCursor()
    {
        return _hexGrid.GetCellByCamearaRay();
    }

    private void ValidateDrag(HexCell currentCell)
    {
        for (_dragDirection = HexDirection.SW; _dragDirection <= HexDirection.SE; _dragDirection++)
        {
            if (_previousCell.GetNeighbor(_dragDirection) != currentCell)
                continue;

            _isDrag = true;
            return;
        }

        _isDrag = false;
    }

    private string[] GetEnumNames(Type enumType)
    {
        return Enum.GetNames(enumType);
    }
}