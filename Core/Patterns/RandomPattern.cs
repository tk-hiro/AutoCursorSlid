namespace DishAuto.Core.Patterns;

/// <summary>範囲内のランダムな点を順に結ぶ。規則的な動きで擦り残しが出る場合向け。</summary>
public sealed class RandomPattern : IPathPattern
{
    private const int PointsPerCycle = 20;

    public IEnumerable<PointD> Generate(Region region, double spacing, Random rng)
    {
        for (int i = 0; i < PointsPerCycle; i++)
            yield return region.RandomPoint(rng);
    }
}
