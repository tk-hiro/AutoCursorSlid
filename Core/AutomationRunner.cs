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
    private const double TickMs = 1.0;                 // 入力を送る間隔の目安
    private const double MaxCatchUpMs = 20.0;          // 処理が遅れたときに早送りで取り戻す上限
    private const double MouseMovedThresholdPx = 60.0; // これ以上ずれたら手で動かされたとみなす
    private const double SpaceHoldMs = 50.0;           // 間隔が短いときは間隔の半分まで縮める
    private const int RepressGapMs = 30;
    private const double KeepPressedIntervalMs = 100.0;
    private const double ForegroundCaptureDelayMs = 150.0;
    private const double StatusIntervalMs = 250.0;

    private readonly InputSender _input = new(config.MoveMode, config.ClickMethod);
    private readonly Stopwatch _clock = new();
    private CancellationToken _ct;
    private IProgress<string>? _progress;
    private nint _foreground;
    private PointD _lastSent;
    private double _segmentStartMs;
    private double _nextDetergentMs;
    private double _spaceReleaseMs = -1;
    private double _nextRepressMs = double.MaxValue;
    private double _nextKeepPressedMs = double.MaxValue;
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
            Thread.Sleep(10);
            if (config.HoldLeftButton)
            {
                if (PrepareTarget(current) is { } problem) return problem;
                _input.LeftDown();
                if (config.RepressIntervalSec > 0)
                    _nextRepressMs = config.RepressIntervalSec * 1000;
                if (config.KeepPressed)
                    _nextKeepPressedMs = KeepPressedIntervalMs;
            }

            _clock.Start();
            _nextDetergentMs = 0;

            while (path.MoveNext())
            {
                var target = path.Current;
                if (MoveAlong(current, target) is { } reason) return reason;
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

    /// <summary>
    /// 開始点の下にあるゲームウィンドウを確認し、クリックが届く状態にする。
    /// </summary>
    private StopReason? PrepareTarget(PointD start)
    {
        nint window = ProcessInfo.TopLevelWindowAt((int)Math.Round(start.X), (int)Math.Round(start.Y));
        if (window == 0) return null;

        NativeMethods.GetWindowThreadProcessId(window, out uint pid);
        if (pid == Environment.ProcessId) return StopReason.RegionCovered;

        // 管理者権限のウィンドウには、こちらも管理者でないとクリックが届かない
        if (ProcessInfo.IsWindowElevated(window) == true && !ProcessInfo.IsCurrentProcessElevated())
            return StopReason.TargetElevated;

        if (config.ClickMethod == ClickMethod.WindowMessage)
        {
            _input.AttachToWindowAt(start);
        }
        else if (NativeMethods.GetForegroundWindow() != window)
        {
            // 前面でないウィンドウは最初のクリックを前面化に使うことがあるため、先に一度クリックしておく
            InputSender.Click();
            Thread.Sleep(80);
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

    /// <summary>
    /// from から to へ動かす。位置は経過時間から決めるので、
    /// 入力の送信が遅れても設定どおりの速度になる。
    /// </summary>
    private StopReason? MoveAlong(PointD from, PointD to)
    {
        double durationMs = PointD.Distance(from, to) / config.SpeedPxPerSec * 1000.0;
        double startMs = Math.Max(_segmentStartMs, _clock.Elapsed.TotalMilliseconds - MaxCatchUpMs);
        double endMs = startMs + durationMs;
        _segmentStartMs = endMs;

        while (true)
        {
            if (CheckStop() is { } reason) return reason;

            double now = _clock.Elapsed.TotalMilliseconds;
            double t = durationMs <= 0 ? 1.0 : Math.Min(1.0, (now - startMs) / durationMs);
            var p = PointD.Lerp(from, to, t);
            _input.MoveTo(p);
            _lastSent = p;

            Tick();
            if (t >= 1.0) return null;
            WaitUntil(Math.Min(endMs, now + TickMs));
        }
    }

    private StopReason? CheckStop()
    {
        if (_ct.IsCancellationRequested) return StopReason.UserStop;
        if (InputSender.IsEscPressed()) return StopReason.EscKey;

        double now = _clock.Elapsed.TotalMilliseconds;
        if (config.TimeLimitSec > 0 && now >= config.TimeLimitSec * 1000.0) return StopReason.TimeUp;

        if (config.StopOnFocusChange)
        {
            // クリックでゲームが前面になるのを少し待ってから、前面ウィンドウを記録する
            if (_foreground == 0)
            {
                if (now >= ForegroundCaptureDelayMs)
                    _foreground = NativeMethods.GetForegroundWindow();
            }
            else if (NativeMethods.GetForegroundWindow() != _foreground)
            {
                return StopReason.FocusChanged;
            }
        }

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
                _spaceReleaseMs = now + Math.Min(SpaceHoldMs, config.DetergentIntervalSec * 1000 / 2);
                _nextDetergentMs = now + config.DetergentIntervalSec * 1000;
            }
        }

        if (now >= _nextRepressMs)
        {
            _input.LeftUp();
            Thread.Sleep(RepressGapMs);
            _input.LeftDown();
            _nextRepressMs = now + config.RepressIntervalSec * 1000;
        }
        else if (now >= _nextKeepPressedMs)
        {
            // ゲーム側で押下が外れたと判定されても、押した状態に戻す
            _input.LeftDown();
            _nextKeepPressedMs = now + KeepPressedIntervalMs;
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
            if (remain > 1.5) Thread.Sleep(1);
            else Thread.SpinWait(20);
        }
    }
}
