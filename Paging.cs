using PagingRoundRobin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;

namespace PagingRR

{
    public partial class PagingWindow : Form
    {
        //***CONSTANTES
        //De estado
        private const int ABORT = -1;
        private const int END = 0;
        private const int EXEC = 1;
        private const int NEW = 2;
        private const int READY = 3;
        private const int BLOCKED = 4;
        private const int UNBLOCKED = 5;
        private const int SUSP = 6;
        private const int FLAG = 9; //Multipropósito
        //De proceso nulo
        private const sbyte NULL = -1;
        //Generales
        private const int PAGE_SIZE = 4;            //Tamaño de cada página
        private const int BLOCKING_TIME = 5;        //Intervalo de bloqueo
        private const int ALL_NOT_OS_FRAMES = 42;   //Marcos totales (no reservados para el SO)
        private const int ENOUGH_FRAMES = 2;        //Marcos mínimos suficientes para llamar proc. nuevo 
        private const int MAX_FRAMES_REQUIRED = 7;  //Cantidad máxima de marcos requeridos para el arreglo
        private const int RESET = 0;                //Resetea variables a cero

        //Round Robin
        int round = 0;

        //Suspendidos
        bool huboSusp = false;

        List<int> freeFrames = new List<int>();             //Lista para control de marcos libres
        List<BCP> all_BCPs = new List<BCP>();               //Lista de BCP
        Queue<Proc> raw_procs = new Queue<Proc>();          //Cola de procesos brutos
        Queue<Proc> new_procs = new Queue<Proc>();          //Cola de procesos nuevos
        Queue<Proc> RAM_procs = new Queue<Proc>();          //Cola de procesos en RAM
        Queue<Proc> finished_procs = new Queue<Proc>();     //Cola de procesos terminados
        Queue<Proc> susp_procs = new Queue<Proc>();         //Cola de procesos suspendidos

        public PagingWindow()
        {
            InitializeComponent();
        }

        private void rawProcsPager(int newlyPagedProcs)
        {
            /* Calcula núm. de págs. y tamaño de
             * cada una para todos los procs brutos. */

            for (int i = 0; i < newlyPagedProcs; i++)
            {
                //*** Saca proc bruto
                Proc p = raw_procs.Dequeue();
                BCP bcp = p.BCPfinder(all_BCPs);

                //*** Calcula núm. de págs.
                //Inicializa total de págs. en proc/BCP
                bcp.totalPages = p.size / PAGE_SIZE;

                /* Si la div. del tamaño del proc genera residuo,
                 * entonces existe una última pág. de menor tamaño
                 * que las demás y debemos sumarla a totalPages. */
                int lastPageSize = p.size % PAGE_SIZE;
                if (lastPageSize != 0)
                    bcp.totalPages++;

                /* Si no hay residuo, la última pág. mide igual
                 * que todas y por tanto ya estaba calculada
                 * desde la división de totalPages. */
                else
                    lastPageSize = PAGE_SIZE;

                /* Guarda total de págs. con su debido tamaño dentro de
                 * un arreglo, salvo la última página, por si es residual. */
                int[] pages = new int[MAX_FRAMES_REQUIRED];
                int j;
                for (j = 0; j < bcp.totalPages - 1; j++)
                    pages[j] = PAGE_SIZE;
                //Y carga la última pág.:
                pages[j] = lastPageSize;

                //Inicializa/carga el resutado en proc/BCP
                bcp.pages = pages;
                //*** Proc nuevo a cola de nuevos
                new_procs.Enqueue(p);
            }
        }

        private void freeFramesInit()
        {
            /* Inicia todos los marcos de memoria
             * de paginación como libres. */

            for (int i = 0; i < ALL_NOT_OS_FRAMES; i++)
                freeFrames.Add(i);
        }

        private void freeFramesUpdater(BCP b)
        {
            /* Recupera marcos desocupados por proc
             * finalizado y limpia sus barras. */

            for (int i = 0; i < b.totalPages; i++)
            {
                progressBarSelection("**",
                    b.frames[i], 0, 1);
                freeFrames.Add(b.frames[i]);
            }
        }

        private void init(int newBornProcs)
        {
            //CREA TODOS LOS PROCESOS
            //Instancia random
            Random ran = new Random();
            //Por cada nuevo proc solicitado...
            for (int i = 0; i < newBornProcs; i++)
            {
                /* Construye proc con el constructor
                 * parametrizado de random. */
                Proc p = new Proc(ran, SO.IDcount++);
                //Por cada proc un BCP
                BCP bcp = new BCP();
                //Relaciona proc a BCP usando ID
                bcp.PID = p.PID;
                bcp.state = NEW;
                // Almacena proc y BCP en sendas listas
                raw_procs.Enqueue(p);
                all_BCPs.Add(bcp);
            }
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            if (textBox_procsAmount.Text == ""
               || textBox_Quantum.Text == "")
            {
                MessageBox.Show(
                    "Ni los procesos ni la ráfaga pueden dejarse en blanco.");
                return;
            }

            if (textBox_procsAmount.Text == "0"
                || textBox_Quantum.Text == "0")
            {
                MessageBox.Show(
                    "Ni los procesos ni la ráfaga pueden ser 0.");
                return;
            }

            //Bloquea entradas y botones
            button_start.Enabled = false;
            textBox_procsAmount.Enabled = false;
            textBox_Quantum.Enabled = false;

            //Recupera datos del GUI
            SO.TotalProcs = int.Parse(textBox_procsAmount.Text);
            SO.Quantum = int.Parse(textBox_Quantum.Text);

            //Inicia la creación de procs
            init(SO.TotalProcs);
            //Calcula paginado de todos los procs nuevos
            rawProcsPager(SO.TotalProcs);

            //total de procs nuevos (todos, en primera ejecución)
            SO.ScreenNewProcs = SO.TotalProcs;

            //Marca todos los marcos como libres
            freeFramesInit();

            //Inicia el timer para los ciclos
            t.Start();
        }

