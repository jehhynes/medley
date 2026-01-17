/**
 * HTML Diff utility for comparing HTML content
 * Ported from TypeScript: https://github.com/BenedicteGiraud/html-diff-js/blob/main/src/htmlDiff.ts
 * TypeScript version with full type safety
 */

/**
 * Match result interface
 */
interface Match {
  startInBefore: number;
  startInAfter: number;
  length: number;
  endInBefore: number;
  endInAfter: number;
}

/**
 * Operation interface for diff operations
 */
interface Operation {
  action: 'equal' | 'insert' | 'delete' | 'replace';
  startInBefore: number;
  endInBefore?: number;
  startInAfter: number;
  endInAfter?: number;
}

/**
 * Type for operation action functions
 */
type OperationAction = (op: Operation, beforeTokens: string[], afterTokens?: string[]) => string;

/**
 * Check if character is end of tag
 */
const isEndOfTag = (char: string): boolean => {
  return char === '>';
};

/**
 * Check if character is start of tag
 */
const isStartOfTag = (char: string): boolean => {
  return char === '<';
};

/**
 * Check if character is whitespace
 */
const isWhitespace = (char: string): boolean => {
  return /^\s+$/.test(char);
};

/**
 * Check if token is a tag
 */
const isTag = (token: string): boolean => {
  return /^\s*<[^>]+>\s*$/.test(token);
};

/**
 * Check if token is not a tag
 */
const isntTag = (token: string): boolean => {
  return !isTag(token);
};

/**
 * Create a match object
 */
const getMatch = (startInBefore1: number, startInAfter1: number, length1: number): Match => {
  const endInBefore = startInBefore1 + length1 - 1;
  const endInAfter = startInAfter1 + length1 - 1;
  return {
    startInBefore: startInBefore1,
    startInAfter: startInAfter1,
    length: length1,
    endInBefore,
    endInAfter,
  };
};

/**
 * Convert HTML string to array of tokens
 */
