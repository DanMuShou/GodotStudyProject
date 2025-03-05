using Godot;
using HexMap.脚本.结构体;

//地形类型
public enum HexEdgeType
{
    Flat,
    Slope,
    Cliff,
}

public class HexMetrics
{
    // public const float OuterRadius = 10f * BaseSize; //?????????????????????
    // public const float ElevationStep = 3f * BaseSize; //?????????????????????
    // private const float CellPerturbStrength = 4f * BaseSize; //?????????????????????
    // public const float ElevationPerturbStrength = 5f * BaseSize; //?????????????????????
    // private const float NoiseScale = 0.003f / BaseSize; //?????????????????????
    // public const float StreamBedElevationOffset = -1.75f * BaseSize; //?????????????????????
    // public const float WaterElevationOffset = -0.5f * BaseSize; //?????????????????????
    public const string ProjectName = "HexMap_GodotProj";

    private static readonly Vector3[] Corners =
    {
        new(0f, 0f, OuterRadius),
        new(-InnerRadius, 0f, 0.5f * OuterRadius),
        new(-InnerRadius, 0f, -0.5f * OuterRadius),
        new(0f, 0f, -OuterRadius),
        new(InnerRadius, 0f, -0.5f * OuterRadius),
        new(InnerRadius, 0f, 0.5f * OuterRadius),
        new(0f, 0f, OuterRadius),
    };

    public static RandomNumberGenerator Rng { get; } = new();
    public static Image NoiseSource;

    private static HexHash[] _hashGrid;

    private const float OuterToInner = 0.866025404f;
    public const float InnerToOuter = 1f / OuterToInner;
    public const float OuterRadius = 1f;
    public const float InnerRadius = OuterRadius * OuterToInner;

    private const float SolidFactor = 0.8f;
    private const float BorderFactor = 1f - SolidFactor;

    public const float ElevationStep = 0.3f;
    private const int TerracesPerSlope = 2;
    public const int TerraceSteps = TerracesPerSlope * 2 + 1;
    private const float HorizontalTerraceStepSize = 1f / TerraceSteps;
    private const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

    private const float CellPerturbStrength = 0.4f;
    public const float ElevationPerturbStrength = 0.15f;

    public const int ChunkSizeX = 5;
    public const int ChunkSizeZ = 5;

    public const float StreamBedElevationOffset = -1.75f;
    public const float WaterElevationOffset = -0.5f;

    private const float WaterFactor = 0.6f;
    private const float WaterBlendFactor = 1 - WaterFactor;

    private const int HashGridSize = 256;
    private const float HashGridScale = 2.5f;

    private static readonly float[][] FeatureThresholds =
    {
        [0.0f, 0.0f, 0.4f],
        [0.0f, 0.4f, 0.6f],
        [0.4f, 0.6f, 0.8f]
    };

    public const float WallHeight = 0.4f;
    public const float WallTowerThreshold = 0.5f;
    private const float WallYOffset = -0.1f;
    private const float WallThickness = 0.075f;
    private const float WallElevationOffset = VerticalTerraceStepSize;

    public const float BridgeDesignLength = 0.7f;

    public static Color[] Colors;


    public static Vector3 GetFirstCorner(HexDirection direction)
        => Corners[(int)direction];

    public static Vector3 GetSecondCorner(HexDirection direction)
        => Corners[(int)direction + 1];

    public static Vector3 GetFirstSolidCorner(HexDirection direction)
        => Corners[(int)direction] * SolidFactor;

    public static Vector3 GetSecondSolidCorner(HexDirection direction)
        => Corners[(int)direction + 1] * SolidFactor;

    public static Vector3 GetFirstWaterCorner(HexDirection direction)
        => Corners[(int)direction] * WaterFactor;

    public static Vector3 GetSecondWaterCorner(HexDirection direction)
        => Corners[(int)direction + 1] * WaterFactor;

    public static Vector3 GetBridge(HexDirection direction)
        => (Corners[(int)direction] + Corners[(int)direction.Next()]) * BorderFactor;

