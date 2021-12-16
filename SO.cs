namespace PagingRR
{
    class SO
    {
        //Esta clase engloba las variables globales.
        private const sbyte NULL = -1;
        //Globales
        public static int IDcount { get; set; }
        public static int TotalProcs { get; set; }
        public static int TotalElapsedTime { get; set; }
        public static int ScreenDoneProcs { get; set; }
        public static int ScreenNewProcs { get; set; }
        public static int ScreenSuspProcs { get; set; }
        public static bool NextProcRequest { get; set; }
        public static bool Paused { get; set; }
        public static bool Interrupted { get; set; }
        public static bool Recovered { get; set; }
        //Ráfaga
        public static int Quantum { get; set; }
        //Paginación y suspención
        public static int currentBlockedPID { get; set; }
        public static bool Waiting { get; set; }
        public static bool SuspRequest { get; set; }
        //Etiquetas de eventos
        public static int DisplayedMsgCountdown { get; set; }
        public static bool showingMsg { get; set; }

        //De procesos
        public static Proc ExecProc { get; set; }
        public static Proc NullProc { get; set; }
        public static string ID { get; set; }
        public static string TME { get; set; }
        public static string Oper1 { get; set; }
        public static string Oper2 { get; set; }
        public static string Result { get; set; }
        public static string Symbol { get; set; }

        //De BCP
        public static BCP ExecProcBCP { get; set; }
        public static BCP NullBCP { get; set; }
        public static int State { get; set; }
        public static string Arrival { get; set; }
        public static string End { get; set; }
        public static string Response { get; set; }
        public static string Exec { get; set; }
        public static string Idle { get; set; }
        public static string Returning { get; set; }
        public static string Blocked { get; set; }
        public static string Suspended { get; set; }

        static SO() //Estático porque los valores no son triviales (bool en verdadero, int en 1, etc.).
        {
            /*NOTA: ExecProc y ExecProcBCP son referencias
             * a algún proc. dentro de una lista dada,
             * por lo que no necesitan instanciarse con
             * new, como sí lo necesitan los proc/BCP nulos. */
            NullProc = new Proc(NULL);
            NullBCP = new BCP(NULL);

            IDcount = 1;
            TotalProcs = 0;
            TotalElapsedTime = 0;
            ScreenDoneProcs = 0;
            ScreenNewProcs = 0;
            ScreenSuspProcs = 0;
            NextProcRequest = true;
            Paused = false;
            Interrupted = false;
            Recovered = false;
            //Ráfaga
            Quantum = 0;
            //Paginación y suspención
            currentBlockedPID = 0;
            Waiting = false;
            SuspRequest = false;
            //Etiquetas de eventos
            DisplayedMsgCountdown = 0;
            showingMsg = false;
            //De procesos
            ID = "";
            TME = "";
            Oper1 = "";
            Oper2 = "";
            Result = "";
            Symbol = "";
            //De BCP
            State = -1;
            Arrival = "";
            End = "";
            Response = "";
            Exec = "";
            Idle = "";
            Returning = "";
            Blocked = "";
            Suspended = "";
        }
    }
}
