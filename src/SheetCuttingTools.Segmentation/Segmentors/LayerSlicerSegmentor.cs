using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Abstractions.Models.Geometry;
using SheetCuttingTools.Infrastructure.Extensions;
using SheetCuttingTools.Infrastructure.Math;
using SheetCuttingTools.Segmentation.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Segmentation.Segmentors
{
    public class LayerSlicerSegmentor
    {
        public async Task<IGeometry[]> SegmentGeometry(IGeometry geometry, Plane3d cutPlane, int segments, CancellationToken cancellationToken = default)
        {
            double highest = geometry.Vertices.Max(cutPlane.DistanceTo);
            double layerHeight = highest / segments;

            var tasks = Enumerable.Range(0, segments).Select(i => Task.Run(async () =>
            {
                var plane1 = new Plane3d(cutPlane.Normal, cutPlane.Constant + i * layerHeight);
                var plane2 = new Plane3d(-cutPlane.Normal, -(cutPlane.Constant + (i + 1) * layerHeight));
                
                var geo = await geometry.SliceWithPlanes([plane1, plane2], cancellationToken);              
                return geo;
            }, cancellationToken)).ToArray();

            var geometries = await Task.WhenAll(tasks);

            var (min, max) = geometry.Vertices.Aggregate((a, b) => a.Min(b), (a, b) => a.Max(b));

            OctTree tree = new(min, max, 0.01);
            List<Vector3d> newVerts = [];
            List<Vector3f> newNormals = [];
            List<Polygon[]> fixedGeometries = [];

            foreach(var geo in geometries)
            {
                List<Polygon> polygons = [];

                foreach(var polygon in geo.Polygons)
                {
                    polygon.Points.Select(p =>
                    {
                        var v = geo.Vertices[p];
                        if (tree.GetValue(v, out var index))
                            return index;

                        index = newVerts.Count;
                        newVerts.Add(v);
                        newNormals.Add(geo.Normals[p]);
                        tree.AddPoint(v, index);
                        return index;

                    }).ToArray();
                    polygons.Add(polygon);
                }
                fixedGeometries.Add(polygons.ToArray());
            }

            return fixedGeometries.Select(x => new RawGeometry
            {
                Parent = geometry,
                Vertices = newVerts.ToArray().AsReadOnly(),
                Normals = newNormals.ToArray().AsReadOnly(),
                Center3d = geometry.Center3d,
                Polygons = x.ToArray().AsReadOnly()
            }).ToArray();
        }
    }
}
