using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PamelloNET
{
    public abstract class DiscordFormat
    {
        public static string Ecranate(string str) {
            str = str.Replace("_", "\\_");
            str = str.Replace("`", "\\`");
            str = str.Replace("~", "\\~");
            str = str.Replace("*", "\\*");
            str = str.Replace("|", "\\|");

            return str;
        }

        public static string PingUser(ulong userID) {
            return $"<@{userID}>";
        }
    }
}
