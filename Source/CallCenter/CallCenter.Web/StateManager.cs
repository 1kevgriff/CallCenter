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

        static StateManager()
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
            ActiveCalls.Remove(ActiveCalls.Find(p => p.Sid == call.Sid));
            InactiveCalls.Add(call);
            BroadcastUpdatedCalls();
            UpdateCallAverageDuration();
        }

        private static void UpdateCallAverageDuration()
        {
            int totalDuration = 0;
            InactiveCalls.ForEach(p =>
                                      {
                                          if (p.Duration.HasValue)
                                              totalDuration += p.Duration.Value;
                                      });
            var context = GlobalHost.ConnectionManager.GetHubContext("DashboardHub");
            context.Clients.updateCallAverageDuration(totalDuration);

        }

        private static void BroadcastUpdatedCalls()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext("DashboardHub");
            context.Clients.updateActiveCalls(ActiveCalls);
            context.Clients.updateInactiveCalls(InactiveCalls);
        }
    }
}