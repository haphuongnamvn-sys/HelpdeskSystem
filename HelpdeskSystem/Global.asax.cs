using System;
using System.Data.Entity;
using System.Security.Claims;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using HelpdeskSystem.Models;
using HelpdeskSystem.Migrations;
using Microsoft.AspNet.Identity;

namespace HelpdeskSystem
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Configure AntiForgery to match ASP.NET Identity claim types consistently
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;

            // Initialize database with migrations + seed data
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ApplicationDbContext, Configuration>());
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            var antiForgeryException = exception as HttpAntiForgeryException 
                ?? exception?.InnerException as HttpAntiForgeryException;

            if (antiForgeryException != null)
            {
                Server.ClearError();

                // Safely sign out the user if anti-forgery claims mismatch occurs (e.g. switching accounts or stale session)
                try
                {
                    var auth = Request.GetOwinContext()?.Authentication;
                    auth?.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                }
                catch { }

                // Clear authentication and verification cookies
                if (Request.Cookies[".AspNet.ApplicationCookie"] != null)
                {
                    Response.Cookies.Add(new HttpCookie(".AspNet.ApplicationCookie") { Expires = DateTime.Now.AddDays(-1) });
                }
                if (Request.Cookies["__RequestVerificationToken"] != null)
                {
                    Response.Cookies.Add(new HttpCookie("__RequestVerificationToken") { Expires = DateTime.Now.AddDays(-1) });
                }

                Response.Redirect("~/Account/Login?mismatch=1");
            }
        }
    }
}
