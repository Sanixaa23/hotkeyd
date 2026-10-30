using System.Windows;
using System.Windows.Input;
using Hotkeyd.Core;
using Microsoft.Win32;

namespace Hotkeyd.App;

/// <summary>
/// Adds or edits one binding. The hotkey is captured by listening for a key
/// press in the box rather than typed, so what is stored is exactly what the
/// user pressed; it is then run through the same parser the rest of the app
/// uses, and Save stays disabled until both the hotkey and a target are valid.
/// </summary>
public partial class BindingEditorWindow : Window
{
    private readonly string _id;
    private readonly bool _enabled;
    public Binding? Result { get; private set; }

    public BindingEditorWindow(Binding existing)
    {
        InitializeComponent();
        _id = existing.Id;
        _enabled = existing.Enabled;
        LabelBox.Text = existing.Label;
        HotkeyBox.Text = existing.HotkeyText;
        KindBox.SelectedIndex = (int)existing.Action.Kind;
        TargetBox.Text = existing.Action.Target;
        ArgsBox.Text = existing.Action.Arguments;
        UpdateKindUi();
        Validate(null, null);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Backdrop.Apply(this);
    }

    private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e) =>
        HotkeyHint.Text = "Şimdi bas…";

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        // A press of a modifier on its own is not a complete hotkey; wait for
        // a real key to arrive alongside it.
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        var mods = Modifiers.None;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) mods |= Modifiers.Control;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) mods |= Modifiers.Alt;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) mods |= Modifiers.Shift;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) mods |= Modifiers.Win;

        var code = (uint)KeyInterop.VirtualKeyFromKey(key);
        HotkeyBox.Text = new Hotkey(mods, new VirtualKey(code)).ToString();
        Validate(null, null);
    }

    private void KindBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateKindUi();
        Validate(null, null);
    }

    private void UpdateKindUi()
    {
        var kind = (ActionKind)KindBox.SelectedIndex;
        TargetLabel.Text = kind switch
        {
            ActionKind.LaunchOrFocus => "Program yolu",
            ActionKind.Open => "Dosya yolu veya bağlantı",
            ActionKind.Command => "Komut satırı",
            _ => "Hedef",
        };
        ArgsPanel.Visibility = kind == ActionKind.LaunchOrFocus ? Visibility.Visible : Visibility.Collapsed;
        BrowseButton.Visibility = kind == ActionKind.Command ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { CheckFileExists = true };
        if (dialog.ShowDialog(this) == true)
        {
            TargetBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(LabelBox.Text))
                LabelBox.Text = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private void Validate(object? sender, EventArgs? e)
    {
        var hotkeyOk = HotkeyParser.TryParse(HotkeyBox.Text, out _, out var error);
        var targetOk = !string.IsNullOrWhiteSpace(TargetBox.Text);
        SaveButton.IsEnabled = hotkeyOk && targetOk;
        if (!string.IsNullOrWhiteSpace(HotkeyBox.Text))
            HotkeyHint.Text = hotkeyOk ? "Geçerli" : error;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Result = new Binding
        {
            Id = _id,
            Label = LabelBox.Text.Trim(),
            HotkeyText = HotkeyBox.Text.Trim(),
            Enabled = _enabled,
            Action = new ActionSpec
            {
                Kind = (ActionKind)KindBox.SelectedIndex,
                Target = TargetBox.Text.Trim(),
                Arguments = ArgsBox.Text.Trim(),
            },
        };
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