const htmlToTokens = (html: string): string[] => {
  let mode: 'char' | 'tag' | 'whitespace' = 'char';
  let currentWord = '';
  const words: string[] = [];

  for (let i = 0; i < html.length; i++) {
    const char = html[i];
    if (!char) continue; // Skip if undefined

    switch (mode) {
      case 'tag':
        if (isEndOfTag(char)) {
          currentWord += '>';
          words.push(currentWord);
          currentWord = '';
          if (isWhitespace(char)) {
            mode = 'whitespace';
          } else {
            mode = 'char';
          }
        } else {
          currentWord += char;
        }
        break;
      case 'char':
        if (isStartOfTag(char)) {
          if (currentWord) {
            words.push(currentWord);
          }
          currentWord = '<';
          mode = 'tag';
        } else if (/\s/.test(char)) {
          if (currentWord) {
            words.push(currentWord);
          }
          currentWord = char;
          mode = 'whitespace';
        } else if (/[\w\\#@]+/i.test(char)) {
          currentWord += char;
        } else {
          if (currentWord) {
            words.push(currentWord);
          }
          currentWord = char;
        }
        break;
      case 'whitespace':
        if (isStartOfTag(char)) {
          if (currentWord) {
            words.push(currentWord);
          }
          currentWord = '<';
          mode = 'tag';
        } else if (isWhitespace(char)) {
          currentWord += char;
        } else {
          if (currentWord) {
            words.push(currentWord);
          }
          currentWord = char;
          mode = 'char';
        }
        break;
      default:
        throw new Error(`Unknown mode ${mode}`);
    }
  }
  if (currentWord) {
    words.push(currentWord);
  }
  return words;
};

/**
 * Find matching block between before and after tokens
 */
const findMatch = (
  beforeTokens: string[],
  indexOfBeforeLocationsInAfterTokens: Record<string, number[]>,
  startInBefore: number,
  endInBefore: number,
  startInAfter: number,
  endInAfter: number,
): Match | undefined => {
  let bestMatchInBefore = startInBefore;
  let bestMatchInAfter = startInAfter;
  let bestMatchLength = 0;
  let matchLengthAt: Record<number, number> = {};

  for (let indexInBefore = startInBefore; indexInBefore < endInBefore; indexInBefore++) {
    const newMatchLengthAt: Record<number, number> = {};
    const lookingFor = beforeTokens[indexInBefore];
    if (!lookingFor) continue; // Skip if undefined
    const locationsInAfter = indexOfBeforeLocationsInAfterTokens[lookingFor] || [];

    for (const indexInAfter of locationsInAfter) {
      if (indexInAfter < startInAfter) {
        continue;
      }
      if (indexInAfter >= endInAfter) {
        break;
      }
      const newMatchLength = (matchLengthAt[indexInAfter - 1] || 0) + 1;
      newMatchLengthAt[indexInAfter] = newMatchLength;
      if (newMatchLength > bestMatchLength) {
        bestMatchInBefore = indexInBefore - newMatchLength + 1;
        bestMatchInAfter = indexInAfter - newMatchLength + 1;
        bestMatchLength = newMatchLength;
      }
    }
    matchLengthAt = newMatchLengthAt;
  }

  if (bestMatchLength !== 0) {
    return getMatch(bestMatchInBefore, bestMatchInAfter, bestMatchLength);
  }
  return undefined;
};

/**
 * Recursively find matching blocks
 */
const recursivelyFindMatchingBlocks = (
  beforeTokens: string[],
  afterTokens: string[],
  indexOfBeforeLocationsInAfterTokens: Record<string, number[]>,
  startInBefore: number,
  endInBefore: number,
  startInAfter: number,
  endInAfter: number,
  matchingBlocks: Match[],
): Match[] => {
  const match = findMatch(
    beforeTokens,
    indexOfBeforeLocationsInAfterTokens,
    startInBefore,
    endInBefore,
    startInAfter,
    endInAfter,
  );
  if (match != null) {
    if (startInBefore < match.startInBefore && startInAfter < match.startInAfter) {
      recursivelyFindMatchingBlocks(
        beforeTokens,
        afterTokens,
        indexOfBeforeLocationsInAfterTokens,
        startInBefore,
        match.startInBefore,
        startInAfter,
        match.startInAfter,
        matchingBlocks,
      );
    }
    matchingBlocks.push(match);
    if (match.endInBefore <= endInBefore && match.endInAfter <= endInAfter) {
      recursivelyFindMatchingBlocks(
        beforeTokens,
        afterTokens,
        indexOfBeforeLocationsInAfterTokens,
        match.endInBefore + 1,
        endInBefore,
        match.endInAfter + 1,
        endInAfter,
        matchingBlocks,
      );
    }
  }
  return matchingBlocks;
};

/**
 * Create index of token locations
 */
const createIndex = (findThese: string[], inThese: string[]): Record<string, number[]> => {
  const index: Record<string, number[]> = {};
  for (const token of findThese) {
    index[token] = [];
    let idx = inThese.indexOf(token);
    while (idx !== -1) {
      index[token].push(idx);
      idx = inThese.indexOf(token, idx + 1);
    }
  }
  return index;
};

/**
 * Find all matching blocks between before and after tokens
 */
const findMatchingBlocks = (beforeTokens: string[], afterTokens: string[]): Match[] => {
  const indexOfBeforeLocationsInAfterTokens = createIndex(beforeTokens, afterTokens);
  return recursivelyFindMatchingBlocks(
    beforeTokens,
    afterTokens,
    indexOfBeforeLocationsInAfterTokens,
    0,
    beforeTokens.length,
    0,
    afterTokens.length,
    [],
  );
};

/**
 * Calculate diff operations
 */
const calculateOperations = (beforeTokens: string[], afterTokens: string[]): Operation[] => {
  let positionInBefore = 0;
  let positionInAfter = 0;
  const operations: Operation[] = [];
  const actionMap: Record<string, 'replace' | 'insert' | 'delete' | 'none'> = {
    'false,false': 'replace',
    'true,false': 'insert',
    'false,true': 'delete',
    'true,true': 'none',
  };
  const matches = findMatchingBlocks(beforeTokens, afterTokens);
  matches.push(getMatch(beforeTokens.length, afterTokens.length, 0));

  for (const match of matches) {
    const matchStartsAtCurrentPositionInBefore = positionInBefore === match.startInBefore;
    const matchStartsAtCurrentPositionInAfter = positionInAfter === match.startInAfter;
    const actionUpToMatchPositions =
      actionMap[`${matchStartsAtCurrentPositionInBefore},${matchStartsAtCurrentPositionInAfter}`];
    if (actionUpToMatchPositions && actionUpToMatchPositions !== 'none') {
      operations.push({
        action: actionUpToMatchPositions,
        startInBefore: positionInBefore,
        endInBefore: actionUpToMatchPositions !== 'insert' ? match.startInBefore - 1 : undefined,
        startInAfter: positionInAfter,
        endInAfter: actionUpToMatchPositions !== 'delete' ? match.startInAfter - 1 : undefined,
      });
    }
    if (match.length !== 0) {
      operations.push({
        action: 'equal',
        startInBefore: match.startInBefore,
        endInBefore: match.endInBefore,
        startInAfter: match.startInAfter,
        endInAfter: match.endInAfter,
      });
    }
    positionInBefore = match.endInBefore + 1;
    positionInAfter = match.endInAfter + 1;
  }

  const postProcessed: Operation[] = [];
  let lastOp: Operation = {
    action: 'equal',
    startInBefore: 0,
    startInAfter: 0
  };

  const isSingleWhitespace = (op: Operation): boolean => {
    if (op.action !== 'equal') {
      return false;
    }
    if (op.endInBefore === undefined) return false;
    if (op.endInBefore - op.startInBefore !== 0) {
      return false;
    }
    const token = beforeTokens[op.startInBefore];
    return token ? /^\s$/.test(token) : false;
  };

  const isWhitespaceOnly = (op: Operation): boolean => {
    if (op.action === 'equal') {
      return false;
    }

    let tokens: string[] = [];

    if (op.action === 'delete' || op.action === 'replace') {
      if (op.endInBefore !== undefined && op.endInBefore >= op.startInBefore) {
        tokens = beforeTokens.slice(op.startInBefore, op.endInBefore + 1);
      }
    }

    if (op.action === 'insert' || op.action === 'replace') {
      if (op.endInAfter !== undefined && op.endInAfter >= op.startInAfter) {
        const afterTokensSlice = afterTokens.slice(op.startInAfter, op.endInAfter + 1);
        if (op.action === 'replace') {
          tokens = tokens.concat(afterTokensSlice);
        } else {
          tokens = afterTokensSlice;
        }
      }
    }

    if (tokens.length === 0) return false;

    return tokens.every(token => /^\s+$/.test(token));
  };

  for (const op of operations) {
    if (
      (isSingleWhitespace(op) && lastOp.action === 'replace') ||
      (op.action === 'replace' && lastOp.action === 'replace')
    ) {
      lastOp.endInBefore = op.endInBefore;
      lastOp.endInAfter = op.endInAfter;
    } else if (isWhitespaceOnly(op)) {
      continue;
    } else {
      postProcessed.push(op);
      lastOp = op;
    }
  }
  return postProcessed;
};

/**
 * Get consecutive tokens matching predicate
 */
const consecutiveWhere = (
  start: number,
  content: string[],
  predicate: (token: string) => boolean
): string[] => {
  const sliced = content.slice(start);
  let lastMatchingIndex: number | undefined;
  for (let index = 0; index < sliced.length; index++) {
    const token = sliced[index];
    if (!token) continue; // Skip if undefined
    const answer = predicate(token);
    if (answer === true) {
      lastMatchingIndex = index;
    }
    if (answer === false) {
      break;
    }
  }
  if (lastMatchingIndex != null) {
    return sliced.slice(0, lastMatchingIndex + 1);
  }
  return [];
};

/**
 * Wrap content in HTML tag
 */
const wrap = (tag: string, content: string[]): string => {
  let rendering = '';
  let position = 0;
  const length = content.length;

  while (position < length) {
    const nonTags = consecutiveWhere(position, content, isntTag);
    position += nonTags.length;
    if (nonTags.length !== 0) {
      const joinedNonTags = nonTags.join('');
      if (/^\s+$/.test(joinedNonTags)) {
        rendering += joinedNonTags;
      } else {
        rendering += `<${tag}>${joinedNonTags}</${tag}>`;
      }
    }
    if (position >= length) {
      break;
    }
    const tags = consecutiveWhere(position, content, isTag);
    position += tags.length;
    rendering += tags.join('');
  }
  return rendering;
};

/**
 * Render equal operation
 */
const equalAction: OperationAction = (op, beforeTokens) => {
  return beforeTokens.slice(op.startInBefore, op.endInBefore === undefined ? undefined : op.endInBefore + 1).join('');
};

/**
 * Render insert operation
 */
const insertAction: OperationAction = (op, _beforeTokens, afterTokens) => {
  if (!afterTokens) return '';
  const val = afterTokens.slice(op.startInAfter, op.endInAfter === undefined ? undefined : op.endInAfter + 1);
  const joined = val.join('');
  if (/^\s+$/.test(joined)) {
    return '';
  }
  return wrap('ins', val);
};

/**
 * Render delete operation
 */
const deleteAction: OperationAction = (op, beforeTokens) => {
  const val = beforeTokens.slice(op.startInBefore, op.endInBefore === undefined ? undefined : op.endInBefore + 1);
  const joined = val.join('');
  if (/^\s+$/.test(joined)) {
    return '';
  }
  return wrap('del', val);
};

/**
 * Render replace operation
 */
const replaceAction: OperationAction = (op, beforeTokens, afterTokens) => {
  return deleteAction(op, beforeTokens) + insertAction(op, beforeTokens, afterTokens);
};

/**
 * Get operation action function
 */
const getOpMap = (action: Operation['action']): OperationAction => {
  switch (action) {
    case 'equal':
      return equalAction;
    case 'insert':
      return insertAction;
    case 'delete':
      return deleteAction;
    case 'replace':
      return replaceAction;
    default:
      throw new Error(`Unknown action ${action}`);
  }
};

/**
 * Render all operations
 */
const renderOperations = (beforeTokens: string[], afterTokens: string[], operations: Operation[]): string => {
  let rendering = '';
  for (const op of operations) {
    rendering += getOpMap(op.action)(op, beforeTokens, afterTokens);
  }
  return rendering;
};

/**
 * Remove all attributes from HTML tags
 * @param html - HTML string
 * @returns HTML with attributes removed
 */
export const removeTagAttributes = (html: string): string => {
  return html.replace(/<[^>]+>/g, (tag) => {
    return tag.replace(/ [^=]+="[^"]+"/g, '');
  });
};

/**
 * Generate HTML diff between two HTML strings
 * @param before - Original HTML
 * @param after - Modified HTML
 * @returns HTML with <ins> and <del> tags showing differences
 */
export const htmlDiff = (before: string, after: string): string => {
  const beforeWithoutAttributes = removeTagAttributes(before);
  const afterWithoutAttributes = removeTagAttributes(after);
  if (beforeWithoutAttributes === afterWithoutAttributes) {
    return before;
  }
  const beforeTokens = htmlToTokens(beforeWithoutAttributes);
  const afterTokens = htmlToTokens(afterWithoutAttributes);
  const opsTokens = calculateOperations(beforeTokens, afterTokens);
  return renderOperations(beforeTokens, afterTokens, opsTokens);
};
