using System.Collections.Generic;
using System.IO;
using Godot;

public partial class HexGrid : Node3D
{
    [Signal]
    public delegate void OnGridMapCreatOrLoadFinishedEventHandler();

    [Export(PropertyHint.Range, "0,0,or_greater")]
    private ulong _seed;

    [Export] private bool _useRandom = false;
    [Export] public int CellCountX = 20, CellCountZ = 15;

    [Export] private Color[] _colors;

    [Export] private HexMapCamera _camera;
    [Export] private NoiseGenerator _noiseGenerator;
    [Export] private HexCellShaderData _cellShaderData;

    [Export] private PackedScene _cellPacked;
    [Export] private PackedScene _chunkPacked;
    [Export] private PackedScene _unitPrefab;


    private HexGridChunk[] _chunks;
    private HexCell[] _cells;
    private readonly List<HexUnit> _units = [];

    private HexCellPriorityQueue _searchFrontier;
    private int _searchFrontierPhase;
    private HexCell _currentPathFrom, _currentPathTo;
    private bool _currentPathExists;

    private int _chunkCountX, _chunkCountZ;
    public bool HasPath => _currentPathExists;


    public void Init()
    {
        HexMetrics.InitializeHashGrid(_seed);

        _noiseGenerator.CreateNoiseImage(
            _useRandom ? HexMetrics.Rng.Randi() : 1, new Vector2I(512, 512),
            out HexMetrics.NoiseSource);

        HexMetrics.Colors = _colors;

        HexUnit.HexUnitPack = _unitPrefab;

        _cellShaderData.Init(CellCountX, CellCountZ);
        CreateMap(CellCountX, CellCountZ);
    }

    private void CreateChunks()
    {
        _chunks = new HexGridChunk[_chunkCountX * _chunkCountZ];

        for (int z = 0, index = 0; z < _chunkCountZ; z++)
        for (var x = 0; x < _chunkCountX; x++)
        {
            var chunk = _chunkPacked.Instantiate<HexGridChunk>();
            _chunks[index++] = chunk;
            AddChild(chunk);
            chunk.Init();
        }
    }

    private void CreateCells()
    {
        _cells = new HexCell[CellCountX * CellCountZ];
        for (int z = 0, index = 0; z < CellCountZ; z++)
        for (var x = 0; x < CellCountX; x++)
        {
            CreateCell(x, z, index);
            index++;
        }
    }

