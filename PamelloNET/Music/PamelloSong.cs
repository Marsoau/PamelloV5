using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PamelloNET;

namespace PamelloNET
{
    public enum PamelloSongType
    {
        Local, YouTube, Spotify
    }

    public class PamelloSong
    {
        public string Name { get; protected set; }
        public string Path { get; protected set; }
        public ulong UserID { get; protected set; }

        public PamelloSongType SongType { get; protected set; }

        public PamelloSong(string name, string path, ulong userid) {
            Name = name;
            Path = path;
            UserID = userid;

            SongType = PamelloSongType.Local;
        }

        public virtual string GetDiscordName() {
            return DiscordFormat.Ecranate(Name);
        }
        public virtual string? GetDiscordImage() {
            return null;
        }
        public virtual string? GetPLValue() {
            StringBuilder strb = new StringBuilder();

            string[]? bebe = Path.Split('/');

            for (int i = 0; i < bebe.Length - 1; i++) {
                strb.Append(bebe[i]);
                strb.Append('/');
            }

            return bebe.ToString();
        }

        public override string ToString() {
            return $"[{SongType}] {Name}";
        }
    }

    public class YouTubeSong : PamelloSong
    {
        public string YTID { get; protected set; }

        public YouTubeSong(string ytid, ulong userid) : base(null, $"{Program.config["music_path"]}{ytid}.mp4", userid) {
            YTID = ytid;

            SongType = PamelloSongType.YouTube;
        }
        public YouTubeSong(string name, string ytid, ulong userid) : base(name, $"{Program.config["music_path"]}{ytid}.mp4", userid) {
            YTID = ytid;

            SongType = PamelloSongType.YouTube;
        }

        public string GetURL() {
            return "https://youtu.be/" + YTID;
        }

        public override string GetDiscordImage() {
            return "https://img.youtube.com/vi/" + YTID + "/maxresdefault.jpg";
        }

        public override string GetDiscordName() {
            return $"[{DiscordFormat.Ecranate(Name)}]({GetURL()})";
        }

        public override string GetPLValue() {
            return YTID;
        }

        public async Task<bool> TryInit() {
            using (YouTubeService? youtubeService = new YouTubeService(new BaseClientService.Initializer() {
                ApiKey = "AIzaSyDPBURkXbO9mnQzbpUPWP6UO7EF3ZgIKIQ",
            })) {
                var searchRequest = youtubeService.Videos.List("snippet");
                searchRequest.Id = YTID;
                VideoListResponse? searchResponse = await searchRequest.ExecuteAsync();

                Video? youTubeVideo = searchResponse.Items.FirstOrDefault();
                if (youTubeVideo is not null) {
                    Name = youTubeVideo.Snippet.Title;
                    return true;
                }
                return false;
            }
        }

        public bool IsDownloaded() {
            return File.Exists($"{Program.config["music_path"]}{YTID}.mp4");
        }

        public async Task Download() {
            if (IsDownloaded()) return;

            try {
                Process? downloader = Process.Start(new ProcessStartInfo {
                    FileName = "python",
                    Arguments = $"{Program.config["downloader_path"]} {Program.config["music_path"]} {YTID}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                });
                if (downloader is null) {
                    throw new Exception("Downloader is null");
                }

                await downloader.WaitForExitAsync();
                downloader.Close();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public override string ToString() {
            return $"[{SongType}] {Name}";
        }
    }
}
