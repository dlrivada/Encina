#pragma warning disable CA2012 // NSubstitute ValueTask stubbing pattern
using System.Buffers;
using System.Text;

using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using FsCheck;
using FsCheck.Xunit;

using Marten;
using Marten.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

namespace Encina.PropertyTests.Marten.GDPR;

/// <summary>
/// Property-based invariants of nested crypto-shredding (#1698) on the real Marten System.Text.Json serializer:
/// graphs of depth 0-4 with lists, dictionaries, nulls, shared references and 1-3 subjects.
/// </summary>
[Trait("Category", "Property")]
[Trait("Provider", "Marten")]
public sealed class CryptoShredderSerializerPropertyTests
{
    private const string Sentinel = "pii-sentinel-";
    private const string Placeholder = "[REDACTED]";

    private sealed class Harness : IDisposable
    {
        private readonly ServiceProvider _provider;

        internal Harness(ISubjectKeyProvider? keys = null)
        {
            Keys = keys ?? new InMemorySubjectKeyProvider(new FakeTimeProvider(), NullLogger<InMemorySubjectKeyProvider>.Instance);
            var services = new ServiceCollection();
            services.AddSingleton(Keys);
            services.AddSingleton(Substitute.For<IForgottenSubjectHandler>());
            _provider = services.BuildServiceProvider();
            Serializer = new CryptoShredderSerializer(
                (SystemTextJsonSerializer)new StoreOptions().Serializer(),
                _provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<CryptoShredderSerializer>.Instance,
                Placeholder);
        }

        internal ISubjectKeyProvider Keys { get; }

        internal CryptoShredderSerializer Serializer { get; }

        internal Node Read(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return Serializer.FromJson<Node>(stream);
        }

        public void Dispose() => _provider.Dispose();
    }

    [Property(MaxTest = 60)]
    public bool Json_ContainsNoPlaintext_AndOneTokenPerNonNullField(int seed)
    {
        using var harness = new Harness();
        var graph = Build(seed);

        var json = harness.Serializer.ToJson(graph);

        return !json.Contains(Sentinel, StringComparison.Ordinal)
            && CountTokens(json) == Occurrences(graph).Count;
    }

    [Property(MaxTest = 60)]
    public bool CallersGraph_IsNeverMutated(int seed)
    {
        using var harness = new Harness();
        var graph = Build(seed);
        var before = Snapshot(graph);

        harness.Serializer.ToJson(graph);

        return Snapshot(graph) == before;
    }

    [Property(MaxTest = 60)]
    public bool RoundTrip_EqualsTheOriginal(int seed)
    {
        using var harness = new Harness();
        var graph = Build(seed);

        var read = harness.Read(harness.Serializer.ToJson(graph));

        return Snapshot(read) == Snapshot(graph);
    }

    [Property(MaxTest = 40)]
    public bool ForgettingSubjects_TurnsExactlyTheirFieldsIntoThePlaceholder(int seed, byte forgetMask)
    {
        using var harness = new Harness();
        var graph = Build(seed);
        var json = harness.Serializer.ToJson(graph);
        var forgotten = Subjects.Where((_, i) => (forgetMask & (1 << i)) != 0).ToHashSet(StringComparer.Ordinal);
        foreach (var subject in forgotten)
        {
            harness.Keys.DeleteSubjectKeysAsync(subject).AsTask().GetAwaiter().GetResult();
        }

        var expected = Occurrences(graph).Select(o => forgotten.Contains(o.Subject) ? Placeholder : o.Value);
        var actual = Occurrences(harness.Read(json)).Select(o => o.Value);

        return expected.SequenceEqual(actual);
    }

    [Property(MaxTest = 40)]
    public bool KeyLookups_AreOncePerDistinctSubjectPerCall(int seed)
    {
        var real = new InMemorySubjectKeyProvider(new FakeTimeProvider(), NullLogger<InMemorySubjectKeyProvider>.Instance);
        var keys = Substitute.For<ISubjectKeyProvider>();
        keys.GetOrCreateSubjectKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => real.GetOrCreateSubjectKeyAsync(ci.ArgAt<string>(0)));
        using var harness = new Harness(keys);
        var graph = Build(seed);

