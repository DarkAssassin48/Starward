from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
RESOURCE_DIR = ROOT / "src/Starward.Language"
KEY = "PureFictionPage_GritMechanic"
PATTERN = re.compile(
    rf'\s*<data name="{re.escape(KEY)}" xml:space="preserve">\s*<value>.*?</value>\s*</data>',
    re.DOTALL,
)

files = sorted(RESOURCE_DIR.glob("Lang*.resx"))
if not files:
    raise RuntimeError("No Lang*.resx files found")

removed = 0
for path in files:
    text = path.read_text(encoding="utf-8-sig")
    updated, count = PATTERN.subn("", text)
    if count > 1:
        raise RuntimeError(f"Found duplicate {KEY} entries in {path.name}")
    if count == 1:
        path.write_text(updated, encoding="utf-8")
        removed += 1

if removed != len(files):
    raise RuntimeError(f"Removed {removed} entries from {len(files)} language files")

print(f"Removed {KEY} from {removed} language files")
