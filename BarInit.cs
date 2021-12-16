namespace PagingRoundRobin
{
    class BarInit
    {
        public int PID { get; set; }
        public int frame { get; set; }
        public int barPercentage { get; set; }
        public sbyte color { get; set; }
        public bool emptyBar { get; set; }
        public string number { get; set; }

        public BarInit(int PID, int frame, int barPercentage, sbyte color, bool emptyBar, string number)
        {
            this.PID = PID;
            this.frame = frame;
            this.barPercentage = barPercentage;
            this.color = color;
            this.emptyBar = emptyBar;
            this.number = number;
        }
    }
}
