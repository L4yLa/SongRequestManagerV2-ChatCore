// Created: 2026-09-08
// Purpose:
//   !difficulty / !bsrd で指定された難易度を、SRM の PLAY から遷移した曲に限って
//   自動選択し、ユーザーが別の難易度へ移動した場合はその難易度をハイライトする。
//
// 設計上の注意 (いずれも実機ログで確認した挙動)
//   - HMUI SegmentedControl のセルは SetData をまたいで再利用される。
//     色を書き換えたセルは、参照を手放す前に必ず元の色へ戻す必要がある。
//     復元をスキップすると解除不能になり、さらに残留色を「元の色」として
//     記録してしまい永久に固着する。
//   - 自動選択は SetData 由来のときだけ行う。didSelectCellEvent から行うと
//     ユーザーの手動選択を指定難易度へ引き戻してしまう。
//   - SegmentedControl.SelectCellWithNumber は didSelectCellEvent を発火しない。
//     そのためユーザー操作とこちらの自動選択は明確に区別できる。
//   - 対象の武装は IRequestBot.PlayNowChanged で行う。CurrentSong は行選択だけで
//     更新されるので使えないし、PlayNow の参照比較でも履歴からのリプレイ
//     (同じインスタンスが渡る) を検知できない。
using HMUI;
using SiraUtil.Affinity;
using SongRequestManagerV2.Bots;
using SongRequestManagerV2.Interfaces;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace SongRequestManagerV2.UI
{
    public class DifficultyHighlighter : IInitializable, IDisposable, IAffinity
    {
        /// <summary>
        /// ハイライト色。暗い背景でも埋もれないよう明るめの赤。
        /// PlaylistManager が同じセルに黄色 (255,255,0) を使うため、
        /// 競合を見分けられるよう別の色にしている。
        /// </summary>
        private static readonly Color32 HIGHLIGHT_COLOR = new Color32(255, 60, 60, 255);

        /// <summary>
        /// 汚染ガードで使う既定の文字色。難易度セルの文字色は非選択時に
        /// 白 (255,255,255) であることを実機で確認している。
        /// </summary>
        private static readonly Color32 DEFAULT_TEXT_COLOR = new Color32(255, 255, 255, 255);

        private const string CUSTOM_LEVEL_ID_PREFIX = "custom_level_";

        private readonly IRequestBot _bot;
        private readonly StandardLevelDetailView _standardLevelDetailView;
        private readonly BeatmapCharacteristicSegmentedControlController _characteristicController;
        private readonly BeatmapDifficultySegmentedControlController _difficultyController;
        private readonly SegmentedControl _difficultySegmentedControl;
        private readonly IconSegmentedControl _characteristicSegmentedControl;

        /// <summary>色を書き換えているテキストと、そのセル番号・書き換え前の色。</summary>
        private CurvedTextMeshPro _highlightedText;
        private int _highlightedIndex = -1;
        private Color32 _originalColor;

        /// <summary>
        /// ハイライト対象のリクエスト。PLAY が押されたときにのみ設定され、
        /// 別の曲が選択された時点に解除される。
        /// </summary>
        private SongRequest _targetSong;

        /// <summary>
        /// ユーザーが難易度セルを手動で選択したか。
        /// true になった以降は自動選択を行わず、ユーザーの操作を尊重する。
        /// </summary>
        private bool _userSelectedDifficulty;

        public DifficultyHighlighter(StandardLevelDetailViewController standardLevelDetailViewController, IRequestBot bot)
        {
            this._bot = bot;
            this._standardLevelDetailView = standardLevelDetailViewController._standardLevelDetailView;
            this._characteristicController = this._standardLevelDetailView._beatmapCharacteristicSegmentedControlController;
            this._characteristicSegmentedControl = this._characteristicController._segmentedControl;
            this._difficultyController = this._standardLevelDetailView._beatmapDifficultySegmentedControlController;
            this._difficultySegmentedControl = this._difficultyController._difficultySegmentedControl;
        }

        public void Initialize()
        {
            if (this._characteristicSegmentedControl != null) {
                this._characteristicSegmentedControl.didSelectCellEvent += this.OnCharacteristicSelected;
            }
            if (this._difficultySegmentedControl != null) {
                this._difficultySegmentedControl.didSelectCellEvent += this.OnDifficultySelected;
            }
            this._bot.PlayNowChanged += this.OnPlayNowChanged;
        }

        public void Dispose()
        {
            if (this._characteristicSegmentedControl != null) {
                this._characteristicSegmentedControl.didSelectCellEvent -= this.OnCharacteristicSelected;
            }
            if (this._difficultySegmentedControl != null) {
                this._difficultySegmentedControl.didSelectCellEvent -= this.OnDifficultySelected;
            }
            this._bot.PlayNowChanged -= this.OnPlayNowChanged;
            this._targetSong = null;
            this._userSelectedDifficulty = false;
            this.Restore();
        }

        /// <summary>
        /// PLAY が押された。ここでのみ対象を武装する。
        /// 履歴からのリプレイでは同じ SongRequest インスタンスが渡されるため、
        /// 参照の変化ではなくイベントで検知している。
        /// このイベントはバックグラウンドスレッドから発火し得るので、
        /// Unity オブジェクトには触らないこと。
        /// </summary>
        private void OnPlayNowChanged(SongRequest request)
        {
            this._targetSong = request?.HasRequestedDifficulty == true ? request : null;
            this._userSelectedDifficulty = false;
        }

        /// <summary>
        /// 難易度セルが再構築されたタイミング。ここでのみ自動選択を許可する。
        /// SetData 内でセルが差し替わるため、1 フレーム待ってから処理する。
        /// </summary>
        [AffinityPostfix]
        [AffinityPatch(typeof(BeatmapDifficultySegmentedControlController), nameof(BeatmapDifficultySegmentedControlController.SetData))]
        private void SetDataPostfix()
        {
            Dispatcher.RunCoroutine(this.ApplyDelayed(true));
        }

        private void OnCharacteristicSelected(SegmentedControl _, int __)
        {
            Dispatcher.RunCoroutine(this.ApplyDelayed(false));
        }

        /// <summary>
        /// ユーザーが難易度を手動で選択した。以降は自動選択で干渉しない。
        /// SelectCellWithNumber はこのイベントを発火しないため、
        /// こちら側の自動選択でここへ入ることはない。
        /// </summary>
        private void OnDifficultySelected(SegmentedControl _, int __)
        {
            this._userSelectedDifficulty = true;
            Dispatcher.RunCoroutine(this.ApplyDelayed(false));
        }

        private IEnumerator ApplyDelayed(bool allowAutoSelect)
        {
            yield return null;
            this.Apply(allowAutoSelect);
        }

        /// <summary>
        /// PLAY で遷移した曲を表示している間だけ、指定難易度を自動選択・ハイライトする。
        /// </summary>
        /// <param name="allowAutoSelect">自動選択を許可するか (SetData 由来のときのみ true)</param>
        private void Apply(bool allowAutoSelect)
        {
            try {
                // セルは再利用されるため、まず必ず元の色へ戻す。
                this.Restore();

                var song = this._targetSong;
                if (song == null) {
                    return;
                }
                if (!this.IsCurrentLevel(song)) {
                    // 別の曲が選択された。以降はハイライトしない。
                    this._targetSong = null;
                    return;
                }

                var selectedCharacteristic = this._characteristicController?.selectedBeatmapCharacteristic;
                if (selectedCharacteristic == null) {
                    return;
                }
                var wanted = string.IsNullOrEmpty(song.RequestedCharacteristic)
                    ? DifficultyResolver.STANDARD_CHARACTERISTIC
                    : song.RequestedCharacteristic;
                if (!string.Equals(selectedCharacteristic.serializedName, wanted, StringComparison.OrdinalIgnoreCase)) {
                    return;
                }

                // BeatSaver の difficulty 表記はゲームの BeatmapDifficulty と同じ名前。
                if (!Enum.TryParse<BeatmapDifficulty>(song.RequestedDifficulty, true, out var difficulty)) {
                    return;
                }

                // GetClosestDifficultyIndex は存在しない難易度でも近いものを返すため、
                // 事前に存在確認する。
                var available = this._difficultyController?._difficulties;
                if (available == null || !available.Contains(difficulty)) {
                    return;
                }

                var cells = this._difficultySegmentedControl?.cells;
                if (cells == null) {
                    return;
                }
                var index = this._difficultyController.GetClosestDifficultyIndex(difficulty);
                if (index < 0 || cells.Count <= index) {
                    return;
                }

                // 自動選択は SetData 由来のときだけ、かつユーザーが手動で選ぶ前だけ行う。
                if (this._difficultySegmentedControl.selectedCellNumber != index
                    && allowAutoSelect
                    && !this._userSelectedDifficulty
                    && this.TrySelectDifficulty(index)) {
                    // 選択によってデータが再構築されるため、以降の処理はその際に行う。
                    return;
                }

                var text = cells[index].GetComponentInChildren<CurvedTextMeshPro>();
                if (text == null) {
                    return;
                }
                var current = text.faceColor;
                // 前回のハイライトが何らかの理由で残留していた場合、それを「元の色」として
                // 記録すると永遠に戻らなくなるため、既定色へ落とす。
                this._originalColor = IsHighlightColor(current) ? DEFAULT_TEXT_COLOR : current;
                this._highlightedText = text;
                this._highlightedIndex = index;
                text.faceColor = HIGHLIGHT_COLOR;
            }
            catch (Exception e) {
                Logger.Error(e);
            }
        }

        /// <summary>
        /// 難易度セルを選択し、ゲーム側へ反映させる。
        /// SegmentedControl.SelectCellWithNumber は didSelectCellEvent を発火しないため、
        /// コントローラー側のセル選択ハンドラを直接呼び出す。
        /// ハンドラは名前ではなく引数の形 (SegmentedControl, int) で探す。
        /// </summary>
        /// <returns>選択操作を行った場合 true。</returns>
        private bool TrySelectDifficulty(int index)
        {
            try {
                if (this._difficultySegmentedControl == null || this._difficultyController == null) {
                    return false;
                }
                var handler = this._difficultyController.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(m =>
                    {
                        var parameters = m.GetParameters();
                        return 0 <= m.Name.IndexOf("DidSelectCell", StringComparison.OrdinalIgnoreCase)
                            && parameters.Length == 2
                            && typeof(SegmentedControl).IsAssignableFrom(parameters[0].ParameterType)
                            && parameters[1].ParameterType == typeof(int);
                    });
                this._difficultySegmentedControl.SelectCellWithNumber(index);
                if (handler == null) {
                    Logger.Error("DifficultyHighlighter: difficulty selection handler was not found. Auto-selection is unavailable.");
                    return false;
                }
                _ = handler.Invoke(this._difficultyController, new object[] { this._difficultySegmentedControl, index });
                return true;
            }
            catch (Exception e) {
                Logger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// 色がハイライト色かどうか。
        /// </summary>
        private static bool IsHighlightColor(Color32 color)
        {
            return color.r == HIGHLIGHT_COLOR.r
                && color.g == HIGHLIGHT_COLOR.g
                && color.b == HIGHLIGHT_COLOR.b
                && color.a == HIGHLIGHT_COLOR.a;
        }

        /// <summary>
        /// 書き換えた色を元へ戻す。
        /// 選択状態によってスキップしてはならない。スキップすると赤のまま参照を
        /// 手放してしまい、別の曲へ移動したときやプレイ後に解除できなくなる。
        /// </summary>
        private void Restore()
        {
            try {
                if (this._highlightedText != null) {
                    this._highlightedText.faceColor = this._originalColor;
                }
            }
            catch (Exception e) {
                Logger.Error(e);
            }
            this._highlightedText = null;
            this._highlightedIndex = -1;
        }

        /// <summary>
        /// 詳細画面に表示中の曲が、対象のリクエストと同一かどうか。
        /// 判定できない場合は false を返し、誤ったハイライトを行わない。
        /// </summary>
        private bool IsCurrentLevel(SongRequest song)
        {
            try {
                if (string.IsNullOrEmpty(song.Hash)) {
                    return false;
                }
                var levelId = this._standardLevelDetailView.beatmapKey.levelId;
                if (string.IsNullOrEmpty(levelId)
                    || !levelId.StartsWith(CUSTOM_LEVEL_ID_PREFIX, StringComparison.OrdinalIgnoreCase)) {
                    return false;
                }
                // WIP 譜面の levelId は "custom_level_<HASH> WIP" 形式になる。
                var hash = levelId.Substring(CUSTOM_LEVEL_ID_PREFIX.Length).Split(' ')[0];
                return string.Equals(hash, song.Hash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception e) {
                Logger.Error(e);
                return false;
            }
        }
    }
}