        private void t_Tick(object sender, EventArgs e)
        {
            mainProcessor();
        }

        private void mainProcessor()
        {
            //GESTIONA PROCS BLOQUEADOS
            blockedProcsManager();

            //Si hay al menos algún proc
            if (SO.TotalProcs > 0)
            {
                //ALIMENTA PROCS A RAM
                RAMprocsFeeder();

                //SOLICITA SIG. PROC A EJECUTAR
                nextExecProcRequester();

                //EJECUTA PROC CORRIENTE
                procExecutor();

                //DESPLIEGA LOS PROCS
                screenPrinter();

                //ALGORITMO ROUND ROBIN
                roundRobin();

                //REFRESCA LAS BARRAS
                barsRefresher();

                SO.TotalElapsedTime++;

                //DESPLIEGA MENSAJES DE EVENTOS
                displayedMsgLittleHelper();
            }

            else //si ya no quedan procs
            {
                //CIERRA TODO
                mightyEnder();
            }
        }

        private void blockedProcsManager()
        {
            /* Cuenta tiempo de bloqueo de un proc
             * y lo habilita una vez transcurrido. */

            foreach (Proc p in RAM_procs)
            {
                BCP bcp = p.BCPfinder(all_BCPs);

                //Si es proc bloqueado, incrementa en uno
                if (bcp.state == BLOCKED)
                {
                    bcp.blocked++;
                    /* y desbloquea al alcanzar el límite
                     * de ciclos de BLOCKING_TIME.*/
                    if (bcp.blocked >= BLOCKING_TIME)
                    {
                        /* Lo marca como desbloqueado para
                         * evitar que procRetreiver recalcule
                         * su tiempo de respuesta. */
                        bcp.state = UNBLOCKED;
                        bcp.blocked = RESET; //Resetea tiempo de bloqueo
                        //Retira proc de candidatura a suspención
                        SO.currentBlockedPID = RESET;
                    }

                    else //Si aún no se ha desbloqueado
                    {
                        //... es candidato a suspensión (de requerirlo)
                        SO.currentBlockedPID = bcp.PID;
                    }
                }
            }

            //Si existe un proceso bloqueado... (un PID nunca es 0)
            if (SO.currentBlockedPID != 0)
            {
                //... y se solicitó suspensión de procs bloqueados
                if (SO.SuspRequest)
                {
                    SO.SuspRequest = false; //Baja bandera
                    incredibleProcSuspender();
                }
            }
        }

        private void RAMprocsFeeder()
        {
            /* Mete new procs a RAM mientras haya marcos. */

            //¿Hay espacio en RAM?
            //Mientras haya al menos 2 marcos (tam. mín. de proc)
            while (freeFrames.Count >= ENOUGH_FRAMES)
            {
                if (new_procs.Any()) //¿Hay procesos nuevos?
                {
                    Proc p = new_procs.Peek(); //Lo fisgoneamos, sin sacarlo
                    BCP bcp = p.BCPfinder(all_BCPs);

                    if (bcp.totalPages <= freeFrames.Count) //¿Cabe el proceso en RAM?
                    {
                        if (SO.Waiting) //Si el proc estaba esperando marcos libres suficientes...
                        {
                            label_genericNotifications.Text = "¡El proceso en espera ha entrado!";
                            SO.Waiting = false;
                        }
                        //Si es un proc desuspendido...
                        if (bcp.state == SUSP)
                        {
                            //Un proc. susp. menos a GUI
                            SO.ScreenSuspProcs--;
                            awesomeSuspProcsWriter();
                        }
                        else
                        {
                            //TIEMPO LLEGADA
                            bcp.arrival = SO.TotalElapsedTime;
                            //Un proceso nuevo menos en GUI
                            SO.ScreenNewProcs--;
                        }
                        //Estado
                        bcp.state = READY;
                        //Lo pasamos de nuevos a RAM (listos)
                        RAM_procs.Enqueue(new_procs.Dequeue());

                        freeFrames.Sort(); //Acomoda los marcos en orden ascendente

                        //Almacena núm. de marco para cada pág. en arreglo temporal
                        int[] frames = new int[MAX_FRAMES_REQUIRED];
                        for (int i = 0; i < bcp.totalPages; i++)
                        {
                            frames[i] = freeFrames[0];
                            freeFrames.RemoveAt(0);
                        }

                        //y luego lo carga en proc/BCP
                        bcp.frames = frames;
                    }

                    else
                    {
                        string unsusp = " ";
                        if (bcp.state == SUSP)
                            unsusp += "reactivado ";
                        SO.Waiting = true;
                        label_genericNotifications.Text =
                            "Proceso " + bcp.PID + unsusp + "de " + bcp.totalPages +
                            " páginas esperando espacio libre en RAM...";
                        break;
                    }
                }

                else
                {
                    if (susp_procs.Any())
                    {
                        huboSusp = true;
                        if (RAM_procs.Any())
                            label_genericNotifications.Text =
                            "Todos los procesos en RAM y algunos suspendidos...";
                        else
                            label_genericNotifications.Text =
                                "Solo restan procesos suspendidos. Esperando a que se reactiven...";
                    }
                    else if(huboSusp)
                    {
                        label_genericNotifications.Text =
                            "Ya no quedan procesos, ¡todos en RAM!";
                    }
                    else
                    {
                        label_genericNotifications.Text =
                            "Ya no quedan procesos nuevos, ¡todos en RAM!";
                    }
                    break;
                }
            }

            if (freeFrames.Count < ENOUGH_FRAMES)
            {
                //Si no hay marcos pero sí procs
                if (new_procs.Any())
                    label_genericNotifications.Text =
                        "No hay marcos suficientes para solicitar nuevos procesos. Espera, por favor.";
                //Si no hay marcos ni procs
                else
                    label_genericNotifications.Text =
                        "Ya no quedan marcos, pero tampoco procesos nuevos, ¡todos en RAM!";
            }
        }

