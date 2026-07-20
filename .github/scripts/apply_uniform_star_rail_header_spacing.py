from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def replace_once(relative_path: str, old: str, new: str) -> None:
    path = ROOT / relative_path
    text = path.read_text(encoding="utf-8-sig")
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"Expected one match in {relative_path}, found {count}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8")


replace_once(
    "src/Starward/Features/GameRecord/StarRail/ForgottenHallPage.xaml",
    '''                                <Grid Height="300" Padding="20,0,20,0">
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height="52" />''',
    '''                                <Grid MinHeight="300" Padding="20,0,20,0">
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height="Auto" />''',
)

replace_once(
    "src/Starward/Features/GameRecord/StarRail/ForgottenHallPage.xaml",
    '''                                    <!--  名称，回合数  -->
                                    <StackPanel VerticalAlignment="Center" Spacing="2">
                                        <TextBlock FontWeight="Bold"
                                                   MaxLines="2"
                                                   Text="{x:Bind local:StarRailRecordTextHelper.NormalizeStageName(Name)}"
                                                   TextTrimming="None"
                                                   TextWrapping="Wrap" />
                                        <TextBlock FontSize="12" Foreground="{ThemeResource TextFillColorSecondaryBrush}">
                                            <Run Text="{x:Bind lang:Lang.ForgottenHallPage_CyclesUsed}" />
                                            <Run Text="{x:Bind RoundNum}" />
                                        </TextBlock>
                                    </StackPanel>''',
    '''                                    <!--  名称，回合数  -->
                                    <Grid MinHeight="56" Padding="0,8,0,8">
                                        <StackPanel VerticalAlignment="Center" Spacing="2">
                                            <TextBlock FontWeight="Bold"
                                                       MaxLines="2"
                                                       Text="{x:Bind local:StarRailRecordTextHelper.NormalizeStageName(Name)}"
                                                       TextTrimming="None"
                                                       TextWrapping="Wrap" />
                                            <TextBlock FontSize="12" Foreground="{ThemeResource TextFillColorSecondaryBrush}">
                                                <Run Text="{x:Bind lang:Lang.ForgottenHallPage_CyclesUsed}" />
                                                <Run Text="{x:Bind RoundNum}" />
                                            </TextBlock>
                                        </StackPanel>
                                    </Grid>''',
)

replace_once(
    "src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml",
    '''                                <Grid Height="304" Padding="20,0,20,0">
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height="56" />''',
    '''                                <Grid MinHeight="304" Padding="20,0,20,0">
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height="Auto" />''',
)

replace_once(
    "src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml",
    '''                                    <!--  名称，回合数  -->
                                    <StackPanel VerticalAlignment="Center" Spacing="2">
                                        <TextBlock FontWeight="Bold"
                                                   MaxLines="2"
                                                   Text="{x:Bind local:StarRailRecordTextHelper.NormalizeStageName(Name)}"
                                                   TextTrimming="None"
                                                   TextWrapping="Wrap" />
                                        <TextBlock FontSize="12" Foreground="{ThemeResource TextFillColorSecondaryBrush}">
                                            <Run Text="{x:Bind lang:Lang.ForgottenHallPage_CyclesUsed}" />
                                            <Run Text="{x:Bind RoundNum}" />
                                        </TextBlock>
                                    </StackPanel>''',
    '''                                    <!--  名称，回合数  -->
                                    <Grid MinHeight="56" Padding="0,8,0,8">
                                        <StackPanel VerticalAlignment="Center" Spacing="2">
                                            <TextBlock FontWeight="Bold"
                                                       MaxLines="2"
                                                       Text="{x:Bind local:StarRailRecordTextHelper.NormalizeStageName(Name)}"
                                                       TextTrimming="None"
                                                       TextWrapping="Wrap" />
                                            <TextBlock FontSize="12" Foreground="{ThemeResource TextFillColorSecondaryBrush}">
                                                <Run Text="{x:Bind lang:Lang.ForgottenHallPage_CyclesUsed}" />
                                                <Run Text="{x:Bind RoundNum}" />
                                            </TextBlock>
                                        </StackPanel>
                                    </Grid>''',
)

replace_once(
    "src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml",
    '''                                <Grid Height="360" Padding="20,0,20,0">
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height="56" />''',
    '''                                <Grid MinHeight="360" Padding="20,0,20,0">
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height="Auto" />''',
)

replace_once(
    "src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml",
    '''                                    <!--  名称  -->
                                    <StackPanel VerticalAlignment="Center" Spacing="2">
                                        <TextBlock FontSize="18"
                                                   FontWeight="Bold"
                                                   MaxLines="2"
                                                   Text="{x:Bind local:StarRailRecordTextHelper.NormalizeStageName(Name)}"
                                                   TextTrimming="None"
                                                   TextWrapping="Wrap" />
                                    </StackPanel>''',
    '''                                    <!--  名称  -->
                                    <Grid MinHeight="56" Padding="0,8,0,8">
                                        <StackPanel VerticalAlignment="Center" Spacing="2">
                                            <TextBlock FontSize="18"
                                                       FontWeight="Bold"
                                                       MaxLines="2"
                                                       Text="{x:Bind local:StarRailRecordTextHelper.NormalizeStageName(Name)}"
                                                       TextTrimming="None"
                                                       TextWrapping="Wrap" />
                                        </StackPanel>
                                    </Grid>''',
)

print("Uniform Star Rail header spacing applied.")
