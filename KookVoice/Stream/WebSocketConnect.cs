using RankedUtils.Utils;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

namespace KookVoice.Stream {
    public class WebSocketConnect {
        private ClientWebSocket WebSocket = new();
        private string token;
        public string rtcpUrl;

        public WebSocketConnect(string token) {
            this.token = token;
            this.rtcpUrl = "";
        }

        public async Task Connect(ulong channelId, Action<string> callback) {
            string url;
            Random r = new();
            CancellationToken cancelToken = CancellationToken.None;
            Logger.Info("开始进行Kook语音连接");
            using (HttpClient client = new()) {
                Logger.Info("1.获取WS链接");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bot " + token);
                HttpResponseMessage res = await client.GetAsync($"https://www.kaiheila.cn/api/v3/gateway/voice?channel_id={channelId}", cancelToken);
                if (res.StatusCode != HttpStatusCode.OK) {
                    Logger.Error("获取WS链接失败");
                    return;
                }
                string json = await res.Content.ReadAsStringAsync(cancelToken);
                Logger.Info(json);
                JsonNode? obj = JsonNode.Parse(json);
                if (obj is not JsonObject || !obj.AsObject().ContainsKey("data")) {
                    Logger.Error("获取WS链接失败");
                    return;
                }
                JsonNode obj1 = obj.AsObject()["data"] ?? throw new InvalidOperationException();
                if (obj1 is not JsonObject || !obj1.AsObject().ContainsKey("gateway_url")) {
                    Logger.Error("获取WS链接失败");
                    return;
                }
                //如果这里加个.Replace("\\u0026", "&")，直接啥都接收不到
                url = obj1.AsObject()["gateway_url"]?.ToJsonString().Replace("\"", "") ?? throw new InvalidOperationException();
                Logger.Info("WebSocket Url: " + url);
            }
            Logger.Info("WebSocket Connect");
            this.WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            await this.WebSocket.ConnectAsync(new Uri(url), new CancellationToken(false));
            Logger.Info("Stage 1");
            string stage1 = "{\"request\":true,\"id\":" + r.Next(1000000, 10000000) + ",\"method\":\"getRouterRtpCapabilities\",\"data\":{}}";
            Logger.Message(stage1);
            await this.WebSocket.SendAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(stage1)), WebSocketMessageType.Text, false, cancelToken);
            var buffer = new byte[1024];
            WebSocketReceiveResult received = await this.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);
            int stage = 1;
            while (!this.WebSocket.CloseStatus.HasValue) {
                string text = Encoding.UTF8.GetString(buffer, 0, received.Count);
                Logger.Info(text);
                switch (stage) {
                    case 1:
                    {
                        Logger.Info("Stage 2");
                        string stage2 = "{\"data\":{\"displayName\":\"\"},\"id\":" + r.Next(1000000, 10000000) + ",\"method\":\"join\",\"request\": true}";
                        Logger.Message(stage2);
                        await this.WebSocket.SendAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(stage2)), WebSocketMessageType.Text, false, cancelToken);
                        stage++;
                        break;
                    }
                    case 2:
                    {
                        Logger.Info("Stage 3");
                        string stage3 = "{\"data\":{\"comedia\":true,\"rtcpMux\":false,\"type\":\"plain\"},\"id\":" + r.Next(1000000, 10000000) + ",\"method\":\"createPlainTransport\",\"request\":true}";
                        Logger.Message(stage3);
                        await this.WebSocket.SendAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(stage3)), WebSocketMessageType.Text, false, cancelToken);
                        stage++;
                        break;
                    }
                    case 3:
                    {
                        Logger.Info("Stage 4");
                        JsonNode? obj = JsonNode.Parse(text);
                        JsonObject data = obj?.AsObject()["data"]?.AsObject() ?? throw new InvalidOperationException();
                        this.rtcpUrl = $"rtp://{data["ip"]?.ToJsonString().Replace("\"", "")}:{data["port"]?.ToJsonString().Replace("\"", "")}?rtcpport={data["rtcpPort"]?.ToJsonString().Replace("\"", "")}";
                        string stage4 = "{\"data\":{\"appData\":{},\"kind\":\"audio\",\"peerId\":\"\",\"rtpParameters\":{\"codecs\":[{\"channels\":2,\"clockRate\":48000,\"mimeType\":\"audio/opus\",\"parameters\":{\"sprop-stereo\":1},\"payloadType\":100}],\"encodings\":[{\"ssrc\":1357}]},\"transportId\":\"" +
                                        data["id"]?.ToJsonString().Replace("\"", "") + "\"},\"id\"" + r.Next(1000000, 10000000) + ",\"method\":\"produce\",\"request\":true}";
                        Logger.Message(stage4);
                        await this.WebSocket.SendAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(stage4)), WebSocketMessageType.Text, false, cancelToken);
                        stage++;
                        break;
                    }
                    case 4:
                    {
                        Logger.Info("Complete rtcp url receive");
                        Logger.Info("Rtcp url: " + this.rtcpUrl);
                        stage++;
                        callback(this.rtcpUrl);
                        break;
                    }
                }
                received = await this.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);
            }
        }
    }
}
