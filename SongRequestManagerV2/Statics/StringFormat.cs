using System.Text;

namespace SongRequestManagerV2.Statics
{
    public class StringFormat
    {
        public static StringBuilder AddSongToQueueText { get; } = new StringBuilder("Request %songName% %songSubName%/%levelAuthorName% %Rating% (%key%) added to queue.");
        public static StringBuilder LookupSongDetail { get; } = new StringBuilder("%songName% %songSubName%/%levelAuthorName% %Rating% (%key%)");
        public static StringBuilder BsrSongDetail { get; } = new StringBuilder("%songName% %songSubName%/%levelAuthorName% %Rating% (%key%)");
        public static StringBuilder LinkSonglink { get; } = new StringBuilder("%songName% %songSubName%/%levelAuthorName% %Rating% (%key%) %BeatsaverLink%");
        public static StringBuilder NextSonglink { get; } = new StringBuilder("%songName% %songSubName%/%levelAuthorName% %Rating% (%key%) requested by %user% is next.");
        // [2026-09-08] %Difficulty% は !difficulty の内容。
        // 未指定時は空文字になり、改行も含めて値側で生成される。
        public static StringBuilder SongHintText { get; } = new StringBuilder("Requested by %user%%LF%Status: %Status%%Info%%Difficulty%%LF%%LF%<size=60%>Request Time: %RequestTime%</size>");
        public static StringBuilder QueueTextFileFormat { get; } = new StringBuilder("%songName%%LF%");         // Don't forget to include %LF% for these. 
        public static StringBuilder QueueListRow2 { get; } = new StringBuilder("%levelAuthorName% (%key%) <color=white>%songlength%</color>");
        public static StringBuilder BanSongDetail { get; } = new StringBuilder("Blocking %songName%/%levelAuthorName% (%key%)");
        public static StringBuilder QueueListFormat { get; } = new StringBuilder("%songName% (%key%)");
        public static StringBuilder HistoryListFormat { get; } = new StringBuilder("%songName% (%key%)");
        // [2026-09-08] 難易度指定必須設定が有効で、難易度が未指定のときに送る案内。
        public static StringBuilder DifficultyRequiredText { get; } = new StringBuilder("@%user% Please specify difficulty. Use !d <difficulty or custom label>");
        public static StringBuilder AddSortOrder { get; } = new StringBuilder("-rating +id");
        public static StringBuilder LookupSortOrder { get; } = new StringBuilder("-rating +id");
        public static StringBuilder AddSongsSortOrder { get; } = new StringBuilder("-rating +id");
    }
}