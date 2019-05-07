using System;

namespace HubManager
{
    public class Packet
    {
        public Packet(Tuple<int, int, int> header = null, object content = null)
        {
            DateTime = DateTime.Now.ToUniversalTime();
            Header = header;
            Content = content;
        }

        public DateTime DateTime { get; }
        public Tuple<int, int, int> Header { get; set; }
        public object Content { get; set; }
        //public int Length { get; set; }
    }
}
