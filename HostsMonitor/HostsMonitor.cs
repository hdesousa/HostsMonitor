using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Collections;

namespace HostsMonitor
{
    public partial class HostsMonitor : ServiceBase
    {
        private int eventId = 1;
        private readonly IDictionary<string, bool> servers = new Dictionary<string, bool>();

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        public HostsMonitor()
        {
            InitializeComponent();
            if (!System.Diagnostics.EventLog.SourceExists("HostsMonitor"))
            {
                System.Diagnostics.EventLog.CreateEventSource("HostsMonitor", "HostsMonitor");
            }

            eventLog1 = new EventLog("HostsMonitor");
            eventLog1.Source = "HostsMonitor";
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            servers["google.com"] = false;
            servers["sapo.pt"] = false;
            eventLog1.WriteEntry("Hosts Monitor Started");

            // Set up a timer that triggers every minute.
            Timer timer = new Timer();
            timer.Interval = 5000; // in miliseconds
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            // Insert monitoring activities here.
            foreach (var server in servers)
            {
                var pingResult = PingHost(server.Key);
                if (pingResult != server.Value)
                {
                    servers[server.Key] = pingResult;
                    eventLog1.WriteEntry("Server " + server.Key + " is " + (pingResult ? "up" : "down"), pingResult ? EventLogEntryType.Warning : EventLogEntryType.Error, eventId++);
                }
            }
            
        }

        protected override void OnStop()
        {
            // Update the service state to Stop Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog1.WriteEntry("Hosts Monitor Stopped");

            // Update the service state to Stopped.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

        private void EventLog1_EntryWritten(object sender, EntryWrittenEventArgs e)
        {

        }
    }
}
