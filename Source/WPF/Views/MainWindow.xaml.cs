using System.Windows;
using System.Windows.Controls;
using WWSTE.WPF.Views;

namespace WWSTE.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Dictionary<MenuItem, UserControl> _views;

    public MainWindow()
    {
        InitializeComponent();

        _views = new Dictionary<MenuItem, UserControl>
        {
            [SimpleViewMenuItem] = new SimpleView(),
            [TwoDocsVerticalViewMenuItem] = new TwoDocsVerticalView(),
            [TwoDocsHorizontalViewMenuItem] = new TwoDocsHorizontalView(),
            [TranslationUpdateViewMenuItem] = new TranslationUpdateView()
        };

        ShowView(SimpleViewMenuItem);
    }

    private void ViewMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ShowView((MenuItem)sender);
    }

    private void ShowView(MenuItem selectedMenuItem)
    {
        ViewContent.Content = _views[selectedMenuItem];

        foreach (MenuItem menuItem in _views.Keys)
        {
            menuItem.IsChecked = menuItem == selectedMenuItem;
        }
    }
}
