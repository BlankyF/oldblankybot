using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Node;
using Victoria.Player;
using Victoria.Responses.Search;
using static blankyBot.PublicFunction;

namespace blankyBot.Commands
{
    public class ResourcesCommands
    {

        private readonly LavaNode _lavaNode;
        private readonly DiscordSocketClient _client;

        public ResourcesCommands(LavaNode lavaNode, DiscordSocketClient client)
        {
            _lavaNode = lavaNode;
            _client = client;
        }
        public readonly EmbedBuilder embedHelp = new EmbedBuilder()
            .WithTitle("Commands list:")
            .AddField($"{prefix}femboy", "Display the seeded femboy percentage rating of the a user. Can accept one paramater.")
            .AddField($"{prefix}furry", "Display the seeded furry percentage rating of the a user. Can accept one paramater.")
            .AddField($"{prefix}gay", "Display the seeded gay percentage rating of the a user. Can accept one paramater.")
            .AddField($"{prefix}help", "Displays help related to the bot!")
            .AddField($"{prefix}roll", "Rolls the dice. Ex: /roll 2d6+2")
            .AddField($"{prefix}ping", "Replies with the ping of the bot")
            .WithFooter(footer => footer.Text = "Page 1 out of 1.")
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();

        public Embed RollCommand(string param)
        {

            EmbedBuilder embed = new();
            Embed embedResult;
            string paramFormatted = "";
            try
            {
                foreach (char item in param)
                {
                    if (item == '+' || item == '-' || item == '*')
                    {
                        paramFormatted += $" {item} ";
                    }
                    else
                    {
                        paramFormatted += item;
                    }
                }
                string[] listParams = paramFormatted.Split(' ');
                for (int indexParam = 0; indexParam < listParams.Length; indexParam++)
                {
                    if (listParams[indexParam].Contains('d'))
                    {
                        string[] diceParams = listParams[indexParam].Split("d");
                        if (diceParams[0] == "") diceParams[0] = "1";
                        if (Convert.ToInt32(diceParams[0]) > 100) throw new ArgumentException("You can roll a dice 100 max");
                        if (Convert.ToInt32(diceParams[0]) < 1 || Convert.ToInt32(diceParams[0]) > 100) throw new ArgumentException("The dice can only have 1 to 100 faces max!");
                        Random getrandom = new();
                        string result = "(";
                        for (int i = 0; i < Convert.ToInt32(diceParams[0]); i++)
                            result += $"{getrandom.Next(1, Convert.ToInt32(diceParams[1])+1)}+";
                        result = result.Remove(result.Length - 1);
                        listParams[indexParam] = $"{result})";
                    }
                }
                string resultCmd = "";
                foreach (var item in listParams)
                {
                    resultCmd += $"{item}";
                }
                string value = new DataTable().Compute(resultCmd, null).ToString();
                embedResult = embed.WithDescription($"Your roll is {value}.\n{resultCmd}")
                    .WithColor(Color.Purple)
                    .Build();
            }
            catch (Exception)
            {
                embedResult =  embed.WithDescription($"Error ! Bad formatting")
                    .WithColor(Color.Purple)
                    .Build();
            }
            return embedResult;
        }

        public Embed RandomCommand(SocketUser user, string name, ulong randomModifier)
        {
            Random rnd;
            // id function
            rnd = new Random((int)(user.Id + randomModifier % 10000000));
            EmbedBuilder embed = PostEmbedPercent(user.Username, rnd.Next(101), name);
            return embed.WithAuthor(user.Username, user.GetAvatarUrl()).Build();
        }

        public Embed RandomTextCommand(string user, string name, int randomModifier)
        {
            Random rnd;
            rnd = new Random(randomModifier);
            return PostEmbedPercent(user, rnd.Next(101), name).Build();
        }

        // MUSIC BOT RESSOURCES

        public Embed Join(ulong guildId, SocketUser user, ISocketMessageChannel channel)
        {
            EmbedBuilder embed = new();
            IGuild guild = _client.GetGuild(guildId);
            if (_lavaNode.HasPlayer(guild))
            {
                return embed.WithDescription("Error : I'm already connected to a voice channel!")
                    .WithColor(Color.Red)
                    .Build();
            }

            var voiceState = user as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                return embed.WithDescription("Error : You must be connected to a voice channel!")
                    .WithColor(Color.Red)
                    .Build();
            }

