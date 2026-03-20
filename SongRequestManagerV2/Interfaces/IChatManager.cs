using ChatCore;
using ChatCore.Interfaces;
using ChatCore.Services;
using SongRequestManagerV2.Bots;
using SongRequestManagerV2.Models.Streamer.bot;
using System.Collections.Concurrent;

namespace SongRequestManagerV2.Interfaces
{
    public interface IChatManager
    {
        ChatCoreInstance CoreInstance { get; }
        ChatServiceMultiplexer MultiplexerInstance { get; }
        ConcurrentQueue<IChatMessage> RecieveChatMessage { get; }
        ConcurrentQueue<IChatMessage> RecieveGenelicChatMessage { get; }
        ConcurrentQueue<RequestInfo> RequestInfos { get; }
        ConcurrentQueue<string> SendMessageQueue { get; }
        StreamerBotWebSocketClient WebSocketClient { get; }

        void QueueChatMessage(string message);
        void SendMessageToStreamerbotServer(string message);
    }
}
