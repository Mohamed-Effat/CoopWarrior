using Microsoft.UI.Xaml.Controls;

namespace CoopWarrior.Pages;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        Version? v = typeof(App).Assembly.GetName().Version;
        VersionText.Text = v == null ? "" : $"Version {v.Major}.{v.Minor}.{v.Build}";
    }
}
