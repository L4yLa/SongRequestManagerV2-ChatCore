// Modified: 2026-04-02
// Changes:
//   - Replaced CatCore dependencies with ChatCore (using directives updated)
//   - Simplified TwitchUser constructor call to JSON-based approach
//   - Added debug logging for unknown CDN domains
// Modified: 2026-08-14
// Changes:
//   - Attach the queue cell HoverHint to the cell root object instead of
//     relying on the BSML `hover-hint` binding on the `tags='hovered'` background,
//     which never receives pointer events (requester info was not shown)
// Modified: 2026-08-23
// Changes:
//   - Recognize r2cdn.beatsaver.com (BeatSaver's Cloudflare R2 download host)
//     as a known CDN domain so it no longer logs as "Unknown CDN domain"
// Modified: 2026-09-08
// Changes:
//   - Added RequestedDifficulty / RequestedDifficultyLabel / RequestedCharacteristic
//     fields (set by !difficulty) with JSON round-trip
//   - Extracted display text generation into RefreshTexts() so that post-hoc
//     modifications can update the row and hover hint without a full list reload
//   - Added grey-out rendering for requests missing a required difficulty
// Modified: 2026-09-09
// Changes:
//   - Dropped the !memo feature (RequestComment and its hover-hint line)
//   - Added IsQueueMessagePending so the "added to queue" notice can be deferred
//     until a difficulty is supplied (avoids sending two chat messages at once)
using BeatSaberMarkupLanguage.Attributes;
using ChatCore.Interfaces;
using ChatCore.Models.Twitch;
using HMUI;
using Newtonsoft.Json;
using SongCore;
using SongRequestManagerV2.Bases;
using SongRequestManagerV2.Configuration;
using SongRequestManagerV2.Models;
using SongRequestManagerV2.SimpleJsons;
using SongRequestManagerV2.Statics;
using SongRequestManagerV2.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Zenject;

namespace SongRequestManagerV2.Bots
{
    public class SongRequest : BindableBase
    {
        [UIComponent("coverImage")]
        public ImageView _coverImage;

        [UIComponent("songNameText")]
        public TextMeshProUGUI _songNameText;

        [UIComponent("authorNameText")]
        public TextMeshProUGUI _authorNameText;

        // [2026-08-14] BSML が生成するカスタムセルのルート GameObject 名（BSML 1.7～1.14）
        private const string CELL_OBJECT_NAME = "BSMLCustomTableCell";

        // [2026-09-08] 難易度未指定時のグレーアウト表現
        private const string DIMMED_TEXT_COLOR = "#606060";
        private static readonly Color DIMMED_IMAGE_COLOR = new Color(0.35f, 0.35f, 0.35f, 1f);

        // [2026-08-14] セルルートに付与した HoverHint
        private HoverHint _cellHoverHint;

        [Inject]
        private readonly DynamicText.DynamicTextFactory _textFactory;
        [Inject]
        private readonly MapDatabase _mapDatabase;

        /// <summary>説明 を取得、設定</summary>
        private string hint_;
        [UIValue("hover-hint")]
        public string Hint
        {
            get => this.hint_ ?? "";

            set => this.SetProperty(ref this.hint_, value);
        }

        private string songName_;
        [UIValue("song-name")]
        public string SongName
        {
            get => this.songName_ ?? "";

            set => this.SetProperty(ref this.songName_, value);
        }

        private string authorName_;
        [UIValue("author-name")]
        public string AuthorName
        {
            get => this.authorName_ ?? "";

            set => this.SetProperty(ref this.authorName_, value);
        }
        /// <summary>
        /// beatsaver json object<br />
        /// https://api.beatsaver.com/docs/index.html?url=./swagger.json
        /// </summary>
        public JSONObject SongNode { get; private set; }
        public JSONObject SongMetaData => this.SongNode["metadata"].AsObject;

