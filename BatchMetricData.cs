using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticToCSV
{
    public class BatchMetricData
    {
        public string MetricName { get; set; }
        public string ItemsProcessed { get; set; }
        public string AttachmentsProcessed { get; set; }
        public string DBFetch { get; set; }
        public string DBUpdate { get; set; }
        public string Total { get; set; }
        public string VaultFetch { get; set; }
        public string RuleProcessing { get; set; }
    }
}
