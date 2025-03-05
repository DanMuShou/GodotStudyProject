using Godot;

public enum XYZ
{
    X,
    Y,
    Z,
}

public struct EdgeVertices
{
    public Vector3 V1, V2, V3, V4, V5;

    public EdgeVertices(Vector3 corner1, Vector3 corner2)
    {
        V1 = corner1;
        V2 = corner1.Lerp(corner2, 0.25f);
        V3 = corner1.Lerp(corner2, 0.5f);
        V4 = corner1.Lerp(corner2, 0.75f);
        V5 = corner2;
    }

    public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
    {
        V1 = corner1;
        V2 = corner1.Lerp(corner2, outerStep);
        V3 = corner1.Lerp(corner2, 0.5f);
        V4 = corner1.Lerp(corner2, 1f - outerStep);
        V5 = corner2;
    }

    public static EdgeVertices TerraceLerp(EdgeVertices a, EdgeVertices b, int step)
    {
        EdgeVertices result;
        result.V1 = HexMetrics.TerraceLerp(a.V1, b.V1, step);
        result.V2 = HexMetrics.TerraceLerp(a.V2, b.V2, step);
        result.V3 = HexMetrics.TerraceLerp(a.V3, b.V3, step);
        result.V4 = HexMetrics.TerraceLerp(a.V4, b.V4, step);
        result.V5 = HexMetrics.TerraceLerp(a.V5, b.V5, step);
        return result;
    }

    public override string ToString()
    {
        return "v1: " + V1 + ", v2: " + V2 + ", v3: " + V4 + ", v4: " + V4 + ", v5: " + V5;
    }
}