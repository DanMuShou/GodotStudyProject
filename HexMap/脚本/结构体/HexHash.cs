namespace HexMap.脚本.结构体;

public struct HexHash
{
    public float A, B, C, D, E;

    public static HexHash Create()
    {
        HexHash hash;
        hash.A = HexMetrics.Rng.Randf() * 0.999f;
        hash.B = HexMetrics.Rng.Randf() * 0.999f;
        hash.C = HexMetrics.Rng.Randf() * 0.999f;
        hash.D = HexMetrics.Rng.Randf() * 0.999f;
        hash.E = HexMetrics.Rng.Randf() * 0.999f;
        return hash;
    }
}