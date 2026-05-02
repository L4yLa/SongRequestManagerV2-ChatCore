# Copilot Context — Song Download Race Condition & Playlist Duplication Bug

## 概要 / Overview

このMODはBeatSaverからの楽曲ダウンロードとBeatSaberへの追加を担当している。  
**症状**:
1. ダウンロード後、曲がすぐに選択・再生されない（1回目）
2. 同じ曲を再度ダウンロードしようとすると正常に選択・再生されるが、プレイリストに**曲が重複**する

This MOD is responsible for downloading songs from BeatSaver and adding them to Beat Saber.  
**Symptoms**:
1. After download the song is not immediately loaded or selected (first attempt)
2. A second download attempt selects and plays it correctly, but **duplicates the song** in the playlist

---

## 原因分析 / Root Cause Analysis

### Bug 1 (主要因 / Primary) — `WaitForRefreshAndSchroll` の競合状態

**ファイル / File:** `SongRequestManagerV2/Views/SRMButton.cs`  
**行 / Lines:** 317–319

```csharp
// Line 317: 既存のロードが完了するまで待機
yield return new WaitWhile(() => !Loader.AreSongsLoaded && Loader.AreSongsLoading);

// Line 318: 新しくExtractしたフォルダを取り込むため再スキャンを要求
Loader.Instance.RefreshSongs(false);

// Line 319: ★ BUG — 再スキャン完了を待つつもりだが即座に通過してしまう
yield return new WaitWhile(() => !Loader.AreSongsLoaded && Loader.AreSongsLoading);
```

**問題の詳細 / Detail:**  
`Loader.Instance.RefreshSongs(false)` は非同期にスキャンをキューに積む。  
`RefreshSongs` を呼んだ直後のフレームでは SongCore がまだフラグを更新していないため、  
`AreSongsLoaded = true`、`AreSongsLoading = false` のまま。  
結果として `WaitWhile` の条件 `!true && false = false` が即座に満たされ、  
`ScrollToLevel` が**スキャン完了前に**呼ばれてしまう。

`RefreshSongs(false)` queues its scan asynchronously. On the very next frame after the call,
SongCore has not yet set `AreSongsLoading = true`, so the condition `!true && false` is already `false`
and `WaitWhile` exits immediately — racing ahead to `ScrollToLevel` before the new song folder is indexed.

---

### Bug 2 (副次因 / Secondary) — `ScrollToLevel` のサイレント失敗

**ファイル / File:** `SongRequestManagerV2/UI/SongListUtils.cs`  
**行 / Lines:** 55–58

```csharp
var song = isWip
    ? Loader.GetLevelById($"custom_level_{levelID.Split('_').Last().ToUpper()} WIP")
    : Loader.GetLevelByHash(levelID.Split('_').Last()); // ← Bug1 のせいで null になる

if (song == null) {
    yield break; // サイレントに終了。エラーも再試行もなし
}
```

Bug 1 の競合状態により `GetLevelByHash` が `null` を返す。  
`yield break` によって何も起こらず、ユーザーへのフィードバックも一切ない。

Due to Bug 1's race condition, `GetLevelByHash` returns `null`.  
`yield break` exits silently with no feedback to the user, masking the root cause entirely.

---

### Bug 3 (副次因 / Secondary) — ディレクトリパス計算が重複ダウンロードを引き起こす

**ファイル / File:** `SongRequestManagerV2/Views/SRMButton.cs`  
**行 / Lines:** 243, 246, 286–298

```csharp
// Line 243: ハッシュチェックより先にディレクトリパスを計算
var currentSongDirectory = this.CreateSongDirectory(request);
var songHash = request.SongVersion["hash"].Value.ToUpper();

if (Loader.GetLevelByHash(songHash) == null) { // Line 246
    // ダウンロード & 展開 ...
```

`CreateSongDirectory` は既存ディレクトリがあればカウンタサフィックスを付けた新パスを返す:

