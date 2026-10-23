namespace DishAuto.Core.Patterns;

/// <summary>
/// 横の8の字（∞）と縦の8の字を交互に描く。
/// 横の8の字は中央でくびれて上下の真ん中が擦れないので、
/// 縦の8の字をその細い部分に合わせて描き、全体をまんべんなく擦る。
/// どちらも中心から始まり中心で終わるので、つなぎ目は滑らか。
/// </summary>
public sealed class FigureEightPattern : IPathPattern
{
    private const int PointsPerLap = 160;
    private const double VerticalWidthRatio = 0.5; // 縦の8の字の幅（範囲の幅に対する割合）
    private const double Margin = 0.97;            // 範囲の端で形が切れないよう少し内側に収める

    public IEnumerable<PointD> Generate(Region region, double spacing, Random rng)
    {
        // 横: 範囲の幅いっぱいの ∞。楕円では輪の高さを抑えないと端で切れる
        double lobeHeight = region.Shape == RegionShape.Ellipse ? Math.Sqrt(0.5) : 1.0;
        foreach (var p in Lap(region, region.Rx * Margin, region.Ry * lobeHeight * Margin, vertical: false))
            yield return p;

        // 縦: 横の8の字のくびれ（中央の上下）を埋める、幅を絞った 8
        foreach (var p in Lap(region, region.Rx * VerticalWidthRatio * Margin, region.Ry * Margin, vertical: true))
            yield return p;
    }

    private static IEnumerable<PointD> Lap(Region region, double ax, double ay, bool vertical)
    {
        for (int i = 0; i < PointsPerLap; i++)
        {
            double t = 2 * Math.PI * i / PointsPerLap;
            double a = Math.Sin(t), b = Math.Sin(2 * t);
            var (x, y) = vertical ? (b, a) : (a, b);
            yield return region.Clamp(new PointD(region.Cx + ax * x, region.Cy + ay * y));
        }
    }
}
