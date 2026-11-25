using Starward.Core.Gacha;
using Starward.Features.Gacha.UIGF;

namespace Starward.RegressionTests;

internal static class LegacyGachaTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        yield return ("Legacy gacha archive fills only the missing UID and language", () =>
        {
            var archive = UIGF3File<GachaLogItem>.ParseForImport("""
                {"info":{"uid":"123","lang":"ru-ru"},"list":[{"id":"1"},{"id":"2","uid":"123","lang":"en-us"}]}
                """);
            Check.Equal(2, archive.List.Count);
            Check.Equal(123L, archive.List[0].Uid);
            Check.Equal("ru-ru", archive.List[0].Lang);
            Check.Equal("en-us", archive.List[1].Lang);
            return Task.CompletedTask;
        });
        yield return ("Legacy gacha importer rejects a modern UIGF file without a legacy list", () =>
            Reject("""{"info":{"version":"v4.2"},"hk4e":[{"uid":"123","list":[{"id":"1"}]}]}"""));
        yield return ("Legacy gacha importer rejects missing and empty lists", async () =>
        {
            await Reject("""{"info":{"uid":"123"}}""");
            await Reject("""{"info":{"uid":"123"},"list":null}""");
            await Reject("""{"info":{"uid":"123"},"list":[]}""");
        });
        yield return ("Legacy gacha importer rejects missing accounts and null rows", async () =>
        {
            await Reject("""{"list":[{"id":"1"}]}""");
            await Reject("""{"info":{"uid":"0"},"list":[{"id":"1"}]}""");
            await Reject("""{"info":{"uid":"123"},"list":[{"id":"1"},null]}""");
        });
    }

    private static Task Reject(string json)
    {
        try
        {
            UIGF3File<GachaLogItem>.ParseForImport(json);
        }
        catch (InvalidDataException)
        {
            return Task.CompletedTask;
        }
        throw new InvalidOperationException("The invalid archive was accepted.");
    }
}
