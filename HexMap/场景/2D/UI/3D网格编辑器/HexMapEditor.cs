using System;
using System.IO;
using Godot;

public enum OptionalToggle
{
    Ignore,
    Yes,
    No
}

public enum TextureToggle
{
    None,
    Sand,
    Grass,
    Mud,
    Stone,
    Snow
}

public partial class HexMapEditor : Control
{
    [Signal]
    public delegate void OnNewMapEventHandler();

    [Signal]
    public delegate void OnSaveOrLoadEventHandler(bool mode);

    [Signal]
    public delegate void OnChangeEditModeEventHandler(bool mode);

    [Export] private TextureToggle
        _textureInit;

    [Export] private OptionalToggle
        _riverInit, _roadInit, _wallInit;

    [Export] private ShaderMaterial _terrainMaterial;
    [Export] private EditPhyProcess _editPhyProcess;
    [Export] private CheckButton _editMode;
    [Export] private TabContainer _editTab;
    [Export] public EnableLevelComponent Elevation { get; private set; }
    [Export] public EnableLevelComponent Water { get; private set; }
    [Export] public EnableLevelComponent Urban { get; private set; }
    [Export] public EnableLevelComponent Farm { get; private set; }
    [Export] public EnableLevelComponent Plant { get; private set; }
    [Export] public EnableLevelComponent Special { get; private set; }
    [Export] public EnableLevelComponent Brush { get; private set; }
    [Export] public OptionSelectComponent Texture { get; private set; }
    [Export] public OptionSelectComponent River { get; private set; }
    [Export] public OptionSelectComponent Road { get; private set; }
    [Export] public OptionSelectComponent Wall { get; private set; }

    [Export] private CheckButton _labelVisual, _gridVisual;
    [Export] private Button _saveBut, _loadBut, _newMapBut;

    private HexGrid _hexGrid;

    public void Init(HexGrid hexGrid)
    {
        _hexGrid = hexGrid;

        _hexGrid.OnGridMapCreatOrLoadFinished += SetInitState;

        _gridVisual.Pressed += () => ShowGrid(_gridVisual.ButtonPressed);
        _editMode.Pressed += () => OnSetEditMode(_editMode.ButtonPressed);

        _saveBut.Pressed += OnSaveButPress;
        _loadBut.Pressed += OnLoadButPress;
        _newMapBut.Pressed += OnNewMapButPress;

        CompletionUiComponent();
        SetInitState();
        _editPhyProcess.Init(_hexGrid);
    }

    private void CompletionUiComponent()
    {
        Elevation.Init("地高", (0, 5));
        Water.Init("水面", (0, 5));
        Urban.Init("城镇", (0, 3));
        Farm.Init("村庄", (0, 3));
        Plant.Init("植被", (0, 3));
        Special.Init("特殊", (0, 3));
        Brush.Init("大笔刷", (0, 3));

        var textureToggle = GetEnumNames(typeof(TextureToggle));
        Texture.Init("纹理", textureToggle);

        var optionalToggle = GetEnumNames(typeof(OptionalToggle));
        River.Init("河流", optionalToggle);
        Road.Init("道路", optionalToggle);
        Wall.Init("城墙", optionalToggle);
    }

    private void SetInitState()
    {
        Elevation.Level = 0;
        Elevation.Enable = false;
        Water.Level = 0;
        Water.Enable = false;
        Urban.Level = 0;
        Urban.Enable = false;
        Farm.Level = 0;
        Farm.Enable = false;
        Plant.Level = 0;
        Plant.Enable = false;
        Special.Level = 0;
        Special.Enable = false;
        Brush.Level = 0;
        Brush.Enable = false;

        Texture.ModeIndex = (int)_textureInit;
        River.ModeIndex = (int)_riverInit;
        Road.ModeIndex = (int)_roadInit;
        Wall.ModeIndex = (int)_wallInit;

        _labelVisual.SetPressed(false);
        _gridVisual.SetPressed(true);
        _editMode.SetPressed(true);

        ShowGrid(true);
        OnSetEditMode(true);
    }

    private void ShowGrid(bool visible)
    {
        _terrainMaterial.SetShaderParameter("_gridOn", visible);
    }

    private string[] GetEnumNames(Type enumType)
    {
        return Enum.GetNames(enumType);
    }

    private void OnSetEditMode(bool isEdit)
    {
        _editTab.CurrentTab = isEdit ? 0 : 1;
        _editPhyProcess.ProcessEnable = isEdit;
        EmitSignalOnChangeEditMode(isEdit);
    }

    private void OnSaveButPress()
    {
        EmitSignalOnSaveOrLoad(true);
    }

    private void OnLoadButPress()
    {
        EmitSignalOnSaveOrLoad(false);
    }

    private void OnNewMapButPress()
    {
        EmitSignalOnNewMap();
    }
}