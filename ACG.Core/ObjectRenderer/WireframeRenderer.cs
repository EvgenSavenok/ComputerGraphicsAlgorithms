﻿using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ACG.Core.Objects;
using ACG.Core.VectorTransformations;

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
            // Получаем указатель на начало буфера пикселей
            int* pBackBuffer = (int*)wb.BackBuffer;
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;

            Parallel.ForEach(model.Faces, face =>
            {
                int count = face.Vertices.Count;
                if (count < 2)
                    // Пропускаем, если грань не имеет хотя бы двух вершин
                    return; 

                for (int i = 0; i < count; i++)
                {
                    // Вычисляем индексы вершин (в OBJ индексы начинаются с 1)
                    int index1 = face.Vertices[i].VertexIndex - 1;
                    int index2 = face.Vertices[(i + 1) % count].VertexIndex - 1;

                    // Проверяем корректность индексов
                    if (index1 < 0 || index1 >= model.TransformedVertices.Length ||
                        index2 < 0 || index2 >= model.TransformedVertices.Length)
                        continue;

                    // Вычисляем экранные координаты вершин
                    int x0 = (int)Math.Round(model.TransformedVertices[index1].X);
                    int y0 = (int)Math.Round(model.TransformedVertices[index1].Y);
                    int x1 = (int)Math.Round(model.TransformedVertices[index2].X);
                    int y1 = (int)Math.Round(model.TransformedVertices[index2].Y);
                    float z0 = model.TransformedVertices[index1].Z;
                    float z1 = model.TransformedVertices[index2].Z;

                    // Если обе точки явно вне экрана или вне диапазона z – пропускаем
                    if ((x0 >= width && x1 >= width) || (x0 <= 0 && x1 <= 0) ||
                        (y0 >= height && y1 >= height) || (y0 <= 0 && y1 <= 0) ||
                        (z0 < camera.ZNear || z1 < camera.ZNear) || (z0 > camera.ZFar || z1 > camera.ZFar))
                    {
                        continue;
                    }

                    // Отрисовываем линию с использованием алгоритма Брезенхэма (все записи будут в один и тот же цвет)
                    DrawLineBresenham(pBackBuffer, width, height, x0, y0, x1, y1, intColor);
                }
            });
        }

        // Сообщаем системе, что изменился весь буфер
        wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }

    // Я ваще не вкурил, что здесь происходит
    // Поэтому просто код с ГПТ вставил
    // Надо разобраться
    // TODO
    private static unsafe void DrawLineBresenham(int* buffer, int width, int height, int x0, int y0, int x1, int y1, int color)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
            {
                buffer[y0 * width + x0] = color;
            }

            if (x0 == x1 && y0 == y1)
                break;

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
    
    public static void Draw3DSelectionHighlight(Scene scene, ObjectModel model, WriteableBitmap wb, Color highlightColor)
    {
        var world = Transformations.CreateWorldTransform(
            model.Scale,
            Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z),
            model.Translation);
        var view = scene.Camera.GetViewMatrix();
        var projection = scene.Camera.GetProjectionMatrix();
        var viewport = scene.GetViewportMatrix();
        var finalTransform = world * view * projection * viewport;

        // Предполагается, что model.Min и model.Max заданы в объектном (локальном) пространстве
        Vector4[] corners = new Vector4[8];
        corners[0] = new Vector4(model.Min.X, model.Min.Y, model.Min.Z, 1);
        corners[1] = new Vector4(model.Max.X, model.Min.Y, model.Min.Z, 1);
        corners[2] = new Vector4(model.Min.X, model.Max.Y, model.Min.Z, 1);
        corners[3] = new Vector4(model.Max.X, model.Max.Y, model.Min.Z, 1);
        corners[4] = new Vector4(model.Min.X, model.Min.Y, model.Max.Z, 1);
        corners[5] = new Vector4(model.Max.X, model.Min.Y, model.Max.Z, 1);
        corners[6] = new Vector4(model.Min.X, model.Max.Y, model.Max.Z, 1);
        corners[7] = new Vector4(model.Max.X, model.Max.Y, model.Max.Z, 1);

        // Преобразуем каждую вершину в экранное пространство
        Point[] screenCorners = new Point[8];
        for (int i = 0; i < 8; i++)
        {
            Vector4 v = Vector4.Transform(corners[i], finalTransform);
            if (v.W > scene.Camera.ZNear && v.W < scene.Camera.ZFar)
            {
                v /= v.W; 
            }

            screenCorners[i] = new Point(v.X, v.Y);
        }

        // Определяем ребра 3D-бокса: 12 ребер (4 нижних, 4 верхних, 4 вертикальных)
        int[][] edges = new int[][]
        {
            // нижняя грань
            [0, 1], [1, 3], [3, 2], [2, 0], 
            // верхняя грань
            [4, 5], [5, 7], [7, 6], [6, 4],
            // вертикальные ребра
            [0, 4], [1, 5], [2, 6], [3, 7] 
        };

        int intColor = highlightColor.ColorToIntBgra();
        unsafe
        {
            wb.Lock();
            int* pBackBuffer = (int*)wb.BackBuffer;
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;
            foreach (var edge in edges)
            {
                int x0 = (int)Math.Round(screenCorners[edge[0]].X);
                int y0 = (int)Math.Round(screenCorners[edge[0]].Y);
                int x1 = (int)Math.Round(screenCorners[edge[1]].X);
                int y1 = (int)Math.Round(screenCorners[edge[1]].Y);
                
                float z0 = model.TransformedVertices[edge[0]].Z;
                float z1 = model.TransformedVertices[edge[1]].Z;
                
                if ((x0 >= width && x1 >= width) || (x0 <= 0 && x1 <= 0) || (y0 >= height && y1 >= height) ||
                    (y0 <= 0 && y1 <= 0) || (z0 < scene.Camera.ZNear || z1 < scene.Camera.ZNear) || (z0 > scene.Camera.ZFar || z1 > scene.Camera.ZFar))
                {
                    continue;
                }
                
                DrawLineBresenham(pBackBuffer, width, height, x0, y0, x1, y1, intColor);
            }

            wb.AddDirtyRect(new Int32Rect(0, 0, width, height));
            wb.Unlock();
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