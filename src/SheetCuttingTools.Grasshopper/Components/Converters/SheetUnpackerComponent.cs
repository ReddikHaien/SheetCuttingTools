﻿using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Grasshopper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Converters
{
    [Guid("0B61CCB7-7DC5-4E07-B9E4-3301A4588B27")]
    public class SheetUnpackerComponent() : GH_Component("Sheet Unpacker", "SU", "Unpacks a sheet into curves and category names", Constants.Category, Constants.HelperCategories)
    {
        public override Guid ComponentGuid => GetType().GUID;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sheet", "S", "A Sheet to unpack into a tree", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Lines", "L", "The lines on the sheet", GH_ParamAccess.tree);
            pManager.AddTextParameter("Categories", "C", "THe line categories on the sheet", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_ObjectWrapper sheetWrapper = new();
            if(!DA.GetData(0, ref sheetWrapper))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get data!");
            }

            Sheet sheet = null!;

            if (sheetWrapper.Value is GH_Sheet ghsheet)
                sheet = ghsheet.Value;
            else if (sheetWrapper.Value is Sheet s)
                sheet = s;
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provided value is not a sheet");
                return;
            }

            DataTree<Curve> curves = new();
            List<string> categories = [];

            int i = 0;
            foreach(var group in sheet.Lines)
            {
                categories.Add(group.Key);
                foreach(var line in group)
                {
                    var path = new GH_Path(0, DA.Iteration, i);
                    var poly = new Polyline(line.Length);
                    poly.AddRange(line.Select(x => (Point3d)x.ToPoint3f()));
                    curves.Add(poly.ToPolylineCurve(), path);
                }
                i++;
            }

            categories.Add("EdgeLabels");
            foreach(var (edge, name) in sheet.BoundaryNames)
            {
                var (a, b) = sheet.FlattenedSegment.GetEdge(edge);
                var p = (a + b) / 2;

                var plane = Plane.WorldXY;

                plane.Origin = p.ToPoint3f();

                var obj = new TextEntity()
                {
                    PlainText = name,
                    Plane = plane,
                    TextHeight = 3,
                };

                sheet.FlattenedSegment.

                var c = obj.Explode();

                curves.AddRange(c, new(0, DA.Iteration, i));
            }

            DA.SetDataTree(0, curves);
            DA.SetDataList(1, categories);
        }
    }
}
