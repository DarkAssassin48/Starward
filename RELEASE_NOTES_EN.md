# Starward 0.18.0 fix mod

Based on the original **Starward 0.18.0 — Dreamland Fest** release.

## Time localization

- Added localized short labels for hours, minutes, and seconds to every bundled Starward language.
- Russian uses `ч`, `м`, `с`; English uses `h`, `m`, `s`.
- Added a shared formatter for hours, minutes, and seconds combinations.
- Removed hard-coded English time suffixes from playtime statistics, Imaginarium Theater, Stygian Onslaught, Shiyu Defense, and other related interface elements.
- Added spacing between numbers and unit labels; negative durations are safely normalized to zero.

## Wish history

- Fixed overlapping text in the average five-star character and item statistics row.
- Average values, UP statistics, and pull counts are placed in separate columns.
- Long localized strings now wrap instead of being clipped.

## Honkai: Star Rail — game records

- Long stage, mode, boss, and enemy names now wrap onto additional lines.
- Header and card heights automatically adapt to their content.
- Fixed missing spaces after closing parentheses and Roman numerals.
- The improvements apply to Forgotten Hall, Pure Fiction, Apocalyptic Shadow, and Anomaly Arbitration.

### Pure Fiction

- Added a selected-buff button below every team with the official icon and localized name.
- Pressing the button opens a flyout with a larger icon, name, and complete effect description.
- Added a separate **Grit Mechanics** section.
- Added support for `simple_desc_mi18m`, `simple_desc_mi18n`, and compatible fallback reading through `JsonExtensionData`.
- The localized mechanic title is stored directly in the Pure Fiction record JSON in SQLite for every team.
- Refreshing the current and previous periods stores the title in the selected Starward language.
- Older records receive the mechanic title after being refreshed again.
- Standardized spacing between the date, characters, buff, and adjacent teams; all three teams use the same 8 px interval.
- Fixed card heights were replaced with minimum heights so the interface expands correctly.

### Apocalyptic Shadow

- Added a selected-buff button below every team composition with its icon, name, and complete description.
- Added support for `desc_mi18n` from the HoYoLAB response.
- Buffs are supported for the first, second, and third teams.
- Fixed wrapping for difficulty titles, boss names, and team descriptions.
- Fixed the third-team background in the top information section.
- Cards now use dynamic minimum heights and standardized internal spacing.

### Forgotten Hall

- Long stage names wrap to a maximum of two lines instead of being truncated with an ellipsis.
- Header height automatically adapts to localized text.
- Fixed merged names containing Roman numerals and closing parentheses.

### Anomaly Arbitration

- Long boss and enemy names wrap to two lines.
- Reserved space prevents titles from overlapping enemy artwork.
- Fixed missing spaces in compound enemy names.

## Genshin Impact

### Account expense history

- Added support for the new official HoYoLAB URL:
  `https://gs.hoyoverse.com/event/user-game-search/genshin/index.html`
- Preserved support for the older `cs.hoyoverse.com/event/user-game-search/hk4e/index.html` address.
- URLs are parsed using safe `Uri` validation and only HTTPS links are accepted.
- The `game_biz` parameter is checked against the selected region.
- The `#/moneyRecord` fragment no longer prevents importing.
- Fixed `ArgumentException: Input URL is invalid` for the new link format.

## HoYoLAB Toolbox auto refresh

- Ported the complete game-record auto-refresh feature from the previous fork version.
- The scheduler runs in the background while Starward is open and performs missed updates on the next application launch.
- Each game has an independent schedule: Disabled, On startup, Daily, Weekly, or Monthly.
- Added a dedicated **HoYoLAB Toolbox Auto Refresh** settings page.
- Individual games or all games can be refreshed manually.
- The page shows the last successful refresh and the next scheduled refresh time.
- Failed scheduled attempts use a two-hour retry delay.
- Successful refreshes notify open game-record pages.

### Automatically refreshed data

**Genshin Impact**

- Spiral Abyss — current and previous periods.
- Imaginarium Theater.
- Stygian Onslaught.
- Traveler's Diary — all available months.

**Honkai: Star Rail**

- Trailblaze Monthly Calendar.
- Simulated Universe.
- Forgotten Hall — current and previous periods.
- Pure Fiction — current and previous periods.
- Apocalyptic Shadow — current and previous periods.
- Anomaly Arbitration.

**Zenless Zone Zero**

- Inter-Knot Monthly Report.
- Shiyu Defense — current and previous periods.
- Deadly Assault — current and previous periods.

## Other interface fixes

- Stygian Onslaught now uses the current language's seconds abbreviation, and long enemy names wrap onto another line.
- Removed hard-coded English `m` and `s` labels from Imaginarium Theater.
- Shiyu Defense clear times now use localized minute and second labels while preserving two-digit seconds.

## Technical changes

- Added the reusable `StarRailBuffButton` control.
- Added a shared helper for normalizing Star Rail stage titles.
- The Grit Mechanics title is stored inside the existing record JSON, so no database schema migration is required.
- Existing saved data and older input URL formats remain compatible.
- Release and Debug builds are verified for x64 and ARM64.
