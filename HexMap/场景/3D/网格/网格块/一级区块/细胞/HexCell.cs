using System.IO;
using System.Linq;
using Godot;

public partial class HexCell : Node3D
{
    [Export] private Label3D _distanceLab;
    [Export] private Sprite3D _highlightSpr;
    [Export] private ShaderMaterial _highlightMat;

    public int Elevation
    {
        get => _elevation;
        set
        {
            if (_elevation == value)
                return;
            _elevation = value;

            var position = GlobalPosition;
            position.Y = _elevation * HexMetrics.ElevationStep +
                         (HexMetrics.SampleNoise(position).Y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
            GlobalPosition = position;

            ValidateRivers();
            for (var i = 0; i < _roads.Length; i++)
            {
                if (_roads[i] && GetElevationDifference((HexDirection)i) > 1f)
                    SetRoad(i, false);
            }

            Refresh();
        }
    }

    private int _elevation = int.MinValue;

    public int TerrainTypeIndex
    {
        get => _terrainTypeIndex;
        set
        {
            if (_terrainTypeIndex == value)
                return;

            _terrainTypeIndex = value;

            ShaderData.RefreshTerrain(this);
        }
    }

    private int _terrainTypeIndex;

    public int WaterLevel
    {
        get => _waterLevel;
        set
        {
            if (value == _waterLevel)
                return;

            _waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }

    private int _waterLevel;

    public int UrbanLevel
    {
        get => _urbanLevel;
        set
        {
            if (_urbanLevel == value)
                return;
            _urbanLevel = value;
            RefreshSelfOnly();
        }
    }

    private int _urbanLevel;

    public int FarmLevel
    {
        get => _farmLevel;
        set
        {
            if (_farmLevel == value)
                return;
            _farmLevel = value;
            RefreshSelfOnly();
        }
    }

    private int _farmLevel;

    public int PlantLevel
    {
        get => _plantLevel;
        set
        {
            if (_plantLevel == value)
                return;
            _plantLevel = value;
            RefreshSelfOnly();
        }
    }

    private int _plantLevel;

    public int SpecialIndex
    {
        get => _specialIndex;
        set
        {
            if (_specialIndex == value || HasRiver)
                return;
            _specialIndex = value;
            RemoveRoads();
            RefreshSelfOnly();
        }
    }

    private int _specialIndex;

    public bool Walled
    {
        get => _walled;
        set
        {
            if (_walled == value)
                return;
            _walled = value;
            Refresh();
        }
    }

    private bool _walled;


    public bool HasRoads => _roads.Any(r => r);
    public bool HasIncomingRiver => _hasIncomingRiver;
    public bool HasOutgoingRiver => _hasOutgoingRiver;

    public HexDirection IncomingRiver => _incomingRiver;
    public HexDirection OutgoingRiver => _outgoingRiver;

    public bool HasRiver => _hasIncomingRiver || _hasOutgoingRiver;
    public bool HasRiverBeginOrEnd => _hasIncomingRiver != _hasOutgoingRiver;

    public HexDirection RiverBeginOrEndDirection => _hasIncomingRiver ? _incomingRiver : _outgoingRiver;

    private bool _hasIncomingRiver,
        _hasOutgoingRiver;

    private HexDirection _incomingRiver,
        _outgoingRiver;

    public float StreamBedY => (_elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;
    public float RiverSurfaceY => (_elevation + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;
    public float WaterSurfaceY => (_waterLevel + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;
    public bool IsUnderwater => _waterLevel > _elevation;
    public bool IsSpecial => _specialIndex > 0;

    public int Distance
    {
        get => _distance;
        set => _distance = value;
    }

    private int _distance;
    public bool IsCellVisible => _visibility > 0;

    private int _visibility;
    public HexCell PathFrom { get; set; }
    public HexCell NextWithSamePriority { get; set; }
    public int SearchHeuristic { get; set; }
    public int SearchPriority => _distance + SearchHeuristic;
    public int SearchPhase { get; set; }
    public HexUnit Unit { get; set; }
    public HexCellShaderData ShaderData { get; set; }
    public int Index { get; set; }

    private readonly bool[] _roads = new bool[6];
    private HexCell[] _neighbors = new HexCell[6];

    public HexCoordinates Coordinates { get; set; }
    public HexGridChunk Chunk;


    private void Refresh()
    {
        if (Chunk == null)
            return;

        Chunk.Refresh();
        foreach (var neighbor in _neighbors)
        {
            if (neighbor != null && neighbor.Chunk != Chunk)
                neighbor.Chunk.Refresh();
        }

        Unit?.ValidateLocation();
    }

    private void RefreshSelfOnly()
    {
        Chunk.Refresh();
        Unit?.ValidateLocation();
    }

    #region 河流处理

    public void SetOutgoingRiver(HexDirection direction)
    {
        if (_hasOutgoingRiver && _outgoingRiver == direction)
            return;

        var neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor))
            return;

        RemoveOutgoingRiver();
        if (_hasIncomingRiver && _incomingRiver == direction)
            RemoveIncomingRiver();

        _hasOutgoingRiver = true;
        _outgoingRiver = direction;
        _specialIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor._hasIncomingRiver = true;
        neighbor._incomingRiver = direction.Opposite();
        neighbor._specialIndex = 0;

        SetRoad((int)direction, false);
    }

    //移除出河流
    private void RemoveOutgoingRiver()
    {
        if (!_hasOutgoingRiver)
            return;

        _hasOutgoingRiver = false;
        RefreshSelfOnly();

        var neighbor = GetNeighbor(_outgoingRiver);
        neighbor._hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    //移除入河流
    private void RemoveIncomingRiver()
    {
        if (!_hasIncomingRiver)
            return;

        _hasIncomingRiver = false;
        RefreshSelfOnly();

        var neighbor = GetNeighbor(_incomingRiver);
        neighbor._hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveRiver()
    {
        RemoveIncomingRiver();
        RemoveOutgoingRiver();
    }

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return _hasIncomingRiver && _incomingRiver == direction
               || _hasOutgoingRiver && _outgoingRiver == direction;
    }

    public bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor != null &&
               (_elevation >= neighbor._elevation || _waterLevel == neighbor._elevation);
    }

    public void ValidateRivers()
    {
        if (_hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(_outgoingRiver)))
            RemoveOutgoingRiver();
        if (_hasIncomingRiver && !GetNeighbor(_incomingRiver).IsValidRiverDestination(this))
            RemoveIncomingRiver();
    }

    #endregion

    #region 道路处理

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return _roads[(int)direction];
    }

    public void AddRoads(HexDirection direction)
    {
        if (!_roads[(int)direction] &&
            !HasRiverThroughEdge(direction) &&
            !IsSpecial &&
            !GetNeighbor(direction).IsSpecial &&
            GetElevationDifference(direction) <= 1f)
        {
            SetRoad((int)direction, true);
        }
    }

    public void RemoveRoads()
    {
        for (var i = 0; i < _roads.Length; i++)
        {
            if (_roads[i])
            {
                SetRoad(i, false);
            }
        }
    }

    private void SetRoad(int index, bool state)
    {
        _roads[index] = state;
        _neighbors[index]._roads[(int)((HexDirection)index).Opposite()] = state;
        _neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    #endregion

    public void IncreaseVisibility()
    {
        _visibility += 1;

        if (_visibility == 1)
            ShaderData.RefreshVisibility(this);
    }

    public void DecreaseVisibility()
    {
        _visibility -= 1;

        if (_visibility == 0)
            ShaderData.RefreshVisibility(this);
    }

    private int GetElevationDifference(HexDirection direction)
    {
        var difference = _elevation - GetNeighbor(direction)._elevation;
        return difference >= 0 ? difference : -difference;
    }

    public void EnableHighlight(Color color)
    {
        _highlightSpr.Modulate = color;
        _highlightSpr.Visible = true;
    }

    public void DisableHighlight()
    {
        _highlightSpr.Visible = false;
    }

    public void SetLabel(string text) => _distanceLab.Text = text;
    public void ShowUi(bool visible) => _distanceLab.Visible = visible;

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)_terrainTypeIndex);
        writer.Write((byte)_elevation);
        writer.Write((byte)_waterLevel);
        writer.Write((byte)_urbanLevel);
        writer.Write((byte)_farmLevel);
        writer.Write((byte)_plantLevel);
        writer.Write((byte)_specialIndex);

