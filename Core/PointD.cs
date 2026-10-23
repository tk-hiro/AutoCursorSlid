namespace DishAuto.Core;

/// <summary>スクリーン座標（物理ピクセル）。</summary>
public readonly record struct PointD(double X, double Y)
{
    public static double Distance(PointD a, PointD b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static PointD Lerp(PointD a, PointD b, double t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}
