namespace ACG.Core.ObjectParser;

public struct FaceVertex
{
    public int VertexIndex;    
    
    public int NormalIndex;

    public override string ToString() => $"v:{VertexIndex}";
}