using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ACG.Core.ObjectParser;
using ACG.Core.ObjectRenderer;
using ACG.Core.Objects;
using Microsoft.WindowsAPICodePack.Dialogs;
using MessageBox = System.Windows.Forms.MessageBox;
using Vector = System.Windows.Vector;

namespace ACG.Views;

public class MainView : INotifyPropertyChanged
{
    public Scene Scene { get; set; } = new ();
    
    private Color _backgroundColor = Colors.White;

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            UpdateView();
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }
    
    private Color _foregroundColor = Colors.Purple;

    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            _foregroundColor = value;
            UpdateView();
            OnPropertyChanged(nameof(ForegroundColor));
        }
    }
    
    private Point _lastMousePos;
    
    private bool _isRightMousePressed = false;
    
    private float RotateSensitivity => MathF.PI / 360.0f;
    
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
    
    private RenderingType _renderingType;
    public RenderingType SelectedRenderingType
    {
        get => _renderingType;
        set
        {
            _renderingType = value;
            UpdateView();
            OnPropertyChanged(nameof(SelectedRenderingType));
        }
    }
    
    // Управление сценой 
    public ICommand LoadFileCommand { get; }
    
    public ICommand ClearSceneCommand { get; }
    
    public ICommand ChangeRenderingModeCommand { get; }
    
    // Изменение модели
    public ICommand MouseWheelCommand { get; }
    
    public ICommand MouseLeftButtonDownCommand { get; }
    
    public ICommand MouseMoveCommand { get; }
    
    // Изменение камеры
    public ICommand KeyDownCommand { get; }
    
    public MainView()
    {
        Scene.Camera = new Camera();
        
        Scene.CanvasWidth = 800;
        Scene.CanvasHeight = 600;
        
        LoadFileCommand = new Command(_ => LoadFile());
        ClearSceneCommand = new Command(_ => ClearScene());
        
        MouseWheelCommand = new Command(OnMouseWheel);
        MouseLeftButtonDownCommand = new Command(OnMouseLeftButtonDown);
        MouseMoveCommand = new Command(OnMouseMove);
        KeyDownCommand = new Command(OnKeyDown);
        
        ChangeRenderingModeCommand = new Command(param =>
        {
            if (param is RenderingType mode)
            {
                SelectedRenderingType = mode;
            }
        });
    }
    
    private void OnKeyDown(object? parameter)
    {
        if (parameter is KeyEventArgs e)
        {
            if (Scene.SelectedModel != null)
            {
                HandleModelKeyPress(e);
            }

            UpdateView();
            OnPropertyChanged(nameof(Scene));
        }
    }

    private void HandleModelKeyPress(KeyEventArgs e)
    {
        var step = Scene.SelectedModel!.GetOptimalTranslationStep();
        MoveModel(e.Key, step);
    }
    
    private void MoveModel(Key key, Vector3 step)
    {
        switch (key)
        {
            case Key.D:
                Scene.SelectedModel!.Translation += new Vector3(step.X, 0, 0);
                break;
            case Key.A:
                Scene.SelectedModel!.Translation += new Vector3(-step.X, 0, 0);
                break;
            case Key.W:
                Scene.SelectedModel!.Translation += new Vector3(0, step.Y, 0);
                break;
            case Key.S:
                Scene.SelectedModel!.Translation += new Vector3(0, -step.Y, 0);
                break;
        }
    }
    
    private void OnMouseLeftButtonDown(object? parameter)
    {
        if (parameter is MouseButtonEventArgs e)
        {
            
            _lastMousePos = e.GetPosition(null);
            if (e.OriginalSource is UIElement uiElement)
            {
                uiElement.Focus();
            }
        }
    }
    
    private void OnMouseMove(object? parameter)
    {
        if (parameter is MouseEventArgs e)
        {
            if (Scene.SelectedModel != null)
            {
                HandleModelRotation(e);
            }
        }
    }
    
    private void HandleModelRotation(MouseEventArgs e)
    {
        Point currentPos = e.GetPosition(null);
        Vector delta = currentPos - _lastMousePos;
        
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            RotateModelAroundItsAxis(delta);

            _lastMousePos = currentPos;
            UpdateView();
            OnPropertyChanged(nameof(Scene));
        }
    }
    
    private void RotateModelAroundItsAxis(Vector delta)
    {
        if (Scene.SelectedModel == null) 
            return;

        Scene.SelectedModel.Rotation = new Vector3(
            Scene.SelectedModel.Rotation.X,
            Scene.SelectedModel.Rotation.Y + (float)delta.X * RotateSensitivity,
            Scene.SelectedModel.Rotation.Z);
    }
     
    private void OnMouseWheel(object? parameter)
    {
        if (parameter is MouseWheelEventArgs e)
        {
            if (Scene.SelectedModel != null && 
                (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                if (e.Delta > 0)
                    Scene.SelectedModel.Scale += Scene.SelectedModel.Delta;
                else
                    Scene.SelectedModel.Scale -= Scene.SelectedModel.Delta;
            }
            else
            {
                Scene.Camera.Radius -= e.Delta / 1000.0f;
                if (Scene.Camera.Radius < Scene.Camera.ZNear)
                    Scene.Camera.Radius = Scene.Camera.ZNear;
                if (Scene.Camera.Radius > Scene.Camera.ZFar)
                    Scene.Camera.Radius = Scene.Camera.ZFar;
            }
            
            e.Handled = true;

            UpdateView();
            OnPropertyChanged(nameof(Scene));
        }
    }
    
    private void ClearScene()
    {
        Scene.Models.Clear();
        Scene.SelectedModel = null;
        Scene.Camera = new();
        UpdateView();
        OnPropertyChanged(nameof(Scene));
    }
    
    private void LoadFile()
    {
        using var dlg = new CommonOpenFileDialog();
        dlg.Filters.Add(new CommonFileDialogFilter("OBJ Files", "*.obj"));
        
        if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
        {
            try
            {
                var loadedModel = ObjectParser.Parse(dlg.FileName!, Scene.Camera);
                WriteableBitmap ??= new WriteableBitmap(
                    Scene.CanvasWidth, Scene.CanvasHeight, 96, 96, PixelFormats.Bgra32, null);

                Scene.Models.Add(loadedModel);
                Scene.SelectedModel = loadedModel;
                
                UpdateView();
                
                OnPropertyChanged(nameof(Scene));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading file: " + ex.Message);
            }
        }
    }

    private void UpdateView()
    {
        BaseRenderer.Render(
            Scene, 
            WriteableBitmap, 
            ForegroundColor, 
            BackgroundColor,
            SelectedRenderingType);
        
        OnPropertyChanged(nameof(WriteableBitmap));
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}