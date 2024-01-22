using Kook;
using Kook.WebSocket;
using KookVoice.Music;
using KookVoice.Stream;
using RankedUtils;
using RankedUtils.Utils;

namespace KookVoice {
    internal class Program {
        public static BotConfig config = new(Environment.CurrentDirectory + @"\main.json");
        private readonly KookSocketClient client;
        private WebSocketConnect connect;

        public static Task Main() {
            return new Program().MainAsync();
        }

        private static Task Log(LogMessage msg) {
            Logger.Info($"[{msg.Source}] {msg}");
            return Task.CompletedTask;
        }

        private Program() {
            this.client = new KookSocketClient(new() {
                AlwaysDownloadVoiceStates = true,
                AlwaysDownloadUsers = true
            });
            Config.StartAutoSave();
        }

        private async Task MainAsync() {
            this.client.Log += Log;
            this.client.MessageReceived += this.Client_MessageReceived;

            if (config.kookBotToken == "") {
                Logger.Error("未找到token");
                Environment.Exit(0);
            }
            this.connect = new(config.kookBotToken);
            await this.client.LoginAsync(TokenType.Bot, config.kookBotToken);
            await this.client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }

        private async Task Client_MessageReceived(SocketMessage message, SocketGuildUser user, SocketTextChannel channel) {
            if (message.Author.Id == this.client.CurrentUser.Id) return;
            if (message.Content == "/ping") await message.AddReactionAsync(new Emoji("✅"));

            await Music.QQMusic.Search(this.connect, message, channel);
        }
    }
}
