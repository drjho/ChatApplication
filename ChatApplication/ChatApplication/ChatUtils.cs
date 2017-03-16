using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication
{
    public class ChatUtils
    {
        public static bool IsValidAdress(string ipString)
        {
            IPAddress address;
            return IPAddress.TryParse(ipString, out address);
        }
    }
}
