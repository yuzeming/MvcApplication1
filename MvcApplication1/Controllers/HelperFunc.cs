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

}