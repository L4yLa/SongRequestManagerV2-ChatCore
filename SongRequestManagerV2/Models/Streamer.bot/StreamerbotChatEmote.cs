// Modified: 2026-03-20
// Changes:
//   - Replaced CatCore dependencies with ChatCore (using directives updated)
//   - Added ChatCore.Models and ChatCore.Utilities using directives
//   - Added Uri, IsAnimated, Type, and UVs properties (IChatEmote interface compliance)
//   - Renamed Url property to Uri and Animated to IsAnimated
//   - Added ToJson() method
using ChatCore.Interfaces;
using ChatCore.Models;
using ChatCore.Utilities;
using System;

namespace SongRequestManagerV2.Models.Streamer.bot
{
    internal class StreamerbotChatEmote : IChatEmote
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Uri { get; set; } = "";
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public bool IsAnimated { get; set; }
        public EmoteType Type { get; set; } = EmoteType.SingleImage;
        public ImageRect UVs { get; set; }

        public JSONObject ToJson()
        {
            var obj = new JSONObject();
            obj.Add(nameof(Id), new JSONString(Id ?? ""));
            obj.Add(nameof(Name), new JSONString(Name ?? ""));
            obj.Add(nameof(Uri), new JSONString(Uri ?? ""));
            obj.Add(nameof(StartIndex), new JSONNumber(StartIndex));
            obj.Add(nameof(EndIndex), new JSONNumber(EndIndex));
            obj.Add(nameof(IsAnimated), new JSONBool(IsAnimated));
            obj.Add(nameof(Type), new JSONString(Type.ToString()));
            return obj;
        }
    }
}
