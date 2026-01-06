// HTML Diff utility for comparing HTML content
// Ported from TypeScript: https://github.com/BenedicteGiraud/html-diff-js/blob/main/src/htmlDiff.ts

(function () {
  'use strict';

  const isEndOfTag = (char) => {
    return char === '>';
  };

  const isStartOfTag = (char) => {
    return char === '<';
  };
  
  const isWhitespace = (char) => {
    return /^\s+$/.test(char);
  };
  
  const isTag = (token) => {
    return /^\s*<[^>]+>\s*$/.test(token);
  };
  
  const isntTag = (token) => {
    return !isTag(token);
  };
  
  const getMatch = (startInBefore1, startInAfter1, length1) => {
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
  
  const htmlToTokens = (html) => {
    let char, currentWord, i, len, mode;
    mode = 'char';
    currentWord = '';
    const words = [];
    for (i = 0, len = html.length; i < len; i++) {
      char = html[i];
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
  
  const findMatch = (
    beforeTokens,
    indexOfBeforeLocationsInAfterTokens,
    startInBefore,
    endInBefore,
    startInAfter,
    endInAfter,
  ) => {
    let bestMatchInAfter,
      bestMatchInBefore,
      bestMatchLength,
      i,
      indexInAfter,
      indexInBefore,
      j,
      len,
      locationsInAfter,
      lookingFor,
      match,
      matchLengthAt,
      newMatchLength,
      newMatchLengthAt,
      ref,
      ref1;
    bestMatchInBefore = startInBefore;
    bestMatchInAfter = startInAfter;
    bestMatchLength = 0;
    matchLengthAt = {};
    for (
      indexInBefore = i = ref = startInBefore, ref1 = endInBefore;
      ref <= ref1 ? i < ref1 : i > ref1;
      indexInBefore = ref <= ref1 ? ++i : --i
    ) {
      newMatchLengthAt = {};
      lookingFor = beforeTokens[indexInBefore];
      locationsInAfter = indexOfBeforeLocationsInAfterTokens[lookingFor];
      for (j = 0, len = locationsInAfter.length; j < len; j++) {
        indexInAfter = locationsInAfter[j];
        if (indexInAfter < startInAfter) {
          continue;
        }
        if (indexInAfter >= endInAfter) {
          break;
        }
        if (matchLengthAt[indexInAfter - 1] == null) {
          matchLengthAt[indexInAfter - 1] = 0;
        }
        newMatchLength = matchLengthAt[indexInAfter - 1] + 1;
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
      match = getMatch(bestMatchInBefore, bestMatchInAfter, bestMatchLength);
    }
    return match;
  };
  
  const recursivelyFindMatchingBlocks = (
    beforeTokens,
    afterTokens,
    indexOfBeforeLocationsInAfterTokens,
    startInBefore,
    endInBefore,
    startInAfter,
    endInAfter,
    matchingBlocks,
  ) => {
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
  
  const createIndex = (findThese, inThese) => {
    let i, idx, len, token;
    if (findThese == null) {
      throw new Error('params must have findThese key');
    }
    if (inThese == null) {
      throw new Error('params must have inThese key');
    }
    const index = {};
    const ref = findThese;
    for (i = 0, len = ref.length; i < len; i++) {
      token = ref[i];
      index[token] = [];
      idx = inThese.indexOf(token);
      while (idx !== -1) {
        index[token].push(idx);
        idx = inThese.indexOf(token, idx + 1);
      }
    }
    return index;
  };
  
  const findMatchingBlocks = (beforeTokens, afterTokens) => {
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
  
  const calculateOperations = (beforeTokens, afterTokens) => {
    let actionUpToMatchPositions,
      i,
      index,
      j,
      lastOp,
      len,
      match,
      matchStartsAtCurrentPositionInAfter,
      matchStartsAtCurrentPositionInBefore,
      op,
      positionInAfter,
      positionInBefore;
    if (beforeTokens == null) {
      throw new Error('beforeTokens?');
    }
    if (afterTokens == null) {
      throw new Error('afterTokens?');
    }
    positionInBefore = positionInAfter = 0;
    const operations = [];
    const actionMap = {
      'false,false': 'replace',
      'true,false': 'insert',
      'false,true': 'delete',
      'true,true': 'none',
    };
    const matches = findMatchingBlocks(beforeTokens, afterTokens);
    matches.push(getMatch(beforeTokens.length, afterTokens.length, 0));
    for (index = i = 0, len = matches.length; i < len; index = ++i) {
      match = matches[index];
      matchStartsAtCurrentPositionInBefore = positionInBefore === match.startInBefore;
      matchStartsAtCurrentPositionInAfter = positionInAfter === match.startInAfter;
      actionUpToMatchPositions =
        actionMap[`${matchStartsAtCurrentPositionInBefore},${matchStartsAtCurrentPositionInAfter}`];
      if (actionUpToMatchPositions !== 'none') {
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
    const postProcessed = [];
    lastOp = {
      action: 'none',
    };
    const isSingleWhitespace = (op) => {
      if (op.action !== 'equal') {
        return false;
      }
      if (op.endInBefore === undefined) return false;
      if (op.endInBefore - op.startInBefore !== 0) {
        return false;
      }
      return /^\s$/.test(beforeTokens[op.startInBefore]);
    };
    for (j = 0; j < operations.length; j++) {
      op = operations[j];
      if (
        (isSingleWhitespace(op) && lastOp.action === 'replace') ||
        (op.action === 'replace' && lastOp.action === 'replace')
      ) {
        lastOp.endInBefore = op.endInBefore;
        lastOp.endInAfter = op.endInAfter;
      } else {
        postProcessed.push(op);
        lastOp = op;
      }
    }
    return postProcessed;
  };
  
  const consecutiveWhere = (start, content, predicate) => {
    let answer, i, index, lastMatchingIndex, len, token;
    content = content.slice(start, +content.length + 1 || 9e9);
    lastMatchingIndex = void 0;
    for (index = i = 0, len = content.length; i < len; index = ++i) {
      token = content[index];
      answer = predicate(token);
      if (answer === true) {
        lastMatchingIndex = index;
      }
      if (answer === false) {
        break;
      }
    }
    if (lastMatchingIndex != null) {
      return content.slice(0, +lastMatchingIndex + 1 || 9e9);
    }
    return [];
  };
  
  const wrap = (tag, content) => {
    let nonTags, position, rendering, tags;
    rendering = '';
    position = 0;
    const length = content.length;
    let whileCondition = true;
    while (whileCondition) {
      if (position >= length) {
        whileCondition = false;
        break;
      }
      nonTags = consecutiveWhere(position, content, isntTag);
      position += nonTags.length;
      if (nonTags.length !== 0) {
        rendering += `<${tag}>${nonTags.join('')}</${tag}>`;
      }
      if (position >= length) {
        whileCondition = false;
        break;
      }
      tags = consecutiveWhere(position, content, isTag);
      position += tags.length;
      rendering += tags.join('');
    }
    return rendering;
  };
  
  const equalAction = (op, beforeTokens) => {
    return beforeTokens.slice(op.startInBefore, op.endInBefore === undefined ? 9e9 : op.endInBefore + 1).join('');
  };
  
  const insertAction = (op, _beforeTokens, afterTokens) => {
    const val = afterTokens.slice(op.startInAfter, op.endInAfter === undefined ? 9e9 : op.endInAfter + 1);
    return wrap('ins', val);
  };
  
  const deleteAction = (op, beforeTokens) => {
    const val = beforeTokens.slice(op.startInBefore, op.endInBefore === undefined ? 9e9 : op.endInBefore + 1);
    return wrap('del', val);
  };
  
  const replaceAction = (op, beforeTokens, afterTokens) => {
    return deleteAction(op, beforeTokens) + insertAction(op, beforeTokens, afterTokens);
  };
  
  const getOpMap = (action) => {
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
  
  const renderOperations = (beforeTokens, afterTokens, operations) => {
    let i, len, op, rendering;
    rendering = '';
    for (i = 0, len = operations.length; i < len; i++) {
      op = operations[i];
      rendering += getOpMap(op.action)(op, beforeTokens, afterTokens);
    }
    return rendering;
  };
  
  const removeTagAttributes = (html) => {
    return html.replace(/<[^>]+>/g, (tag) => {
      return tag.replace(/ [^=]+="[^"]+"/g, '');
    });
  };
  
  const htmlDiff = (before, after) => {
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

  // Export for use in other modules
  window.HtmlDiff = {
    htmlDiff: htmlDiff,
    removeTagAttributes: removeTagAttributes
  };

})();
