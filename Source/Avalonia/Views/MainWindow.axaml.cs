using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace WWSTE.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MiLight_Click(object? sender, RoutedEventArgs e)
        => Application.Current!.RequestedThemeVariant = ThemeVariant.Light;

    private void MiDark_Click(object? sender, RoutedEventArgs e)
        => Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;

    private void MiSystem_Click(object? sender, RoutedEventArgs e)
        => Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
}
