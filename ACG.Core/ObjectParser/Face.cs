namespace ACG.Core.ObjectParser;

public class Face
{
    public List<FaceVertex> Vertices { get; } = [];

    public override string ToString() => string.Join(" | ", Vertices);
}