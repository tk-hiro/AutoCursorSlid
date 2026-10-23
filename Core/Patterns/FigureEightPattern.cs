namespace DishAuto.Core.Patterns;

/// <summary>
/// 横の8の字と縦の8の字を交互に描く。横→縦を1組として、組ごとに少しずつ小さくして
/// 中心まで行き、また大きくしていく。どちらの8の字も中心から始まり中心で終わるので、つなぎ目は滑らか。
/// </summary>
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
            foreach (var p in Lap(region, s, vertical: false)) yield return p;
            foreach (var p in Lap(region, s, vertical: true)) yield return p;
        }
    }

    private static IEnumerable<PointD> Lap(Region region, double scale, bool vertical)
    {
        for (int i = 0; i < PointsPerLap; i++)
        {
            double t = 2 * Math.PI * i / PointsPerLap;
            double a = Math.Sin(t), b = Math.Sin(2 * t);
            // 横: 左右に輪が並ぶ ∞、縦: 上下に輪が並ぶ 8
            var (x, y) = vertical ? (b, a) : (a, b);
            yield return region.Clamp(new PointD(region.Cx + scale * region.Rx * x, region.Cy + scale * region.Ry * y));
        }
    }
}
