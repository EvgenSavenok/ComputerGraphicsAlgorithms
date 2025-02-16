using System.Globalization;
using System.IO;
using System.Numerics;
using ACG.Core.Objects;

namespace ACG.Core.ObjectParser;

public static class ObjectParser
{
   public static ObjectModel Parse(string filePath)
    {
        var model = new ObjectModel();
        var culture = CultureInfo.InvariantCulture;
        Vector4 min = new(float.MaxValue, float.MaxValue, float.MaxValue, 1.0f);
        Vector4 max = new(float.MinValue, float.MinValue, float.MinValue, 1.0f);
        
        foreach (var line in File.ReadLines(filePath))
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            var tokens = trimmedLine.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                continue;

            switch (tokens[0])
            {
                case "v":
                    ParseVertex(tokens, model, ref min, ref max, culture);
                    break;
                case "f":
                    ParseFace(tokens, model);
                    break;
            }
        }
        ComputeBoundingBox(model, min, max); 
        
        return model;
    }

    private static void ParseVertex(string[] tokens, 
        ObjectModel model, 
        ref Vector4 min, 
        ref Vector4 max, 
        CultureInfo culture)
    {
        float x = float.Parse(tokens[1], culture);
        float y = float.Parse(tokens[2], culture);
        float z = float.Parse(tokens[3], culture);
        
        Vector4 vertex = new(x, y, z, 1.0f);
        model.SourceVertices.Add(vertex);

        // For future normalization of model
        min = Vector4.Min(min, vertex);
        max = Vector4.Max(max, vertex);
    }

    private static void ParseFace(string[] tokens, ObjectModel model)
    {
        var face = new Face();
        
        for (int i = 1; i < tokens.Length; i++)
        {
            if (int.TryParse(tokens[i].Split('/')[0], out var vertexIndex))
                face.Vertices.Add(new FaceVertex { VertexIndex = vertexIndex });
        }
        
        model.Faces.Add(face);
    }

    private static void ComputeBoundingBox(ObjectModel model, Vector4 min, Vector4 max)
    {
        var diff = Vector4.Abs(max - min);
        
        float maxDiff = MathF.Max(diff.X, MathF.Max(diff.Y, diff.Z));
        
        // To avoid zero division (cause model has only one point) need to replace 0 by 1
        model.Scale = 2.0f / (maxDiff == 0 ? 1 : maxDiff);
        
        model.Delta = model.Scale / 10.0f; 
        
        model.TransformedVertices = new Vector4[model.SourceVertices.Count];
        
        model.Min = min;
        model.Max = max;
    }
}