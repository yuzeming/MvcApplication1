using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Web.Mvc;
using WebMatrix.WebData;
using MvcApplication1.Models;
using System.Web.Security;

namespace MvcApplication1.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class InitializeSimpleMembershipAttribute : ActionFilterAttribute
    {
        private static SimpleMembershipInitializer _initializer;
        private static object _initializerLock = new object();
        private static bool _isInitialized;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // 确保每次启动应用程序时只初始化一次 ASP.NET Simple Membership
            LazyInitializer.EnsureInitialized(ref _initializer, ref _isInitialized, ref _initializerLock);
        }

        private class SimpleMembershipInitializer
        {
            public SimpleMembershipInitializer()
            {
                Database.SetInitializer<MyDbContext>(null);

                try
                {
                    using (var context = new MyDbContext())
                    {
                        if (!context.Database.Exists())
                        {
                            // 创建不包含 Entity Framework 迁移架构的 SimpleMembership 数据库
                            ((IObjectContextAdapter)context).ObjectContext.CreateDatabase();
                            foreach (string role in new string[] {"root", "user" })
                                if (!Roles.RoleExists(role))
                                    Roles.CreateRole(role);

                            if (!WebSecurity.UserExists("root"))
                            {
                                WebSecurity.CreateUserAndAccount("root", "toor123");
                                Roles.AddUserToRole("root", "root");
                            }

                            foreach (string role in new string[] { "chenli", "zhangli", "guoyuchen", "pantianxiang", })
                                if (!WebSecurity.UserExists(role))
                                {
                                    WebSecurity.CreateUserAndAccount(role, "123456");
                                    Roles.AddUserToRole(role, "root");
                                }
                        }
                    }

                    WebSecurity.InitializeDatabaseConnection("DefaultConnection", "UserProfiles", "UserId", "UserName",true);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("无法初始化 ASP.NET Simple Membership 数据库。有关详细信息，请参阅 http://go.microsoft.com/fwlink/?LinkId=256588", ex);
                }
            }
        }
    }
}
