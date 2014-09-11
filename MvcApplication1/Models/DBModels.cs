using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Security;

namespace MvcApplication1.Models
{
    public class MyDbContext : DbContext
    {
        public MyDbContext()
            : base("DefaultConnection")
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Problem> Problems { set; get; }
        public DbSet<Submit> Submits { get; set; }
    }

    public class UserIntializer : DropCreateDatabaseIfModelChanges<MyDbContext>
    {
        protected override void Seed(MyDbContext context)
        {
            base.Seed(context);
        }
    }

    public class UserProfile
    {
        [Key]
        public int UserId { get; set; }
        public string UserName { get; set; }
    }

    public class Problem
    {
        [Key]
        public int ID { set; get; }

        [Required]
        public string Title { set; get; }

        [Required]
        public bool Public { set; get; }

        [Required]
        public bool PublicData { set; get; }

        [Required]
        public string CheckSum { set; get; }

        [Required]
        public string Description { set; get; }
    //    public string Solution { set; get; }
    }


    public enum SubmitState
    {
        Waiting,
        Running,

        Accepted,
        TimeLimitExceeded,
        MemoryLimitExceeded,
        WrongAnswer,
        RuntimeError,
        OutputLimitExceeded,
        CompileError,
        SystemError,
        ValidatorError,
    }

    public class Submit
    {
        public Submit()
        {
            Time = DateTime.Now;
            State = SubmitState.Waiting;
        }
        [Key]
        public int ID { get; set; }
        [Required]
        public string Lang { get; set; }
        public virtual Problem Prob { get; set; }
        public virtual UserProfile User { get; set; }

        [Required]
        public DateTime Time { get; set; }

        [Required]
        public string Source { get; set; }
        public SubmitState State { get; set; }
        public int Score { get; set; }
        public string Result { get; set; } //Json
        public string CompilerRes { get; set; }
    }

    public class JsonConfig
    {
        public string Title { get; set; }
        public string[][] Data {get;set;}
    }
}
