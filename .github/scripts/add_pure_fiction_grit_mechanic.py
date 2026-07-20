from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

page = ROOT / "src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml"
text = page.read_text(encoding="utf-8-sig")

for node in ("Node1", "Node2", "Node3"):
    marker = f'BuffDescription="{{x:Bind {node}.Buff.Desc}}"'
    addition = marker + f'\n                                                                   MechanicDescription="{{x:Bind {node}.Buff.SimpleDesc}}"'
    if addition in text:
        continue
    count = text.count(marker)
    if count != 1:
        raise RuntimeError(f"Expected one {node} buff binding, found {count}")
    text = text.replace(marker, addition, 1)

page.write_text(text, encoding="utf-8")

translations = {
    "Lang.resx": "Grit Mechanic",
    "Lang.de-DE.resx": "Kampfgeist-Mechanik",
    "Lang.es-ES.resx": "Mecánica de Espíritu de Combate",
    "Lang.it-IT.resx": "Meccanica dello Spirito combattivo",
    "Lang.ja-JP.resx": "闘志メカニズム",
    "Lang.ko-KR.resx": "투지 메커니즘",
    "Lang.ru-RU.resx": "Механика Боевого духа",
    "Lang.th-TH.resx": "กลไกจิตวิญญาณการต่อสู้",
    "Lang.vi-VN.resx": "Cơ chế Ý chí Chiến đấu",
    "Lang.zh-CN.resx": "战意机制",
    "Lang.zh-HK.resx": "戰意機制",
    "Lang.zh-TW.resx": "戰意機制",
}

resource_dir = ROOT / "src/Starward.Language"
key = "PureFictionPage_GritMechanic"

for filename, value in translations.items():
    path = resource_dir / filename
    resource = path.read_text(encoding="utf-8-sig")
    if f'name="{key}"' in resource:
        continue
    entry = (
        f'  <data name="{key}" xml:space="preserve">\n'
        f'    <value>{value}</value>\n'
        f'  </data>\n'
    )
    if "</root>" not in resource:
        raise RuntimeError(f"Missing </root> in {filename}")
    path.write_text(resource.replace("</root>", entry + "</root>", 1), encoding="utf-8")

print("Pure Fiction grit mechanic bindings and resources added.")