    private void CreateCell(int x, int z, int index)
    {
        Vector3 position;
        //计算HexCell的位置 z * 0.5f - z / 2 运用int float的除法运算
        position.X = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
        position.Y = 0f;
        position.Z = z * (HexMetrics.OuterRadius * 1.5f);

        var cell = _cells[index] = _cellPacked.Instantiate<HexCell>();
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.ShaderData = _cellShaderData;
        cell.Index = index;

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, _cells[index - 1]);
        }

        if (z > 0)
        {
            if (z % 2 == 0)
            {
                cell.SetNeighbor(HexDirection.NE, _cells[index - CellCountX]);
                if (x > 0)
                    cell.SetNeighbor(HexDirection.NW, _cells[index - CellCountX - 1]);
            }
            else
            {
                cell.SetNeighbor(HexDirection.NW, _cells[index - CellCountX]);
                if (x < CellCountX - 1)
                    cell.SetNeighbor(HexDirection.NE, _cells[index - CellCountX + 1]);
            }
        }

        AddCellToChunk(x, z, cell, position);
    }

    private void AddCellToChunk(int x, int z, HexCell cell, Vector3 position)
    {
        var chunkX = x / HexMetrics.ChunkSizeX;
        var chunkZ = z / HexMetrics.ChunkSizeZ;
        var chunk = _chunks[chunkX + chunkZ * _chunkCountX];

        var localX = x - chunkX * HexMetrics.ChunkSizeX;
        var localZ = z - chunkZ * HexMetrics.ChunkSizeZ;

        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);
        cell.Chunk = chunk;
        cell.GlobalPosition = position;
        cell.Elevation = 0;
        cell.DisableHighlight();
        cell.SetLabel(null);
    }


    private bool Search(HexCell fromCell, HexCell toCell, int speed)
    {
        _searchFrontierPhase += 2;

        if (_searchFrontier == null)
            _searchFrontier = new HexCellPriorityQueue();
        else
            _searchFrontier.Clear();

        fromCell.SearchPhase = _searchFrontierPhase;
        fromCell.Distance = 0;
        _searchFrontier.Enqueue(fromCell);

        while (_searchFrontier.Count > 0)
        {
            var current = _searchFrontier.Dequeue();
            current.SearchPhase += 1;

            if (current == toCell)
            {
                return true;
            }

            for (var dir = HexDirection.SW; dir <= HexDirection.SE; dir++)
            {
                var neighbor = current.GetNeighbor(dir);
                if (neighbor == null || neighbor.SearchPhase > _searchFrontierPhase) continue;
                if (neighbor.IsUnderwater || neighbor.Unit is not null) continue;

                var edgeType = current.GetEdgeType(neighbor);

                if (edgeType == HexEdgeType.Cliff) continue;

                var currentTurn = (current.Distance - 1) / speed;
                var moveCost = 0;

                if (current.HasRoadThroughEdge(dir))
                {
                    moveCost += 1;
                }
                else if (current.Walled != neighbor.Walled)
                {
                    continue;
                }
                else
                {
                    moveCost += edgeType == HexEdgeType.Flat ? 5 : 10;
                    moveCost += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                }

                var distance = current.Distance + moveCost;
                var turn = (distance - 1) / speed;

                if (turn > currentTurn)
                {
                    distance = turn * speed + moveCost;
                }

                if (neighbor.SearchPhase < _searchFrontierPhase)
                {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.Coordinates.DistanceTo(toCell.Coordinates);
                    _searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    var oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    _searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        return false;
    }

    private List<HexCell> GetVisibleCells(HexCell fromCell, int range)
    {
        List<HexCell> visibleCells = [];

        _searchFrontierPhase += 2;

        if (_searchFrontier == null)
            _searchFrontier = new HexCellPriorityQueue();
        else
            _searchFrontier.Clear();

        fromCell.SearchPhase = _searchFrontierPhase;
        fromCell.Distance = 0;
        _searchFrontier.Enqueue(fromCell);

        while (_searchFrontier.Count > 0)
        {
            var current = _searchFrontier.Dequeue();
            current.SearchPhase += 1;

            visibleCells.Add(current);

            for (var dir = HexDirection.SW; dir <= HexDirection.SE; dir++)
            {
                var neighbor = current.GetNeighbor(dir);
                if (neighbor == null || neighbor.SearchPhase > _searchFrontierPhase) continue;
                if (neighbor.IsUnderwater || neighbor.Unit is not null) continue;

                var edgeType = current.GetEdgeType(neighbor);

                if (edgeType == HexEdgeType.Cliff) continue;

                var distance = current.Distance + 1;

                if (distance > range) continue;

                if (neighbor.SearchPhase < _searchFrontierPhase)
                {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    _searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    var oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    _searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        return visibleCells;
    }

    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        var cells = GetVisibleCells(fromCell, range);
        foreach (var t in cells)
        {
            t.IncreaseVisibility();
        }

        ListPool<HexCell>.Add(cells);
    }

    public void DecreaseVisibility(HexCell fromCell, int range)
    {
        var cells = GetVisibleCells(fromCell, range);
        foreach (var t in cells)
        {
            t.DecreaseVisibility();
        }

        ListPool<HexCell>.Add(cells);
    }

    private void ShowPath(int speed)
    {
        if (_currentPathExists)
        {
            var current = _currentPathTo;
            while (current != _currentPathFrom)
            {
                var turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Colors.White);
                current = current.PathFrom;
            }
        }

        _currentPathFrom.EnableHighlight(Colors.Blue);
        _currentPathTo.EnableHighlight(Colors.Red);
    }

    public void ClearPath()
    {
        if (_currentPathExists)
        {
            var current = _currentPathTo;
            while (current != _currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }

            current.DisableHighlight();
            _currentPathExists = false;
        }
        else if (_currentPathFrom != null)
        {
            _currentPathFrom.DisableHighlight();
            _currentPathTo.DisableHighlight();
        }

        _currentPathFrom = _currentPathTo = null;
    }

    private void ClearUnits()
    {
        foreach (var unit in _units)
        {
            unit.Die();
        }

        _units.Clear();
    }

    public HexCell GetCell(Vector3 position)
    {
        var coordinates = HexCoordinates.FromPosition(position);
        var index = coordinates.X + coordinates.Z * CellCountX + coordinates.Z / 2;
        return _cells[index];
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        var z = coordinates.Z;
        if (z < 0 || z >= CellCountZ)
            return null;

        var x = coordinates.X + z / 2;
        if (x < 0 || x >= CellCountX)
            return null;

        return _cells[x + z * CellCountX];
    }

    public HexCell GetCellByCamearaRay()
    {
        var from = _camera.Camera.ProjectRayOrigin(GetViewport().GetMousePosition());
        var to = from + _camera.Camera.ProjectRayNormal(GetViewport().GetMousePosition()) * 400.0f;

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = true;

        var spaceState = GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(query);

        if (result.Count != 0 && result.TryGetValue("position", out var position))
        {
            return GetCell((Vector3)position);
        }

        return null;
    }

    public void FindPath(HexCell fromCell, HexCell toCell, int speed)
    {
        ClearPath();
        _currentPathFrom = fromCell;
        _currentPathTo = toCell;
        _currentPathExists = Search(fromCell, toCell, speed);
        ShowPath(speed);
    }

    public List<HexCell> GetCellsPath()
    {
        if (!_currentPathExists)
            return null;
        var path = ListPool<HexCell>.Get();

        for (var c = _currentPathTo; c != _currentPathFrom; c = c.PathFrom)
        {
            path.Add(c);
        }

        path.Add(_currentPathFrom);
        path.Reverse();

        return path;
    }

    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        _units.Add(unit);
        AddChild(unit);
        unit.HexGrid = this;
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit(HexUnit unit)
    {
        _units.Remove(unit);
        unit.QueueFree();
    }

    public void ShowUi(bool visible)
    {
        foreach (var cell in _cells)
            cell.ShowUi(visible);
    }

    public bool CreateMap(int x, int z)
    {
        if (x <= 0 || x % HexMetrics.ChunkSizeX != 0 ||
            z <= 0 || z % HexMetrics.ChunkSizeZ != 0)
        {
            GD.PrintErr("Invalid map size");
            return false;
        }

        ClearPath();
        ClearUnits();

        if (_chunks != null)
            foreach (var chunk in _chunks)
                chunk.QueueFree();

        CellCountX = x;
        CellCountZ = z;

        _chunkCountX = CellCountX / HexMetrics.ChunkSizeX;
        _chunkCountZ = CellCountZ / HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();

        EmitSignalOnGridMapCreatOrLoadFinished();
        return true;
    }


    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)CellCountX);
        writer.Write((byte)CellCountZ);

        foreach (var cell in _cells)
            cell.Save(writer);

        writer.Write(_units.Count);
        foreach (var unit in _units)
            unit.Save(writer);
    }

    public void Load(BinaryReader reader, int header)
    {
        ClearPath();
        ClearUnits();

        var x = 20;
        var z = 15;
        if (header >= 1)
        {
            x = reader.ReadByte();
            z = reader.ReadByte();
        }

        if (x != CellCountX || z != CellCountZ)
        {
            if (!CreateMap(x, z))
                return;
        }

        foreach (var cell in _cells)
            cell.Load(reader);

        foreach (var chunk in _chunks)
            chunk.Refresh();

        if (header >= 2)
        {
            var unitCount = reader.ReadInt32();
            for (var i = 0; i < unitCount; i++)
            {
                HexUnit.Load(reader, this);
            }
        }

        EmitSignalOnGridMapCreatOrLoadFinished();
    }
}