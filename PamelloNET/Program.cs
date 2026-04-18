using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetEscapades.Configuration.Yaml;
using PamelloNET.Builders;
using PamelloNET.Modules;
using PamelloNET.Music;
using System.Text;

namespace PamelloNET
{
    public class Program
    {
        public static DiscordSocketClient? client { get; private set; }

        public static EphemeralInteractionHandler QueueEHandler { get; private set; }
        public static PlayerManager playerManager { get; private set; }

        public static IConfigurationRoot config { get; private set; }

        public static Task Main() => new Program().MainAsync();
        public Program() {
            if (!File.Exists("config.yaml")) {
                Console.WriteLine("Can`t find config.yaml file");
                throw new Exception();
            }

            QueueEHandler = new EphemeralInteractionHandler();
            playerManager = new PlayerManager();

            config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddYamlFile("config.yaml")
                .Build();
        }

        public async Task MainAsync() {
            Console.OutputEncoding = Encoding.UTF8;

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                    services
                    .AddSingleton(config)
                    .AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig {
                        GatewayIntents = GatewayIntents.All,
                        AlwaysDownloadUsers = true,
                    }
                ))
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                ).Build();

            await RunAsync(host);
        }

        public async Task RunAsync(IHost host) {
            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            client = provider.GetRequiredService<DiscordSocketClient>();
            var slashCommands = provider.GetRequiredService<InteractionService>();
            await provider.GetRequiredService<InteractionHandler>().InitializeAsync();
            var config = provider.GetRequiredService<IConfigurationRoot>();


            client.Log += async (LogMessage message) => { Console.WriteLine(message.Message); };
            slashCommands.Log += async (LogMessage message) => { Console.WriteLine(message.Message); };

            client.Ready += async () => {
                await slashCommands.RegisterCommandsToGuildAsync(UInt64.Parse(config["test_guild_id"]));
                Console.WriteLine("Ready");
            };

            await client.LoginAsync(TokenType.Bot, config["token"]);
            await client.StartAsync();

            await Task.Delay(-1);
        }
    }
}
