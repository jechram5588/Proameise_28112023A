using HslCommunication;
using HslCommunication.Profinet.AllenBradley;
using HslCommunication.Profinet.Melsec;
using Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRO_28112023A.Dlls
{
    public class PLC_AllenBrandly
    {
        private AllenBradleyNet AllenLogixTCP = null;
        private readonly object PLCLock = new object();
        private bool IsConected = false;
        public Log Logger;
        private string PLC_IP = "";
        private int PLC_PORT = 0;
        private byte PLC_Slot = 0;


        public enum TipoDato
        {
            Float, Entero, String, Boolean
        }

        public PLC_AllenBrandly(string ServiceName, string Ip, int Port, byte slot)
        {
            Logger = new Log(ServiceName);
            PLC_IP = Ip;
            PLC_PORT = Port;
            PLC_Slot = slot;
        }

        #region PLC_Methods
        public bool ConectaPLC()
        {
            try
            {
                AllenLogixTCP = null;
                AllenLogixTCP = new AllenBradleyNet();

                if (!System.Net.IPAddress.TryParse(PLC_IP, out System.Net.IPAddress address))
                {
                    Logger.PrimaryLog("Conexion a PLC", "IP Erronea", EventLogEntryType.Error, true);
                    return false;
                }

                AllenLogixTCP.IpAddress = PLC_IP;
                AllenLogixTCP.Port = PLC_PORT;
                AllenLogixTCP.Slot = PLC_Slot;

                AllenLogixTCP.ReceiveTimeOut = 2500;
                AllenLogixTCP.ConnectClose();
            }
            catch (Exception ex)
            {
                Logger.PrimaryLog("Conexion PLC", "Error en IP " + ex.Message, EventLogEntryType.Error, true);
            }

            try
            {
                OperateResult connect = AllenLogixTCP.ConnectServer();
                if (connect.IsSuccess)
                {
                    Logger.PrimaryLog("Conexion a PLC", "Conectado Correctamente", EventLogEntryType.Error, true);
                    IsConected = true;
                    return true;
                }
                else
                {
                    Logger.PrimaryLog("Conexion a PLC", "No se logro conectar", EventLogEntryType.Error, true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.PrimaryLog("Conexion a PLC", ex.Message.ToString(), EventLogEntryType.Error, true);
                return false;
            }
        }
        public void DesconectaPLC()
        {
            if (AllenLogixTCP != null)
            {
                IsConected = false;
                AllenLogixTCP.ConnectClose();
            }
        }

        public bool WritePLC(TipoDato Tipo, string Variable, string Valor)
        {
            lock (this.PLCLock)
            {
                if (!IsConected)
                    this.ConectaPLC();
            }
            if (Valor != null)
            {
                OperateResult result = new OperateResult();
                try
                {
                    lock (this.PLCLock)
                    {
                        switch (Tipo)
                        {
                            case TipoDato.Entero:
                                result = AllenLogixTCP.Write(Variable, int.Parse(Valor));
                                break;
                            case TipoDato.Float:
                                result = AllenLogixTCP.Write(Variable, float.Parse(Valor, CultureInfo.InvariantCulture));
                                break;
                            case TipoDato.String:
                                result = AllenLogixTCP.Write(Variable, Valor);
                                break;
                            case TipoDato.Boolean:
                                result = AllenLogixTCP.Write(Variable, bool.Parse(Valor));
                                break;
                        }
                        return result.IsSuccess;
                    }
                }
                catch (Exception ex)
                {
                    Logger.PrimaryLog("Escribe PLC", string.Format("{0}", ex.Message), EventLogEntryType.Error, true);
                    return false;
                }
            }
            else
            {
                Logger.PrimaryLog("Escribe PLC", "Error de null", EventLogEntryType.Error, true);
                return false;
            }
        }

        public string ReadPLC(TipoDato Tipo, string Variable, ushort cantidad)
        {
            string res = "";
            //OperateResult result = new OperateResult();
            lock (this.PLCLock)
            {
                if (!IsConected)
                    this.ConectaPLC();
            }
            lock (this.PLCLock)
            {
                switch (Tipo)
                {
                    case TipoDato.Entero:
                        res = ReadResultRender(AllenLogixTCP.ReadInt32(Variable));
                        break;
                    case TipoDato.Float:
                        res = ReadResultRender(AllenLogixTCP.ReadFloat(Variable));
                        break;
                    case TipoDato.String:
                        res = ReadResultRender(AllenLogixTCP.ReadString(Variable, cantidad));
                        res = res.Replace("\r", "");
                        res = res.Replace('\0', ' ');
                        res = res.Replace(" ", "");
                        break;
                    case TipoDato.Boolean:
                        res = ReadResultRender(AllenLogixTCP.ReadBool(Variable));
                        break;
                }
            }

            return res.ToString();
        }

        public string[] ReadPLCBulk(string variable, int cantidad)
        {
            lock (this.PLCLock)
            {
                if (!IsConected)
                    this.ConectaPLC();
            }
            string[] res = new string[2];
            try
            {
                lock (this.PLCLock)
                {
                    OperateResult<byte[]> read = AllenLogixTCP.Read(variable, Convert.ToUInt16(cantidad));
                    if (read.IsSuccess)
                    {
                        res[0] = "OK";
                        res[1] = HslCommunication.BasicFramework.SoftBasic.ByteToHexString(read.Content);
                        string convert = HexStrToAscci(HexStringToByteArray(res[1]));
                        convert = convert.Replace("\r", "");
                        res[1] = ReverseString2a1(convert);
                        res[1] = res[1].Replace('\0', ' ');
                        res[1] = res[1].Replace(" ", "");
                    }
                    else
                    {
                        res[0] = "NG";
                        res[1] = read.ToMessageShowString();
                    }
                }
            }
            catch (Exception ex)
            {
                res[0] = "ERROR";
                res[1] = ex.Message;
            }
            return res;
        }

        public static string ReadResultRender<T>(OperateResult<T> result)
        {
            if (result.IsSuccess)
            {
                return result.Content.ToString();
            }
            else
            {
                return "";
            }
        }


        public string HexStrToAscci(byte[] buffer)
        {
            Encoding enc8 = Encoding.UTF8;
            return enc8.GetString(buffer);
        }
        public string ReverseString2a1(string s)
        {
            /* acomodamos el string leido del plc*/
            char[] array = new char[s.Length];
            string last = "";

            if (!(s.Length % 2 == 0))
                last = s.Substring(s.Length - 1, 1);

            for (int i = 0; i < s.Length - 1; i++)
            {
                if (i % 2 == 0)
                    array[i + 1] = s[i];
                else
                    array[i - 1] = s[i];
            }
            if (last != "")
                array[s.Length - 1] = last.ToArray()[0];
            /*separamos Rack type y rack number*/
            //J59W RH 16 #101     50         N

            return new string(array);
        }

        public byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }
        public string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            return sb.ToString().ToUpper();
        }
        public byte[] FromHex(string hex)
        {
            hex = hex.Replace(" ", "");
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }
        #endregion
    }
}
