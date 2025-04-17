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
using System.Linq;
using System.Runtime.InteropServices;

namespace SheetCuttingTools.Grasshopper.Components.Data;

[Guid("A0406BFA-A690-40B5-86E4-4B995D9E30AC")]
public class GeometryEdgesComponent() : GH_Component("Geometry edges", "GE", "Retrieves the edges from a geometry", Constants.Category, Constants.GeometryInformationCategory)
{
    public override Guid ComponentGuid => GetType().GUID;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Geonmetry", "G", "The geometry to extract edge information from", GH_ParamAccess.item);
        pManager.AddGenericParameter("Edge filters", "E", "Edge filters to use for selecting edges to be processed", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddPointParameter("Points 3D", "P3D", "The points in the edge", GH_ParamAccess.tree);
        pManager.AddPointParameter("Points 2D", "P2D", "The points in the edge if the incoming geometry is flattened", GH_ParamAccess.tree);
        pManager.AddGenericParameter("Edges 3D", "E3D", "The actual edge object", GH_ParamAccess.tree);
        pManager.AddGenericParameter("Edges 2D", "E2D", "The actual edge object if the incoming geometry is flattened", GH_ParamAccess.tree);

    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_ObjectWrapper wrapper = new();
        if (!DA.GetData(0, ref wrapper))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Missing geometry!");
            return;
        }

        List<GH_ObjectWrapper> edgeFilters = [];
        DA.GetDataList(1, edgeFilters);

        IEdgeFilter[] filters = edgeFilters
            .Select(x => x.Value as IEdgeFilter)
            .Where(e => e is not null)
            .ToArray();

        IGeometry geometry = wrapper.CreateGeometry();

        if (geometry is IFlattenedGeometry flattened)
        {
            LoadDataFromFlattened(flattened, filters, DA);
        }
        else
        {
            LoadDataFromUnflattened(geometry, filters, DA);
        }
    }

    private static void LoadDataFromUnflattened(IGeometry geometry, IEdgeFilter[] filters, IGH_DataAccess DA)
    {
        DataTree<Point3d> points = new();
        DataTree<Edge> edges = new();
        foreach (var (polygon, pIndex) in geometry.Polygons.Select((x, i) => (x, i)))
        {
            foreach (var (edge, eIndex) in polygon.GetEdges().Select((x, i) => (x, i)))
            {
                var candidate = new EdgeFilterCandidate(edge, geometry);

                if (filters.Length > 0 && !filters.All(x => x.FilterEdge(candidate)))
                    continue;

                var path = new GH_Path(0, DA.Iteration, pIndex, eIndex);
                (g3.Vector3d a, g3.Vector3d b) = geometry.GetVertices(edge);
                points.AddRange([a.ToPoint3d(), b.ToPoint3d()], path);
                edges.Add(edge, path);
            }
        }

        DA.SetDataTree(0, points);
        DA.SetDataTree(2, edges);
    }

    private static void LoadDataFromFlattened(IFlattenedGeometry geometry, IEdgeFilter[] filters, IGH_DataAccess DA)
    {
        DataTree<Point3d> points3d = new();
        DataTree<Point3d> points2d = new();
        DataTree<Edge> edges3d = new();
        DataTree<Edge> edges2d = new();
        foreach (((Polygon original, Polygon placed), int pIndex) in geometry.PlacedPolygons.Select((x, i) => (x, i)))
        {
            foreach (((Edge oEdge, Edge pEdge), int eIndex) in original.GetEdges().Zip(placed.GetEdges()).Select((x, i) => (x, i)))
            {
                var path = new GH_Path(0, DA.Iteration, pIndex, eIndex);

                var candidate = new EdgeFilterCandidate(oEdge, geometry)
                {
                    FlattenedEdge = pEdge,
                };

                if (filters.Length > 0 && !filters.All(x => x.FilterEdge(candidate)))
                    continue;

                (g3.Vector3d a3d, g3.Vector3d b3d) = geometry.GetVertices(oEdge);
                (g3.Vector2d a2d, g3.Vector2d b2d) = geometry.GetPoints(pEdge);
                points3d.AddRange([a3d.ToPoint3d(), b3d.ToPoint3d()], path);
                points2d.AddRange([a2d.ToRhinoPoint3d(), b2d.ToRhinoPoint3d()], path);
                edges3d.Add(oEdge, path);
                edges2d.Add(pEdge, path);
            }
        }

        DA.SetDataTree(0, points3d);
        DA.SetDataTree(1, points2d);
        DA.SetDataTree(2, edges3d);
        DA.SetDataTree(3, edges2d);
    }
}
