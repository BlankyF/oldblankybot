using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using blankyBot.Handler;
using Discord.Net;
using Newtonsoft.Json;
using blankyBot.Commands;
using Victoria.Node;
using Victoria;
using Victoria.Node.EventArgs;
using Victoria.Player;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace blankyBot
{
    public class Program
    {

        /*APP INIT */
        static void Main(string[] args)
        {
            Console.WriteLine("Blanky application start");
            //starts the discord bot
            new Program().RunBotAsync().GetAwaiter().GetResult();
        }

        /*DISCORD BOT INIT*/
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private LavaNode node;

        public async Task RunBotAsync()
        {
            Console.WriteLine("Blanky Bot bot start");
            _client = new DiscordSocketClient();
            var config = new DiscordSocketConfig { GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.GuildVoiceStates };
            _client = new DiscordSocketClient(config);
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddLogging()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<LavaNode>()
                .AddSingleton<NodeConfiguration>()
                .AddLavaNode()
                .BuildServiceProvider();

            // Logging
            _client.Log += Client_Log;
            // Bot Authentification init
            RegisterCommandsAsync();
            string? token = Environment.GetEnvironmentVariable("TOKEN");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.SetGameAsync("Blanky Bot 1.0.1");
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _client.StartAsync();
            _client.Ready += SlashCommandDictionary;
            await Task.Delay(-1);
        }
        public async Task SlashCommandDictionary()
        {
            node = _services.GetRequiredService<LavaNode>();
            if (!node.IsConnected)
            {
                await node.ConnectAsync();
                Console.WriteLine($"Node connection : {node.IsConnected}");
            }
            node.OnTrackEnd += Autoplay;
            Dictionary<string,SlashCommandBuilder> dictionary = new Dictionary<string,SlashCommandBuilder>();
            SlashCommandBuilder helpCommand = new SlashCommandBuilder()
                .WithName("help")
                .WithDescription("Displays all the available commands of the bot.");
            dictionary.Add("help", helpCommand);
            SlashCommandBuilder pingCommand = new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Test the latency of the bot.");
            dictionary.Add("ping", pingCommand);
            SlashCommandBuilder rollCommand = new SlashCommandBuilder()
                .WithName("roll")
                .AddOption("options", ApplicationCommandOptionType.String, "The type of roll you want. Ex: 2d6", isRequired: true)
                .WithDescription("Rolls dices like in D&D.");
            dictionary.Add("roll", rollCommand);
            SlashCommandBuilder furryCommand = new SlashCommandBuilder()
                .WithName("furry")
                .AddOption("user", ApplicationCommandOptionType.User, "The user you want to evalute.")
                .WithDescription("Scientifically calculates how much of a furry a user is.");
            dictionary.Add("furry", furryCommand);
            SlashCommandBuilder femboyCommand = new SlashCommandBuilder()
                .WithName("femboy")
                .AddOption("users", ApplicationCommandOptionType.User, "The user you want to evalute.")
                .WithDescription("Scientifically calculates how much of a femboy a user is.");
            dictionary.Add("femboy", femboyCommand);
            SlashCommandBuilder gayCommand = new SlashCommandBuilder()
                .WithName("gay")
                .AddOption("user", ApplicationCommandOptionType.User, "The user you want to evalute.")
                .WithDescription("Scientifically calculates how gay a user is.");
            dictionary.Add("gay", gayCommand);
            /// MUSIC BOT
            SlashCommandBuilder joinCommand = new SlashCommandBuilder()
                .WithName("join")
                .WithDescription("Makes the music bot join your channel.");
            dictionary.Add("join", joinCommand);
            SlashCommandBuilder leaveCommand = new SlashCommandBuilder()
                .WithName("leave")
                .WithDescription("Makes the music bot leave your channel.");
            dictionary.Add("leave", leaveCommand);
            SlashCommandBuilder playCommand = new SlashCommandBuilder()
                .WithName("play")
                .AddOption("url", ApplicationCommandOptionType.String, "The music you want.", isRequired: true)
                .AddOption("shuffle", ApplicationCommandOptionType.Boolean, "Shuffle if it's a playlist you're adding to the queue.", isRequired: false)
                .WithDescription("Plays a song from YouTube, Spotify, etc.");
            dictionary.Add("play", playCommand);
            SlashCommandBuilder pauseCommand = new SlashCommandBuilder()
                .WithName("pause")
                .WithDescription("Pauses the music bot.");
            dictionary.Add("pause", pauseCommand);
            SlashCommandBuilder resumeCommand = new SlashCommandBuilder()
                .WithName("resume")
                .WithDescription("Resumes the music bot if it's paused.");
            dictionary.Add("resume", resumeCommand);
            SlashCommandBuilder stopCommand = new SlashCommandBuilder()
                .WithName("stop")
                .WithDescription("Stop the music currently played by the bot.");
            dictionary.Add("stop", stopCommand);
            SlashCommandBuilder skipCommand = new SlashCommandBuilder()
                .WithName("skip")
                .WithDescription("Skips the first enqueued music in the music bot.");
            dictionary.Add("skip", skipCommand);
            SlashCommandBuilder nowPlayingCommand = new SlashCommandBuilder()
                .WithName("nowplaying")
                .WithDescription("Shows the music currently played by the music bot.");
            dictionary.Add("nowplaying", nowPlayingCommand);
            SlashCommandBuilder queueCommand = new SlashCommandBuilder()
                .WithName("queue")
                .AddOption("page", ApplicationCommandOptionType.Integer, "Page number.", isRequired: false)
                .WithDescription("Shows the queue of songs that are about to be played by the bot.");
            dictionary.Add("queue", queueCommand);
            SlashCommandBuilder shuffleCommand = new SlashCommandBuilder()
                .WithName("shuffle")
                .WithDescription("Shuffle the queue. (won't affect the curretly played song)");
            dictionary.Add("shuffle", shuffleCommand);
            try
            {
                IReadOnlyCollection<SocketApplicationCommand> commands = await _client.GetGlobalApplicationCommandsAsync();
                foreach (KeyValuePair<string, SlashCommandBuilder> item in dictionary)
                {
                    SocketApplicationCommand? command = commands.Where(x => x.Name == item.Key).FirstOrDefault();
                    if (command is null)
                    {
                        await _client.CreateGlobalApplicationCommandAsync(item.Value.Build());
                        continue;
                    }

                    if (item.Value.Options is null || command.Options.Count == 0)
                    {
                        continue;
                    }

                    if (item.Value.Options.Count != command.Options.Count)
                    {
                        await _client.CreateGlobalApplicationCommandAsync(item.Value.Build());
                        continue;
                    }

                    for (int i = 0; i < command.Options.Count; i++)
                    {
                        if (command.Options.ElementAt(i).Name != item.Value.Options[i].Name || command.Options.ElementAt(i).Description != item.Value.Options[i].Description || command.Options.ElementAt(i).Type != item.Value.Options[i].Type)
                        {
                            await _client.CreateGlobalApplicationCommandAsync(item.Value.Build());
                            continue;
                        }
                    }

                }
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
            _client.SlashCommandExecuted += SlashCommandHandler;
        }

        private async static Task Autoplay(TrackEndEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
        {
            if (arg.Player.Vueue.Count > 0)
            {
                // queues up the next track by skipping if the queue isn't empty
                await arg.Player.TextChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithTitle($"Finished playing {arg.Track}.")
                    .WithColor(Color.Purple)
                    .Build());
                await arg.Player.TextChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithTitle($"Now playing {arg.Player.Vueue.First().Title}.")
                    .WithColor(Color.Purple)
                    .Build());
                await arg.Player.SkipAsync();
            }
            else
            {
                // if playlist empty
                await arg.Player.TextChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithTitle($"Finished playing {arg.Track}.")
                    .WithDescription($"This was the last track of the playlist.\nUse {PublicFunction.prefix}play or the slash command /play to queue up more songs!")
                    .WithColor(Color.Purple)
                    .Build());
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {

            SlashCommands slashCommands = new(node, _client);
            switch (command.Data.Name)
            {
                case "help":
                    await slashCommands.HelpCommand(command);
                    break;
                case "ping":
                    await slashCommands.PingCommand(command);
                    break;
                case "roll":
                    await slashCommands.RollCommand(command);
                    break;
                case "furry":
                    await slashCommands.FurryCommand(command);
                    break;
                case "femboy":
                    await slashCommands.FemboyCommand(command);
                    break;
                case "gay":
                    await slashCommands.GayCommand(command);
                    break;
                // MUSIC BOT
                case "join":
                    await slashCommands.JoinCommand(command);
                    break;
                case "leave":
                    await slashCommands.LeaveCommand(command);
                    break;
                case "play":
                    await slashCommands.PlayCommand(command);
                    break;
                case "pause":
                    await slashCommands.PauseCommand(command);
                    break;
                case "resume":
                    await slashCommands.ResumeCommand(command);
                    break;
                case "stop":
                    await slashCommands.StopCommand(command);
                    break;
                case "skip":
                    await slashCommands.SkipCommand(command);
                    break;
                case "nowplaying":
                    await slashCommands.NowPlaying(command);
                    break;
                case "queue":
                    await slashCommands.Queue(command);
                    break;
                case "shuffle":
                    await slashCommands.Shuffle(command);
                    break;
            }
        }

        // Logging
        private Task Client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        // Command init
        public void RegisterCommandsAsync()
        {
            ReactionHandler reactionHandler = new(_client);
            FiregatorHandler firegatorHandler = new(_client);
            MessageEditedHandler editedHandler = new(_client);
            MessageDeleteHandler deleteHandler = new(_client);
            MessageAddedHandler messageHandler = new(_client, _commands, _services);
            FireGatorTracker FireGator = new();
            _client.ReactionAdded += reactionHandler.HandleReactionAsync;
            _client.ReactionRemoved += reactionHandler.HandleReactionAsync;
            _client.ReactionsCleared += reactionHandler.HandleReactionClearAsync;
            _client.MessageReceived += messageHandler.HandleCommandAsync;
            _client.MessageDeleted += deleteHandler.HandleDeleteAsync;
            _client.MessageUpdated += editedHandler.HandleEditAsync;
            FireGator.Start();
            FireGator.OnThursday += firegatorHandler.HandleFiregatorAsync;
        }
    }
}