using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Segmentation.Models;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Segmentation.Tests.Models
{
    [TestClass]
    public class SegmentBuilderTests
    {

        [TestMethod]
        public void UpdateBoundary_FirstPolygon_AddsEverythingInOrder()
        {
            // ARRANGE
            Vector3[] verts =
            [
                new(0, 0, 0),
                new(0, 1, 0),
                new(0, 0, 1),
                new(0, 1, 1),
            ];

            Vector3[] normals =
            [
                new(0, 1, 0),
                new(0, 1, 0),
                new(0, 1, 0),
                new(0, 1, 0),
            ];

            Polygon[] polygons =
            [
                new([0, 1, 2, 3])
            ];

            var model = new Model(verts, normals, polygons);
            var builder = new SegmentBuilder(model, []);

            var polygon = new Polygon([0, 1, 2, 3]);

            var edge = new Edge(0, 1);

            // ACT
            builder.UpdateBoundary(polygon, edge);

            // ASSERT
            builder.Boundary.Count.ShouldBe(4);
            builder.Boundary.ShouldSatisfyAllConditions(
                x => x[0].ShouldBeEquivalentTo(new Edge(0, 1)),    
                x => x[1].ShouldBeEquivalentTo(new Edge(1, 2)),    
                x => x[2].ShouldBeEquivalentTo(new Edge(2, 3)),    
                x => x[3].ShouldBeEquivalentTo(new Edge(3, 0))
            );
        }

        [TestMethod]
        public void UpdateBoundary_WithExistingBoundary_AddsEdgesInCorrectOrder()
        {
            // ARRANGE
            Vector3[] verts =
            [
                new(0, 0, 0),
                new(0, 1, 0),
                new(0, 0, 1),
                new(0, 1, 1),
            ];

            Vector3[] normals =
            [
                new(0, 1, 0),
                new(0, 1, 0),
                new(0, 1, 0),
                new(0, 1, 0),
            ];

            Polygon[] polygons =
            [
                new([0, 1, 2, 3])
            ];

            var model = new Model(verts, normals, polygons);
            var builder = new SegmentBuilder(model, []);

            var polygon1 = new Polygon([0, 1, 2, 3]);
            var edge1 = new Edge(0, 1);

            var polygon2 = new Polygon([7, 1, 2, 4, 5, 6]);
            var edge2 = new Edge(1, 2);

            // ACT
            builder.UpdateBoundary(polygon1, edge1);
            builder.UpdateBoundary(polygon2, edge2);

            // ASSERT
            builder.Boundary.Count.ShouldBe(8);
            builder.Boundary.ShouldSatisfyAllConditions(
                x => x[0].ShouldBeEquivalentTo(new Edge(0, 1)),
                x => x[1].ShouldBeEquivalentTo(new Edge(1, 7)),
                x => x[2].ShouldBeEquivalentTo(new Edge(7, 6)),
                x => x[3].ShouldBeEquivalentTo(new Edge(6, 5)),
                x => x[4].ShouldBeEquivalentTo(new Edge(5, 4)),
                x => x[5].ShouldBeEquivalentTo(new Edge(4, 2)),
                x => x[6].ShouldBeEquivalentTo(new Edge(2, 3)),
                x => x[7].ShouldBeEquivalentTo(new Edge(3, 0))
            );
        }
    }
}