        private void nextExecProcRequester()
        {
            /* Si se solicita nuevo proc
             * llama a procRetreiver. */

            if (SO.NextProcRequest)
                procRetreiver();
        }

        /* Subrutina que valida si un proc
         * puede recuperarse de la RAM. */
        private void procRetreiver()
        {
            /* Gets a proc and sets its correct
             * BCP values whether it's its
             * first execution lap or not. */

            //Si no hay procs, regresa
            if (!RAM_procs.Any()) return;

            /* No se saca de RAM, se copia,
             * pues debe imprimirse en pantalla
             * y esto se hace recorriendo toda
             * la cola de procs en RAM. */
            foreach (Proc p in RAM_procs)
            {
                SO.ExecProc = p;
                SO.ExecProcBCP = p.BCPfinder(all_BCPs);
                //Sale cuando halla un proc no bloqueado
                if (SO.ExecProcBCP.state != BLOCKED)
                    break;
            }
            /* Si alcanza esta parte con proc bloqueado,
             * significa que todos los procs lo están,
             * limpia el proc corriente y regresa. */
            if (SO.ExecProcBCP.state == BLOCKED)
            {
                execProcNullifier();
                return;
            }

            /* Calcula tiempo de respuesta solo la 
             * primera vez, cuando el estado es READY.
             * Procs reingresados a RAM por
             * desbloqueo (UNBLOCKED) o ciclo
             * Round Robin (FLAG) son ignorados. */
                if (SO.ExecProcBCP.state == READY)
                //TIEMPO RESPUESTA
                SO.ExecProcBCP.response =
                    SO.TotalElapsedTime - SO.ExecProcBCP.arrival;

            /* Marca proceso como en ejecución y
             * baja la bandera de solicitud de proc*/
            SO.ExecProcBCP.state = EXEC;
            SO.NextProcRequest = false;
            /* Siempre que se saca nuevo proc su
             * ciclo es necesariamente nuevo. */
            round = RESET;
        }

        //Subrutina de actualización de proc corriente
        private void execProcNullifier()
        {
            /* Sobreescribe SO.ExecProc con el proc nulo.
             * Esta actualización es importante para que
             * el proc en ejecución coincida con el estado
             * actual del programa y así se evitan gran
             * cantidad de errores.
             * 
             * Entre otras cosas, impide que los eventos
             * (interrupción, error...) tengan efecto
             * (se validan contra proc nulo).
             * También obliga a mainProcessor a ciclarse
             * con proc nulo mientras haya suspendidos,
             * (SO.TotalProcs > 0), sin dejar de aumentar
             * el contador. */

            SO.ExecProc = SO.NullProc;
            SO.ExecProcBCP = SO.NullBCP;
        }

        private void procExecutor()
        {
            /* Si está corriendo el proc nulo,
             * intenta solicitar nuevo proc. */
            if (SO.ExecProc.PID == NULL)
            {
                procRetreiver();
                //Si falló en sacar un nuevo proc, regresa
                if (SO.NextProcRequest)
                    return;
            }

            //TIEMPO DE SERVICIO
            SO.ExecProcBCP.exec++;
            //Guarda proc corriente para imprimirlo
            proc2SOstrings(SO.ExecProc);

            //Si el proc cumplió su ciclo o hubo error, se le mata
            if (SO.ExecProcBCP.exec >= SO.ExecProc.TME
                || SO.ExecProcBCP.state == ABORT)
                procKiller();
        }

