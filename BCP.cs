namespace PagingRR
{
    public class BCP
    {
        //Identificador
        public int PID { get; set; }
        public int state { get; set; }
        //Variables de tiempo
        public double arrival { get; set; }
        public double end { get; set; }
        public double response { get; set; }
        public double exec { get; set; }
        public double idle { get; set; }
        public double returning { get; set; }
        public double blocked { get; set; }
        public double suspended { get; set; }
        public int totalPages { get; set; }
        public int[] pages { get; set; }
        public int[] frames { get; set; }
        /* Nota: los arreglos de pages y frames se inicializan
         * cada que se va a paginar un nuevo proceso.
         * Los que aquí aparecen son solo referencias. */

        public BCP()
        {
            PID = 0;
            state = -2;
            arrival = 0.0;
            end = 0.0;
            response = 0.0;
            exec = 0.0;
            idle = 0.0;
            returning = 0.0;
            blocked = 0.0;
            suspended = 0.0;
            totalPages = 0;
        }

        public BCP(int nulo)
        {
            PID = nulo; //nulo = -1
            state = -2;
            arrival = 0.0;
            end = 0.0;
            response = 0.0;
            exec = 0.0;
            idle = 0.0;
            returning = 0.0;
            blocked = 0.0;
            suspended = 0.0;
            totalPages = 0;
        }
    }
}