        public JSONObject SongVersion { get; private set; }
        public bool IsWIP { get; private set; }
        public IChatUser Requestor { get; private set; }
        public DateTime RequestTime { get; private set; }
        public RequestStatus Status { get; set; }
        public string RequestInfo; // Contains extra song info, Like : Sub/Donation request, Deck pick, Empty Queue pick,Mapper request, etc.

        // [2026-09-08] !difficulty による後追い指定
        /// <summary>
        /// 指定された難易度。BeatSaver 表記 (Easy / Normal / Hard / Expert / ExpertPlus)。
        /// 未指定の場合は空文字。
        /// </summary>
        public string RequestedDifficulty { get; set; } = "";
        /// <summary>指定に使われたカスタム難易度ラベル。標準難易度名で指定された場合は空文字。</summary>
        public string RequestedDifficultyLabel { get; set; } = "";
        /// <summary>指定された characteristic。未指定の場合は空文字。</summary>
        public string RequestedCharacteristic { get; set; } = "";

        /// <summary>
        /// [2026-09-09] 難易度指定必須のために「キューに追加」通知を保留中かどうか。
        /// Twitch のレート制限を避けるため、案内と追加通知を同時に送らない。
        /// 永続化しない (再起動後に改めて通知しないため)。
        /// </summary>
        public bool IsQueueMessagePending { get; set; }

        /// <summary>難易度が指定済みかどうか。</summary>
        public bool HasRequestedDifficulty => !string.IsNullOrEmpty(this.RequestedDifficulty);

        /// <summary>
        /// [2026-09-09] 指定難易度の表示名。カスタムラベルと、Standard 以外の
        /// characteristic も含む。未指定の場合は空文字。
        /// ホバー枠とチャット通知で共用する。
        /// </summary>
        public string RequestedDifficultyDisplay
        {
            get {
                if (!this.HasRequestedDifficulty) {
                    return "";
                }
                var name = DifficultyResolver.ToDisplayName(this.RequestedDifficulty);
                var text = string.IsNullOrEmpty(this.RequestedDifficultyLabel)
                    ? name
                    : $"{this.RequestedDifficultyLabel} ({name})";
                if (!string.IsNullOrEmpty(this.RequestedCharacteristic)
                    && !string.Equals(this.RequestedCharacteristic, DifficultyResolver.STANDARD_CHARACTERISTIC, StringComparison.OrdinalIgnoreCase)) {
                    text = $"[{this.RequestedCharacteristic}] {text}";
                }
                return text;
            }
        }
        /// <summary>!challenge 指定されているかどうか。</summary>
        public bool IsChallenge => !string.IsNullOrEmpty(this.RequestInfo)
            && 0 <= this.RequestInfo.IndexOf("!challenge", StringComparison.OrdinalIgnoreCase);
        /// <summary>難易度指定必須が有効で、かつ難易度が未指定かどうか。</summary>
        public bool NeedsDifficulty => RequestBotConfig.Instance?.RequireDifficulty == true && !this.HasRequestedDifficulty;
        /// <summary>再生可能かどうか。履歴表示では参照しない。</summary>
        public bool IsPlayable => !this.IsChallenge && !this.NeedsDifficulty;

        /// <summary>
        /// bsr key
        /// </summary>
        public string ID { get; private set; }
        /// <summary>マップのハッシュ。</summary>
        public string Hash => this._hash;
        private string _hash;
        private string _coverURL;
        private string _downloadURL;
        private string _songName;
        private string _rating;

        private static readonly ConcurrentDictionary<string, Texture2D> _cachedTextures = new ConcurrentDictionary<string, Texture2D>();

