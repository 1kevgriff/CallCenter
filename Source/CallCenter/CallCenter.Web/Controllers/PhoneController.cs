using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
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

        public ActionResult IncomingCall(string CallSid, string FromCity, string FromState, string FromZip, string FromCountry)
        {
            LocationalCall call = (LocationalCall)GetCall(CallSid);
            StateManager.AddNewCall(call);
            StateManager.AddToLog(CallSid, "Incoming call...");
            call.City = FromCity;
            call.Country = FromCountry;
            call.ZipCode = FromZip;
            call.State = FromState;

            TwilioResponse response = new TwilioResponse();
            response.BeginGather(new
            {
                action = Url.Action("ServiceRequest"),
                timeout = 120,
                method = "POST",
                numDigits = 1
            });
            response.Say("Welcome to the Bank of Griff.");
            response.Pause();
            response.Say("Press 1 to manage your account.");
            response.Say("Press 2 to take out a loan.");
            response.Say("Press 3 to talk to a representative.");
            response.Pause();
            response.EndGather();

            return SendTwilioResult(response);
        }
        public ActionResult CallComplete(string CallSid)
        {
            LocationalCall call = (LocationalCall)GetCall(CallSid);
            StateManager.CompletedCall(call);
            StateManager.AddToLog(CallSid, "Call is completed.");

            TwilioResponse response = new TwilioResponse();
            response.Say("Goodbye baby cakes");
            response.Hangup();

            return SendTwilioResult(response);
        }
        public ActionResult ServiceRequest(string CallSid, string Digits)
        {
            var call = GetCall(CallSid);
            TwilioResponse response = new TwilioResponse();

            switch (Digits)
            {
                case "0":
                    {
                        StateManager.AddToLog(CallSid, string.Format("User selected option {0} from service selection.", "Return to Menu"));
                        response.Say("Returning to the main menu.");
                        response.Redirect(Url.Action("IncomingCall"));
                    }
                    break;
                case "1":
                    {
                        StateManager.AddToLog(CallSid, string.Format("User selected option {0} from service selection.", "Manage Account"));
                        response.BeginGather(
    new { action = Url.Action("ManageAccount"), timeout = 120, method = "POST", numDigits = 8 });
                        response.Say("Please enter your 8 digit account number");
                        response.EndGather();
                    }
                    break;
                case "2":
                    {
                        StateManager.AddToLog(CallSid, string.Format("User selected option {0} from service selection.", "Take a Loan"));
                        response.Say(
                            "All of our loan officers are currently giving money away to people less deserving than you.");
                    }
                    break;
                case "3":
                    {
                        StateManager.AddToLog(CallSid, string.Format("User selected option {0} from service selection.", "Talk to a Representative"));
                    }
                    break;
                default:
                    {
                        response.Say("Oy vey.");
                        response.Redirect(Url.Action("IncomingCall"));
                    } break;
            }

            return SendTwilioResult(response);
        }
        public ActionResult ManageAccount(string CallSid, string Digits)
        {
            var call = GetCall(CallSid);
            TwilioResponse response = new TwilioResponse();
            response.Say("Sorry.  Account management is currently down for repairs.  Please try again in 6 to 8 weeks.");
            response.Hangup();

            return SendTwilioResult(response);
        }

        private static ActionResult SendTwilioResult(TwilioResponse response)
        {
            Stream result = new MemoryStream(Encoding.Default.GetBytes(response.ToString()));

            return new FileStreamResult(result, "application/xml");
        }
        private static LocationalCall GetCall(string CallSid)
        {
            string accountSid = "ACa2de2b9a03db42ee981073b917cc8132";
            string authToken = "921a664399748302a019ee35c40e888c";

            TwilioRestClient client = new TwilioRestClient(accountSid, authToken);
            var call = client.GetCall(CallSid);
            return new LocationalCall(call);
        }
    }
}
