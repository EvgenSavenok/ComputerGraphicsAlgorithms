using System.Numerics;
using ACG.Core.ObjectParser;

namespace ACG.Core.Objects;

public class ObjectModel
{
    private float _scale;
    public List<Vector4> SourceVertices { get; } = [];
    
    public List<Vector3> Normals { get; } = [];
    
    public Vector4[] TransformedVertices { get; set; } = [];
    
    public Vector3 Translation { get; set; } = Vector3.Zero;
    
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    
    public List<Face> Faces { get; } = [];
    
    public float Delta { get; set; }
    
    public Vector4 Min { get; set; }
    
    public Vector4 Max { get; set; }

    public float Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            Delta = _scale / 10.0f;
        }
    }
    
    public void ApplyFinalTransformation(Matrix4x4 finalTransform, Camera camera)
    {
        int count = SourceVertices.Count;
        
        Parallel.For(0, count, i =>
        {
            // Тут вектор умножается на матрицу
            var v = Vector4.Transform(SourceVertices[i], finalTransform);
            // Тут мы проверяем, что полученная вершина попадает в минимальный и максимальный диапазон
            // нашей камеры (чтобы избежать деления на ноль)
            if (v.W > camera.ZNear && v.W < camera.ZFar)
            {
                // У нас используется система однородных координат, где есть 4D-вектор
                // Он выглядит как (x, y, z, w)
                // Но нам четвертый компонент w не нужен
                // Поэтому нам нужно разделить все координаты на него, чтобы эта w стала равной 1
                // Это называется нормализацией координат
                // Если w = 1, то вектор (x, y, z) уже представляет собой нормальные 3D-координаты
                v /= v.W;
            }
            TransformedVertices[i] = v;
        });
    }
    
    // Нужен для перемещения объекта
    public Vector3 GetOptimalTranslationStep()
    {
        // Ширина объекта
        float dx = Max.X - Min.X;
        // Высота объекта
        float dy = Max.Y - Min.Y;
        // Глубина объекта
        float dz = Max.Z - Min.Z;

        float stepX = dx / 50.0f;
        float stepY = dy / 50.0f;
        float stepZ = dz / 50.0f;

        return new Vector3(stepX, stepY, stepZ);
    }
}