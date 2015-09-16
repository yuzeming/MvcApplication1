using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using MvcApplication1.Models;
using MvcApplication1.FormModels;
using MvcApplication1.Filters;
using Newtonsoft.Json;
using WebMatrix.WebData;
using AutoMapper;

namespace MvcApplication1.Controllers
{
    public static  class HelperFunc
    {
        public static List<SelectListItem> GetTagList(int nowselect = 0,String ZeroText = "(全部)")
        {
            var db = new MyDbContext();
            var tags = db.Tags.ToList();
            var ret = new List<SelectListItem>();
            ret.Add(new SelectListItem() { Value = "0", Text = ZeroText, Selected = (nowselect == 0) });
            foreach (var x in tags)
                ret.Add(new SelectListItem() { Value = x.ID.ToString(), Text = x.Name, Selected = (nowselect == x.ID) });
            db.Dispose();
            return ret;
        }

        public static string ReadZip(ZipArchive zip, string path)
        {
            var entry = zip.GetEntry(path);
            if (entry == null)
                return null;
            var reader = new StreamReader(entry.Open());
            var content = reader.ReadToEnd();
            reader.Close();
            return content;
        }

        public static Stream ReadZipStream(ZipArchive zip, string path)
        {
            var entry = zip.GetEntry(path);
            if (entry == null)
                return null;
            return entry.Open();
        }

        public static string HashFile(Stream f)
        {
            f.Seek(0, SeekOrigin.Begin);
            var Cng = new SHA256Cng();
            var x = Cng.ComputeHash(f);
            return BitConverter.ToString(x).Replace("-", "").ToLower();
        }

        public static string GetZipPath(int id)
        {
            string path = HttpContext.Current.Server.MapPath("~/Problems");
            string fn = System.IO.Path.Combine(path, id.ToString() + ".zip");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return fn;
        }

        public static bool ReadConfig(ref Problem tmp, Stream file)
        {
            try
            {
                tmp.FileList = new List<String>();
                var zip = new ZipArchive(file, ZipArchiveMode.Read);
                if (string.IsNullOrWhiteSpace(tmp.Title))
                {
                    JsonConfig cfg = JsonConvert.DeserializeObject<JsonConfig>(HelperFunc.ReadZip(zip, "config.json"));
                    tmp.Title = cfg.Title;
                }
                foreach (var entry in zip.Entries)
                {
                    if (entry.FullName.StartsWith("file/",StringComparison.OrdinalIgnoreCase) && entry.FullName.Length > 5)
                    {
                        tmp.FileList.Add(entry.FullName);
                    }
                }
                tmp.Description = HelperFunc.ReadZip(zip, "prob.html");
                tmp.Solution = HelperFunc.ReadZip(zip, "solve.html");
            }
            catch (InvalidDataException)
            {
                return false;
            }
            return true;
        }
    }

}