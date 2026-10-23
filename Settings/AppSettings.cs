using DishAuto.Core;
using DishAuto.ViewModels;

namespace DishAuto.Settings;

/// <summary>画面で編集し、JSON に保存する設定。</summary>
public sealed class AppSettings : ObservableObject
{
    private int _regionX, _regionY, _regionWidth, _regionHeight;
    private RegionShape _shape = RegionShape.Ellipse;
    private PatternKind _pattern = PatternKind.ZigzagHorizontal;
    private MoveMode _moveMode = MoveMode.Absolute;
    private bool _holdLeftButton = true;
    private double _speedPxPerSec = 1500;
    private int _spacing = 20;
    private bool _detergentEnabled = true;
    private double _detergentIntervalSec = 5.0;
    private double _repressIntervalSec;
    private int _timeLimitSec = 60;
    private int _countdownSec = 3;
    private bool _stopOnUserMouse = true;
    private bool _stopOnFocusChange = true;
    private bool _minimizeWhileRunning = true;

    public int RegionX { get => _regionX; set => SetField(ref _regionX, value); }
    public int RegionY { get => _regionY; set => SetField(ref _regionY, value); }
    public int RegionWidth { get => _regionWidth; set => SetField(ref _regionWidth, value); }
    public int RegionHeight { get => _regionHeight; set => SetField(ref _regionHeight, value); }
    public RegionShape Shape { get => _shape; set => SetField(ref _shape, value); }
    public PatternKind Pattern { get => _pattern; set => SetField(ref _pattern, value); }
    public MoveMode MoveMode { get => _moveMode; set => SetField(ref _moveMode, value); }
    public bool HoldLeftButton { get => _holdLeftButton; set => SetField(ref _holdLeftButton, value); }
    public double SpeedPxPerSec { get => _speedPxPerSec; set => SetField(ref _speedPxPerSec, Math.Round(value)); }
    public int Spacing { get => _spacing; set => SetField(ref _spacing, value); }
    public bool DetergentEnabled { get => _detergentEnabled; set => SetField(ref _detergentEnabled, value); }
    public double DetergentIntervalSec { get => _detergentIntervalSec; set => SetField(ref _detergentIntervalSec, value); }
    public double RepressIntervalSec { get => _repressIntervalSec; set => SetField(ref _repressIntervalSec, value); }
    public int TimeLimitSec { get => _timeLimitSec; set => SetField(ref _timeLimitSec, value); }
    public int CountdownSec { get => _countdownSec; set => SetField(ref _countdownSec, value); }
    public bool StopOnUserMouse { get => _stopOnUserMouse; set => SetField(ref _stopOnUserMouse, value); }
    public bool StopOnFocusChange { get => _stopOnFocusChange; set => SetField(ref _stopOnFocusChange, value); }
    public bool MinimizeWhileRunning { get => _minimizeWhileRunning; set => SetField(ref _minimizeWhileRunning, value); }

    public Region ToRegion() => new(RegionX, RegionY, RegionWidth, RegionHeight, Shape);

    public void SetRegion(Region region)
    {
        RegionX = region.X;
        RegionY = region.Y;
        RegionWidth = region.Width;
        RegionHeight = region.Height;
    }

    /// <summary>範囲外の値を丸めた実行用設定を作る。</summary>
    public RunConfig ToRunConfig() => new(
        Region: ToRegion(),
        Pattern: Pattern,
        MoveMode: MoveMode,
        HoldLeftButton: HoldLeftButton,
        SpeedPxPerSec: Math.Clamp(SpeedPxPerSec, 50, 20000),
        Spacing: Math.Clamp(Spacing, 3, 500),
        DetergentEnabled: DetergentEnabled,
        DetergentIntervalSec: Math.Max(0.2, DetergentIntervalSec),
        RepressIntervalSec: Math.Max(0, RepressIntervalSec),
        TimeLimitSec: Math.Max(0, TimeLimitSec),
        CountdownSec: Math.Clamp(CountdownSec, 0, 60),
        StopOnUserMouse: StopOnUserMouse,
        StopOnFocusChange: StopOnFocusChange);
}
