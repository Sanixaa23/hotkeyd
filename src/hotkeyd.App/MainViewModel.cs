using System.Collections.ObjectModel;
using Hotkeyd.Core;

namespace Hotkeyd.App;

/// <summary>
/// The dashboard's state. It owns the list of bindings, the master switch and
/// the login-startup setting, and it is the one place that turns the list into
/// a registration plan and applies it.
///
/// Every change flows through <see cref="Recompute"/>: rebuild the plan, show
/// each row its verdict, and -- when the master switch is on -- hand the ready
/// rows to the OS and fold back which the OS actually accepted. So the badges
/// never claim a hotkey is live unless Windows said yes, and the config is
/// saved on every change so a crash loses nothing.
/// </summary>
public sealed class MainViewModel : Observable
{
    private readonly ConfigStore _store;
    private HotkeyHost? _host;
    private bool _masterOn = true;
    private bool _startupOn;
    private string _summary = "";

    public ObservableCollection<BindingRowVM> Rows { get; } = new();

    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }

    /// <summary>Raised when a row needs the editor dialog (add or edit).</summary>
    public event Func<Binding, Binding?>? EditRequested;

    public MainViewModel(ConfigStore store, Config config)
    {
        _store = store;
        foreach (var binding in config.Bindings)
            Rows.Add(new BindingRowVM(binding, _ => Recompute()));

        _startupOn = Startup.IsEnabled();

        AddCommand = new RelayCommand(_ => Add());
        EditCommand = new RelayCommand(row => Edit((BindingRowVM)row!), row => row is not null);
        DeleteCommand = new RelayCommand(row => Delete((BindingRowVM)row!), row => row is not null);
    }

    public bool MasterOn
    {
        get => _masterOn;
        set { if (Set(ref _masterOn, value)) Recompute(); }
    }

    public bool StartupOn
    {
        get => _startupOn;
        set
        {
            if (!Set(ref _startupOn, value)) return;
            try { Startup.Set(value); }
            catch (Exception error) { _startupOn = !value; Raise(); ShowError(error.Message); }
        }
    }

    public string Summary { get => _summary; private set => Set(ref _summary, value); }

    public string? LastError { get; private set; }

    /// <summary>Called by the window once its handle exists.</summary>
    public void AttachHost(HotkeyHost host)
    {
        _host = host;
        host.Fired += OnFired;
        Recompute();
    }

    private void Add()
    {
        var created = EditRequested?.Invoke(new Binding());
        if (created is null) return;
        Rows.Add(new BindingRowVM(created, _ => Recompute()));
        Recompute();
    }

    private void Edit(BindingRowVM row)
    {
        var edited = EditRequested?.Invoke(row.Binding);
        if (edited is null) return;
        row.Replace(edited);
        Recompute();
    }

    private void Delete(BindingRowVM row)
    {
        Rows.Remove(row);
        Recompute();
    }

    private void Recompute()
    {
        var plan = RegistrationPlan.Build(Rows.Select(r => r.Binding));

        // Which rows the OS accepted, when the master switch is on.
        var accepted = new HashSet<string>();
        if (_masterOn && _host is not null)
        {
            foreach (var result in _host.Apply(plan))
                if (result.Ok) accepted.Add(result.Binding.Id);
        }
        else
        {
            _host?.Clear();
        }

        var byId = plan.ToDictionary(p => p.Binding.Id);
        int active = 0, conflict = 0, invalid = 0;
        foreach (var row in Rows)
        {
            var p = byId[row.Binding.Id];
            var osActive = _masterOn && accepted.Contains(row.Binding.Id);
            var detail = p.Detail;
            if (p.State == BindingState.Ready && _masterOn && !osActive)
                detail = "Windows bu kısayolu reddetti (başka program kullanıyor olabilir)";
            row.ShowState(p.State, detail, osActive);

            if (osActive) active++;
            else if (p.State == BindingState.Conflict) conflict++;
            else if (p.State == BindingState.Invalid) invalid++;
        }

        Summary = _masterOn
            ? $"{active} etkin · {conflict} çakışma · {invalid} geçersiz"
            : "tümü duraklatıldı";

        Save();
    }

    private void OnFired(Binding binding)
    {
        var (ok, message) = ActionRunner.Run(binding.Action);
        var name = string.IsNullOrWhiteSpace(binding.Label) ? binding.HotkeyText : binding.Label;
        Summary = ok ? $"{name}: {message}" : $"{name}: HATA {message}";
    }

    private void Save()
    {
        try { _store.Save(new Config { Bindings = Rows.Select(r => r.Binding).ToList() }); }
        catch (Exception error) { ShowError("kaydedilemedi: " + error.Message); }
    }

    private void ShowError(string message)
    {
        LastError = message;
        Summary = "HATA: " + message;
    }
}
