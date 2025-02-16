using System.Numerics;
using System.Windows;

namespace ACG.Core.Objects;

public class Scene
{
    public int CanvasWidth { get; set; }
    public int CanvasHeight { get; set; }
    
    public List<ObjectModel> Models { get; } = [];
    
    public Camera Camera { get; set; } = new();
    
    public ObjectModel? SelectedModel { get; set; }
}