```csharp
while (Directory.Exists(result)) {
    result = $"{result.Substring(0, resultLength)}({count})"; // (1), (2), ...
    count++;
}
```

2回目の試みでは最初のフォルダ `1a2b (Title - Author)/` がすでに存在するため、  
`(1)` サフィックス付きの新フォルダに再度ZIPを展開する。  
SongCore は両方のフォルダをロードするため、プレイリストに**同じ曲が2回**現れる。

On the second attempt the first folder already exists, so a new `(1)` directory is computed.
The ZIP is downloaded and extracted again into the new path, and SongCore then loads both
directories — causing the duplicate in the playlist.

---

## エンドツーエンドのシナリオ / End-to-End Scenario

### 1回目のPLAY（曲がSongCoreに未登録）

| ステップ | ファイル:行 | 内容 |
|---|---|---|
| PLAYボタン押下 | `RequestBotListView.cs:475` | `PlayProcessEvent` 発火 |
| キューから削除 | `SRMButton.cs:236-237` | 曲を削除、ステータスをPlayedに |
| ディレクトリ計算 | `SRMButton.cs:243` | `CustomLevels/1a2b (Title - Author)/` |
| ハッシュチェック | `SRMButton.cs:246` | `GetLevelByHash` → null → ダウンロード分岐へ |
| ZIP展開 | `SRMButton.cs:247-262` | 上記ディレクトリに展開 |
| コルーチン開始 | `SRMButton.cs:263` | `WaitForRefreshAndSchroll` 実行 |
| 待機(line 317) | `SRMButton.cs:317` | ロード中でないため即通過 |
| `RefreshSongs` | `SRMButton.cs:318` | 非同期スキャンをキュー |
| 待機(line 319) | `SRMButton.cs:319` | **★ 競合：即座に通過** |
| `ScrollToLevel` 呼び出し | `SRMButton.cs:323` | スキャン未完了のまま呼ばれる |
| ハッシュ検索 | `SongListUtils.cs:55` | **null が返る** |
| サイレント終了 | `SongListUtils.cs:57` | `yield break` — 何も選択されない |

### 2回目のPLAY（スキャン未完了のまま再試行）

| ステップ | ファイル:行 | 内容 |
|---|---|---|
| ディレクトリ計算 | `SRMButton.cs:243` | 既存フォルダあり → `(1)` サフィックスの新パス |
| ハッシュチェック | `SRMButton.cs:246` | まだ null → 再度ダウンロード分岐へ |
| 重複ZIP展開 | `SRMButton.cs:247-262` | `(1)` ディレクトリに**重複展開** |
| `RefreshSongs` | `SRMButton.cs:318` | 両フォルダをロード |
| `ScrollToLevel` | `SRMButton.cs:323` | 今度はヒット → 選択・再生 ✓ |
| 結果 | — | **プレイリストに同じ曲が2つ** |

---

## 修正方針 / Suggested Fixes

### Fix 1 — `WaitForRefreshAndSchroll` の待機条件を修正

**ファイル / File:** `SongRequestManagerV2/Views/SRMButton.cs`  
**Lines:** 317–319

現状の問題は「RefreshSongs後にロード開始を確認してから待機する」ロジックがない点。  
修正案：`RefreshSongs` の後にロードが完全に終わるまで適切に待機する。

```csharp
// Before (buggy):
yield return new WaitWhile(() => !Loader.AreSongsLoaded && Loader.AreSongsLoading);
Loader.Instance.RefreshSongs(false);
yield return new WaitWhile(() => !Loader.AreSongsLoaded && Loader.AreSongsLoading);

// After (fixed):
yield return new WaitWhile(() => !Loader.AreSongsLoaded && Loader.AreSongsLoading);
Loader.Instance.RefreshSongs(false);
// RefreshSongs はフラグを非同期に設定するため、1フレーム待機してからロード完了を確認
yield return null; // フラグが更新されるのを1フレーム待つ / wait one frame for flags to update
yield return new WaitWhile(() => Loader.AreSongsLoading || !Loader.AreSongsLoaded);
```

