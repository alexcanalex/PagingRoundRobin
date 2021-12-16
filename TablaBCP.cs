using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace PagingRR
{
    public partial class TablaBCP : Form
    {
        //Constantes de estado
        private const int ABORT = -1;
        private const int END = 0;
        private const int EXEC = 1;
        private const int NEW = 2;
        private const int READY = 3;
        private const int BLOCKED = 4;
        private const int UNBLOCKED = 5;
        private const int SUSP = 6;
        private const int FLAG = 9; //Multipropósito

        Queue<Proc> ramP;
        Queue<Proc> newP;
        Queue<Proc> suspP;
        Queue<Proc> endedP;
        List<BCP> bcps;
        Queue<string[]> all_procs_log;

        public TablaBCP(Queue<Proc> RAM_procs, Queue<Proc> new_procs,
            Queue<Proc> susp_procs, Queue<Proc> finished_procs,
            List<BCP> all_BCPs)
        {
            InitializeComponent();

            //Copia colas y listas recibidas
            ramP = RAM_procs;
            newP = new_procs;
            suspP = susp_procs;
            endedP = finished_procs;
            bcps = all_BCPs;

            all_procs_log = allProcs2strings();
            trivialZerosVerifier(all_procs_log);
            tabla_gridview.DataSource = list2DataTable(all_procs_log);
        }

        private void TablaBCP_Load(object sender, EventArgs e)
        {

        }

        private void trivialZerosVerifier(Queue<string[]> procs)
        {
            foreach (string[] str_a in procs)
            {
                if (str_a[3] == "NUEVO")
                {
                    str_a[2] = "N/A";
                    str_a[8] = "N/A";
                    for (int i = 0; i < 13; i++)
                        if (str_a[i] == "0")
                            str_a[i] = "N/A";
                }

                else if (str_a[3] == "LISTO")
                {
                    str_a[2] = "N/A";
                    str_a[6] = "N/A";
                    str_a[9] = "N/A";
                    str_a[10] = "N/A";
                    str_a[12] = "N/A";
                }

                else if (str_a[3] == "DESBLOQUEADO"
                    || str_a[3] == "EJECUCIÓN")
                {
                    str_a[2] = "N/A";
                    str_a[9] = "N/A";
                    str_a[10] = "N/A";
                    str_a[12] = "N/A";
                }

                else if (str_a[3] == "BLOQUEADO")
                {
                    str_a[2] = "N/A";
                    str_a[9] = "N/A";
                    str_a[10] = "N/A";
                }

                else if (str_a[3] == "SUSPENDIDO")
                {
                    str_a[2] = "N/A";
                    str_a[9] = "N/A";
                    str_a[10] = "N/A";
                }

                else if (str_a[3] == "ABORTADO")
                {
                    str_a[2] = "ERROR";
                    str_a[12] = "N/A";
                }

                else //TERMINADO
                {
                    str_a[12] = "N/A";
                }
            }
        }

        private Queue<string[]> allProcs2strings()
        {
            /* De cada lista saca todos los procs
             * y los convierte en arreglos de
             * palabras que almacena en una cola. */
            Queue<string[]> allProcs = new Queue<string[]>();

            foreach (Proc p in ramP)
                allProcs.Enqueue(proc2stringArray(p));
            foreach (Proc p in newP)
                allProcs.Enqueue(proc2stringArray(p));
            foreach (Proc p in suspP)
                allProcs.Enqueue(proc2stringArray(p));
            foreach (Proc p in endedP)
                allProcs.Enqueue(proc2stringArray(p));

            return allProcs;
        }

        //Subrutina de allProcs2strings
        private string[] proc2stringArray(Proc p)
        {
            /* Convierte proc y su BCP a palabras y las
             * almacena en un arreglo de tantos campos
             * como palabras se hayan obtenido y al
             * final regresa dicho arreglo. */
            const int COLUMNS = 13;

            string[] process_str = new string[COLUMNS];
            BCP bcp = p.BCPfinder(bcps);

            string PID = p.PID.ToString();
            string TME = p.TME.ToString();
            string Oper = p.oper1.ToString() + p.symbol + p.oper2.ToString();
            string Result = p.result.ToString("0.##");

            string State;
            switch (bcp.state)
            {
                case ABORT:     State = "ABORTADO";      break;
                case END:       State = "TERMINADO";     break;
                case EXEC:      State = "EJECUCIÓN";     break;
                case NEW:       State = "NUEVO";         break;
                case READY:     State = "LISTO";         break;
                case BLOCKED:   State = "BLOQUEADO";     break;
                case UNBLOCKED: State = "DESBLOQUEADO";  break;
                case SUSP:      State = "SUSPENDIDO";    break;
                case FLAG:      State = "LISTO";         break; //Flag en este prog. es para ciclos RR
                default:        State = "INDETERMINADO"; break;
            }

            string Arrival = bcp.arrival.ToString();
            string End = bcp.end.ToString();
            string Response = bcp.response.ToString();
            string Exec = bcp.exec.ToString();
            string Idle = bcp.idle.ToString();
            string Returning = bcp.returning.ToString();
            string Blocked = bcp.blocked.ToString();
            string Remaining = (p.TME - bcp.exec).ToString();

            // Calcula tiempo de espera de procs en RAM
            if (State == "LISTO" ||
                State == "EJECUCIÓN" ||
                State == "BLOQUEADO" ||
                State == "DESBLOQUEADO" ||
                State == "SUSPENDIDO")
            {
                int idleT = SO.TotalElapsedTime - (int)(bcp.arrival + bcp.exec);
                idleT--;
                Idle = idleT.ToString();
            }

            int i = 0;
            process_str[i++] = PID;
            process_str[i++] = Oper;
            process_str[i++] = Result;
            process_str[i++] = State;
            process_str[i++] = TME;
            process_str[i++] = Arrival;
            process_str[i++] = Response;
            process_str[i++] = Exec;
            process_str[i++] = Remaining;
            process_str[i++] = End;
            process_str[i++] = Returning;
            process_str[i++] = Idle;
            process_str[i] = Blocked;

            return process_str;
        }

        private DataTable list2DataTable(Queue<string[]> procs)
        {
            DataTable table = new DataTable();
            const int COLUMNS = 13;

            //Nombre de las columnas
            string[] columnNames = new string[COLUMNS];
            int c = 0;
            columnNames[c++] = "PID";
            columnNames[c++] = "Operación";
            columnNames[c++] = "Resultado";
            columnNames[c++] = "Estado";
            columnNames[c++] = "TME";
            columnNames[c++] = "Llegada";
            columnNames[c++] = "Respuesta";
            columnNames[c++] = "Servicio";
            columnNames[c++] = "Restante";
            columnNames[c++] = "Fin";
            columnNames[c++] = "Retorno";
            columnNames[c++] = "Espera";
            columnNames[c] = "Bloqueado";

            //Agrega tantas columnas como campos
            for (int i = 0; i < COLUMNS; i++)
            {
                table.Columns.Add(columnNames[i]);
            }
            //Agrega tantas filas como elementos
            foreach (string[] str_a in procs)
                table.Rows.Add(str_a);

            return table;
        }
    }
}
