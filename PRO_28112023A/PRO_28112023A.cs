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

        private Data Tags;
        List<Data> lData;

        public bool DebugMode = true;
        public PLC_Mitsubishi PLC;
        public Log Logger;
        Thread ClassThread1 = null;
        Thread ClassThread2 = null;
        Thread ClassThread3 = null;

        public enum Steps
        {
            Conexion, Desconexion, Alive,
            StartRead, EndRead
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
            if (ClassThread1 != null)
            {
                ClassThread1.Abort();
            }
        }
        public void LoadConfig()
        {
            int error = 0;
            try
            {
                lData = new List<Data>();

                PLC_IP = "192.168.1.1";
                PLC_Port = 48819;
                PLC_HeartBeat = "HearBeart";
                PLC = new PLC_Mitsubishi("PLC " + PLC_Port, PLC_IP, PLC_Port);

                Tags = new Data();

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
                ClassThread1 = new Thread(Pruebas1);
                ClassThread1.Start();
            }
        }
        public void Pruebas1(){
            Data a = new Data();
            a.Serial_Number = DateTime.Now.ToLongTimeString();
            lData.Add(a);

            a = new Data();
            a.Serial_Number = DateTime.Now.ToLongTimeString();
            lData.Add(a);

            a = new Data();
            a.Serial_Number = DateTime.Now.ToLongTimeString();
            lData.Add(a);

            ExportaExcel();
        }

        public DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
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
                    excel.ImportDataTable(stats1.EndRowIndex+1, 1, dt, false);
                    //System.Diagnostics.Process.Start(file);

                    excel.Save();
                }
            }
            catch (Exception ex)
            {
                Logger.PrimaryLog("ExportaExcel", ex.Message, EventLogEntryType.Error, true);
            }
        }

        public void HiloMaq1()
        {
            Logger.PrimaryLog("HiloMaq1", "Proceso Hilo Maq1 Iniciado", EventLogEntryType.Information, true);
            int cAlive = 0, intentos = 0;
            bool BanderaAlive = false;

            Steps Current = Steps.Alive, Next = Steps.Conexion;

            while (Running)
            {
                try
                {
                    if (!BanderaAlive || intentos >= 3) Current = Next; else Current = Steps.Alive;

                    switch (Current)
                    {
                        case Steps.Alive:
                            Logger.PrimaryLog("MainRutine", "Escribe Alive", EventLogEntryType.Information, false);
                            BanderaAlive = false;
                            if (cAlive <= 5)
                                cAlive++;
                            else
                                cAlive = 0;
                            if (PLC.EscribePLC(PLC_Mitsubishi.TipoDato.Entero, PLC_HeartBeat, cAlive.ToString()))
                            {
                                intentos = 0;
                                Thread.Sleep(250);
                                Next = Steps.StartRead;
                            }
                            else
                            {
                                Logger.PrimaryLog("MainRutine", "Error al escribir alive", EventLogEntryType.Error, true);
                                Thread.Sleep(250);
                                intentos++;
                                if (intentos > 3)
                                {
                                    Next = Steps.Desconexion;
                                }
                            }
                            Thread.Sleep(1000);
                            break;
                        case Steps.StartRead:
                            Logger.PrimaryLog("MainRutine", "Start Read", EventLogEntryType.Information, false);
                           
                            BanderaAlive = true;
                            break;
                        case Steps.Desconexion:
                            Logger.PrimaryLog("MainRutine", "Desconect", EventLogEntryType.Information, true);
                            PLC.DesconectaPLC();
                            if (Running)
                                Next = Steps.Conexion;
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.PrimaryLog("MainRutine", ex.Message, EventLogEntryType.Error, true);
                    Next = Steps.Desconexion;
                }
            }
        }
                
       
    }
}
