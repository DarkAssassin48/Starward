# Starward fix mod — maintenance snapshot

This document describes the verified custom modification set currently ported to **Starward 0.18.2**.

## Current canonical baseline

- Upstream repository: `Scighost/Starward`
- Upstream version: `0.18.2`
- Upstream commit: `ac7c9c495ede338df7d8eb03f098dc901caa74a9`
- Verified port commit: `c4b8e367f0e24d28d9226782d974e4c044bbefa5`
- Testing pull request: `#13`
- Canonical maintenance branch: `agent/starward-testing`
- Version snapshot branch: `mod/0.18.2`

The authoritative mod definition for future upgrades is the semantic difference between the exact upstream baseline above and the canonical maintenance snapshot. Do not replay the historical 0.18.0 development commit sequence.

## Verified build status

GitHub Actions run `33355533420` passed all build targets:

- Debug x64 — passed
- Release x64 — passed
- Debug ARM64 — passed
- Release ARM64 — passed

## Feature groups to preserve

### 1. HoYoLAB Toolbox automatic game-record refresh — critical

Supported games:

- Genshin Impact (`hk4e`)
- Honkai: Star Rail (`hkrpg`)
- Zenless Zone Zero (`nap`)

Schedules are stored separately per game:

- Disabled
- On startup
- Daily
- Weekly
- Monthly

Behavior to preserve:

- Background scheduler works while Starward is running.
- Missed scheduled refreshes execute after the next application start.
- Failed scheduled attempts have a two-hour retry cooldown.
- A semaphore prevents concurrent refresh jobs.
- Schedule changes wake the scheduler immediately; no periodic polling is required.
- Manual refresh is available for one game or all games.
- Settings show last successful and next planned refresh times.
- Successful updates notify open game-record pages through `GameRecordAutoRefreshCompletedMessage`.
- API client selection is role-safe: background refreshes must not change the client selected by the currently open HoYoLAB Toolbox page.
- China/Bilibili roles update Hyperion device fingerprint without changing the currently selected client.
- Existing schedule keys must remain stable across upgrades.

Persistent keys:

- `game_record_auto_refresh_interval_{game}`
- `game_record_auto_refresh_last_time_{game}_{uid}`
- `game_record_auto_refresh_last_attempt_{game}_{uid}`

Automatically refreshed records:

**Genshin Impact**
- Spiral Abyss: current + previous
- Imaginarium Theater
- Stygian Onslaught
- Traveler's Diary: all available months and detail categories

**Honkai: Star Rail**
- Trailblaze Calendar: all available months and detail categories
- Simulated Universe
- Forgotten Hall: current + previous
- Pure Fiction: current + previous
- Apocalyptic Shadow: current + previous
- Anomaly Arbitration / Challenge Peak

**Zenless Zone Zero**
- Inter-Knot monthly report: all available months and report types
- Shiyu Defense: current + previous
- Deadly Assault: current + previous

Primary files:

- `src/Starward/Features/GameRecord/GameRecordAutoRefreshService.cs`
- `src/Starward/Features/GameRecord/GameRecordAutoRefreshInterval.cs`
- `src/Starward/Features/GameRecord/GameRecordAutoRefreshResult.cs`
- `src/Starward/Features/GameRecord/GameRecordAutoRefreshCompletedMessage.cs`
- `src/Starward/Features/Setting/HoyolabToolboxAutoRefreshSetting.xaml`
- `src/Starward/Features/Setting/HoyolabToolboxAutoRefreshSetting.xaml.cs`
- `src/Starward/AppConfig.Setting.cs`
- `src/Starward/AppConfig.ServiceProvider.cs`
- `src/Starward/App.xaml.cs`
- `src/Starward/Features/Setting/SettingPage.xaml`
- `src/Starward/Features/Setting/SettingPage.xaml.cs`
- `src/Starward/Features/GameRecord/GameRecordService.cs`
- `src/Starward/Features/GameRecord/GameRecordPage.xaml.cs`

### 2. Localization strategy

Custom auto-refresh UI text is implemented through `HoYoLabAutoRefreshText.cs`.

For Starward 0.18.2 the mod deliberately does **not** overwrite upstream `Lang*.resx` files. The facade first reads Starward resources when available and otherwise uses built-in fallbacks. Russian and English fallbacks are complete.

This avoids conflicts with Crowdin updates and preserves new upstream translations such as Deadly Assault Trial/Adversity mode strings.

### 3. Localized time formatting

`LocalizedTimeFormatter.cs` provides localized hour/minute/second labels and shared formatting.

- Russian fallback: `ч`, `м`, `с`
- English/default fallback: `h`, `m`, `s`
- Negative values normalize safely to zero.
- User-visible values contain consistent spaces between numbers and units.

