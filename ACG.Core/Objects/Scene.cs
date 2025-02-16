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
        // Из 3D в 2D
        var projection = Camera.GetProjectionMatrix();
        // Из координат - в экранные пиксели
        var viewport = GetViewportMatrix();
    
        foreach (var model in Models)
        {
            UpdateModelTransform(model, view, projection, viewport);
        }
    }
    
    // Сюда мы передаем все три матрицы, которые создали до этого, чтобы
    // вместе с мировой матрицей (которую тут посчитаем) рассчитать новое положение объекта
    private void UpdateModelTransform(
        ObjectModel model, 
        Matrix4x4 view, 
        Matrix4x4 projection, 
        Matrix4x4 viewport)
    {
        var world = Transformations.CreateWorldTransform(
            // Размер модели относительно её оригинала
            model.Scale,
            // Показывает ориентацию матрицы в пространстве
            // Поворот вокруг оси Y называется Yaw, вокруг X — Pitch, вокруг Z — Roll
            Matrix4x4.CreateFromYawPitchRoll(
                model.Rotation.Y, 
                model.Rotation.X, 
                model.Rotation.Z),
            // Перемещение модели в пространстве
            model.Translation);

        // Порядок перемножения важен!
        // Сначала мировая трансформация, затем камера, затем проекция,
        // затем преобразование в координаты экрана
        var finalTransform = world * view * projection * viewport;
        model.ApplyFinalTransformation(finalTransform, Camera);
    }
}