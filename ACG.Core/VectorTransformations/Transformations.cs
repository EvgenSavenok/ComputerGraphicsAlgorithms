using System.Numerics;

namespace ACG.Core.VectorTransformations;

public static class Transformations
{
    // up - для того, чтобы показать, где находится ось Y в камере
    // Чтобы оси в ней не перепутались местами
    // В этом методе мы камеру помещаем в центр сцены, а все остальные объекты - перестраиваем под камеру
    public static Matrix4x4 CreateViewMatrix(Vector3 eye, Vector3 target, Vector3 up)
    {
        // Нормализовать вектор - это сделать его длину равной 1
        // Здесь мы нормализуем каждую ось камеры, чтобы не было искажений в пространстве
        // То есть, по факту, делаем каждую ось одинаковой длины, равной 1
        var zAxis = Vector3.Normalize(eye - target);  
        // Cross - векторное произведение
        // Через Cross мы получаем x, который перпендикулярен up и z 
        // z смотрит в eye, up - вверх
        // То есть x будет смотреть вправо относительно камеры 
        // Вправо - потому что есть правило правой руки при перемножении двух векторов
        var xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis)); 
        var yAxis = Vector3.Cross(zAxis, xAxis);

        // Получаем сдвиги каждого объекта относительно камеры на сцене, чтобы поместить их вокруг неё
        float tx = -Vector3.Dot(xAxis, eye);
        float ty = -Vector3.Dot(yAxis, eye);
        float tz = -Vector3.Dot(zAxis, eye);
        
        var view = new Matrix4x4(
            xAxis.X, xAxis.Y, xAxis.Z, tx,
            yAxis.X, yAxis.Y, yAxis.Z, ty,
            zAxis.X, zAxis.Y, zAxis.Z, tz,
            0.0f,    0.0f,    0.0f,    1.0f);

        // Транспонируем матрицу, потому что есть два порядка - столбцовый и строчный
        // Нам нужен строчный, а работали со столбцовым
        // Поэтому нужно преобразовать
        view = Matrix4x4.Transpose(view);

        return view;
    }
    
    // aspect - соотношение сторон экрана
    // fov - угол, показывающий, насколько широко камера может видеть
    // znear - минимальная дальность, на которой камера видит объекты
    // zfar - максимальная дальность видимости
    // Тут мы создаем переспективную матрицу, которая может преобразовать 3D в 2D
    // Она нужна, чтобы можно было рендерить объекты как в реальном мире, с учетом перспективы:
    // Чем дальше ты от камеры, тем ты меньше
    public static Matrix4x4 CreatePerspectiveProjection(
        float fov, 
        float aspect, 
        float znear, 
        float zfar)
    {
        float tanHalfFov = MathF.Tan(fov / 2);
        // Далее мы делим, потому что нужно получить число от 0 до 1
        // Мы все эти оси нормировали (см. метод CreateViewMatrix), и их длины равны 1 
        // Масштабирование по оси X
        float m00 = 1 / (aspect * tanHalfFov);
        // Масштабирование по оси Y
        // Тут без aspect, потому что его уже учли в X
        float m11 = 1 / tanHalfFov;
        // Масштабирование по оси Z 
        float m22 = zfar / (znear - zfar);
        // Тут вычисляется глубина для объекта в перспективе
        // Про эту формулку еще Оношко рассказывал на ассемблере, хех
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
    
    // Рассчитываем область видимости
    public static Matrix4x4 CreateViewportMatrix(
        float width,
        float height, 
        // Смещение начала области видимости по осям X и Y (тут по нулям, потому что начинам 
        // с пикселя в левом верхнем углу
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