        //Subrutina de procExecutor
        private void procKiller()
        {
            /* Se encarga de actualizar todos los
             * parámetros cuando un proceso finaliza,
             * ya sea terminado (normal) o abortado
             * (error).*/

            SO.ScreenDoneProcs++;      //Un proc terminado más a GUI
            SO.TotalProcs--;           //Un proceso total restante menos
            SO.NextProcRequest = true; //Vamos a necesitar otro proc!

            //Si el proceso terminó sin errores
            if (SO.ExecProcBCP.state != ABORT)
                SO.ExecProcBCP.state = END;

            //TIEMPO FINAL

            /* incrementa uno porque SO.totalElapsedTime
             * se actualiza más adelante en otra función. */
            SO.ExecProcBCP.end = SO.TotalElapsedTime + 1;
            //TIEMPO RETORNO
            SO.ExecProcBCP.returning =
                SO.ExecProcBCP.end - SO.ExecProcBCP.arrival;
            //TIEMPO ESPERA
            SO.ExecProcBCP.idle =
                SO.ExecProcBCP.returning - SO.ExecProcBCP.exec;

            //Reutiliza y limpia marcos desocupados
            freeFramesUpdater(SO.ExecProcBCP);
            //Lo cambia de RAM a terminados
            if (RAM_procs.Any())
                finished_procs.Enqueue(RAM_procs.Dequeue());
            //También su ciclo RR ha terminado
            round = RESET;
            //Actualiza proc corriente (ninguno/nulo)
            execProcNullifier();
        }

        private void screenPrinter()
        {
            /* Toda la información a pantalla GUI. */

            //Tiempo total transcurrido
            label_totalElapsedTime.Text =
                "Tiempo total: " + SO.TotalElapsedTime.ToString();
            //Procesos nuevos restantes
            label_newProcs.Text =
                "Procesos nuevos: " + SO.ScreenNewProcs.ToString();
            //Procesos suspendidos
            label_suspProcs.Text =
                "Procs. suspendidos: " + SO.ScreenSuspProcs.ToString();
            //Procesos terminados
            label_procsDone.Text =
                "Procs. terminados: " + SO.ScreenDoneProcs.ToString();
            //Ráfaga
            label_rafaga.Text =
                "Ráfaga: " + SO.Quantum.ToString();

            //EN EJECUCIÓN

            /* Si es proc nulo y no hay nada en RAM y quedan procs.
             * Este nos sirve para cuando tenemos suspendidos. */
            if (SO.ExecProc.PID == NULL && !RAM_procs.Any()
                && SO.TotalProcs > 0)
            {
                richTextBox_exec.Rtf =
                    @"{\rtf1\ansi \b Proceso corriente: \b0\ }";
                richTextBox_exec.AppendText(
                    "\nID:\t\t" + "NULO"
                    + "\nOperación:\t" + "N/A"
                    + "\nTME:\t\t" + "N/A"
                    + "\nTT:\t\t" + "N/A"
                    + "\nTiempo Restante:\t" + "N/A"
                    + "\nLlegada:\t\t" + "N/A"
                    + "\nRespuesta:\t" + "N/A"
                    + "\nEstado:\t\t" + "N/A");
            }

            else
            {
                int remainingTime =
                int.Parse(SO.TME) - int.Parse(SO.Exec);
                string remainingT = remainingTime.ToString();

                richTextBox_exec.Rtf =
                    @"{\rtf1\ansi \b Proceso corriente: \b0\ }";
                richTextBox_exec.AppendText(
                    "\nID:\t\t" + SO.ID + "\nOperación:\t"
                    + SO.Oper1 + " " + SO.Symbol + " " + SO.Oper2
                    + "\nTME:\t\t" + SO.TME
                    + "\nTT:\t\t" + SO.Exec
                    + "\nTiempo Restante:\t" + remainingT
                    + "\nLlegada:\t\t" + SO.Arrival
                    + "\nRespuesta:\t" + SO.Response
                    + "\nEstado:\t\t" + SO.State);
            }

            /* Si el último proc en RAM está bloqueado
             * y el proc corriente es nulo*/
            BCP bcp = new BCP();
            if (RAM_procs.Any())
                bcp = RAM_procs.Peek().BCPfinder(all_BCPs);
            if (bcp.state == BLOCKED && SO.ExecProc.PID == NULL)
            {
                richTextBox_exec.Rtf =
                    @"{\rtf1\ansi \b Proceso corriente: \b0\ }";
                richTextBox_exec.AppendText(
                    "\nID:\t\t" + "NULO"
                    + "\nOperación:\t" + "N/A"
                    + "\nTME:\t\t" + "N/A"
                    + "\nTT:\t\t" + "N/A"
                    + "\nTiempo Restante:\t" + "N/A"
                    + "\nLlegada:\t\t" + "N/A"
                    + "\nRespuesta:\t" + "N/A"
                    + "\nEstado:\t\t" + "N/A");
            }

            //EN RAM
            foreach (BCP b in all_BCPs)
            {
                int progress;
                sbyte color;

                if (b.state == EXEC
                    || b.state == READY
                    || b.state == BLOCKED
                    || b.state == UNBLOCKED
                    || b.state == FLAG)
                {
                    for (int i = 0; i < b.totalPages; i++)
                    {
                        if (b.state == EXEC) color = 2; //2 Rojo
                        else if (b.state == BLOCKED) color = 3; //3 Amarillo
                        else color = 1; //Solo el 1 las vuelve a pintar verdes

                        progress = b.pages[i] * 25;
                        progressBarSelection(
                            b.PID.ToString(), b.frames[i], progress, color);
                    }
                }
            }

            //NUEVOS
            string newStr = "\n";
            foreach (Proc p in new_procs)
            {
                proc2SOstrings(p);
                newStr += SO.ID + "\t" + SO.TME + "\t"
                       + SO.Oper1 + SO.Symbol + SO.Oper2 + "\n";
            }
            richTextBox_new.Rtf =
                @"{\rtf\ansi \b ID:       TME:   Operación:\b0\ }";
            richTextBox_new.AppendText(newStr);

            //TERMINADOS
            string finishedStr = "\n";
            foreach (Proc p in finished_procs)
            {
                proc2SOstrings(p);
                //Si el proceso fue abortado
                if (SO.State == ABORT) SO.Result = "ERROR";
                finishedStr += SO.ID + "      " + SO.Oper1
                            + " " + SO.Symbol + " " + SO.Oper2
                            + "\t" + "= " + SO.Result + "\n";
            }
            richTextBox_done.Rtf =
                @"{\rtf1\ansi \b ID:   Operación:  Resultado: \b0\ }";
            richTextBox_done.AppendText(finishedStr);
        }

