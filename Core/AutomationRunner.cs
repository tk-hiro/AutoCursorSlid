using System.Diagnostics;
using DishAuto.Core.Patterns;
using DishAuto.Native;

namespace DishAuto.Core;

/// <summary>
/// 範囲内をパターンに沿って動かし（設定により左ボタンを押したまま）、一定間隔でスペースを押す。
/// 専用スレッドで <see cref="Run"/> を呼び出すこと。
/// </summary>
public sealed class AutomationRunner(RunConfig config)
{
    private const double StepPx = 3.0;                 // 1回の移動量。大きすぎると擦った判定にならない
    private const double MouseMovedThresholdPx = 60.0; // これ以上ずれたら手で動かされたとみなす
    private const double SpaceHoldMs = 50.0;
    private const int RepressGapMs = 30;
    private const double StatusIntervalMs = 250.0;

    private readonly InputSender _input = new(config.MoveMode);
    private readonly Stopwatch _clock = new();
    private CancellationToken _ct;
    private IProgress<string>? _progress;
    private nint _foreground;
    private PointD _lastSent;
    private double _nextDetergentMs;
    private double _spaceReleaseMs = -1;
    private double _nextRepressMs = double.MaxValue;
    private double _nextStatusMs;

    public StopReason Run(CancellationToken ct, IProgress<string>? progress)
    {
        _ct = ct;
        _progress = progress;
        NativeMethods.timeBeginPeriod(1);
        try
        {
            if (Countdown() is { } cancelled) return cancelled;

            using var path = InfinitePath().GetEnumerator();
            path.MoveNext();
            var current = path.Current;

            // 押す前に開始点へ移動しておく（範囲外からドラッグしないように）
            _input.MoveTo(current);
            _lastSent = current;
            Thread.Sleep(30);
            if (config.HoldLeftButton)
                InputSender.LeftDown();

            // クリックでゲームが前面になるのを待ってから前面ウィンドウを記録する
            Thread.Sleep(100);
            _foreground = NativeMethods.GetForegroundWindow();

            _clock.Start();
            _nextDetergentMs = 0;
            if (config.HoldLeftButton && config.RepressIntervalSec > 0)
                _nextRepressMs = config.RepressIntervalSec * 1000;

            while (path.MoveNext())
            {
                var target = path.Current;
                if (MoveSmoothly(current, target) is { } reason) return reason;
                current = target;
            }
            return StopReason.UserStop;
        }
        finally
        {
            InputSender.EmergencyRelease();
            NativeMethods.timeEndPeriod(1);
        }
    }

    private StopReason? Countdown()
    {
        var sw = Stopwatch.StartNew();
        double totalMs = config.CountdownSec * 1000.0;
        int shown = -1;
        while (sw.Elapsed.TotalMilliseconds < totalMs)
        {
            if (_ct.IsCancellationRequested) return StopReason.UserStop;
            if (InputSender.IsEscPressed()) return StopReason.EscKey;

            int remain = (int)Math.Ceiling((totalMs - sw.Elapsed.TotalMilliseconds) / 1000.0);
            if (remain != shown)
            {
                _progress?.Report($"開始まで {remain} 秒… ゲーム画面を前面にしてください");
                shown = remain;
            }
            Thread.Sleep(20);
        }
        return null;
    }

    private IEnumerable<PointD> InfinitePath()
    {
        var pattern = PatternFactory.Create(config.Pattern);
        var rng = new Random();
        while (true)
        {
            int count = 0;
            foreach (var p in pattern.Generate(config.Region, config.Spacing, rng))
            {
                count++;
                yield return config.Region.Clamp(p);
            }
            if (count == 0) yield return config.Region.Center;
        }
    }

    /// <summary>from から to へ、速度設定に合わせて少しずつ動かす。</summary>
    private StopReason? MoveSmoothly(PointD from, PointD to)
    {
        double distance = PointD.Distance(from, to);
        int steps = Math.Max(1, (int)Math.Ceiling(distance / StepPx));
        double stepMs = distance / config.SpeedPxPerSec * 1000.0 / steps;
        double startMs = _clock.Elapsed.TotalMilliseconds;

        for (int i = 1; i <= steps; i++)
        {
            if (CheckStop() is { } reason) return reason;

            var p = PointD.Lerp(from, to, (double)i / steps);
            _input.MoveTo(p);
            _lastSent = p;

            Tick();
            WaitUntil(startMs + stepMs * i);
        }
        return null;
    }

    private StopReason? CheckStop()
    {
        if (_ct.IsCancellationRequested) return StopReason.UserStop;
        if (InputSender.IsEscPressed()) return StopReason.EscKey;
        if (config.TimeLimitSec > 0 && _clock.Elapsed.TotalSeconds >= config.TimeLimitSec) return StopReason.TimeUp;
        if (config.StopOnFocusChange && NativeMethods.GetForegroundWindow() != _foreground) return StopReason.FocusChanged;
        if (config.StopOnUserMouse && PointD.Distance(InputSender.GetCursor(), _lastSent) > MouseMovedThresholdPx)
            return StopReason.MouseMoved;
        return null;
    }

    /// <summary>洗剤・押し直し・状態表示など、時間で発生する処理。</summary>
    private void Tick()
    {
        double now = _clock.Elapsed.TotalMilliseconds;

        if (config.DetergentEnabled)
        {
            if (_spaceReleaseMs >= 0 && now >= _spaceReleaseMs)
            {
                InputSender.SpaceUp();
                _spaceReleaseMs = -1;
            }
            if (now >= _nextDetergentMs)
            {
                InputSender.SpaceDown();
                _spaceReleaseMs = now + SpaceHoldMs;
                _nextDetergentMs = now + config.DetergentIntervalSec * 1000;
            }
        }

        if (now >= _nextRepressMs)
        {
            InputSender.LeftUp();
            Thread.Sleep(RepressGapMs);
            InputSender.LeftDown();
            _nextRepressMs = now + config.RepressIntervalSec * 1000;
        }

        if (now >= _nextStatusMs)
        {
            ReportStatus(now);
            _nextStatusMs = now + StatusIntervalMs;
        }
    }

    private void ReportStatus(double nowMs)
    {
        var text = $"実行中  経過 {nowMs / 1000:0} 秒";
        if (config.TimeLimitSec > 0)
            text += $" / 残り {Math.Max(0, config.TimeLimitSec - nowMs / 1000):0} 秒";
        if (config.DetergentEnabled)
            text += $"  洗剤まで {Math.Max(0, (_nextDetergentMs - nowMs) / 1000):0.0} 秒";
        _progress?.Report(text);
    }

    private void WaitUntil(double targetMs)
    {
        while (true)
        {
            double remain = targetMs - _clock.Elapsed.TotalMilliseconds;
            if (remain <= 0) return;
            if (remain > 2) Thread.Sleep(1);
            else Thread.SpinWait(50);
        }
    }
}