### Fix 2 — 重複ダウンロードを防ぐ：ディレクトリの事前存在チェック

**ファイル / File:** `SongRequestManagerV2/Views/SRMButton.cs`  
**Lines:** 243, 246-264

ダウンロード済みのフォルダが既存である場合、再ダウンロードをスキップする。  
`CreateSongDirectory` はフォルダが存在しない場合のみ `currentSongDirectory` を使うべき。

```csharp
// Before (buggy):
var currentSongDirectory = this.CreateSongDirectory(request);
var songHash = request.SongVersion["hash"].Value.ToUpper();

if (Loader.GetLevelByHash(songHash) == null) {
    // ← SongCoreに未登録でも実際にはディスク上に存在することがある
    var result = await request.DownloadZip(...);
    archive.ExtractToDirectory(currentSongDirectory);
    ...
}

// After (fixed): ディスク上の存在もチェック / also check disk existence
var songHash = request.SongVersion["hash"].Value.ToUpper();

if (Loader.GetLevelByHash(songHash) == null) {
    // ディスク上に既存フォルダがない場合のみダウンロード
    var currentSongDirectory = this.CreateSongDirectory(request);
    var result = await request.DownloadZip(...);
    archive.ExtractToDirectory(currentSongDirectory);
    ...
}
```

### Fix 3 — `ScrollToLevel` での失敗時フィードバック追加（任意だが推奨）

**ファイル / File:** `SongRequestManagerV2/UI/SongListUtils.cs`  
**Lines:** 55–58

```csharp
// Before:
if (song == null) {
    yield break;
}

// After:
if (song == null) {
    Logger.Debug($"[ScrollToLevel] Level not found for hash: {levelID}. SongCore scan may still be in progress.");
    yield break;
}
```

---

## 関連ファイル / Relevant Files

| ファイル | 役割 |
|---|---|
| `SongRequestManagerV2/Views/SRMButton.cs` | PLAYボタン処理・ダウンロード・`WaitForRefreshAndSchroll` |
| `SongRequestManagerV2/UI/SongListUtils.cs` | SongCoreでの曲検索・スクロール・選択 |
| `SongRequestManagerV2/Bots/SongRequest.cs` | `DownloadZip()` / SongRequest モデル |
| `SongRequestManagerV2/Bots/RequestBot.cs` | キュー管理・`DequeueRequest` |
| `SongRequestManagerV2/Bots/RequestManager.cs` | リクエストキューの読み書き |
| `SongRequestManagerV2/Views/RequestBotListView.cs` | UI・`PlayButtonClick` イベント |

---

## 調査者メモ / Investigator Notes

- `SongCore.Loader.GetLevelByHash()` は SongCore のインメモリキャッシュに対して検索する。  
  `RefreshSongs` が非同期スキャンを**完了**するまでは新しいフォルダはキャッシュに存在しない。
- `WaitWhile(() => !Loader.AreSongsLoaded && Loader.AreSongsLoading)` という条件は  
  「ロード中かつ未完了」の両方が同時に true の場合のみ待機する論理。  
  `RefreshSongs` 後にフラグがまだ反映されていない 1フレームの間にこの条件が評価されると  
  誤って通過してしまう。
- `Loader.SongsLoadedEvent` イベントを使う方法（コールバック駆動の待機）が  
  より堅牢な代替手段となりうる。`RequestBotListView.cs:508` では既にこのイベントを  
  `SongLoader_SongsLoadedEvent` として購読している実装例がある。

`Loader.SongsLoadedEvent` (already subscribed in `RequestBotListView.cs:508` as `SongLoader_SongsLoadedEvent`)
provides a callback-driven alternative to the polling approach and would be a more robust solution.