        //Subrutina de screenPrinter
        private void progressBarSelection(
            string ID, int frame, int pageSize, sbyte color)
        {
            switch (frame)
            {
                case 0:
                    _00.Text = ID;
                    pb00.Value = pageSize;
                    progressBarColour.SetState(pb00, color);
                    break;
                case 1:
                    _01.Text = ID;
                    pb01.Value = pageSize;
                    progressBarColour.SetState(pb01, color);
                    break;
                case 2:
                    _02.Text = ID;
                    pb02.Value = pageSize;
                    progressBarColour.SetState(pb02, color);
                    break;
                case 3:
                    _03.Text = ID;
                    pb03.Value = pageSize;
                    progressBarColour.SetState(pb03, color);
                    break;
                case 4:
                    _04.Text = ID;
                    pb04.Value = pageSize;
                    progressBarColour.SetState(pb04, color);
                    break;
                case 5:
                    _05.Text = ID;
                    pb05.Value = pageSize;
                    progressBarColour.SetState(pb05, color);
                    break;
                case 6:
                    _06.Text = ID;
                    pb06.Value = pageSize;
                    progressBarColour.SetState(pb06, color);
                    break;
                case 7:
                    _07.Text = ID;
                    pb07.Value = pageSize;
                    progressBarColour.SetState(pb07, color);
                    break;
                case 8:
                    _08.Text = ID;
                    pb08.Value = pageSize;
                    progressBarColour.SetState(pb08, color);
                    break;
                case 9:
                    _09.Text = ID;
                    pb09.Value = pageSize;
                    progressBarColour.SetState(pb09, color);
                    break;
                case 10:
                    _10.Text = ID;
                    pb10.Value = pageSize;
                    progressBarColour.SetState(pb10, color);
                    break;
                case 11:
                    _11.Text = ID;
                    pb11.Value = pageSize;
                    progressBarColour.SetState(pb11, color);
                    break;
                case 12:
                    _12.Text = ID;
                    pb12.Value = pageSize;
                    progressBarColour.SetState(pb12, color);
                    break;
                case 13:
                    _13.Text = ID;
                    pb13.Value = pageSize;
                    progressBarColour.SetState(pb13, color);
                    break;
                case 14:
                    _14.Text = ID;
                    pb14.Value = pageSize;
                    progressBarColour.SetState(pb14, color);
                    break;
                case 15:
                    _15.Text = ID;
                    pb15.Value = pageSize;
                    progressBarColour.SetState(pb15, color);
                    break;
                case 16:
                    _16.Text = ID;
                    pb16.Value = pageSize;
                    progressBarColour.SetState(pb16, color);
                    break;
                case 17:
                    _17.Text = ID;
                    pb17.Value = pageSize;
                    progressBarColour.SetState(pb17, color);
                    break;
                case 18:
                    _18.Text = ID;
                    pb18.Value = pageSize;
                    progressBarColour.SetState(pb18, color);
                    break;
                case 19:
                    _19.Text = ID;
                    pb19.Value = pageSize;
                    progressBarColour.SetState(pb19, color);
                    break;
                case 20:
                    _20.Text = ID;
                    pb20.Value = pageSize;
                    progressBarColour.SetState(pb20, color);
                    break;
                case 21:
                    _21.Text = ID;
                    pb21.Value = pageSize;
                    progressBarColour.SetState(pb21, color);
                    break;
                case 22:
                    _22.Text = ID;
                    pb22.Value = pageSize;
                    progressBarColour.SetState(pb22, color);
                    break;
                case 23:
                    _23.Text = ID;
                    pb23.Value = pageSize;
                    progressBarColour.SetState(pb23, color);
                    break;
                case 24:
                    _24.Text = ID;
                    pb24.Value = pageSize;
                    progressBarColour.SetState(pb24, color);
                    break;
                case 25:
                    _25.Text = ID;
                    pb25.Value = pageSize;
                    progressBarColour.SetState(pb25, color);
                    break;
                case 26:
                    _26.Text = ID;
                    pb26.Value = pageSize;
                    progressBarColour.SetState(pb26, color);
                    break;
                case 27:
                    _27.Text = ID;
                    pb27.Value = pageSize;
                    progressBarColour.SetState(pb27, color);
                    break;
                case 28:
                    _28.Text = ID;
                    pb28.Value = pageSize;
                    progressBarColour.SetState(pb28, color);
                    break;
                case 29:
                    _29.Text = ID;
                    pb29.Value = pageSize;
                    progressBarColour.SetState(pb29, color);
                    break;
                case 30:
                    _30.Text = ID;
                    pb30.Value = pageSize;
                    progressBarColour.SetState(pb30, color);
                    break;
                case 31:
                    _31.Text = ID;
                    pb31.Value = pageSize;
                    progressBarColour.SetState(pb31, color);
                    break;
                case 32:
                    _32.Text = ID;
                    pb32.Value = pageSize;
                    progressBarColour.SetState(pb32, color);
                    break;
                case 33:
                    _33.Text = ID;
                    pb33.Value = pageSize;
                    progressBarColour.SetState(pb33, color);
                    break;
                case 34:
                    _34.Text = ID;
                    pb34.Value = pageSize;
                    progressBarColour.SetState(pb34, color);
                    break;
                case 35:
                    _35.Text = ID;
                    pb35.Value = pageSize;
                    progressBarColour.SetState(pb35, color);
                    break;
                case 36:
                    _36.Text = ID;
                    pb36.Value = pageSize;
                    progressBarColour.SetState(pb36, color);
                    break;
                case 37:
                    _37.Text = ID;
                    pb37.Value = pageSize;
                    progressBarColour.SetState(pb37, color);
                    break;
                case 38:
                    _38.Text = ID;
                    pb38.Value = pageSize;
                    progressBarColour.SetState(pb38, color);
                    break;
                case 39:
                    _39.Text = ID;
                    pb39.Value = pageSize;
                    progressBarColour.SetState(pb39, color);
                    break;
                case 40:
                    _40.Text = ID;
                    pb40.Value = pageSize;
                    progressBarColour.SetState(pb40, color);
                    break;
                case 41:
                    _41.Text = ID;
                    pb41.Value = pageSize;
                    progressBarColour.SetState(pb41, color);
                    break;
                case 42:
                    _42.Text = ID;
                    pb42.Value = pageSize;
                    progressBarColour.SetState(pb42, color);
                    break;
                case 43:
                    _43.Text = ID;
                    pb43.Value = pageSize;
                    progressBarColour.SetState(pb43, color);
                    break;
                case 44:
                    _44.Text = ID;
                    pb44.Value = pageSize;
                    progressBarColour.SetState(pb44, color);
                    break;
                default:
                    break;
            }
        }

