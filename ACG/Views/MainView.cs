using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ACG.Core.ObjectParser;
using ACG.Core.Objects;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace ACG.Views;

public class MainView : INotifyPropertyChanged
{
    public Scene Scene { get; set; } = new ();
    
    public event PropertyChangedEventHandler? PropertyChanged;
    private WriteableBitmap? _writeableBitmap;
    
    public WriteableBitmap? WriteableBitmap
    {
        get => _writeableBitmap;
        set
        {
            _writeableBitmap = value;
            OnPropertyChanged(nameof(WriteableBitmap));
        }
    }
    
    public ICommand LoadFileCommand { get; }

    public MainView()
    {
        LoadFileCommand = new Command(_ => LoadFile());
    }
    
    private void LoadFile()
    {
        using var dlg = new CommonOpenFileDialog();
        dlg.Filters.Add(new CommonFileDialogFilter("OBJ Files", "*.obj"));
        if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
        {
            try
            {
                var loadedModel = ObjectParser.Parse(dlg.FileName!);
                WriteableBitmap ??= new WriteableBitmap(
                    Scene.CanvasWidth, Scene.CanvasHeight, 96, 96, PixelFormats.Bgra32, null);

                Scene.Models.Add(loadedModel);
                Scene.SelectedModel = loadedModel;
                
                OnPropertyChanged(nameof(Scene));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки файла: " + ex.Message);
            }
        }
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}