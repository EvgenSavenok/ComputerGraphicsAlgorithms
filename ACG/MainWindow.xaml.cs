using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ACG.Views;

namespace ACG;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainView vm)
        {
            vm.Scene.CanvasHeight = (int)ImagePanel.ActualHeight;
            vm.Scene.CanvasWidth = (int)ImagePanel.ActualWidth;
        }
    }
}