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
        }

        private static void BroadcastUpdatedCalls()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext("DashboardHub");
            context.Clients.updateActiveCalls(ActiveCalls);
            context.Clients.updateInactiveCalls(InactiveCalls);
            UpdateAreaCodes();
        }

        public static void BroadcastToClient(dynamic caller)
        {
            caller.updateActiveCalls(ActiveCalls);
            caller.updateInactiveCalls(InactiveCalls);
            UpdateAreaCodes();
        }

        private static void UpdateAreaCodes()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext("DashboardHub");

            var areaCodeCounts = new Dictionary<string, int>();

            foreach (var areaCode in ActiveCalls.OrderBy(p => p.From).Select(call => ExtractAreaCode(call.From)))
            {
                if (areaCodeCounts.ContainsKey(areaCode))
                    areaCodeCounts[areaCode] += 1;
                else
                    areaCodeCounts[areaCode] = 1;
            }

            foreach (var areaCode in InactiveCalls.OrderBy(p => p.From).Select(call => ExtractAreaCode(call.From)))
            {
                if (areaCodeCounts.ContainsKey(areaCode))
                    areaCodeCounts[areaCode] += 1;
                else
                    areaCodeCounts[areaCode] = 1;
            }

            List<WijPieChartSeriesItem> areaCodeList = new List<WijPieChartSeriesItem>();

            if (areaCodeCounts.Any())
            {
                areaCodeList = areaCodeCounts.Select(keyValuePair => new WijPieChartSeriesItem()
                                                                         {
                                                                             data = keyValuePair.Value,
                                                                             label = keyValuePair.Key,
                                                                             legendEntry = true
                                                                         }).ToList();
            }
            else
            {
                areaCodeList = new List<WijPieChartSeriesItem>()
                                   {new WijPieChartSeriesItem() {data = 1, label = "None", legendEntry = false}};
            }

            context.Clients.updateAreaCodeChart(areaCodeList);
        }

        private static string ExtractAreaCode(string phoneNumber)
        {
            return phoneNumber.Substring(2, 3);
        }
    }

    public class WijPieChartSeriesItem
    {
        public string label { get; set; }
        public bool legendEntry { get; set; }
        public int data { get; set; }
    }
}