        writer.Write(_walled);

        if (_hasIncomingRiver)
            writer.Write((byte)(_incomingRiver + 128));
        else
            writer.Write((byte)0);

        if (_hasOutgoingRiver)
            writer.Write((byte)(_outgoingRiver + 128));
        else
            writer.Write((byte)0);

        var roadFlags = 0;
        for (var i = 0; i < _roads.Length; i++)
            if (_roads[i])
                roadFlags |= 1 << i;
        writer.Write((byte)roadFlags);
    }

    public void Load(BinaryReader reader)
    {
        TerrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);

        Elevation = reader.ReadByte();
        WaterLevel = reader.ReadByte();
        UrbanLevel = reader.ReadByte();
        FarmLevel = reader.ReadByte();
        PlantLevel = reader.ReadByte();
        SpecialIndex = reader.ReadByte();

        Walled = reader.ReadBoolean();

        var riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            _hasIncomingRiver = true;
            _incomingRiver = (HexDirection)(riverData - 128);
        }
        else
            _hasIncomingRiver = false;

        riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            _hasOutgoingRiver = true;
            _outgoingRiver = (HexDirection)(riverData - 128);
        }
        else
            _hasOutgoingRiver = false;

        int roadFlags = reader.ReadByte();
        for (var i = 0; i < _roads.Length; i++)
            _roads[i] = (roadFlags & (1 << i)) != 0;
    }

    public HexCell GetNeighbor(HexDirection direction)
    {
        return _neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(Elevation, _neighbors[(int)direction].Elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(Elevation, otherCell.Elevation);
    }
}