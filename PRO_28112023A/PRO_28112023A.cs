using Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PRO_28112023A.Class;
using PRO_28112023A.Dlls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private Devices DMs;

        public bool DebugMode = true;
        public PLC_Mitsubishi PLC;
        public Log Logger;
        Thread ClassThread = null;
        JObject JsonData = null;

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
            if (ClassThread != null)
            {
                ClassThread.Abort();
            }
        }
        public void LoadConfig()
        {
            int error = 0;
            /*Carga de parametros app*/
            try
            {
                string file = AppDomain.CurrentDomain.BaseDirectory.ToString() + "Devices.json";
                if (File.Exists(file))
                {
                    JsonData = JObject.Parse(File.ReadAllText(file));
                    DMs = JsonConvert.DeserializeObject<Devices>(JsonData.ToString());
                    Logger.PrimaryLog("LoadConfig", "Carga de parametros correcta", EventLogEntryType.Information, true);
                }
                else
                {
                    error += 1;
                    Logger.PrimaryLog("LoadConfig", "Sin Archivo de configuracion", EventLogEntryType.Error, true);
                }
            }
            catch (Exception ex)
            {
                Logger.PrimaryLog("LoadConfig", ex.Message, EventLogEntryType.Error, true);
                error++;
            }
            if (error == 0)
            {
                ClassThread = new Thread(MainRutine);
                ClassThread.Start();
            }
        }
        public void MainRutine()
        {
            Logger.PrimaryLog("MainRutine", "Proceso Principal", EventLogEntryType.Information, true);
            int cAlive = 0, intentos = 0;
            bool BanderaAlive = false;
            PLC = new PLC_Mitsubishi("PLC " + DMs.PLC_Port, DMs.PLC_IP, DMs.PLC_Port);
            Steps Current = Steps.Conexion, Next = Steps.Conexion;
            while (Running)
            {
                try
                {
                    if (!BanderaAlive || intentos >= 3) Current = Next; else Current = Steps.Alive;

                    switch (Current)
                    {
                        case Steps.Conexion:
                            if (PLC.ConectaPLC())
                            {
                                Logger.PrimaryLog("Conexion", "Conectado Correctamente", EventLogEntryType.Information, true);
                                Next = Steps.Alive;
                                intentos = 0;
                                Logger.PrimaryLog("Conexion", "Inicia", EventLogEntryType.Information, false);
                            }
                            else
                            {
                                Logger.PrimaryLog("Conexion", "ERROR al conectar", EventLogEntryType.Error, true);
                            }
                            Thread.Sleep(1000);
                            break;
                        case Steps.Alive:
                            Logger.PrimaryLog("MainRutine", "Escribe Alive", EventLogEntryType.Information, false);
                            BanderaAlive = false;
                            if (cAlive <= 5)
                                cAlive++;
                            else
                                cAlive = 0;
                            if (PLC.EscribePLC(PLC_Mitsubishi.TipoDato.Entero, DMs.PLC_HeartBeat, cAlive.ToString()))
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
                            if (PLC.LeePLC(PLC_Mitsubishi.TipoDato.Entero, DMs.StartRead, 1) == "1")
                            {
                                Devices DMs_Data = JsonConvert.DeserializeObject<Devices>(JsonData.ToString());

                                Logger.PrimaryLog("MainRutine", "Trigger", EventLogEntryType.Information, false);

                                DMs_Data.Barcode = PLC.LeePLC(PLC_Mitsubishi.TipoDato.String, DMs.Barcode, 8);
                                DMs_Data.Judgment = PLC.LeePLC(PLC_Mitsubishi.TipoDato.Entero, DMs.Judgment, 0) == "1" ? "OK" : "NG";

                                LeeData(PLC, 1, 32, DMs_Data);

                                PLC.EscribePLC(PLC_Mitsubishi.TipoDato.Entero, DMs.EndRead, "1");
                                Logger.PrimaryLog("MainRutine", "End Read", EventLogEntryType.Information, false);

                                Logger.PrimaryLog("MainRutine", "SQL Insert", EventLogEntryType.Information, false);
                            }
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
                }//end try
                catch (Exception ex)
                {
                    PLC.EscribePLC(PLC_Mitsubishi.TipoDato.Entero, DMs.EndRead, "1");
                    Logger.PrimaryLog("MainRutine", ex.Message, EventLogEntryType.Error, true);
                    Next = Steps.Desconexion;
                }
            }
        }

        public void LeeData(PLC_Mitsubishi _plc, int Ini, int Fin, Devices DMs_Data)
        {

            foreach (DataPoints item in DMs_Data.DataPoints.Where(d => d.Id >= Ini && d.Id <= Fin && d.Standard.StartsWith("D")))
            {
                for (int i = 0; i < 3; i++)
                {
                    string Standard = "";
                    if (i > 0) Logger.PrimaryLog("LeeData", item.Standard + $" Intento {i}", EventLogEntryType.Error, true);
                    Standard = _plc.LeePLC(PLC_Mitsubishi.TipoDato.Entero, item.Standard, 0);
                    if (Standard != "")
                    {
                        item.Standard = (Convert.ToInt32(Standard) / 100.0).ToString();
                        break;
                    }
                }
            }

            foreach (DataPoints item in DMs_Data.DataPoints.Where(d => d.Id >= Ini && d.Id <= Fin && d.Upper.StartsWith("D")))
            {
                for (int i = 0; i < 3; i++)
                {
                    string Upper = "";
                    if (i > 0) Logger.PrimaryLog("LeeData", item.Upper + $" Intento {i}", EventLogEntryType.Error, true);
                    Upper = _plc.LeePLC(PLC_Mitsubishi.TipoDato.Entero, item.Upper, 0);
                    if (Upper != "")
                    {
                        item.Upper = (Convert.ToInt32(Upper) / 100.0).ToString();
                        break;
                    }
                }
            }

            foreach (DataPoints item in DMs_Data.DataPoints.Where(d => d.Id >= Ini && d.Id <= Fin && d.Lower.StartsWith("D")))
            {
                for (int i = 0; i < 3; i++)
                {
                    string Lower = "";

                    if (i > 0) Logger.PrimaryLog("LeeData", item.Lower + $" Intento {i}", EventLogEntryType.Error, true);
                    Lower = _plc.LeePLC(PLC_Mitsubishi.TipoDato.Entero, item.Lower, 0);
                    if (Lower != "")
                    {
                        item.Lower = (Convert.ToInt32(Lower) / 100.0).ToString();
                        break;
                    }
                }
            }

            foreach (DataPoints item in DMs_Data.DataPoints.Where(d => d.Id >= Ini && d.Id <= Fin && d.StdValue.StartsWith("D")))
            {
                for (int i = 0; i < 3; i++)
                {
                    string StdValue = "";
                    if (i > 0) Logger.PrimaryLog("LeeData", item.StdValue + $" Intento {i}", EventLogEntryType.Error, true);
                    StdValue = _plc.LeePLC(PLC_Mitsubishi.TipoDato.Entero, item.StdValue, 0);
                    if (StdValue != "")
                    {
                        item.StdValue = (Convert.ToInt32(StdValue) / 100.0).ToString(); ;
                        break;
                    }
                }
            }

            foreach (DataPoints item in DMs_Data.DataPoints.Where(d => d.Id >= Ini && d.Id <= Fin && d.Result.StartsWith("D")))
            {
                for (int i = 0; i < 3; i++)
                {
                    string Result = "";
                    if (i > 0) Logger.PrimaryLog("LeeData", item.Result + $" Intento {i}", EventLogEntryType.Error, true);
                    Result = _plc.LeePLC(PLC_Mitsubishi.TipoDato.Entero, item.Result, 0);
                    if (Result != "")
                    {
                        item.Result = (Convert.ToInt32(Result) / 100.0).ToString(); ;
                        break;
                    }
                }
            }

            foreach (DataPoints item in DMs_Data.DataPoints.Where(d => d.Id >= Ini && d.Id <= Fin && d.Judgment.StartsWith("D")))
            {
                for (int i = 0; i < 3; i++)
                {
                    string Judgment = "";

                    if (i > 0) Logger.PrimaryLog("LeeData", item.Judgment + $" Intento {i}", EventLogEntryType.Error, true);
                    Judgment = _plc.LeePLC(PLC_Mitsubishi.TipoDato.Entero, item.Judgment, 0);
                    if (Judgment != "")
                    {
                        item.Judgment = Judgment == "1" ? "OK" : "NG";
                        break;
                    }
                }
            }



        }

    }
}