            try
            {
                _lavaNode.JoinAsync(voiceState.VoiceChannel, channel as ITextChannel);
                return embed.WithDescription($"Joined the channel : {voiceState.VoiceChannel.Name}!")
                    .WithColor(Color.Purple)
                    .WithAuthor(user)
                    .WithTitle("Bot joined the channel!")
                    .Build();
            }
            catch (Exception exception)
            {
                return embed.WithDescription($"Error : {exception.Message}")
                    .WithColor(Color.Red)
                    .Build();
            }

        }
        public Embed Leave(ulong guildId, SocketUser user)
        {
            EmbedBuilder embed = new();
            IGuild guild = _client.GetGuild(guildId);
            if (!_lavaNode.TryGetPlayer(guild, out var player))
            {
                return embed.WithDescription("Error : I'm not connected to any voice channels!")
                    .WithColor(Color.Red)
                    .Build();
            }

            var voiceChannel = (user as IVoiceState).VoiceChannel ?? player.VoiceChannel;
            if (voiceChannel == null)
            {
                return embed.WithDescription("Error : Not sure which voice channel to disconnect from.")
                    .WithColor(Color.Red)
                    .Build();
            }

            try
            {
                _lavaNode.LeaveAsync(voiceChannel);
                return embed.WithDescription($"I've left the channel \"{voiceChannel.Name}\"!")
                    .WithColor(Color.Purple)
                    .WithAuthor(user)
                    .WithTitle("Bot left!")
                    .Build();
            }
            catch (Exception exception)
            {
                return embed.WithDescription($"Error : {exception.Message}")
                    .WithColor(Color.Red)
                    .Build();
            }
        }
        public async Task<Embed> Play(ulong guildId, SocketUser user, ISocketMessageChannel channel, string searchQuery, bool isShuffle = false)
        {
            EmbedBuilder embed = new();
            IGuild guild = _client.GetGuild(guildId);
            ITextChannel iChannel = channel as ITextChannel;

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return embed.WithDescription($"Error : Please provide search terms.")
                    .WithColor(Color.Red)
                    .Build();
            }

            if (!_lavaNode.TryGetPlayer(guild, out var player))
            {
                var voiceState = user as IVoiceState;
                if (voiceState?.VoiceChannel == null)
                {
                    return embed.WithDescription($"Error : You must be connected to a voice channel!")
                        .WithColor(Color.Red)
                        .Build();
                }

                try
                {
                    player = await _lavaNode.JoinAsync(voiceState.VoiceChannel, iChannel);
                    await iChannel.SendMessageAsync(embed: embed.WithDescription($"Joined {voiceState.VoiceChannel.Name}!")
                        .WithColor(Color.Purple)
                        .Build());
                }
                catch (Exception exception)
                {
                    return embed.WithDescription($"Error : {exception.Message}")
                        .WithColor(Color.Red)
                        .Build();
                }
            }

            var searchResponse = await _lavaNode.SearchAsync(Uri.IsWellFormedUriString(searchQuery, UriKind.Absolute) ? SearchType.Direct : SearchType.YouTube, searchQuery);
            if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
            {
                return embed.WithDescription($"Error : I wasn't able to find anything for `{searchQuery}`.")
                    .WithColor(Color.Red)
                    .Build();
            }
            Embed embedResult;
            if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
            {
                List<LavaTrack> playlist = searchResponse.Tracks.ToList();
                if (isShuffle)
                {
                    playlist = playlist.OrderBy(x => Random.Shared.Next()).ToList();
                }
                player.Vueue.Enqueue(playlist);
                string artwork = await playlist.FirstOrDefault().FetchArtworkAsync();
                embedResult = embed.WithDescription($"{user.Username} enqueued [{playlist.Count} songs.]({searchQuery})")
                    .WithColor(Color.Purple)
                    .WithAuthor(user)
                    .WithTitle("Music Added!")
                    .WithImageUrl(artwork)
                    .Build();
            }
            else
            {
                var track = searchResponse.Tracks.FirstOrDefault();
                player.Vueue.Enqueue(track);
                string artwork = await track.FetchArtworkAsync();
                embedResult = embed.WithDescription($"{user.Username} enqueued :\n[{track?.Title}]({track?.Url})")
                    .WithColor(Color.Purple)
                    .WithAuthor(user)
                    .WithTitle("Music Added!")
                    .WithImageUrl(artwork)
                    .Build();
            }

