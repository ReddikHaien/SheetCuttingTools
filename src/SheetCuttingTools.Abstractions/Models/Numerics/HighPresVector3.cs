using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Models.Numerics
{
    public readonly struct HighPresVector3(double x, double y, double z)
    {
        public double X { get; } = x;
        public double Y { get; } = y;
        public double Z { get; } = z;

        public override string ToString()
            => $"{X}, {Y}, {Z}";
    }
}
