using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ACG.Core.ObjectParser;
using ACG.Core.Objects;

namespace ACG.Core.ObjectRenderer;

// First lab
public static class WireframeRenderer
{
    public static void DrawWireframe(
        ObjectModel model, 
        WriteableBitmap wb, 
        Color clearColor, 
        Camera camera)
    {
        int intColor = clearColor.ColorToIntBgra();
        
        wb.Lock();

        unsafe
        {
            int* pBackBuffer = (int*)wb.BackBuffer;
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;

            Parallel.ForEach(model.Faces, face =>
            {
                DrawFace(model, face, pBackBuffer, width, height, intColor, camera);
            });
        }

        wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }
    
    private static unsafe void DrawFace(
        ObjectModel model, 
        Face face, 
        int* pBackBuffer, 
        int width, 
        int height, 
        int color, 
        Camera camera)
    {
        int count = face.Vertices.Count;
        if (count < 2) 
            // У каждой поверхности должна быть хотя бы одна вершина
            return;

        // Проходимся по всем вершинам грани
        for (int i = 0; i < count; i++)
        {
            // Индексы вершин в OBJ файлах начинаются с 1, поэтому из индекса каждой вершины вычитается 1
            int index1 = face.Vertices[i].VertexIndex - 1;
            int index2 = face.Vertices[(i + 1) % count].VertexIndex - 1;

            if (!AreIndicesValid(model, index1, index2)) 
                continue;

            var (x0, y0, z0) = GetScreenCoordinates(model, index1);
            var (x1, y1, z1) = GetScreenCoordinates(model, index2);

            // Проверяем, выходит ли вершина за рамки экрана или камеры, чтобы не отрисовывать
            if (IsOutsideScreen(x0, y0, x1, y1, width, height) 
                || IsOutsideCameraView(z0, z1, camera))
                continue;

            DrawLineBresenham(pBackBuffer, width, height, x0, y0, x1, y1, color);
        }
    }

    private static bool AreIndicesValid(ObjectModel model, int index1, int index2)
    {
        return index1 >= 0 && index1 < model.TransformedVertices.Length &&
               index2 >= 0 && index2 < model.TransformedVertices.Length;
    }

    private static (int, int, float) GetScreenCoordinates(ObjectModel model, int index)
    {
        var vertex = model.TransformedVertices[index];
        // Мы здесь все округляем, потому что нужно представить координаты как пиксели
        // А пискели не могут быть дробными
        // Z оставляем как есть, потому что это - глубина, а она может быть любой
        return ((int)Math.Round(vertex.X), (int)Math.Round(vertex.Y), vertex.Z);
    }

    private static bool IsOutsideScreen(int x0, int y0, int x1, int y1, int width, int height)
    {
        return (x0 >= width && x1 >= width) || (x0 <= 0 && x1 <= 0) ||
               (y0 >= height && y1 >= height) || (y0 <= 0 && y1 <= 0);
    }

    private static bool IsOutsideCameraView(float z0, float z1, Camera camera)
    {
        return (z0 < camera.ZNear || z1 < camera.ZNear) || (z0 > camera.ZFar || z1 > camera.ZFar);
    }
    
    // :D
    private static unsafe void DrawLineBresenham(
        int* buffer, 
        int width, 
        int height, 
        int x0,
        int y0,
        int x1, 
        int y1,
        int color)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        // Направления, в которых нужно двигаться
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        // Будет использоваться для корректировки отклонений
        int err = dx - dy;

        // Двигаемся по линии, точка за точкой
        while (true)
        {
            // Проверка на то, что точка внутри экрана
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
            {
                // Записываем цвет пикселя в буфер (буфер хранит в себе все пиксели текущего изображения)
                // Формула y0 * width + x0 нужна для вычисления пикселя,
                // который хранится по факту как двумерная координата, но у нас он в одномерном массиве
                // По факту, это и есть процесс отрисовки линии
                buffer[y0 * width + x0] = color;
            }

            // Если текущая точка совпала с конечной, то выходим из цикла, завершив рисование
            if (x0 == x1 && y0 == y1)
                break;

            // Вычисление координат следующей отрисовываемой точки на основе ошибки err
            // Умножаем на 2, чтобы избавиться от константы 0.5 в неравенствах
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
    
    private static int ColorToIntBgra(this Color color)
    {
        return (color.B << 0) | (color.G << 8) | (color.R << 16) | (color.A << 24);
    }
    
    public static void ClearBitmap(WriteableBitmap wb, Color clearColor)
    {
        int intColor = clearColor.ColorToIntBgra();

        wb.Lock();
        try
        {
            unsafe
            {
                int* pBackBuffer = (int*)wb.BackBuffer;
                int totalPixels = wb.PixelWidth * wb.PixelHeight;

                for (int i = 0; i < totalPixels; i++)
                {
                    pBackBuffer[i] = intColor;
                }
            }

            wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        }
        finally
        {
            wb.Unlock();
        }
    }

}