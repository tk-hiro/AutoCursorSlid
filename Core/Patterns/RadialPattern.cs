namespace DishAuto.Core.Patterns;

/// <summary>中心から外側へ往復しながら、少しずつ角度を変えて1周する。</summary>
public sealed class RadialPattern : IPathPattern
{
    public IEnumerable<PointD> Generate(Region region, double spacing, Random rng)
    {
        // 矩形は角まで届くよう長めに伸ばす（はみ出した分は範囲の端で止まる）
        double reach = region.Shape == RegionShape.Rectangle ? Math.Sqrt(2) : 1.0;
        double maxR = Math.Max(region.Rx, region.Ry);
        int spokes = Math.Max(8, (int)Math.Ceiling(2 * Math.PI * maxR / spacing));

        for (int i = 0; i < spokes; i++)
        {
            double theta = 2 * Math.PI * i / spokes;
            yield return region.Center;
            yield return region.Clamp(new PointD(
                region.Cx + reach * region.Rx * Math.Cos(theta),
                region.Cy + reach * region.Ry * Math.Sin(theta)));
        }
    }
}
