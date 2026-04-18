using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PamelloNET
{
    public abstract class PamelloMethods
    {
        public static Color HexToDColor(int colorcode) {
            return new Color(
                colorcode / 0x10000,
                (colorcode / 0x100) % 0x100,
                colorcode % 0x100
            );
        }

        public static string CutYTID(string url) {
            string shorturl = "https://youtu.be/";
            string longurl = "https://www.youtube.com/watch?v=";

            bool isShort = true;

            if (url.Length >= (shorturl.Length + 11))
                for (int i = 0; i < shorturl.Length; i++) {
                    if (shorturl[i] != url[i]) {
                        isShort = false;
                        break;
                    }
                }
            else return null;
            if (isShort) return url.Split('/').Last();

            if (url.Length >= (longurl.Length + 11))
                for (int i = 0; i < longurl.Length; i++) {
                    if (longurl[i] != url[i]) {
                        return null;
                    }
                }
            else return null;
            return url.Split('&').First().Split('=').Last();
        }

        // https://www.youtube.com/watch?v=lZGo8jcEW2M&list=RDMM&index=3
        public static string CutYTListID(string url) {
            return url.Split("list=").Last().Split('&').First();
        }
    }
}
