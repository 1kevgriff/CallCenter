using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CallCenter.Web.Hubs;
using SignalR;
using Twilio;

namespace CallCenter.Web
{
    public class StateManager
    {
        private static List<Call> ActiveCalls { get; set; }
        private static List<Call> InactiveCalls { get; set; }

        public StateManager()
        {
            ActiveCalls = new List<Call>();
            InactiveCalls = new List<Call>();
        }
        
        public static void AddNewCall(Call call)
        {
            ActiveCalls.Add(call);
            BroadcastUpdatedCalls();
        }

        public static void CompletedCall(Call call)
        {
            ActiveCalls.Remove(call);
            InactiveCalls.Add(call);
            BroadcastUpdatedCalls();
        }

        private static void BroadcastUpdatedCalls()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext("DashboardHub");
            context.Clients.updateActiveCalls(ActiveCalls);
            context.Clients.updateInactiveCalls(InactiveCalls);
        }
    }
}