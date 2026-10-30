using Hotkeyd.Core;
using Xunit;

namespace Hotkeyd.Tests;

public class PlanTests
{
    private static Binding Bind(string hotkey, bool enabled = true, string label = "") =>
        new() { HotkeyText = hotkey, Enabled = enabled, Label = label };

    [Fact]
    public void A_clean_set_is_all_ready()
    {
        var plan = RegistrationPlan.Build(new[]
        {
            Bind("Ctrl+Alt+1"), Bind("Ctrl+Alt+2"), Bind("Win+D"),
        });
        Assert.All(plan, row => Assert.Equal(BindingState.Ready, row.State));
        Assert.All(plan, row => Assert.NotNull(row.Hotkey));
    }

    [Fact]
    public void The_first_claimant_keeps_the_combination_and_the_rest_conflict()
    {
        var plan = RegistrationPlan.Build(new[]
        {
            Bind("Ctrl+Alt+K", label: "first"),
            Bind("ctrl+alt+k", label: "second"),
            Bind("Alt+Ctrl+K", label: "third"),
        });
        Assert.Equal(BindingState.Ready, plan[0].State);
        Assert.Equal(BindingState.Conflict, plan[1].State);
        Assert.Equal(BindingState.Conflict, plan[2].State);
        // The conflict names the owner so the UI can point at it.
        Assert.Contains("first", plan[1].Detail);
    }

    [Fact]
    public void A_disabled_binding_does_not_claim_its_combination()
    {
        var plan = RegistrationPlan.Build(new[]
        {
            Bind("Ctrl+Alt+K", enabled: false),
            Bind("Ctrl+Alt+K", enabled: true),
        });
        Assert.Equal(BindingState.Disabled, plan[0].State);
        // Because the first is off, the second is free to take the combination.
        Assert.Equal(BindingState.Ready, plan[1].State);
    }

    [Fact]
    public void An_unparseable_binding_is_invalid_not_registered()
    {
        var plan = RegistrationPlan.Build(new[] { Bind("Ctrl+Nope") });
        Assert.Equal(BindingState.Invalid, plan[0].State);
        Assert.Null(plan[0].Hotkey);
        Assert.NotEqual("", plan[0].Detail);
    }

    [Fact]
    public void An_invalid_binding_never_blocks_a_later_valid_one()
    {
        var plan = RegistrationPlan.Build(new[]
        {
            Bind("garbage"), Bind("Ctrl+Alt+K"),
        });
        Assert.Equal(BindingState.Invalid, plan[0].State);
        Assert.Equal(BindingState.Ready, plan[1].State);
    }
}
