using gs;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Flattening.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Behaviors.EdgeFilters
{
    public class IsOfStripTypeEdgeFilter(int edgekind, bool inverse = false) : IEdgeFilter
    {
        public bool FilterEdge(in EdgeFilterCandidate candidate)
        {
            if (!candidate.FlattenedEdge.HasValue)
                return false;

            var strip = GetStripGeometry(candidate.Model);

            if (strip is null)
                return false;

            var kind = strip.EdgeKinds.GetValueOrDefault(candidate.FlattenedEdge.Value, -1);

            return (kind == edgekind) ? !inverse : inverse;
        }

        private StripFlattenedGeometry? GetStripGeometry(IGeometry geometry)
        {
            do
            {
                if (geometry is StripFlattenedGeometry strip)
                    return strip;

                geometry = geometry.Parent!;
            } while (geometry is not null);

            return null;
        }

        public string Name()
            => $"{IEdgeFilter.RootName}/IsOfStripType";
    }
}
