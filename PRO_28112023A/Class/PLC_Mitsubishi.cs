using HslCommunication;
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
    public class PLC_Mitsubishi
    {
        private MelsecMcNet melsec_net = null;
        private readonly object PLCLock = new object();
        private bool IsConected = false;
        public Log Logger;
        private string PLC_IP = "";
        private int PLC_PORT = 0;

        public enum TipoDato
        {
            Float, Entero, String
        }

        public PLC_Mitsubishi(string ServiceName, string Ip, int Port)
        {
            Logger = new Log(ServiceName);
            PLC_IP = Ip;
            PLC_PORT = Port;
        }

        #region PLC_Methods
        public bool ConectaPLC()
        {
            try
            {
                melsec_net = null;
                melsec_net = new MelsecMcNet();

                if (!System.Net.IPAddress.TryParse(PLC_IP, out System.Net.IPAddress address))
                {
                    Logger.PrimaryLog("Conexion a PLC", "IP Erronea", EventLogEntryType.Error, true);
                    return false;
                }

                melsec_net.IpAddress = PLC_IP;
                melsec_net.Port = PLC_PORT;
                melsec_net.ReceiveTimeOut = 2500;
                melsec_net.ConnectClose();
            }
            catch (Exception ex)
            {
                Logger.PrimaryLog("Conexion PLC", "Error en IP " + ex.Message, EventLogEntryType.Error, true);
            }

            try
            {
                OperateResult connect = melsec_net.ConnectServer();
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
            if (melsec_net != null)
            {
                IsConected = false;
                melsec_net.ConnectClose();
            }
        }

        public bool EscribePLC(TipoDato Tipo, string Variable, string Valor)
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
                                result = melsec_net.Write(Variable, int.Parse(Valor));
                                break;
                            case TipoDato.Float:
                                result = melsec_net.Write(Variable, float.Parse(Valor, CultureInfo.InvariantCulture));
                                break;
                            case TipoDato.String:
                                result = melsec_net.Write(Variable, Valor);
                                break;
                        }
                        if (result.IsSuccess)
                        {
                            return true;
                        }
                        else
                            return false;
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
                Logger.PrimaryLog("EscribePLC", "Error de null", EventLogEntryType.Error, true);
                return false;
            }
        }

        public string LeePLC(TipoDato Tipo, string Variable, ushort cantidad)
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
                        res = ReadResultRender(melsec_net.ReadInt16(Variable));
                        break;
                    case TipoDato.Float:
                        res = ReadResultRender(melsec_net.ReadFloat(Variable));
                        break;
                    case TipoDato.String:
                        res = ReadResultRender(melsec_net.ReadString(Variable, cantidad));
                        res = res.Replace("\r", "");
                        res = res.Replace('\0', ' ');
                        res = res.Replace(" ", "");
                        break;
                }
            }

            return res.ToString();
        }

        public string[] LeerPLCBulk(string variable, int cantidad)
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
                    OperateResult<byte[]> read = melsec_net.Read(variable, Convert.ToUInt16(cantidad));
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
