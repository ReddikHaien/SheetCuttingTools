using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.GeometryMaking.Parts
{
    public interface IPartMaker
    {
        /// <summary>
        /// returns the required gap between a polygon and a edge.
        /// </summary>
        /// <param name="maleSide">Wich side this part is on.</param>
        /// <returns>7</returns>
        double GetRequiredGap(bool maleSide);


        void CreatePart(Edge edge, Vector2d pointA, Vector2d pointB, Vector2d normal, IFlattenedGeometry flattenedGeometry, PartMakerContext context);
    }
}
