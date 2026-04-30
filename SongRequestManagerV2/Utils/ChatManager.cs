// Modified: 2026-03-20
// Changes:
//   - Replaced CatCore dependencies with ChatCore (using directives updated)
//   - Renamed CatCoreInstance to ChatCoreInstance
//   - Replaced MultiplexedMessage/MultiplexedChannel with IChatMessage/IChatChannel
//   - Removed CatCore-specific service properties (TwitchChannelManagementService, etc.)
//   - Changed OnChatConnected event to OnJoinChannel
//   - Added StopAllServices() call in Dispose
//   - Removed commented-out code
using ChatCore;
using ChatCore.Interfaces;
using ChatCore.Services;
using ChatCore.Services.Twitch;
using SongRequestManagerV2.Bots;
using SongRequestManagerV2.Configuration;
using SongRequestManagerV2.Interfaces;
using SongRequestManagerV2.Models.Streamer.bot;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using Zenject;

namespace SongRequestManagerV2.Utils
{
    public class ChatManager : IDisposable, IInitializable, IChatManager
    {
        private bool _disposedValue;

        public ChatCoreInstance CoreInstance { get; private set; }
        public ChatServiceMultiplexer MultiplexerInstance { get; private set; }
        public TwitchService TwitchService { get; private set; }
        public IChatChannel Channel { get; private set; }
        public ConcurrentQueue<IChatMessage> RecieveChatMessage { get; } = new ConcurrentQueue<IChatMessage>();
        public ConcurrentQueue<IChatMessage> RecieveGenelicChatMessage { get; } = new ConcurrentQueue<IChatMessage>();
        public ConcurrentQueue<RequestInfo> RequestInfos { get; } = new ConcurrentQueue<RequestInfo>();
        public ConcurrentQueue<string> SendMessageQueue { get; } = new ConcurrentQueue<string>();
        public StreamerBotWebSocketClient WebSocketClient { get; private set; } = new StreamerBotWebSocketClient();

        public void Initialize()
        {
            Logger.Debug("Initialize call");
            try {
                this.CoreInstance = ChatCoreInstance.Create();
                this.MultiplexerInstance = this.CoreInstance.RunAllServices();
                this.MultiplexerInstance.OnTextMessageReceived += this.MultiplexerInstance_OnTextMessageReceived;
                this.MultiplexerInstance.OnJoinChannel += this.MultiplexerInstance_OnJoinChannel;
                this.TwitchService = this.MultiplexerInstance.GetTwitchService();
                this.WebSocketClient.OnReceivedMessage += this.OnWebsocketMessageReceived;
                if (RequestBotConfig.Instance.EnableStreamerBot) {
                    this.WebSocketClient.StartClient();
                }
            }
            catch (Exception e) {
                Logger.Error(e);
            }
        }

        private void MultiplexerInstance_OnJoinChannel(IChatService svc, IChatChannel channel)
        {
            try {
                this.Channel = channel;
            }
            catch (Exception e) {
                Logger.Error(e);
            }
        }

        private void MultiplexerInstance_OnTextMessageReceived(IChatService svc, IChatMessage message)
        {
            this.RecieveChatMessage.Enqueue(message);
        }

        /// <summary>
        /// メッセージを送信キューへ追加します。
        /// </summary>
        /// <param name="message">ストリームサービスへ送信したい文字列</param>
        public void QueueChatMessage(string message)
        {
            this.SendMessageQueue.Enqueue($"{RequestBotConfig.Instance.BotPrefix}{message}");
        }

        private void OnWebsocketMessageReceived(object sender, string message)
        {
            var chatEntity = StreamerbotMessageParser.MessagePaese(message);
            this.RecieveGenelicChatMessage.Enqueue(chatEntity);
        }

        protected virtual async void Dispose(bool disposing)
        {
            if (!this._disposedValue) {
                if (disposing) {
                    Logger.Debug("Dispose call");
                    this.MultiplexerInstance.OnTextMessageReceived -= this.MultiplexerInstance_OnTextMessageReceived;
                    this.MultiplexerInstance.OnJoinChannel -= this.MultiplexerInstance_OnJoinChannel;
                    this.WebSocketClient.OnReceivedMessage -= this.OnWebsocketMessageReceived;
                    await this.WebSocketClient.StopClient();
                    this.CoreInstance.StopAllServices();
                }
                this._disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void SendMessageToStreamerbotServer(string message)
        {
            var jsons = StreamerbotMessageParser.CreateSendCommentActionJson(message);
            foreach (var json in jsons) {
                _ = this.WebSocketClient?.SendAsync(json.ToString());
            }
        }
    }
}
