using System.Windows;
using System.Windows.Interop;
using Hotkeyd.Core;

namespace Hotkeyd.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private HotkeyHost? _host;

    public MainWindow(ConfigStore store, Config config)
    {
        InitializeComponent();
        _vm = new MainViewModel(store, config);
        _vm.EditRequested += OnEditRequested;
        DataContext = _vm;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // The host needs a real window handle to receive WM_HOTKEY, which
        // only exists once the source is initialised.
        Backdrop.Apply(this);
        var source = (HwndSource)PresentationSource.FromVisual(this)!;
        _host = new HotkeyHost(source);
        _vm.AttachHost(_host);
    }

    private Binding? OnEditRequested(Binding binding)
    {
        var editor = new BindingEditorWindow(binding) { Owner = this };
        return editor.ShowDialog() == true ? editor.Result : null;
    }

    protected override void OnClosed(EventArgs e)
    {
        _host?.Dispose();
        base.OnClosed(e);
    }
}
