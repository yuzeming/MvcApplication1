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
    [Authorize]
    public class ProblemController : Controller
    {
        private MyDbContext db = new MyDbContext();

        public ActionResult Index()
        {
            var tmp = db.Submits.Where(m => m.User.UserId == WebSecurity.CurrentUserId)
                .GroupBy(m => m.Prob.ID)
                .Select(x => x.OrderByDescending(z => z.Time).FirstOrDefault())
                .ToList()
                .ToDictionary(y => y.Prob.ID);
            ViewBag.Record = tmp;
            return View(db.Problems.ToList());
        }

        public ActionResult Details(int id = 0,int? c = null)
        {
            Problem problem = db.Problems.Find(id);
            if (problem == null)
            {
                return HttpNotFound();
            }
            ViewBag.c = c;
            return View(problem);
        }


        public ActionResult Create()
        {
            return View(new UploadProblemModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UploadProblemModel form)
        {
            Mapper.CreateMap<UploadProblemModel,Problem>();
            var tmp = Mapper.Map<Problem>(form);
            if (form.File != null)
            {
                ReadConfig(ref tmp,form.File.InputStream);
            }
            else
            {
                ModelState.AddModelError("File","必须选择一个数据文件。");
            }
            if (ModelState.IsValid)
            {
                tmp.CheckSum = HashFile(form.File.InputStream);
                db.Problems.Add(tmp);
                db.SaveChanges();
                form.File.SaveAs(GetZipPath(tmp.ID));
                return RedirectToAction("Index");
            }
            return View(tmp);
        }

        public ActionResult Edit(int id = 0)
        {
            Problem problem = db.Problems.Find(id);
            if (problem == null) return HttpNotFound();

            Mapper.CreateMap<Problem,UploadProblemModel>();
            var tmp = Mapper.Map<UploadProblemModel>(problem);
            return View(tmp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UploadProblemModel form,int id = 0)
        {
            var prob = db.Problems.Find(id);
            if (prob == null)
            {
                return HttpNotFound();
            }
            if (form.File != null)
            {
                ReadConfig(ref prob, form.File.InputStream);
            }
            if (ModelState.IsValid)
            {
                prob.Title = form.Title;
                prob.Public = form.Public;
                prob.PublicData = form.PublicData;
                prob.CheckSum = HashFile(form.File.InputStream);
                db.Problems.AddOrUpdate(prob);
                db.SaveChanges();
                System.IO.File.Delete(GetZipPath(id));
                form.File.SaveAs(GetZipPath(id));
                return RedirectToAction("Index");
            }
            return View(form);
        }

        
        public ActionResult Delete(int id = 0)
        {
            Problem problem = db.Problems.Find(id);
            if (problem == null)
            {
                return HttpNotFound();
            }
            return View(problem);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Problem problem = db.Problems.Find(id);
            db.Problems.Remove(problem);
            db.SaveChanges();
            System.IO.File.Delete(GetZipPath(id));
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult Downland(int id)
        {
            Problem problem = db.Problems.Find(id);
            if (problem != null)
            {
                return File(GetZipPath(id), "application/zip", id.ToString() + ".zip");
            }
            return HttpNotFound();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        [NonAction]
        public string ReadZip(ZipArchive zip, string path)
        {
            var entry = zip.GetEntry(path);
            if (entry == null)
                return null;
            var reader = new StreamReader(entry.Open());
            var content = reader.ReadToEnd();
            reader.Close();
            return content;
        }

        [NonAction]
        public string GetZipPath(int id)
        {
            string path = Server.MapPath("~/Problems");
            string fn = System.IO.Path.Combine(path, id.ToString() + ".zip");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return fn;
        }

        [NonAction]
        public void ReadConfig(ref Problem tmp,Stream file)
        {
            try
            {
                var zip = new ZipArchive(file, ZipArchiveMode.Read);
                if (string.IsNullOrWhiteSpace(tmp.Title))
                {
                    JsonConfig cfg = JsonConvert.DeserializeObject<JsonConfig>(ReadZip(zip, "config.json"));
                    tmp.Title = cfg.Title;
                }
                tmp.Description = ReadZip(zip, "prob.html");
               // tmp.Solution = ReadZip(zip, "solve.html");
            }
            catch (InvalidDataException) 
            {
                ModelState.AddModelError("File", "请上传 ZIP 格式的压缩包");
            }
        }

        public string HashFile(Stream f)
        {
            f.Seek(0, SeekOrigin.Begin);
            var Cng = new SHA256Cng();
            var x = Cng.ComputeHash(f);
            return BitConverter.ToString(x).Replace("-", "").ToLower();
        }

    }
}