            if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
            {
                return embedResult;
            }

            player.Vueue.TryDequeue(out var lavaTrack);
            await player.PlayAsync(lavaTrack);
            return embedResult;
        }
        public async Task<Embed> Pause(ulong guildId, SocketUser user)
        {
            EmbedBuilder embed = new();
            IGuild guild = _client.GetGuild(guildId);

            if (!_lavaNode.TryGetPlayer(guild, out var player))
            {
                return embed.WithDescription("Error : I'm not connected to a voice channel.")
                    .WithColor(Color.Red)
                    .WithAuthor(user)
                    .Build();
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                return embed.WithDescription("Error : I cannot pause when I'm not playing anything!")
                    .WithColor(Color.Red)
                    .WithAuthor(user)
                    .Build();
            }

            try
            {
                await player.PauseAsync();
                return embed.WithDescription($"Paused: {player.Track.Title}")
                    .WithColor(Color.Purple)
                    .WithAuthor(user)
                    .WithTitle("Music paused!")
                    .Build();
            }
            catch (Exception exception)
            {
                return embed.WithDescription($"Error : {exception.Message}")
                    .WithColor(Color.Red)
                    .Build();
            }
        }
        public async Task<Embed> Resume(ulong guildId, SocketUser user)
        {
            EmbedBuilder embed = new();
            IGuild guild = _client.GetGuild(guildId);
            if (!_lavaNode.TryGetPlayer(guild, out var player))
            {
                return embed.WithDescription("Error : I'm not connected to a voice channel.")
                    .WithColor(Color.Red)
                    .Build();
            }

            if (player.PlayerState != PlayerState.Paused)
            {
                return embed.WithDescription("Error : I cannot resume when I'm not paused!")
                    .WithColor(Color.Red)
                    .Build();
            }

            try
            {
                await player.ResumeAsync();
                return embed.WithDescription($"Resumed: {player.Track.Title}")
                    .WithColor(Color.Purple)
                    .WithAuthor(user)
                    .WithTitle("Music resumed!")
                    .Build();
            }
            catch (Exception exception)
            {
                return embed.WithDescription($"Error : {exception.Message}")
                    .WithColor(Color.Red)
                    .Build();
            }
        }
        public async Task<Embed> Stop(ulong guildId, SocketUser user)
        {
            EmbedBuilder embed = new();
            IGuild guild = _client.GetGuild(guildId);
            if (!_lavaNode.TryGetPlayer(guild, out var player))
            {
                return embed.WithDescription($"Error : I'm not connected to a voice channel.")
                    .WithColor(Color.Red)
                    .Build();
            }

            if (player.PlayerState == PlayerState.Stopped)
            {
                return embed.WithDescription($"Error : Woaaah there, I can't stop the stopped forced.")
                    .WithColor(Color.Red)
                    .Build();
            }

            try
            {
                await player.StopAsync();
                return embed.WithColor(Color.Purple)
                    .WithAuthor(user)
                    .WithTitle("The music that was playing was skipped!")
                    .Build();
            }
            catch (Exception exception)
            {
                return embed.WithDescription($"Error : {exception.Message}")
                    .WithColor(Color.Red)
                    .Build();
            }
        }
        public async Task<Embed> Skip(ulong guildId, SocketUser user)
        {
            EmbedBuilder embed = new();
            IGuild guild = _client.GetGuild(guildId);

            if (!_lavaNode.TryGetPlayer(guild, out var player))
            {
                return embed.WithDescription($"Error : I'm not connected to a voice channel.")
                    .WithColor(Color.Red)
                    .Build();
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                return embed.WithDescription($"Error : I can't skip when nothing is playing.")
                    .WithColor(Color.Red)
                    .Build();
            }
            try
            {
                string skippedTrack = player.Track.Title;
                if (player.Vueue.Count > 0)
                {
                    var embedResult = embed.WithDescription($"Skipped: {skippedTrack}\nNow Playing: {player.Vueue.FirstOrDefault().Title}")
                        .WithColor(Color.Purple)
                        .WithAuthor(user)
                        .WithTitle("Song skipped!")
                        .Build();
                    await player.SkipAsync();
                    return embedResult;
                } 
                else
                {
                    await player.StopAsync();
                    return embed.WithDescription($"Skipped: {skippedTrack}")
                        .WithColor(Color.Purple)
                        .WithAuthor(user)
                        .WithTitle("Song skipped!")
                        .Build();
                }
            }
            catch (Exception exception)
            {
                return embed.WithDescription($"Error : {exception.Message}")
                    .WithColor(Color.Red)
                    .Build();
            }
        }
        public async Task<Embed> NowPlaying(ulong guildId, SocketUser user)
        {
            EmbedBuilder embed = new();
            IGuild guild = _client.GetGuild(guildId);

            if (!_lavaNode.TryGetPlayer(guild, out var player))
            {
                return embed.WithDescription($"Error : I'm not connected to a voice channel.")
                    .WithColor(Color.Red)
                    .Build();
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                return embed.WithDescription($"Error : I'm not playing any tracks.")
                    .WithColor(Color.Red)
                    .Build();
            }

            LavaTrack track = player.Track;
            var artwork = await track.FetchArtworkAsync();
            string time = $"{track.Position:mm\\:ss}/{track.Duration:mm\\:ss}";
            return embed.WithAuthor(track.Author, user.GetAvatarUrl(), track.Url)
                .WithTitle($"Now Playing: ")
                .WithDescription($"[{track.Title}]({track.Url})")
                .WithImageUrl(artwork)
                .WithFooter(time)
                .Build();
        }

        public Embed Shuffle(ulong guildId, SocketUser user)
        {
            EmbedBuilder embed = new();
            IGuild guild = _client.GetGuild(guildId);

            if (!_lavaNode.TryGetPlayer(guild, out var player))
            {
                return embed.WithDescription($"Error : I'm not connected to a voice channel.")
                    .WithColor(Color.Red)
                    .Build();
            }

            if (player.Vueue.Count == 0)
            {
                return embed.WithDescription($"Error : Queue is empty.")
                    .WithColor(Color.Red)
                    .Build();
            }
            player.Vueue.Shuffle();

            string result = "";
            for (int trackNumber = 0 ; trackNumber < player.Vueue.Count; trackNumber++)
            {
                if (trackNumber >= player.Vueue.Count)
                {
                    break;
                }
                string title = player.Vueue.ElementAt(trackNumber).Title;
                if (title.Length > 80)
                {
                    title = $"{title[..77]}...";
                }
                result += $"\n{trackNumber + 1} : [{title}]({player.Vueue.ElementAt(trackNumber).Url}) [{player.Vueue.ElementAt(trackNumber).Duration:mm\\:ss}]";
            }
            return embed.WithAuthor(user)
                .WithTitle($"Queue shuffled! ")
                .WithDescription(result)
                .Build();
        }
        public Embed Queue(ulong guildId, SocketUser user, int pageNumber)
        {
            EmbedBuilder embed = new();
            IGuild guild = _client.GetGuild(guildId);

            if (!_lavaNode.TryGetPlayer(guild, out var player))
            {
                return embed.WithDescription($"Error : I'm not connected to a voice channel.")
                    .WithColor(Color.Red)
                    .Build();
            }
            if (player.Vueue.Count == 0)
            {
                return embed.WithDescription($"Error : Queue is empty.")
                    .WithColor(Color.Red)
                    .Build();
            }
            string result = "";
            for (int trackNumber = 0 + ( ( pageNumber - 1 ) * 10 ); trackNumber < 10 + ( ( pageNumber - 1 ) * 10); trackNumber++)
            {
                if ( trackNumber >= player.Vueue.Count )
                {
                    break;
                }
                string title = player.Vueue.ElementAt(trackNumber).Title;
                if (title.Length > 60)
                {
                    title = $"{title[..57]}...";
                }
                result += $"\n{ trackNumber + 1 } : [{ title }]({ player.Vueue.ElementAt(trackNumber).Url }) [{player.Vueue.ElementAt(trackNumber).Duration:mm\\:ss}]";
            }
            if ( result == "" )
            {
                return embed.WithDescription($"Error : There is nothing on this page!.")
                    .WithColor(Color.Red)
                    .Build();
            }
            int PageTotal = player.Vueue.Count / 10;
            if(player.Vueue.Count % 10 != 0)
            {
                PageTotal++;
            }
            return embed.WithAuthor(user)
                .WithTitle($"List of queued music :")
                .WithDescription(result)
                .WithFooter($"Page { pageNumber }/{ PageTotal }. { player.Vueue.Count } tracks enqueued")
                .Build();
        }

        public static TimeSpan StripMilliseconds(TimeSpan time)
        {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
    }
}
