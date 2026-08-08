# Starward 0.18.1 fix mod

Based on the official **Starward 0.18.1** release.

## Ported modifications

- Fully ported HoYoLAB Toolbox automatic game-record refresh for Genshin Impact, Honkai: Star Rail and Zenless Zone Zero.
- Preserved independent per-game schedules: disabled, on startup, daily, weekly and monthly.
- Preserved manual refresh for one/all games, last/next refresh status, and the two-hour retry cooldown after failed scheduled attempts.
- Background refresh uses the API client associated with each role and does not change the state of the currently opened HoYoLAB Toolbox page.
- Preserved localized time units and related time-display fixes.
- Preserved Honkai: Star Rail record improvements: long-name wrapping, Pure Fiction and Apocalyptic Shadow buff cards, Fighting Spirit mechanism support, and related layout fixes.
- Preserved both new and legacy Genshin account spending-history URLs.
- Preserved gacha statistics layout fixes and related UI corrections.

## Starward 0.18.1 compatibility

- Preserved the official new Deadly Assault trial/adversity hard-mode support.
- Preserved the new `HasHard`, `HardTotalScore`, `HardTotalStar` fields and the official 0.18.1 database migration.
- Shiyu Defense keeps the new 0.18.1 rank-background images together with the mod's localized completion-time formatting.
- Official 0.18.1 Crowdin resource files are not replaced by old versions. Auto-refresh text and time units use safe code fallbacks, including complete Russian fallbacks.
- All other official 0.18.1 files remain on the upstream version unless explicitly modified by the mod.

## Build verification

GitHub Actions successfully built all four configurations:

- Debug x64
- Release x64
- Debug ARM64
- Release ARM64

CI run: `31258462389`.
