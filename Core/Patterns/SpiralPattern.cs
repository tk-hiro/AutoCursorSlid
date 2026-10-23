namespace DishAuto.Core.Patterns;

/// <summary>外周から中心へ渦巻き状に進み、中心から外周へ戻る。</summary>
public sealed class SpiralPattern : IPathPattern
{
    public IEnumerable<PointD> Generate(Region region, double spacing, Random rng)
    {
        var points = region.Shape == RegionShape.Ellipse
            ? EllipticSpiral(region, spacing)
            : RectangularSpiral(region, spacing);

        foreach (var p in points) yield return p;
        for (int i = points.Count - 1; i >= 0; i--) yield return points[i];
    }

    private static List<PointD> EllipticSpiral(Region r, double spacing)
    {
        double turns = Math.Max(1.0, Math.Min(r.Rx, r.Ry) / spacing);
        double maxTheta = 2 * Math.PI * turns;
        double maxR = Math.Max(r.Rx, r.Ry);

        var points = new List<PointD>();
        for (double theta = 0; theta < maxTheta;)
        {
            double f = 1.0 - theta / maxTheta;
            points.Add(new PointD(r.Cx + f * r.Rx * Math.Cos(theta), r.Cy + f * r.Ry * Math.Sin(theta)));
            theta += Math.Clamp(spacing / (f * maxR + 1.0), 0.05, 0.5);
        }
        points.Add(r.Center);
        return points;
    }

    private static List<PointD> RectangularSpiral(Region r, double spacing)
    {
        double left = r.X + spacing / 2, top = r.Y + spacing / 2;
        double right = r.X + r.Width - spacing / 2, bottom = r.Y + r.Height - spacing / 2;

        var points = new List<PointD>();
        while (left <= right && top <= bottom)
        {
            points.Add(new(left, top));
            points.Add(new(right, top));
            points.Add(new(right, bottom));
            points.Add(new(left, bottom));
            left += spacing;
            top += spacing;
            right -= spacing;
            bottom -= spacing;
        }
        if (points.Count == 0) points.Add(r.Center);
        return points;
    }
}
