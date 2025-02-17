using g3;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Contracts
{
    public interface IFlattenedGeometry : IGeometry
    {
        public IReadOnlyList<Vector2d> Points { get; }

        public IReadOnlyList<(Polygon Original, Polygon Placed)> PlacedPolygons { get; }

        public IDictionary<Edge, Vector2d> BoundaryNormal { get; }

        public Vector2d Center2D { get; }
    }
}
