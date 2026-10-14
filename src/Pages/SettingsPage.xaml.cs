using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CoopWarrior.Pages;

public sealed partial class SettingsPage : Page
{
    readonly bool loading;

    public SettingsPage()
    {
        loading = true;
        InitializeComponent();
        MinimizeToggle.IsOn = App.Store.MinimizeToTray;
        CloseToggle.IsOn = App.Store.CloseToTray;
        loading = false;
    }

    void Toggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (loading) return;
        App.Store.MinimizeToTray = MinimizeToggle.IsOn;
        App.Store.CloseToTray = CloseToggle.IsOn;
        App.Store.SaveSettings();
        App.Window?.UpdateTray();
    }
}
