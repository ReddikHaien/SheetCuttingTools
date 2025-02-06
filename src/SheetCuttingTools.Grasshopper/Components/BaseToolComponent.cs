using Grasshopper.Kernel;
using GrasshopperAsyncComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components
{
    public abstract class BaseToolComponent : GH_AsyncComponent
    {
        public BaseToolComponent(string name, string nickname, string description, string category, string subcategory) : base(name, nickname, description, category, subcategory)
        {
            BaseWorker = CreateWorker();
        }

        protected new ToolWorker BaseWorker
        {
            get => (ToolWorker)base.BaseWorker;
            private set => base.BaseWorker = value;
        }

        protected abstract ToolWorker CreateWorker();

        protected override void RegisterInputParams(GH_InputParamManager pManager)
            => CreateWorker().RegisterInputsParams(pManager);

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
            => CreateWorker().RegisterOutputParams(pManager);

        public override Guid ComponentGuid => GetType().GUID;

        protected abstract class ToolWorker(BaseToolComponent parent) : WorkerInstance(parent)
        {
            public abstract void RegisterInputsParams(GH_InputParamManager pManager);
            public abstract void RegisterOutputParams(GH_OutputParamManager pManager);


            public void AddWarningMessage(string message)
                => Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);

            public void AddErrorMessage(string message)
                => Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
        }

    }
}
