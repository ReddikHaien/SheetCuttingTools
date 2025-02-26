using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Grasshopper.Models;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SheetCuttingTools.Grasshopper.Components.Converters
{
    [Guid("1BB6D6B6-74DB-489D-9401-263F552F3FE2")]
    public class GeometryConverterComponent() : GH_Component("Object to Geometry", "O2G", "Converts a Rhino object into a geometry object", Constants.Category, Constants.HelperCategories)
    {
        public override Guid ComponentGuid => GetType().GUID;

        protected override Bitmap Icon => Icons.Helpers_GeometryConverter;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Model", "M", "A Rhino model", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Geometry", "G", "A geometry object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            try
            {
                GH_ObjectWrapper wrapper = new();
                if (!DA.GetData(0, ref wrapper))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to get geometry");
                    return;
                }

                var geometry = wrapper.CreateGeometry();

                DA.SetData(0, new GH_Geometry(geometry));
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Something went wrong: {e}");
            }
        }
    }
}
