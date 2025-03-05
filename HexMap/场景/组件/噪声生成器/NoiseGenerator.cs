using Godot;

public partial class NoiseGenerator : Node
{
    private FastNoiseLite _noiseLite = new();

    public void CreateNoiseImage(uint seed, Vector2I imageSize, out Image noiseImage)
    {
        noiseImage = Image.CreateEmpty(imageSize.X, imageSize.Y, false, Image.Format.Rgba8);

        _noiseLite.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _noiseLite.Seed = (int)seed;
        _noiseLite.Frequency = 1f / 20f;

        for (var x = 0; x < imageSize.X; x++)
        for (var y = 0; y < imageSize.Y; y++)
        {
            var noiseR = _noiseLite.GetNoise2D(x, y);
            var noiseG = _noiseLite.GetNoise2D(x + imageSize.X, y + imageSize.Y);
            var noiseB = _noiseLite.GetNoise2D(x + imageSize.X * 2, y + imageSize.Y * 2);
            var noiseA = _noiseLite.GetNoise2D(x + imageSize.X * 3, y + imageSize.Y * 3);
            var color = new Color((noiseR + 1f) / 2f, (noiseG + 1f) / 2f, (noiseB + 1f) / 2f);
            noiseImage.SetPixel(x, y, color);
        }
    }
}