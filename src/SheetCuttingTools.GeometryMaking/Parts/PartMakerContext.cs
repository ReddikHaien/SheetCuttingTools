﻿using g3;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.GeometryMaking.Parts
{
    public class PartMakerContext(bool maleSide)
    {
        public bool MaleSide { get; } = maleSide;

        private readonly Dictionary<string, (List<Vector2d> points, HashSet<Edge> edges)> groups = [];

        /// <summary>
        /// Adds a line of points to this context.
        /// </summary>
        /// <param name="closed">If the line should be closed.</param>
        /// <param name="points">The points to add.</param>
        public void AddLine(IReadOnlyList<Vector2d> points, string category = "Line", bool closed = false)
        {
            foreach(var (a ,b) in points.SlidingWindow(closed))
            {
                AddSegment(a, b, category);
            }
        }

        public void AddSegment(Vector2d a, Vector2d b, string category = "Line")
        {
            if (!groups.TryGetValue(category, out var group))
            {
                group = ([], []);
                groups.Add(category, group);
            }

            var (points, edges) = group;

            var ia = points.FindIndex(x => x.EpsilonEqual(a, 0.01));
            if (ia == -1)
            {
                ia = points.Count;
                points.Add(a);
            }

            var ib = points.FindIndex(x => x.EpsilonEqual(b, 0.01));
            if (ib == -1)
            {
                ib = points.Count;
                points.Add(b);
            }

            if (a != b)
                edges.Add(new Edge(ia, ib));
        }

        /// <summary>
        /// Merges a different context into this one.
        /// </summary>
        /// <param name="other">The other context to merge.</param>
        public void AddContext(PartMakerContext other)
        {
            foreach(var group in other.groups)
            {
                foreach(var edge in group.Value.edges)
                {
                    var a = group.Value.points[edge.A];
                    var b = group.Value.points[edge.B];
                    AddSegment(a, b, group.Key);
                }
            }
        }
    }
}
