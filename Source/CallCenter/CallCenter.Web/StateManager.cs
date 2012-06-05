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
            BroadcastActiveCalls();
        }
        public static void CompletedCall(Call call)
        {
            ActiveCalls.Remove(ActiveCalls.Find(p => p.Sid == call.Sid));
            InactiveCalls.Add(call);
            BroadcastActiveCalls();
        }

        private static void BroadcastActiveCalls()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext("DashboardHub");
            context.Clients.updateActiveCallCount(ActiveCalls);
            context.Clients.updateInactiveCallCount(InactiveCalls);
            context.Clients.updateCallGrid(GetWijmoCallGrid());
            BroadcastAreaCodes();
        }

        private static void BroadcastAreaCodes()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext("DashboardHub");

            var areaCodeCounts = new Dictionary<string, int>();

            List<Call> allCalls = new List<Call>();
            allCalls.AddRange(ActiveCalls);
            allCalls.AddRange(InactiveCalls);

            foreach (var areaCode in allCalls.OrderBy(p => p.From).Select(call => ExtractAreaCode(call.From)))
            {
                if (areaCodeCounts.ContainsKey(areaCode))
                    areaCodeCounts[areaCode] += 1;
                else
                    areaCodeCounts[areaCode] = 1;
            }

            List<WijPieChartSeriesItem> areaCodeList;

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
        public static void PreloadClient(string connectionId)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext("DashboardHub");
            context.Clients[connectionId].updateActiveCallCount(ActiveCalls);
            context.Clients[connectionId].updateInactiveCallCount(InactiveCalls);
            context.Clients[connectionId].updateCallGrid(GetWijmoCallGrid());
            BroadcastAreaCodes();
        }

        /* Helpers */
        private static string ExtractAreaCode(string phoneNumber)
        {
            return phoneNumber.Substring(2, 3);
        }
        private static List<Dictionary<string, string>> GetWijmoCallGrid()
        {
            var calls = ActiveCalls.Select(activeCall => new Dictionary<string, string>
                                                             {
                                                                 {"Number", CensorPhoneNumber(activeCall.From)},
                                                                 {"Status", "Active"},
                                                                 {
                                                                     "Duration",
                                                                     string.Format("{0} seconds",
                                                                                   GetCallDuration(activeCall))
                                                                     }
                                                             }).ToList();
            calls.AddRange(InactiveCalls.Select(activeCall => new Dictionary<string, string>
                                                                  {
                                                                      {"Number", CensorPhoneNumber(activeCall.From)},
                                                                      {"Status", "Completed"},
                                                                      {
                                                                          "Duration",
                                                                          string.Format("{0} seconds",
                                                                                        GetCallDuration(activeCall))
                                                                          }
                                                                  }).ToList());

            return calls;
        }
        private static int GetCallDuration(Call activeCall)
        {
            string accountSid = "ACa2de2b9a03db42ee981073b917cc8132";
            string authToken = "921a664399748302a019ee35c40e888c";

            TwilioRestClient client = new TwilioRestClient(accountSid, authToken);
            var call = client.GetCall(activeCall.Sid);
            return call.Duration.HasValue ? call.Duration.Value : 0;
        }
        private static string CensorPhoneNumber(string number)
        {
            return number.Substring(0, 8) + "****";
        }
    }

    public class WijPieChartSeriesItem
    {
        public string label { get; set; }
        public bool legendEntry { get; set; }
        public int data { get; set; }
    }
}