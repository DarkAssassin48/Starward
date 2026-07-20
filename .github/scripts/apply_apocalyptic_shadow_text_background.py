from pathlib import Path

path = Path("src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml")
text = path.read_text(encoding="utf-8-sig")
old = '''                        <Image Grid.Column="2"
                               Margin="4,0"
                               HorizontalAlignment="Stretch"
                               VerticalAlignment="Stretch"
                               RenderTransformOrigin="0.5,0.5"
                               Source="{StaticResource StarwardModeBossBg}"
                               Stretch="Fill"
                               Visibility="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss, Converter={StaticResource ObjectToVisibilityConverter}}">
                            <Image.RenderTransform>
                                <ScaleTransform ScaleX="-1" />
                            </Image.RenderTransform>
                        </Image>
                        <StackPanel Grid.Column="2"
                                    HorizontalAlignment="Center"
                                    Orientation="Horizontal"
                                    Spacing="12"
                                    x:Name="TierceBossRoot"
                                    x:Load="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss, Converter={StaticResource ObjectToBoolConverter}}">
                            <sc:CachedImage Width="40"
                                            Height="40"
                                            VerticalAlignment="Center"
                                            CornerRadius="20"
                                            Source="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss.Icon}" />
                            <TextBlock MaxWidth="150"
                                       VerticalAlignment="Center"
                                       FontSize="12"
                                       TextTrimming="None"
                                       TextWrapping="Wrap">
                                <Run Text="{x:Bind lang:Lang.ForgottenHallPage_TeamSetup}" />
                                <Run Text="3" />
                                <LineBreak />
                                <Run Foreground="{ThemeResource TextFillColorSecondaryBrush}" Text="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss.Name}" />
                            </TextBlock>
                        </StackPanel>'''
new = '''                        <StackPanel Grid.Column="2"
                                    HorizontalAlignment="Center"
                                    Orientation="Horizontal"
                                    Spacing="12"
                                    x:Name="TierceBossRoot"
                                    x:Load="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss, Converter={StaticResource ObjectToBoolConverter}}">
                            <sc:CachedImage Width="40"
                                            Height="40"
                                            VerticalAlignment="Center"
                                            CornerRadius="20"
                                            Source="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss.Icon}" />
                            <Grid MaxWidth="158" VerticalAlignment="Center">
                                <Image HorizontalAlignment="Stretch"
                                       VerticalAlignment="Stretch"
                                       IsHitTestVisible="False"
                                       RenderTransformOrigin="0.5,0.5"
                                       Source="{StaticResource StarwardModeBossBg}"
                                       Stretch="Fill">
                                    <Image.RenderTransform>
                                        <ScaleTransform ScaleX="-1" />
                                    </Image.RenderTransform>
                                </Image>
                                <TextBlock MaxWidth="150"
                                           Margin="4,2"
                                           VerticalAlignment="Center"
                                           FontSize="12"
                                           TextTrimming="None"
                                           TextWrapping="Wrap">
                                    <Run Text="{x:Bind lang:Lang.ForgottenHallPage_TeamSetup}" />
                                    <Run Text="3" />
                                    <LineBreak />
                                    <Run Foreground="{ThemeResource TextFillColorSecondaryBrush}" Text="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss.Name}" />
                                </TextBlock>
                            </Grid>
                        </StackPanel>'''
count = text.count(old)
if count != 1:
    raise RuntimeError(f"Expected one current tierce boss block, found {count}")
path.write_text(text.replace(old, new, 1), encoding="utf-8")
print("Apocalyptic Shadow background now follows text size.")
