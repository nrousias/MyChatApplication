using System;
using System.Collections.Generic;
using System.Text;

namespace ChatClient
{
    public class Message
    {
        public  string message { get; set; }
        public int timestamp { get; set; }
        public string nickname { get; set; }
        public string type { get; set; }
    }
}
