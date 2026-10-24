// Created: 2026-09-08
// Purpose:
//   チャットで指定された難易度文字列を、BeatSaver のマップ JSON 上の
//   difficulty / characteristic / label へ解決する。
//   BeatSaver のレスポンスは versions[].diffs[] に difficulty / characteristic に加えて
//   label (マップ側の _difficultyLabel) を持つため、カスタム難易度ラベルによる指定にも対応する。
//   (BeatSaver 本体 modules/shared/src/commonMain/kotlin/io/beatmaps/api/maps.kt の
//    MapDifficulty.label を根拠とする)
// Modified: 2026-09-09
// Changes:
//   - 照合を「パターンに対する大文字小文字区別なしの完全一致」のみに変更。
//     記号除去による正規化と前方一致を廃止した。
//   - 標準難易度のエイリアスより先に判定されないよう、標準名 → カスタムラベルの順に変更。
using SongRequestManagerV2.SimpleJsons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SongRequestManagerV2.Bots
{
    /// <summary>
    /// 難易度指定文字列の解決を行う。
    /// </summary>
    public static class DifficultyResolver
    {
        /// <summary>既定で優先する characteristic。</summary>
        public const string STANDARD_CHARACTERISTIC = "Standard";

        /// <summary>BeatSaver 表記の標準難易度 (表示順)。</summary>
        private static readonly string[] s_difficultyOrder = { "Easy", "Normal", "Hard", "Expert", "ExpertPlus" };

        /// <summary>
        /// 標準難易度のエイリアス表。照合は大文字小文字を区別しない完全一致。
        /// 1 文字のエイリアス (e / n / h) は Easy と Expert 等で曖昧になるため採用しない。
        /// </summary>
        private static readonly Dictionary<string, string> s_aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "easy", "Easy" }, { "ez", "Easy" }, { "es", "Easy" }, { "イージー", "Easy" },
            { "normal", "Normal" }, { "norm", "Normal" }, { "nor", "Normal" }, { "nml", "Normal" }, { "ノーマル", "Normal" },
            { "hard", "Hard" }, { "hrd", "Hard" }, { "hd", "Hard" }, { "ハード", "Hard" },
            { "expert", "Expert" }, { "exp", "Expert" }, { "ex", "Expert" }, { "エキスパート", "Expert" },
            { "expertplus", "ExpertPlus" }, { "expert+", "ExpertPlus" }, { "exp+", "ExpertPlus" },
            { "ex+", "ExpertPlus" }, { "e+", "ExpertPlus" }, { "ep", "ExpertPlus" },
            { "エキスパートプラス", "ExpertPlus" }, { "エキプラ", "ExpertPlus" },
        };

        /// <summary>
        /// 解決結果。
        /// </summary>
        public class ResolvedDifficulty
        {
            /// <summary>BeatSaver 表記の難易度 (Easy / Normal / Hard / Expert / ExpertPlus)。</summary>
            public string Difficulty { get; set; }
            /// <summary>characteristic (Standard / OneSaber / 360Degree など)。</summary>
            public string Characteristic { get; set; }
            /// <summary>カスタム難易度ラベル。無い場合は空文字。</summary>
            public string Label { get; set; }
            /// <summary>ゲーム表記の難易度名 (ExpertPlus → Expert+)。</summary>
            public string DisplayName => ToDisplayName(this.Difficulty);
            /// <summary>ラベル付きの表示名。</summary>
            public string FullDisplayName => string.IsNullOrEmpty(this.Label)
                ? this.DisplayName
                : $"{this.Label} ({this.DisplayName})";
        }

        private class DiffEntry
        {
            public string Difficulty;
            public string Characteristic;
            public string Label;
        }

        /// <summary>
        /// 指定可能な標準難易度のエイリアス一覧 (ヘルプ表示用)。
        /// </summary>
        public static string DescribeStandardAliases()
        {
            var builder = new StringBuilder();
            foreach (var difficulty in s_difficultyOrder) {
                if (0 < builder.Length) {
                    _ = builder.Append(" / ");
                }
                _ = builder.Append(string.Join(",", s_aliases.Where(x => x.Value == difficulty).Select(x => x.Key)));
            }
            return builder.ToString();
        }

        /// <summary>
        /// 難易度指定文字列を解決する。
        /// 照合順は 標準難易度エイリアス → カスタム難易度ラベル。いずれも
        /// 大文字小文字を区別しない完全一致のみで、部分一致は行わない。
        /// </summary>
        /// <param name="songVersion">BeatSaver の versions[] 要素</param>
        /// <param name="input">チャットで指定された文字列</param>
        /// <param name="resolved">解決結果</param>
        /// <returns>解決できた場合 true</returns>
        public static bool TryResolve(JSONObject songVersion, string input, out ResolvedDifficulty resolved)
        {
            resolved = null;
            if (songVersion == null || string.IsNullOrEmpty(input)) {
                return false;
            }
            var entries = GetEntries(songVersion);
            if (entries.Count == 0) {
                return false;
            }

            var trimmed = input.Trim();
            if (trimmed.Length == 0) {
                return false;
            }

            // 1. 標準難易度名 / エイリアスの完全一致
            DiffEntry hit = null;
            if (s_aliases.TryGetValue(trimmed, out var standard)) {
                hit = FindBest(entries, e => string.Equals(e.Difficulty, standard, StringComparison.OrdinalIgnoreCase));
            }

            // 2. カスタム難易度ラベルの完全一致
            if (hit == null) {
                hit = FindBest(entries, e => !string.IsNullOrEmpty(e.Label)
                    && string.Equals(e.Label.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
            }

            if (hit == null) {
                return false;
            }

            resolved = new ResolvedDifficulty
            {
                Difficulty = hit.Difficulty,
                Characteristic = string.IsNullOrEmpty(hit.Characteristic) ? STANDARD_CHARACTERISTIC : hit.Characteristic,
                Label = hit.Label ?? ""
            };
            return true;
        }

        /// <summary>
        /// 指定可能な難易度の一覧を、チャット通知用の短い文字列にする。
        /// Standard が存在する場合は Standard のみを列挙する。
        /// </summary>
        public static string DescribeAvailable(JSONObject songVersion)
        {
            var entries = GetEntries(songVersion);
            if (entries.Count == 0) {
                return "";
            }
            var target = entries.Where(e => string.Equals(e.Characteristic, STANDARD_CHARACTERISTIC, StringComparison.OrdinalIgnoreCase)).ToList();
            if (target.Count == 0) {
                target = entries;
            }
            var builder = new StringBuilder();
            foreach (var entry in target.OrderBy(e => Array.IndexOf(s_difficultyOrder, e.Difficulty))) {
                if (0 < builder.Length) {
                    _ = builder.Append(" / ");
                }
                _ = builder.Append(ToDisplayName(entry.Difficulty));
                if (!string.IsNullOrEmpty(entry.Label)) {
                    _ = builder.Append($"({entry.Label})");
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// BeatSaver 表記の難易度をゲーム表記に変換する。
        /// </summary>
        public static string ToDisplayName(string beatSaverDifficulty)
        {
            return string.Equals(beatSaverDifficulty, "ExpertPlus", StringComparison.OrdinalIgnoreCase)
                ? "Expert+"
                : beatSaverDifficulty ?? "";
        }

        private static List<DiffEntry> GetEntries(JSONObject songVersion)
        {
            var result = new List<DiffEntry>();
            try {
                var diffs = songVersion?["diffs"].AsArray;
                if (diffs == null) {
                    return result;
                }
                foreach (var diff in diffs.Children) {
                    var difficulty = diff["difficulty"].Value;
                    if (string.IsNullOrEmpty(difficulty)) {
                        continue;
                    }
                    result.Add(new DiffEntry
                    {
                        Difficulty = difficulty,
                        Characteristic = diff["characteristic"].Value ?? "",
                        Label = diff["label"].Value ?? ""
                    });
                }
            }
            catch (Exception e) {
                Logger.Error(e);
            }
            return result;
        }

        /// <summary>
        /// 条件に一致するもののうち Standard を優先して返す。
        /// </summary>
        private static DiffEntry FindBest(List<DiffEntry> entries, Func<DiffEntry, bool> predicate)
        {
            return entries.FirstOrDefault(e => predicate(e)
                    && string.Equals(e.Characteristic, STANDARD_CHARACTERISTIC, StringComparison.OrdinalIgnoreCase))
                ?? entries.FirstOrDefault(predicate);
        }
    }
}
