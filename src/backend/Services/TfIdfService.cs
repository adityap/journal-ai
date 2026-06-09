using System.Text.RegularExpressions;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Models;

namespace JournalAI.Backend.Services;

/// <summary>
/// Builds an entry-similarity graph using TF-IDF vectors and cosine similarity
/// over each entry's title + body text. Pure/stateless — no I/O — so it is cheap
/// to unit test and safe to register as a singleton.
/// </summary>
public class TfIdfService
{
    // Common English words that carry little signal for similarity.
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "the", "and", "for", "are", "but", "not", "you", "all", "any", "can", "had",
        "her", "was", "one", "our", "out", "day", "get", "has", "him", "his", "how",
        "its", "may", "new", "now", "old", "see", "two", "way", "who", "did", "yes",
        "this", "that", "with", "have", "from", "they", "been", "were", "what", "when",
        "your", "said", "them", "then", "than", "into", "just", "over", "also", "back",
        "after", "about", "their", "would", "there", "could", "which", "while", "these",
        "those", "very", "much", "more", "some", "such", "only", "even", "most", "like",
    };

    private static readonly Regex TokenSplit = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    /// <summary>
    /// Compute the similarity graph for a set of entries.
    /// </summary>
    /// <param name="entries">Entries to relate.</param>
    /// <param name="threshold">Minimum cosine similarity (0..1) for an edge.</param>
    public SimilarityGraphDto BuildSimilarityGraph(IReadOnlyList<Entry> entries, double threshold = 0.1)
    {
        var graph = new SimilarityGraphDto
        {
            Nodes = entries.Select(e => new GraphNodeDto
            {
                Id = e.Id,
                Title = e.Title,
                SentimentLabel = e.SentimentLabel,
                SentimentScore = e.SentimentScore,
                CreatedAt = e.CreatedAt
            }).ToList()
        };

        if (entries.Count < 2)
            return graph;

        // 1. Tokenize each document.
        var docTokens = entries.Select(e => Tokenize($"{e.Title} {e.BodyText}")).ToList();

        // 2. Document frequency per term.
        var docFreq = new Dictionary<string, int>();
        foreach (var tokens in docTokens)
        {
            foreach (var term in tokens.Distinct())
                docFreq[term] = docFreq.GetValueOrDefault(term) + 1;
        }

        int n = entries.Count;

        // 3. TF-IDF vector per document (term -> weight), L2-normalized.
        var vectors = new List<Dictionary<string, double>>(n);
        foreach (var tokens in docTokens)
        {
            var vec = new Dictionary<string, double>();
            if (tokens.Count > 0)
            {
                var counts = new Dictionary<string, int>();
                foreach (var t in tokens)
                    counts[t] = counts.GetValueOrDefault(t) + 1;

                foreach (var (term, count) in counts)
                {
                    double tf = (double)count / tokens.Count;
                    double idf = Math.Log((double)n / docFreq[term]) + 1.0; // smoothed
                    vec[term] = tf * idf;
                }

                Normalize(vec);
            }
            vectors.Add(vec);
        }

        // 4. Pairwise cosine similarity (vectors are normalized, so it's a dot product).
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                double sim = Dot(vectors[i], vectors[j]);
                if (sim >= threshold)
                {
                    graph.Edges.Add(new GraphEdgeDto
                    {
                        Source = entries[i].Id,
                        Target = entries[j].Id,
                        Weight = Math.Round(sim, 4)
                    });
                }
            }
        }

        return graph;
    }

    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return TokenSplit
            .Split(text.ToLowerInvariant())
            .Where(t => t.Length >= 3 && !StopWords.Contains(t))
            .ToList();
    }

    private static void Normalize(Dictionary<string, double> vec)
    {
        double norm = Math.Sqrt(vec.Values.Sum(v => v * v));
        if (norm <= 0) return;
        foreach (var key in vec.Keys.ToList())
            vec[key] /= norm;
    }

    private static double Dot(Dictionary<string, double> a, Dictionary<string, double> b)
    {
        // Iterate over the smaller vector for efficiency.
        if (a.Count > b.Count)
            (a, b) = (b, a);

        double sum = 0;
        foreach (var (term, weight) in a)
        {
            if (b.TryGetValue(term, out var other))
                sum += weight * other;
        }
        return sum;
    }
}
