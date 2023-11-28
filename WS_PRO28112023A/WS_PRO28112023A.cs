using PRO_28112023A;
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

namespace WS_PRO28112023A
{
    public partial class WS_PRO28112023A : ServiceBase
    {
        private System.Timers.Timer timer1 = null;
        public PRO_28112023A.PRO_28112023A Server;

        public WS_PRO28112023A()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            timer1 = new System.Timers.Timer();
            this.timer1.Interval = 1000;
            this.timer1.Elapsed += new ElapsedEventHandler(this.timer1_Tick);
            timer1.Enabled = true;
            Server = new PRO_28112023A.PRO_28112023A();
            Server.Start();
        }
        private void timer1_Tick(object sender, ElapsedEventArgs e)
        {
            if (!Server.Status) this.Stop();
        }

        protected override void OnStop()
        {
            Server.Stop();
        }
    }
}
