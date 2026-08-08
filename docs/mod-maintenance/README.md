# Starward mod maintenance guide

This branch is the canonical preservation snapshot for the custom Starward modifications previously released as **Starward 0.18.0 fix mod**.

## Canonical baseline

- Upstream repository: `Scighost/Starward`
- Upstream baseline version: `0.18.0`
- Upstream baseline commit: `433dc28a7a798a79a92e81c89f702f9f13b18f9d`
- Canonical mod snapshot branch: `maintenance/starward-mod`
- Source snapshot branch: `fix/localized-time-units-0.18.0`

Do **not** use the full historical commit count of the old mod branch as a cherry-pick list. The old implementation branch contains many intermediate commits. For future upgrades, treat the final tree state of `maintenance/starward-mod` versus the exact upstream baseline above as the source of truth.

## Goal for every future Starward release

When upstream publishes a new release:

1. Preserve the new upstream release unchanged in a branch/tag such as `upstream-X.Y.Z`.
2. Create a new mod integration branch from that upstream release, for example `mod/X.Y.Z`.
3. Reapply the feature groups below in dependency order.
4. Prefer semantic/manual porting for files that upstream changed since the previous baseline instead of blindly replacing them.
5. Build and test x64 + ARM64, Debug + Release before merging into the main fork/releasing.
6. Keep `maintenance/starward-mod` untouched as the last known-good reference until the new port is verified.

## Feature groups to preserve

### 1. HoYoLAB Toolbox automatic game-record refresh — critical

This is the highest-priority custom feature.

Core behavior:

- Background scheduler starts with Starward and waits for application/database startup before network work.
- Supported games: Genshin Impact (`hk4e`), Honkai: Star Rail (`hkrpg`), Zenless Zone Zero (`nap`).
- Per-game schedule: Disabled / On startup / Daily / Weekly / Monthly.
- Missed scheduled refreshes run after the next Starward startup.
- Failed scheduled attempts use a 2-hour retry cooldown.
- A semaphore prevents concurrent refresh jobs.
- Scheduler wakes immediately when the user changes a schedule instead of polling continuously.
- Manual refresh is available for one game or all games.
- The settings page shows the last successful refresh and next scheduled refresh.
- Successful refreshes publish `GameRecordAutoRefreshCompletedMessage` through `WeakReferenceMessenger` so opened record pages can reload.
- Chinese/Bilibili endpoints update Hyperion device fingerprint before record calls.
- Refresh language follows `AppConfig.Language` / current UI culture.

Persisted settings keys:

- `game_record_auto_refresh_interval_{game}`
- `game_record_auto_refresh_last_time_{game}_{uid}`
- `game_record_auto_refresh_last_attempt_{game}_{uid}`

Data refreshed automatically:

**Genshin Impact**
- Spiral Abyss: current + previous
- Imaginarium Theater
- Stygian Onslaught
- Traveler's Diary: all available months and both detail categories

**Honkai: Star Rail**
- Trailblaze Calendar: all available months and both detail categories
- Simulated Universe
- Forgotten Hall: current + previous
- Pure Fiction: current + previous
- Apocalyptic Shadow: current + previous
- Anomaly Arbitration / Challenge Peak

**Zenless Zone Zero**
- Inter-Knot monthly report: all available months and all report data types
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

Porting notes:

- `GameRecordService.cs` is a high-conflict file because upstream regularly changes HoYoLAB endpoints and record models. Always merge custom wrapper/refresh methods into the new upstream implementation instead of replacing the file.
- `App.xaml.cs`, service registration, and setting navigation are small hooks; reapply them after the core service compiles.
- Preserve config key names so existing user schedules survive upgrades.

### 2. HoYoLAB Auto Refresh localization facade — critical

Files:

- `src/Starward.Language/HoYoLabAutoRefreshText.cs`
- `src/Starward.Language/Lang.resx`
- `src/Starward.Language/Lang.ru-RU.resx`
- all other built-in `Lang.*.resx` files touched by the mod

Purpose:

- Provides stable UI strings for the auto-refresh settings page.
- Keeps Russian and English complete and provides fallback-compatible entries for all built-in languages.
- Avoids relying on generated resource properties that may temporarily lag behind `.resx` changes during project migrations/builds.

Porting notes:

- Localization files are a guaranteed conflict hotspot after Crowdin updates.
- Never copy an old `.resx` over a newer upstream `.resx`; merge only the custom resource entries by key.
- If upstream resource generation changes, keep the user-visible strings but adapt the facade to the new generated API.

### 3. Localized time units

Files:

- `src/Starward.Language/LocalizedTimeFormatter.cs`
- `src/Starward/Converters/TimeSpanToStringConverter.cs`
- time-unit resource entries in `Lang*.resx`
- call-site changes in game record / play-time UI

Behavior:

- Localized hour/minute/second abbreviations.
- Russian: `ч`, `м`, `с`; English: `h`, `m`, `s`.
- Consistent spacing between value and unit.
- Shared formatting for hour/minute/second combinations.
- Negative durations normalize safely to zero.

Affected UI includes Play Time, Imaginarium Theater, Stygian Onslaught and Shiyu Defense.

### 4. Honkai: Star Rail record UI improvements

Preserve:

- Dynamic wrapping/height for long stage, mode, boss and enemy names.
- Missing-space normalization after closing brackets / Roman numerals.
- Pure Fiction selected buff buttons + popup details.
- Pure Fiction Fighting Spirit mechanism title/description support, including API compatibility via extension data.
- Apocalyptic Shadow selected buff buttons + `desc_mi18n` support.
- Third-team support where available.
- Dynamic minimum card heights and corrected spacing/backgrounds.
- Forgotten Hall / Pure Fiction / Apocalyptic Shadow / Challenge Peak layout fixes.

