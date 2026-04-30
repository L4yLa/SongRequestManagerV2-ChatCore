// Modified: 2026-04-02
// Changes:
//   - Replaced CatCore dependencies with ChatCore equivalents
//   - Added `using ChatCore.Utilities`
//   - Added Badges property
//   - Added ToJson() method
//   - Added property default values
using ChatCore.Interfaces;
using ChatCore.Utilities;
using System;

namespace SongRequestManagerV2.Models
{
    internal class GenericChatUser : IChatUser
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Color { get; set; } = "";
        public bool IsBroadcaster { get; set; }
        public bool IsModerator { get; set; }
        public IChatBadge[] Badges { get; set; } = new IChatBadge[0];

        public GenericChatUser(string json) { }
        public GenericChatUser() { }

        public JSONObject ToJson()
        {
            var obj = new JSONObject();
            obj.Add(nameof(Id), new JSONString(Id ?? ""));
            obj.Add(nameof(UserName), new JSONString(UserName ?? ""));
            obj.Add(nameof(DisplayName), new JSONString(DisplayName ?? ""));
            obj.Add(nameof(Color), new JSONString(Color ?? ""));
            obj.Add(nameof(IsBroadcaster), new JSONBool(IsBroadcaster));
            obj.Add(nameof(IsModerator), new JSONBool(IsModerator));
            return obj;
        }
    }
}
