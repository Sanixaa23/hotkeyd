using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Hotkeyd.Core;

namespace Hotkeyd.App;

public abstract class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Raise(name);
        return true;
    }
}

/// <summary>
/// One row in the list. It wraps an immutable <see cref="Binding"/> and
/// exposes its parts for display; an edit swaps in a whole new binding rather
/// than mutating the record, which keeps the record's value-equality honest.
/// The state (ready, conflict, invalid, off) and its colour are pushed in by
/// the parent after it builds the plan, so a row never decides its own
/// verdict in isolation from the others.
/// </summary>
public sealed class BindingRowVM : Observable
{
    private readonly Action<BindingRowVM> _onEnabledChanged;
    private string _stateText = "";
    private Brush _stateBrush = Brushes.Gray;
    private string _detail = "";

    public BindingRowVM(Binding binding, Action<BindingRowVM> onEnabledChanged)
    {
        Binding = binding;
        _onEnabledChanged = onEnabledChanged;
    }

    public Binding Binding { get; private set; }

    public string Label => string.IsNullOrWhiteSpace(Binding.Label)
        ? "(adsız)" : Binding.Label;
    public string HotkeyText => Binding.HotkeyText;
    public string ActionDescription => Binding.Action.Describe();

    public bool Enabled
    {
        get => Binding.Enabled;
        set
        {
            if (Binding.Enabled == value) return;
            Binding = Binding with { Enabled = value };
            Raise();
            _onEnabledChanged(this);
        }
    }

    public string StateText { get => _stateText; private set => Set(ref _stateText, value); }
    public Brush StateBrush { get => _stateBrush; private set => Set(ref _stateBrush, value); }
    public string Detail { get => _detail; private set => Set(ref _detail, value); }
    public bool HasDetail => !string.IsNullOrEmpty(_detail);

    public void Replace(Binding binding)
    {
        Binding = binding;
        Raise(nameof(Label));
        Raise(nameof(HotkeyText));
        Raise(nameof(ActionDescription));
        Raise(nameof(Enabled));
    }

    public void ShowState(BindingState state, string detail, bool osActive)
    {
        (StateText, StateBrush) = state switch
        {
            BindingState.Ready when osActive => ("etkin", Green),
            BindingState.Ready => ("kayıt reddedildi", Red),
            BindingState.Disabled => ("kapalı", Gray),
            BindingState.Invalid => ("geçersiz", Amber),
            BindingState.Conflict => ("çakışma", Red),
            _ => ("?", Gray),
        };
        Detail = detail;
        Raise(nameof(HasDetail));
    }

    // Apple's system hues, matching the theme.
    private static readonly Brush Green = Freeze("#34C759");
    private static readonly Brush Red = Freeze("#FF3B30");
    private static readonly Brush Amber = Freeze("#FF9F0A");
    private static readonly Brush Gray = Freeze("#8E8E93");

    private static Brush Freeze(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
