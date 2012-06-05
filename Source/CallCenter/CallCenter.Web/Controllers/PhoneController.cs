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

		public ActionResult IncomingCall(string CallSid)
		{
			var call = GetClient(CallSid);
			StateManager.AddNewCall(call);

			TwilioResponse response = new TwilioResponse();
			response.Say("Welcome to the Bank of Griff.");
			response.Pause();
			response.Say("Press 1 to manage your account.");
			response.Say("Press 2 to take out a loan.");
			response.Say("Press 3 to talk to a representative.");
			response.Pause();
			response.Gather(new { action = Url.Action("ServiceRequest"), 
				timeout = 120, method = "POST", numDigits = 1 });

			return SendTwilioResult(response);
		}

		private static ActionResult SendTwilioResult(TwilioResponse response)
		{
			Stream result = new MemoryStream(Encoding.Default.GetBytes(response.ToString()));

			return new FileStreamResult(result, "application/xml");
		}

		public ActionResult CallComplete(string CallSid)
		{
			var call = GetClient(CallSid);
			StateManager.CompletedCall(call);

			TwilioResponse response = new TwilioResponse();
			response.Say("Goodbye baby cakes");
			response.Hangup();

			return SendTwilioResult(response);
		}

		private static Call GetClient(string CallSid)
		{
            string accountSid = "ACa2de2b9a03db42ee981073b917cc8132";
            string authToken = "921a664399748302a019ee35c40e888c";

			TwilioRestClient client = new TwilioRestClient(accountSid, authToken);
			var call = client.GetCall(CallSid);
			return call;
		}

		public ActionResult ServiceRequest(string CallSid, string Digits)
		{
			var call = GetClient(CallSid);
			TwilioResponse response = new TwilioResponse();
			response.Say(string.Format("You pressed {0}", Digits));
			response.Pause();
			response.Say("Way to go.");
			response.Hangup();

			Stream result = new MemoryStream(Encoding.Default.GetBytes(response.ToString()));

			return new FileStreamResult(result, "text/plain");
		}
	}
}
