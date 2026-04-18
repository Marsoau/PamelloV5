using Discord;
using PamelloNET.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace PamelloNET.Builders
{
    public class QueueEmbedBuilder : EmbedBuilder
    {
        public byte page { get; private set; }
        public byte pages { get; private set; }
        public QueueEmbedBuilder(PamelloPlayer player, byte page) {
            Color = PamelloMethods.HexToDColor(int.Parse(Program.config["main_color"]));
            Title = "Queue" + (
                player.LoopMode == PlayerLoopMode.Loop ?
                " (Looped)"
                : player.LoopMode == PlayerLoopMode.SingleLoop ?
                " (Single Looped)"
                : ""
            );

            byte maxSongs = byte.Parse(Program.config["queue_page_max_songs"]);

            pages = 0;
            string songstr;
            StringBuilder descriptionBuilder = new StringBuilder();

            if (player.Queue.Count > 0) {
                int i = 0;
                foreach (PamelloSong song in player.Queue) {
                    songstr = $"`{i + 1}`{(i == player.QN ? " - **now >** " : " - ")}{song.GetDiscordName()}\nAdded by **{DiscordFormat.Ecranate(Program.client.GetUser(song.UserID).Username)}** ({song.SongType})\n";

                    if (descriptionBuilder.Length + songstr.Length > 4000 || (i != 0 && i % maxSongs == 0))
                        pages++;

                    if (pages == page) descriptionBuilder.Append(songstr);

                    i++;
                }
                AddField(
                    "Current Song",
                    $"`{player.QN + 1}` - {player.Queue[player.QN].GetDiscordName()}"
                );

                WithDescription(descriptionBuilder.ToString());
                ImageUrl = player.Queue[player.QN].GetDiscordImage();
                WithFooter($"Page {page + 1}/{pages + 1} | Songs {page * maxSongs + 1}-{(
                    page == pages ?
                    player.Queue.Count
                    : (page + 1) * maxSongs
                )}/{player.Queue.Count}");
            }
            else {
                Description = "Empty";
            }

            this.page = page;
        }
    }

    public class QueueComponentsBuilder : ComponentBuilder
    {
        public QueueComponentsBuilder(byte page, byte pages, int tokenid) {
            ButtonBuilder refreshButton = new ButtonBuilder() {
                Label = "Refresh Page",
                CustomId = $"qbutton{tokenid}&refresh",
                Style = ButtonStyle.Primary
            };
            ButtonBuilder prevButton = new ButtonBuilder() {
                Label = "Prev Page",
                CustomId = $"qbutton{tokenid}&{page - 1}",
                Style = ButtonStyle.Secondary
            };
            ButtonBuilder nextButton = new ButtonBuilder() {
                Label = "Next Page",
                CustomId = $"qbutton{tokenid}&{page + 1}",
                Style = ButtonStyle.Secondary
            };

            if (page != 0) WithButton(prevButton);
            WithButton(refreshButton);
            if (page != pages) WithButton(nextButton);
        }
    }
}