        public SongRequest Init(JSONObject obj)
        {
            _ = this.Init(
                obj["song"].AsObject,
                this.CreateRequester(obj),
                DateTime.FromFileTime(long.Parse(obj["time"].Value)),
                (RequestStatus)Enum.Parse(typeof(RequestStatus),
                obj["status"].Value),
                obj["requestInfo"].Value);
            // [2026-09-08] 旧バージョンで保存されたキューにはこれらのキーが無いため、
            // 欠損時は空文字となる (SimpleJson は未存在キーの Value に "" を返す)。
            this.RequestedDifficulty = obj["requestedDifficulty"].Value ?? "";
            this.RequestedDifficultyLabel = obj["requestedDifficultyLabel"].Value ?? "";
            this.RequestedCharacteristic = obj["requestedCharacteristic"].Value ?? "";
            return this;
        }

        public SongRequest Init(JSONObject song, IChatUser requestor, DateTime requestTime, RequestStatus status = RequestStatus.Invalid, string requestInfo = "")
        {
            this.SongNode = song;
            this._songName = this.SongMetaData["songName"].Value;
            this.ID = this.SongNode["id"].Value?.ToLower();
            this.Requestor = requestor;
            this.Status = status;
            this.RequestTime = requestTime;
            this.RequestInfo = requestInfo;
            var version = this.SongNode["versions"].AsArray.Children.FirstOrDefault(x => x["state"].Value == MapStatus.Published.ToString());
            if (version == null) {
                this.SongVersion = this.SongNode["versions"].AsArray.Children.OrderBy(x => DateTime.Parse(x["createdAt"].Value)).LastOrDefault().AsObject;
                this.IsWIP = true;
            }
            else {
                this.SongVersion = this.SongNode["versions"].AsArray.Children.FirstOrDefault(x => x["state"].Value == MapStatus.Published.ToString()).AsObject;
                this.IsWIP = false;
            }
            this._hash = this.SongVersion["hash"].Value;
            this._coverURL = this.SongVersion["coverURL"].Value;
            // as.だのna.だの指定されると重くなるっぽい？
            this._downloadURL = this.SongVersion["downloadURL"].Value
                .Replace(RequestBot.BEATMAPS_AS_CDN_ROOT_URL, RequestBot.BEATMAPS_CDN_ROOT_URL)
                .Replace(RequestBot.BEATMAPS_NA_CDN_ROOT_URL, RequestBot.BEATMAPS_CDN_ROOT_URL);
            // [2026-08-23] r2cdn.beatsaver.com はBeatSaver公式の別CDN経路（Cloudflare R2）。
            // as./na.のような単純なリージョン別ミラーではなく別バックエンドのため、
            // cdn.beatsaver.com への書き換えは行わず、既知ドメインとして扱うのみとする。
            if (!this._downloadURL.StartsWith(RequestBot.BEATMAPS_CDN_ROOT_URL)
                && !this._downloadURL.StartsWith(RequestBot.BEATMAPS_R2_CDN_ROOT_URL)) {
                try {
                    Logger.Debug($"Unknown CDN domain: {new Uri(this._downloadURL).Host}");
                }
                catch (Exception) { }
            }
            if (this._mapDatabase.PPMap.TryGetValue(this.ID, out var pp)) {
                this.SongNode.Add("pp", new JSONNumber(pp));
            }
            return this;
        }

        [UIAction("#post-parse")]
        internal void Setup()
        {
            this._rating = RequestBotConfig.Instance.PPSearch && this._mapDatabase.PPMap.TryGetValue(this.ID, out var pp) && 0 < pp
                ? $" <size=50%>{Utility.GetRating(this.SongNode)} <color=#4169e1>{pp:0.00} PP</color></size>"
                : $" <size=50%>{Utility.GetRating(this.SongNode)}</size>";
            this.SetupHoverHint();
            // #post-parse はメインスレッドで実行されるため同期的に適用する。
            // Dispatcher 経由にすると 1 フレーム分テキストが空になる。
            this.RefreshTexts();
            this.SetCover();
        }

