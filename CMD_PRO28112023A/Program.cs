using PRO_28112023A;
using PRO_28112023A.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMD_PRO28112023A
{
    class Program
    {
        static void Main(string[] args)
        {
            PRO_28112023A.PRO_28112023A ser = new PRO_28112023A.PRO_28112023A();
            ser.Start();
            //List<Data> lDatos = new List<Data>();

            //Data Valores = new Data { };
            //Valores.Serial_Number_Actuador = "1";
            //Valores.Serial_Number = "number 1";
            //lDatos.Add(Valores);


            //Valores = new Data ();
            //Valores.Serial_Number_Actuador = "2";
            //Valores.Serial_Number = "number 2";
            //lDatos.Add(Valores);

            //Valores = new Data();
            //Valores.Serial_Number_Actuador = "3";
            //Valores.Serial_Number = "number 3";
            //lDatos.Add(Valores);

            //foreach (Data item in lDatos)
            //{
            //    Console.WriteLine($" {item.GetHashCode()} {item.Serial_Number_Actuador} {item.Serial_Number}");
            //}

            //var d = lDatos.Find(x => x.Serial_Number_Actuador == "2");

            //lDatos.Remove(d);

            //foreach (Data item in lDatos)
            //{
            //    Console.WriteLine($" {item.GetHashCode()} {item.Serial_Number_Actuador} {item.Serial_Number}");
            //}

            //Console.ReadKey();

        }
    }
}
