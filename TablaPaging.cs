using PagingRR;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace PagingRoundRobin
{
    public partial class TablaPaging : Form
    {
        //Constantes de estado
        private const int EXEC = 1;
        private const int READY = 3;
        private const int BLOCKED = 4;
        private const int UNBLOCKED = 5;
        private const int FLAG = 9;

        List<BCP> bcps;

        public TablaPaging(List<BCP> all_BCPs)
        {
            InitializeComponent();

            //Copia cola recibida
            bcps = all_BCPs;
            List<string[]> frames = pagesLister();

            tabla_gridview.DataSource = list2DataTable(frames);
        }

        private void TablaPaging_Load(object sender, EventArgs e)
        {

        }

        private List<string[]> pagesLister()
        {
            //Lista de todos los marcos en entero
            List<int[]> all_frames = new List<int[]>();

            //Lista de control de marcos desocupados
            int[] remainingFrames = new int[42];

            //Se inicializa
            for (int i = 0; i < 42; i++)
                remainingFrames[i] = i;

            //Para cada BCP
            foreach (BCP b in bcps)
            {
                //Si son procesos paginados
                if (b.state == EXEC || b.state == READY
                    || b.state == BLOCKED || b.state == UNBLOCKED
                    || b.state == FLAG)
                {
                    //Cada página a su respectivo marco
                    for (int i = 0; i < b.totalPages; i++)
                    {
                        int k = 0;
                        int[] frame = new int[3];

                        frame[k++] = b.frames[i]; //Marco ocupado
                        frame[k++] = b.pages[i]; //Tamaño de página
                        frame[k] = b.PID; //Proceso que lo ocupa

                        //Agregamos el arreglo de enteros a la lista
                        all_frames.Add(frame);

                        //Elimina el marco que ya no queda
                        for (int j = 0; j < remainingFrames.Length; j++)
                            if (remainingFrames[j] == b.frames[i])
                                remainingFrames[j] = -1; //Marco inválido significa borrado
                    }
                }
            }

            //Agrega todos los marcos desocupados a lista all_frames
            for (int i = 0; i < remainingFrames.Length; i++)
            {
                //Si es un marco desocupado
                if (remainingFrames[i] != -1)
                {
                    int k = 0;
                    int[] frame = new int[3];

                    frame[k++] = remainingFrames[i]; //Marco
                    frame[k++] = 0; //Tamaño inválido
                    frame[k] = 0; //PID inválido

                    all_frames.Add(frame);
                }
            }

            //Acomodamos la lista del primer marco al último
            List<int[]> all_frames_sorted = new List<int[]>();

            int idx = 0;
            while (all_frames.Count > 0) //Mientras no acomodemos todos
            {
                for (int i = 0; i < all_frames.Count; i++) //De todos los marcos buscamos el enésimo número (0,1,2...)
                {
                    if (all_frames[i][0] == idx) //Si su ID es el enésimo
                    {
                        all_frames_sorted.Add(all_frames[i]); //Lo agregamos a los acomodados
                        all_frames.RemoveAt(i); //Lo sacamos de la lista
                        idx++; //Buscamos el siguiente enésimo número
                        break; //Salimos del for, pues los IDs son únicos
                    }
                }
            }

            //Cambiamos todos los arreglos de enteros a palabras
            List<string[]> allFrames = new List<string[]>();

            foreach (int[] i_a in all_frames_sorted)
            {
                int k = 0;
                string[] s_a = new string[3];

                s_a[k] = i_a[k++].ToString();
                s_a[k] = i_a[k++].ToString();

                //Si no hay proceso
                if (i_a[k] == 0)
                    s_a[k] = "Ninguno";
                else
                    s_a[k] = i_a[k].ToString();

                allFrames.Add(s_a);
            }

            //Agregamos el Linuxito :3
            for (int i = 0; i < 3; i++)
            {
                int k = 0;
                string[] s_a = new string[3];

                s_a[k++] = (i + 42).ToString();
                s_a[k++] = "4";
                s_a[k] = "Kernel Linux";

                allFrames.Add(s_a);
            }

            //Y la devolvemos
            return allFrames;
        }

        private DataTable list2DataTable(List<string[]> procs)
        {
            DataTable table = new DataTable();

            //Nombre de las columnas
            string[] columnNames = new string[3];
            int c = 0;
            columnNames[c++] = "Marco";
            columnNames[c++] = "Espacio ocupado";
            columnNames[c] = "Proceso residente";

            //Agrega tantas columnas como campos
            int columns = 3;
            for (int i = 0; i < columns; i++)
                table.Columns.Add(columnNames[i]);

            //Agrega tantas filas como elementos
            foreach (string[] int_a in procs)
                table.Rows.Add(int_a);

            return table;
        }
    }
}
