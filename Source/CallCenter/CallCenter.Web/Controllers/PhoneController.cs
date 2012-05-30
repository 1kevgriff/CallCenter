using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Twilio;
using Twilio.TwiML;

namespace CallCenter.Web.Controllers
{
    public class PhoneController : Controller
    {
        //
        // GET: /Phone/

        public ActionResult IncomingCall(string CallSid)
        {
            string accountSid = "";
            string authToken = "";

            TwilioRestClient client = new TwilioRestClient(accountSid, authToken);
            var call = client.GetCall(CallSid);

            TwilioResponse response = new TwilioResponse();
            response.Say("Welcome to the Bank of Griff.");
            response.Say("Goodbye");
            response.Hangup();

            Stream result = new MemoryStream(Encoding.Default.GetBytes(response.ToString()));

            return new FileStreamResult(result, "text/plain");
        }

    }
}
