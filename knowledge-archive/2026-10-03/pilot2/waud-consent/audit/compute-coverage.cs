// Computes line coverage for Encina.Compliance.Consent files per flag from cobertura XML.
using System.Xml.Linq;

var flags = new[] { "unit", "guard", "contract", "property", "integration" };
var root = @"D:\Proyectos\Encina\.claude\worktrees\waud-consent\artifacts\audit\coverage";

foreach (var flag in flags)
{
    var dir = Path.Combine(root, flag);
    if (!Directory.Exists(dir))
    {
        Console.WriteLine($"{flag}: NO DATA (directory missing)");
        continue;
    }

    var files = Directory.GetFiles(dir, "coverage.cobertura.xml", SearchOption.AllDirectories);
    if (files.Length == 0)
    {
        Console.WriteLine($"{flag}: NO DATA (no cobertura file)");
        continue;
    }

    long covered = 0, total = 0;
    var perFile = new Dictionary<string, (long covered, long total)>();

    foreach (var f in files)
    {
        var doc = XDocument.Load(f);
        foreach (var classEl in doc.Descendants("class"))
        {
            var filename = (string?)classEl.Attribute("filename") ?? "";
            if (!filename.Replace('\\', '/').Contains("Encina.Compliance.Consent/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var line in classEl.Descendants("line"))
            {
                var hits = int.Parse((string)line.Attribute("hits")!, System.Globalization.CultureInfo.InvariantCulture);
                total++;
                if (hits > 0) covered++;

                var key = filename;
                perFile.TryGetValue(key, out var cur);
                perFile[key] = (cur.covered + (hits > 0 ? 1 : 0), cur.total + 1);
            }
        }
    }

    var pct = total == 0 ? 0 : (double)covered / total * 100;
    Console.WriteLine($"{flag}: {covered}/{total} lines = {pct:F1}%");
    foreach (var kv in perFile.OrderBy(k => k.Key))
    {
        var p = kv.Value.total == 0 ? 0 : (double)kv.Value.covered / kv.Value.total * 100;
        Console.WriteLine($"    {kv.Key}: {kv.Value.covered}/{kv.Value.total} = {p:F1}%");
    }
}