Primary custom files:

- `src/Starward/Features/GameRecord/StarRail/StarRailBuffButton.xaml`
- `src/Starward/Features/GameRecord/StarRail/StarRailBuffButton.xaml.cs`
- `src/Starward/Features/GameRecord/StarRail/HoYoLabMechanismBuffLabels.cs`
- `src/Starward/Features/GameRecord/StarRail/StarRailRecordTextHelper.cs`
- `src/Starward.Core/GameRecord/StarRail/PureFiction/PureFictionBuff.cs`
- `src/Starward.Core/GameRecord/StarRail/ApocalypticShadow/ApocalypticShadowBuff.cs`
- `src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml`
- `src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml`
- `src/Starward/Features/GameRecord/StarRail/ForgottenHallPage.xaml`
- `src/Starward/Features/GameRecord/StarRail/ChallengePeakPage.xaml`

Compatibility requirement:

- Fighting Spirit mechanism names are stored inside the existing JSON record payload; do not introduce a database schema migration unless a future upstream model forces it.
- Old records must remain readable.

### 5. Genshin account spending-history URL compatibility

File:

- `src/Starward.Core/SelfQuery/SelfQueryClient.cs`

Preserve:

- New official HoYoLAB URL: `https://gs.hoyoverse.com/event/user-game-search/genshin/index.html`
- Legacy URL support: `https://cs.hoyoverse.com/event/user-game-search/hk4e/index.html`
- HTTPS-only validation through `Uri` parsing.
- `game_biz` validation against selected region.
- `#/moneyRecord` fragment must not break import.

### 6. Gacha statistics layout fix

Files:

- `src/Starward/Features/Gacha/GachaStatsCard.xaml`
- `src/Starward/Features/Gacha/GachaTypeStats.cs`

Preserve:

- Prevent overlap in the five-star average / UP / total row.
- Keep separate columns for average, UP stats and count.
- Allow long localized text to wrap instead of clipping.

### 7. ZZZ / Genshin / PlayTime small UI fixes

Preserve only the custom behavior that is still missing upstream. Do not overwrite newer upstream layouts.

Relevant paths include:

- `src/Starward/Features/GameRecord/Genshin/ImaginariumTheaterPage.xaml.cs`
- `src/Starward/Features/GameRecord/Genshin/StygianOnslaughtPage.xaml`
- `src/Starward/Features/GameRecord/Genshin/StygianOnslaughtPage.xaml.cs`
- `src/Starward/Features/GameRecord/ZZZ/ShiyuDefensePage.xaml.cs`
- `src/Starward/Features/PlayTime/PlayTimeButton.xaml.cs`

## Upgrade conflict policy

Use three categories when porting to a new upstream release:

### A — Add-only custom files

Usually safe to copy first, then fix namespaces/API changes if compilation fails.

Examples:

- `GameRecordAutoRefreshService.cs`
- `GameRecordAutoRefreshInterval.cs`
- `GameRecordAutoRefreshResult.cs`
- `GameRecordAutoRefreshCompletedMessage.cs`
- `HoyolabToolboxAutoRefreshSetting.xaml(.cs)`
- `HoYoLabAutoRefreshText.cs`
- `LocalizedTimeFormatter.cs`
- `StarRailBuffButton.xaml(.cs)`
- `HoYoLabMechanismBuffLabels.cs`
- `StarRailRecordTextHelper.cs`

### B — Small integration hooks

Merge manually into upstream current files.

Examples:

- `App.xaml.cs`
- `AppConfig.ServiceProvider.cs`
- `AppConfig.Setting.cs`
- `SettingPage.xaml(.cs)`
- `GameRecordPage.xaml.cs`

### C — High-conflict upstream-owned files

Never replace wholesale. Compare the old baseline to the old mod, identify the intent, and re-implement that intent on top of the new upstream file.

Examples:

- `GameRecordService.cs`
- `Lang*.resx`
- Star Rail record models/pages
- Gacha UI
- ZZZ record pages
- `SelfQueryClient.cs`

## Current known upstream delta after the baseline

As of 2026-08-08, upstream `0.18.1` (`346e924d25ccaa0df9e53a71492814a96b418c49`) already changes several files that overlap this mod:

- `src/Starward/Features/GameRecord/GameRecordService.cs`
- `src/Starward/Features/GameRecord/ZZZ/ShiyuDefensePage.xaml.cs`
- all localization `.resx` files

Therefore a 0.18.1 port should manually merge those files. The new upstream Deadly Assault layout should be preserved and the auto-refresh call should target the current upstream service/API rather than restoring the 0.18.0 implementation wholesale.

## Verification checklist after every port

1. Build Debug x64.
2. Build Release x64.
3. Build Debug ARM64.
4. Build Release ARM64.
5. Launch with an existing user database; confirm no migration/crash.
6. Open HoYoLAB Toolbox Auto Refresh settings.
7. Confirm saved schedules survive restart.
8. Test manual refresh separately for Genshin, Star Rail and ZZZ.
9. Test Refresh All.
10. Confirm last/next refresh status updates.
11. Set a schedule, restart Starward, confirm startup/due refresh behavior.
12. Simulate one failed operation and confirm other operations continue.
13. Confirm failed scheduled jobs do not retry more often than every 2 hours.
14. Open affected game-record pages and confirm they reload after successful background refresh.
15. Verify Russian + English UI, time abbreviations, long-title wrapping and Star Rail buff details.
16. Verify Genshin spending-history import with both new and legacy URLs.
17. Verify gacha statistics layout with long localized strings.

## Source-of-truth rule

For future maintenance, use:

`diff(upstream baseline 433dc28a..., maintenance/starward-mod)`

as the authoritative definition of this mod. Release notes are descriptive; the branch tree is canonical.
