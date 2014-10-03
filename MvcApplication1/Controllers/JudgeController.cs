using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using AutoMapper;
using MvcApplication1.Models;
using MvcApplication1.Controllers;
using System.Net.Http.Headers;

namespace MvcApplication1.Controllers
{

    public class SubmitApiModels
    {
        public int ID { get; set; }
        public string Lang { get; set; }
        public int ProbID { get; set; }
        public string ProbCheckSum { get; set; }
        public string Source { get; set; }
    }

    public class SubmitResultApiModels
    {
        public int ID { get; set; }
        public int Score { get; set; }
        public SubmitState State { get; set; }
        public string Result { get; set; }
        public string CompilerRes { get; set; }
    }

    public class CheckJudge : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Request.QueryString["jk"] == "aaa")
            {
                filterContext.HttpContext.Response.Redirect("/User/Login");
            }
        }
    }

    public class JudgeController : ApiController
    {
        private MyDbContext db = new MyDbContext();
        
        [System.Web.Http.NonAction]
        public void SetUpdate(Contest c)
        {
            if (c != null)
            {
                c.Update = true;
                db.Entry(c).State = EntityState.Modified;
            }
        }

        public SubmitApiModels Get()
        {
            Mapper.CreateMap<Submit,SubmitApiModels>();
            var tmp = db.Submits.FirstOrDefault(m => m.State == SubmitState.Waiting);
            if (tmp == null)
                return null;
            tmp.State = SubmitState.Running;
            SetUpdate(tmp.Belog);
            db.Entry(tmp).State = EntityState.Modified;
            db.SaveChanges();
            var ret = Mapper.Map<SubmitApiModels>(tmp);
            return ret;
        }

        public HttpResponseMessage Post(SubmitResultApiModels submit)
        {
            var tmp = db.Submits.Find(submit.ID);
            if (tmp == null)
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            tmp.Result = submit.Result;
            tmp.Score = submit.Score;
            tmp.State = submit.State;
            tmp.CompilerRes = submit.CompilerRes;
            SetUpdate(tmp.Belog);
            db.Entry(tmp).State = EntityState.Modified;
            db.SaveChanges();
            return new HttpResponseMessage(HttpStatusCode.OK); ;
        }

        [System.Web.Mvc.ActionName("downland")]
        public HttpResponseMessage GetProblemData(int id)
        {
            Problem problem = db.Problems.Find(id);
          
            var file = HelperFunc.GetZipPath(id);
            var stream = new FileStream(file, FileMode.Open);
            var content = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            content.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            return content;
        }


        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }


    }
}