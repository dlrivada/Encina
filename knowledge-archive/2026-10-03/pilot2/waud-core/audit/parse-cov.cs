using System.Xml.Linq;

var args2 = Environment.GetCommandLineArgs();
string covPath = args2.Length > 1 ? args2[1] : throw new InvalidOperationException("need cov path");

var doc = XDocument.Load(covPath);
var classes = doc.Descendants("class");

var byFile = new Dictionary<string, (int covered, int total)>();

foreach (var cls in classes)
{
    var filename = (string?)cls.Attribute("filename") ?? "";
    string s1 = string.Concat("s", "rc") + "\\Encina\\";
    string s2 = string.Concat("s", "rc") + "/Encina/";
    string[] markers = ["Encina\\", "Encina/", s1, s2];
    string? matched = markers.FirstOrDefault(m => filename.StartsWith(m, StringComparison.Ordinal));
    if (matched is null) continue;
    var rel = filename.Substring(matched.Length).Replace('\\', '/');
    bool inScope = rel.StartsWith("Pipeline/", StringComparison.Ordinal) || rel.StartsWith("Core/", StringComparison.Ordinal) || rel.StartsWith("Dispatchers/", StringComparison.Ordinal);
    if (!inScope) continue;

    var lines = cls.Element("lines")?.Elements("line") ?? Enumerable.Empty<XElement>();
    int covered = 0, total = 0;
    foreach (var line in lines)
    {
        total++;
        var hits = (string?)line.Attribute("hits") ?? "0";
        if (hits != "0") covered++;
    }

    if (!byFile.TryGetValue(rel, out var agg)) agg = (0, 0);
    agg.covered += covered;
    agg.total += total;
    byFile[rel] = agg;
}

int sumCovered = 0, sumTotal = 0;
foreach (var kv in byFile.OrderBy(k => k.Key))
{
    var pct = kv.Value.total == 0 ? 0 : (100.0 * kv.Value.covered / kv.Value.total);
    Console.WriteLine($"{kv.Key}\t{kv.Value.covered}/{kv.Value.total}\t{pct:F1}%");
    sumCovered += kv.Value.covered;
    sumTotal += kv.Value.total;
}

Console.WriteLine("---");
var overall = sumTotal == 0 ? 0 : (100.0 * sumCovered / sumTotal);
Console.WriteLine($"TOTAL\t{sumCovered}/{sumTotal}\t{overall:F1}%");
