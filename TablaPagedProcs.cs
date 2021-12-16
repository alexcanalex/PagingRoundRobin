using PagingRR;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace PagingRoundRobin
{
    public partial class TablaPagedProcs : Form
    {
        Queue<Proc> ram;
        List<BCP> all_bcps;

        public TablaPagedProcs(Queue<Proc> RAM_procs, List<BCP> all_BCPs)
        {
            InitializeComponent();

            //Copia colas y listas recibidas
            ram = RAM_procs;
            all_bcps = all_BCPs;

            tabla_gridview.DataSource = list2DataTable(RAMprocs2BCPs());
        }

        private void TablaPaging_Load(object sender, EventArgs e)
        {

        }

        private List<BCP> RAMprocs2BCPs()
        {
            /* Crea lista de BCPs de 
             * todos los procesos en RAM. */
            List<BCP> bcps = new List<BCP>();

            foreach (Proc p in ram)
                bcps.Add(p.BCPfinder(all_bcps));
            return bcps;
        }

        private DataTable list2DataTable(List<BCP> procs)
        {
            /* Crea tabla de todos los
             * procs paginados. */
            DataTable table = new DataTable();
            //Cantidad de columnas
            const int COLUMNS = 4;

            //Nombre de las columnas
            string[] columnNames = new string[COLUMNS];
            int c = 0;
            columnNames[c++] = "PID";
            columnNames[c++] = "Tamaño";
            columnNames[c++] = "Páginas";
            columnNames[c] = "Marcos";

            //Agrega tantas columnas como campos
            for (int i = 0; i < COLUMNS; i++)
                table.Columns.Add(columnNames[i]);

            //Agrega tantas filas como elementos

            /* Convierte cada elemento relevante
             * del BCP en palabras que agrega a
             * un arreglo de palabras. */
            foreach (BCP b in procs)
            {
                //Calcula tamaño total del proc
                int size = 0;
                for (int i = 0; i < b.totalPages; i++)
                    size += b.pages[i];

                //Carga todas las filas del enésimo BCP
                for (int i = 0; i < b.totalPages; i++)
                {
                    string[] str_p = new string[COLUMNS];

                    //Carga la fila enésima del enésimo BCP
                    int k = 0;
                    str_p[k++] = b.PID.ToString();
                    str_p[k++] = size.ToString();
                    str_p[k++] = i.ToString();
                    str_p[k] = b.frames[i].ToString();

                    table.Rows.Add(str_p);
                }
            }
            return table;
        }
    }
}
