using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CallCenter.Web.Controllers
{
    public class PhoneController : Controller
    {
        //
        // GET: /Phone/

        public ActionResult IncomingCall()  
        {
            return View();
        }

    }
}
