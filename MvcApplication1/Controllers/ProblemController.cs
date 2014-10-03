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
            if (!User.IsInRole("admin"))
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
            if (User.IsInRole("admin") || problem.Public)
                return View(problem);
            return View("Error", new HttpException(403, "题目是隐藏的。请尝试使用管理员账号登陆。"));
        }

        [Authorize(Roles="admin")]
        public ActionResult Create()
        {
            ViewBag.tagList = new SelectList(db.Tags, "ID", "Name");
            return View(new UploadProblemModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public ActionResult Create(UploadProblemModel form)
        {
            Mapper.CreateMap<UploadProblemModel,Problem>()
                .ForMember( x => x.Tag , s => s.Ignore());
            var tmp = Mapper.Map<Problem>(form);
            tmp.Tag = db.Tags.Find(form.Tag);
            if (form.File == null || !HelperFunc.ReadConfig(ref tmp, form.File.InputStream))
                ModelState.AddModelError("File","必须选择一个有效的数据文件。");

            if (ModelState.IsValid)
            {
                tmp.CheckSum = HelperFunc.HashFile(form.File.InputStream);
                db.Problems.Add(tmp);
                db.SaveChanges();
                form.File.SaveAs(HelperFunc.GetZipPath(tmp.ID));
                return RedirectToAction("Index");
            }
            ViewBag.tagList = new SelectList(db.Tags, "ID", "Name", form.Tag);
            return View(form);
        }

        [Authorize(Roles = "admin")]
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
        [Authorize(Roles = "admin")]
        public ActionResult Edit(UploadProblemModel form,int id = 0)
        {
            var prob = db.Problems.Find(id);
            if (prob == null)
                return HttpNotFound();
            if (form.File != null)
                if(!HelperFunc.ReadConfig(ref prob, form.File.InputStream))
                    ModelState.AddModelError("File", "必须选择一个有效的数据文件。");

            if (ModelState.IsValid)
            {
                prob.Title = form.Title;
                prob.Public = form.Public;
                prob.PublicData = form.PublicData;
                prob.Tag = db.Tags.Find(form.Tag) ;
                if (form.File != null)
                {
                    prob.CheckSum = HelperFunc.HashFile(form.File.InputStream);
                    System.IO.File.Delete(HelperFunc.GetZipPath(id));
                    form.File.SaveAs(HelperFunc.GetZipPath(id));
                }
                db.Entry(prob).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            ViewBag.tagList = new SelectList(db.Tags, "ID", "Name", form.Tag);
            return View(form);
        }

        [Authorize(Roles = "admin")]
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
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Problem problem = db.Problems.Find(id);
            db.Problems.Remove(problem);
            db.SaveChanges();
            System.IO.File.Delete(HelperFunc.GetZipPath(id));
            return RedirectToAction("Index");
        }

        public ActionResult Downland(int id)
        {
            Problem problem = db.Problems.Find(id);
            if (problem == null)
                return HttpNotFound();
            if (problem.PublicData || User.IsInRole("admin"))
                return File(HelperFunc.GetZipPath(id), "application/zip", id.ToString() + ".zip");
            return View("Error", new HttpException(403, "没有权限下载数据"));
        }

        public ActionResult file(int id,string fn)
        {
            Problem problem = db.Problems.Find(id);
            if (problem == null)
                return HttpNotFound();
            if (problem.Public || User.IsInRole("admin"))
            {
                return Content(id.ToString()+" "+fn);
            }
            return View("Error", new HttpException(403, "题目是隐藏的。请尝试使用管理员账号登陆。"));
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

    }
}