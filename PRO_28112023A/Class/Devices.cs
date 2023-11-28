using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRO_28112023A.Class
{
    public class Devices
    {
        public string PLC_IP { get; set; }
        public int PLC_Port { get; set; }
        public string PLC_HeartBeat { get; set; }
        public string StartRead { get; set; }
        public string EndRead { get; set; }

        public string Barcode { get; set; }
        public string Judgment { get; set; }
        public List<DataPoints> DataPoints { get; set; }
    }

    public class DataPoints
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Standard { get; set; }
        public string Upper { get; set; }
        public string Lower { get; set; }
        public string StdValue { get; set; }
        public string Result { get; set; }
        public string Judgment { get; set; }
    }
}
