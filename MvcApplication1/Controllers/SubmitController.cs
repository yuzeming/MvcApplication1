using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebMatrix.WebData;
using MvcApplication1.Models;
using MvcApplication1.FormModels;
using Newtonsoft.Json;
using AutoMapper;
using MvcApplication1.Filters;


namespace MvcApplication1.Controllers
{
    [Authorize]
    public class SubmitController : Controller
    {
        private MyDbContext db = new MyDbContext();

        [NonAction]
        public void SetUpdate(Contest c)
        {
            if (c != null)
            {
                c.Update = true;
                db.Entry(c).State = EntityState.Modified;
            }
        }

        public IQueryable<Submit> Search(IQueryable<Submit> query, SubmitSearchModel search)
        {
            if (search.S != null)
                query = query.Where(m => m.ID == search.S);
            if (search.C != null)
                query = query.Where(m => m.Belog.ID == search.C);
            if (search.P != null)
                query = query.Where(m => m.Prob.ID == search.P);

            if (!String.IsNullOrWhiteSpace(search.U))
                query = query.Where(m => m.User.UserName == search.U);
            if (search.State != null)
                query = query.Where(m => m.State == search.State);
            return query;
        }

        public bool isSearch(SubmitSearchModel search)
        {
            return search.P != null || search.S != null || search.C != null || !String.IsNullOrWhiteSpace(search.U) || search.State != null;
        }

        public List<SelectListItem> GetSelectListItem(SubmitSearchModel search)
        {
            var StateList = new List<SelectListItem>();
            StateList.Add(new SelectListItem{Text = "(状态)",Value = "",Selected = search.State == null});
            foreach (var x in Enum.GetNames(typeof(SubmitState)))
            {
                StateList.Add(new SelectListItem { Text = x, Value = x, Selected = x == search.State.ToString() });
            }
            return StateList;
        }


        public ActionResult Index(SubmitSearchModel search)
        {
            ViewBag.StateList = GetSelectListItem(search);
            var query = Search(db.Submits,search);
            ViewBag.Res =Pagination( query.OrderByDescending(m => m.Time)).ToList();
            ViewBag.isSearch = isSearch(search);
            return View(search);
        }

        [HttpPost, ActionName("Index")]
        [ValidateAntiForgeryToken]
        [AuthorizeAttribute(Users = "root")]
        public ActionResult IndexPost(SubmitSearchModel search) 
        {
            var query = Search(db.Submits,search);
            if (Request.Form["Rejudge"] == "REJDGE")
            {
                foreach (var x in query.ToList())
                {
                    x.State = SubmitState.Waiting;
                    SetUpdate(x.Belog);
                    db.Entry(x).State = EntityState.Modified;
                }
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult Details(int id = 0)
        {
            Submit submit = db.Submits.Find(id);
            if (submit == null)
            {
                return HttpNotFound();
            }
            ViewBag.Res = JsonConvert.DeserializeObject(
                string.IsNullOrEmpty(submit.Result) ? "[]" : submit.Result
            );
            return View(submit);
        }

        public ActionResult Create(int id = 0,int c = 0)
        {
            var tmp = db.Problems.Find(id);
            if (tmp == null)
                return HttpNotFound();
            ViewBag.ProbTitle = tmp.Title;
            if (c!=0)
            {
                var contest = db.Contests.Find(c);
                if (contest == null)
                    return View("Error", new HttpException(403, "没有找到比赛"));
                if (contest.State != ContestState.Running)
                    return View("Error", new HttpException(403, "比赛已经结束或尚未开始。"));
                if (!contest.UserList.Any(x => x.UserName == User.Identity.Name))
                    return View("Error", new HttpException(403, "您没有参与这场比赛。"));
                ViewBag.ContTitle = contest.Title;
            }

            return View(new SubmitModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult Create(SubmitModel submit,int id = 0,int c = 0)
        {
            var prob = db.Problems.Find(id);
            if (prob == null)
                return HttpNotFound();
            var tmp = CreateSubmit(prob, submit);
            if (c != 0)
            {
                var tc = db.Contests.Find(c);
                tmp.Belog = tc;
                SetUpdate(tc);
            }
            if (ModelState.IsValid)
            {
                db.Submits.Add(tmp);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(submit);
        }

        public ActionResult Delete(int id = 0)
        {
            Submit submit = db.Submits.Find(id);
            if (submit == null)
            {
                return HttpNotFound();
            }
            return View(submit);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Submit submit = db.Submits.Find(id);
            SetUpdate(submit.Belog);
            db.Submits.Remove(submit);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        #region 帮助程序

        [NonAction]
        public Submit CreateSubmit(Problem prob,SubmitModel x)
        {
            var ret = new Submit();
            ret.User = db.UserProfiles.Find(WebSecurity.CurrentUserId);
            ret.Prob = prob;
            ret.Lang = x.Lang;
            ret.Source = x.Src;
            return ret;
        }

        [NonAction]
        public IQueryable<Submit> Pagination(IQueryable<Submit> tmp)
        {
            int PAGE_SIZE = 30;
            int page = 1, totpage = (tmp.Count() + PAGE_SIZE - 1) / PAGE_SIZE; 
            if (Request.QueryString["page"] != null)
            {
                Int32.TryParse(Request.QueryString["page"], out page);
            }
            ViewBag.page = page; ViewBag.totpage = totpage;  //数据传递给ViewBag
            return tmp.Skip((page - 1) * PAGE_SIZE).Take(PAGE_SIZE);
        }

        #endregion
    }
}