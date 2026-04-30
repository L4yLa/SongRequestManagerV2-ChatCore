// Modified: 2026-03-21
// Changes:
//   - Replaced CatCore dependencies with ChatCore (using directives updated)
//   - Added Badges property
//   - Added ToJson() method
//   - Qualified JSON.Parse as SimpleJsons.JSON.Parse
using ChatCore.Interfaces;
using ChatCore.Utilities;
using System;

namespace SongRequestManagerV2.Models.Streamer.bot
{
    internal class StreamerbotChatUser : IChatUser
    {
        public string Id { get; set; }

        public string UserName { get; set; }

        public string DisplayName { get; set; }

        public string Color { get; set; }

        public bool IsBroadcaster { get; set; }

        public bool IsModerator { get; set; }

        public IChatBadge[] Badges { get; set; } = new IChatBadge[0];

        public StreamerbotChatUser(string json)
        {
            try {
                var jsonNode = SimpleJsons.JSON.Parse(json);
                this.Id = jsonNode["userId"]?.Value;
                this.UserName = jsonNode["username"]?.Value;
                this.DisplayName = jsonNode["displayName"]?.Value;
                this.Color = jsonNode["color"]?.Value;
                this.IsBroadcaster = jsonNode["role"]?.AsInt == 4;
                this.IsModerator = jsonNode["role"]?.AsInt == 3;
            }
            catch (Exception e) {
                Logger.Error(e);
            }
        }

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