from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]


def wrap_avatar_control(relative_path: str, node: str, row: int, column_span: int, margin: str, visibility: str | None) -> None:
    path = ROOT / relative_path
    text = path.read_text(encoding="utf-8-sig")
    marker = f'ItemsSource="{{x:Bind {node}.Avatars}}"'
    marker_pos = text.find(marker)
    if marker_pos < 0:
        raise RuntimeError(f"Could not find {node} avatar control in {relative_path}")

    control_start = text.rfind("<ItemsControl", 0, marker_pos)
    control_end = text.find("</ItemsControl>", marker_pos)
    if control_start < 0 or control_end < 0:
        raise RuntimeError(f"Could not locate {node} ItemsControl boundaries in {relative_path}")
    control_end += len("</ItemsControl>")

    line_start = text.rfind("\n", 0, control_start) + 1
    indent = text[line_start:control_start]
    block = text[control_start:control_end]

    opening_end = block.find(">")
    opening = block[:opening_end + 1]
    body = block[opening_end + 1:]
    for attribute in ("Grid.Row", "Grid.ColumnSpan", "Margin", "Visibility"):
        opening = re.sub(rf'\s+{re.escape(attribute)}="[^"]*"', "", opening)
    inner_block = opening + body
    inner_block = (indent + "    " + inner_block.replace("\n", "\n" + indent + "    ")).rstrip()

    outer_attributes = [
        f'Grid.Row="{row}"',
        f'Grid.ColumnSpan="{column_span}"',
        f'Margin="{margin}"',
        'HorizontalAlignment="Center"',
        'Spacing="8"',
    ]
    if visibility:
        outer_attributes.append(f'Visibility="{visibility}"')

    outer_open = f"<StackPanel {' '.join(outer_attributes)}>"
    buff = (
        f'<local:StarRailBuffButton BuffDescription="{{x:Bind {node}.Buff.Desc}}"\n'
        f'{indent}                              BuffIcon="{{x:Bind {node}.Buff.Icon}}"\n'
        f'{indent}                              BuffName="{{x:Bind {node}.Buff.Name}}"\n'
        f'{indent}                              HorizontalAlignment="Center"\n'
        f'{indent}                              x:Load="{{x:Bind {node}.Buff, Converter={{StaticResource ObjectToBoolConverter}}}}" />'
    )

    replacement = (
        indent + outer_open + "\n" +
        inner_block + "\n" +
        indent + "    " + buff + "\n" +
        indent + "</StackPanel>"
    )
    text = text[:line_start] + replacement + text[control_end:]
    path.write_text(text, encoding="utf-8")


pure_fiction = "src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml"
wrap_avatar_control(
    pure_fiction,
    "Node1",
    2,
    2,
    "0,0,0,8",
    "{x:Bind IsFast, Converter={StaticResource BoolToVisibilityReversedConverter}}",
)
wrap_avatar_control(
    pure_fiction,
    "Node2",
    4,
    2,
    "0,0,0,8",
    "{x:Bind IsFast, Converter={StaticResource BoolToVisibilityReversedConverter}}",
)
wrap_avatar_control(pure_fiction, "Node3", 1, 2, "0,0,0,8", None)

pure_path = ROOT / pure_fiction
pure_text = pure_path.read_text(encoding="utf-8-sig")
if pure_text.count('<Grid x:Name="Node3Root"\n                                      Height="132"') != 1:
    raise RuntimeError("Unexpected Pure Fiction Node3 height declaration")
pure_text = pure_text.replace(
    '<Grid x:Name="Node3Root"\n                                      Height="132"',
    '<Grid x:Name="Node3Root"\n                                      MinHeight="132"',
    1,
)
pure_path.write_text(pure_text, encoding="utf-8")

apocalyptic_shadow = "src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml"
wrap_avatar_control(
    apocalyptic_shadow,
    "Node1",
    3,
    3,
    "0,4,0,8",
    "{x:Bind IsFast, Converter={StaticResource BoolToVisibilityReversedConverter}}",
)
wrap_avatar_control(
    apocalyptic_shadow,
    "Node2",
    6,
    3,
    "0,4,0,8",
    "{x:Bind IsFast, Converter={StaticResource BoolToVisibilityReversedConverter}}",
)
wrap_avatar_control(apocalyptic_shadow, "Node3", 2, 2, "0,4,0,8", None)

shadow_path = ROOT / apocalyptic_shadow
shadow_text = shadow_path.read_text(encoding="utf-8-sig")
if shadow_text.count('<Grid Height="144"\n                                      x:Name="Node3Root"') != 1:
    raise RuntimeError("Unexpected Apocalyptic Shadow Node3 height declaration")
shadow_text = shadow_text.replace(
    '<Grid Height="144"\n                                      x:Name="Node3Root"',
    '<Grid MinHeight="144"\n                                      x:Name="Node3Root"',
    1,
)
shadow_path.write_text(shadow_text, encoding="utf-8")

print("Star Rail buff UI applied successfully.")
