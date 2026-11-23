# Starward 0.18.2 fix mod

Based on the official **Starward 0.18.2** release.

## Ported modifications

- Fully ported HoYoLAB Toolbox automatic game-record refresh for Genshin Impact, Honkai: Star Rail and Zenless Zone Zero.
- Preserved independent per-game schedules: disabled, on startup, daily, weekly and monthly.
- Preserved manual refresh for one/all games, last/next refresh status, and the two-hour retry cooldown after failed scheduled attempts.
- Background refresh uses the API client associated with each role and does not change the state of the currently opened HoYoLAB Toolbox page.
- Expired HoYoLAB `cookie_token_v2` values are refreshed automatically through a saved `stoken_v2`, then the failed HSR calendar request is retried once.
- Preserved localized time units and related time-display fixes.
- Preserved Honkai: Star Rail record improvements: long-name wrapping, Pure Fiction and Apocalyptic Shadow buff cards, Fighting Spirit mechanism support, and related layout fixes.
- Preserved both new and legacy Genshin account spending-history URLs.
- Preserved gacha statistics layout fixes and related UI corrections.

## Starward 0.18.2 compatibility

- Preserved the official detailed playtime statistics, charts, merged official/Bilibili records, and elapsed-time display on the launch button.
- The new playtime button and statistics dialog keep the mod's localized duration units.
- Preserved the official 0.18.2 Shiyu Defense layout fix together with localized completion-time formatting.
- Preserved the official new Deadly Assault trial/adversity hard-mode support.
- Preserved the `HasHard`, `HardTotalScore`, `HardTotalStar` fields and their official database migration.
- Official 0.18.2 Crowdin resource files are not replaced by old versions. Auto-refresh text and time units use safe code fallbacks, including complete Russian fallbacks.
- All other official 0.18.2 files remain on the upstream version unless explicitly modified by the mod.

## Build verification

GitHub Actions successfully built all four configurations:

- Debug x64
- Release x64
- Debug ARM64
- Release ARM64

CI run: `33355533420`.
