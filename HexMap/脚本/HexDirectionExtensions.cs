public enum HexDirection
{
    SW,
    W,
    NW,
    NE,
    E,
    SE
}

public static class HexDirectionExtensions
{
    public static HexDirection Opposite(this HexDirection direction)
    {
        return (int)direction < 3 ? direction + 3 : direction - 3;
    }

    public static HexDirection Previous(this HexDirection direction)
    {
        return direction == HexDirection.SW ? HexDirection.SE : direction - 1;
    }

    public static HexDirection Previous2(this HexDirection direction)
    {
        direction -= 2;
        return direction >= HexDirection.SW ? direction : (direction + 6);
    }

    public static HexDirection Next(this HexDirection direction)
    {
        return direction == HexDirection.SE ? HexDirection.SW : direction + 1;
    }

    public static HexDirection Next2(this HexDirection direction)
    {
        direction += 2;
        return direction <= HexDirection.SE ? direction : (direction - 6);
    }
}