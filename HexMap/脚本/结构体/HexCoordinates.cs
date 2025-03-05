using System.IO;
using Godot;

public struct HexCoordinates
{
    public int X { get; }
    public int Z { get; }

    public int Y => -X - Z;

    public HexCoordinates(int x, int z)
    {
        X = x;
        Z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinates(x - z / 2, z);
    }

    public static HexCoordinates FromPosition(Vector3 position)
    {
        var x = position.X / (HexMetrics.InnerRadius * 2f);
        var y = -x;
        var offset = position.Z / (HexMetrics.OuterRadius * 3f);
        x -= offset;
        y -= offset;
        var iX = Mathf.RoundToInt(x);
        var iY = Mathf.RoundToInt(y);
        var iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ != 0)
        {
            var dX = Mathf.Abs(x - iX);
            var dY = Mathf.Abs(y - iY);
            var dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ)
                iX = -iY - iZ;
            else if (dZ > dX) iZ = -iX - iY;
        }

        return new HexCoordinates(iX, iZ);
    }

    public int DistanceTo(HexCoordinates other)
    {
        return ((X < other.X ? other.X - X : X - other.X) +
                (Y < other.Y ? other.Y - Y : Y - other.Y) +
                (Z < other.Z ? other.Z - Z : Z - other.Z)) / 2;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Z);
    }

    public static HexCoordinates Load(BinaryReader reader)
    {
        return new HexCoordinates(reader.ReadInt32(), reader.ReadInt32());
    }

    public string GetStr()
    {
        return X + " " + Y + " " + Z;
    }

    public override string ToString()
    {
        return X + "\n" + Y + "\n" + Z;
    }
}