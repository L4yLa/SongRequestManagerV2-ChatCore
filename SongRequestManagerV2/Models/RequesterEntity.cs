
using ChatCore.Interfaces;
using ChatCore.Utilities;

namespace SongRequestManagerV2.Models
{
    public class RequesterEntity : IChatUser
    {
        public string Id { get; set; } = "unknown";
        public string UserName { get; set; } = "unknown";
        public string DisplayName { get; set; } = "unknown";
        public string Color { get; set; } = "";
        public bool IsBroadcaster { get; set; }
        public bool IsModerator { get; set; }
        public IChatBadge[] Badges { get; set; } = new IChatBadge[0];

        public RequesterEntity()
        {
            this.Id = "unknow";
            this.UserName = "unknow";
            this.DisplayName = "unknown";
            this.Color = "";
            this.Badges = new IChatBadge[0];
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
