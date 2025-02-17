using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Abstractions.Models.Geometry;
using SheetCuttingTools.Infrastructure.Extensions;
using SheetCuttingTools.Infrastructure.Math;

namespace SheetCuttingTools.Segmentation.Segmentors
{
    public class CakeSlicerSegmentor
    {
        public async Task<IGeometry[]> SegmentGeometry(IGeometry geometry, int segments, Vector3d origin, Vector3d zAxis, Vector3d xAxis, CancellationToken cancellationToken)
        {
            var degrees = 360.0 / segments;

            var tasks = Enumerable.Range(0, segments).Select(i => Task.Run(async () =>
            {
                var m1 = Matrix3d.AxisAngleD(zAxis, degrees * i);
                var m2 = Matrix3d.AxisAngleD(zAxis, degrees * (i + 1));

                var normal1 = m1 * xAxis;
                var normal2 = -(m2 * xAxis);

                var plane1 = new Plane3d(normal1, origin);
                var plane2 = new Plane3d(normal2, origin);

                var bounds = await Task.WhenAll(geometry.Polygons.Chunk(128).Select(chunk => Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return chunk.Select(p => GeometryMath.CutPolygon(p, geometry, plane1, plane2));
                },cancellationToken)));

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
                            var index = newPoints.FindIndex(x => x.EpsilonEqual(p, 0.001));
                            if (index < 0)
                            {
                                newPoints.Add(p);
                                newNormals.Add(Vector3f.AxisY);
                                index = newPoints.Count-1;
                            }

                            mapped[i] = l + index;
                        }
                        else
                        {
                            mapped[i] = polygon.Points[i];
                        }
                    }
                    polygonsInSegment.Add(new(mapped));
                }

                return new RawGeometry
                {
                    Parent = geometry,
                    Polygons = [.. polygonsInSegment],
                    Vertices = geometry.Vertices.Combine([.. newPoints]),
                    Normals = geometry.Normals.Combine([.. newNormals]),
                };
            },cancellationToken)).ToArray();

            var segmentPolygons = await Task.WhenAll(tasks);

            return segmentPolygons;
        }

        private enum PolygonPosition
        {
            Outide,
            Partial,
            Inside
        }
    }
}
