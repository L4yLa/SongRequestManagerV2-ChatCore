// Modified: 2026-04-02
// Changes:
//   - Replaced CatCore dependencies with ChatCore equivalents
//   - Added Channel property
//   - Added Metadata property (ReadOnlyDictionary<string, string>)
//   - Added property default values
using ChatCore.Interfaces;
using ChatCore.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SongRequestManagerV2.Models
{
    public class MessageEntity : IChatMessage
    {
        public string Id { get; set; } = "";
        public bool IsSystemMessage { get; set; }
        public bool IsActionMessage { get; set; }
        public bool IsHighlighted { get; set; }
        public bool IsPing { get; set; }
        public bool IsMentioned { get; set; }
        public string Message { get; set; } = "";
        public IChatUser Sender { get; set; }
        public IChatChannel Channel { get; set; }
        public IChatEmote[] Emotes { get; set; }
        public ReadOnlyDictionary<string, string> Metadata { get; set; }

        public MessageEntity()
        {
            this.Id = "";
            this.Message = "";
            this.Sender = new GenericChatUser();
            this.Emotes = new IChatEmote[0];
            this.Metadata = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        }

        public JSONObject ToJson()
        {
            return new JSONObject();
        }
    }
}
