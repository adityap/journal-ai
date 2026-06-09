import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getSimilarityGraph, SimilarityGraph } from '../services/graphClient';
import { getErrorMessage } from '../utils/errors';
import '../styles/visualization.css';

/**
 * Related-entries view: renders the backend TF-IDF similarity graph.
 * Entries are placed on a circle; edges connect entries with similar text
 * (thicker/brighter = more similar). Click a node to open the entry.
 */
const SIZE = 600;
const CENTER = SIZE / 2;
const RADIUS = 240;

function sentimentColor(score?: number | null): string {
  if (score == null) return '#9e9e9e';
  if (score > 0.1) return '#4caf50';
  if (score < -0.1) return '#f44336';
  return '#ff9800';
}

export const RelatedGraph: React.FC = () => {
  const navigate = useNavigate();
  const [graph, setGraph] = useState<SimilarityGraph | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    getSimilarityGraph(0.1)
      .then((g) => {
        setGraph(g);
        setError(null);
      })
      .catch((err) => setError(getErrorMessage(err, 'Failed to load related entries')))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <div className="timeline-status loading">Building relationship graph…</div>;
  }

  if (error) {
    return <div className="timeline-status error" role="alert">{error}</div>;
  }

  const nodes = graph?.nodes ?? [];
  const edges = graph?.edges ?? [];

  if (nodes.length < 2) {
    return (
      <div className="empty-state">
        <div className="empty-state-icon">🔗</div>
        <h2 className="empty-state-title">Not enough entries yet</h2>
        <p className="empty-state-text">Add a couple more entries to see how they relate.</p>
      </div>
    );
  }

  // Deterministic circular layout: node i at angle (2π·i / n).
  const positions = new Map<string, { x: number; y: number }>();
  nodes.forEach((node, i) => {
    const angle = (2 * Math.PI * i) / nodes.length - Math.PI / 2;
    positions.set(node.id, {
      x: CENTER + RADIUS * Math.cos(angle),
      y: CENTER + RADIUS * Math.sin(angle),
    });
  });

  return (
    <div className="related-graph">
      <p className="related-graph-hint">
        {edges.length > 0
          ? `${nodes.length} entries · ${edges.length} connection${edges.length === 1 ? '' : 's'} (similar text). Click an entry to open it.`
          : `${nodes.length} entries · no strong text similarities found yet.`}
      </p>
      <svg
        viewBox={`0 0 ${SIZE} ${SIZE}`}
        className="related-graph-svg"
        role="img"
        aria-label="Entry relationship graph"
      >
        {edges.map((e, i) => {
          const a = positions.get(e.source);
          const b = positions.get(e.target);
          if (!a || !b) return null;
          return (
            <line
              key={`edge-${i}`}
              x1={a.x}
              y1={a.y}
              x2={b.x}
              y2={b.y}
              stroke="#5b6ee1"
              strokeOpacity={Math.min(0.15 + e.weight, 0.9)}
              strokeWidth={1 + e.weight * 4}
            />
          );
        })}
        {nodes.map((node) => {
          const p = positions.get(node.id)!;
          const label = (node.title && node.title.trim()) || '(untitled)';
          const short = label.length > 18 ? label.slice(0, 17) + '…' : label;
          return (
            <g
              key={node.id}
              transform={`translate(${p.x}, ${p.y})`}
              style={{ cursor: 'pointer' }}
              onClick={() => navigate(`/entries/${node.id}`)}
            >
              <circle r={10} fill={sentimentColor(node.sentimentScore)} stroke="#fff" strokeWidth={2} />
              <text
                x={0}
                y={p.y > CENTER ? 26 : -16}
                textAnchor="middle"
                fontSize={13}
                fill="#333"
              >
                {short}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
};

export default RelatedGraph;