        private void roundRobin()
        {
            /* Algoritmo de planificación
             * de rondas Round Robin. */

            /* Si el ciclo RR está completado
             * y hay procs en RAM (porque
             * vamos a desencolar) */
            if (++round == SO.Quantum && RAM_procs.Any())
            {
                /* Lo marca como flagueado para evitar
                 * que procRetreiver recalcule su
                 * tiempo de respuesta. */
                SO.ExecProcBCP.state = FLAG;
                //Encola al final al proc cuya ráfaga terminó
                RAM_procs.Enqueue(RAM_procs.Dequeue());
                //y continúa con el siguiente formado
                procRetreiver();
            }
        }

        private void barsRefresher()
        {
            /* Cada ciclo timer carga los
             * nuevos valores de las variables,
             * pero a veces esto no implica
             * refrescarlos en pantalla. Esta
             * función se asegura de que las
             * barras estén actualizadas al 
             * final de cada ciclo. */

            lbl_kernel1.Text = "Kernel";
            lbl_kernel2.Text = "Linux";
            lbl_kernel3.Text = "5.11.13";
            pb00.Refresh();
            pb01.Refresh();
            pb02.Refresh();
            pb03.Refresh();
            pb04.Refresh();
            pb05.Refresh();
            pb06.Refresh();
            pb07.Refresh();
            pb08.Refresh();
            pb09.Refresh();
            pb10.Refresh();
            pb11.Refresh();
            pb12.Refresh();
            pb13.Refresh();
            pb14.Refresh();
            pb15.Refresh();
            pb16.Refresh();
            pb17.Refresh();
            pb18.Refresh();
            pb19.Refresh();
            pb20.Refresh();
            pb21.Refresh();
            pb22.Refresh();
            pb23.Refresh();
            pb24.Refresh();
            pb25.Refresh();
            pb26.Refresh();
            pb27.Refresh();
            pb28.Refresh();
            pb29.Refresh();
            pb30.Refresh();
            pb31.Refresh();
            pb32.Refresh();
            pb33.Refresh();
            pb34.Refresh();
            pb35.Refresh();
            pb36.Refresh();
            pb37.Refresh();
            pb38.Refresh();
            pb39.Refresh();
            pb40.Refresh();
            pb41.Refresh();
            pb42.Refresh();
            pb43.Refresh();
            pb44.Refresh();
        }

