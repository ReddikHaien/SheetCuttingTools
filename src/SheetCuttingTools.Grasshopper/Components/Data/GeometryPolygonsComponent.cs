using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Grasshopper.Helpers;
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
                DataTree<Point3d> points2d = new();
                int i = 0;
                foreach (var (Original, Placed) in flattened.PlacedPolygons)
                {
                    int actualId = flattened.Parent.Polygons.Select((x, i) => (x, i)).First(x => x.x == Original).i;
                    var branch = new GH_Path(0, DA.Iteration, actualId);
                    points3d.AddRange(Original.Points.Select(x => flattened.Vertices[x].ToPoint3d()), branch);
                    points2d.AddRange(Placed.Points.Select(x => flattened.Points[x].ToRhinoPoint3d()), branch);
                    i++;
                }
                DA.SetDataTree(1, points2d);
            }
            else
            {
                int i = 0;
                foreach (var polygon in geo.Polygons)
                {
                    var branch = new GH_Path(0, DA.Iteration, i);
                    points3d.AddRange(polygon.Points.Select(x => geo.Vertices[x].ToPoint3d()), branch);
                    i++;
                }
            }

            DA.SetDataTree(0, points3d);

        }
    }
}
