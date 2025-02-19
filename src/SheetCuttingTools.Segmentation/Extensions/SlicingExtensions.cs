using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Abstractions.Models.Geometry;
using SheetCuttingTools.Infrastructure.Extensions;
using SheetCuttingTools.Infrastructure.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SheetCuttingTools.Segmentation.Extensions
{
    internal static class SlicingExtensions
    {
        public static async Task<IGeometry> SliceWithPlanes(this IGeometry geometry, Plane3d[] planes, CancellationToken cancellationToken = default)
        {
            if (planes.Length == 0)
                return geometry;

            var bounds = await Task.WhenAll(geometry.Polygons.Chunk(1024).Select(chunk => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return chunk.Select(p => GeometryMath.CutPolygon(p, geometry, planes));
            }, cancellationToken)));

            List<Polygon> polygonsInSegment = [];
            List<Vector3d> newPoints = [];
            List<Vector3f> newNormals = [];

            var l = geometry.Vertices.Count;

            foreach ((Polygon polygon, Vector3d[] addedPoints) in bounds.SelectMany(x => x))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (polygon.Points.Length == 0)
                    continue;

                int[] mapped = new int[polygon.Points.Length];
                for (int i = 0; i < mapped.Length; i++)
                {
                    if (polygon.Points[i] < 0)
                    {
                        var p = addedPoints[-polygon.Points[i] - 1];
                        (var _, int index) = geometry.Vertices.Select((x, i) => (point: x, index: i)).FirstOrDefault(y => y.Item1.EpsilonEqual(p, 0.001), (Vector3d.Zero, -1));

                        if (index >= 0)
                        {
                            mapped[i] = index;
                            continue;
                        }

                        index = newPoints.FindIndex(x => x.EpsilonEqual(p, 0.001));
                        if (index < 0)
                        {
                            newPoints.Add(p);
                            newNormals.Add(Vector3f.AxisY);
                            index = newPoints.Count - 1;
                        }

                        mapped[i] = l + index;
                    }
                    else
                    {
                        mapped[i] = polygon.Points[i];
                    }
                }

                // remove invalid polygons
                var reduced = mapped.Distinct().ToArray();
                if (reduced.Length < 3)
                    continue;

                polygonsInSegment.Add(new(reduced));
            }

            return new RawGeometry
            {
                Parent = geometry,
                Polygons = [.. polygonsInSegment],
                Vertices = geometry.Vertices.Combine([.. newPoints]),
                Normals = geometry.Normals.Combine([.. newNormals]),
            };
        }
    }
}
