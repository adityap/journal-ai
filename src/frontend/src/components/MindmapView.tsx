import React, { useState, useEffect } from 'react';
import { entriesClient, Entry } from '../services/entriesClient';
import { listCategories, buildCategoryNameMap } from '../services/categoriesClient';
import { getErrorMessage } from '../utils/errors';
import '../styles/visualization.css';

interface MindmapNode {
  id: string;
  label: string;
  value: number;
  color: string;
  children?: MindmapNode[];
}

export const MindmapView: React.FC = () => {
  const [entries, setEntries] = useState<Entry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [mindmapData, setMindmapData] = useState<MindmapNode | null>(null);
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set(['root']));
  const [categoryNames, setCategoryNames] = useState<Record<string, string>>({});

  useEffect(() => {
    loadEntries();
    listCategories()
      .then((cats) => setCategoryNames(buildCategoryNameMap(cats)))
      .catch(() => setCategoryNames({}));
  }, []);

  useEffect(() => {
    if (entries.length > 0) {
      generateMindmapData();
    }
    // Regenerates when entries or category names change; generateMindmapData reads both.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [entries, categoryNames]);

  const loadEntries = async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await entriesClient.listEntries(1, 100);
      setEntries(result.items);
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to load entries'));
    } finally {
      setLoading(false);
    }
  };

  const generateMindmapData = () => {
    // Group entries by category
    const categoryGroups: Record<string, Entry[]> = {};
    const tagGroups: Record<string, Entry[]> = {};

    entries.forEach((entry) => {
      const category =
        (entry.categoryId && categoryNames[entry.categoryId]) || 'Uncategorized';
      if (!categoryGroups[category]) {
        categoryGroups[category] = [];
      }
      categoryGroups[category].push(entry);

      // Also group by tags
      if (entry.tags && entry.tags.length > 0) {
        entry.tags.forEach((tag) => {
          if (!tagGroups[tag]) {
            tagGroups[tag] = [];
          }
          tagGroups[tag].push(entry);
        });
      }
    });

    // Build mindmap structure
    const categoryNodes: MindmapNode[] = Object.entries(categoryGroups).map(
      ([category, items]) => ({
        id: `category-${category}`,
        label: `${category} (${items.length})`,
        value: items.length,
        color: getCategoryColor(category),
        children: items.slice(0, 5).map((entry) => ({
          id: entry.id,
          label: entry.title || (entry.bodyText || '').substring(0, 30) + '...',
          value: 1,
          color: getSentimentColor(entry.sentimentScore || 0.5)
        }))
      })
    );

    const root: MindmapNode = {
      id: 'root',
      label: `📚 Journal (${entries.length} entries)`,
      value: entries.length,
      color: '#667eea',
      children: categoryNodes
    };

    setMindmapData(root);
  };

  const getCategoryColor = (category: string): string => {
    const colors = ['#667eea', '#764ba2', '#f093fb', '#4facfe', '#00f2fe'];
    const hash = category.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0);
    return colors[hash % colors.length];
  };

  const getSentimentColor = (score: number): string => {
    if (score >= 0.7) return '#4caf50'; // Happy - green
    if (score >= 0.4) return '#ffc107'; // Neutral - yellow
    return '#f44336'; // Sad - red
  };

  const toggleNode = (nodeId: string) => {
    const newExpanded = new Set(expandedNodes);
    if (newExpanded.has(nodeId)) {
      newExpanded.delete(nodeId);
    } else {
      newExpanded.add(nodeId);
    }
    setExpandedNodes(newExpanded);
  };

  const renderMindmapNode = (
    node: MindmapNode,
    depth: number = 0,
    _index: number = 0
  ): React.ReactNode => {
    const isExpanded = expandedNodes.has(node.id);
    const hasChildren = node.children && node.children.length > 0;

    return (
      <div key={node.id} className={`mindmap-node depth-${depth}`}>
        <div className="mindmap-node-content">
          <div
            className="mindmap-node-bubble"
            style={{
              backgroundColor: node.color,
              cursor: hasChildren ? 'pointer' : 'default'
            }}
            onClick={() => hasChildren && toggleNode(node.id)}
            title={node.label}
          >
            {hasChildren && (
              <span className={`mindmap-toggle ${isExpanded ? 'expanded' : ''}`}>
                {isExpanded ? '−' : '+'}
              </span>
            )}
            <span className="mindmap-label">{node.label}</span>
          </div>

          {hasChildren && isExpanded && (
            <div className="mindmap-children">
              {node.children?.map((child, idx) => (
                <div key={child.id} className="mindmap-child">
                  {renderMindmapNode(child, depth + 1, idx)}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    );
  };

  if (loading) {
    return (
      <div className="mindmap-container">
        <div className="loading">Loading entries...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="mindmap-container">
        <div className="error-message">
          <p>❌ {error}</p>
          <button onClick={loadEntries} className="btn btn-primary">
            Retry
          </button>
        </div>
      </div>
    );
  }

  if (!mindmapData || entries.length === 0) {
    return (
      <div className="mindmap-container">
        <div className="empty-state">
          <p>📚 No entries to visualize</p>
          <p>Create some entries to see your mindmap!</p>
        </div>
      </div>
    );
  }

  return (
    <div className="mindmap-container">
      <div className="mindmap-header">
        <h2>📊 Journal Mindmap</h2>
        <p className="mindmap-subtitle">
          Click on nodes to expand/collapse categories
        </p>
      </div>

      <div className="mindmap-legend">
        <div className="legend-item">
          <div className="legend-color" style={{ backgroundColor: '#4caf50' }}></div>
          <span>Happy (0.7+)</span>
        </div>
        <div className="legend-item">
          <div className="legend-color" style={{ backgroundColor: '#ffc107' }}></div>
          <span>Neutral (0.4-0.7)</span>
        </div>
        <div className="legend-item">
          <div className="legend-color" style={{ backgroundColor: '#f44336' }}></div>
          <span>Sad (&lt;0.4)</span>
        </div>
      </div>

      <div className="mindmap-canvas">
        {renderMindmapNode(mindmapData)}
      </div>

      <div className="mindmap-stats">
        <div className="stat-card">
          <h3>📝 Total Entries</h3>
          <p className="stat-value">{entries.length}</p>
        </div>
        <div className="stat-card">
          <h3>📁 Categories</h3>
          <p className="stat-value">{mindmapData.children?.length || 0}</p>
        </div>
        <div className="stat-card">
          <h3>🏷️ Avg Sentiment</h3>
          <p className="stat-value">
            {(
              entries.reduce((sum, e) => sum + (e.sentimentScore || 0.5), 0) /
              entries.length
            ).toFixed(2)}
          </p>
        </div>
      </div>
    </div>
  );
};
