namespace DishAuto.Core.Patterns;

/// <summary>円を1周ずつ描きながら外側から内側へ小さくしていき、中心から外側へ戻る。</summary>
public sealed class ConcentricPattern : IPathPattern
{
    private const double SegmentPx = 8.0; // 円周を何pxごとの折れ線で近似するか

    public IEnumerable<PointD> Generate(Region region, double spacing, Random rng)
    {
        // 矩形は角まで届くよう少し大きい円にする（はみ出した分は範囲の端に沿う）
        double maxScale = region.Shape == RegionShape.Rectangle ? Math.Sqrt(2) : 1.0;
        int rings = Math.Max(1, (int)Math.Round(Math.Min(region.Rx, region.Ry) * maxScale / spacing));
        double maxR = Math.Max(region.Rx, region.Ry);

        var points = new List<PointD>();
        for (int k = 0; k < rings; k++)
        {
            double f = maxScale * (1.0 - (double)k / rings);
            int count = Math.Clamp((int)Math.Ceiling(2 * Math.PI * f * maxR / SegmentPx), 12, 720);
            for (int i = 0; i <= count; i++)
            {
                double theta = 2 * Math.PI * i / count;
                points.Add(region.Clamp(new PointD(region.Cx + f * region.Rx * Math.Cos(theta), region.Cy + f * region.Ry * Math.Sin(theta))));
            }
        }
        points.Add(region.Center);

        foreach (var p in points) yield return p;
        for (int i = points.Count - 1; i >= 0; i--) yield return points[i];
    }
}
