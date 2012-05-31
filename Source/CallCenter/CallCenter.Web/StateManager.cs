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
        private List<Call> ActiveCalls { get; set; }
        private List<Call> InactiveCalls { get; set; }

        private DashboardHub _context = (DashboardHub) GlobalHost.ConnectionManager.GetHubContext<DashboardHub>();

        public StateManager()
        {
            ActiveCalls = new List<Call>();
            InactiveCalls = new List<Call>();
        }
        
        public void AddNewCall(Call call)
        {
            ActiveCalls.Add(call);
            BroadcastUpdatedCalls();
        }

        public void CompletedCall(Call call)
        {
            ActiveCalls.Remove(call);
            InactiveCalls.Add(call);
        }

        private void BroadcastUpdatedCalls()
        {
            _context.Clients.updateActiveCalls(ActiveCalls);
            _context.Clients.updateInactiveCalls(InactiveCalls);
        }
    }
}