        harness.Serializer.ToJson(graph);

        var distinct = Occurrences(graph).Select(o => o.Subject).Distinct().Count();
        return keys.ReceivedCalls().Count() == distinct;
    }

    [Property(MaxTest = 40)]
    public bool MissingSubjectAnywhere_ThrowsAndLeavesTheBufferEmpty(int seed)
    {
        using var harness = new Harness();
        var graph = Build(seed);
        var victims = AllNodes(graph).Where(n => n.Email is not null).ToList();
        if (victims.Count == 0)
        {
            return true;
        }

        victims[Math.Abs(seed % victims.Count)].SubjectId = null;
        var buffer = new ArrayBufferWriter<byte>();
        try
        {
            harness.Serializer.WriteTo(buffer, graph);
            return false;
        }
        catch (CryptoShreddingEncryptionException ex)
        {
            return ex.Reason == CryptoShreddingEncryptionFailureReason.SubjectIdMissing && buffer.WrittenCount == 0;
        }
    }

    [Property(MaxTest = 60)]
    public bool Token_FormatParse_RoundTrips(PositiveInt version, byte[] ciphertext)
    {
        var token = CryptoShreddingToken.Format(version.Get, new byte[12], ciphertext ?? [], new byte[16]);

        return CryptoShreddingToken.TryParse(token, out var parsed)
            && parsed.Version == version.Get
            && parsed.Ciphertext.SequenceEqual(ciphertext ?? []);
    }

    [Property(MaxTest = 60)]
    public bool Token_ArbitraryStringsWithoutThePrefix_NeverParse(string value) =>
        value is null || value.StartsWith(CryptoShreddingToken.Prefix, StringComparison.Ordinal) || !CryptoShreddingToken.TryParse(value, out _);

    // -- Graph model ---------------------------------------------------------------------------------------------

    private static readonly string[] Subjects = ["s-1", "s-2", "s-3"];

    public sealed class Node
    {
        public string? SubjectId { get; set; }

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
        public string? Email { get; set; }

        public Node? Child { get; set; }

        public List<Node?> Items { get; set; } = [];

        public Dictionary<string, Node> Map { get; set; } = [];
    }

    private static Node Build(int seed)
    {
        var random = new Random(seed);
        var counter = 0;
        Node? shared = null;
        Node Make(int depth)
        {
            var node = new Node
            {
                SubjectId = Subjects[random.Next(Subjects.Length)],
                Email = random.Next(5) == 0 ? null : $"{Sentinel}{counter++}",
            };
            if (depth >= 4)
            {
                return node;
            }

            if (random.Next(2) == 0)
            {
                node.Child = Make(depth + 1);
            }

            for (var i = random.Next(3); i > 0; i--)
            {
                node.Items.Add(random.Next(4) == 0 ? null : Make(depth + 1));
            }

            if (random.Next(3) == 0)
            {
                shared ??= Make(depth + 1);
                node.Map[$"k{counter++}"] = shared;
            }

            return node;
        }

        return Make(0);
    }

    private static IEnumerable<Node> AllNodes(Node root)
    {
        var stack = new Stack<Node>([root]);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            foreach (var child in Children(node).Reverse())
            {
                stack.Push(child);
            }
        }
    }

    private static IEnumerable<Node> Children(Node node)
    {
        if (node.Child is not null)
        {
            yield return node.Child;
        }

        foreach (var item in node.Items.OfType<Node>())
        {
            yield return item;
        }

        foreach (var value in node.Map.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Value))
        {
            yield return value;
        }
    }

    private static List<(string Subject, string Value)> Occurrences(Node root) =>
        [.. AllNodes(root).Where(n => n.Email is not null).Select(n => (n.SubjectId!, n.Email!))];

    private static string Snapshot(Node root) =>
        string.Join("|", AllNodes(root).Select(n => $"{n.SubjectId}:{n.Email}:{n.Items.Count}:{n.Map.Count}"));

    private static int CountTokens(string json)
    {
        var count = 0;
        for (var index = json.IndexOf("\"cs2:", StringComparison.Ordinal); index >= 0; index = json.IndexOf("\"cs2:", index + 1, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
