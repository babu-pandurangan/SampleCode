using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiagnosticToCSV
{
    class Program
    {
        static void Main (string[] args)
        {
            ConversionEARMain(args);
            //
            //ListTest();
        }

        static void ListTest ()
        {
            List<int> testList = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
            for(List<int> subList = testList.Take(6).ToList(); subList.Count > 0; subList = testList.Take(6).ToList())
            {
                testList.RemoveRange(0, subList.Count);
                Trace.WriteLine($"Get {subList.Count}");
            }
            Trace.WriteLine("Done");
        }
        static void ConversionEARMain(string[] args)
        {
            if (args.Length > 0)
            {
                //
                foreach (string folder in args)
                {
                    string fasaMetrics = Path.Combine(folder, "ear_fasa_perf.csv");
                    using (StreamWriter fasaWriter = new StreamWriter(fasaMetrics))
                    {
                        fasaWriter.WriteLine("time, FasaId, Message, Envelope, Dupe, Duration");
                        ProcessFolder(folder, null, fasaWriter);
                    }
                }
            }
        }

        static void ConversionMain(string[] args)
        {
            if (args.Length > 0)
            {
                //
                foreach (string folder in args)
                {
                    string batchMetrics = Path.Combine(folder, "vco_batch_perf.csv");
                    string fasaMetrics = Path.Combine(folder, "vco_fasa_perf.csv");
                    using (StreamWriter batchWriter = new StreamWriter(batchMetrics))
                    {
                        using (StreamWriter fasaWriter = new StreamWriter(fasaMetrics))
                        {
                            batchWriter.WriteLine("time, ItemsProcessed, AttachmensProcessed, RuleProcessing, DBRead, DBUpdate, Vault Read, Total");
                            fasaWriter.WriteLine("time, FasaId, ItemsProcessed, AttachmentsProcessed, RuleProcessing, VaultFetch, Total");
                            ProcessFolder(folder, batchWriter, fasaWriter);
                        }
                    }
                }
            }
        }

        private static void ProcessFolder(string folder, StreamWriter batchWriter, StreamWriter fasaWriter)
        {
            DirectoryInfo di = new DirectoryInfo(folder);
            foreach (FileInfo fi in di.GetFiles("*.log"))
            {
                //
                if (batchWriter != null)
                    ProcessFile(fi, batchWriter, fasaWriter);
                else
                    ProcessEARFile(fi, fasaWriter);
            }
            foreach (DirectoryInfo subDir in di.GetDirectories()) ProcessFolder(subDir.FullName, batchWriter, fasaWriter);
        }
        private static void ProcessEARFile(FileInfo fi, StreamWriter fasaWriter)
        {
            //
            using (StreamReader reader = new StreamReader(fi.FullName))
            {
                long lineCount = 0;
                Regex r = new Regex("([a-zA-Z0-9_]+)=([a-zA-Z-.0-9]+)");
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    if (line.Length > 100) continue;
                    string time = line.Substring(0, 23);
                    EARMetricData metric = GetEARMetricDataFromLine(r, line);
                    fasaWriter.WriteLine($"{time}, {metric.FASA}, {metric.MessageCount}, {metric.EnvelopeCount}, {metric.DupeCount}, {metric.Duration}");
                    // Parse Line
                    lineCount++;
                }
                Console.Out.WriteLine("Done reading {0} lines from {1}", lineCount, fi.FullName);
            }
        }

        private static void ProcessFile (FileInfo fi, StreamWriter batchWriter, StreamWriter fasaWriter)
        {
            //
            using (StreamReader reader = new StreamReader(fi.FullName))
            {
                long lineCount = 0;
                Regex r = new Regex("([a-zA-Z0-9_]+)=([a-zA-Z-.0-9]+)");
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    string time = line.Substring(0, 24);
                    BatchMetricData metric = GetBatchMetricDataFromLine(r, line);
                    if (metric.MetricName == "BatchProcessing")
                        batchWriter.WriteLine($"{time}, {metric.ItemsProcessed}, {metric.AttachmentsProcessed}, {metric.RuleProcessing}, {metric.DBFetch}, {metric.DBUpdate}, {metric.VaultFetch}, {metric.Total}");
                    if (metric.MetricName == "MessagesScanning")
                    {
                        //
                        FasaMetricData fasaMetric = GetFasaMetricDataFromLine(r, line);
                        if (!string.IsNullOrEmpty(fasaMetric.RuleProcessing))
                            fasaWriter.WriteLine($"{time}, {fasaMetric.FasaId}, {fasaMetric.ItemsProcessed}, {fasaMetric.AttachmentsProcessed}, {fasaMetric.RuleProcessing}, {fasaMetric.VaultFetch}, {fasaMetric.Total}");
                    }
                    // Parse Line
                    lineCount++;
                }
                Console.Out.WriteLine("Done reading {0} lines from {1}", lineCount, fi.FullName);
            }
        }
        private static EARMetricData GetEARMetricDataFromLine(Regex r, string line)
        {
            EARMetricData returnVal = new EARMetricData();
            //
            foreach (Match m in r.Matches(line))
            {
                string key = m.Value.Split('=')[0];
                string value = m.Value.Split('=')[1];
                switch (key)
                {
                    case "FasaId":
                        returnVal.FASA = value;
                        break;
                    case "MESSAGE_COUNT":
                        returnVal.MessageCount = int.Parse(value);
                        break;
                    case "ENV_COUNT":
                        returnVal.EnvelopeCount = int.Parse(value);
                        break;
                    case "DUPE_COUNT":
                        returnVal.DupeCount = int.Parse(value);
                        break;
                    case "Duration":
                        returnVal.Duration = int.Parse(value);
                        break;
                }
            }
            return returnVal;
        }

        private static BatchMetricData GetBatchMetricDataFromLine(Regex r, string line)
        {
            BatchMetricData returnVal = new BatchMetricData();
            foreach (Match m in r.Matches(line))
            {
                string key = m.Value.Split('=')[0];
                string value = m.Value.Split('=')[1];
                switch (key)
                {
                    case "MetricName":
                        returnVal.MetricName = value;
                        break;
                    case "BatchRulesEngineProcessing":
                        returnVal.RuleProcessing = value.Remove(value.Length - 2, 2);
                        break;
                    case "ItemsProcessed":
                        returnVal.ItemsProcessed = value;
                        break;
                    case "AttachmentsProcessed":
                        returnVal.AttachmentsProcessed = value;
                        break;
                    case "DBBatchDataFetch":
                        returnVal.DBFetch = value.Remove(value.Length - 2, 2);
                        break;
                    case "BatchVaultRead":
                        returnVal.VaultFetch = value.Remove(value.Length - 2, 2);
                        break;
                    case "DBBatchStatusUpdate":
                        returnVal.DBUpdate = value.Remove(value.Length - 2, 2);
                        break;
                    case "Total":
                        returnVal.Total = value.Remove(value.Length - 2, 2);
                        break;
                }
            }
            return returnVal;
        }
        private static FasaMetricData GetFasaMetricDataFromLine(Regex r, string line)
        {
            FasaMetricData returnVal = new FasaMetricData();
            foreach (Match m in r.Matches(line))
            {
                string key = m.Value.Split('=')[0];
                string value = m.Value.Split('=')[1];
                switch (key)
                {
                    case "FasaId":
                        returnVal.FasaId = value;
                        break;
                    case "RulesEngineScanning":
                        returnVal.RuleProcessing = value.Remove(value.Length - 2, 2);
                        break;
                    case "ItemsProcessed":
                        returnVal.ItemsProcessed = value;
                        break;
                    case "AttachmentsProcessed":
                        returnVal.AttachmentsProcessed = value;
                        break;
                    case "VaultRead":
                        returnVal.VaultFetch = value.Remove(value.Length - 2, 2);
                        break;
                    case "Total":
                        returnVal.Total = value.Remove(value.Length - 2, 2);
                        break;
                }
            }
            return returnVal;
        }
    }
}
