namespace DishAuto.Core;

public enum RegionShape
{
    Rectangle,
    Ellipse,
}

public enum PatternKind
{
    ZigzagHorizontal,
    ZigzagVertical,
    Spiral,
    Random,
    CircleScrub,
}

public enum MoveMode
{
    Absolute,
    Relative,
}

public enum StopReason
{
    UserStop,
    EscKey,
    TimeUp,
    FocusChanged,
    MouseMoved,
}

public static class StopReasonExtensions
{
    public static string ToJapanese(this StopReason reason) => reason switch
    {
        StopReason.UserStop => "停止ボタン / F10 で停止しました",
        StopReason.EscKey => "Esc キーで停止しました",
        StopReason.TimeUp => "制限時間に達したので停止しました",
        StopReason.FocusChanged => "前面のウィンドウが切り替わったので停止しました",
        StopReason.MouseMoved => "マウスが手で動かされたので停止しました",
        _ => reason.ToString(),
    };
}

public sealed record Option<T>(T Value, string Label);

/// <summary>コンボボックスの選択肢。</summary>
public static class Options
{
    public static IReadOnlyList<Option<RegionShape>> Shapes { get; } =
    [
        new(RegionShape.Rectangle, "矩形"),
        new(RegionShape.Ellipse, "楕円（丸い皿向け）"),
    ];

    public static IReadOnlyList<Option<PatternKind>> Patterns { get; } =
    [
        new(PatternKind.ZigzagHorizontal, "ジグザグ（横）"),
        new(PatternKind.ZigzagVertical, "ジグザグ（縦）"),
        new(PatternKind.Spiral, "渦巻き"),
        new(PatternKind.Random, "ランダム"),
        new(PatternKind.CircleScrub, "円を描いて擦る"),
    ];

    public static IReadOnlyList<Option<MoveMode>> MoveModes { get; } =
    [
        new(MoveMode.Absolute, "絶対座標（通常はこちら）"),
        new(MoveMode.Relative, "相対移動（絶対座標で反応しない場合）"),
    ];
}
