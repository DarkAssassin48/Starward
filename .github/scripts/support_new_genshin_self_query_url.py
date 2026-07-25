from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
path = ROOT / "src/Starward.Core/SelfQuery/SelfQueryClient.cs"
text = path.read_text(encoding="utf-8-sig")

old_parse = '''        string game_biz = Regex.Match(url, "game_biz=([^&#]+)").Groups[1].Value;
        if (game_biz != gameBiz.ToString())
        {
            throw new ArgumentException($"Input url doesn't match the game region ({gameBiz}).", nameof(url));
        }
        this.gameBiz = gameBiz;
        authQuery = new Uri(url).Query;
'''

new_parse = '''        url = url.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? inputUri)
            || !inputUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Input url is invalid.", nameof(url));
        }

        string game_biz = Uri.UnescapeDataString(
            Regex.Match(inputUri.Query, "(?:^|[?&])game_biz=([^&#]+)", RegexOptions.IgnoreCase).Groups[1].Value);
        if (!game_biz.Equals(gameBiz.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Input url doesn't match the game region ({gameBiz}).", nameof(url));
        }
        this.gameBiz = gameBiz;
        authQuery = inputUri.Query;
'''

if old_parse not in text:
    raise RuntimeError("Could not locate SelfQueryClient URL parsing block")
text = text.replace(old_parse, new_parse, 1)

old_global = '''            if (url.StartsWith("https://cs.hoyoverse.com/event/user-game-search/hk4e/index.html"))
            {
                prefixUrl = "https://public-operation-hk4e-sg.hoyoverse.com";
            }
'''

new_global = '''            if (url.StartsWith("https://cs.hoyoverse.com/event/user-game-search/hk4e/index.html", StringComparison.OrdinalIgnoreCase))
            {
                prefixUrl = "https://public-operation-hk4e-sg.hoyoverse.com";
            }
            if (inputUri.Host.Equals("gs.hoyoverse.com", StringComparison.OrdinalIgnoreCase)
                && (inputUri.AbsolutePath.Equals("/event/user-game-search/genshin/index.html", StringComparison.OrdinalIgnoreCase)
                    || inputUri.AbsolutePath.Equals("/event/user-game-search/hk4e/index.html", StringComparison.OrdinalIgnoreCase)))
            {
                prefixUrl = "https://public-operation-hk4e-sg.hoyoverse.com";
            }
'''

if old_global not in text:
    raise RuntimeError("Could not locate global Genshin URL block")
text = text.replace(old_global, new_global, 1)

path.write_text(text, encoding="utf-8")
print("Added support for gs.hoyoverse.com Genshin account history URLs")
