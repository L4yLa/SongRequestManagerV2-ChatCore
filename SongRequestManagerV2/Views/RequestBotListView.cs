using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using IPA.Utilities;
using SongCore;
using SongRequestManagerV2.Bases;
using SongRequestManagerV2.Bots;
using SongRequestManagerV2.Configuration;
using SongRequestManagerV2.Extentions;
using SongRequestManagerV2.Interfaces;
using SongRequestManagerV2.Localizes;
using SongRequestManagerV2.Statics;
using SongRequestManagerV2.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRUIControls;
using Zenject;
using Keyboard = SongRequestManagerV2.Bots.Keyboard;

namespace SongRequestManagerV2.Views
{
    [HotReload]
    public class RequestBotListView : ViewContlollerBindableBase, IInitializable
    {
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // プロパティ
        // ui elements
        /// <summary>説明 を取得、設定</summary>
        private string _playButtonName_;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("play-button-text")]
        public string PlayButtonText
        {
            get => this._playButtonName_ ?? "PLAY";

            set => this.SetProperty(ref this._playButtonName_, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private string _skipButtonName_;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("skip-button-text")]
        public string SkipButtonName
        {
            get => this._skipButtonName_ ?? "SKIP";

            set => this.SetProperty(ref this._skipButtonName_, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private string _historyButtonText_;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("history-button-text")]
        public string HistoryButtonText
        {
            get => this._historyButtonText_ ?? "HISTORY";

            set => this.SetProperty(ref this._historyButtonText_, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private string _historyHoverHint;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("history-hint")]
        public string HistoryHoverHint
        {
            get => this._historyHoverHint ?? "";

            set => this.SetProperty(ref this._historyHoverHint, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private string _queueButtonText;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("queue-button-text")]
        public string QueueButtonText
        {
            get => this._queueButtonText ?? "QUEUQ CLOSE";

            set => this.SetProperty(ref this._queueButtonText, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private string _blacklistButtonText;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("blacklist-button-text")]
        public string BlackListButtonText
        {
            get => this._blacklistButtonText ?? "BLACK LIST";

            set => this.SetProperty(ref this._blacklistButtonText, value);
        }

        /// <summary>説明 を取得、設定</summary>
        [UIValue("requests")]
        public List<object> Songs { get; } = new List<object>();
        /// <summary>説明 を取得、設定</summary>
        private string _progressText;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("progress-text")]
        public string ProgressText
        {
            get => this._progressText ?? "Download Progress - 0 %";

            set => this.SetProperty(ref this._progressText, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private bool _isHistoryButtonEnable;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("history-button-enable")]
        public bool IsHistoryButtonEnable
        {
            get => this._isHistoryButtonEnable;

            set => this.SetProperty(ref this._isHistoryButtonEnable, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private bool _isPlayButtonEnable;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("play-button-enable")]
        public bool IsPlayButtonEnable
        {
            get => this._isPlayButtonEnable;

            set => this.SetProperty(ref this._isPlayButtonEnable, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private bool _isSkipButtonEnable;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("skip-button-enable")]
        public bool IsSkipButtonEnable
        {
            get => this._isSkipButtonEnable;

            set => this.SetProperty(ref this._isSkipButtonEnable, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private bool _isBlacklistButtonEnable;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("blacklist-button-enable")]
        public bool IsBlacklistButtonEnable
        {
            get => this._isBlacklistButtonEnable;

            set => this.SetProperty(ref this._isBlacklistButtonEnable, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private bool _isShowHistory = false;
        /// <summary>説明 を取得、設定</summary>
        public bool IsShowHistory
        {
            get => this._isShowHistory;

            set => this.SetProperty(ref this._isShowHistory, value);
        }
        [UIValue("version")]
        public string Version { get => $"<size=120%>Version - {Plugin.Version}"; set { } }

        private int SelectedRow => this._bot.CurrentSong == null ? -1 : this.Songs.IndexOf(this._bot.CurrentSong);

        /// <summary>説明 を取得、設定</summary>
        private bool _isActiveButton;
        [UIValue("is-active-button")]
        /// <summary>説明 を取得、設定</summary>
        public bool IsActiveButton
        {
            get => this._isActiveButton;

            set => this.SetProperty(ref this._isActiveButton, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private bool _anyUpdate;
        [UIValue("any-update")]
        /// <summary>説明 を取得、設定</summary>
        public bool AnyUpdate
        {
            get => this._anyUpdate;

            set => this.SetProperty(ref this._anyUpdate, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private string _notifyNewVersionText = "";
        [UIValue("new-version-text")]
        /// <summary>説明 を取得、設定</summary>
        public string NotifyNewVersionText
        {
            get => this._notifyNewVersionText;

            set => this.SetProperty(ref this._notifyNewVersionText, value);
        }
        /// <summary>説明 を取得、設定</summary>
        private string _updateButtonText = "";
        [UIValue("update-button-text")]
        /// <summary>説明 を取得、設定</summary>
        public string UpdateButtonText
        {
            get => this._updateButtonText;

            set => this.SetProperty(ref this._updateButtonText, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private bool _activeUpdateButton;
        [UIValue("update-button-active")]
        /// <summary>説明 を取得、設定</summary>
        public bool ActiveUpdateButton
        {
            get => this._activeUpdateButton;

            set => this.SetProperty(ref this._activeUpdateButton, value);
        }
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // イベントアクション
        public event Action<string> ChangeTitle;
        public event Action<SongRequest, bool> PlayProcessEvent;
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // Unity Message
        protected override void OnDestroy()
        {
            this._bot.UpdateUIRequest -= this.UpdateRequestUI;
            this._bot.SetButtonIntactivityRequest -= this.SetUIInteractivity;
            this._bot.PropertyChanged -= this.OnBotPropertyChanged;
            Loader.SongsLoadedEvent -= this.SongLoader_SongsLoadedEvent;
            if (this._audioSource != null) {
                Destroy(this._audioSource);
                this._audioSource = null;
            }
            this._onConfirm = null;
            this._onDecline = null;
            base.OnDestroy();
        }
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // オーバーライドメソッド
        protected override void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);
            if (args.PropertyName == nameof(this.IsShowHistory)) {
                this.UpdateRequestUI(true);
                this.SetUIInteractivity();
            }
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            this.UpdateRequestUI(true);
            this.SetUIInteractivity(true);
            this.IsActiveButton = true;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            if (!this._confirmDialogActive) {
                this.IsShowHistory = false;
            }
            this.UpdateRequestUI();
            this.SetUIInteractivity(true);
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // パブリックメソッド
        public void ChangeProgressText(double progress)
        {
            MainThreadInvoker.Instance.Enqueue(() =>
            {
                this.ProgressText = $"{ResourceWrapper.Get("TEXT_DOWNLOAD_PROGRESS")} - {progress * 100:0.00} %";
            });
        }

        public void UpdateRequestUI(bool selectRowCallback = false)
        {
            if (SceneManager.GetActiveScene().name == "GameCore") {
                return;
            }
            if (!this.isActivated) {
                return;
            }
            Dispatcher.RunOnMainThread(() =>
            {
                try {
                    this.QueueButtonText = RequestBotConfig.Instance.RequestQueueOpen ? ResourceWrapper.Get("BUTTON_QUEUE_OPEN") : ResourceWrapper.Get("BUTTON_QUEUE_CLOSE");
                    this.HistoryHoverHint = this.IsShowHistory ? ResourceWrapper.Get("HOVERHINT_REQUESTS") : ResourceWrapper.Get("HOVERHINT_HISTORY");
                    this.HistoryButtonText = this.IsShowHistory ? ResourceWrapper.Get("BUTTON_REQUESTS") : ResourceWrapper.Get("BUTTON_HISTORY");
                    this.PlayButtonText = this.IsShowHistory ? ResourceWrapper.Get("BUTTON_REPLAY") : ResourceWrapper.Get("BUTTON_PLAY");
                    this.RefreshSongQueueList(selectRowCallback);
                    var underline = this._queueButton.GetComponentsInChildren<ImageView>(true).FirstOrDefault(x => x.name == "Underline");
                    if (underline != null) {
                        underline.color = RequestBotConfig.Instance.RequestQueueOpen ? Color.green : Color.red;
                    }
                }
                catch (Exception e) {
                    Logger.Error(e);
                }
                try {
                    var underline = this._playButton.GetComponentsInChildren<ImageView>(true).FirstOrDefault(x => x.name == "Underline");
                    if (underline != null) {
                        underline.color = this.SelectedRow >= 0 ? Color.green : Color.red;
                    }
                }
                catch (Exception e) {
                    Logger.Error(e);
                }
            });
        }
        /// <summary>
        /// Alter the state of the buttons based on selection
        /// </summary>
        /// <param name="interactive">Set to false to force disable all buttons, true to auto enable buttons based on states</param>
        public void SetUIInteractivity(bool interactive = true)
        {
            if (!this.isActivated) {
                return;
            }
            try {
                var toggled = interactive;

                if (this._requestTable.NumberOfCells() == 0 || this.SelectedRow == -1 || this.SelectedRow >= this.Songs.Count()) {
                    toggled = false;
                }

                var playButtonEnabled = toggled;
                if (toggled && !this.IsShowHistory) {
                    // [2026-09-08] !challenge 判定を SongRequest.IsPlayable へ統合した。
                    // 難易度指定必須が有効で未指定のリクエストもここで再生不可になる。
                    // 合わせて CurrentSong が null の場合の NRE も回避する。
                    playButtonEnabled = this._bot.CurrentSong?.IsPlayable == true;
                }
                this.IsPlayButtonEnable = playButtonEnabled;

                var skipButtonEnabled = toggled;
                if (toggled && this.IsShowHistory) {
                    skipButtonEnabled = false;
                }
                this.IsSkipButtonEnable = skipButtonEnabled;

                this.IsBlacklistButtonEnable = toggled;

                // history button can be enabled even if others are disabled
                this.IsHistoryButtonEnable = true;
                this.IsHistoryButtonEnable = interactive;

                this.IsPlayButtonEnable = interactive;
                this.IsSkipButtonEnable = interactive;
                this.IsBlacklistButtonEnable = interactive;
                // history button can be enabled even if others are disabled
                this.IsHistoryButtonEnable = true;
            }
            catch (Exception e) {
                Logger.Error(e);
            }
        }
        public void RefreshSongQueueList(bool selectRowCallback = false)
        {
            try {
                lock (s_lockObject) {
                    this.Songs.Clear();
                    if (this.IsShowHistory) {
                        this.Songs.AddRange(RequestManager.HistorySongs);
                    }
                    else {
                        this.Songs.AddRange(RequestManager.RequestSongs);
                    }
                    Dispatcher.RunOnMainThread(() =>
                    {
                        try {
                            var tableView = this._requestTable?.TableView;
                            if (tableView == null) {
                                return;
                            }
                            tableView.ReloadData();
                            // [2026-08-27] 旧実装は SelectedRow が -1 のとき (uint)(-1) が約42億となり
                            // selectRowCallback=true の経路で選択処理が丸ごとスキップされていた。
                            // また selectRowCallback=false の経路では SelectCellWithIdx(-1) の
                            // 副作用に選択解除を依存していたため、呼び出し元によって挙動が変わっていた。
                            // 選択解除と選択を明示的に分岐させ、呼び出し元に依存しないようにする。
                            var row = this.SelectedRow;
                            if (row < 0 || row >= this._requestTable.NumberOfCells()) {
                                tableView.ClearSelection();
                                return;
                            }
                            // [2026-09-08] SelectCellWithIdx は selected-cell をプログラムから
                            // 発火させるため、その間はチャット案内を抑止する。
                            this._suppressSelectionNotice = true;
                            try {
                                tableView.SelectCellWithIdx(row, selectRowCallback);
                            }
                            finally {
                                this._suppressSelectionNotice = false;
                            }
                            tableView.ScrollToCellWithIdx(row, TableView.ScrollPositionType.Center, true);
                        }
                        catch (Exception e) {
                            Logger.Error(e);
                        }
                    });
                }
            }
            catch (Exception e) {
                Logger.Error(e);
            }
        }
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // プライベートメソッド
        private void OnBotPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is IRequestBot bot) {
                if (e.PropertyName == nameof(bot.CurrentSong)) {
                    this._playButton.GetComponentsInChildren<ImageView>().FirstOrDefault(x => x.name == "Underline").color = this.SelectedRow >= 0 ? Color.green : Color.red;
                }
            }
        }
        [UIAction("#post-parse")]
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void PostParse()
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            // Set default RequestFlowCoordinator title
            ChangeTitle?.Invoke(this.IsShowHistory ? ResourceWrapper.Get("TEXT_FLOWCORDINATER_TITLE_HISTORY") : ResourceWrapper.Get("TEXT_FLOWCORDINATER_TITLE_QUEUE"));
        }

        [UIAction("history-click")]
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void HistoryButtonClick()
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            this.IsShowHistory = !this.IsShowHistory;

            ChangeTitle?.Invoke(this.IsShowHistory ? ResourceWrapper.Get("TEXT_FLOWCORDINATER_TITLE_HISTORY") : ResourceWrapper.Get("TEXT_FLOWCORDINATER_TITLE_QUEUE"));
        }
        [UIAction("skip-click")]
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void SkipButtonClick()
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            if (this._requestTable.NumberOfCells() > 0) {
                void _onConfirm()
                {
                    // skip it
                    this._bot.Skip(this._bot.CurrentSong);
                    // indicate dialog is no longer active
                    this._confirmDialogActive = false;
                }

                // get song
                var song = this._bot.CurrentSong.SongMetaData;

                // indicate dialog is active
                this._confirmDialogActive = true;

                // show dialog
                this.ShowDialog("Skip Song Warning", $"Skipping {song["songName"].Value} by {song["songAuthorName"].Value}\r\nDo you want to continue?", _onConfirm, () => { this._confirmDialogActive = false; });
            }
        }
        [UIAction("blacklist-click")]
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void BlacklistButtonClick()
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            if (this._requestTable.NumberOfCells() > 0) {
                void _onConfirm()
                {
                    this._bot.Blacklist(this._bot.CurrentSong, this.IsShowHistory, true);
                    this._confirmDialogActive = false;
                }

                // get song
                var song = this._bot.CurrentSong.SongMetaData;

                // indicate dialog is active
                this._confirmDialogActive = true;

                // show dialog
                this.ShowDialog("Blacklist Song Warning", $"Blacklisting {song["songName"].Value} by {song["songAuthorName"].Value}\r\nDo you want to continue?", _onConfirm, () => { this._confirmDialogActive = false; });
            }
        }
        [UIAction("play-click")]
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void PlayButtonClick()
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            if (this._requestTable?.NumberOfCells() > 0) {
                // [2026-09-08] ボタンの interactable で防いでいるが、多重防御として明示的に弾く
                if (!this.IsShowHistory && this._bot.CurrentSong?.IsPlayable != true) {
                    return;
                }
                RequestBot.Played.Add(this._bot.CurrentSong.SongNode);
                this._bot.WriteJSON(RequestBot.playedfilename, RequestBot.Played);
                this.SetUIInteractivity(false);
                PlayProcessEvent?.Invoke(this._bot.CurrentSong, this.IsShowHistory);
            }
        }

        [UIAction("queue-click")]
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void QueueButtonClick()
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            RequestBotConfig.Instance.RequestQueueOpen = !RequestBotConfig.Instance.RequestQueueOpen;
            this._bot.WriteQueueStatusToFile(this._bot.QueueMessage(RequestBotConfig.Instance.RequestQueueOpen));
            this._chatManager.QueueChatMessage(RequestBotConfig.Instance.RequestQueueOpen ? "Queue is open." : "Queue is closed.");
            this.UpdateRequestUI();
        }

        [UIAction("selected-cell")]
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void SelectedCell(TableView _, object row)
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            var clip = this._randomSoundPicker?.PickRandomObject();
            if (clip) {
                this._audioSource?.PlayOneShot(clip, 1f);
            }
            this._bot.CurrentSong = row as SongRequest;
            this._playButton.GetComponentsInChildren<ImageView>().FirstOrDefault(x => x.name == "Underline").color = this.SelectedRow >= 0 ? Color.green : Color.red;

            if (!this.IsShowHistory) {
                var current = this._bot.CurrentSong;
                this.IsPlayButtonEnable = current?.IsPlayable == true;
                // [2026-09-08] 難易度未指定でグレーアウトされている行が選択されたら、
                // リクエスト者へ指定方法を案内する。
                if (current != null && current.NeedsDifficulty) {
                    this.NotifyDifficultyRequired(current);
                }
            }
            this.SetUIInteractivity();
        }

        /// <summary>
        /// [2026-09-08] 難易度未指定のリクエストが選択されたときの案内。
        /// selected-cell はプログラムからの選択でも発火するため抑止フラグで守り、
        /// 連続選択によるスパムを冷却時間で抑える。
        /// </summary>
        private void NotifyDifficultyRequired(SongRequest song)
        {
            try {
                if (this._suppressSelectionNotice) {
                    return;
                }
                var now = DateTime.UtcNow;
                if (this._lastNotifiedSong == song && now - this._lastNotifiedAt < s_notifyCooldown) {
                    return;
                }
                this._lastNotifiedSong = song;
                this._lastNotifiedAt = now;
                _ = this._textFactory.Create()
                    .AddSong(song.SongNode)
                    .AddUser(song.Requestor)
                    .QueueMessage(StringFormat.DifficultyRequiredText.ToString());
            }
            catch (Exception e) {
                Logger.Error(e);
            }
        }
        private void SongLoader_SongsLoadedEvent(Loader arg1, System.Collections.Concurrent.ConcurrentDictionary<string, BeatmapLevel> arg2)
        {
            this._requestTable?.TableView?.ReloadData();
        }
        [UIAction("update")]
        private void UpdateSRM()
        {
            if (!this._updateChecker.AnyUpdate) {
                return;
            }
            this.ActiveUpdateButton = false;
            this._updateChecker.UpdateMod().Await(r =>
            {
                if (r) {
                    this.ActiveUpdateButton = false;
                    this.NotifyNewVersionText = ResourceWrapper.Get("TEXT_UPDATE_SUCCESS");
                }
                else {
                    this.ActiveUpdateButton = true;
                }
            });
        }
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // メンバ変数
        private static readonly object s_lockObject = new object();
        private bool _confirmDialogActive = false;
        private Keyboard _centerKeys;

        [UIComponent("request-list")]
        private readonly CustomCellListTableData _requestTable;
        [UIComponent("queue-button")]
        private readonly NoTransitionsButton _queueButton;
        [UIComponent("play-button")]
        private readonly NoTransitionsButton _playButton;

        [Inject]
        protected PhysicsRaycasterWithCache _physicsRaycaster;
        [Inject]
        private readonly Keyboard.KEYBOARDFactiry _factiry;
        [Inject]
        private readonly IRequestBot _bot;
        [Inject]
        private readonly IChatManager _chatManager;
        [Inject]
        private readonly DynamicText.DynamicTextFactory _textFactory;

        // [2026-09-08] 難易度未指定の案内メッセージ制御
        private static readonly TimeSpan s_notifyCooldown = TimeSpan.FromSeconds(30);
        private bool _suppressSelectionNotice;
        private SongRequest _lastNotifiedSong;
        private DateTime _lastNotifiedAt = DateTime.MinValue;
        private AudioSource _audioSource;
        private RandomObjectPicker<AudioClip> _randomSoundPicker;
        private IUpdateChecker _updateChecker;
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // 構築・破棄
        [Inject]
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void Constractor(IUpdateChecker updateChecker)
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            this._bot.UpdateUIRequest -= this.UpdateRequestUI;
            this._bot.UpdateUIRequest += this.UpdateRequestUI;
            this._bot.SetButtonIntactivityRequest -= this.SetUIInteractivity;
            this._bot.SetButtonIntactivityRequest += this.SetUIInteractivity;
            this._bot.PropertyChanged -= this.OnBotPropertyChanged;
            this._bot.PropertyChanged += this.OnBotPropertyChanged;
            this._updateChecker = updateChecker;
        }

        public async void Initialize()
        {
            this._audioSource = Instantiate(Resources.FindObjectsOfTypeAll<BasicUIAudioManager>().FirstOrDefault().GetField<AudioSource, BasicUIAudioManager>("_audioSource"));
            this._audioSource.pitch = 1;
            var clips = Resources.FindObjectsOfTypeAll<BasicUIAudioManager>().FirstOrDefault().GetField<AudioClip[], BasicUIAudioManager>("_clickSounds");
            this._randomSoundPicker = new RandomObjectPicker<AudioClip>(clips, 0.07f);
            try {
                Loader.SongsLoadedEvent += this.SongLoader_SongsLoadedEvent;
            }
            catch (Exception e) {
                Logger.Error(e);
            }
            try {
                try {
                    this._centerKeys = this._factiry.Create().Setup(this.rectTransform, "", false, -15, 15);
                    this._centerKeys.AddKeyboard("CenterPanel.kbd");
                }
                catch (Exception e) {
                    Logger.Error(e);
                }
                _ = this._centerKeys.DefaultActions();
                try {
                    #region History button
                    // History button
                    this.HistoryButtonText = ResourceWrapper.Get("BUTTON_HISTORY");
                    #endregion
                }
                catch (Exception e) {
                    Logger.Error(e);
                }
                try {
                    #region Blacklist button
                    // Blacklist button
                    this.BlackListButtonText = ResourceWrapper.Get("BUTTON_BLACK_LIST");
                    #endregion
                }
                catch (Exception e) {
                    Logger.Error(e);
                }
                try {
                    #region Skip button
                    this.SkipButtonName = ResourceWrapper.Get("BUTTON_SKIP");
                    #endregion
                }
                catch (Exception e) {
                    Logger.Error(e);
                }
                try {
                    #region Play button
                    // Play button
                    this.PlayButtonText = ResourceWrapper.Get("BUTTON_PLAY");
                    #endregion
                }
                catch (Exception e) {
                    Logger.Error(e);
                }
                try {
                    #region Queue button
                    // Queue button
                    this.QueueButtonText = RequestBotConfig.Instance.RequestQueueOpen ? ResourceWrapper.Get("BUTTON_QUEUE_OPEN") : ResourceWrapper.Get("BUTTON_QUEUE_CLOSE");
                    #endregion
                }
                catch (Exception e) {
                    Logger.Error(e);
                }
                try {
                    #region Progress
                    this.ChangeProgressText(0f);
                    #endregion
                }
                catch (Exception e) {
                    Logger.Error(e);
                }
                this.UpdateButtonText = ResourceWrapper.Get("BUTTON_SRM_UPDATE");
            }
            catch (Exception e) {
                Logger.Error(e);
            }
            var anyUpdate = await this._updateChecker.CheckUpdate(Plugin.MetaData);
            if (anyUpdate) {
                this.AnyUpdate = true;
                this.NotifyNewVersionText = ResourceWrapper.Get("TEXT_NOTIFY_NEW_UPDATE").Replace("%NEWVERSION%", $"{this._updateChecker.CurrentLatestVersion}");
                this.ActiveUpdateButton = true;
            }
            else {
                this.AnyUpdate = false;
            }
        }

        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region Modal
        private Action _onConfirm;
        private Action _onDecline;

        [UIComponent("modal")]
        internal ModalView _modal;
        /// <summary>説明 を取得、設定</summary>
        private string _title;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("title")]
        public string Title
        {
            get => this._title ?? "";

            set => this.SetProperty(ref this._title, value);
        }

        /// <summary>説明 を取得、設定</summary>
        private string _message;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("message")]
        public string Message
        {
            get => this._message ?? "";

            set => this.SetProperty(ref this._message, value);
        }

        [UIAction("yes-click")]
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void YesClick()
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            this._modal.Hide(true);
            this._onConfirm?.Invoke();
            this._onConfirm = null;
        }

        [UIAction("no-click")]
#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void NoClick()
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            this._modal.Hide(true);
            this._onDecline?.Invoke();
            this._onDecline = null;
        }

        [UIAction("show-dialog")]
        public void ShowDialog(string title, string message, Action onConfirm = null, Action onDecline = null)
        {
            this.Title = title;
            this.Message = message;

            this._onConfirm = onConfirm;
            this._onDecline = onDecline;

            this._modal.Show(true);
        }
        #endregion
    }
}