namespace DishAuto.Core.Patterns;

/// <summary>範囲内を1周するための通過点の列を生成する。実行中は繰り返し呼ばれる。</summary>
public interface IPathPattern
{
    IEnumerable<PointD> Generate(Region region, double spacing, Random rng);
}

public static class PatternFactory
{
    public static IPathPattern Create(PatternKind kind) => kind switch
    {
        PatternKind.ZigzagHorizontal => new ZigzagPattern(vertical: false),
        PatternKind.ZigzagVertical => new ZigzagPattern(vertical: true),
        PatternKind.Spiral => new SpiralPattern(),
        PatternKind.Concentric => new ConcentricPattern(),
        PatternKind.FigureEight => new FigureEightPattern(),
        PatternKind.Radial => new RadialPattern(),
        PatternKind.Random => new RandomPattern(),
        PatternKind.CircleScrub => new CircleScrubPattern(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