        /// <summary>
        /// セルのルート GameObject に HoverHint を付与する。
        /// bsml 側の hover-hint バインドは tags='hovered' の背景に付与されるが、
        /// この背景はレイキャスト対象にならないため PointerEnter が届かず、
        /// リクエスト者情報が表示されない。セルルートは Touchable を持ち
        /// ポインタイベントを確実に受け取れるため、そちらに付け替える。
        /// </summary>
        private void SetupHoverHint()
        {
            try {
                if (this._coverImage == null) {
                    return;
                }
                // CustomCellTableCell を型で参照すると基底型チェーンの都合で
                // Interactable アセンブリ参照が必要になるため、名前でセルルートを遡る。
                GameObject cellObject = null;
                for (var t = this._coverImage.transform; t != null; t = t.parent) {
                    if (t.name == CELL_OBJECT_NAME) {
                        cellObject = t.gameObject;
                        break;
                    }
                }
                if (cellObject == null) {
                    return;
                }
                if (!cellObject.TryGetComponent(out this._cellHoverHint)) {
                    this._cellHoverHint = BeatSaberMarkupLanguage.BeatSaberUI.DiContainer.InstantiateComponent<HoverHint>(cellObject);
                }
                this._cellHoverHint.text = this.Hint;
            }
            catch (Exception e) {
                Logger.Error(e);
            }
        }

        [UIAction("selected")]
        private void Selected() { }

        [UIAction("hovered")]
        private void Hovered() { }

        [UIAction("un-selected-un-hovered")]
        private void UnSelectedUnHovered() { }
        /// <summary>
        /// lookup song from level id
        /// </summary>
        /// <returns></returns>
        private BeatmapLevel GetCustomLevel()
        {
            return Loader.GetLevelByHash(this._hash.ToUpper());
        }

        /// <summary>
        /// [2026-09-08] !difficulty による変更後に表示を作り直す。
        /// セルが生存していればホバー枠のテキストも即時更新される。
        /// </summary>
        public void RefreshDisplay()
        {
            Dispatcher.RunOnMainThread(() =>
            {
                try {
                    this.RefreshTexts();
                }
                catch (Exception e) {
                    Logger.Error(e);
                }
            });
        }

        /// <summary>
        /// [2026-09-08] 行テキストとホバー枠テキストを生成する。
        /// セルは毎回作り直されるため、色指定はバインドされる文字列そのものに焼き込む。
        /// </summary>
        private void RefreshTexts()
        {
            var dt = this._textFactory.Create().AddSong(this.SongNode).AddUser(this.Requestor); // Get basic fields
            _ = dt.Add("Status", this.Status.ToString());
            _ = dt.Add("Info", this.RequestInfo != "" ? " / " + this.RequestInfo : "");
            _ = dt.Add("RequestTime", this.RequestTime.ToLocalTime().ToString("hh:mm"));
            _ = dt.Add("Difficulty", this.GetDifficultyHintText());

            var builder = new StringBuilder();
            _ = builder.Append(this.IsWIP ? $"<color=\"yellow\">[WIP]</color> {this._songName}" : this._songName);
            _ = builder.Append(this._rating);
            this.SongName = this.ApplyDimming(builder.ToString());
            this.AuthorName = this.ApplyDimming(dt.Parse(StringFormat.QueueListRow2));
            this.Hint = dt.Parse(StringFormat.SongHintText);
            if (this._cellHoverHint != null) {
                this._cellHoverHint.text = this.Hint;
            }
            if (this._coverImage != null) {
                this._coverImage.color = this.NeedsDifficulty ? DIMMED_IMAGE_COLOR : Color.white;
            }
        }

        /// <summary>
        /// 難易度指定必須設定で未指定の場合、表示をグレーアウトする。
        /// </summary>
        private string ApplyDimming(string text)
        {
            return this.NeedsDifficulty ? $"<color={DIMMED_TEXT_COLOR}>{text}</color>" : text;
        }

