import { apiClient } from './apiClient';

export interface GraphNode {
  id: string;
  title?: string | null;
  sentimentLabel?: string | null;
  sentimentScore?: number | null;
  createdAt: string;
}

export interface GraphEdge {
  source: string;
  target: string;
  weight: number;
}

export interface SimilarityGraph {
  nodes: GraphNode[];
  edges: GraphEdge[];
}

/**
 * Fetch the TF-IDF similarity graph of the user's entries.
 * `threshold` is the minimum cosine similarity (0..1) for an edge.
 */
export const getSimilarityGraph = async (threshold = 0.1): Promise<SimilarityGraph> => {
  const response = await apiClient.get<SimilarityGraph>(`/entries/graph?threshold=${threshold}`);
  return response.data;
};
