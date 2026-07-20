from pathlib import Path

path = Path("src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml")
text = path.read_text(encoding="utf-8-sig")
old = '''                        <Image Grid.Column="2"
                               MaxHeight="44"
                               Margin="40,-2,0,-2"
                               Stretch="Fill"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Bottom"
                               RenderTransformOrigin="0.5,0.5"
                               Source="{StaticResource StarwardModeBossBg}"
                               Visibility="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss, Converter={StaticResource ObjectToVisibilityConverter}}">'''
new = '''                        <Image Grid.Column="2"
                               Margin="4,0"
                               HorizontalAlignment="Stretch"
                               VerticalAlignment="Stretch"
                               RenderTransformOrigin="0.5,0.5"
                               Source="{StaticResource StarwardModeBossBg}"
                               Stretch="Fill"
                               Visibility="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss, Converter={StaticResource ObjectToVisibilityConverter}}">'''
count = text.count(old)
if count != 1:
    raise RuntimeError(f"Expected one tierce boss background block, found {count}")
path.write_text(text.replace(old, new, 1), encoding="utf-8")
print("Apocalyptic Shadow tierce boss background updated.")
