using GenericMusicClient;
using GenericMusicClient.Model;
using Kook.WebSocket;
using KookVoice.Stream;

namespace KookVoice.Music {
    public static class QQMusic {
        private static MusicClient musicClient = new(PlatformType.QQ);
        public static async Task Search(WebSocketConnect connect, SocketMessage message, SocketTextChannel channel) {
            string msg = message.Content;
            if (msg == null || !msg.StartsWith('/')) return;

            string[] m = msg[1..].Split(' ');
            if (new[] {
                    "search"
                }.Contains(m[0]) && m.Length >= 2) {
                List<SongInfo> result = await musicClient.GetByName(string.Join(' ', m.ToList().Skip(1)));
                string s = string.Join("\n", result.Select(x => x.Name + " " + x.Author[0] + " " + x.DirectUrl));
                await channel.SendTextAsync(s);
            }
            if (new[] {
                    "play"
                }.Contains(m[0]) && m.Length >= 2) {
                await connect.Connect(7990970202733644, (rtcpUrl) => {
                    FfmpegHelper.PlayMusic(rtcpUrl, m[1]);
                });
            }
        }
    }
}