    public static Vector3 GetWaterBridge(HexDirection direction)
        => (Corners[(int)direction] + Corners[(int)direction.Next()]) * WaterBlendFactor;

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        //阶梯/-/-/-为一层 奇数竖直 偶数水平
        //偶数插值步长
        var t = step * HorizontalTerraceStepSize;
        a.X += (b.X - a.X) * t;
        a.Z += (b.Z - a.Z) * t;
        //奇数插值步长
        var v = (step + 1) / 2 * VerticalTerraceStepSize;
        a.Y += (b.Y - a.Y) * v;
        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        var h = step * HorizontalTerraceStepSize;
        return a.Lerp(b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
            return HexEdgeType.Flat;

        var delta = elevation2 - elevation1;

        return delta is 1 or -1 ? HexEdgeType.Slope : HexEdgeType.Cliff;
    }

    public static Vector4 SampleNoise(Vector3 position)
    {
        var size = 40 * OuterRadius * 2;
        var offset = new Vector2(OuterRadius, InnerRadius);
        var offUv = new Vector2(
            (position.X + offset.X) / size,
            (position.Z + offset.Y) / size);

        if (offUv.X >= 1)
            offUv.X -= 1;
        if (offUv.Y >= 1)
            offUv.Y -= 1;

        var noiseSize = NoiseSource.GetSize();

        var x = offUv.X * noiseSize.X - 1;
        var y = offUv.Y * noiseSize.Y - 1;

        var x0 = (int)x;
        var x1 = Mathf.Min(x0 + 1, noiseSize.X - 1);
        var y0 = (int)y;
        var y1 = Mathf.Min(y0 + 1, noiseSize.Y - 1);

        var s = x - x0;
        var t = y - y0;

        var c00 = NoiseSource.GetPixel(x0, y0);
        var c10 = NoiseSource.GetPixel(x1, y0);
        var c01 = NoiseSource.GetPixel(x0, y1);
        var c11 = NoiseSource.GetPixel(x1, y1);

        var c0 = c00 * (1 - s) + c10 * s;
        var c1 = c01 * (1 - s) + c11 * s;

        var finalColor = c0 * (1 - t) + c1 * t;
        return new Vector4(finalColor.R, finalColor.G, finalColor.B, finalColor.A);
    }

    public static Vector3 Perturb(Vector3 position)
    {
        var sample = SampleNoise(position);
        position.X += (sample.X * 2f - 1f) * CellPerturbStrength;
        position.Z += (sample.Z * 2f - 1f) * CellPerturbStrength;
        return position;
    }

    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
        => (Corners[(int)direction] + Corners[(int)direction + 1]) * (0.5f * SolidFactor);

    public static void InitializeHashGrid(ulong seed)
    {
        _hashGrid = new HexHash[HashGridSize * HashGridSize];
        var saveState = Rng.State;
        Rng.SetSeed(seed);

        for (var i = 0; i < _hashGrid.Length; i++)
            _hashGrid[i] = HexHash.Create();

        Rng.SetState(saveState);
    }

    public static HexHash SampleHashGrid(Vector3 position)
    {
        var x = (int)(position.X * HashGridScale) % HashGridSize;
        if (x < 0)
            x += HashGridSize;

        var z = (int)(position.Z * HashGridScale) % HashGridSize;
        if (z < 0)
            z += HashGridSize;

        return _hashGrid[x + z * HashGridSize];
    }

    public static float[] GetFeatureThresholds(int level)
        => FeatureThresholds[level];

    public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
    {
        Vector3 offset;
        offset.X = far.X - near.X;
        offset.Y = 0f;
        offset.Z = far.Z - near.Z;
        return offset * (WallThickness * 0.5f);
        // return ((offset * 10f).Normalized() * (WallThickness * 0.5f)) / 10f;
    }

    public static Vector3 WallLerp(Vector3 near, Vector3 far)
    {
        near.X += (far.X - near.X) * 0.5f;
        near.Z += (far.Z - near.Z) * 0.5f;
        var v = near.Y < far.Y ? WallElevationOffset : (1f - WallElevationOffset);
        near.Y += (far.Y - near.Y) * v + WallYOffset;
        return near;
    }
}