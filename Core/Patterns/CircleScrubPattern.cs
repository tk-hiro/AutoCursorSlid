namespace DishAuto.Core.Patterns;

/// <summary>小さな円を描きながら横方向に進み、全体をなめるように擦る。</summary>
public sealed class CircleScrubPattern : IPathPattern
{
    private const double AngleStep = Math.PI / 6;   // 1周12点
    private const double AdvanceRatio = 0.25;        // 1点ごとに半径の25%進む

    public IEnumerable<PointD> Generate(Region region, double spacing, Random rng)
    {
        double radius = spacing;
        var rows = ZigzagPattern.BuildRows(region, spacing * 1.5, vertical: false);

        var points = new List<PointD>();
        double angle = 0;
        foreach (var (a, b) in rows)
        {
            double length = PointD.Distance(a, b);
            int count = Math.Max(1, (int)Math.Ceiling(length / (radius * AdvanceRatio)));
            for (int i = 0; i <= count; i++)
            {
                var c = PointD.Lerp(a, b, (double)i / count);
                var p = new PointD(c.X + radius * Math.Cos(angle), c.Y + radius * Math.Sin(angle));
                points.Add(region.Clamp(p));
                angle += AngleStep;
            }
        }

        foreach (var p in points) yield return p;
        for (int i = points.Count - 1; i >= 0; i--) yield return points[i];
    }
}
