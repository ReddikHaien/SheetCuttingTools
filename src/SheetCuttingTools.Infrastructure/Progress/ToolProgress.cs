using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Infrastructure.Progress
{
    public class ToolProgress(string id, Action<string, double> reporter) : IProgress<double>
    {
        private readonly string id = id;
        private readonly Action<string, double> reporter = reporter;

        public void Report(double value)
            => reporter.Invoke(id, value);
    }
}
