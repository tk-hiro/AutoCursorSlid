namespace DishAuto.Core;

/// <summary>1回の実行で使う設定のスナップショット。</summary>
public sealed record RunConfig(
    Region Region,
    PatternKind Pattern,
    MoveMode MoveMode,
    bool HoldLeftButton,
    ClickMethod ClickMethod,
    bool KeepPressed,
    double SpeedPxPerSec,
    double Spacing,
    bool DetergentEnabled,
    double DetergentIntervalSec,
    double RepressIntervalSec,
    int TimeLimitSec,
    int CountdownSec,
    bool StopOnUserMouse,
    bool StopOnFocusChange);