        private void displayedMsgLittleHelper()
        {
            if (SO.showingMsg) //Si hay mensaje de evento...
                //... y ya se mostró por el tiempo de una ráfaga
                if (SO.DisplayedMsgCountdown++ == SO.Quantum)
                {
                    label_pressedKey.Text = "";
                    SO.DisplayedMsgCountdown = RESET;
                    SO.showingMsg = false;
                }
        }

        private void mightyEnder()
        {
            /* Mighty Ender: "I can finish anything!".
             * Se encarga de la limpieza y cierre
             * del programa. */

            //Para el timer
            t.Stop();

            //Se imprime una última vez Tiempo total transcurrido
            label_totalElapsedTime.Text =
                "Tiempo total: " + SO.TotalElapsedTime.ToString();

            //Borra etiquetas innecesarias
            label_genericNotifications.Text = "";

            //Actualiza etiqueta de entrada de eventos de teclado
            label_pressedKey.Text = "PROGRAMA FINALIZADO.";

            //Vacía la caja del proc en ejecución
            richTextBox_exec.Rtf =
                @"{\rtf\ansi \b Tiempo de respuesta:  0\b0\ }";
            richTextBox_exec.AppendText(
                "\n\nID:\t\tvacío.\nOperación:\tvacío." +
                "\nTME:\t\tvacío.\nTT:\t\tvacío.\nTR:\t\tvacío.");

            //Llama a la tabla BCP
            TablaBCP t_bcp = new TablaBCP(
                RAM_procs, new_procs, susp_procs, finished_procs, all_BCPs);
            t_bcp.ShowDialog();
        }

        private void proc2SOstrings(Proc p)
        {
            /* Convierte proc y su BCP a
             * palabras y los guarda en SO. */
            BCP bcp = p.BCPfinder(all_BCPs);

            SO.ID = p.PID.ToString();
            SO.TME = p.TME.ToString();
            SO.Oper1 = p.oper1.ToString();
            SO.Oper2 = p.oper2.ToString();
            SO.Result = p.result.ToString("0.##");
            SO.Symbol = p.symbol;

            SO.State = bcp.state;
            SO.Arrival = bcp.arrival.ToString();
            SO.End = bcp.end.ToString();
            SO.Response = bcp.response.ToString();
            SO.Exec = bcp.exec.ToString();
            SO.Idle = bcp.idle.ToString();
            SO.Returning = bcp.returning.ToString();
            SO.Blocked = bcp.blocked.ToString();
            SO.Suspended = bcp.suspended.ToString();
        }

        private void incredibleProcSuspender()
        {
            /* Circula por toda la cola de procs en RAM en busca
             * del proc que coincide con el ID del lastBlockedPID.
             * Como desencola toda la cola, al finalizar, todos
             * los procs quedan en el mismo orden inicial. */

            //Guardamos tamaño de RAM
            int ramProcsSize = RAM_procs.Count();
            for (int i = 0; i < ramProcsSize; i++)
            {
                /* Limpia proc corriente para sacarlo
                 * con seguridad de la RAM.*/
                execProcNullifier();

                /* Este Dequeue está validado.
                 * IncredibleProcSuspender solo
                 * es llamado cuando existe algún
                 * proc bloqueado (en RAM). */
                Proc p = RAM_procs.Dequeue();
                BCP b = p.BCPfinder(all_BCPs);
                if (b.PID == SO.currentBlockedPID)
                {
                    //Flaguea como susp
                    b.state = SUSP;
                    //Resetea tiempo de bloqueo
                    b.blocked = RESET;
                    //lo borra de las barras
                    freeFramesUpdater(b);
                    //Manda a cola de sups
                    susp_procs.Enqueue(p);
                    //Escribe susps en memoria secundaria
                    awesomeSuspProcsWriter();
                    //Un proc. susp. más a GUI
                    SO.ScreenSuspProcs++;
                }
                //Si no, a reencolarlo
                else
                {
                    RAM_procs.Enqueue(p);
                }
            }
        }

