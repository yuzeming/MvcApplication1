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
        public DbSet<Contest> Contests { get; set; }
        public DbSet<Tag> Tags { get; set; }
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

        public virtual Tag Tag { get; set; }
    }


    public enum SubmitState
    {
        Waiting = 999,
        Running = 1000,

        Accepted = 0,
        TimeLimitExceeded = 2,
        MemoryLimitExceeded = 3,
        WrongAnswer = 4,
        RuntimeError = 5,
        OutputLimitExceeded = 6,
        CompileError = 7,
        SystemError = 100,
        ValidatorError = 101,
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
        public virtual Contest Belog { get; set; }
    }

    public enum ContestState 
    {
        [Display(Name="未开始")]
        Before,
        [Display(Name="正在进行")]
        Running,
        [Display(Name="已结束")]
        Past,
    }

    public class Contest
    {
        [Key]
        public int ID { get; set; }

        public string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public bool Public { get; set; }

        public virtual ICollection<UserProfile> UserList { get; set; }
        public virtual ICollection<Problem> ProbList { get; set; }

        public string Result { get; set; } // JSON
        public bool Update { get; set; }  // 惰性更新结果数据

        public virtual Tag Tag { get; set; }

        public ContestState State
        {
            get
            {
                if (DateTime.Now < Start)
                    return ContestState.Before;
                if (DateTime.Now > End)
                    return ContestState.Past;
                return ContestState.Running;
            }
        }
    }

    public class Tag
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }
    }

    public class JsonConfig
    {
        public string Title { get; set; }
        public string[][] Data {get;set;}
    }
}
