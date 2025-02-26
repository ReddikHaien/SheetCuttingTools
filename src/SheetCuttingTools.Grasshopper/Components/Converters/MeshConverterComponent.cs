using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SheetCuttingTools.Grasshopper.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Converters
{
    [Guid("D220485A-BC4F-48FA-80ED-7195FA38F482")]
    public class MeshConverterComponent() : GH_Component("Geometry to mesh", "G2M", "Converts a geometry to a mesh", Constants.Category,Constants.HelperCategories)
    {
        protected override Bitmap Icon => Icons.Helpers_MeshConverter;

        public override Guid ComponentGuid => GetType().GUID;
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Geometry", "G", "The geometry to convert to a mesh", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "The produced mesh.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_ObjectWrapper wrapper = new();
            if (!DA.GetData(0, ref wrapper))
            {
                return;
            }

            var geometry = wrapper.CreateGeometry();

            var mesh = MeshHelpers.CreateRhinoMesh(geometry);

            DA.SetData(0, mesh);
        }
    }
}
