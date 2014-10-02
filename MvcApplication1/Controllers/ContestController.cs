using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcApplication1.Models;
using MvcApplication1.FormModels;
using AutoMapper;
using Newtonsoft.Json;

namespace MvcApplication1.Controllers
{
    public class ContestController : Controller
    {
        private MyDbContext db = new MyDbContext();

        public ContestController()
        {
            Mapper.CreateMap<ContestFormModel, Contest>();
            Mapper.CreateMap<Contest, ContestFormModel>()
                .ForMember(x => x.ProbStr, s => s.MapFrom(z => GetProbStr(z)))
                .ForMember(x => x.UserStr, s => s.MapFrom(z => GetUserStr(z)))
                .ForMember(x => x.Tag, s => s.MapFrom(z => z.Tag.ID));
        }

        public ActionResult Index(int tag=0)
        {
            var query = db.Contests.AsQueryable();
            if (tag != 0)
                query = query.Where(x => x.Tag.ID == tag);
            ViewBag.tagList = HelperFunc.GetTagList(tag);
            return View(query.ToList());
        }

        public ActionResult Details(int id = 0)
        {
            Contest contest = db.Contests.Find(id);
            if (contest == null)
                return View("Error", new HttpException(403, "没有找到比赛。"));
            if (contest.State == ContestState.Before)
                return View("Error", new HttpException(403, "比赛尚未开始。"));
            if (!contest.UserList.Any(x => x.UserName == User.Identity.Name))
                return View("Error", new HttpException(403, "您没有参与这场比赛。"));
            return View(contest);
        }

