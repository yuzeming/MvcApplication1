using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Security;
using System.Web;
using MvcApplication1.Models;

namespace MvcApplication1.FormModels
{
    public class LocalPasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "当前密码")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "{0} 必须至少包含 {2} 个字符。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "新密码")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "确认新密码")]
        [Compare("NewPassword", ErrorMessage = "新密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginModel
    {
        [Required]
        [Display(Name = "用户名")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; }

        [Display(Name = "记住我?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterModel
    {
        [Required]
        [Display(Name = "用户名")]
        public string UserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "{0} 必须至少包含 {2} 个字符。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "确认密码")]
        [Compare("Password", ErrorMessage = "密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; }
    }

    public class UploadProblemModel
    {
        [Display(Name = "题目标题")]
        public string Title { get; set; }

        [Display(Name = "数据文件")]
        public HttpPostedFileBase File { get; set; }

        [Display(Name = "公开")]
        public bool Public { get; set; }

        [Display(Name = "公开测试数据")]
        public bool PublicData { get; set; }
    }

    public class SubmitModel
    {
        [Display(Name = "程序语言")]
        [Required]
        public string Lang { get; set; }

        [Display(Name = "源代码")]
        [DataType(DataType.MultilineText)]
        [Required]
        public string Src { get; set; }
    }

    public class SubmitSearchModel
    {
        public int? S { get; set; }
        public int? C { get; set; }
        public int? P { get; set; }
        public string U { get; set; }
        public SubmitState? State { get; set; }
    }

    public class ContestFormModel
    {
        [Required]
        [Display(Name = "比赛名称")]
        public string Title { get; set; }
        [Required]
        [Display(Name = "开始时间")]
        public DateTime Start { get; set; }
        [Required]
        [Display(Name = "结束时间")]
        public DateTime End { get; set; }

        [Display(Name = "公开")]
        public bool Public { get; set; }

        [Display(Name = "题目列表")]
        public string ProbStr { get; set; }

        [Display(Name = "用户列表")]
        public string UserStr { get; set; }
    }

    public class JsonContestReslut
    {
        public int Src { get; set; }
        public string UserName { get; set; }
        public List<String> Details { get; set; }
    }
}