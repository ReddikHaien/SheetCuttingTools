using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Data
{
    [Guid("8FFDC9EC-5A03-46C8-9C62-CC993D634667")]
    public class GeometryPolygonsComponent() : GH_Component("Geometry Polygons", "GP", "Returns the polygons inside a geometry as lists of points", Constants.Category, Constants.GeometryInformationCategory)
    {
        public override Guid ComponentGuid => GetType().GUID;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Geometry", "G", "The geomery to get polygons from", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("3D Points", "3DP", "The points in each polygon", GH_ParamAccess.tree);
            pManager.AddPointParameter("2D Points", "2DP", "The unrolled points in each polygon if the incoming geometry is flattened. This is null if there is no flattened points", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var wrapper = new GH_ObjectWrapper();
            if (!DA.GetData(0, ref wrapper))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Missing geometry input");
                return;
            }

            var geo = wrapper.CreateGeometry();

            DataTree<Point3d> points3d = new();
            
            if (geo is IFlattenedGeometry flattened)
            {
                LoadDataFromFlattened(flattened, DA);
            }
            else
            {
                LoadDataFromUnflattened(geo, DA);
            }

            DA.SetDataTree(0, points3d);

        }

        private static void LoadDataFromUnflattened(IGeometry geometry, IGH_DataAccess DA)
        {
            DataTree<Point3d> points3d = new();
            foreach ((Polygon polygon, int pIndex) in geometry.Polygons.Select((x, i) => (x, i)))
            {
                var path = new GH_Path(0, DA.Iteration, pIndex);

                points3d.AddRange(geometry.GetVertices(polygon).Select(x => x.ToPoint3d()), path);
            }

            DA.SetDataTree(0, points3d);
        }

        private static void LoadDataFromFlattened(IFlattenedGeometry geometry, IGH_DataAccess DA)
        {
            DataTree<Point3d> points3d = new();
            DataTree<Point3d> points2d = new();
            foreach (((Polygon original, Polygon placed), int pIndex) in geometry.PlacedPolygons.Select((x, i) => (x, i)))
            {
                var path = new GH_Path(0, DA.Iteration, pIndex);
   
                points3d.AddRange(geometry.GetVertices(original).Select(x => x.ToPoint3d()), path);
                points2d.AddRange(geometry.GetPoints(placed).Select(x => x.ToRhinoPoint3d()), path);
            }

            DA.SetDataTree(0, points3d);
            DA.SetDataTree(1, points2d);
        }
    }
}
