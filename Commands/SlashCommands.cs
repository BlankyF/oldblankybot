using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Victoria.Node;

namespace blankyBot.Commands
{
    public class SlashCommands
    {

        private readonly LavaNode _lavaNode;
        private readonly DiscordSocketClient _client;
        private readonly ResourcesCommands ressources;

        public SlashCommands(LavaNode lavaNode, DiscordSocketClient client)
        {
            _lavaNode = lavaNode;
            _client = client;
            ressources = new(_lavaNode, _client);
        }
        public async Task HelpCommand(SocketSlashCommand command)
        {
            await command.RespondAsync(embed: ressources.embedHelp.Build());
        }

        public async Task PingCommand(SocketSlashCommand command)
        {
            TimeSpan ping = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now) - TimeZoneInfo.ConvertTimeToUtc(command.CreatedAt.DateTime);
            await command.RespondAsync($"Pong: {ping.TotalMilliseconds} ms");
        }
        public async Task RollCommand(SocketSlashCommand command)
        {
            Embed embed = ressources.RollCommand((string)command.Data.Options.First().Value);
            await command.RespondAsync(embed: embed);
        }
        public async Task FurryCommand(SocketSlashCommand command)
        {
            Embed embed;
            if (command.Data.Options.Count == 0)
            {
                embed = ressources.RandomCommand(command.User, "furry", 2);
            }
            else
            {
                SocketUser user = (SocketUser)command.Data.Options.First().Value;
                embed = ressources.RandomCommand(user, "furry", 2);
            }
            await command.RespondAsync(embed: embed);
        }
        public async Task FemboyCommand(SocketSlashCommand command)
        {
            Embed embed;
            if (command.Data.Options.Count == 0)
            {
                embed = ressources.RandomCommand(command.User, "femboy", 0);
            }
            else
            {
                SocketUser user = (SocketUser)command.Data.Options.First().Value;
                embed = ressources.RandomCommand(user, "femboy", 0);
            }
            await command.RespondAsync(embed: embed);
        }
        public async Task GayCommand(SocketSlashCommand command)
        {
            Embed embed;
            if (command.Data.Options.Count == 0)
            {
                embed = ressources.RandomCommand(command.User, "gay", 1);
            }
            else
            {
                SocketUser user = (SocketUser)command.Data.Options.First().Value;
                embed = ressources.RandomCommand(user, "gay", 1);
            }
            await command.RespondAsync(embed: embed);
        }
        // MUSIC BOT COMMANDS
        public async Task JoinCommand(SocketSlashCommand command)
        {
            Embed embed;
            embed = ressources.Join((ulong)command.GuildId, command.User, command.Channel);
            await command.RespondAsync(embed: embed);
        }
        public async Task LeaveCommand(SocketSlashCommand command)
        {
            Embed embed;
            embed = ressources.Leave((ulong)command.GuildId, command.User);
            await command.RespondAsync(embed: embed);
        }
        public async Task PlayCommand(SocketSlashCommand command)
        {
            EmbedBuilder embed = new();
            if (command.Data.Options.Count == 0)
            {
                await command.RespondAsync(embed: embed.WithDescription($"Error : not enough argument.")
                    .WithColor(Color.Red)
                    .Build());
            }

            if (command.Data.Options.Count > 1)
            {
                await command.RespondAsync(embed: await ressources.Play(guildId: (ulong)command.GuildId, user: command.User, channel: command.Channel, searchQuery: (string)command.Data.Options.First().Value, isShuffle: (bool)command.Data.Options.Last().Value));
            }
            else
            {
                await command.RespondAsync(embed: await ressources.Play(guildId: (ulong)command.GuildId, user: command.User, channel: command.Channel, searchQuery: (string)command.Data.Options.First().Value));
            }
        }
        public async Task PauseCommand(SocketSlashCommand command) => await command.RespondAsync(embed: await ressources.Pause((ulong)command.GuildId, command.User));
        public async Task ResumeCommand(SocketSlashCommand command) => await command.RespondAsync(embed: await ressources.Resume((ulong)command.GuildId, command.User));
        public async Task StopCommand(SocketSlashCommand command) => await command.RespondAsync(embed: await ressources.Stop((ulong)command.GuildId, command.User));
        public async Task SkipCommand(SocketSlashCommand command) => await command.RespondAsync(embed: await ressources.Skip((ulong)command.GuildId, command.User));
        public async Task NowPlaying(SocketSlashCommand command) => await command.RespondAsync(embed: await ressources.NowPlaying((ulong)command.GuildId, command.User));
        public async Task Shuffle(SocketSlashCommand command) => await command.RespondAsync(embed: ressources.Shuffle((ulong)command.GuildId, command.User));
        public async Task Queue(SocketSlashCommand command)
        
        {
            if (command.Data.Options.Count != 0 && (long)command.Data.Options.First()?.Value > 0)
            {
                await command.RespondAsync(embed: ressources.Queue((ulong)command.GuildId, command.User, Convert.ToInt32((long)command.Data.Options.First().Value)));
                return;
            }
            await command.RespondAsync(embed: ressources.Queue((ulong)command.GuildId, command.User, 1));
        }
    }
}