        /// <summary>
        /// ホバー枠に表示する難易度行。改行は DynamicText の %LF% と同じ "\n" を使う。
        /// </summary>
        private string GetDifficultyHintText()
        {
            if (!this.HasRequestedDifficulty) {
                return this.NeedsDifficulty ? "\nDifficulty: <color=#ff6060>Not specified</color>" : "";
            }
            return $"\nDifficulty: {this.RequestedDifficultyDisplay}";
        }

        public void SetCover()
        {
            Dispatcher.RunOnMainThread(async () =>
            {
                try {
                    if (this._coverImage == null) {
                        return;
                    }
                    this._coverImage.enabled = false;

                    var imageSet = false;

                    if (Loader.AreSongsLoaded) {
                        var level = this.GetCustomLevel();
                        if (level != null) {
                            //Logger.Debug("custom level found");
                            // set image from song's cover image
                            var tex = await level.previewMediaData.GetCoverSpriteAsync();
                            this._coverImage.sprite = tex;
                            imageSet = true;
                        }
                    }

                    if (!imageSet) {
                        var url = !string.IsNullOrEmpty(this._coverURL) ? this._coverURL : $"{RequestBot.BEATMAPS_CDN_ROOT_URL}/{this._hash.ToLower()}.jpg";
                        if (!_cachedTextures.TryGetValue(url, out var tex)) {
                            var b = await WebClient.DownloadImage(url, CancellationToken.None).ConfigureAwait(true);

                            tex = new Texture2D(2, 2);
                            _ = tex.LoadImage(b);

                            try {
                                _ = _cachedTextures.AddOrUpdate(url, tex, (s, v) => tex);
                            }
                            catch (Exception e) {
                                Logger.Error(e);
                            }
                        }
                        this._coverImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
                catch (Exception e) {
                    Logger.Error(e);
                }
                finally {
                    if (this._coverImage != null) {
                        this._coverImage.enabled = true;
                    }
                }
            });
        }

        public JSONObject ToJson()
        {
            try {
                var obj = new JSONObject();
                obj.Add("status", new JSONString(this.Status.ToString()));
                obj.Add("requestInfo", new JSONString(this.RequestInfo));
                obj.Add("time", new JSONString(this.RequestTime.ToFileTime().ToString()));
                obj.Add("requestor", JsonConvert.SerializeObject(this.Requestor));
                // [2026-09-08] !difficulty の指定内容
                obj.Add("requestedDifficulty", new JSONString(this.RequestedDifficulty ?? ""));
                obj.Add("requestedDifficultyLabel", new JSONString(this.RequestedDifficultyLabel ?? ""));
                obj.Add("requestedCharacteristic", new JSONString(this.RequestedCharacteristic ?? ""));
                obj.Add("song", this.SongNode);
                return obj;
            }
            catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        private IChatUser CreateRequester(JSONObject obj)
        {
            try {
                var requesterText = obj["requestor"].Value;
                var userObj = JSONNode.Parse(requesterText);
                var temp = new TwitchUser(userObj.ToString());
                return temp;
            }
            catch (Exception e) {
                Logger.Error(e);
                return new GenericChatUser(obj["requestor"].AsObject.ToString());
            }
        }

        public async Task<byte[]> DownloadZip(CancellationToken token = default(CancellationToken), IProgress<double> progress = null)
        {
            try {
                var url = !string.IsNullOrEmpty(this._downloadURL)
                    ? this._downloadURL
                    : $"{RequestBot.BEATMAPS_CDN_ROOT_URL}/{this._hash.ToLower()}.zip";
                var response = await WebClient.SendAsync(HttpMethod.Get, url, token, progress);

                return response?.IsSuccessStatusCode == true ? response.ContentToBytes() : null;
            }
            catch (Exception e) {
                Logger.Error(e);
                return null;
            }
        }

        public class SongRequestFactory : PlaceholderFactory<SongRequest>
        {

        }
    }
}