Relevant call sites include Play Time, Imaginarium Theater, Stygian Onslaught and Shiyu Defense.

### 4. Honkai: Star Rail game-record UI improvements

Preserve:

- Wrapping and dynamic height for long stage/mode/boss/enemy names.
- Spacing normalization after closing brackets and Roman numerals.
- Pure Fiction selected buff button and detail popup.
- Fighting Spirit mechanism title/description compatibility and persistence inside the existing JSON record payload.
- Apocalyptic Shadow selected buff buttons and description support.
- Third-team support where returned by API.
- Dynamic card heights and corrected layout spacing/backgrounds.
- Forgotten Hall / Pure Fiction / Apocalyptic Shadow / Challenge Peak layout fixes.

Custom helpers/controls:

- `StarRailBuffButton.xaml(.cs)`
- `HoYoLabMechanismBuffLabels.cs`
- `StarRailRecordTextHelper.cs`

### 5. Genshin account spending-history URL compatibility

`SelfQueryClient.cs` preserves support for both:

- `https://gs.hoyoverse.com/event/user-game-search/genshin/index.html`
- `https://cs.hoyoverse.com/event/user-game-search/hk4e/index.html`

Requirements:

- HTTPS-only URI validation.
- Validate `game_biz` against selected region.
- `#/moneyRecord` fragments must not break import.

### 6. Gacha statistics layout

Preserve the non-overlapping five-star statistics row:

- average value
- UP statistics
- total count

Long localized strings must wrap instead of clipping.

## Starward 0.18.2 compatibility decisions

The 0.18.2 port explicitly keeps upstream changes instead of restoring old files.

### Play time statistics

Preserved upstream 0.18.2 functionality:

- detailed playtime statistics and charts
- merged official/Bilibili server records
- elapsed game time on the launch button
- the new `PlayTimeRecordService`, `PlayTimeStatsService`, statistics dialog and chart controls

The mod's `LocalizedTimeFormatter` is layered on the new playtime button instead of restoring the removed 0.18.1 implementation.

### Deadly Assault

Preserved the upstream adversity/hard-mode support introduced before 0.18.2:

- `HasHard`
- `HardTotalScore`
- `HardTotalStar`
- database schema v20 columns
- hard-mode API endpoint/model changes

The mod's role-safe `GameRecordService` API-client selection is layered on top of those changes.

### Shiyu Defense

The merged page keeps both:

- upstream 0.18.2 layout fix and `RankBackground(...)` / rank-background images
- mod `LocalizedTimeFormatter.FormatMinutesSeconds(...)` timing output

### Localization

All official 0.18.2 Crowdin `.resx` files are kept unchanged. Custom text/time-unit fallbacks live in code to reduce future merge conflicts.

## Future upstream upgrade procedure

For every new Starward version `X.Y.Z`:

1. Save the exact upstream release commit as `upstream-X.Y.Z`.
2. Create `mod/X.Y.Z` directly from that upstream commit.
3. Compare upstream changes from the previous baseline to the new release.
4. Reapply add-only custom files first.
5. Merge small integration hooks semantically.
6. For high-conflict upstream-owned files, port the intent instead of replacing the file.
7. Preserve new upstream APIs, database migrations, resources and UI changes.
8. Build Debug/Release for x64 and ARM64.
9. Only after all build targets pass, merge into `main`.
10. Update `maintenance/starward-mod` to the verified snapshot and update this document/manifest.

### High-conflict files

Treat these as manual semantic merges:

- `src/Starward/Features/GameRecord/GameRecordService.cs`
- `src/Starward.Language/Lang*.resx`
- Star Rail game-record models/pages
- ZZZ game-record pages
- `src/Starward.Core/SelfQuery/SelfQueryClient.cs`
- gacha UI files

## Verification checklist

After each future port:

1. Build Debug x64.
2. Build Release x64.
3. Build Debug ARM64.
4. Build Release ARM64.
5. Launch with an existing user database.
6. Open HoYoLAB Toolbox Auto Refresh settings.
7. Confirm schedules survive restart.
8. Test manual refresh for Genshin, Star Rail and ZZZ separately.
9. Test Refresh All.
10. Confirm last/next refresh status updates.
11. Confirm scheduled/startup refresh behavior.
12. Confirm a failed scheduled attempt observes the 2-hour cooldown.
13. Confirm open record pages reload after a successful background refresh.
14. Verify Russian and English auto-refresh UI and time formatting.
15. Verify Star Rail long-name wrapping and buff detail popups.
16. Verify both Genshin spending-history URL formats.
17. Verify gacha statistics with long localized strings.
18. Verify ZZZ Deadly Assault adversity/hard mode remains functional.