        [Authorize(Roles = "admin")]
        public ActionResult Create()
        {
            ViewBag.tagList = new SelectList(db.Tags, "ID", "Name");
            return View(new ContestFormModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public ActionResult Create(ContestFormModel form)
        {
            var tmp = toContest(form);
            if (ModelState.IsValid)
            {
                db.Contests.Add(tmp);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.tagList = new SelectList(db.Tags, "ID", "Name",form.Tag);
            return View(form);
        }

        [Authorize(Roles = "admin")]
        public ActionResult Edit(int id = 0)
        {
            Contest contest = db.Contests.Find(id);
            if (contest == null)
            {
                return HttpNotFound();
            }
            ViewBag.tagList = new SelectList(db.Tags, "ID", "Name",contest.Tag.ID);
            return View(Mapper.Map<ContestFormModel>(contest));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public ActionResult Edit(int id,ContestFormModel form)
        {
            var tmp = db.Contests.Find(id);
            tmp.Title = form.Title;
            tmp.End = form.End;
            tmp.Start = form.Start;
            tmp.Public = form.Public;
            tmp.Tag = db.Tags.Find(form.Tag);
            tmp.ProbList.Clear();
            foreach (var p in GetProbList(form))
                tmp.ProbList.Add(p);

            tmp.UserList.Clear();
            foreach (var p in GetUserList(form))
                tmp.UserList.Add(p);

            tmp.Update = true;
            if (ModelState.IsValid)
            {
                db.Contests.AddOrUpdate(tmp);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.tagList = new SelectList(db.Tags, "ID", "Name",form.Tag);
            return View(form);
        }

        [Authorize(Roles = "admin")]
        public ActionResult Delete(int id = 0)
        {
            Contest contest = db.Contests.Find(id);
            if (contest == null)
            {
                return HttpNotFound();
            }
            return View(contest);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            Contest contest = db.Contests.Find(id);
            db.Contests.Remove(contest);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Rank(int id,string type="html")
        {
            var contest = db.Contests.Find(id);
            if (contest == null)
                return HttpNotFound();

            if (!contest.UserList.Any(x => x.UserName == User.Identity.Name))
                return View("Error", new HttpException(403, "您没有参与这场比赛。"));
            if (contest.State == ContestState.Before)
                return View("Error", new HttpException(403, "比赛还没有开始"));
            List<JsonContestReslut> res;
            if (contest.Update || String.IsNullOrWhiteSpace(contest.Result))
            {
                res = GetReslutList(contest);
                contest.Result = JsonConvert.SerializeObject(res);
                contest.Update = false;
                db.Entry(contest).State = EntityState.Modified;
                db.SaveChanges();
            }
            else
                res = JsonConvert.DeserializeObject<List<JsonContestReslut>>(contest.Result);
            ViewBag.prob = contest.ProbList.OrderBy( x => x.ID).ToList();
            ViewBag.C = id;
            ViewBag.CTitle = contest.Title;
            if (type == "html")
                return View(res);
            if (type == "csv")
                return new FileContentResult(
                    System.Text.Encoding.UTF8.GetBytes(
                        CSVFile(ViewBag.prob, res)
                     ),
                    "Application/x-csv") { FileDownloadName = "导出的成绩.csv" };
                 
            return HttpNotFound();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }


        #region 帮助程序

        public string CSVFile(List<Problem> prob,List<JsonContestReslut> res)
        {
            string csv = "User,";
            foreach (var p in prob)
            {
                csv+=p.Title+"("+p.ID.ToString()+"),";
            }
            csv += "Total\n";
            foreach (var r in res)
            {
                csv+=r.UserName+",";
                foreach (var x in r.Details)
                    csv += x+",";
                csv+=r.Src +"\n";
            }
            return csv;
        }

        public Contest toContest(ContestFormModel form)
        {
            var tmp = new Contest();
            tmp.Title = form.Title;
            tmp.Start = form.Start;
            tmp.End = form.End;
            tmp.Public = form.Public;
            tmp.ProbList = GetProbList(form);
            tmp.UserList = GetUserList(form);
            tmp.Tag = db.Tags.Find(form.Tag);
            if (tmp.Start > tmp.End)
                ModelState.AddModelError("Start", "开始时间不能晚于结束时间");

            return tmp;
        }

        [NonAction]
        public List<JsonContestReslut> GetReslutList(Contest cont, List<Problem> prob = null, List<UserProfile> user = null)
        {
            var ret = new List<JsonContestReslut>();
            var q = db.Submits.AsQueryable();
            if (cont != null)
            {
                q.Where(x => x.Belog == cont);
                if (prob == null)
                    prob = cont.ProbList.OrderBy( x => x.ID).ToList();
                if (user == null)
                    user = cont.UserList.OrderBy(x => x.UserId).ToList();
            }
            foreach (var u in user)
            {
                var tmp = new JsonContestReslut { Src = 0, UserName = u.UserName, Details = new List<String>() };
                var qu = q.Where(x => x.User.UserId == u.UserId);
                foreach (var p in prob)
                {
                    var qp = qu.Where(x => x.Prob.ID == p.ID).OrderByDescending(x => x.ID).OrderBy(x => x.State).FirstOrDefault();
                    if (qp == null)
                        tmp.Details.Add("(未提交)");
                    else
                    {
                        if (qp.State == SubmitState.Running || qp.State == SubmitState.Waiting)
                            tmp.Details.Add(qp.State.ToString());
                        else
                        {
                            tmp.Details.Add(qp.State.ToString() + " (" + qp.Score + ")");
                            tmp.Src += qp.Score;
                        }
                    }
                }
                ret.Add(tmp);
            }
            ret.Sort( delegate(JsonContestReslut a, JsonContestReslut b) {
                return b.Src - a.Src;
            });
            return ret;
        }

        public List<UserProfile> GetUserList(ContestFormModel x)
        {
            if (String.IsNullOrWhiteSpace(x.UserStr))
                return null;
            var tmpUser = new List<UserProfile>();
            foreach (var i in x.UserStr.Split(new char[] { ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var tmp = db.UserProfiles.FirstOrDefault(m => m.UserName == i);
                if (tmp == null)
                {
                    ModelState.AddModelError("UserStr", "无法找到这个用户" + i);
                    return null;
                }
                tmpUser.Add(tmp);
            }
            return tmpUser;
        }

        public string GetUserStr(Contest x)
        {
            string ret = "";
            foreach (var i in x.UserList)
                ret += i.UserName + "\n";
            return ret;
        }

        public List<Problem> GetProbList(ContestFormModel x)
        {
            var tmpPorb = new List<Problem>();
            if (String.IsNullOrWhiteSpace(x.ProbStr))
                return tmpPorb;
            foreach (var s in x.ProbStr.Split(new char[] { ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int i;
                if (!Int32.TryParse(s, out i))
                {
                    ModelState.AddModelError("ProbStr", "这不是一个数字" + i);
                    return null;
                }
                var tmp = db.Problems.Find(i);
                if (tmp == null)
                {
                    ModelState.AddModelError("ProbStr", "无法找到这个题目" + i);
                    return null;
                }
                tmpPorb.Add(tmp);
            }
            return tmpPorb;
        }

        public string GetProbStr(Contest x)
        {
            string ret = "";
            foreach (var i in x.ProbList)
                ret += i.ID.ToString() + "\n";
            return ret;
        }
        #endregion
    }
}