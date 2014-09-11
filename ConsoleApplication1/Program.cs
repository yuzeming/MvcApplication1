using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;


namespace ConsoleApplication1
{
    public class JsonConfig
    {
        public string Title { get; set; }
        public string[][] Data { get; set; }
    }

    public class SubmitApiModels
    {
        public int ID { get; set; }
        public string Lang { get; set; }
        public int ProbID { get; set; }
        public string ProbCheckSum { get; set; }
        public string Source { get; set; }
    }

    public enum SubmitState
    {
        Waiting,
        Running,

        Accepted,
        TimeLimitExceeded,
        MemoryLimitExceeded,
        WrongAnswer,
        RuntimeError,
        OutputLimitExceeded,
        CompileError,
        SystemError,
        ValidatorError,
    }

    public class SubmitResultApiModels
    {
        public int ID { get; set; }
        public int Score { get; set; }
        public SubmitState State { get; set; }
        public string Result { get; set; }
        public string CompilerRes { get; set; }
    }

    public class Program
    {
        static public string Host = "http://localhost:54764";
        static public string WorkDir = Environment.CurrentDirectory;

        static public string ApiUrl = Host + "/api/Judge";
        static public string DownlandUrl = Host + "/Problem/{0}/Downland";
        static public WebClient wc = new WebClient();

        static public string DataDir = Path.Combine(WorkDir, "Prob");
        static public string SrcDir = Path.Combine(WorkDir, "Src");

        static public ZipArchive ReadZip(SubmitApiModels x)
        {
           
           string fn = Path.Combine(DataDir,x.ProbID.ToString() + "_" + x.ProbCheckSum);
           
           if (!File.Exists(fn))
           {
               string url =String.Format(DownlandUrl,x.ProbID);
               wc.DownloadFile(url, fn);
           }
           return new ZipArchive( new FileStream(fn,FileMode.Open),ZipArchiveMode.Read);
        }

        static public SubmitApiModels QueryTask()
        {
            string str = wc.DownloadString(ApiUrl);
            var task = JsonConvert.DeserializeObject<SubmitApiModels>(str);
            return task;
        }

        static public string SaveSrc(SubmitApiModels x)
        {
            string fn = Path.Combine(SrcDir,x.ID.ToString()+"."+x.Lang);
            var f = new StreamWriter(fn,false);
            f.Write(x.Source);
            f.Flush();
            f.Close();
            return fn;
        }

        static public string ReadZip(ZipArchive zip, string path)
        {
            var entry = zip.GetEntry(path);
            if (entry == null)
                return null;
            var reader = new StreamReader(entry.Open());
            var content = reader.ReadToEnd();
            reader.Close();
            return content;
        }

        static void Main(string[] args)
        {

            
            while (true)
            {
                var task = QueryTask();
                if (task == null)
                    continue;
                var zip = ReadZip(task);
                var src = SaveSrc(task);
                var config = JsonConvert.DeserializeObject<JsonConfig>(ReadZip(zip,"config.json"));
                foreach (var x in config.Data)
                {
                    Console.Write(x.ToString());
                }

            }
        }
    }
}
