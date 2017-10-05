using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticToCSV
{
    public class EARMetricData
    {
        public string FASA { get; set; }

        public int MessageCount { get; set; }

        public int EnvelopeCount { get; set; }

        public int DupeCount { get; set; }

        public int Duration { get; set; }
        /// more properties here
    }
}