        //Subrutina de incredibleProcSuspender
        private async void awesomeSuspProcsWriter()
        {
            string all_susp = "";
            foreach (Proc p in susp_procs)
            {
                BCP b = p.BCPfinder(all_BCPs);

                //Calcula tiempo de espera
                int idle = SO.TotalElapsedTime - (int)(b.arrival + b.exec);
                idle--;

                string susp = "PID: " + p.PID.ToString() + " TME: " + p.TME.ToString()
               + " Operación: " + p.oper1.ToString() + " " + p.symbol + " " + p.oper2.ToString() + " = " + p.result.ToString("0.##")
               + " Estado: " + b.state + " Llegada: " + b.arrival.ToString() + " Finalización: " + b.end.ToString()
               + " Respuesta: " + b.response.ToString() + " Servicio: " + b.exec.ToString() + " Espera: " + idle.ToString()
               + " Regreso: " + b.returning.ToString() + " Tiempo de bloqueo: " + b.blocked.ToString() + "\n";
                all_susp += susp;
            }
            await File.WriteAllTextAsync("C:\\Users\\Álex\\Desktop\\ProcsEmulatorAlpha3\\suspended.txt", all_susp);
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            string ch = e.KeyChar.ToString().ToUpper();

            if (ch == "P")
            {
                if (SO.Paused) return;
                label_pressedKey.Text = "PROGRAMA PAUSADO";
                SO.showingMsg = true; //Mostrando mensaje
                SO.Paused = true;
                t.Stop();
            }

            else if (ch == "C")
            {
                if (!SO.Paused) return;
                label_pressedKey.Text = "EJECUCIÓN REANUDADA";
                SO.showingMsg = true;
                SO.Paused = false;
                t.Start();
            }

            else if (ch == "E")
            {
                //No trabaja con prog pausado
                if (SO.Paused) return;
                //No trabaja sobre proc nulo
                if (SO.ExecProc.PID == NULL) return;
                t.Stop();
                SO.ExecProcBCP.state = ABORT;
                label_pressedKey.Text = "HAN OCURRIDO ERRORES";
                SO.showingMsg = true;
                t.Start();
            }

            else if (ch == "I")
            {
                //Validaciones
                if (SO.Paused) return;
                if (SO.ExecProc.PID == NULL) return;

                //No trabaja si ya se solicitó bloqueo
                if (SO.Interrupted) return;
                else SO.Interrupted = true;

                //Si hay procs listos...
                if (RAM_procs.Any())
                {
                    t.Stop();
                    SO.NextProcRequest = true;
                    SO.ExecProcBCP.state = BLOCKED;
                    //Lo mete al final de la cola
                    RAM_procs.Enqueue(RAM_procs.Dequeue());
                    //Si hay solicitud de suspender
                    if (SO.SuspRequest)
                        label_pressedKey.Text = "PROC. INTERRUMPIDO Y SUSPENDIDO";
                    else
                        label_pressedKey.Text = "PROCESO INTERRUMPIDO";
                    SO.showingMsg = true;
                    SO.Interrupted = false;
                    t.Start();
                }
            }

            else if (ch == "N")
            {
                if (SO.Paused) return;
                init(1);
                rawProcsPager(1);
                SO.TotalProcs++;
                SO.ScreenNewProcs++;
                label_pressedKey.Text = "¡NUEVO PROCESO CREADO!";
                SO.showingMsg = true;
            }

            else if (ch == "B")
            {
                t.Stop();
                label_pressedKey.Text = "TABLA BCP";
                SO.showingMsg = true;
                SO.Paused = true;
                TablaBCP t_bcp = new TablaBCP(
                    RAM_procs, new_procs, susp_procs, finished_procs, all_BCPs);
                t_bcp.ShowDialog();
            }

            else if (ch == "A")
            {
                t.Stop();
                label_pressedKey.Text =
                    "TABLA PROCESOS PAGINADOS";
                SO.showingMsg = true;
                SO.Paused = true;
                TablaPagedProcs t_pagedP =
                    new TablaPagedProcs(RAM_procs, all_BCPs);
                t_pagedP.ShowDialog();
            }

            else if (ch == "T")
            {
                t.Stop();
                label_pressedKey.Text =
                    "TABLA DE PAGINACIÓN COMPLETA";
                SO.showingMsg = true;
                SO.Paused = true;
                TablaPaging t_pag =
                    new TablaPaging(all_BCPs);
                t_pag.ShowDialog();
            }

            else if (ch == "S")
            {
                if (SO.Paused) return;
                SO.SuspRequest = true;
                //Si hay procs bloqueados
                if (SO.currentBlockedPID != 0)
                    label_pressedKey.Text =
                        "PROCESO SUSPENDIDO";
                else
                    label_pressedKey.Text =
                        "SOLICITUD PARA SUSPENDER PROCESO";
                SO.showingMsg = true;
            }

            else if (ch == "R")
            {
                if (SO.Paused) return;
                if (SO.Recovered) return;
                else SO.Recovered = true;
                //Si hay suspendidos
                if (susp_procs.Any())
                {
                    new_procs.Enqueue(susp_procs.Dequeue());
                    for (int i = 0; i < new_procs.Count() - 1; i++)
                        new_procs.Enqueue(new_procs.Dequeue());
                    label_pressedKey.Text =
                       "¡PROCESO REACTIVADO!";
                }
                else
                {
                    label_pressedKey.Text =
                        "NO HAY PROCESOS SUSPENDIDOS";
                }
                SO.showingMsg = true;
                SO.Recovered = false;
            }
        }
    }

    //progressBarColour
    public static class progressBarColour
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr w, IntPtr l);
        public static void SetState(this ProgressBar pBar, int state)
        {
            SendMessage(pBar.Handle, 1040, (IntPtr)state, IntPtr.Zero);
        }
    }
}