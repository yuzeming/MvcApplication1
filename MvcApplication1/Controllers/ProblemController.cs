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

        public ActionResult Index(int tag = 0)
        {
            var tmp = db.Submits.Where(m => m.User.UserId == WebSecurity.CurrentUserId)
                .GroupBy(m => m.Prob.ID)
                .Select(x => x.OrderByDescending(z => z.Time).FirstOrDefault())
                .ToList()
                .ToDictionary(y => y.Prob.ID);
            ViewBag.Record = tmp;
            var query = db.Problems.AsQueryable();
            if (User.Identity.Name != "root")
                query = query.Where(x => x.Public);
            if (tag != 0)
                query = query.Where(x => x.Tag.ID == tag);
            ViewBag.tagList = HelperFunc.GetTagList(tag);
            return View(query.ToList());
        }

        public ActionResult Details(int id = 0,int c = 0)
        {
            Problem problem = db.Problems.Find(id);
            Contest contest = db.Contests.Find(c);
            ViewBag.c = c;
            ViewBag.ContTitle = contest!=null ? contest.Title : null;
            if (problem == null)
                return HttpNotFound();
            if (c!=0)
            {
                if (contest == null)
                    return View("Error", new HttpException(403, "没有找到比赛。"));
                if (contest.State == ContestState.Before)
                    return View("Error", new HttpException(403, "比赛尚未开始。"));
                if (!contest.UserList.Any(x => x.UserName == User.Identity.Name))
                    return View("Error", new HttpException(403, "您没有参与这场比赛。"));
                ViewBag.ContState = contest.State;
                
                return View(problem);
            }
            if (User.Identity.Name == "root" || problem.Public)
                return View(problem);
            return View("Error", new HttpException(403, "题目是隐藏的。请尝试使用管理员账号登陆。"));
        }

        public ActionResult Create()
        {
            ViewBag.tagList = new SelectList(db.Tags, "ID", "Name");
            return View(new UploadProblemModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UploadProblemModel form)
        {
            Mapper.CreateMap<UploadProblemModel,Problem>()
                .ForMember( x => x.Tag , s => s.Ignore());
            var tmp = Mapper.Map<Problem>(form);
            tmp.Tag = db.Tags.Find(form.Tag);
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
            ViewBag.tagList = new SelectList(db.Tags, "ID", "Name", form.Tag);
            return View(form);
        }

        public ActionResult Edit(int id = 0)
        {
            var problem = db.Problems.Find(id);
            if (problem == null) return HttpNotFound();

            Mapper.CreateMap<Problem,UploadProblemModel>()
                .ForMember(x => x.Tag, s => s.MapFrom(y => y==null?0:y.Tag.ID));
            var tmp = Mapper.Map<UploadProblemModel>(problem);
            ViewBag.tagList = new SelectList(db.Tags, "ID", "Name", tmp.Tag);
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
                prob.Tag = ( form.Tag == 0 ? null : db.Tags.Find(form.Tag) );
                if (form.File != null)
                {
                    prob.CheckSum = HashFile(form.File.InputStream);
                    System.IO.File.Delete(GetZipPath(id));
                    form.File.SaveAs(GetZipPath(id));
                }
                db.Entry(prob).State = EntityState.Modified;
                //db.Problems.AddOrUpdate(prob);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            ViewBag.tagList = new SelectList(db.Tags, "ID", "Name", form.Tag);
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

        #region 帮助程序

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

        [NonAction]
        public string HashFile(Stream f)
        {
            f.Seek(0, SeekOrigin.Begin);
            var Cng = new SHA256Cng();
            var x = Cng.ComputeHash(f);
            return BitConverter.ToString(x).Replace("-", "").ToLower();
        }
        #endregion
    }
}