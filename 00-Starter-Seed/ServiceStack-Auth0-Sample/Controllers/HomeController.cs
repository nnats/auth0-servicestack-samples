using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace ServiceStack_Auth0_Sample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewData["client_id"] = WebConfigurationManager.AppSettings["oauth.auth0.AppId"];
            ViewData["domain"] = WebConfigurationManager.AppSettings["oauth.auth0.OAuthServerUrl"].Substring(8);
            ViewData["callback"] = WebConfigurationManager.AppSettings["callback"];
            return View("default");
        }
    }
}
