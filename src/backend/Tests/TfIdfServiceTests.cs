using Xunit;
using JournalAI.Backend.Models;
using JournalAI.Backend.Services;

namespace JournalAI.Backend.Tests;

/// <summary>
/// Unit tests for the TF-IDF similarity graph builder.
/// </summary>
public class TfIdfServiceTests
{
    private readonly TfIdfService _svc = new();

    private static Entry Entry(string title, string body) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Title = title,
        BodyText = body,
        Confidentiality = "public",
        CreatedAt = DateTime.UtcNow,
        ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
    };

    [Fact]
    public void BuildSimilarityGraph_AlwaysReturnsOneNodePerEntry()
    {
        var entries = new List<Entry> { Entry("A", "alpha"), Entry("B", "beta"), Entry("C", "gamma") };

        var graph = _svc.BuildSimilarityGraph(entries);

        Assert.Equal(3, graph.Nodes.Count);
        Assert.All(entries, e => Assert.Contains(graph.Nodes, n => n.Id == e.Id));
    }

    [Fact]
    public void BuildSimilarityGraph_FewerThanTwoEntries_HasNoEdges()
    {
        Assert.Empty(_svc.BuildSimilarityGraph(new List<Entry>()).Edges);
        Assert.Empty(_svc.BuildSimilarityGraph(new List<Entry> { Entry("solo", "only one") }).Edges);
    }

    [Fact]
    public void BuildSimilarityGraph_SimilarEntries_AreConnected_DissimilarAreNot()
    {
        var hiking1 = Entry("Mountain hike", "We hiked the mountain trail and enjoyed the summit views");
        var hiking2 = Entry("Trail day", "A long mountain trail hike, summit was windy but the views were great");
        var cooking = Entry("Pasta night", "Boiled spaghetti and prepared a tomato garlic sauce for dinner");

        var graph = _svc.BuildSimilarityGraph(new List<Entry> { hiking1, hiking2, cooking }, threshold: 0.1);

        // The two hiking entries should be linked...
        Assert.Contains(graph.Edges, e =>
            (e.Source == hiking1.Id && e.Target == hiking2.Id) ||
            (e.Source == hiking2.Id && e.Target == hiking1.Id));

        // ...and neither should be linked to the unrelated cooking entry.
        Assert.DoesNotContain(graph.Edges, e => e.Source == cooking.Id || e.Target == cooking.Id);
    }

    [Fact]
    public void BuildSimilarityGraph_IdenticalText_HasHighSimilarity()
    {
        var a = Entry("Same", "the quick brown fox jumps over lazy dogs repeatedly today");
        var b = Entry("Same", "the quick brown fox jumps over lazy dogs repeatedly today");

        var graph = _svc.BuildSimilarityGraph(new List<Entry> { a, b }, threshold: 0.1);

        var edge = Assert.Single(graph.Edges);
        Assert.True(edge.Weight > 0.9, $"expected near-1 similarity, got {edge.Weight}");
    }

    [Fact]
    public void BuildSimilarityGraph_HighThreshold_SuppressesWeakEdges()
    {
        var a = Entry("One", "mountain trail hike summit views");
        var b = Entry("Two", "mountain weather forecast for the weekend");

        // A threshold of 1.0 should admit essentially nothing (no identical docs here).
        var graph = _svc.BuildSimilarityGraph(new List<Entry> { a, b }, threshold: 1.0);

        Assert.Empty(graph.Edges);
    }
}
