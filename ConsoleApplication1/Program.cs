using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

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
        static public string DownlandUrl = Host + "/Problem/Downland/{0}";
        static public WebClient wc = new WebClient();

        static public string DataDir = Path.Combine(WorkDir, "Prob");
        static public string SrcDir = Path.Combine(WorkDir, "Src");
        static public string TmpDir = Path.Combine(WorkDir, "Tmp");

        static public Dictionary<string, string[]> CompileConf = new Dictionary<string, string[]>();

        static public ZipArchive ReadZip(SubmitApiModels x)
        {

           if (!Directory.Exists(DataDir))
            Directory.CreateDirectory(DataDir);
           string fn = Path.Combine(DataDir, x.ProbID.ToString() + "_" + x.ProbCheckSum + ".zip");
           
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
            if (!Directory.Exists(SrcDir))
                Directory.CreateDirectory(SrcDir);
            string fn = Path.GetFullPath(Path.Combine(SrcDir,x.ID.ToString()+"."+x.Lang));
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

        static string CompileSrc(SubmitApiModels task, string srcfile, ref SubmitResultApiModels res)
        {
            if (!Directory.Exists(TmpDir))
                Directory.CreateDirectory(TmpDir);
            res.State = SubmitState.CompileError;
            if (!CompileConf.ContainsKey(task.Lang))
            {
                res.CompilerRes = "没有为这个语言配置编译器" + task.Lang;
                return null;
            }
            var exe = Path.GetFullPath(Path.Combine(TmpDir,task.ID.ToString()));
            var cc = CompileConf[task.Lang];
            var c = cc[0].Split(new char[] {' '},2);
            c[1] = c[1].Replace("{{SRC}}", srcfile).Replace("{{EXE}}", exe);
            var psi = new ProcessStartInfo();
            psi.CreateNoWindow = true;
            psi.WorkingDirectory = TmpDir;
            psi.FileName = c[0];
            psi.Arguments = c[1];
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            var ps = Process.Start(psi);
            ps.WaitForExit(10 * 1000); // 10s
            if (!ps.HasExited)
            {
                ps.Kill();
                res.CompilerRes = "编译超过时间限制。";
                return null;
            }
            res.CompilerRes = ps.StandardOutput.ReadToEnd() + ps.StandardError.ReadToEnd();
            if (ps.ExitCode != 0)
                return exe;
            return null;
        }

        static public void RunTest(string exe,)

        static void Main(string[] args)
        {
            var cpp = new string[]{ "g++ {{SRC}} -Wall -o {{EXE}}", "{{EXE}}" };
            CompileConf.Add("cpp", cpp);
            while (true)
            {
                var task = QueryTask();
                if (task == null)
                    continue;
                var zip = ReadZip(task);
                var src = SaveSrc(task);
                var res = new SubmitResultApiModels{ID=task.ID};
                var exe = CompileSrc(task, src, ref res);
                var config = JsonConvert.DeserializeObject<JsonConfig>(ReadZip(zip,"config.json"));
                foreach (var x in config.Data)
                {
                    RunTest(exe, x);
                }
                zip.Dispose();
            }
        }
    }
}
