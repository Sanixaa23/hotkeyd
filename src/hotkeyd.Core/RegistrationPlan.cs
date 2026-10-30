namespace Hotkeyd.Core;

/// <summary>Why a binding will or will not be registered with the OS.</summary>
public enum BindingState
{
    /// <summary>Parses, enabled, and no other enabled binding shares its combination.</summary>
    Ready,
    /// <summary>Switched off by the user; not registered, not a problem.</summary>
    Disabled,
    /// <summary>The hotkey text does not parse; <see cref="PlanRow.Detail"/> says why.</summary>
    Invalid,
    /// <summary>Another enabled binding claims the same combination first.</summary>
    Conflict,
}

/// <summary>One binding's verdict within a plan.</summary>
public sealed record PlanRow(Binding Binding, BindingState State, string Detail)
{
    /// <summary>The parsed hotkey when <see cref="State"/> is Ready, else null.</summary>
    public Hotkey? Hotkey { get; init; }
}

/// <summary>
/// The decision, made before the operating system is touched, of which
/// bindings may be registered. It is pure: the same bindings always give the
/// same plan, so it can be tested without a keyboard or a window.
///
/// The rule is fail-closed. Two enabled bindings on the same combination do
/// not race for it and do not both half-work; the first in order keeps it and
/// the rest are marked <see cref="BindingState.Conflict"/> and left
/// unregistered. The OS allows only one owner per combination anyway, so
/// deciding it here makes the reason visible instead of letting the second
/// registration fail with a bare error code.
/// </summary>
public static class RegistrationPlan
{
    public static IReadOnlyList<PlanRow> Build(IEnumerable<Binding> bindings)
    {
        var rows = new List<PlanRow>();
        var claimed = new Dictionary<Hotkey, Binding>();

        foreach (var binding in bindings)
        {
            if (!binding.Enabled)
            {
                rows.Add(new PlanRow(binding, BindingState.Disabled, "kapalı"));
                continue;
            }

            var hotkey = binding.Parse(out var error);
            if (hotkey is null)
            {
                rows.Add(new PlanRow(binding, BindingState.Invalid, error));
                continue;
            }

            if (claimed.TryGetValue(hotkey.Value, out var owner))
            {
                var name = string.IsNullOrEmpty(owner.Label)
                    ? owner.HotkeyText
                    : owner.Label;
                rows.Add(new PlanRow(binding, BindingState.Conflict,
                    $"'{hotkey}' zaten '{name}' tarafından kullanılıyor"));
                continue;
            }

            claimed[hotkey.Value] = binding;
            rows.Add(new PlanRow(binding, BindingState.Ready, "")
            {
                Hotkey = hotkey,
            });
        }

        return rows;
    }
}
