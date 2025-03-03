using System.Numerics;
using System.Windows;
using ACG.Core.VectorTransformations;

namespace ACG.Core.Objects;

public class Scene
{
    public int CanvasWidth { get; set; }
    public int CanvasHeight { get; set; }
    
    public List<ObjectModel> Models { get; } = [];
    
    public Camera Camera { get; set; } = new();
    
    public ObjectModel? SelectedModel { get; set; }
    
    public Matrix4x4 GetViewportMatrix() =>
        Transformations.CreateViewportMatrix(CanvasWidth, CanvasHeight);
    
    public void UpdateAllModels()
    {
        // Из мировых координат - в координаты вида камеры
        var view = Camera.GetViewMatrix();
        // Из 3D в нормализованные координаты (где диапазон от -1 до 1)
        var projection = Camera.GetProjectionMatrix();
        // Из координат - в экранные пиксели (в пространство окна просмотра)
        var viewport = GetViewportMatrix();
    
        foreach (var model in Models)
        {
            UpdateModelTransform(model, view, projection, viewport);
        }
    }
    
    private void UpdateModelTransform(
        ObjectModel model, 
        Matrix4x4 view, 
        Matrix4x4 projection, 
        Matrix4x4 viewport)
    {
        var world = Transformations.CreateWorldTransform(
            model.Scale,
            // Показывает ориентацию матрицы в пространстве
            // Поворот вокруг оси Y называется Yaw, вокруг X — Pitch, вокруг Z — Roll
            Matrix4x4.CreateFromYawPitchRoll(
                model.Rotation.Y, 
                model.Rotation.X, 
                model.Rotation.Z),
            // Перемещение модели в пространстве
            model.Translation);
        
        var finalTransform = world * view * projection * viewport;
        model.ApplyFinalTransformation(finalTransform, Camera);
    }
}