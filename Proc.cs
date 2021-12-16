using System;
using System.Collections.Generic;

namespace PagingRR
{
    public class Proc
    {
        public int PID { get; set; }
        public int size { get; set; }
        //Tiempo Máximo Estimado
        public int TME { get; set; }
        public double oper1 { get; set; }
        public double oper2 { get; set; }
        public double result { get; set; }
        public string symbol { get; set; }

        //CONSTRUCTORES
        public Proc()
        {
            PID = 0;
            size = 0;
            TME = 0;
            oper1 = 0.0;
            oper2 = 0.0;
            result = 0.0;
            symbol = "";
        }

        public Proc(Proc p)
        {
            this.PID = p.PID;
            this.size = p.size;
            this.TME = p.TME;
            this.oper1 = p.oper1;
            this.oper2 = p.oper2;
            this.result = p.result;
            this.symbol = p.symbol;
        }

        //Constructor random
        public Proc(Random r, int ID)
        {
            this.PID = ID;
            this.size = r.Next(5, 25);
            this.TME = r.Next(6, 15);
            this.oper1 = r.Next(1, 99);
            this.oper2 = r.Next(1, 99);

            //Convierte núm. random en operador
            int ran = r.Next(1, 5);
            if (ran == 1) this.symbol = "+";
            else if (ran == 2) this.symbol = "-";
            else if (ran == 3) this.symbol = "*";
            else if (ran == 4) this.symbol = "/";
            else this.symbol = "%";

            /* Calcula el resultado de la operación
             * y lo asigna según corresponda. */
            if (this.symbol == "+")
                this.result = this.oper1 + this.oper2;
            else if (this.symbol == "-")
                this.result = this.oper1 - this.oper2;
            else if (this.symbol == "*")
                this.result = this.oper1 * this.oper2;
            else if (this.symbol == "/")
                this.result = this.oper1 / this.oper2;
            else
                this.result = this.oper1 % this.oper2;
        }

        public Proc(int nulo)
        {
            this.PID = nulo; //nulo = -1
            this.size = nulo;
            this.TME = 0;
            this.oper1 = 0.0;
            this.oper2 = 0.0;
            this.result = 0.0;
            this.symbol = "";
        }

        //MÉTODOS
        public BCP BCPfinder(List<BCP> bcp_queue)
        {
            /* Recibe una lista de BCPs y
             * relaciona el proceso con su
             * respectivo BCP mediante el ID.
             * Regresa un objeto clase BCP. */

            //Busca en cola BCP con su PID
            foreach (BCP bcp in bcp_queue)
                if (this.PID == bcp.PID)
                    return bcp;
            return SO.NullBCP;
        }

    }
}
