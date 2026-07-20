from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]


def replace_regex(relative_path: str, pattern: str, replacement: str, expected: int = 1) -> None:
    path = ROOT / relative_path
    text = path.read_text(encoding="utf-8-sig")
    result, count = re.subn(pattern, replacement, text, flags=re.MULTILINE | re.DOTALL)
    if count != expected:
        raise RuntimeError(f"Expected {expected} matches in {relative_path}, found {count}: {pattern}")
    path.write_text(result, encoding="utf-8")


helper = ROOT / "src/Starward/Features/GameRecord/StarRail/StarRailRecordTextHelper.cs"
helper.write_text(
    '''using System.Text.RegularExpressions;

namespace Starward.Features.GameRecord.StarRail;


/// <summary>
/// Normalizes stage titles returned by the game-record API for display in the UI.
/// </summary>
public static class StarRailRecordTextHelper
{

    private static readonly Regex MissingSpaceAfterClosingParenthesisRegex = new(
        @"\\)(?=\\p{L})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MissingSpaceAfterRomanNumeralRegex = new(
        @"\\b([IVXLCDM]{1,8})(?=\\p{Lu}\\p{Ll})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);


    public static string NormalizeStageName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string result = MissingSpaceAfterClosingParenthesisRegex.Replace(value.Trim(), ") ");
        return MissingSpaceAfterRomanNumeralRegex.Replace(result, "$1 ");
    }

}
''',
    encoding="utf-8",
)

stage_title_pattern = (
    r'<TextBlock\s+FontWeight="Bold"\s+'
    r'Text="\{x:Bind Name\}"\s+'
    r'TextTrimming="CharacterEllipsis"\s*/>'
)
stage_title_replacement = '''<TextBlock FontWeight="Bold"
                                                   MaxLines="2"
                                                   Text="{x:Bind local:StarRailRecordTextHelper.NormalizeStageName(Name)}"
                                                   TextTrimming="None"
                                                   TextWrapping="Wrap" />'''

replace_regex(
    "src/Starward/Features/GameRecord/StarRail/ForgottenHallPage.xaml",
    stage_title_pattern,
    stage_title_replacement,
)
replace_regex(
    "src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml",
    stage_title_pattern,
    stage_title_replacement,
)

replace_regex(
    "src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml",
    r'<TextBlock\s+Name="TextBlock_Deepest"\s+Grid\.Column="1"\s+'
    r'HorizontalAlignment="Left"\s+VerticalAlignment="Center"\s+'
    r'IsTextTrimmedChanged="TextBlock_Deepest_IsTextTrimmedChanged"\s+'
    r'TextTrimming="CharacterEllipsis">',
    '''<TextBlock Name="TextBlock_Deepest"
                               Grid.Column="1"
                               HorizontalAlignment="Stretch"
                               VerticalAlignment="Center"
                               MaxLines="2"
                               TextTrimming="None"
                               TextWrapping="Wrap">''',
)

replace_regex(
    "src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml",
    r'<TextBlock\s+VerticalAlignment="Center"\s+FontSize="12">',
    '''<TextBlock MaxWidth="150"
                                       VerticalAlignment="Center"
                                       FontSize="12"
                                       TextTrimming="None"
                                       TextWrapping="Wrap">''',
    expected=3,
)

replace_regex(
    "src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml",
    r'<TextBlock\s+FontSize="18"\s+FontWeight="Bold"\s+'
    r'Text="\{x:Bind Name\}"\s+TextTrimming="CharacterEllipsis"\s*/>',
    '''<TextBlock FontSize="18"
                                                   FontWeight="Bold"
                                                   MaxLines="2"
                                                   Text="{x:Bind local:StarRailRecordTextHelper.NormalizeStageName(Name)}"
                                                   TextTrimming="None"
                                                   TextWrapping="Wrap" />''',
)

replace_regex(
    "src/Starward/Features/GameRecord/StarRail/ChallengePeakPage.xaml",
    r'<StackPanel\s+Grid\.Row="0"\s+Margin="20,12,0,0">',
    '''<StackPanel Grid.Row="0"
                                Grid.ColumnSpan="2"
                                Margin="20,12,120,0">''',
)

replace_regex(
    "src/Starward/Features/GameRecord/StarRail/ChallengePeakPage.xaml",
    r'<!--\s+BOSS 名称\s+-->\s*'
    r'<StackPanel\s+Orientation="Horizontal">\s*'
    r'<StackPanel\s+Orientation="Horizontal"\s+'
    r'Visibility="\{x:Bind CurrentChallengePeakRecord\.BossRecord\.HardMode, Converter=\{StaticResource BoolToVisibilityConverter\}\}">\s*'
    r'<TextBlock\s+FontSize="16"\s+FontWeight="Bold">\s*'
    r'<Run\s+Text="\["\s*/>\s*'
    r'<Run\s+Text="\{x:Bind CurrentChallengePeakRecord\.BossInfo\.HardModeNameMi18n\}"\s*/>\s*'
    r'<Run\s+Text="\]"\s*/>\s*'
    r'</TextBlock>\s*</StackPanel>\s*'
    r'<TextBlock\s+FontSize="16"\s+FontWeight="Bold"\s+'
    r'Text="\{x:Bind CurrentChallengePeakRecord\.BossInfo\.NameMi18n\}"\s*/>\s*'
    r'</StackPanel>',
    '''<!--  BOSS 名称  -->
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto" />
                                <ColumnDefinition />
                            </Grid.ColumnDefinitions>
                            <StackPanel Orientation="Horizontal"
                                        Visibility="{x:Bind CurrentChallengePeakRecord.BossRecord.HardMode, Converter={StaticResource BoolToVisibilityConverter}}">
                                <TextBlock FontSize="16" FontWeight="Bold">
                                    <Run Text="[" />
                                    <Run Text="{x:Bind CurrentChallengePeakRecord.BossInfo.HardModeNameMi18n}" />
                                    <Run Text="] " />
                                </TextBlock>
                            </StackPanel>
                            <TextBlock Grid.Column="1"
                                       FontSize="16"
                                       FontWeight="Bold"
                                       MaxLines="2"
                                       Text="{x:Bind CurrentChallengePeakRecord.BossInfo.NameMi18n}"
                                       TextTrimming="None"
                                       TextWrapping="Wrap" />
                        </Grid>''',
)

print("Star Rail text wrapping migration applied successfully.")
