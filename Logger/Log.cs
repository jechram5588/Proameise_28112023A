using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public class Log
    {
        private readonly object Write_Lock = new object();
        string App = "";
        bool bDebug = false;
        string LogFolder = "";
        public Log(string app)
        {
            App = app;
            var appSettings = ConfigurationManager.AppSettings;
            bDebug = Convert.ToBoolean(appSettings["DebugMode"]);
            //try
            //{
            //    if (!EventLog.SourceExists(App))
            //    {
            //        EventLog.CreateEventSource(App, App);
            //    }
            //}
            //catch { }
            LogFolder = AppDomain.CurrentDomain.BaseDirectory + "Logs";
            if (!System.IO.Directory.Exists(LogFolder))
                System.IO.Directory.CreateDirectory(LogFolder);
        }
        public void PrimaryLog(string origen, string evento, EventLogEntryType tipo, bool Forzarlog)
        {
            Console.WriteLine(string.Format("{0}:\t{1}->{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), origen, evento));
            //try
            //{
            //if (Forzarlog || tipo == EventLogEntryType.Error || tipo == EventLogEntryType.Warning || DebugMode)
            //{
            //    eventLog = new EventLog();
            //    eventLog.Source = ServiceName;
            //    //eventLog.Log = ServiceName;
            //    eventLog.WriteEntry(string.Format("{0}: {1}-> {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), origen, evento), tipo);
            //    eventLog.Close();
            //}
            //}
            //catch
            //{
            lock(this.Write_Lock)
            {
                StreamWriter sw = null;
                try
                {
                    if (tipo == EventLogEntryType.Error || Forzarlog)
                    {
                        sw = new StreamWriter(LogFolder + "\\" + this.App + "_" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt", true);
                        sw.WriteLine(string.Format("{0}: {1}-> {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), origen, evento));
                        sw.Flush();
                        sw.Close();
                    }
                }
           
            catch { }
            }

        }
    }

}
