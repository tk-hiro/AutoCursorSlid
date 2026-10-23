using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shapes;
using DishAuto.Core;
using static DishAuto.Native.NativeMethods;

namespace DishAuto.Views;

/// <summary>
/// 画面全体を覆う半透明のオーバーレイで、ドラッグにより範囲を選ばせる。
/// 結果は GetCursorPos の物理ピクセル座標で返すため、DPI 設定に影響されない。
/// </summary>
public partial class RegionSelectWindow : Window
{
    private readonly RegionShape _shape;
    private readonly Region? _previous;
    private Point _startDip;
    private POINT _startPx;
    private bool _dragging;

    public RegionSelectWindow(RegionShape shape, Region? previous)
    {
        InitializeComponent();
        _shape = shape;
        _previous = previous;
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
    }

    public Region? Result { get; private set; }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // 仮想デスクトップ（全モニター）を物理ピクセルで覆う
        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowPos(hwnd, 0,
            GetSystemMetrics(SM_XVIRTUALSCREEN), GetSystemMetrics(SM_YVIRTUALSCREEN),
            GetSystemMetrics(SM_CXVIRTUALSCREEN), GetSystemMetrics(SM_CYVIRTUALSCREEN),
            SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Activate();
        Focus();

        // ヒントはプライマリモニターの左上付近に出す
        var primaryTopLeft = PointFromScreen(new Point(0, 0));
        Canvas.SetLeft(Hint, primaryTopLeft.X + 24);
        Canvas.SetTop(Hint, primaryTopLeft.Y + 24);

        if (_previous is { } prev)
        {
            var tl = PointFromScreen(new Point(prev.X, prev.Y));
            var br = PointFromScreen(new Point(prev.X + prev.Width, prev.Y + prev.Height));
            Place(Previous, tl, br, _shape);
            Previous.Visibility = Visibility.Visible;
        }
    }

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _startDip = e.GetPosition(Root);
        GetCursorPos(out _startPx);
        Root.CaptureMouse();

        Place(Selection, _startDip, _startDip, _shape);
        Selection.Visibility = Visibility.Visible;
    }

    private void Root_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragging)
            Place(Selection, _startDip, e.GetPosition(Root), _shape);
    }

    private void Root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        Root.ReleaseMouseCapture();

        GetCursorPos(out var endPx);
        var region = new Region(
            Math.Min(_startPx.X, endPx.X),
            Math.Min(_startPx.Y, endPx.Y),
            Math.Abs(endPx.X - _startPx.X),
            Math.Abs(endPx.Y - _startPx.Y),
            _shape);

        if (!region.IsValid)
        {
            Selection.Visibility = Visibility.Collapsed;
            HintText.Text = $"範囲が小さすぎます（{Region.MinSize}px 以上）。もう一度ドラッグしてください。\nEsc / 右クリックでキャンセル";
            return;
        }

        Result = region;
        DialogResult = true;
    }

    private void Root_MouseRightButtonUp(object sender, MouseButtonEventArgs e) => DialogResult = false;

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) DialogResult = false;
    }

    /// <summary>2点を対角とする位置に図形を置く。楕円は角丸を最大にして表現する。</summary>
    private static void Place(Rectangle shape, Point a, Point b, RegionShape kind)
    {
        double x = Math.Min(a.X, b.X), y = Math.Min(a.Y, b.Y);
        double w = Math.Abs(b.X - a.X), h = Math.Abs(b.Y - a.Y);
        Canvas.SetLeft(shape, x);
        Canvas.SetTop(shape, y);
        shape.Width = w;
        shape.Height = h;
        shape.RadiusX = kind == RegionShape.Ellipse ? w / 2 : 0;
        shape.RadiusY = kind == RegionShape.Ellipse ? h / 2 : 0;
    }
}
