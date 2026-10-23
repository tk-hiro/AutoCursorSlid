namespace DishAuto.Core.Patterns;

/// <summary>往復しながら端から端まで進み、終わったら逆順に戻る。</summary>
public sealed class ZigzagPattern(bool vertical) : IPathPattern
{
    public IEnumerable<PointD> Generate(Region region, double spacing, Random rng)
    {
        var rows = BuildRows(region, spacing, vertical);

        foreach (var (a, b) in rows)
        {
            yield return a;
            yield return b;
        }
        for (int i = rows.Count - 1; i >= 0; i--)
        {
            yield return rows[i].B;
            yield return rows[i].A;
        }
    }

    /// <summary>向きが交互になる走査線の一覧。</summary>
    internal static List<(PointD A, PointD B)> BuildRows(Region region, double spacing, bool vertical)
    {
        double length = vertical ? region.Width : region.Height;
        int count = Math.Max(1, (int)Math.Round(length / spacing));
        double step = length / count;

        var rows = new List<(PointD A, PointD B)>();
        for (int i = 0; i < count; i++)
        {
            double c = (vertical ? region.X : region.Y) + step * (i + 0.5);
            var span = vertical ? region.VerticalSpan(c) : region.HorizontalSpan(c);
            if (span is not { } s) continue;

            var a = vertical ? new PointD(c, s.Start) : new PointD(s.Start, c);
            var b = vertical ? new PointD(c, s.End) : new PointD(s.End, c);
            rows.Add(rows.Count % 2 == 0 ? (a, b) : (b, a));
        }
        return rows;
    }
}
