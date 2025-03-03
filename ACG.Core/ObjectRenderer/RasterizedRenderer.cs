using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ACG.Core.ObjectParser;
using ACG.Core.Objects;
using ACG.Core.VectorTransformations;

namespace ACG.Core.ObjectRenderer;

public class RasterizedRenderer
{
    private static float[,]? _zBuffer;
    
    public static void ClearZBuffer(int width, int height, Camera camera)
    {
        _zBuffer ??= new float[width, height];
        float initDepth = camera.ZFar;
        
        // Инициализируем буфер самыми дальними значениями, чтобы можно было потом перезаписать
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++) 
                _zBuffer[x, y] = initDepth;
    }

    public static unsafe void DrawTriangles(ObjectModel model, WriteableBitmap wb, Color color, Camera camera)
    {
        int width = wb.PixelWidth;
        int height = wb.PixelHeight;
        var world = CreateWorldMatrix(model);
        
        wb.Lock();
        int* buffer = (int*)wb.BackBuffer;
        
        Parallel.ForEach(model.Faces, face => ProcessFace(face, model, world, camera, color, buffer, width, height));
        
        wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }

    private static Matrix4x4 CreateWorldMatrix(ObjectModel model)
    {
        return Transformations.CreateWorldTransform(
            model.Scale,
            Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z),
            model.Translation);
    }

    private static unsafe void ProcessFace(
        Face face, 
        ObjectModel model, 
        Matrix4x4 world, 
        Camera camera, 
        Color color, 
        int* buffer, 
        int width, 
        int height)
    {
        if (face.Vertices.Count < 3) 
            return;
        
        for (int j = 1; j < face.Vertices.Count - 1; j++)
        {
            ProcessTriangle(face, j, model, world, camera, color, buffer, width, height);
        }
    }

    private static unsafe void ProcessTriangle(
        Face face, 
        int j, 
        ObjectModel model, 
        Matrix4x4 world, 
        Camera camera,
        Color color, 
        int* buffer, 
        int width, 
        int height)
    {
        int idx0 = face.Vertices[0].VertexIndex - 1;
        int idx1 = face.Vertices[j].VertexIndex - 1;
        int idx2 = face.Vertices[j + 1].VertexIndex - 1;
        
        if (!IsValidIndex(idx0, model) || !IsValidIndex(idx1, model) || !IsValidIndex(idx2, model)) 
            return;
        
        Vector3 worldV0 = TransformToWorld(model.SourceVertices[idx0], world);
        Vector3 worldV1 = TransformToWorld(model.SourceVertices[idx1], world);
        Vector3 worldV2 = TransformToWorld(model.SourceVertices[idx2], world);
        
        Vector3 normal = CalculateNormal(worldV0, worldV1, worldV2);
        
        if (IsBackFace(normal, worldV0, camera))
            return;
        
        var shadedColor = ApplyLambert(color, normal, camera.LambertLight);
        
        Vector3 screenV0 = model.TransformedVertices[idx0].AsVector3();
        Vector3 screenV1 = model.TransformedVertices[idx1].AsVector3();
        Vector3 screenV2 = model.TransformedVertices[idx2].AsVector3();
        
        DrawFilledTriangle(screenV0, screenV1, screenV2, shadedColor, buffer, width, height);
    }

    private static bool IsValidIndex(int index, ObjectModel model)
    {
        return index >= 0 && index < model.TransformedVertices.Length;
    }

    private static Vector3 TransformToWorld(Vector4 vertex, Matrix4x4 world)
    {
        return Vector4.Transform(vertex, world).AsVector3();
    }

    private static Vector3 CalculateNormal(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        return Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
    }

    private static bool IsBackFace(Vector3 normal, Vector3 worldV0, Camera camera)
    {
        Vector3 viewDirection = worldV0 - camera.Eye;
        return Vector3.Dot(normal, viewDirection) > 0;
    }
    
    private static unsafe void DrawFilledTriangle(
        Vector3 v0, 
        Vector3 v1,
        Vector3 v2,
        Color color, 
        int* buffer,
        int width, 
        int height)
    {
        // Определяем ограничивающий прямоугольник, чтобы не перебирать все пиксели экрана
        // Округляем через Ceiling и Floor, чтобы можно было представить в виде пикселей (они ведь целые)
        int minX = Math.Max(0, (int)Math.Floor(Math.Min(v0.X, Math.Min(v1.X, v2.X))));
        int maxX = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(v0.X, Math.Max(v1.X, v2.X))));
        int minY = Math.Max(0, (int)Math.Floor(Math.Min(v0.Y, Math.Min(v1.Y, v2.Y))));
        int maxY = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(v0.Y, Math.Max(v1.Y, v2.Y))));

        // Проверяем треугольник на вырожденность (площадь равна 0)
        float denom = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
        if (Math.Abs(denom) < float.Epsilon) 
            // Вырожденный треугольник, так как точки находятся на одной линии
            return; 
        
        float invDenom = 1.0f / denom;
        
        for (var y = minY; y <= maxY; y++)
        {
            if (y < 0 || y >= height)
                return;
            
            for (var x = minX; x <= maxX; x++)
            {
                if (x < 0 || x >= width)
                    continue;

                // Вычисляем барицентрические координаты: alpha, beta, gamma
                // Они однозначно задают положение точки в афинном пространстве
                float alpha = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) * invDenom;
                float beta  = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) * invDenom;
                float gamma = 1 - alpha - beta;
                
                // Если точка внутри треугольника (включая границы)
                if (alpha >= 0 && beta >= 0 && gamma >= 0)
                {
                    // Интерполируем глубину по барицентрическим координатам
                    float depth = alpha * v0.Z + beta * v1.Z + gamma * v2.Z;
                    // Если новый фрагмент ближе (меньшее значение depth) – обновляем Z-буфер и рисуем пиксель
                    if (depth < _zBuffer![x, y])
                    {
                        _zBuffer[x, y] = depth;
                        buffer[y * width + x] = ColorToIntBgra(color);
                    }
                }
            }
        }
    }

    private static int ColorToIntBgra(Color color)
    {
        return (color.B << 0) | (color.G << 8) | (color.R << 16) | (color.A << 24);
    }

    // Источник света - направленный
    private static Color ApplyLambert(Color baseColor, Vector3 normal, Vector3 lambertLight)
    {
        Vector3 lightDir = Vector3.Normalize(lambertLight);
        
        // Интенсивность – косинус угла между нормалью и направлением света 
        // Чем меньше, тем сильнее освещается
        float intensity = MathF.Max(Vector3.Dot(normal, -lightDir), 0);
        
        return Color.FromArgb(baseColor.A,
            (byte)(baseColor.R * intensity),
            (byte)(baseColor.G * intensity),
            (byte)(baseColor.B * intensity));
    }
}