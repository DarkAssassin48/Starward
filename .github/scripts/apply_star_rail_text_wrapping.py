from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def replace_once(relative_path: str, old: str, new: str) -> None:
    path = ROOT / relative_path
    text = path.read_text(encoding="utf-8-sig")
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"Expected one match in {relative_path}, found {count}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8")


def replace_count(relative_path: str, old: str, new: str, expected: int) -> None:
    path = ROOT / relative_path
    text = path.read_text(encoding="utf-8-sig")
    count = text.count(old)
    if count != expected:
        raise RuntimeError(f"Expected {expected} matches in {relative_path}, found {count}")
    path.write_text(text.replace(old, new), encoding="utf-8")


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

replace_once(
    "src/Starward/Features/GameRecord/StarRail/ForgottenHallPage.xaml",
    '''                                        <TextBlock FontWeight="Bold"
                                                   Text="{x:Bind Name}"
                                                   TextTrimming="CharacterEllipsis" />''',
    '''                                        <TextBlock FontWeight="Bold"
                                                   MaxLines="2"
                                                   Text="{x:Bind local:StarRailRecordTextHelper.NormalizeStageName(Name)}"
                                                   TextTrimming="None"
                                                   TextWrapping="Wrap" />''',
)

replace_once(
    "src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml",
    '''                                        <TextBlock FontWeight="Bold"
                                                   Text="{x:Bind Name}"
                                                   TextTrimming="CharacterEllipsis" />''',
    '''                                        <TextBlock FontWeight="Bold"
                                                   MaxLines="2"
                                                   Text="{x:Bind local:StarRailRecordTextHelper.NormalizeStageName(Name)}"
                                                   TextTrimming="None"
                                                   TextWrapping="Wrap" />''',
)

replace_once(
    "src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml",
    '''                    <TextBlock Name="TextBlock_Deepest"
                               Grid.Column="1"
                               HorizontalAlignment="Left"
                               VerticalAlignment="Center"
                               IsTextTrimmedChanged="TextBlock_Deepest_IsTextTrimmedChanged"
                               TextTrimming="CharacterEllipsis">''',
    '''                    <TextBlock Name="TextBlock_Deepest"
                               Grid.Column="1"
                               HorizontalAlignment="Stretch"
                               VerticalAlignment="Center"
                               MaxLines="2"
                               TextTrimming="None"
                               TextWrapping="Wrap">''',
)

replace_count(
    "src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml",
    '''                            <TextBlock VerticalAlignment="Center" FontSize="12">''',
    '''                            <TextBlock MaxWidth="150"
                                       VerticalAlignment="Center"
                                       FontSize="12"
                                       TextTrimming="None"
                                       TextWrapping="Wrap">''',
    3,
)

replace_once(
    "src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml",
    '''                                        <TextBlock FontSize="18"
                                                   FontWeight="Bold"
                                                   Text="{x:Bind Name}"
                                                   TextTrimming="CharacterEllipsis" />''',
    '''                                        <TextBlock FontSize="18"
                                                   FontWeight="Bold"
                                                   MaxLines="2"
                                                   Text="{x:Bind local:StarRailRecordTextHelper.NormalizeStageName(Name)}"
                                                   TextTrimming="None"
                                                   TextWrapping="Wrap" />''',
)

replace_once(
    "src/Starward/Features/GameRecord/StarRail/ChallengePeakPage.xaml",
    '''<StackPanel Grid.Row="0" Margin="20,12,0,0">''',
    '''<StackPanel Grid.Row="0"
                                Grid.ColumnSpan="2"
                                Margin="20,12,120,0">''',
)

replace_once(
    "src/Starward/Features/GameRecord/StarRail/ChallengePeakPage.xaml",
    '''<StackPanel Orientation="Horizontal">
                            <StackPanel Orientation="Horizontal"
                                        Visibility="{x:Bind CurrentChallengePeakRecord.BossRecord.HardMode, Converter={StaticResource BoolToVisibilityConverter}}">
                                <TextBlock FontSize="16" FontWeight="Bold">
                                    <Run Text="["/>
                                    <Run Text="{x:Bind CurrentChallengePeakRecord.BossInfo.HardModeNameMi18n}"/>
                                    <Run Text="]"/>
                                </TextBlock>
                            </StackPanel>
                            <TextBlock FontSize="16"
                                       FontWeight="Bold"
                                       Text="{x:Bind CurrentChallengePeakRecord.BossInfo.NameMi18n}" />
                        </StackPanel>''',
    '''<Grid>
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
