using Discord;
using PamelloNET.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PamelloNET.Builders
{
    public class MiscEmbedBuilder : EmbedBuilder
    {
        public MiscEmbedBuilder(string message) {
            Color = PamelloMethods.HexToDColor(int.Parse(Program.config["none_color"]));
            Title = "Processing...";
            Description = message;
        }
        public MiscEmbedBuilder(string title, string message) {
            Color = PamelloMethods.HexToDColor(int.Parse(Program.config["none_color"]));
            Title = title;
            Description = message;
        }
    }
    public class InfoEmbedBuilder : EmbedBuilder
    {
        public InfoEmbedBuilder(string message) {
            Color = PamelloMethods.HexToDColor(int.Parse(Program.config["info_color"]));
            Title = "Info";
            Description = message;
        }
        public InfoEmbedBuilder(string title, string message) {
            Color = PamelloMethods.HexToDColor(int.Parse(Program.config["info_color"]));
            Title = title;
            Description = message;
        }
    }
    public class ErrorEmbedBuilder : EmbedBuilder
    {
        public ErrorEmbedBuilder(string message) {
            Color = PamelloMethods.HexToDColor(int.Parse(Program.config["error_color"]));
            Title = "Error";
            Description = message;
        }
    }

    public class PlayEmbedBuilder : EmbedBuilder
    {
        public Embed BuildProcessing() {
            Color = PamelloMethods.HexToDColor(int.Parse(Program.config["none_color"]));
            Title = "Processing...";
            Description = "Getting info...";

            return Build();
        }
        public Embed BuildDownloading() {
            Color = PamelloMethods.HexToDColor(int.Parse(Program.config["none_color"]));
            Title = "Downloading...";
            Description = "Donwloading audio from YouTube video...";

            return Build();
        }
        public Embed BuildFinal(PamelloSong song, bool isNew) {
            Color = PamelloMethods.HexToDColor(int.Parse(Program.config["main_color"]));
            Title = isNew ? "Downloaded And Added To Queue" : "Added To Queue";
            Description = song.GetDiscordName();
            ThumbnailUrl = song.GetDiscordImage();

            return Build();
        }
        public Embed BuildFinal(PamelloPlayList playlist, bool temp = false) {
            Color = PamelloMethods.HexToDColor(int.Parse(Program.config["main_color"]));
            Title = temp ? "YouTube Playlist Added To Queue" : $"Playlist \"{playlist}\" Added To Queue";
            Description = temp ? $"`{playlist.Songs.Count}` songs added to queue" : $"`{playlist.Songs.Count}` songs from playlist \"**{playlist}**\" added to queue";
            ThumbnailUrl = playlist.Songs.First().GetDiscordImage();

            return Build();
        }


    }

    public class PlayListEmbedBuilder : EmbedBuilder
    {
        public byte page { get; private set; }
        public byte pages { get; private set; }

        public PlayListEmbedBuilder(PamelloPlayList playlist, byte page) {
            Color = PamelloMethods.HexToDColor(int.Parse(Program.config["main_color"]));
            Title = playlist.Name;

            pages = 0;
            string songstr;
            StringBuilder sbuilder = new StringBuilder();

            byte maxSongs = byte.Parse(Program.config["playlist_page_max_songs"]);

            if (playlist.Songs.Count > 0) {
                int i = 0;
                foreach (PamelloSong song in playlist) {
                    songstr = $"`{i + 1}` - {song.GetDiscordName()} [{song.SongType}]\n";

                    if (sbuilder.Length + songstr.Length > 1000 || (i != 0 && i % maxSongs == 0))
                        pages++;

                    if (pages == page) sbuilder.Append(songstr);

                    i++;
                }
                AddField("Is Private: ", playlist.IsPrivate.ToString());
                AddField("Owner: ", Program.client.GetUser(playlist.OwnerID).Username);
                AddField("Songs: ", sbuilder.ToString());

                WithFooter($"Page {page + 1}/{pages + 1} | Songs {page * maxSongs + 1}-{(
                    page == pages ?
                    playlist.Songs.Count
                    : (page + 1) * maxSongs
                )}/{playlist.Songs.Count} | {playlist.Ver}");
            }
            else {
                Description = "Empty";
                WithFooter(playlist.Ver);
            }

            ImageUrl = playlist.Songs.First().GetDiscordImage();
            this.page = page;
        }
    }
}
