/**
 * Simple Sentiment Analyzer
 * Analyzes text to calculate sentiment score between -1 and 1
 */

const POSITIVE_WORDS = [
  'good', 'great', 'excellent', 'amazing', 'wonderful', 'fantastic', 'awesome',
  'love', 'like', 'enjoy', 'prefer', 'happy', 'joy', 'pleased', 'grateful', 'thankful', 'perfect',
  'best', 'beautiful', 'brilliant', 'outstanding', 'terrific', 'incredible',
  'delighted', 'excited', 'thrilled', 'blessed', 'proud', 'success', 'succeed',
  'accomplished', 'achieved', 'win', 'won', 'victory', 'triumph', 'celebrate',
  'cheerful', 'optimistic', 'confident', 'peaceful', 'calm', 'relaxed',
  'nice', 'lovely', 'pleasant', 'charming', 'attractive', 'wonderful', 'favorable',
  'approved', 'approve', 'right', 'correct', 'fine', 'ok', 'okay', 'better',
  'improved', 'improve', 'improving', 'wonderful', 'splendid', 'magnificent',
  'superb', 'superior', 'impressive', 'inspiring', 'hopeful', 'wonderful', 'blessed',
];

const NEGATIVE_WORDS = [
  'bad', 'terrible', 'awful', 'horrible', 'poor', 'worst', 'hate', 'dislike', 'sad',
  'angry', 'upset', 'disappointed', 'frustrated', 'anxious', 'worried',
  'stressed', 'depressed', 'miserable', 'alone', 'lonely', 'scared', 'fear',
  'scared', 'worried', 'confused', 'lost', 'failure', 'failed', 'fail',
  'stupid', 'useless', 'worthless', 'pain', 'painful', 'suffering', 'sick',
  'ill', 'tired', 'exhausted', 'broken', 'shattered', 'devastated',
  'disgusted', 'annoyed', 'irritated', 'furious', 'enraged', 'betrayed',
  'ugly', 'unpleasant', 'rude', 'unkind', 'wrong', 'incorrect', 'disapprove',
  'disappointing', 'decline', 'declining', 'declined', 'unfavorable', 'dull',
  'boring', 'tedious', 'irritating', 'gloomy', 'somber', 'dark',
];

const INTENSIFIERS = {
  'very': 1.5,
  'really': 1.5,
  'extremely': 1.8,
  'incredibly': 1.8,
  'so': 1.3,
  'absolutely': 1.7,
  'completely': 1.5,
  'totally': 1.5,
  'quite': 1.2,
  'rather': 1.2,
  'fairly': 1.1,
  'somewhat': 0.8,
  'little': 0.8,
  'slightly': 0.8,
};

const NEGATIONS = ['not', 'no', 'never', 'neither', 'nobody', 'nothing', 'nowhere', "don't", "didn't", "doesn't", "won't", "wouldn't", "can't", "couldn't"];

/**
 * Calculate sentiment score from text
 * Returns a score between -1 (very negative) and 1 (very positive)
 */
export function analyzeSentiment(text: string): number {
  if (!text || text.trim().length === 0) {
    return 0;
  }

  const words = text.toLowerCase()
    .replace(/[^\w\s]/g, ' ')
    .split(/\s+/)
    .filter(w => w.length > 0);

  let score = 0;
  let sentimentWordCount = 0;

  for (let i = 0; i < words.length; i++) {
    const word = words[i];
    let wordScore = 0;

    // Check for positive words
    if (POSITIVE_WORDS.includes(word)) {
      wordScore = 1;
    }
    // Check for negative words
    else if (NEGATIVE_WORDS.includes(word)) {
      wordScore = -1;
    }

    // If we found a sentiment word, apply modifiers
    if (wordScore !== 0) {
      // Check for intensifiers in the previous positions (up to 2 words back)
      let intensifierMultiplier = 1;
      for (let j = 1; j <= 2 && i - j >= 0; j++) {
        const prevWord = words[i - j];
        if (INTENSIFIERS[prevWord as keyof typeof INTENSIFIERS]) {
          intensifierMultiplier = INTENSIFIERS[prevWord as keyof typeof INTENSIFIERS];
          break; // Use the closest intensifier
        }
      }

      // Check for negations in the previous positions (up to 2 words back)
      let hasNegation = false;
      for (let j = 1; j <= 2 && i - j >= 0; j++) {
        if (NEGATIONS.includes(words[i - j])) {
          hasNegation = true;
          break;
        }
      }

      // Apply negation first (reverses the sentiment), then intensifier
      if (hasNegation) {
        wordScore *= -0.4; // Negation makes sentiment slightly opposite but not full reverse
      } else {
        wordScore *= intensifierMultiplier; // Only apply intensifier if no negation
      }

      score += wordScore;
      sentimentWordCount++;
    }
  }

  // Calculate average sentiment
  let finalScore = sentimentWordCount > 0 ? score / sentimentWordCount : 0;

  // Normalize to [-1, 1] range
  finalScore = Math.max(-1, Math.min(1, finalScore));

  // Round to 2 decimal places
  const rounded = Math.round(finalScore * 100) / 100;

  return rounded;
}

/**
 * Get a label for the sentiment score
 */
export function getSentimentLabel(score: number): string {
  if (score > 0.5) return '😄 Very Positive';
  if (score > 0.1) return '🙂 Positive';
  if (score > -0.1) return '😐 Neutral';
  if (score > -0.5) return '😟 Negative';
  return '😞 Very Negative';
}
