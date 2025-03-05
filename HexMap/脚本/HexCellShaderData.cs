using Godot;

public partial class HexCellShaderData : Node
{
    private Image _imageSource;
    private Color[] _cellData;
    private ImageTexture _cellTexture;

    public void Init(int x, int z)
    {
        _imageSource = Image.CreateEmpty(x, z, false, Image.Format.Rgba8);
        _cellTexture = ImageTexture.CreateFromImage(_imageSource);

        RenderingServer.GlobalShaderParameterSet("_hexCellDate", Variant.CreateFrom(_cellTexture));
        RenderingServer.GlobalShaderParameterSet(
            "_hexCellDataTexelSize", new Vector4(1f / x, 1f / z, x, z));

        if (_cellData == null || _cellData.Length != x * z)
        {
            _cellData = new Color[x * z];
        }
        else
        {
            for (var i = 0; i < _cellData.Length; i++)
            {
                _cellData[i] = new Color(0, 0, 0, 0);
            }
        }

        SetPhysicsProcess(true);
    }

    private void SetImageInfo(HexCell cell)
    {
        _imageSource.SetPixel(cell.Index % _imageSource.GetWidth(), cell.Index / _imageSource.GetWidth(),
            _cellData[cell.Index]);
    }

    public override void _PhysicsProcess(double delta)
    {
        _cellTexture.Update(_imageSource);
        SetPhysicsProcess(false);
    }

    public void RefreshTerrain(HexCell cell)
    {
        _cellData[cell.Index].A8 = (byte)cell.TerrainTypeIndex;
        SetImageInfo(cell);
        SetPhysicsProcess(true);
    }

    public void RefreshVisibility(HexCell cell)
    {
        _cellData[cell.Index].R8 = cell.IsCellVisible ? (byte)255 : (byte)0;
        SetImageInfo(cell);
        SetPhysicsProcess(true);
    }
}