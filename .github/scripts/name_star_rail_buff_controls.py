from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def add_names(relative_path: str, prefix: str) -> None:
    path = ROOT / relative_path
    text = path.read_text(encoding="utf-8-sig")
    for node in ("Node1", "Node2", "Node3"):
        old = f'<local:StarRailBuffButton BuffDescription="{{x:Bind {node}.Buff.Desc}}"'
        new = f'<local:StarRailBuffButton x:Name="{prefix}{node}BuffButton"\n                                                                   BuffDescription="{{x:Bind {node}.Buff.Desc}}"'
        count = text.count(old)
        if count != 1:
            raise RuntimeError(f"Expected one {node} buff control in {relative_path}, found {count}")
        text = text.replace(old, new, 1)
    path.write_text(text, encoding="utf-8")


add_names(
    "src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml",
    "PureFiction",
)
add_names(
    "src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml",
    "ApocalypticShadow",
)

print("Named all Star Rail buff controls.")
