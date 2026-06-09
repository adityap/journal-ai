namespace JournalAI.Backend.Data.Dtos;

/// <summary>
/// A node in the entry-similarity graph (one per entry).
/// </summary>
public class GraphNodeDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? SentimentLabel { get; set; }
    public decimal? SentimentScore { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// An undirected edge between two entries, weighted by cosine similarity (0..1).
/// </summary>
public class GraphEdgeDto
{
    public Guid Source { get; set; }
    public Guid Target { get; set; }
    public double Weight { get; set; }
}

/// <summary>
/// Entry-similarity graph: nodes plus the edges above the similarity threshold.
/// </summary>
public class SimilarityGraphDto
{
    public List<GraphNodeDto> Nodes { get; set; } = new();
    public List<GraphEdgeDto> Edges { get; set; } = new();
}
