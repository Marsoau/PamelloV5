using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System.Reflection;

namespace PamelloNET
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient client;
        private readonly InteractionService commands;
        private readonly IServiceProvider services;

        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services) {
            this.client = client;
            this.commands = commands;
            this.services = services;
        }

        public async Task InitializeAsync() {
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            client.InteractionCreated += HandleInteraction;
        }

        private async Task HandleInteraction(SocketInteraction interaction) {
            try {
                var ctx = new SocketInteractionContext(client, interaction);
                await commands.ExecuteCommandAsync(ctx, services);
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
