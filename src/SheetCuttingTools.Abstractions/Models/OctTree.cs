using g3;

namespace SheetCuttingTools.Abstractions.Models
{
    public class OctTree(Vector3d min, Vector3d max, double epsilon)
    {
        private readonly double epsilon = epsilon;
        private readonly Node root = new Node.Branch(new Node.Leaf(min, max, epsilon));

        public bool AddPoint(Vector3d point, int value)
            => root.AddPoint(point, value);

        public bool GetValue(Vector3d point, out int value)
            => root.GetValue(point, out value);

        public bool Contains(Vector3d point)
            => root.Contains(point);

        protected abstract record Node
        {
            private readonly double epsilon;

            public AxisAlignedBox3d Box { get; }

            public Vector3d Min => Box.Min;
            public Vector3d Max => Box.Max;

            public Vector3d Center => Box.Center;

            public abstract bool AddPoint(Vector3d point, int value);

            public abstract bool Contains(Vector3d point);

            public abstract bool GetValue(Vector3d point, out int value);

            public Node(Vector3d min, Vector3d max, double epsilon)
            {
                Box = new AxisAlignedBox3d(min, max);
                this.epsilon = epsilon;
            }

            public record Branch : Node
            {

                private readonly Node[] nodes;

                public Branch(Leaf leaf) : base(leaf.Min, leaf.Max, leaf.epsilon)
                {

                    var center = Center;

                    var dirs = Max - Center;


                    Vector3d[] offsets = [
                        new (0, 0, 0),           // 0 0 0
                        new (dirs.x, 0, 0),      // 1 0 0
                        new (0, dirs.y, 0),      // 0 1 0
                        new (dirs.x, dirs.y, 0), // 1 1 0
                        new (0, 0, dirs.z),      // 0 0 1
                        new (dirs.x, 0, dirs.z), // 1 0 1
                        new (0, dirs.y, dirs.z), // 0 1 1
                        dirs,                    // 1 1 1

                    ];

                    nodes = offsets.Select(x => new Leaf(Min + x, Center + x, epsilon) as Node).ToArray();

                    foreach(var (p, val) in leaf.Points)
                    {
                        AddPoint(p, val);
                    }
                }

                public override bool AddPoint(Vector3d point, int value)
                {
                    if (!Box.Contains(point))
                        return false;

                    var index = GetNodeIndex(point);

                    var node = nodes[index];

                    if (!node.AddPoint(point, value))
                    {
                        if (node is not Leaf leaf)
                        {
                            return false;
                        }
                        node = new Branch(leaf);
                        nodes[index] = node;        
                    }

                    return node.AddPoint(point, value);
                }

                public override bool Contains(Vector3d point)
                {
                    if (!Box.Contains(point))
                        return false;

                    var index = GetNodeIndex(point);
                    return nodes[index].Contains(point);

                }

                public override bool GetValue(Vector3d point, out int value)
                {
                    if (!Box.Contains(point))
                    {
                        value = -1;
                        return false;
                    }

                    var index = GetNodeIndex(point);
                    return nodes[index].GetValue(point, out value);
                }

                private int GetNodeIndex(Vector3d point)
                {
                    var center = Center;
                    var a = point.x < center.x ? 0 : 1;
                    var b = point.y < center.y ? 0 : 2;
                    var c = point.z < center.z ? 0 : 4;
                    return a + b + c;
                }
            }

            public record Leaf : Node
            {
                private readonly List<(Vector3d Point, int Value)> points = [];

                public IEnumerable<(Vector3d, int)> Points => points;

                public Leaf(Vector3d min, Vector3d max, double epsilon) : base(min, max, epsilon)
                {

                }

                public override bool AddPoint(Vector3d point, int value)
                {
                    if (points.Count > 1024)
                        return false;

                    points.Add((point, value));
                    return true;
                }

                public override bool Contains(Vector3d point)
                    => points.Any(x => x.Point.EpsilonEqual(point, epsilon));

                public override bool GetValue(Vector3d point, out int value)
                {
                    value = -1;
                    
                    foreach(var p in points)
                    {
                        if (p.Point.EpsilonEqual(point, epsilon))
                        {
                            value = p.Value;
                            return true;
                        }
                    }

                    return false;
                }
            }
        }
    }
}
