using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;

namespace SheetCuttingTools.Abstractions.Behaviors;

public interface IPartMaker : IBehavior
{
    public new const string RootName = $"{IBehavior.RootName}/PartMaker";
    /// <summary>
    /// returns the required gap between a polygon and a edge.
    /// </summary>
    /// <param name="maleSide">Wich side this part is on.</param>
    /// <returns>7</returns>
    double GetRequiredGap(IPartMakerContext partMakerContext);

    void CreatePart(IPartMakerContext context, IPartGeometryOutput geometryOutput);
}

public interface IPartMakerContext
{
    bool MaleSide { get; }

    Edge Edge { get; }

    Vector2d A { get; }

    Vector2d B { get; }

    Vector2d OriginalB { get; }

    Vector2d OriginalA { get; }
    
    Vector2d U { get; }

    Vector2d V { get; }
}

public interface IPartGeometryOutput
{
    void AddCircle(Vector2d position, double radius, string category = "Line");
    void AddLine(IEnumerable<Vector2d> points, string category = "Line", bool closed = false);
    void AddSegment(Vector2d a, Vector2d b, string category = "Line");
}
