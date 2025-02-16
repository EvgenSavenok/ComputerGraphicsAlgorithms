using System.Numerics;
using ACG.Core.ObjectParser;

namespace ACG.Core.Objects;

public class ObjectModel
{
    private float _scale;
    public List<Vector4> SourceVertices { get; } = [];
    
    public Vector4[] TransformedVertices { get; set; } = [];
    
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
}