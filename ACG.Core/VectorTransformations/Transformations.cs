using System.Numerics;

namespace ACG.Core.VectorTransformations;

public static class Transformations
{
    public static Matrix4x4 CreateViewMatrix(Vector3 eye, Vector3 target, Vector3 up)
    {
        var zAxis = Vector3.Normalize(eye - target);  
        var xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis)); 
        var yAxis = Vector3.Cross(zAxis, xAxis);

        float tx = -Vector3.Dot(xAxis, eye);
        float ty = -Vector3.Dot(yAxis, eye);
        float tz = -Vector3.Dot(zAxis, eye);
        
        var view = new Matrix4x4(
            xAxis.X, xAxis.Y, xAxis.Z, tx,
            yAxis.X, yAxis.Y, yAxis.Z, ty,
            zAxis.X, zAxis.Y, zAxis.Z, tz,
            0.0f,    0.0f,    0.0f,    1.0f);
        
        view = Matrix4x4.Transpose(view);

        return view;
    }
    
    // aspect - соотношение сторон экрана
    // fov - угол, показывающий, насколько широко камера может видеть
    // znear - минимальная дальность, на которой камера видит объекты
    // zfar - максимальная дальность видимости
    public static Matrix4x4 CreatePerspectiveProjection(
        float fov, 
        float aspect, 
        float znear, 
        float zfar)
    {
        float tanHalfFov = MathF.Tan(fov / 2);
        // Далее мы делим, потому что нужно получить число от 0 до 1
        // Мы все эти оси нормировали (см. метод CreateViewMatrix), и их длины равны 1 
        float m00 = 1 / (aspect * tanHalfFov);
        float m11 = 1 / tanHalfFov;
        float m22 = zfar / (znear - zfar);
        // Глубина объекта в перспективе
        float m32 = (znear * zfar) / (znear - zfar);

        var perspective = new Matrix4x4(
            m00, 0,    0,   0,
            0,   m11,  0,   0,
            0,   0,    m22, m32,
            0,   0,   -1,   0
        );

        perspective = Matrix4x4.Transpose(perspective);

        return perspective;
    }
    
    public static Matrix4x4 CreateViewportMatrix(
        float width,
        float height, 
        float xMin = 0.0f,
        float yMin = 0.0f)
    {
        var viewportMatrix = new Matrix4x4(
            width / 2,  0,            0,  xMin + width / 2,
            0,         -height / 2,   0,  yMin + height / 2,
            0,          0,            1,  0,
            0,          0,            0,  1
        );

        viewportMatrix = Matrix4x4.Transpose(viewportMatrix);

        return viewportMatrix;
    }
    
    public static Matrix4x4 CreateWorldTransform(
        float scale, 
        Matrix4x4 rotation, 
        Vector3 translation)
    {
        // Внутри эта функция просто умножает все координаты объекта на scale
        var scaleMatrix = Matrix4x4.CreateScale(scale);
        
        // Вектор translation определяет, на какое расстояние объект должен быть перемещён
        // вдоль осей X, Y и Z
        var translationMatrix = Matrix4x4.CreateTranslation(translation);
        
        var worldMatrix = translationMatrix * rotation * scaleMatrix;
        
        return worldMatrix;
    }
}