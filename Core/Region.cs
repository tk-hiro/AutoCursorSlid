namespace DishAuto.Core;

/// <summary>カーソルを動かす範囲。座標は物理ピクセル。楕円の場合は矩形に内接する楕円。</summary>
public sealed record Region(int X, int Y, int Width, int Height, RegionShape Shape)
{
    public const int MinSize = 10;

    public bool IsValid => Width >= MinSize && Height >= MinSize;

    public double Cx => X + Width / 2.0;
    public double Cy => Y + Height / 2.0;
    public double Rx => Width / 2.0;
    public double Ry => Height / 2.0;
    public PointD Center => new(Cx, Cy);

    public bool Contains(PointD p)
    {
        if (Shape == RegionShape.Rectangle)
            return p.X >= X && p.X <= X + Width && p.Y >= Y && p.Y <= Y + Height;

        double nx = (p.X - Cx) / Rx, ny = (p.Y - Cy) / Ry;
        return nx * nx + ny * ny <= 1.0 + 1e-9;
    }

    /// <summary>範囲外の点を境界上に寄せる。</summary>
    public PointD Clamp(PointD p)
    {
        if (Shape == RegionShape.Rectangle)
            return new(Math.Clamp(p.X, X, X + Width), Math.Clamp(p.Y, Y, Y + Height));

        double nx = (p.X - Cx) / Rx, ny = (p.Y - Cy) / Ry;
        double r = Math.Sqrt(nx * nx + ny * ny);
        return r <= 1.0 ? p : new(Cx + nx / r * Rx, Cy + ny / r * Ry);
    }

    /// <summary>高さ y における範囲の左右端。範囲外なら null。</summary>
    public (double Start, double End)? HorizontalSpan(double y)
    {
        if (y < Y || y > Y + Height) return null;
        if (Shape == RegionShape.Rectangle) return (X, X + Width);

        double ny = (y - Cy) / Ry;
        double t = 1.0 - ny * ny;
        if (t < 0) return null;
        double half = Rx * Math.Sqrt(t);
        return (Cx - half, Cx + half);
    }

    /// <summary>横位置 x における範囲の上下端。範囲外なら null。</summary>
    public (double Start, double End)? VerticalSpan(double x)
    {
        if (x < X || x > X + Width) return null;
        if (Shape == RegionShape.Rectangle) return (Y, Y + Height);

        double nx = (x - Cx) / Rx;
        double t = 1.0 - nx * nx;
        if (t < 0) return null;
        double half = Ry * Math.Sqrt(t);
        return (Cy - half, Cy + half);
    }

    public PointD RandomPoint(Random rng)
    {
        for (int i = 0; i < 100; i++)
        {
            var p = new PointD(X + rng.NextDouble() * Width, Y + rng.NextDouble() * Height);
            if (Contains(p)) return p;
        }
        return Center;
    }

    public override string ToString() => $"X={X} Y={Y} W={Width} H={Height}";
}
