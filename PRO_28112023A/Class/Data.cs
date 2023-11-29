using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRO_28112023A.Class
{
    public class Data
    {
        public string Maq1StartRead { get; set; }
        public string Maq1EndRead { get; set; }

        public string Maq2StartRead { get; set; }
        public string Maq2EndRead { get; set; }

        public string HeartBeat { get; set; }

        public string Serial_Number { get; set; }
        public string Date { get; set; }
        public string Part_Number { get; set; }
        public string Serial_Number_Actuador { get; set; }
        public string Screw1_Torque { get; set; }
        public string No_Turns_1 { get; set; }
        public string Screw2_Torque { get; set; }
        public string No_Turns_2 { get; set; }

        public string Sensor1_OK { get; set; }
        public string Sensor2_OK { get; set; }
        public string Sensor3_OK { get; set; }
        public string Sensor4_OK { get; set; }

        public string Soft_Actuador { get; set; }
        public string Movement_Vanes { get; set; }

        public string Current_Amp { get; set; }
        public string Voltage_Vcc { get; set; }
        public string Customer_Position { get; set; }

        public string Actuador_Speed { get; set; }
        public string Customer_QR_OK { get; set; }
        public string Corret_Soft_Actuador_OK { get; set; }
    }
}
