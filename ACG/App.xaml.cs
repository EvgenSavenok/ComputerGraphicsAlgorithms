using System.Configuration;
using System.Data;
using System.Windows;

namespace ACG;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        Environment.Exit(0); 
    }

}