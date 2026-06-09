import React, { useState, useEffect } from 'react';
import { entriesClient, Entry } from '../services/entriesClient';
import { getErrorMessage } from '../utils/errors';
import '../styles/visualization.css';

interface SentimentDataPoint {
  date: string;
  score: number;
  count: number;
}

export const SentimentChart: React.FC = () => {
  const [entries, setEntries] = useState<Entry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [chartData, setChartData] = useState<SentimentDataPoint[]>([]);
  const [dateRange, setDateRange] = useState<'week' | 'month' | 'all'>('month');
  const [maxScore, setMaxScore] = useState(1);

  useEffect(() => {
    loadEntries();
  }, []);

  useEffect(() => {
    if (entries.length > 0) {
      generateChartData();
    }
    // Regenerates when entries or the date range change; generateChartData reads both.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [entries, dateRange]);

  const loadEntries = async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await entriesClient.listEntries(1, 200);
      setEntries(result.items);
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to load entries'));
    } finally {
      setLoading(false);
    }
  };

  const generateChartData = () => {
    const now = new Date();
    let startDate = new Date();

    if (dateRange === 'week') {
      startDate.setDate(now.getDate() - 7);
    } else if (dateRange === 'month') {
      startDate.setMonth(now.getMonth() - 1);
    } else {
      startDate = new Date(0); // All time
    }

    // Group entries by date
    const dataByDate: Record<string, { sum: number; count: number }> = {};

    entries
      .filter((entry) => new Date(entry.createdAt) >= startDate)
      .forEach((entry) => {
        const date = new Date(entry.createdAt);
        const dateKey = date.toISOString().split('T')[0];

        if (!dataByDate[dateKey]) {
          dataByDate[dateKey] = { sum: 0, count: 0 };
        }

        const score = entry.sentimentScore || 0.5;
        dataByDate[dateKey].sum += score;
        dataByDate[dateKey].count += 1;
      });

    // Convert to sorted array
    const data = Object.entries(dataByDate)
      .map(([date, { sum, count }]) => ({
        date,
        score: sum / count,
        count
      }))
      .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

    setChartData(data);
    setMaxScore(Math.max(1, Math.max(...data.map((d) => d.score))));
  };

  const getBarColor = (score: number): string => {
    if (score >= 0.7) return '#4caf50';
    if (score >= 0.4) return '#ffc107';
    return '#f44336';
  };

  const getBarHeight = (score: number): number => {
    return (score / maxScore) * 200;
  };

  const getSentimentLabel = (score: number): string => {
    if (score >= 0.7) return '😊 Happy';
    if (score >= 0.4) return '😐 Neutral';
    return '😟 Sad';
  };

  if (loading) {
    return (
      <div className="sentiment-chart-container">
        <div className="loading">Loading entries...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="sentiment-chart-container">
        <div className="error-message">
          <p>❌ {error}</p>
          <button onClick={loadEntries} className="btn btn-primary">
            Retry
          </button>
        </div>
      </div>
    );
  }

  const avgSentiment =
    entries.length > 0
      ? entries.reduce((sum, e) => sum + (e.sentimentScore || 0.5), 0) / entries.length
      : 0;

  const entriesWithSentiment = entries.filter((e) => e.sentimentScore !== null);

  return (
    <div className="sentiment-chart-container">
      <div className="chart-header">
        <h2>📈 Sentiment Trends</h2>
        <div className="date-range-selector">
          <button
            className={`range-btn ${dateRange === 'week' ? 'active' : ''}`}
            onClick={() => setDateRange('week')}
          >
            Week
          </button>
          <button
            className={`range-btn ${dateRange === 'month' ? 'active' : ''}`}
            onClick={() => setDateRange('month')}
          >
            Month
          </button>
          <button
            className={`range-btn ${dateRange === 'all' ? 'active' : ''}`}
            onClick={() => setDateRange('all')}
          >
            All
          </button>
        </div>
      </div>

      <div className="chart-stats">
        <div className="stat-card">
          <h3>📊 Average Sentiment</h3>
          <p className="stat-value">{avgSentiment.toFixed(2)}</p>
          <p className="stat-label">{getSentimentLabel(avgSentiment)}</p>
        </div>
        <div className="stat-card">
          <h3>😊 Happy Days</h3>
          <p className="stat-value">
            {chartData.filter((d) => d.score >= 0.7).length}
          </p>
          <p className="stat-label">Score ≥ 0.7</p>
        </div>
        <div className="stat-card">
          <h3>😐 Neutral Days</h3>
          <p className="stat-value">
            {chartData.filter((d) => d.score >= 0.4 && d.score < 0.7).length}
          </p>
          <p className="stat-label">0.4-0.7 range</p>
        </div>
        <div className="stat-card">
          <h3>😟 Difficult Days</h3>
          <p className="stat-value">
            {chartData.filter((d) => d.score < 0.4).length}
          </p>
          <p className="stat-label">Score &lt; 0.4</p>
        </div>
      </div>

      {chartData.length === 0 ? (
        <div className="empty-state">
          <p>📊 No sentiment data available for this period</p>
          <p>Create entries with sentiment scores to see trends!</p>
        </div>
      ) : (
        <div className="chart-canvas">
          <div className="bar-chart">
            {chartData.map((dataPoint) => (
              <div key={dataPoint.date} className="bar-group">
                <div
                  className="bar"
                  style={{
                    height: `${getBarHeight(dataPoint.score)}px`,
                    backgroundColor: getBarColor(dataPoint.score)
                  }}
                  title={`${dataPoint.date}: ${dataPoint.score.toFixed(2)} (${dataPoint.count} entries)`}
                >
                  {dataPoint.count > 0 && (
                    <span className="bar-label">{dataPoint.score.toFixed(2)}</span>
                  )}
                </div>
                <div className="bar-date">{dataPoint.date.slice(5)}</div>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="chart-legend">
        <h3>📝 Sentiment Scale</h3>
        <div className="legend-items">
          <div className="legend-item">
            <div className="legend-color" style={{ backgroundColor: '#4caf50' }}></div>
            <div className="legend-text">
              <strong>Happy</strong>
              <small>0.7 - 1.0</small>
            </div>
          </div>
          <div className="legend-item">
            <div className="legend-color" style={{ backgroundColor: '#ffc107' }}></div>
            <div className="legend-text">
              <strong>Neutral</strong>
              <small>0.4 - 0.7</small>
            </div>
          </div>
          <div className="legend-item">
            <div className="legend-color" style={{ backgroundColor: '#f44336' }}></div>
            <div className="legend-text">
              <strong>Difficult</strong>
              <small>0.0 - 0.4</small>
            </div>
          </div>
        </div>
      </div>

      {entriesWithSentiment.length === 0 && chartData.length === 0 && (
        <div className="data-note">
          💡 Tip: Enable sentiment analysis in your entries to track emotional trends
        </div>
      )}
    </div>
  );
};
