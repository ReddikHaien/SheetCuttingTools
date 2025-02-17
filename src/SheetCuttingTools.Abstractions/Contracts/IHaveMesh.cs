using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Contracts
{
    public interface IHaveMesh
    {
        public DMesh3 Mesh { get; }
    }
}
