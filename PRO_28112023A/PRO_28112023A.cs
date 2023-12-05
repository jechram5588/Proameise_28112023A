using DocumentFormat.OpenXml.Drawing;
using Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PRO_28112023A.Class;
using PRO_28112023A.Dlls;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Configuration;
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

        private Data TagsM1;
        private Data TagsM2;

        private List<Data> lData;
        private readonly object DatosLock = new object();


        public bool DebugMode = true;
        public PLC_AllenBrandly PLC;
        public Log Logger;

        private Thread thMaq1 = null;
        private Thread thMaq2 = null;
        private Thread thAlive = null;

        private string Directorio = "";

        public enum Steps
        {
            Disconnect, Alive, StartRead, ReadValues, ExportarExcel, EndRead
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

                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                Directorio = config.AppSettings.Settings["Directorio"].Value;

                PLC = new PLC_AllenBrandly("PLC ", PLC_IP, PLC_Port, PLC_Slot);
                TagsM1 = new Data();
                TagsM1.Triggers = new Triggers { };
                TagsM2 = new Data();
                TagsM2.Triggers = new Triggers { };

                TagsM1.Triggers.StartRead = "Trigger_M1";
                TagsM1.Triggers.EndRead = "Trigger_M1_End";

                TagsM1.Serial_Number_Actuador = "ASSY_STR_SERIAL_NUM.DATA";
                TagsM1.Screw1_Torque = "ASSY_TRACE_T1_TORQUE";
                TagsM1.No_Turns_1 = "ASSY_TRACE_T1_ANGLE";
                TagsM1.Screw2_Torque = "ASSY_TRACE_T2_TORQUE";
                TagsM1.No_Turns_2 = "ASSY_TRACE_T2_ANGLE";

                TagsM2.Serial_Number_Actuador = "TEST_STR_SERIAL_NUM.DATA";
                TagsM2.Serial_Number = "TEST_TRACE_SERIAL_NUM.DATA";
                TagsM2.Part_Number = "TEST_STR_PART_NUM.DATA";
                TagsM2.Triggers.StartRead = "Trigger_M2";
                TagsM2.Triggers.EndRead = "Trigger_M2_End";

                TagsM2.Current_Amp = "TEST_CURRENT_DATA";
                TagsM2.Voltage_Vcc = "TEST_VOLTAGE_DATA";
                TagsM2.Customer_Position = "TEST_POSITION_DATA";

                TagsM2.Actuador_Speed = "TEST_SPEED_DATA";

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
                thMaq2 = new Thread(Maquina2);
                thMaq2.Start();
            }
        }

        public void Maquina1()
        {
            Logger.PrimaryLog("Maquina1", "Iniciado", EventLogEntryType.Information, true);
            Steps paso = Steps.StartRead;
            Data Valores = null;

            while (Running)
            {
                switch (paso)
                {
                    case Steps.StartRead:
                        if (PLC.ReadPLC(PLC_AllenBrandly.TipoDato.Boolean, TagsM1.Triggers.StartRead, 0) == "True")
                        {
                            Logger.PrimaryLog("Maquina1:StartRead", "Trigger Detectado", EventLogEntryType.Information, false);
                            paso = Steps.ReadValues;
                        }
                        else
                            Thread.Sleep(1000);
                        break;
                    case Steps.ReadValues:
                        Valores = new Data {};
                        
                        Valores.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        
                        Valores.Serial_Number_Actuador = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.String, TagsM1.Serial_Number_Actuador, 13);

                        Valores.Screw1_Torque = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.Float, TagsM1.Screw1_Torque, 0);
                        Valores.No_Turns_1 = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.Float, TagsM1.No_Turns_1, 0);
                        Valores.Screw2_Torque = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.Float, TagsM1.Screw2_Torque, 0);
                        Valores.No_Turns_2 = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.Float, TagsM1.No_Turns_2, 0);

                        Valores.Sensor1_OK = "OK";
                        Valores.Sensor2_OK = "OK";
                        Valores.Sensor3_OK = "OK";
                        Valores.Sensor4_OK = "OK";

                        lock (DatosLock) 
                            lData.Add(Valores);
                        
                        paso = Steps.EndRead;
                        break;
                    case Steps.EndRead:
                        lData.Add(Valores);
                        PLC.WritePLC(PLC_AllenBrandly.TipoDato.Boolean, TagsM1.Triggers.EndRead, "True");
                        paso = Steps.StartRead;
                        Logger.PrimaryLog("Maquina1:EndRead", "Trigger Enviado", EventLogEntryType.Information, false);
                        break;
                    default:
                        break;
                }
            }
            Logger.PrimaryLog("Maquina1", "Terminado", EventLogEntryType.Information, true);
        }

        public void Maquina2()
        {
            Logger.PrimaryLog("Maquina2", "Iniciado", EventLogEntryType.Information, true);
            Steps paso = Steps.StartRead;
            string Id_Serial = "";
            Data Valores = null;
            while (Running)
            {
                switch (paso)
                {
                    case Steps.StartRead:
                        if (PLC.ReadPLC(PLC_AllenBrandly.TipoDato.Boolean, TagsM2.Triggers.StartRead, 0) == "True")
                        {
                            Logger.PrimaryLog("Maquina2:StartRead", "Trigger Detectado", EventLogEntryType.Information, false);
                            paso = Steps.ReadValues;
                        }
                        else
                            Thread.Sleep(1000);
                        break;
                    case Steps.ReadValues:
                        Valores = new Data { };

                        Valores.Serial_Number_Actuador = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.String, TagsM2.Serial_Number_Actuador, 13);
                        Id_Serial = Valores.Serial_Number_Actuador;

                        Valores.Part_Number = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.String, TagsM2.Part_Number,7);
                        Valores.Serial_Number = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.String, TagsM2.Serial_Number,27);
                        Valores.Soft_Actuador = "OK";
                        Valores.Movement_Vanes = "OK";
                        Valores.Current_Amp = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.Float, TagsM2.Current_Amp, 0);
                        Valores.Voltage_Vcc = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.Float, TagsM2.Voltage_Vcc, 0);
                        Valores.Customer_Position = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.Float, TagsM2.Customer_Position, 0);

                        Valores.Actuador_Speed = PLC.ReadPLC(PLC_AllenBrandly.TipoDato.Float, TagsM2.Actuador_Speed, 0);
                        Valores.Customer_QR_OK = "OK";
                        Valores.Corret_Soft_Actuador_OK = "OK";

                        lock (DatosLock) {
                            var data = lData.Find(x => x.Serial_Number_Actuador == Id_Serial);
                            if (data != null)
                            {
                                data.Part_Number = Valores.Part_Number;
                                data.Serial_Number = Valores.Serial_Number;
                                data.Soft_Actuador = Valores.Soft_Actuador;
                                data.Movement_Vanes = Valores.Movement_Vanes;
                                data.Current_Amp = Valores.Current_Amp;
                                data.Voltage_Vcc = Valores.Voltage_Vcc;
                                data.Customer_Position = Valores.Customer_Position;
                                data.Actuador_Speed = Valores.Actuador_Speed;
                                data.Customer_QR_OK = Valores.Customer_QR_OK;
                                data.Corret_Soft_Actuador_OK = Valores.Corret_Soft_Actuador_OK;
                                Logger.PrimaryLog("Maquina2:", "Encontrado", EventLogEntryType.Information, false);
                            }
                            else { 
                                lData.Add(Valores);
                                Logger.PrimaryLog("Maquina2:", "Nuevo Data", EventLogEntryType.Information, false);
                            }
                        }
                        ExportaExcel(Id_Serial);
                        paso = Steps.EndRead;
                        break;
                    case Steps.EndRead:
                        PLC.WritePLC(PLC_AllenBrandly.TipoDato.Boolean, TagsM2.Triggers.EndRead, "True");
                        paso = Steps.StartRead;
                        Logger.PrimaryLog("Maquina2:EndRead", "Trigger Enviado", EventLogEntryType.Information, false);
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

            dataTable.Columns["Serial_Number"].SetOrdinal(0);
            dataTable.Columns["Date"].SetOrdinal(1);
            dataTable.Columns["Part_Number"].SetOrdinal(2);
            dataTable.Columns["Serial_Number_Actuador"].SetOrdinal(3);
            dataTable.Columns["Screw1_Torque"].SetOrdinal(4);
            dataTable.Columns["No_Turns_1"].SetOrdinal(5);
            dataTable.Columns["Screw2_Torque"].SetOrdinal(6);
            dataTable.Columns["No_Turns_2"].SetOrdinal(7);

            dataTable.Columns["Sensor1_OK"].SetOrdinal(8);
            dataTable.Columns["Sensor2_OK"].SetOrdinal(9);
            dataTable.Columns["Sensor3_OK"].SetOrdinal(10);
            dataTable.Columns["Sensor4_OK"].SetOrdinal(11);

            dataTable.Columns["Soft_Actuador"].SetOrdinal(12);
            dataTable.Columns["Movement_Vanes"].SetOrdinal(13);

            dataTable.Columns["Current_Amp"].SetOrdinal(14);
            dataTable.Columns["Voltage_Vcc"].SetOrdinal(15);
            dataTable.Columns["Customer_Position"].SetOrdinal(16);

            dataTable.Columns["Actuador_Speed"].SetOrdinal(17);
            dataTable.Columns["Customer_QR_OK"].SetOrdinal(18);
            dataTable.Columns["Corret_Soft_Actuador_OK"].SetOrdinal(19);


            return dataTable;
        }
        public void ExportaExcel(string Id)
        {
            try
            {
                string Nuevo = AppDomain.CurrentDomain.BaseDirectory.ToString() + "\\Templates\\template.xlsx";
                string Fecha = DateTime.Now.ToString("yyyy-MM-dd");
                string Existente = Directorio + "\\" + Fecha + ".xlsx";
                
                List<Data> Temp = new List<Data>();

                lock (DatosLock) {
                    var exportar = lData.Find(x => x.Serial_Number_Actuador == Id);

                    if (exportar != null)
                        Temp.Add(exportar);

                    if(Temp.Count>0)
                        if (!File.Exists(Existente))
                        {
                            SLDocument excel = new SLDocument(Nuevo);
                            DataTable dt = ToDataTable(Temp);
                            excel.ImportDataTable(3, 1, dt, false);
                            excel.SaveAs(Existente);
                        }
                        else
                        {
                            Console.WriteLine("Excel from " + Existente);
                            SLDocument excel = new SLDocument(Existente);
                            SLWorksheetStatistics stats1 = excel.GetWorksheetStatistics();
                            DataTable dt = ToDataTable(Temp);
                            excel.ImportDataTable(stats1.EndRowIndex + 1, 1, dt, false);
                            excel.Save();
                        }
                    lData.RemoveAll(x => x.Serial_Number_Actuador == Id);
                }
               
                Logger.PrimaryLog("Exportar Excel", $"Terminado", EventLogEntryType.Information, true);

            }
            catch (Exception ex)
            {
                Logger.PrimaryLog("ExportaExcel", ex.Message, EventLogEntryType.Error, true);
            }
        }
    }
}
