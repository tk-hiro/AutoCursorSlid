namespace DishAuto.Core.Patterns;

/// <summary>8の字を描く。1周ごとに少しずつ小さくして中心まで行き、また大きくしていく。</summary>
public sealed class FigureEightPattern : IPathPattern
{
    private const int PointsPerLap = 120;

    public IEnumerable<PointD> Generate(Region region, double spacing, Random rng)
    {
        int laps = Math.Max(2, (int)Math.Ceiling(Math.Min(region.Rx, region.Ry) / spacing));
        var scales = Enumerable.Range(0, laps).Select(k => 1.0 - (double)k / laps).ToList();
        scales.AddRange(Enumerable.Range(1, laps - 1).Select(k => (double)k / laps));

        foreach (double s in scales)
        {
            for (int i = 0; i < PointsPerLap; i++)
            {
                double t = 2 * Math.PI * i / PointsPerLap;
                yield return region.Clamp(new PointD(
                    region.Cx + s * region.Rx * Math.Sin(t),
                    region.Cy + s * region.Ry * Math.Sin(2 * t)));
            }
        }
    }
}
