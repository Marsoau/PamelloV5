using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Newtonsoft.Json.Linq;
using PamelloNET.Builders;
using PamelloNET.Music;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace PamelloNET.Modules
{
	public enum PlayCommandType
	{
		YouTube, Local
	}
	public enum PlayCommandMode
	{
		Single, List
	}

	public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        /*
        
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /step {}");

            try {
                //
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        
        */
        public async Task PlayCommand(PlayCommandMode pcm, PlayCommandType pct, string? value, PamelloPlayList? playlist) {
			PamelloPlayer player;
			SocketVoiceChannel? vc = Context.Guild.GetUser(Context.User.Id).VoiceChannel;
			PlayEmbedBuilder playEmbedBuilder = new PlayEmbedBuilder();

			if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
				if (vc is null) throw new Exception("Connect to voice channel first");

				player = Program.playerManager.CreatePlayer( Context.Guild.Id, await vc.ConnectAsync());
			}

			if (pcm == PlayCommandMode.Single) {
				await RespondAsync(embed: playEmbedBuilder.BuildProcessing());

				if (pct == PlayCommandType.YouTube) {
					YouTubeSong ytsong = new YouTubeSong(PamelloMethods.CutYTID(value), Context.User.Id);
					bool isnew = false;

					if (await ytsong.TryInit()) {
						if (!ytsong.IsDownloaded()) {
							await ModifyOriginalResponseAsync(message => message.Embed = playEmbedBuilder.BuildDownloading());
							
							isnew = true;
							await ytsong.Download();
						}
					}
						
					else throw new Exception($"YouTube song doesn`t exist");

					player.QueueAdd(ytsong);
					await ModifyOriginalResponseAsync(message => message.Embed = playEmbedBuilder.BuildFinal(ytsong, isnew));
				}
				else if (pct == PlayCommandType.Local) {
					if (!File.Exists(value)) throw new Exception($"File \"{value}\" doesn`t exist");
					PamelloSong lsong = new PamelloSong(value.Split('/').Last().Split('.').First(), value, Context.User.Id);
					
					player.QueueAdd(lsong);
					await ModifyOriginalResponseAsync(message => message.Embed = playEmbedBuilder.BuildFinal(lsong, false));
				}
			}
			else if (pcm == PlayCommandMode.List) {
				foreach (PamelloSong song in playlist) {
					player.QueueAdd(song);
				}
				await ModifyOriginalResponseAsync(message => message.Embed = playEmbedBuilder.BuildFinal(playlist, pct == PlayCommandType.YouTube));
			}

			await player.MainLoop();
		}

		//work
		[SlashCommand("ping", "Check connection with bot")]
		public async Task HandlePingCommand() {
			await RespondAsync($"Pong {DiscordFormat.PingUser(Context.User.Id)}!", ephemeral: true);
		}

        //work
        [SlashCommand("help", "Help with commands", runMode: RunMode.Async)]
		public async Task HandleHelpCommand() {

        }

        //~work
        [SlashCommand("play", "Play song from YouTube", runMode: RunMode.Async)]
        public async Task HandlePlayCommand(string url) {
            Console.WriteLine($"[{Context.User.Username}] /play {url}");

            try {
                await PlayCommand(PlayCommandMode.Single, PlayCommandType.YouTube, url, null);
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        //~work
        [SlashCommand("search", "Play song from YouTube", runMode: RunMode.Async)]
        public async Task HandleSearchCommand(string request) {
            Console.WriteLine($"[{Context.User.Username}] /search {request}");

            try {
                StringBuilder strb = new StringBuilder();
                ComponentBuilder cBuilder = new ComponentBuilder();
                cBuilder.ActionRows = new List<ActionRowBuilder>(4);

                await RespondAsync("searching...", ephemeral: true);

                using (YouTubeService? youtubeService = new YouTubeService(new BaseClientService.Initializer() {
                    ApiKey = "AIzaSyDPBURkXbO9mnQzbpUPWP6UO7EF3ZgIKIQ",
                })) {
                    var searchRequest = youtubeService.Search.List("snippet");
                    searchRequest.Q = request;
                    searchRequest.MaxResults = 16;
                    SearchListResponse? searchResponse = await searchRequest.ExecuteAsync();

                    List<SearchResult> videos = new List<SearchResult>();
                    
                    foreach (SearchResult result in searchResponse.Items) {
                        if (result.Id.VideoId is not null) videos.Add(result);
                        if (videos.Count == 8) break;
                    }

                    string label1, label2;

                    for (int i = 0; i < (videos.Count / 2) * 2; i += 2) {
                        Console.WriteLine();

                        label1 = videos[i].Snippet.Title;
                        label2 = videos[i + 1].Snippet.Title;

                        cBuilder.AddRow(new ActionRowBuilder()
                        .WithButton(new ButtonBuilder {
                            CustomId = $"{videos[i].Id.VideoId}&search&{i}",
                            Label = label1.Substring(0, label1.Length < 80 ? label1.Length : 80),
                            Style = ButtonStyle.Danger
                        })
                        .WithButton(new ButtonBuilder {
                            CustomId = $"{videos[i + 1].Id.VideoId}&search&{i + 1}",
                            Label = label2.Substring(0, label2.Length < 80 ? label2.Length : 80),
                            Style = ButtonStyle.Danger
                        }));
                    }
                    if (videos.Count % 2 == 1) {
                        label1 = videos.Last().Snippet.Title;

                        cBuilder.AddRow(new ActionRowBuilder().WithButton(new ButtonBuilder { 
                            CustomId = $"9search&{videos.Last().Id.VideoId}",
                            Label = label1.Substring(0, label1.Length < 72 ? label1.Length : 72),
                            Style = ButtonStyle.Danger
                        }));
                    }
                }

                await ModifyOriginalResponseAsync(message => {
                    message.Components = cBuilder.Build();
                    message.Content = null;
                });
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        [ComponentInteraction("*&search&*")]
        public async Task HadleSearchInteraction(string wild) {
            try {
                Console.WriteLine($"[button play] {wild}");

                await PlayCommand(PlayCommandMode.Single, PlayCommandType.YouTube, $"https://youtu.be/{wild}", null);
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        [SlashCommand("play-local", "Play song from YouTube", runMode: RunMode.Async)]
        public async Task HandlePlayLocalCommand(string path) {
            Console.WriteLine($"[{Context.User.Username}] /play-local {path}");

            try {
                await PlayCommand(PlayCommandMode.Single, PlayCommandType.Local, path, null);
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }


        //~work
        [SlashCommand("play-list", "View info about playlist", runMode: RunMode.Async)]
		public async Task HandleViewListCommand(PlayCommandType pct, string value) {
			PamelloPlayList playlist = null;
            EmbedBuilder ebuilder = new EmbedBuilder {
                Color = PamelloMethods.HexToDColor(int.Parse(Program.config["none_color"])),
                Title = "Processing...",
                Description = "Getting playlist songs info...",
            };

            Console.WriteLine($"[{Context.User.Username}] /play-list {pct} {value}");

            await RespondAsync(embed: ebuilder.Build());

			try {
				if (pct == PlayCommandType.Local) {
					playlist = new PamelloPlayList(value);
					await playlist.Load(Context.User.Id);
				}
				else if (pct == PlayCommandType.YouTube) {
					playlist = new PamelloPlayList("temp");
					await playlist.YouTubeInit(Context.User.Id, false, PamelloMethods.CutYTListID(value));
				}
			}
			catch (Exception exception) {
                await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                return;
			}

			try {
				await PlayCommand(PlayCommandMode.List, pct, null, playlist);
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        //work
        [SlashCommand("playlists", "View all playlists")]
		public async Task HandlePlaylistsCommand() {
            try {
                DirectoryInfo playlistsInfo = new DirectoryInfo(Program.config["playlist_path"]);
                InfoEmbedBuilder infoEmbed = new InfoEmbedBuilder("PlayLists", "");

                foreach (FileInfo file in playlistsInfo.GetFiles()) {
                    infoEmbed.Description += $"{file.Name}\n";
                }

                await RespondAsync(embed: infoEmbed.Build());
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        //~nice
        [SlashCommand("save-list", "Save songs from queue to playlist", runMode: RunMode.Async)]
		public async Task HandleSaveListCommand(string name, bool isprivate = false, bool overwrite = false) {
			PamelloPlayer player;
			if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) 
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);

            Console.WriteLine($"[{Context.User.Username}] /save-list {name} {isprivate} {overwrite}");

            try {
                PamelloPlayList playlist = new PamelloPlayList(name);

                playlist.NewInit(Context.User.Id, isprivate, player.Queue);

                playlist.Save(overwrite);
                await RespondAsync(embed: new InfoEmbedBuilder("Create Playlist", $"Created playlist \"**{playlist.Name}\"** with `{playlist.Songs.Count}` songs").Build());
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }


        //~nice
        [SlashCommand("queue", "View player queue", runMode: RunMode.Async)]
		public async Task HandleQueueCommand() {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /queue");

            int tokenid = Program.QueueEHandler.Register(Context.User.Id);
            byte page = 0;

            QueueEmbedBuilder qeBuilder = new QueueEmbedBuilder(player, page);
            if (qeBuilder.pages < page) throw new Exception("Page out of range");
                
            QueueComponentsBuilder qcBuilder = new QueueComponentsBuilder(page, qeBuilder.pages, tokenid);

            await RespondAsync(embed: qeBuilder.Build(), components: qcBuilder.Build(), ephemeral: true);

            string? value;
            while (Program.QueueEHandler.IsActive(tokenid)) {
                if (Program.QueueEHandler.IsTriggered(tokenid)) {
                    value = Program.QueueEHandler.Respond(tokenid);
                    if (value is null) throw new Exception("QueueEHandler.Respond returned null value");

                    if (value != "refresh") page = byte.Parse(value);

                    qeBuilder = new QueueEmbedBuilder(player, page);
                    if (qeBuilder.pages < page) {
                        page = qeBuilder.pages;

                        qeBuilder = new QueueEmbedBuilder(player, page);
                    }
                    qcBuilder = new QueueComponentsBuilder(page, qeBuilder.pages, tokenid);


                    await ModifyOriginalResponseAsync(message => {
                        message.Embed = qeBuilder.Build();
                        message.Components = qcBuilder.Build();
                    });
                }
                Thread.Sleep(250);
            }
        }

        //work
        [ComponentInteraction("qbutton*")]
		public async Task HandleQNextButton(string wild) {
            try {
                Program.QueueEHandler.Trigger(wild);
                await RespondAsync();
            }
            catch (Exception exception) {
                Console.WriteLine($"[Command Exception] {exception.Message}");
                await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
            }
            
        }

        //~nice
        [SlashCommand("shuffle", "Shuffle player queue")]
		public async Task HandleShuffleCommand() {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /shuffle");

            try {
                player.Shuffle();
                await RespondAsync(embed: new InfoEmbedBuilder("Queue Shuffle", $"Queue shuffled").Build(), ephemeral: true);
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        //~nice
        [SlashCommand("clear", "Clear player queue")]
		public async Task HandleClearCommand() {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /clear");

            try {
                player.Clear();
                await RespondAsync(embed: new InfoEmbedBuilder("Queue Clear", $"Queue cleared").Build());
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }


        //~nice
        [SlashCommand("mult", "Multiply song in queue N times")]
		public async Task HandleMultCommand(short pos, short n) {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /mult {pos} {n}");

            try {
                player.Mult((short)(pos - 1), n);
                await RespondAsync(embed: new InfoEmbedBuilder("Song Mult", $"Song in pos {pos} multiplied {n} times").Build(), ephemeral: true);
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        //~nice
        [SlashCommand("jump", "jump player queue")]
		public async Task HandleJumpCommand(short pos) {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /jump {pos}");

            try {
                PamelloSong song = player.Jump((short)(pos - 1));
                await RespondAsync(embed: new InfoEmbedBuilder("Jump", $"Jumped to song {song.GetDiscordName()}").Build(), ephemeral: true);
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        //~nice
        [SlashCommand("loop", "jump player queue")]
		public async Task HandleJumpCommand(PlayerLoopMode? mode = null) {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /loop {mode}");

            try {
                if (mode is not null) player.SetMode((PlayerLoopMode)mode);
                else {
                    if (mode != player.LoopMode)
                        player.SetMode(
                            player.LoopMode == PlayerLoopMode.Loop
                            ? PlayerLoopMode.UnLoop
                            : PlayerLoopMode.Loop
                        );
                    else await RespondAsync(embed: new InfoEmbedBuilder(
                            player.LoopMode == PlayerLoopMode.UnLoop
                            ? "Queue already unlooped"
                            : player.LoopMode == PlayerLoopMode.Loop
                            ? "Queue already looped"
                            : "Current already song looped"
                        ).Build(), ephemeral: true);
                }

                await RespondAsync(embed: new InfoEmbedBuilder(
                    player.LoopMode == PlayerLoopMode.UnLoop
                    ? "Queue unlooped"
                    : player.LoopMode == PlayerLoopMode.Loop
                    ? "Queue looped"
                    : "Current song looped"
                ).Build(), ephemeral: true);

                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
		}

        //~nice
        [SlashCommand("jump-return", "jump player queue")]
		public async Task HandleStepCommand(short pos) {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /step {pos}");

            try {
                PamelloSong song = player.Leap((short)(pos - 1));
                await RespondAsync(embed: new InfoEmbedBuilder("Step", $"Stepped on song {song.GetDiscordName()}").Build(), ephemeral: true);
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        //~
        [SlashCommand("move", "leap player queue")]
		public async Task HandleMoveCommand(short from, short to) {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /move {from} {to}");

            try {
                player.Move((short)(from - 1), (short)(to - 1));
                await RespondAsync(embed: new InfoEmbedBuilder("Song Move", $"Song from pos `{from}` moved to pos `{to}`").Build(), ephemeral: true);
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }


        //~nice
        [SlashCommand("swap", "swap player queue")]
		public async Task HandleSwapCommand(short from, short to) {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /swap {from} {to}");

            try {
                player.Swap((short)(from - 1), (short)(to - 1));
                await RespondAsync(embed: new InfoEmbedBuilder("Song Swap", $"Songs in pos `{from}` and `{to}` swapped").Build(), ephemeral: true);
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }


        //~nice
        [SlashCommand("skip", "Skip song in specific position, or, if not specified, current song")]
        public async Task HandleSkipCommand() {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /skip");

            try {
                PamelloSong song = player.Skip();
                await RespondAsync(embed: new InfoEmbedBuilder("Song Skip", $"Skipped song {song.GetDiscordName()}").Build());
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        //~nice
        [SlashCommand("back", "Skip song in specific position, or, if not specified, current song")]
        public async Task HandleBackCommand() {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /back");

            try {
                PamelloSong song = player.Back();
                await RespondAsync(embed: new InfoEmbedBuilder("Back", $"Going back to {song.GetDiscordName()}").Build());
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        //nice
        [SlashCommand("remove", "Remove song in specific position, or, if not specified, current song")]
		public async Task HandleRemoveCommand(short pos) {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /remove {pos}");

            try {
                PamelloSong song = player.Remove((short)(pos - 1), 1);
                await RespondAsync(embed: new InfoEmbedBuilder("Song Remove", $"Song {song.GetDiscordName()} removed from queue").Build());
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }

        //nice
        [SlashCommand("remove-range", "Remove all songs from position A (including) to position B (including)")]
		public async Task HandleRemoveRangeCommand(short pos_a, short pos_b) {
            PamelloPlayer player;
            if (!Program.playerManager.GetPlayer(Context.Guild.Id, out player)) {
                await RespondAsync(embed: new ErrorEmbedBuilder("Can`t find player in this guild").Build(), ephemeral: true);
                return;
            }

            Console.WriteLine($"[{Context.User.Username}] /remove-range {pos_a} {pos_b}");

            try {
                if (pos_a > pos_b) {
                    short buff = pos_a;
                    pos_a = pos_b;
                    pos_b = buff;
                }

                if (pos_b - pos_a < 1) throw new Exception("Incorrect range");

                player.Remove((short)(pos_a - 1), (short)(pos_b - pos_a + 1));
                await RespondAsync(embed: new InfoEmbedBuilder("Song Remove Range", $"Songs from pos `{pos_a}` to `{pos_b}` removed").Build());
                Program.QueueEHandler.Trigger("all&refresh");
            }
            catch (Exception exception) {
                IUserMessage? msg = await GetOriginalResponseAsync();

                Console.WriteLine($"[Command Exception] {exception.Message}");
                if (msg is null) await RespondAsync(embed: new ErrorEmbedBuilder(exception.Message).Build(), ephemeral: true);
                else await ModifyOriginalResponseAsync(original => original.Embed = new ErrorEmbedBuilder(exception.Message).Build());
            }
        }
    }
}
