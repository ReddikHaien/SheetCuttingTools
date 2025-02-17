using g3;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Abstractions.Models.Geometry;
using SheetCuttingTools.Infrastructure.Math;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Infrastructure.Tests.Math
{
    [TestClass]
    public class GeometryMathTests
    {
        [TestMethod]
        public void CutPolygon_Given1PlaneAndPolygon_CutsPolygon()
        {
            // ARRANGE
            var geometry = new RawGeometry
            {
                Vertices = [
                    new Vector3d(1.0, 1.0, 0.0),
                    new Vector3d(0.0, -1.0, 0.0),
                    new Vector3d(0.0, 1.0, 0.0)
                ],
            };

            var poly = new Polygon([0, 1, 2]);

            var plane = new Plane3d(Vector3d.AxisY, Vector3d.Zero);
                
            // ACT
            var (n, s) = GeometryMath.CutPolygon(poly, geometry, plane);

            //ASSERT
            n.Points.Length.ShouldBe(4);
            s.Length.ShouldBe(2, "Should only have one new point");
            
        }

        [TestMethod]
        public void CutPolygon_Given2PlanesAndPolygon_CutsPolygon()
        {
            var geometry = new RawGeometry
            {
                Vertices = [
                    new Vector3d(-1.0, -1.0, 0.0),
                    new Vector3d(-1.0, 1.0, 0.0),
                    new Vector3d(1.0, 1.0, 0.0),
                    new Vector3d(1.0, -1.0, 0.0)
                ],
            };

            var poly = new Polygon([0, 1, 2, 3]);

            var n = (Vector3d.AxisX + Vector3d.AxisY).Normalized;


            var plane = new Plane3d(n, new Vector3d(-0.5, -0.5, 0.0));
            var plane2 = new Plane3d(-n, new Vector3d(0.5, 0.5, 0.0));

            var (np, s) = GeometryMath.CutPolygon(poly, geometry, plane, plane2);
        }
    }
}
