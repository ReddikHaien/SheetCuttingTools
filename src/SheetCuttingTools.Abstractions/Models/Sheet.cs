using g3;
using SheetCuttingTools.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Models
{
    public class Sheet
    {
        /// <summary>
        /// The segment this sheet is built upon
        /// </summary>
        public IFlattenedGeometry FlattenedSegment { get; set; }

        /// <summary>
        /// The lines in the sheet, grouped by their category.
        /// </summary>
        public ILookup<string, Vector2d[]> Lines { get; set; }

        /// <summary>
        /// name of boundary edges, used to identity matching edges on different geometries
        /// </summary>
        public IReadOnlyDictionary<Edge, string> BoundaryNames {  get; set; }
    }
}
