using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PamelloNET.Music
{
    public class PlayerManager
    {
        public Dictionary<ulong, PamelloPlayer> Players { get; private set; }

        public PlayerManager() {
            Players = new Dictionary<ulong, PamelloPlayer>();
        }


        public bool GetPlayer(ulong GuildID, out PamelloPlayer? player) {
            return Players.TryGetValue(GuildID, out player);
        }

        public PamelloPlayer CreatePlayer(ulong GuildID, IAudioClient aclient) {
            if (Players.ContainsKey(GuildID)) return Players[GuildID];

            PamelloPlayer player = new PamelloPlayer(aclient);
            Players.Add(GuildID, player);
            return player;
        }
    }
}
