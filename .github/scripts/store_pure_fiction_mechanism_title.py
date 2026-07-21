from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

service_path = ROOT / "src/Starward/Features/GameRecord/GameRecordService.cs"
service = service_path.read_text(encoding="utf-8-sig")

using_marker = "using Starward.Features.Database;\n"
using_line = "using Starward.Features.GameRecord.StarRail;\n"
if using_line not in service:
    if service.count(using_marker) != 1:
        raise RuntimeError("Could not locate GameRecordService using marker")
    service = service.replace(using_marker, using_marker + using_line, 1)

old_method_start = '''    public async Task<PureFictionInfo> RefreshPureFictionInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var info = await _gameRecordClient.GetPureFictionInfoAsync(role, schedule);
        if (info.ScheduleId == 0)
        {
            return info;
        }
        var obj = new
'''

new_method_start = '''    public async Task<PureFictionInfo> RefreshPureFictionInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        string recordLanguage = string.IsNullOrWhiteSpace(AppConfig.Language)
            ? System.Globalization.CultureInfo.CurrentUICulture.Name
            : AppConfig.Language;
        Language = recordLanguage;

        var info = await _gameRecordClient.GetPureFictionInfoAsync(role, schedule);
        if (info.ScheduleId == 0)
        {
            return info;
        }

        string mechanismTitle = HoYoLabMechanismBuffLabels.GetForLanguage(recordLanguage);
        SetPureFictionMechanismTitle(info, mechanismTitle);

        var obj = new
'''

if new_method_start not in service:
    if service.count(old_method_start) != 1:
        raise RuntimeError("Could not locate RefreshPureFictionInfoAsync method start")
    service = service.replace(old_method_start, new_method_start, 1)

list_marker = '''    public List<PureFictionInfo> GetPureFictionInfoList(GameRecordRole role)
'''
helper = '''    private static void SetPureFictionMechanismTitle(PureFictionInfo info, string title)
    {
        if (info.AllFloorDetail is null || string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        foreach (PureFictionFloorDetail floor in info.AllFloorDetail)
        {
            SetPureFictionMechanismTitle(floor.Node1, title);
            SetPureFictionMechanismTitle(floor.Node2, title);
            SetPureFictionMechanismTitle(floor.Node3, title);
        }
    }


    private static void SetPureFictionMechanismTitle(PureFictionNode? node, string title)
    {
        if (node?.Buff is not null && !string.IsNullOrWhiteSpace(node.Buff.SimpleDesc))
        {
            node.Buff.MechanismName = title;
        }
    }


'''

if helper not in service:
    if service.count(list_marker) != 1:
        raise RuntimeError("Could not locate Pure Fiction list method")
    service = service.replace(list_marker, helper + list_marker, 1)

service_path.write_text(service, encoding="utf-8")

page_path = ROOT / "src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml"
page = page_path.read_text(encoding="utf-8-sig")

for node in ("Node1", "Node2", "Node3"):
    description = f'MechanicDescription="{{x:Bind {node}.Buff.SimpleDesc}}"'
    title = f'MechanicTitle="{{x:Bind {node}.Buff.MechanismName}}"'
    replacement = description + "\n                                                                   " + title
    if title in page:
        continue
    if page.count(description) != 1:
        raise RuntimeError(f"Expected one mechanism description binding for {node}")
    page = page.replace(description, replacement, 1)

page_path.write_text(page, encoding="utf-8")

print("Pure Fiction mechanism titles are now stored with records and bound in the UI.")
