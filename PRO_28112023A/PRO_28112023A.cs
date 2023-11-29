using DocumentFormat.OpenXml.Drawing;
using Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PRO_28112023A.Class;
using PRO_28112023A.Dlls;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PRO_28112023A
{
    public class PRO_28112023A
    {
        public bool Status { get { return Running; } set { Running = value; } }
        public bool Running = false;

        /*  Parametros de la App   */
        public string PLC_IP { get; set; }
        public int PLC_Port { get; set; }
        public string PLC_HeartBeat { get; set; }
        public byte PLC_Slot { get; set; }

        private Data Tags;
        List<Data> lData;

        public bool DebugMode = true;
        public PLC_AllenBrandly PLC;
        public Log Logger;

        private Thread thMaq1 = null;
        private Thread thMaq2 = null;
        private Thread thAlive = null;

        public enum Steps
        {
            Disconnect, Alive, StartRead, ReadValues, EndRead
        }

        public PRO_28112023A()
        {
            Logger = new Log("PRO_28112023A");
        }
        public void Start()
        {
            try
            {
                Logger.PrimaryLog("Start", "Inicio", EventLogEntryType.Information, true);
                Running = true;
                LoadConfig();
            }
            catch (Exception ex)
            {
                Logger.PrimaryLog("Start", "Error al consultar configuracion inicial" + ex.Message.ToString(), EventLogEntryType.Error, true);
                Stop();
            }

        }
        public void Stop()
        {
            Running = false;
            if (thMaq1 != null) thMaq1.Abort();
            if (thMaq2 != null) thMaq2.Abort();
            if (thAlive != null) thAlive.Abort();
        }
        public void LoadConfig()
        {
            int error = 0;
            try
            {
                lData = new List<Data>();

                PLC_IP = "192.168.1.1";
                PLC_Port = 44818;
                PLC_Slot = 0;

                PLC = new PLC_AllenBrandly("PLC " + 4, PLC_IP, PLC_Port, PLC_Slot);
                Tags = new Data();


                Tags.Maq1StartRead = "Maq1StartRead";
                Tags.Maq1EndRead = "Maq1EndRead";

                Tags.Maq2StartRead = "Maq2StartRead";
                Tags.Maq2EndRead = "Maq2EndRead";

                Tags.HeartBeat = "HeartBeat";

                Tags.Serial_Number = "Serial_Number";
                Tags.Date = "Date";
                Tags.Part_Number = "Part_Number";
                Tags.Serial_Number_Actuador = "Serial_Number_Actuador";
                Tags.Screw1_Torque = "Screw1_Torque";
                Tags.No_Turns_1 = "No_Turns_1";
                Tags.Screw2_Torque = "Screw2_Torque";
                Tags.No_Turns_2 = "No_Turns_2";

                Tags.Sensor1_OK = "Sensor1_OK";
                Tags.Sensor2_OK = "Sensor2_OK";
                Tags.Sensor3_OK = "Sensor3_OK";
                Tags.Sensor4_OK = "Sensor4_OK";

                Tags.Soft_Actuador = "Soft_Actuador";
                Tags.Movement_Vanes = "Movement_Vanes";

                Tags.Current_Amp = "Current_Amp";
                Tags.Voltage_Vcc = "Voltage_Vcc";
                Tags.Customer_Position = "Customer_Position";

                Tags.Actuador_Speed = "Actuador_Speed";
                Tags.Customer_QR_OK = "Customer_QR_OK";
                Tags.Corret_Soft_Actuador_OK = "Corret_Soft_Actuador_OK";

                Logger.PrimaryLog("LoadConfig", "Carga de parametros correcta", EventLogEntryType.Information, true);
            }
            catch (Exception ex)
            {
                Logger.PrimaryLog("LoadConfig", ex.Message, EventLogEntryType.Error, true);
                error++;
            }
            if (error == 0)
            {
                thMaq1 = new Thread(Maquina1);
                thMaq1.Start();
                thMaq2 = new Thread(Maquina1);
                thMaq2.Start();
            }
        }

        public void Maquina1(){
            Logger.PrimaryLog("Maquina1", "Iniciado", EventLogEntryType.Information, true);
            Steps paso = Steps.StartRead;

            Data Valores = null;
            while (Running)
            {
                switch (paso)
                {
                    case Steps.StartRead:
                        if (PLC.ReadPLC(PLC_AllenBrandly.TipoDato.Boolean, Tags.Maq1StartRead, 0) == "1")
                        {
                            Logger.PrimaryLog("Maquina1:\tStartRead", "Trigger Detectado", EventLogEntryType.Information, false);
                            Valores = new Data();
                            paso = Steps.ReadValues;
                        }
                        else
                            Thread.Sleep(1000);
                        break;
                    case Steps.ReadValues:
                        break;
                    case Steps.EndRead:
                        lData.Add(Valores);
                        PLC.WritePLC(PLC_AllenBrandly.TipoDato.Boolean, Tags.Maq1EndRead, "1");
                        paso = Steps.StartRead;
                        Logger.PrimaryLog("Maquina1:\tEndRead", "Trigger Detectado", EventLogEntryType.Information, false);
                        break;
                    default:
                        break;
                }
            }
            Logger.PrimaryLog("Maquina1", "Terminado", EventLogEntryType.Information, true);
        }
        public DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                dataTable.Columns.Add(prop.Name);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            return dataTable;
        }
        public void ExportaExcel()
        {
            try
            {
                string file = AppDomain.CurrentDomain.BaseDirectory.ToString() + "\\Templates\\template.xlsx";
                if (File.Exists(file))
                {
                    SLDocument excel = new SLDocument(file);
                    SLWorksheetStatistics stats1 = excel.GetWorksheetStatistics();
                    DataTable dt = ToDataTable(lData);
                    excel.ImportDataTable(stats1.EndRowIndex + 1, 1, dt, false);
                    //System.Diagnostics.Process.Start(file);
                    excel.Save();
                }
            }
            catch (Exception ex)
            {
                Logger.PrimaryLog("ExportaExcel", ex.Message, EventLogEntryType.Error, true);
            }
        }
    }
}
