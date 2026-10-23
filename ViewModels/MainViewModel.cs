using DishAuto.Settings;

namespace DishAuto.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private string _status = "待機中  範囲を選択して F9 で開始";
    private bool _isRunning;

    public MainViewModel(AppSettings settings)
    {
        Settings = settings;
        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName?.StartsWith("Region", StringComparison.Ordinal) == true)
                OnPropertyChanged(nameof(RegionText));
        };
    }

    public AppSettings Settings { get; }

    public string Status { get => _status; set => SetField(ref _status, value); }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetField(ref _isRunning, value))
                OnPropertyChanged(nameof(IsIdle));
        }
    }

    public bool IsIdle => !IsRunning;

    public string RegionText
    {
        get
        {
            var r = Settings.ToRegion();
            return r.IsValid ? r.ToString() : "未選択";
        }
    }
}
