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
        public static List<SelectListItem> GetTagList(int nowselect = 0)
        {
            var db = new MyDbContext();
            var tags = db.Tags.ToList();
            var ret = new List<SelectListItem>();
            ret.Add(new SelectListItem() { Value = "0", Text = "(全部)", Selected = (nowselect == 0) });
            foreach (var x in tags)
                ret.Add(new SelectListItem() { Value = x.ID.ToString(), Text = x.Name, Selected = (nowselect == x.ID) });
            db.Dispose();
            return ret;
        }
    }

    public class CanUseReadContestFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var db = new MyDbContext();
            int? id = filterContext.ActionParameters["id"] as Int32?;
            Contest contest = db.Contests.Find(id);
            if (contest == null)
                throw new HttpException(404, "没有这样的比赛");
            if (!contest.UserList.Any(x => x.UserName == filterContext.HttpContext.User.Identity.Name))
                throw new HttpException(403, "您没有参与这场比赛。");
            if (contest.State == ContestState.Before)
                throw new HttpException(403, "比赛还没有开始");
        }
    }
}