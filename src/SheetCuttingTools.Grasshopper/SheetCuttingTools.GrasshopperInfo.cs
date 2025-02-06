using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SheetCuttingTools.Grasshopper
{
    [Guid("AFD6E88C-0768-4BE0-B7FF-7EF488FC70E8")]
    public class SheetCuttingTools_GrasshopperInfo : GH_AssemblyInfo
    {
        public override string Name => "SheetCuttingTools.Grasshopper";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => GetType().GUID;

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}