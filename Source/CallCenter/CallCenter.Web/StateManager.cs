using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Web;
using CallCenter.Web.Hubs;
using SignalR;
using Twilio;

namespace CallCenter.Web
{
    public class StateManager
    {
        private static List<LocationalCall> ActiveCalls { get; set; }
        private static List<LocationalCall> InactiveCalls { get; set; }
        private static List<LocationalCall> AllCalls
        {
            get
            {
                List<LocationalCall> allCalls = new List<LocationalCall>();
                allCalls.AddRange(ActiveCalls);
                allCalls.AddRange(InactiveCalls);
                return allCalls;
            }
        }

        private static List<LogItem> Log { get; set; } 
        private static Timer updateUITimer;

        static StateManager()
        {
            ActiveCalls = new List<LocationalCall>();
            InactiveCalls = new List<LocationalCall>();
            updateUITimer = new Timer();
            updateUITimer.Elapsed += (sender, args) => BroadcastActiveCalls();
            updateUITimer.Interval = 1000; // 1 second
            updateUITimer.Start();
        }

        public static void AddNewCall(LocationalCall call)
        {
            ActiveCalls.Add(call);
            BroadcastActiveCalls();
        }
        public static void CompletedCall(LocationalCall call)
        {
            var locationalCall = ActiveCalls.Find(p => p.Sid == call.Sid);
            ActiveCalls.Remove(locationalCall);
            InactiveCalls.Add(locationalCall);
            BroadcastActiveCalls();
        }
        public static void PreloadClient(string connectionId)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext("DashboardHub");
            context.Clients[connectionId].updateActiveCallCount(ActiveCalls);
            context.Clients[connectionId].updateInactiveCallCount(InactiveCalls);
            context.Clients[connectionId].updateCallGrid(GetWijmoCallGrid());
            context.Clients[connectionId].updateLastUpdated(DateTime.Now.ToString());
            BroadcastAreaCodes();
        }
        public static void AddToLog(string sid, string logText)
        {
            Log.Add(new LogItem()
                        {
                            PhoneNumber = CensorPhoneNumber(AllCalls.Find(p => p.Sid == sid).From),
                            LogText = logText,
                            Date = DateTime.Now
                        });
        }
        
        private static void BroadcastActiveCalls()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext("DashboardHub");
            context.Clients.updateActiveCallCount(ActiveCalls);
            context.Clients.updateInactiveCallCount(InactiveCalls);
            context.Clients.updateCallGrid(GetWijmoCallGrid());
            context.Clients.updateLastUpdated(DateTime.Now.ToString());
            context.Clients.updateLogGrid(GetWijmoLogGrid());
            BroadcastAreaCodes();
        }



        private static void BroadcastAreaCodes()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext("DashboardHub");

            var areaCodeCounts = new Dictionary<string, int>();

            foreach (var areaCode in AllCalls.OrderByDescending(p => p.DateCreated).Select(call => ExtractAreaCode(call.From)))
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
                areaCodeList = new List<WijPieChartSeriesItem>() { new WijPieChartSeriesItem() { data = 1, label = "None", legendEntry = false } };
            }

            context.Clients.updateAreaCodeChart(areaCodeList);
        }

        /* Helpers */
        private static string ExtractAreaCode(string phoneNumber)
        {
            return phoneNumber.Substring(2, 3);
        }
        private static List<Dictionary<string, string>> GetWijmoCallGrid()
        {
            var calls = AllCalls.OrderByDescending(p => p.DateCreated).Select(activeCall => new Dictionary<string, string>
                                                             {
                                                                 {"Number", CensorPhoneNumber(activeCall.From)},
                                                                 {"Status", GetCallStatus(activeCall)},
                                                                 {
                                                                     "Duration",
                                                                     string.Format("{0} seconds",
                                                                                   GetCallDuration(activeCall))
                                                                     },
                                                                     {"Date", activeCall.DateCreated.ToString()},
                                                                     {"City", activeCall.City},
                                                                 {"State", activeCall.State},
                                                                 {"Zip Code", activeCall.ZipCode},
                                                                 {"Country", activeCall.Country}
                                                             }).ToList();

            return calls;
        }
        private static List<Dictionary<string, string>> GetWijmoLogGrid()
        {
            var log = Log.OrderByDescending(p => p.Date).Select(l => new Dictionary<string, string>
                                                                         {
                                                                             {"Date", l.Date.ToString()},
                                                                             {"Number", l.PhoneNumber},
                                                                             {"Text", l.LogText}
                                                                         }).ToList();
            return log;
        }
        private static string GetCallStatus(Call activeCall)
        {
            string accountSid = "ACa2de2b9a03db42ee981073b917cc8132";
            string authToken = "921a664399748302a019ee35c40e888c";

            TwilioRestClient client = new TwilioRestClient(accountSid, authToken);
            var call = client.GetCall(activeCall.Sid);
            return call.Status;
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

    internal class LogItem
    {
        public string PhoneNumber { get; set; }
        public DateTime Date { get; set; }
        public string LogText { get; set; }
    }

    public class WijPieChartSeriesItem
    {
        public string label { get; set; }
        public bool legendEntry { get; set; }
        public int data { get; set; }
    }
}