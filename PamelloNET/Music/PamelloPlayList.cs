using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using PamelloNET.Builders;
using System.Runtime.ConstrainedExecution;
using YamlDotNet.Core.Tokens;

namespace PamelloNET
{
    public class PamelloPlayList
    {
        public string Name { get; protected set; }
        public ulong OwnerID { get; protected set; }
        public bool IsPrivate { get; protected set; }

        private bool IsInitialized { get; set; }
        public string Ver { get; protected set; }
        public string AVer { get; protected set; }

        public List<PamelloSong> Songs { get; protected set; }

        public PamelloPlayList(string name) {
            Name = name;

            AVer = "ver-5.2";
            Songs = new List<PamelloSong>();
            IsInitialized = false;
        }

        public IEnumerator<PamelloSong> GetEnumerator() {
            return Songs.GetEnumerator();
        }

        public void Save(bool overwrite) {
            if (Songs.Count == 0) throw new Exception("Can`t save empty playlist)");

            if (File.Exists($"{Program.config["playlist_path"]}{Name}.pml")) {
                if (!overwrite) throw new Exception("Can\\`t overwrite file (set patameter `overwrite` to `true` for overwriting)");

                using (BinaryReader binReader =
                    new BinaryReader(File.Open($"{Program.config["playlist_path"]}{Name}.pml", FileMode.Open))) {

                    string ver = binReader.ReadString();

                    if (binReader.BaseStream.Length > 0) {
                        if (binReader.ReadBoolean())
                            if (binReader.ReadUInt64() != OwnerID) throw new Exception("File Protected From Changes");
                    }

                    binReader.Close();
                }
            }

            using (BinaryWriter binWriter =
                new BinaryWriter(File.Open($"{Program.config["playlist_path"]}{Name}.pml", FileMode.Create))) {

                binWriter.Write(AVer);

                binWriter.Write(IsPrivate);
                binWriter.Write(OwnerID);
                binWriter.Write((short)Songs.Count);

                foreach (PamelloSong song in Songs) {
                    binWriter.Write((byte)song.SongType);
                    binWriter.Write(song.Name);
                    binWriter.Write(song.GetPLValue());
                }

                binWriter.Close();
            }
        }

        public async Task Load(ulong userid) {
            await Load(userid, Program.config["playlist_path"]);
        }

        public async Task Load(ulong userid, string path) {
            if (!File.Exists($"{path}{Name}.pml")) throw new Exception("PlayList file doesn`t exist");

            using (BinaryReader binReader =
                new BinaryReader(File.Open($"{path}{Name}.pml", FileMode.Open))) {
                if (binReader.BaseStream.Length > 0) {
                    Ver = binReader.ReadString();
                    if (Ver == "ver-5.2") {
                        IsPrivate = binReader.ReadBoolean();
                        OwnerID = binReader.ReadUInt64();

                        PamelloSongType stype;
                        string fvalue;
                        string svalue;

                        YouTubeSong ytsong;
                        short scount = binReader.ReadInt16();
                        for (short i = 0; i < scount; i++) {
                            stype = (PamelloSongType)binReader.ReadByte();
                            fvalue = binReader.ReadString();
                            svalue = binReader.ReadString();

                            if (stype == PamelloSongType.Local) {
                                Songs.Add(new PamelloSong(fvalue, svalue, userid));
                            }
                            else if (stype == PamelloSongType.YouTube) {
                                ytsong = new YouTubeSong(fvalue, svalue, userid);

                                if (!ytsong.IsDownloaded())
                                    if (await ytsong.TryInit())
                                        await ytsong.Download();

                                    else throw new Exception($"YouTube song \"{fvalue}\" ({svalue}) doesn`t exist");

                                Songs.Add(ytsong);
                            }
                        }

                        binReader.Close();
                    }
                    else if (Ver == "ver-5.1") {
                        IsPrivate = binReader.ReadBoolean();
                        OwnerID = binReader.ReadUInt64();

                        PamelloSongType stype;
                        string svalue;

                        YouTubeSong ytsong;
                        short scount = binReader.ReadInt16();
                        for (short i = 0; i < scount; i++) {
                            stype = (PamelloSongType)binReader.ReadByte();
                            svalue = binReader.ReadString();

                            if (stype == PamelloSongType.Local) {
                                Songs.Add(new PamelloSong(svalue.Split('/').Last(), svalue, userid));
                            }
                            else if (stype == PamelloSongType.YouTube) {
                                ytsong = new YouTubeSong(svalue, userid);

                                if (await ytsong.TryInit())
                                    await ytsong.Download();
                                else throw new Exception($"YouTube song \"{svalue}\" doesn`t exist");

                                Songs.Add(ytsong);
                            }
                        }

                        binReader.Close();
                    }
                    else throw new Exception($"Can`t convert playlist version ({Ver}) to current version ({AVer})");

                    IsInitialized = true;
                }
                else {
                    throw new Exception($"Empty PlayList file");
                }
            }
        }

        public async Task YouTubeInit(ulong userid, bool isprivate, string ytplid) {
            YouTubeSong ytsong;

            using (YouTubeService? plYoutubeService = new YouTubeService(new BaseClientService.Initializer() {
                ApiKey = "AIzaSyDPBURkXbO9mnQzbpUPWP6UO7EF3ZgIKIQ",
            })) {
                var searchRequest = plYoutubeService.PlaylistItems.List("snippet");
                searchRequest.PlaylistId = ytplid;
                searchRequest.MaxResults = 48;
                PlaylistItemListResponse? playListResponse = await searchRequest.ExecuteAsync();

                foreach (PlaylistItem video in playListResponse.Items) {
                    ytsong = new YouTubeSong(
                        video.Snippet.Title,
                        video.Snippet.ResourceId.VideoId,
                        userid
                    );

                    try {
                        await ytsong.Download();
                        Songs.Add(ytsong);
                    }
                    catch(Exception exception) {
                        Console.WriteLine(exception.Message);
                    }
                }
            }

            IsPrivate = isprivate;
            OwnerID = userid;
            IsInitialized = true;
        }

        public PlayListEmbedBuilder? GetEmbedBuilder(byte page = 0) {
            if (IsInitialized) return new PlayListEmbedBuilder(this, page);
            else return null;
        }

        public void NewInit(ulong userid, bool isprivate, List<PamelloSong> songs) {
            IsPrivate = isprivate;
            OwnerID = userid;
            Songs = songs;

            IsInitialized = true;
        }

        public override string ToString() {
            return Name;
        }
    }
}
