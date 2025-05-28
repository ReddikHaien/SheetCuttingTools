using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;

namespace SheetCuttingTools.Grasshopper.Components.Data
{
    [Guid("FC325C7A-08DD-4560-9B54-31BB8D8B100B")]
    public class GeometryEdgeLabelsComponent() : GH_Component("Geometry Edge labels", "GEL", "Keeps track of assigned labels for an edge.", Constants.Category, Constants.HelperCategories)
    {
        private static readonly ConditionalWeakTable<string, EdgesState> states = [];

        public override Guid ComponentGuid => GetType().GUID;

        private string currentId;
        private EdgesState currentSet;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Edge", "E", "The edge to get a label for", GH_ParamAccess.item);
            pManager.AddTextParameter("Set Id", "Id", "The id for the stateful set, identical edges in the same set will have identical id, but might have different id in different sets", GH_ParamAccess.item, "42");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Label", "L", "The label for the incoming edge", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_ObjectWrapper edgeWrapper = new();
            if (!DA.GetData(0, ref edgeWrapper) || edgeWrapper.Value is not Edge edge)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get edge");
                return;
            }

            string setId = null!;
            if (!DA.GetData(1, ref setId))
            {
                setId = "42";
            }

            if (currentId != setId)
            {
                currentSet = states.GetOrCreateValue(setId);
                currentId = setId;
            }

            var label = currentSet.GetLabelForEdge(edge);

            DA.SetData(0, label);
        }

        internal class EdgesState
        {
            private readonly Dictionary<Edge, string> labels = [];

            public string GetLabelForEdge(Edge edge)
            {
                if(!labels.TryGetValue(edge, out var label))
                {
                    label = CreateLabel(edge);
                    labels.Add(edge, label);
                }

                return label;
            }

            private string CreateLabel(Edge edge)
            {
                return $"{labels.Count:X}";
            }
        }
    }

}
